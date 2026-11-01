namespace CodeQuest.Models;

/// <summary>
/// One input/expected-output pair for a WriteCode question.
/// Stored as JSON in Question.TestCasesJson.
/// </summary>
public record TestCase(string Input, string ExpectedOutput);
