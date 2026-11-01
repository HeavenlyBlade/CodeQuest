using CodeQuest.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace CodeQuest.Data;

public static class DbSeeder
{
    // Helper: multiple-choice question
    private static Question Mc(Difficulty difficulty, string text,
        string a, string b, string c, string d, string correct) => new()
    {
        Text = text,
        Type = ChallengeType.MultipleChoice,
        Difficulty = difficulty,
        ChoiceA = a, ChoiceB = b, ChoiceC = c, ChoiceD = d,
        CorrectAnswer = correct
    };

    // Helper: predict-the-output question
    private static Question Out(Difficulty difficulty, string code, string correct) => new()
    {
        Text = "What does this code print?\n\n" + code,
        Type = ChallengeType.PredictOutput,
        Difficulty = difficulty,
        CorrectAnswer = correct
    };

    /// <summary>
    /// Seeds the zone, lessons, monsters, and question bank.
    /// Skips if any Zone already exists (idempotent).
    /// </summary>
    public static void Seed(GameDbContext db)
    {
        if (db.Zones.Any()) return;

        var zone = new Zone
        {
            Name = "Variables Valley",
            Description = "A peaceful valley where every hero learns to store and use data.",
            OrderNumber = 1
        };

        // ----- Lessons -----
        zone.Lessons.AddRange(new[]
        {
            new Lesson
            {
                OrderNumber = 1,
                Title = "The Storage Chests",
                Story = "You arrive in Variables Valley, where villagers keep everything in labeled chests: " +
                        "gold in one, names in another, secrets in a third. The village elder hands you a blank label " +
                        "and says, 'A hero who cannot name a chest will lose everything inside it.'",
                Explanation = "A variable is a labeled chest that holds a value. Every chest has a type that says what can " +
                              "go inside: int holds whole numbers, double holds decimals, string holds text, and bool holds " +
                              "true or false. You create one by writing the type, a name, and = followed by the value. " +
                              "Anything after // on a line is a comment: a note for humans that the computer ignores.",
                CodeExample = "// Creating four chests\nint gold = 50;\ndouble speed = 1.5;\nstring heroName = \"Ana\";\nbool hasKey = true;\n\nConsole.WriteLine(gold);"
            },
            new Lesson
            {
                OrderNumber = 2,
                Title = "The Goblin's Tricks",
                Story = "Deep in the valley, a goblin has been shuffling the villagers' chests, adding to some, " +
                        "taking from others, and gluing labels together. To stop him, you must learn the same tricks yourself.",
                Explanation = "Operators let you work with values. + - * / do math, and % gives the remainder after dividing " +
                              "(10 % 3 is 1). Shortcuts like x += 3 mean x = x + 3, and x *= 2 means x = x * 2. " +
                              "Dividing two whole numbers gives a whole number, so 7 / 2 is 3, not 3.5. " +
                              "With strings, + glues text together. A chest marked const can never change after it is created.",
                CodeExample = "int x = 5;\nx += 3;                // x is now 8\nConsole.WriteLine(x % 3);  // prints 2\n\nstring s = \"Hi\";\nConsole.WriteLine(s + \"!\"); // prints Hi!\n\nconst int Max = 10;    // can never change"
            },
            new Lesson
            {
                OrderNumber = 3,
                Title = "The Dragon's Riddles",
                Story = "At the top of the valley sleeps the Variable Dragon. It does not breathe fire. " +
                        "It speaks in riddles, and a hero who reads code carelessly will fall for every one.",
                Explanation = "Slow down and trace the code line by line. In x++ the chest is used first, then increased. " +
                              "In ++x it is increased first, then used. Joining a string and a number with + gives a string, " +
                              "so 'Hero' + 1 becomes Hero1. Whole-number division throws away the decimal part. " +
                              "Variable names cannot start with a digit, and the = sign assigns a value while == compares two values.",
                CodeExample = "int x = 5;\nint a = x++;   // a is 5, then x becomes 6\nint b = ++x;   // x becomes 7, then b is 7\n\nConsole.WriteLine(a + b);   // prints 12\nConsole.WriteLine(\"Hero\" + 1);  // prints Hero1"
            }
        });

        // ----- Question bank -----
        zone.Questions.AddRange(new[]
        {
            // Easy deck (6 questions)
            Mc(Difficulty.Easy, "Which keyword declares a whole-number variable in C#?",
                "string", "int", "bool", "double", "B"),
            Mc(Difficulty.Easy, "Which data type stores true or false?",
                "bool", "int", "string", "char", "A"),
            Mc(Difficulty.Easy, "Which symbol assigns a value to a variable?",
                "==", "=", "=>", ":=", "B"),
            Mc(Difficulty.Easy, "Which data type stores text?",
                "int", "string", "bool", "double", "B"),
            Mc(Difficulty.Easy, "Which one starts a single-line comment in C#?",
                "//", "#", "--", "<!--", "A"),
            Out(Difficulty.Easy, "int a = 2;\nint b = 3;\nConsole.WriteLine(a + b);", "5"),

            // Medium deck (6 questions)
            Out(Difficulty.Medium, "int x = 5;\nx = x + 3;\nConsole.WriteLine(x);", "8"),
            Out(Difficulty.Medium, "int a = 10;\nint b = 3;\nConsole.WriteLine(a % b);", "1"),
            Out(Difficulty.Medium, "string s = \"Hi\";\nConsole.WriteLine(s + \"!\");", "Hi!"),
            Out(Difficulty.Medium, "int x = 3;\nx *= 2;\nConsole.WriteLine(x);", "6"),
            Out(Difficulty.Medium, "double d = 7 / 2;\nConsole.WriteLine(d);", "3"),
            Mc(Difficulty.Medium, "Which line correctly declares a constant?",
                "int const Max = 10;", "const int Max = 10;", "constant int Max = 10;", "final int Max = 10;", "B"),

            // Hard deck (6 questions)
            Mc(Difficulty.Hard, "What value does x hold?\n\nint x = 7 / 2;",
                "3.5", "3", "4", "2", "B"),
            Out(Difficulty.Hard, "string name = \"Hero\";\nConsole.WriteLine(name + 1);", "Hero1"),
            Out(Difficulty.Hard, "int x = 2;\nx += 5;\nx++;\nConsole.WriteLine(x);", "8"),
            Mc(Difficulty.Hard, "Which line is a valid variable declaration?",
                "int 2x = 5;", "string name = \"Ana\";", "bool flag = \"true\";", "double d == 1.5;", "B"),
            Out(Difficulty.Hard, "int x = 5;\nConsole.WriteLine(x++ + ++x);", "12"),
            Out(Difficulty.Hard, "int a = 5;\nint b = 2;\nConsole.WriteLine(a / b + a % b);", "3"),
        });

        // ----- Monsters (with OrderNumber) -----
        var lesson1 = zone.Lessons[0];
        var lesson2 = zone.Lessons[1];
        var lesson3 = zone.Lessons[2];

        zone.Challenges.AddRange(new[]
        {
            new Challenge
            {
                OrderNumber = 1,
                Lesson = lesson1,   // paired with The Storage Chests
                MonsterName = "Valley Slime",
                MonsterHp = 30,
                MonsterAttack = 5,
                Difficulty = Difficulty.Easy,
                QuestionsToDefeat = 3,
                IsBoss = false,
                XpReward = 10,
                GoldReward = 5
            },
            new Challenge
            {
                OrderNumber = 2,
                Lesson = lesson2,   // paired with The Goblin's Tricks
                MonsterName = "Data Goblin",
                MonsterHp = 30,
                MonsterAttack = 8,
                Difficulty = Difficulty.Medium,
                QuestionsToDefeat = 3,
                IsBoss = false,
                XpReward = 15,
                GoldReward = 8
            },
            new Challenge
            {
                OrderNumber = 3,
                Lesson = lesson3,   // paired with The Dragon's Riddles
                MonsterName = "Variable Dragon",
                MonsterHp = 40,
                MonsterAttack = 12,
                Difficulty = Difficulty.Hard,
                QuestionsToDefeat = 4,
                IsBoss = true,
                XpReward = 40,
                GoldReward = 25
            }
        });

        db.Zones.Add(zone);
        db.SaveChanges();
    }

    /// <summary>
    /// Seeds Identity roles and the teacher account.
    /// Call AFTER db.Database.Migrate().
    /// </summary>
    public static async Task SeedUsersAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var config = services.GetRequiredService<IConfiguration>();
        var db = services.GetRequiredService<GameDbContext>();

        // Ensure roles exist
        foreach (var role in new[] { "Student", "Teacher" })
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        // Seed teacher account
        const string teacherEmail = "teacher@codequest.local";
        if (await userManager.FindByEmailAsync(teacherEmail) == null)
        {
            var password = config["TeacherSeedPassword"]
                ?? throw new InvalidOperationException(
                    "TeacherSeedPassword not configured. Add it to appsettings.Development.json.");
            var teacher = new IdentityUser
            {
                UserName = teacherEmail,
                Email = teacherEmail,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(teacher, password);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(teacher, "Teacher");
        }
    }
}
