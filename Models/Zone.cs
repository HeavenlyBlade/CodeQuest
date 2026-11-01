namespace CodeQuest.Models;

public class Zone
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int OrderNumber { get; set; }

    public List<Lesson> Lessons { get; set; } = new();
    public List<Challenge> Challenges { get; set; } = new();
    public List<Question> Questions { get; set; } = new();
}
