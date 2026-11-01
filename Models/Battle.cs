namespace CodeQuest.Models;

public class Battle
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public Player? Player { get; set; }
    public int ChallengeId { get; set; }
    public Challenge? Challenge { get; set; }
    public int MonsterHpLeft { get; set; }
    public BattleStatus Status { get; set; } = BattleStatus.InProgress;
    public int? CurrentQuestionId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public List<Attempt> Attempts { get; set; } = new();
}
