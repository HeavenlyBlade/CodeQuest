namespace CodeQuest.Models;

public class Question
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public ChallengeType Type { get; set; }
    public Difficulty Difficulty { get; set; }
    public string? ChoiceA { get; set; }
    public string? ChoiceB { get; set; }
    public string? ChoiceC { get; set; }
    public string? ChoiceD { get; set; }
    public string CorrectAnswer { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public int ZoneId { get; set; }
    public Zone? Zone { get; set; }
}