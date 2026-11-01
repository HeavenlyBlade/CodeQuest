namespace CodeQuest.Models;

public class Challenge
{
    public int Id { get; set; }
    public string MonsterName { get; set; } = string.Empty;
    public int MonsterHp { get; set; } = 30;
    public int MonsterAttack { get; set; } = 10;
    public bool IsBoss { get; set; } = false;
    public int XpReward { get; set; } = 10;
    public int GoldReward { get; set; } = 5;
    public Difficulty Difficulty { get; set; } = Difficulty.Easy;
    public int QuestionsToDefeat { get; set; } = 3;
    public int OrderNumber { get; set; }
    public int ZoneId { get; set; }
    public Zone? Zone { get; set; }

    /// <summary>The lesson this monster is paired with. Complete this lesson to unlock the monster.</summary>
    public int? LessonId { get; set; }
    public Lesson? Lesson { get; set; }
}
