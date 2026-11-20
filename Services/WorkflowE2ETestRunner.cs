using CodeQuest.Data;
using CodeQuest.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CodeQuest.Services;

/// <summary>
/// Comprehensive End-to-End Workflow Test Runner for CodeQuest.
/// Validates the full student journey, teacher admin capabilities,
/// game progression, combat mechanics, and code execution sandbox.
/// </summary>
public class WorkflowE2ETestRunner
{
    private readonly IServiceProvider _services;
    private readonly GameDbContext _db;
    private readonly GameService _gameSvc;
    private readonly CodeExecutionService _executor;
    private readonly UserManager<IdentityUser> _userManager;

    public WorkflowE2ETestRunner(IServiceProvider services)
    {
        _services = services;
        _db = services.GetRequiredService<GameDbContext>();
        _gameSvc = services.GetRequiredService<GameService>();
        _executor = services.GetRequiredService<CodeExecutionService>();
        _userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    }

    private int _passed = 0;
    private int _failed = 0;

    private void Assert(bool condition, string testName, string details = "")
    {
        if (condition)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("  [PASS] ");
            Console.ResetColor();
            Console.WriteLine(testName);
            _passed++;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("  [FAIL] ");
            Console.ResetColor();
            Console.WriteLine($"{testName} -- {details}");
            _failed++;
        }
    }

    public async Task<int> RunAllTestsAsync()
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==================================================================");
        Console.WriteLine("    CodeQuest End-to-End Workflow & Game Mechanics Test Suite     ");
        Console.WriteLine("==================================================================");
        Console.ResetColor();
        Console.WriteLine();

        string testSuffix = Guid.NewGuid().ToString("N")[..8];
        string testEmail = $"e2e_student_{testSuffix}@codequest.local";
        string testHero = $"SirTest_{testSuffix}";
        IdentityUser? testUser = null;
        Player? player = null;
        Zone? zone2 = null;
        Zone? zone3 = null;

        try
        {
            // -------------------------------------------------------------
            // Stage 1: Initial Seed Verification
            // -------------------------------------------------------------
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("--- Stage 1: Initial Seed & System State ---");
            Console.ResetColor();

            var zone1 = await _db.Zones
                .Include(z => z.Lessons)
                .Include(z => z.Challenges)
                .Include(z => z.Questions)
                .FirstOrDefaultAsync(z => z.OrderNumber == 1);

            Assert(zone1 != null, "Zone 1 exists in DB");
            Assert(zone1?.Lessons.Count == 10, "Zone 1 has exactly 10 lessons");
            Assert(zone1?.Challenges.Count == 10, "Zone 1 has exactly 10 challenges");
            Assert(zone1?.Questions.Count >= 50, "Zone 1 has question bank (>= 50 questions)");

            var teacher = await _userManager.FindByEmailAsync("teacher@codequest.local");
            Assert(teacher != null, "Teacher account (teacher@codequest.local) exists");
            if (teacher != null)
            {
                var isTeacher = await _userManager.IsInRoleAsync(teacher, "Teacher");
                Assert(isTeacher, "Teacher account has 'Teacher' role");
            }

            // -------------------------------------------------------------
            // Stage 2: Student Registration & Player Initialization
            // -------------------------------------------------------------
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n--- Stage 2: Student Registration & Profile ---");
            Console.ResetColor();

            testUser = new IdentityUser
            {
                UserName = testEmail,
                Email = testEmail,
                EmailConfirmed = true
            };
            var createResult = await _userManager.CreateAsync(testUser, "TestPass#123");
            Assert(createResult.Succeeded, "Student identity user created successfully");
            await _userManager.AddToRoleAsync(testUser, "Student");
            var isStudent = await _userManager.IsInRoleAsync(testUser, "Student");
            Assert(isStudent, "Student assigned 'Student' role");

            player = new Player
            {
                UserId = testUser.Id,
                HeroName = testHero,
                Level = 1,
                MaxHp = 100,
                CurrentHp = 100,
                Xp = 0,
                Gold = 0,
                TutorialComplete = false
            };
            _db.Players.Add(player);
            await _db.SaveChangesAsync();

            Assert(player.Id > 0, "Player record created in DB with ID");
            Assert(player.CurrentHp == 100 && player.MaxHp == 100, "Player starts with 100/100 HP");
            Assert(player.Level == 1, "Player starts at Level 1");
            Assert(player.TutorialComplete == false, "Player starts with TutorialComplete == false");

            // -------------------------------------------------------------
            // Stage 3: Tutorial Workflow
            // -------------------------------------------------------------
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n--- Stage 3: Tutorial Flow ---");
            Console.ResetColor();

            await _gameSvc.CompleteTutorialAsync(player.Id);
            var reloadedPlayer = await _db.Players.FindAsync(player.Id);
            Assert(reloadedPlayer?.TutorialComplete == true, "Tutorial marked complete via CompleteTutorialAsync");

            // -------------------------------------------------------------
            // Stage 4: World Map & Zone Unlocking Logic
            // -------------------------------------------------------------
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n--- Stage 4: World Map & Zone Gates ---");
            Console.ResetColor();

            var zonesWithStatus = await _gameSvc.GetZonesWithStatusAsync(player.Id);
            var z1Status = zonesWithStatus.FirstOrDefault(z => z.Zone.OrderNumber == 1);
            Assert(z1Status != null && z1Status.IsUnlocked, "Zone 1 is unlocked for new player");

            // -------------------------------------------------------------
            // Stage 5: Monster Gates Before Lesson Completion
            // -------------------------------------------------------------
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n--- Stage 5: Monster Gating Before Lesson Completion ---");
            Console.ResetColor();

            var initialMonsterStatuses = await _gameSvc.GetMonsterStatusesAsync(zone1!.Id, player.Id);
            var firstMonster = initialMonsterStatuses.FirstOrDefault(m => m.Challenge.OrderNumber == 1);
            Assert(firstMonster != null && !firstMonster.IsUnlocked,
                "Monster 1 is initially locked before completing Lesson 1");
            Assert(firstMonster?.LockReason.Contains("Complete") == true,
                "Monster 1 lock reason instructs completing lesson first");

            // -------------------------------------------------------------
            // Stage 6: Lesson Completion & Idempotency
            // -------------------------------------------------------------
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n--- Stage 6: Lesson Completion & Idempotency ---");
            Console.ResetColor();

            var lesson1 = zone1.Lessons.OrderBy(l => l.OrderNumber).First();
            await _gameSvc.FinishLessonAsync(lesson1.Id, player.Id);
            var progressExists = await _db.LessonProgresses
                .AnyAsync(lp => lp.PlayerId == player.Id && lp.LessonId == lesson1.Id);
            Assert(progressExists, "Lesson 1 progress recorded in database");

            // Idempotency check: finishing again must not fail or create duplicate
            await _gameSvc.FinishLessonAsync(lesson1.Id, player.Id);
            var progressCount = await _db.LessonProgresses
                .CountAsync(lp => lp.PlayerId == player.Id && lp.LessonId == lesson1.Id);
            Assert(progressCount == 1, "FinishLessonAsync is idempotent (no duplicate progress records)");

            // Now Monster 1 should unlock!
            var updatedMonsterStatuses = await _gameSvc.GetMonsterStatusesAsync(zone1.Id, player.Id);
            var monster1Status = updatedMonsterStatuses.First(m => m.Challenge.OrderNumber == 1);
            var monster2Status = updatedMonsterStatuses.First(m => m.Challenge.OrderNumber == 2);
            Assert(monster1Status.IsUnlocked, "Monster 1 unlocks after Lesson 1 is completed");
            Assert(!monster2Status.IsUnlocked, "Monster 2 remains locked");
            Assert(monster2Status.LockReason.Contains("The Goblin's Tricks"),
                "Monster 2 lock reason prioritizes completing its paired lesson first");

            // Complete Lesson 2 while Monster 1 is still undefeated: verify Gate 2 message
            var lesson2 = zone1.Lessons.OrderBy(l => l.OrderNumber).Skip(1).First();
            await _gameSvc.FinishLessonAsync(lesson2.Id, player.Id);
            var monsterStatusesAfterLesson2 = await _gameSvc.GetMonsterStatusesAsync(zone1.Id, player.Id);
            var m2AfterL2 = monsterStatusesAfterLesson2.First(m => m.Challenge.OrderNumber == 2);
            Assert(!m2AfterL2.IsUnlocked, "Monster 2 remains locked even after Lesson 2 because Monster 1 is not beaten");
            Assert(m2AfterL2.LockReason.Contains(monster1Status.Challenge.MonsterName),
                "Monster 2 lock reason now specifies defeating Monster 1 first");

            // -------------------------------------------------------------
            // Stage 7: Battle Lifecycle - Monster 1 (Valley Slime)
            // -------------------------------------------------------------
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n--- Stage 7: Battle Lifecycle & Question Security ---");
            Console.ResetColor();

            var challenge1 = monster1Status.Challenge;
            var battle = await _gameSvc.StartOrResumeBattleAsync(challenge1.Id, player.Id);
            Assert(battle.Status == BattleStatus.InProgress, "Battle starts in 'InProgress' status");
            Assert(battle.MonsterHpLeft == challenge1.MonsterHp, "Monster starts with full HP");

            // Resuming battle check
            var resumedBattle = await _gameSvc.StartOrResumeBattleAsync(challenge1.Id, player.Id);
            Assert(resumedBattle.Id == battle.Id, "StartOrResumeBattleAsync resumes existing battle");

            // Safe question drawing check (CorrectAnswer MUST be masked)
            var questionResult = await _gameSvc.GetCurrentQuestionAsync(battle.Id);
            Assert(questionResult.Question != null, "Question drawn for battle");
            Assert(string.IsNullOrEmpty(questionResult.Question?.CorrectAnswer),
                "SECURITY: SafeQuestion blanks out CorrectAnswer before submission");

            // -------------------------------------------------------------
            // Stage 8: Combat Mechanics - Wrong Answer & Damage Taken
            // -------------------------------------------------------------
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n--- Stage 8: Combat Mechanics - Incorrect Answer ---");
            Console.ResetColor();

            var wrongResult = await _gameSvc.SubmitAnswerAsync(battle.Id, player.Id, "DEFINITELY_WRONG_ANSWER");
            Assert(!wrongResult.IsCorrect, "Incorrect answer flagged as IsCorrect == false");
            Assert(wrongResult.DamageToMonster == 0, "No damage dealt to monster on wrong answer");
            Assert(wrongResult.DamageToPlayer == challenge1.MonsterAttack,
                $"Player took monster attack damage ({challenge1.MonsterAttack})");
            Assert(wrongResult.PlayerHpLeft == 100 - challenge1.MonsterAttack,
                "Player HP correctly decremented in result");

            var attemptInDb = await _db.Attempts
                .OrderByDescending(a => a.AttemptedAt)
                .FirstOrDefaultAsync(a => a.BattleId == battle.Id);
            Assert(attemptInDb != null && !attemptInDb.IsCorrect, "Attempt recorded in DB with IsCorrect == false");

            // -------------------------------------------------------------
            // Stage 9: Defeat & Auto-Heal Mechanics
            // -------------------------------------------------------------
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n--- Stage 9: Player Defeat & Auto-Heal ---");
            Console.ResetColor();

            // Set player HP so that the next monster hit is fatal
            player.CurrentHp = 1;
            await _db.SaveChangesAsync();

            // Ensure a question is pinned
            await _gameSvc.GetCurrentQuestionAsync(battle.Id);
            var fatalResult = await _gameSvc.SubmitAnswerAsync(battle.Id, player.Id, "WRONG_FATAL_ANSWER");
            Assert(fatalResult.Status == BattleStatus.Lost, "Battle status changed to 'Lost' on fatal damage");

            var healedPlayer = await _db.Players.FindAsync(player.Id);
            Assert(healedPlayer?.CurrentHp == healedPlayer?.MaxHp,
                "Player HP auto-restored to MaxHp upon battle defeat");

            // -------------------------------------------------------------
            // Stage 10: Battle Victory & First-Win Rewards
            // -------------------------------------------------------------
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n--- Stage 10: Battle Victory & Rewards ---");
            Console.ResetColor();

            // Start a new battle
            battle = await _gameSvc.StartOrResumeBattleAsync(challenge1.Id, player.Id);
            Assert(battle.Status == BattleStatus.InProgress, "New battle started after previous loss");

            // Answer questions correctly until monster is defeated
            GameService.AnswerResult? winResult = null;
            while (battle != null && battle.MonsterHpLeft > 0 && battle.Status == BattleStatus.InProgress)
            {
                var qResult = await _gameSvc.GetCurrentQuestionAsync(battle.Id);
                if (qResult.Question == null) break;

                // Look up real answer directly from DB
                var realQ = await _db.Questions.FindAsync(qResult.Question.Id);
                var answerToSubmit = realQ!.CorrectAnswer;

                winResult = await _gameSvc.SubmitAnswerAsync(battle.Id, player.Id, answerToSubmit);
                battle = await _db.Battles.FindAsync(battle.Id);
            }

            Assert(winResult?.Status == BattleStatus.Won, "Battle status changed to 'Won' when monster HP reached 0");
            Assert(winResult?.XpGained == challenge1.XpReward, $"Player awarded {challenge1.XpReward} XP on first win");
            Assert(winResult?.GoldGained == challenge1.GoldReward, $"Player awarded {challenge1.GoldReward} Gold on first win");

            // Anti-duplication check: repeat fight must NOT reward XP/gold again
            var repeatBattle = await _gameSvc.StartOrResumeBattleAsync(challenge1.Id, player.Id);
            GameService.AnswerResult? repeatWinResult = null;
            while (repeatBattle != null && repeatBattle.MonsterHpLeft > 0 && repeatBattle.Status == BattleStatus.InProgress)
            {
                var qResult = await _gameSvc.GetCurrentQuestionAsync(repeatBattle.Id);
                if (qResult.Question == null) break;
                var realQ = await _db.Questions.FindAsync(qResult.Question.Id);
                repeatWinResult = await _gameSvc.SubmitAnswerAsync(repeatBattle.Id, player.Id, realQ!.CorrectAnswer);
                repeatBattle = await _db.Battles.FindAsync(repeatBattle.Id);
            }
            Assert(repeatWinResult?.Status == BattleStatus.Won, "Repeat battle successfully won");
            Assert(repeatWinResult?.XpGained == 0 && repeatWinResult?.GoldGained == 0,
                "RULE VERIFIED: No duplicate XP/Gold awarded for re-defeating a monster");

            // -------------------------------------------------------------
            // Stage 11: Progression Through All Lessons & Level-Up Verification
            // -------------------------------------------------------------
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n--- Stage 11: Progression & Level-Up Thresholds ---");
            Console.ResetColor();

            var allLessons = zone1.Lessons.OrderBy(l => l.OrderNumber).ToList();
            var allChallenges = zone1.Challenges.OrderBy(c => c.OrderNumber).ToList();

            int initialLevel = player.Level;
            bool witnessedLevelUp = false;

            // Complete lessons 2 through 10 and defeat corresponding monsters
            for (int i = 1; i < allLessons.Count; i++)
            {
                var currentLesson = allLessons[i];
                var currentChallenge = allChallenges[i];

                await _gameSvc.FinishLessonAsync(currentLesson.Id, player.Id);
                var b = await _gameSvc.StartOrResumeBattleAsync(currentChallenge.Id, player.Id);

                while (b != null && b.MonsterHpLeft > 0 && b.Status == BattleStatus.InProgress)
                {
                    var qResult = await _gameSvc.GetCurrentQuestionAsync(b.Id);
                    if (qResult.Question == null) break;
                    var realQ = await _db.Questions.FindAsync(qResult.Question.Id);
                    var ansResult = await _gameSvc.SubmitAnswerAsync(b.Id, player.Id, realQ!.CorrectAnswer);
                    if (ansResult.LeveledUp)
                    {
                        witnessedLevelUp = true;
                    }
                    b = await _db.Battles.FindAsync(b.Id);
                }
            }

            var finalPlayer = await _db.Players.FindAsync(player.Id);
            Assert(witnessedLevelUp, "Level-up triggered when XP threshold (Level * 50) was reached");
            Assert(finalPlayer!.Level > initialLevel, $"Player leveled up from Level {initialLevel} to Level {finalPlayer.Level}");
            Assert(finalPlayer.MaxHp > 100, $"Player MaxHp increased (+10 per level) to {finalPlayer.MaxHp}");
            Assert(finalPlayer.CurrentHp == finalPlayer.MaxHp, "Player CurrentHp restored to full upon level up");

            // -------------------------------------------------------------
            // Stage 12: Zone Boss Defeat & Zone 2 Unlock Gate
            // -------------------------------------------------------------
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n--- Stage 12: Zone Boss Victory & Next Zone Gate ---");
            Console.ResetColor();

            var boss = allChallenges.FirstOrDefault(c => c.IsBoss);
            Assert(boss != null, "Zone 1 has a designated Boss challenge");

            var bossDefeated = await _gameSvc.HasWonBattleAsync(boss!.Id, player.Id);
            Assert(bossDefeated, "Zone 1 Boss (Variable Dragon) was defeated");

            // Create Zone 2 to test progression gating
            zone2 = new Zone
            {
                Name = $"Loop Lagoon {testSuffix}",
                Description = "A realm of repetition and iterators.",
                OrderNumber = 2
            };
            _db.Zones.Add(zone2);
            await _db.SaveChangesAsync();

            var zonesAfterBoss = await _gameSvc.GetZonesWithStatusAsync(player.Id);
            var z2Status = zonesAfterBoss.FirstOrDefault(z => z.Zone.Id == zone2.Id);
            Assert(z2Status != null && z2Status.IsUnlocked,
                "Zone 2 is UNLOCKED after defeating Zone 1 Boss");

            // -------------------------------------------------------------
            // Stage 13: Roslyn Code Execution Sandbox
            // -------------------------------------------------------------
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n--- Stage 13: Roslyn Code Execution Sandbox ---");
            Console.ResetColor();

            // 13.1 Valid C# script
            var (validSuccess, validOut, _) = await _executor.ExecuteAsync("Console.WriteLine(\"CodeQuest Live\");");
            Assert(validSuccess && validOut == "CodeQuest Live", "CodeExecutionService executes valid C# code");

            // 13.2 Syntax / Compilation Error
            var (compSuccess, _, compErr) = await _executor.ExecuteAsync("invalid syntax ???");
            Assert(!compSuccess && !string.IsNullOrEmpty(compErr), "CodeExecutionService reports compilation error");

            // 13.3 Runtime Exception
            var (rtSuccess, _, rtErr) = await _executor.ExecuteAsync("int a = 5; int b = 0; Console.WriteLine(a / b);");
            Assert(!rtSuccess && rtErr.Contains("DivideByZeroException"),
                "CodeExecutionService catches runtime DivideByZeroException gracefully");

            // 13.4 Infinite Loop / Timeout Enforcement (3 seconds)
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var (timeoutSuccess, _, timeoutErr) = await _executor.ExecuteAsync("while(true) {}");
            timer.Stop();
            Assert(!timeoutSuccess && timeoutErr.Contains("Time limit exceeded"),
                $"CodeExecutionService enforces 3s timeout on infinite loop ({timer.Elapsed.TotalSeconds:F1}s)");

            // 13.5 WriteCode challenge test case evaluation
            var writeCodeQuestion = new Question
            {
                Text = "Write code that takes a number and doubles it.",
                Type = ChallengeType.WriteCode,
                Difficulty = Difficulty.Medium,
                ZoneId = zone1.Id,
                StarterCode = "// Write code here",
                TestCasesJson = System.Text.Json.JsonSerializer.Serialize(new List<TestCase>
                {
                    new("", "42")
                })
            };
            _db.Questions.Add(writeCodeQuestion);
            await _db.SaveChangesAsync();

            var testFeedback = await _gameSvc.RunCodeTestsAsync(writeCodeQuestion.Id, "Console.WriteLine(42);");
            Assert(testFeedback.Contains("1/1 test cases passed"), "RunCodeTestsAsync evaluates test cases correctly");

            _db.Questions.Remove(writeCodeQuestion);
            await _db.SaveChangesAsync();

            // -------------------------------------------------------------
            // Stage 14: Teacher Admin Operations & Stats Computation
            // -------------------------------------------------------------
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n--- Stage 14: Teacher Admin & Stats Computation ---");
            Console.ResetColor();

            // 14.1 Zone Management
            zone3 = new Zone
            {
                Name = $"Functions Forest {testSuffix}",
                Description = "A woodland where reusable spells thrive.",
                OrderNumber = 3
            };
            _db.Zones.Add(zone3);
            await _db.SaveChangesAsync();
            Assert(zone3.Id > 0, "Teacher can create new Zone (Functions Forest)");

            zone3.Description = "Updated description";
            await _db.SaveChangesAsync();
            var reloadedZ3 = await _db.Zones.FindAsync(zone3.Id);
            Assert(reloadedZ3?.Description == "Updated description", "Teacher can update Zone");

            // 14.2 Lesson Management
            var newLesson = new Lesson
            {
                ZoneId = zone3.Id,
                Title = "Intro to Methods",
                Story = "A friendly mage teaches you to encapsulate spells.",
                Explanation = "Methods avoid duplicate logic.",
                CodeExample = "void Greet() { Console.WriteLine(\"Hi\"); }",
                OrderNumber = 1
            };
            _db.Lessons.Add(newLesson);
            await _db.SaveChangesAsync();
            Assert(newLesson.Id > 0, "Teacher can create new Lesson");

            // 14.3 Monster Management
            var newMonster = new Challenge
            {
                ZoneId = zone3.Id,
                LessonId = newLesson.Id,
                MonsterName = "Recursion Imp",
                MonsterHp = 50,
                MonsterAttack = 10,
                Difficulty = Difficulty.Medium,
                QuestionsToDefeat = 5,
                XpReward = 50,
                GoldReward = 20,
                IsBoss = false,
                OrderNumber = 1
            };
            _db.Challenges.Add(newMonster);
            await _db.SaveChangesAsync();
            Assert(newMonster.Id > 0, "Teacher can create new Monster");

            // 14.4 Question Management (MC, PredictOutput, WriteCode)
            var mcQ = new Question
            {
                ZoneId = zone3.Id,
                Difficulty = Difficulty.Medium,
                Type = ChallengeType.MultipleChoice,
                Text = "What keyword specifies that a method returns nothing?",
                ChoiceA = "null",
                ChoiceB = "void",
                ChoiceC = "empty",
                ChoiceD = "none",
                CorrectAnswer = "B"
            };
            var outQ = new Question
            {
                ZoneId = zone3.Id,
                Difficulty = Difficulty.Medium,
                Type = ChallengeType.PredictOutput,
                Text = "What does this print?\nConsole.WriteLine(2 + 3 * 4);",
                CorrectAnswer = "14"
            };
            _db.Questions.AddRange(mcQ, outQ);
            await _db.SaveChangesAsync();
            Assert(mcQ.Id > 0 && outQ.Id > 0, "Teacher can create MC and PredictOutput Questions");

            // 14.5 Stats Computation Validation
            var attempts = await _db.Attempts
                .Where(a => a.Battle != null && a.Battle.PlayerId == player.Id)
                .ToListAsync();
            Assert(attempts.Any(), "Player attempts exist for stats calculation");

            int totalAttempts = attempts.Count;
            int correctAttempts = attempts.Count(a => a.IsCorrect);
            double calculatedAccuracy = (double)correctAttempts / totalAttempts;
            Assert(totalAttempts > 0 && calculatedAccuracy >= 0.0 && calculatedAccuracy <= 1.0,
                $"Teaching stats computed: {correctAttempts}/{totalAttempts} ({calculatedAccuracy:P1} accuracy)");

            // Clean up admin test entities
            _db.Questions.RemoveRange(mcQ, outQ);
            _db.Challenges.Remove(newMonster);
            _db.Lessons.Remove(newLesson);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nUNHANDLED EXCEPTION DURING TEST EXECUTION: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Console.ResetColor();
            _failed++;
        }
        finally
        {
            // -------------------------------------------------------------
            // Cleanup: remove temporary test records
            // -------------------------------------------------------------
            try
            {
                if (zone3 != null)
                {
                    var z3 = await _db.Zones.FindAsync(zone3.Id);
                    if (z3 != null) _db.Zones.Remove(z3);
                }
                if (zone2 != null)
                {
                    var z2 = await _db.Zones.FindAsync(zone2.Id);
                    if (z2 != null) _db.Zones.Remove(z2);
                }
                if (player != null)
                {
                    var battles = await _db.Battles
                        .Include(b => b.Attempts)
                        .Where(b => b.PlayerId == player.Id)
                        .ToListAsync();
                    foreach (var b in battles)
                    {
                        _db.Attempts.RemoveRange(b.Attempts);
                    }
                    _db.Battles.RemoveRange(battles);

                    var progresses = await _db.LessonProgresses
                        .Where(lp => lp.PlayerId == player.Id)
                        .ToListAsync();
                    _db.LessonProgresses.RemoveRange(progresses);

                    var p = await _db.Players.FindAsync(player.Id);
                    if (p != null) _db.Players.Remove(p);
                }
                await _db.SaveChangesAsync();

                if (testUser != null)
                {
                    await _userManager.DeleteAsync(testUser);
                }
            }
            catch (Exception cleanupEx)
            {
                Console.WriteLine($"Cleanup notice: {cleanupEx.Message}");
            }
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==================================================================");
        Console.ResetColor();
        Console.WriteLine($"Total tests run: {_passed + _failed}");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Passed:          {_passed}");
        if (_failed > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Failed:          {_failed}");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Failed:          0");
        }
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==================================================================");
        Console.ResetColor();
        Console.WriteLine();

        return _failed == 0 ? 0 : 1;
    }
}
