namespace CodeQuest.Models;

public class LessonProgress
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public Player? Player { get; set; }
    public int LessonId { get; set; }
    public Lesson? Lesson { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
