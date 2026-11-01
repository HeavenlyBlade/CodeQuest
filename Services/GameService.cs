using CodeQuest.Data;
using CodeQuest.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CodeQuest.Services;

/// <summary>
/// Core game logic: zones, lessons, monster unlocking, and battles.
/// Registered as a scoped service (one per request/circuit).
/// </summary>
public class GameService
{
    private readonly GameDbContext _db;
    private readonly CodeExecutionService _executor;

    public GameService(GameDbContext db, CodeExecutionService executor)
    {
        _db = db;
        _executor = executor;
    }

    // -------------------------------------------------------------------------
    // Zone status
    // -------------------------------------------------------------------------

    /// <summary>Result type: zone + whether it's unlocked for the player.</summary>
    public record ZoneStatus(Zone Zone, bool IsUnlocked);

    /// <summary>Returns all zones ordered by OrderNumber, with unlock status.</summary>
    public async Task<List<ZoneStatus>> GetZonesWithStatusAsync(int playerId)
    {
        var zones = await _db.Zones
            .Include(z => z.Challenges)
            .Include(z => z.Lessons)
            .OrderBy(z => z.OrderNumber)
            .ToListAsync();

        var result = new List<ZoneStatus>();
        foreach (var zone in zones)
        {
            result.Add(new ZoneStatus(zone, await IsZoneUnlockedAsync(zone, playerId, zones)));
        }
        return result;
    }

    /// <summary>
    /// A zone is unlocked if it is OrderNumber==1, or the player has won the
    /// boss battle of the previous zone.
    /// </summary>
    private async Task<bool> IsZoneUnlockedAsync(Zone zone, int playerId, List<Zone> allZones)
    {
        if (zone.OrderNumber == 1) return true;

        var prevZone = allZones.FirstOrDefault(z => z.OrderNumber == zone.OrderNumber - 1);
        if (prevZone == null) return false;

        var boss = prevZone.Challenges.FirstOrDefault(c => c.IsBoss);
        if (boss == null) return false;

        return await _db.Battles
            .AsNoTracking()
            .AnyAsync(b => b.PlayerId == playerId
                        && b.ChallengeId == boss.Id
                        && b.Status == BattleStatus.Won);
    }

    // -------------------------------------------------------------------------
    // Lesson completion
    // -------------------------------------------------------------------------

    /// <summary>Marks a lesson complete for a player. Idempotent — ignores duplicate.</summary>
    public async Task FinishLessonAsync(int lessonId, int playerId)
    {
        var progress = new LessonProgress
        {
            LessonId = lessonId,
            PlayerId = playerId,
            CompletedAt = DateTime.UtcNow
        };
        _db.LessonProgresses.Add(progress);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Unique constraint violation = already completed, ignore
            _db.Entry(progress).State = EntityState.Detached;
        }
    }

    // -------------------------------------------------------------------------
    // Monster unlock
    // -------------------------------------------------------------------------

    /// <summary>Result type: monster + unlock status + human-readable lock reason.</summary>
    public record MonsterStatus(Challenge Challenge, bool IsUnlocked, string LockReason);

    /// <summary>Returns monsters for a zone with unlock logic applied.</summary>
    public async Task<List<MonsterStatus>> GetMonsterStatusesAsync(int zoneId, int playerId)
    {
        var monsters = await _db.Challenges
            .Where(c => c.ZoneId == zoneId)
            .Include(c => c.Lesson)   // needed for lesson title in lock reason
            .OrderBy(c => c.OrderNumber)
            .ToListAsync();

        var progress = await _db.LessonProgresses
            .Where(lp => lp.PlayerId == playerId)
            .ToListAsync();

        var result = new List<MonsterStatus>();
        Challenge? prev = null;

        foreach (var monster in monsters)
        {
            // Gate 1: paired lesson must be completed
            bool linkedLessonDone = monster.LessonId == null
                || progress.Any(p => p.LessonId == monster.LessonId);

            // Gate 2: previous monster must be beaten (first monster has no prev)
            bool prevBeaten = prev == null || await _db.Battles
                .AsNoTracking()
                .AnyAsync(b => b.PlayerId == playerId
                            && b.ChallengeId == prev.Id
                            && b.Status == BattleStatus.Won);

            bool isUnlocked = linkedLessonDone && prevBeaten;

            string lockReason = isUnlocked ? "" :
                !linkedLessonDone
                    ? $"Complete '{monster.Lesson?.Title ?? "the lesson"}' first"
                    : $"Defeat {prev!.MonsterName} first";

            result.Add(new MonsterStatus(monster, isUnlocked, lockReason));
            prev = monster;
        }

        return result;
    }

    /// <summary>
    /// Returns the Challenge paired with a given lesson, or null if none.
    /// Used by LessonPage to show the "Fight!" button after lesson completion.
    /// </summary>
    public async Task<Challenge?> GetPairedChallengeAsync(int lessonId)
    {
        return await _db.Challenges
            .FirstOrDefaultAsync(c => c.LessonId == lessonId);
    }

    /// <summary>
    /// Returns true if the player has a Won battle for the given challenge.
    /// Used by ZonePage and LessonPage to check if a monster has been beaten.
    /// </summary>
    public async Task<bool> HasWonBattleAsync(int challengeId, int playerId)
    {
        return await _db.Battles.AsNoTracking()
            .AnyAsync(b => b.PlayerId == playerId
                        && b.ChallengeId == challengeId
                        && b.Status == BattleStatus.Won);
    }

    // -------------------------------------------------------------------------
    // Battle
    // -------------------------------------------------------------------------

    /// <summary>
    /// Starts a new battle or resumes an existing InProgress one.
    /// </summary>
    public async Task<Battle> StartOrResumeBattleAsync(int challengeId, int playerId)
    {
        // Resume if there's already an in-progress battle
        var existing = await _db.Battles
            .Include(b => b.Attempts)
            .FirstOrDefaultAsync(b => b.PlayerId == playerId
                                   && b.ChallengeId == challengeId
                                   && b.Status == BattleStatus.InProgress);
        if (existing != null) return existing;

        var challenge = await _db.Challenges.FindAsync(challengeId)
            ?? throw new InvalidOperationException($"Challenge {challengeId} not found.");

        var battle = new Battle
        {
            PlayerId = playerId,
            ChallengeId = challengeId,
            MonsterHpLeft = challenge.MonsterHp,
            Status = BattleStatus.InProgress
        };
        _db.Battles.Add(battle);
        await _db.SaveChangesAsync();
        return battle;
    }

    // -------------------------------------------------------------------------
    // Question drawing
    // -------------------------------------------------------------------------

    /// <summary>
    /// Wraps a question for safe browser delivery: CorrectAnswer is blanked out
    /// until the player submits. Creates a NEW object (never mutates the EF entity).
    /// </summary>
    private static Question SafeQuestion(Question q) => new Question
    {
        Id = q.Id,
        Text = q.Text,
        Type = q.Type,
        Difficulty = q.Difficulty,
        ChoiceA = q.ChoiceA,
        ChoiceB = q.ChoiceB,
        ChoiceC = q.ChoiceC,
        ChoiceD = q.ChoiceD,
        CorrectAnswer = string.Empty,  // NEVER sent to the browser before submission
        ZoneId = q.ZoneId,
        StarterCode = q.StarterCode
        // TestCasesJson intentionally omitted — not needed by the browser
    };

    public record SafeQuestionResult(Question? Question, bool NoQuestionsLeft);

    /// <summary>
    /// Returns the current question for the battle (safe copy, no CorrectAnswer).
    /// Draws a new random question if CurrentQuestionId is null.
    /// </summary>
    public async Task<SafeQuestionResult> GetCurrentQuestionAsync(int battleId)
    {
        var battle = await _db.Battles
            .Include(b => b.Attempts)
            .Include(b => b.Challenge)   // needed for ZoneId and Difficulty
            .FirstOrDefaultAsync(b => b.Id == battleId);

        if (battle == null || battle.Status != BattleStatus.InProgress)
            return new SafeQuestionResult(null, false);

        // If a question is already pinned to this battle, return it
        if (battle.CurrentQuestionId.HasValue)
        {
            var pinned = await _db.Questions.FindAsync(battle.CurrentQuestionId.Value);
            if (pinned != null) return new SafeQuestionResult(SafeQuestion(pinned), false);
        }

        // Draw a random unused question
        var usedIds = battle.Attempts.Select(a => a.QuestionId).ToHashSet();
        var available = await _db.Questions
            .Where(q => q.ZoneId == battle.Challenge!.ZoneId
                     && q.Difficulty == battle.Challenge.Difficulty
                     && q.IsActive
                     && !usedIds.Contains(q.Id))
            .ToListAsync();

        if (!available.Any())
        {
            // No unused questions — monster retreats
            battle.Status = BattleStatus.Lost;
            await _db.SaveChangesAsync();
            return new SafeQuestionResult(null, true);
        }

        var rnd = new Random();
        var chosen = available[rnd.Next(available.Count)];
        battle.CurrentQuestionId = chosen.Id;
        await _db.SaveChangesAsync();

        return new SafeQuestionResult(SafeQuestion(chosen), false);
    }

    // -------------------------------------------------------------------------
    // Answer submission
    // -------------------------------------------------------------------------

    /// <summary>Full result returned to the battle page after answer submission.</summary>
    public record AnswerResult(
        bool IsCorrect,
        string CorrectAnswer,
        int DamageToMonster,
        int DamageToPlayer,
        int MonsterHpLeft,
        int PlayerHpLeft,
        BattleStatus Status,
        int XpGained,
        int GoldGained,
        bool LeveledUp,
        int NewLevel
    );

    /// <summary>Processes a player's answer: records attempt, updates HP, checks win/loss.</summary>
    public async Task<AnswerResult> SubmitAnswerAsync(int battleId, int playerId, string answer)
    {
        var battle = await _db.Battles
            .Include(b => b.Attempts)
            .Include(b => b.Challenge)
            .FirstOrDefaultAsync(b => b.Id == battleId)
            ?? throw new InvalidOperationException("Battle not found.");

        var player = await _db.Players.FirstOrDefaultAsync(p => p.Id == playerId)
            ?? throw new InvalidOperationException("Player not found.");

        var question = await _db.Questions.FindAsync(battle.CurrentQuestionId)
            ?? throw new InvalidOperationException("Current question not found.");

        // ---- Check correctness ----
        bool isCorrect;
        if (question.Type == ChallengeType.WriteCode)
        {
            isCorrect = await EvaluateWriteCodeAsync(question, answer);
        }
        else
        {
            isCorrect = question.Type switch
            {
                ChallengeType.MultipleChoice =>
                    answer.Trim().Equals(question.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase),
                ChallengeType.PredictOutput =>
                    answer.Trim() == question.CorrectAnswer.Trim(),  // case-sensitive
                _ => false
            };
        }

        int damageToMonster = 0;
        int damageToPlayer = 0;

        var challenge = battle.Challenge!;

        if (isCorrect)
        {
            // Damage per correct answer = ceil(MonsterHp / QuestionsToDefeat)
            damageToMonster = (int)Math.Ceiling((double)challenge.MonsterHp / challenge.QuestionsToDefeat);
            battle.MonsterHpLeft = Math.Max(0, battle.MonsterHpLeft - damageToMonster);
        }
        else
        {
            damageToPlayer = challenge.MonsterAttack;
            player.CurrentHp = Math.Max(0, player.CurrentHp - damageToPlayer);
        }

        // Record the attempt
        _db.Attempts.Add(new Attempt
        {
            BattleId = battleId,
            QuestionId = question.Id,
            SubmittedAnswer = answer,
            IsCorrect = isCorrect,
            DamageDealt = damageToMonster,
            DamageTaken = damageToPlayer,
            AttemptedAt = DateTime.UtcNow
        });

        // Clear the pinned question — next call to GetCurrentQuestion will draw a new one
        battle.CurrentQuestionId = null;

        int xpGained = 0;
        int goldGained = 0;
        bool leveledUp = false;
        int newLevel = player.Level;

        // ---- Win condition ----
        if (battle.MonsterHpLeft <= 0)
        {
            battle.Status = BattleStatus.Won;

            // First-win check (current battle not yet saved, so AsNoTracking won't see it)
            bool firstWin = !await _db.Battles
                .AsNoTracking()
                .AnyAsync(b => b.PlayerId == playerId
                            && b.ChallengeId == battle.ChallengeId
                            && b.Status == BattleStatus.Won);

            if (firstWin)
            {
                xpGained = challenge.XpReward;
                goldGained = challenge.GoldReward;
                player.Xp += xpGained;
                player.Gold += goldGained;

                // Level-up loop
                int levelBefore = player.Level;
                while (player.Xp >= player.Level * 50)
                {
                    player.Xp -= player.Level * 50;
                    player.Level++;
                    player.MaxHp += 10;
                    player.CurrentHp = player.MaxHp;  // full restore on level-up
                }
                if (player.Level > levelBefore)
                {
                    leveledUp = true;
                    newLevel = player.Level;
                }
            }
        }

        // ---- Defeat condition ----
        if (player.CurrentHp == 0 && battle.Status == BattleStatus.InProgress)
        {
            battle.Status = BattleStatus.Lost;
            player.CurrentHp = player.MaxHp;  // restore HP for retry
        }

        await _db.SaveChangesAsync();

        return new AnswerResult(
            IsCorrect: isCorrect,
            CorrectAnswer: question.CorrectAnswer,
            DamageToMonster: damageToMonster,
            DamageToPlayer: damageToPlayer,
            MonsterHpLeft: battle.MonsterHpLeft,
            PlayerHpLeft: player.CurrentHp,
            Status: battle.Status,
            XpGained: xpGained,
            GoldGained: goldGained,
            LeveledUp: leveledUp,
            NewLevel: newLevel
        );
    }

    /// <summary>
    /// Runs the student's code against all test cases in the question.
    /// Returns true only if EVERY test case passes.
    /// </summary>
    private async Task<bool> EvaluateWriteCodeAsync(Question question, string code)
    {
        if (string.IsNullOrWhiteSpace(question.TestCasesJson))
            return false; // no test cases defined — treat as unanswerable

        List<TestCase> testCases;
        try
        {
            testCases = JsonSerializer.Deserialize<List<TestCase>>(question.TestCasesJson)
                        ?? new List<TestCase>();
        }
        catch
        {
            return false; // malformed JSON — teacher must fix the question
        }

        foreach (var tc in testCases)
        {
            var (success, stdout, _) = await _executor.ExecuteAsync(code, tc.Input);
            if (!success || stdout.Trim() != tc.ExpectedOutput.Trim())
                return false;
        }
        return testCases.Count > 0; // no test cases = not correct
    }

    // -------------------------------------------------------------------------
    // Tutorial
    // -------------------------------------------------------------------------

    /// <summary>Marks the tutorial as complete for the given player.</summary>
    public async Task CompleteTutorialAsync(int playerId)
    {
        var player = await _db.Players.FindAsync(playerId);
        if (player == null) return;
        player.TutorialComplete = true;
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Runs student code against a question's test cases WITHOUT recording an attempt.
    /// Used by the "Run Code" button in BattlePage for feedback before submitting.
    /// Returns a formatted string showing pass/fail per test case.
    /// </summary>
    public async Task<string> RunCodeTestsAsync(int questionId, string code)
    {
        var question = await _db.Questions.FindAsync(questionId);
        if (question == null || string.IsNullOrWhiteSpace(question.TestCasesJson))
            return "No test cases found for this question.";

        List<TestCase> testCases;
        try { testCases = JsonSerializer.Deserialize<List<TestCase>>(question.TestCasesJson) ?? new(); }
        catch { return "Error reading test cases. Contact your teacher."; }

        var sb = new System.Text.StringBuilder();
        int passed = 0;

        for (int i = 0; i < testCases.Count; i++)
        {
            var tc = testCases[i];
            var (success, stdout, stderr) = await _executor.ExecuteAsync(code, tc.Input);

            if (!success)
            {
                sb.AppendLine($"Test {i + 1}: ❌ Error — {stderr}");
            }
            else if (stdout.Trim() == tc.ExpectedOutput.Trim())
            {
                sb.AppendLine($"Test {i + 1}: ✅ Passed");
                passed++;
            }
            else
            {
                sb.AppendLine($"Test {i + 1}: ❌ Failed");
                sb.AppendLine($"  Expected: {tc.ExpectedOutput}");
                sb.AppendLine($"  Got:      {stdout}");
            }
        }

        sb.AppendLine();
        sb.Append($"{passed}/{testCases.Count} test cases passed.");
        return sb.ToString();
    }
}
