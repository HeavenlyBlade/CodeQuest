namespace CodeQuest.Models;

public class Lesson
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Story { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string CodeExample { get; set; } = string.Empty;
    public int OrderNumber { get; set; }
    public int ZoneId { get; set; }
    public Zone? Zone { get; set; }
}
