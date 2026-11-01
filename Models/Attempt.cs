namespace CodeQuest.Models;

public class Attempt
{
    public int Id { get; set; }

    public int BattleId { get; set; }
    public Battle? Battle { get; set; }

    public int QuestionId { get; set; }
    public Question? Question { get; set; }

    public string SubmittedAnswer { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int DamageDealt { get; set; }
    public int DamageTaken { get; set; }
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
}