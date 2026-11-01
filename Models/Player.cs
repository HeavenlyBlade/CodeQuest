namespace CodeQuest.Models;

public class Player
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;   // links to the Identity user
    public string HeroName { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public int Xp { get; set; } = 0;
    public int Gold { get; set; } = 0;
    public int CurrentHp { get; set; } = 100;
    public int MaxHp { get; set; } = 100;
    public bool TutorialComplete { get; set; } = false;
}
