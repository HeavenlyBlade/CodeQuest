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

    // WriteCode-only fields (null for MultipleChoice and PredictOutput)
    /// <summary>Pre-filled code scaffold shown in the Monaco editor.</summary>
    public string? StarterCode { get; set; }

    /// <summary>
    /// JSON-serialized List&lt;TestCase&gt;. Each test case has Input and ExpectedOutput.
    /// Example: [{"Input":"5","Expected":"25"}]
    /// </summary>
    public string? TestCasesJson { get; set; }

    public int ZoneId { get; set; }
    public Zone? Zone { get; set; }
}