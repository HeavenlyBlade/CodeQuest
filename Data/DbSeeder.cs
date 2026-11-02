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
            Description = "The first frontier of every coder's journey. Here, heroes learn to name things, store values, and bend numbers to their will.",
            OrderNumber = 1
        };

        // ----- Lessons (10 total) -----
        zone.Lessons.AddRange(new[]
        {
            new Lesson
            {
                OrderNumber = 1,
                Title = "The Storage Chests",
                Story = "You arrive at the entrance of Variables Valley. The village elder, an old wizard with ink-stained robes, greets you. 'Every spell needs ingredients,' he says, pointing to rows of labeled chests. 'A variable is your chest. Name it well, and it will serve you forever.'",
                Explanation = "A variable is a named storage location in memory. In C#, you declare one by writing a type, a name, and a value. The main types are: int (whole numbers), double (decimals), string (text), and bool (true/false). Variable names must start with a letter or underscore, never a digit.",
                CodeExample = "int gold = 50;\ndouble speed = 1.5;\nstring heroName = \"Ana\";\nbool hasKey = true;\n\nConsole.WriteLine(heroName); // prints Ana\nConsole.WriteLine(gold);     // prints 50"
            },
            new Lesson
            {
                OrderNumber = 2,
                Title = "The Goblin's Tricks",
                Story = "A mischievous goblin has been raiding the village chests — adding coins to some, removing them from others, and stitching names together with thread. The elder sighs. 'To catch a goblin, you must think like one. Learn the operators.'",
                Explanation = "Operators let you work with variable values. Arithmetic: + - * / % (remainder). Shorthand: x += 3 means x = x + 3. Integer division truncates: 7 / 2 = 3 (not 3.5). The % operator gives the remainder: 10 % 3 = 1. String + glues text together. A const variable can never change after declaration.",
                CodeExample = "int x = 10;\nx += 3;  // x is now 13\nx *= 2;  // x is now 26\nConsole.WriteLine(x % 5); // prints 1\n\nstring s = \"Hello\";\nConsole.WriteLine(s + \" World\"); // prints Hello World\n\nconst int MAX = 100; // can never change"
            },
            new Lesson
            {
                OrderNumber = 3,
                Title = "Numbers and Decimals",
                Story = "The valley's merchant keeps two sets of scales: one for whole gold coins, one for precise gem weights. 'You must know which scale to use,' she warns, 'or your math will betray you.'",
                Explanation = "int holds whole numbers only. double holds decimals. Dividing two ints always gives an int — 7 / 2 = 3, not 3.5. To get a decimal result, at least one number must be a double: 7.0 / 2 = 3.5. You can cast between types: (double)7 / 2 = 3.5. Math.Round() rounds a double to the nearest integer.",
                CodeExample = "int a = 7;\nint b = 2;\nConsole.WriteLine(a / b);          // prints 3 (integer division)\nConsole.WriteLine((double)a / b);  // prints 3.5\n\ndouble price = 9.75;\nConsole.WriteLine(Math.Round(price)); // prints 10"
            },
            new Lesson
            {
                OrderNumber = 4,
                Title = "The Name Weaver",
                Story = "Deep in the valley lives the Name Weaver, a spider-like creature who spins words into new shapes. She can measure a name, shout it to the sky, or snip out a piece of it. 'Strings,' she whispers, 'have many secrets.'",
                Explanation = "A string is a sequence of characters. Key properties and methods: Length (number of characters), ToUpper() / ToLower() (change case), Contains(text) (true/false if text is found), Substring(start, length) (extract part of string), Replace(old, new) (swap text), Trim() (remove leading/trailing spaces). String indexing starts at 0: \"Hello\"[0] is 'H'.",
                CodeExample = "string name = \"Hero\";\nConsole.WriteLine(name.Length);        // 4\nConsole.WriteLine(name.ToUpper());     // HERO\nConsole.WriteLine(name.Contains(\"er\")); // True\nConsole.WriteLine(name.Substring(1,2)); // er\nConsole.WriteLine(name[0]);            // H"
            },
            new Lesson
            {
                OrderNumber = 5,
                Title = "True or False",
                Story = "The valley's gatekeeper poses a question to every traveler: 'Is your heart true, or false?' She stamps a seal on those who answer correctly. Booleans, the elder explains, are the language of decisions.",
                Explanation = "A bool holds exactly one of two values: true or false. Comparison operators produce bools: == (equal), != (not equal), < (less than), > (greater than), <= (less than or equal), >= (greater than or equal). Logical operators combine bools: && (AND — both must be true), || (OR — at least one must be true), ! (NOT — flips the value).",
                CodeExample = "int hp = 80;\nbool isAlive = hp > 0;         // true\nbool fullHp = hp == 100;       // false\nbool lowHp = hp < 20;         // false\n\nConsole.WriteLine(isAlive);   // True\nConsole.WriteLine(isAlive && !fullHp); // True"
            },
            new Lesson
            {
                OrderNumber = 6,
                Title = "The Switcher's Cave",
                Story = "At the mouth of the Switcher's Cave, a shapeshifter blocks the path. It can turn a number into words and words into numbers in the blink of an eye. 'Type conversion,' the elder calls it. 'Learn it, or be stuck at the gate forever.'",
                Explanation = "Type conversion changes a value from one type to another. Explicit cast: (int)3.9 truncates to 3. Implicit: assigning an int to a double works automatically. Parsing: int.Parse(\"42\") converts a string to int — crashes if the string is not a valid number. ToString() converts any value to a string. int.TryParse() is the safe version — returns true/false and never crashes.",
                CodeExample = "double d = 3.9;\nint i = (int)d;          // i = 3 (truncated, not rounded)\n\nstring input = \"42\";\nint parsed = int.Parse(input); // parsed = 42\n\nint num = 7;\nstring s = num.ToString();     // s = \"7\"\n\nbool ok = int.TryParse(\"abc\", out int result); // ok = false"
            },
            new Lesson
            {
                OrderNumber = 7,
                Title = "Constants and the Iron Law",
                Story = "The Iron Golem guards the valley's ancient tablet — a list of laws that never change. 'Some values,' it rumbles, 'must be protected from change. That is what const is for.'",
                Explanation = "The const keyword declares a constant — a variable whose value is set once and can never be changed. Constants must be assigned at declaration. By convention, constants use PascalCase or ALL_CAPS. readonly is similar but can be set in a constructor. Use const for values that are truly fixed: mathematical constants, max sizes, version numbers.",
                CodeExample = "const double PI = 3.14159;\nconst int MAX_LEVEL = 99;\nconst string GAME_TITLE = \"CodeQuest\";\n\n// PI = 3.0; // ERROR — cannot assign to a const\nConsole.WriteLine(PI);         // 3.14159\nConsole.WriteLine(MAX_LEVEL);  // 99"
            },
            new Lesson
            {
                OrderNumber = 8,
                Title = "Reading the Signs",
                Story = "The Shadow Doppelganger mimics every traveler it meets — but always one step behind. To defeat it, you must predict its next move by reading the signs it leaves in its wake. Trace the code carefully.",
                Explanation = "Tracing code means following it line by line and tracking each variable's value. Common traps: x++ uses the old value then increments (post-increment), ++x increments first then uses (pre-increment). Integer division drops the decimal. Concatenating a string and a number with + produces a string. These tricks are exactly what the Shadow Doppelganger uses against careless heroes.",
                CodeExample = "int x = 5;\nint a = x++;  // a = 5, then x becomes 6\nint b = ++x;  // x becomes 7, then b = 7\nConsole.WriteLine(a + b); // prints 12\n\nConsole.WriteLine(\"Level\" + 2 + 1); // prints Level21\nConsole.WriteLine(\"Level\" + (2+1)); // prints Level3"
            },
            new Lesson
            {
                OrderNumber = 9,
                Title = "The Increment Twins",
                Story = "Two twin spirits haunt the valley path: one always adds one step forward, the other subtracts. They look identical but act at different moments. Confusing them has cost many heroes dearly.",
                Explanation = "x++ (post-increment) returns x then adds 1. ++x (pre-increment) adds 1 then returns x. x-- and --x work the same but subtract. Compound assignment operators are shorthand: x += n (add n), x -= n (subtract n), x *= n (multiply by n), x /= n (divide by n), x %= n (remainder). All modify x in place.",
                CodeExample = "int x = 3;\nConsole.WriteLine(x++); // prints 3, then x becomes 4\nConsole.WriteLine(++x); // x becomes 5, prints 5\n\nint y = 10;\ny -= 3;   // y = 7\ny *= 2;   // y = 14\nConsole.WriteLine(y); // prints 14"
            },
            new Lesson
            {
                OrderNumber = 10,
                Title = "The Dragon's Riddles",
                Story = "At the summit of Variables Valley, the Variable Dragon waits. It does not breathe fire. Instead it speaks — in riddles made of code. Only a hero who has mastered every lesson can parse its words and emerge victorious.",
                Explanation = "This final lesson is a review. The Variable Dragon tests everything: variable types, operators, string methods, boolean logic, type casting, constants, and increment tricks. Trace each line carefully. Remember: integer division truncates, post-increment uses before adding, string + number gives a string. You have learned all of this. Now prove it.",
                CodeExample = "int a = 5, b = 2;\nConsole.WriteLine(a / b);         // 2\nConsole.WriteLine(a % b);         // 1\nConsole.WriteLine((double)a / b); // 2.5\n\nstring s = \"Quest\";\nConsole.WriteLine(s.Length);      // 5\nConsole.WriteLine(s[0]);          // Q\n\nint x = 3;\nConsole.WriteLine(x++ * 2);       // 6 (uses 3, then x becomes 4)"
            }
        });

        // ----- Question bank -----

        // === EASY (16 questions: 8 MC + 8 Output) ===
        zone.Questions.AddRange(new[]
        {
            Mc(Difficulty.Easy, "Which keyword declares a whole-number variable in C#?",
                "string", "int", "bool", "double", "B"),
            Mc(Difficulty.Easy, "Which data type stores true or false?",
                "double", "string", "bool", "int", "C"),
            Mc(Difficulty.Easy, "Which symbol is used to ASSIGN a value to a variable?",
                "==", "=>", ":", "=", "D"),
            Mc(Difficulty.Easy, "Which data type stores text like a name or sentence?",
                "int", "bool", "double", "string", "D"),
            Mc(Difficulty.Easy, "Which of these is a valid variable name in C#?",
                "2speed", "hero-name", "_goldCount", "int", "C"),
            Mc(Difficulty.Easy, "What is the value of x after: int x = 4; x += 6;",
                "4", "6", "10", "46", "C"),
            Mc(Difficulty.Easy, "Which one starts a single-line comment in C#?",
                "//", "##", "/*", "--", "A"),
            Mc(Difficulty.Easy, "What does the % operator do?",
                "Divides two numbers", "Returns the remainder after division", "Multiplies decimals", "Converts types", "B"),

            Out(Difficulty.Easy, "int a = 3;\nint b = 4;\nConsole.WriteLine(a + b);", "7"),
            Out(Difficulty.Easy, "int x = 10;\nx -= 3;\nConsole.WriteLine(x);", "7"),
            Out(Difficulty.Easy, "string name = \"Ana\";\nConsole.WriteLine(name);", "Ana"),
            Out(Difficulty.Easy, "int x = 5;\nint y = 2;\nConsole.WriteLine(x % y);", "1"),
            Out(Difficulty.Easy, "int x = 3;\nx *= 3;\nConsole.WriteLine(x);", "9"),
            Out(Difficulty.Easy, "string a = \"Hello\";\nstring b = \"World\";\nConsole.WriteLine(a + b);", "HelloWorld"),
            Out(Difficulty.Easy, "int x = 8;\nConsole.WriteLine(x / 2);", "4"),
            Out(Difficulty.Easy, "int x = 10;\nx %= 3;\nConsole.WriteLine(x);", "1"),
        });

        // === MEDIUM (16 questions: 8 MC + 8 Output) ===
        zone.Questions.AddRange(new[]
        {
            Mc(Difficulty.Medium, "What is the result of 7 / 2 in C# when both are ints?",
                "3.5", "4", "3", "2", "C"),
            Mc(Difficulty.Medium, "Which method converts a string \"99\" to the integer 99?",
                "ToString()", "int.Parse()", "Convert()", "int.Cast()", "B"),
            Mc(Difficulty.Medium, "What does string.Length return for \"Code\"?",
                "3", "5", "4", "0", "C"),
            Mc(Difficulty.Medium, "Which comparison operator means 'not equal to'?",
                "!=", ">=", "<=", "~=", "A"),
            Mc(Difficulty.Medium, "What does ToUpper() do to a string?",
                "Reverses it", "Removes spaces", "Converts all letters to uppercase", "Converts to int", "C"),
            Mc(Difficulty.Medium, "What is the value of: bool result = (5 > 3) && (2 < 1);",
                "true", "false", "null", "error", "B"),
            Mc(Difficulty.Medium, "Which line correctly declares a constant in C#?",
                "int const MAX = 10;", "const int MAX = 10;", "constant int MAX = 10;", "final int MAX = 10;", "B"),
            Mc(Difficulty.Medium, "What does Contains() return if the substring is NOT found?",
                "0", "null", "-1", "false", "D"),

            Out(Difficulty.Medium, "int x = 5;\nx = x + 3;\nConsole.WriteLine(x);", "8"),
            Out(Difficulty.Medium, "int a = 10;\nint b = 3;\nConsole.WriteLine(a % b);", "1"),
            Out(Difficulty.Medium, "string s = \"Hello\";\nConsole.WriteLine(s.Length);", "5"),
            Out(Difficulty.Medium, "string s = \"hero\";\nConsole.WriteLine(s.ToUpper());", "HERO"),
            Out(Difficulty.Medium, "double d = 7 / 2;\nConsole.WriteLine(d);", "3"),
            Out(Difficulty.Medium, "int x = 3;\nx *= 2;\nConsole.WriteLine(x);", "6"),
            Out(Difficulty.Medium, "bool b = (10 > 5);\nConsole.WriteLine(b);", "True"),
            Out(Difficulty.Medium, "string s = \"CodeQuest\";\nConsole.WriteLine(s.Substring(0, 4));", "Code"),
        });

        // === HARD (20 questions: 10 MC + 10 Output) ===
        zone.Questions.AddRange(new[]
        {
            Mc(Difficulty.Hard, "What does (int)3.9 produce?",
                "4", "3", "3.9", "error", "B"),
            Mc(Difficulty.Hard, "What is the value of x? int x = 7 / 2;",
                "3.5", "3", "4", "2", "B"),
            Mc(Difficulty.Hard, "Which line is a valid variable declaration?",
                "int 2x = 5;", "string name = \"Ana\";", "bool flag = \"true\";", "double d == 1.5;", "B"),
            Mc(Difficulty.Hard, "What does ++x do compared to x++?",
                "They are identical",
                "++x increments first then returns; x++ returns first then increments",
                "++x decrements",
                "x++ increments first", "B"),
            Mc(Difficulty.Hard, "What is the result of \"Level\" + 2 + 1?",
                "Level3", "Level21", "3", "Error", "B"),
            Mc(Difficulty.Hard, "What does int.TryParse(\"abc\", out int r) return?",
                "0", "throws exception", "false", "null", "C"),
            Mc(Difficulty.Hard, "What is the value of: int a = 5; int b = 2; Console.WriteLine(a / b + a % b);",
                "4", "3", "2.5", "error", "B"),
            Mc(Difficulty.Hard, "Which keyword prevents a variable from being reassigned after declaration?",
                "static", "fixed", "const", "sealed", "C"),
            Mc(Difficulty.Hard, "What does Math.Round(4.5) return in C#?",
                "4", "5", "4.5", "error", "A"),
            Mc(Difficulty.Hard, "What is the result of: bool x = !(3 > 2);",
                "true", "false", "null", "error", "B"),

            Out(Difficulty.Hard, "string name = \"Hero\";\nConsole.WriteLine(name + 1);", "Hero1"),
            Out(Difficulty.Hard, "int x = 2;\nx += 5;\nx++;\nConsole.WriteLine(x);", "8"),
            Out(Difficulty.Hard, "int x = 5;\nConsole.WriteLine(x++ + ++x);", "12"),
            Out(Difficulty.Hard, "int a = 5;\nint b = 2;\nConsole.WriteLine(a / b + a % b);", "3"),
            Out(Difficulty.Hard, "string s = \"CodeQuest\";\nConsole.WriteLine(s[0]);", "C"),
            Out(Difficulty.Hard, "int x = 5;\nint a = x++;\nint b = ++x;\nConsole.WriteLine(a + b);", "12"),
            Out(Difficulty.Hard, "double d = (double)7 / 2;\nConsole.WriteLine(d);", "3.5"),
            Out(Difficulty.Hard, "int x = 10;\nx /= 3;\nConsole.WriteLine(x);", "3"),
            Out(Difficulty.Hard, "string s = \"Hello World\";\nConsole.WriteLine(s.Replace(\"World\", \"Hero\"));", "Hello Hero"),
            Out(Difficulty.Hard, "int x = 3;\nConsole.WriteLine(x-- + --x);", "4"),
        });

        // ----- Monsters (10 total, paired with lessons by OrderNumber) -----
        var lessons = zone.Lessons;

        zone.Challenges.AddRange(new[]
        {
            new Challenge
            {
                OrderNumber = 1,
                Lesson = lessons[0],
                MonsterName = "Valley Slime",
                MonsterHp = 25,
                MonsterAttack = 5,
                Difficulty = Difficulty.Easy,
                QuestionsToDefeat = 5,
                IsBoss = false,
                XpReward = 10,
                GoldReward = 5
            },
            new Challenge
            {
                OrderNumber = 2,
                Lesson = lessons[1],
                MonsterName = "Data Goblin",
                MonsterHp = 25,
                MonsterAttack = 6,
                Difficulty = Difficulty.Easy,
                QuestionsToDefeat = 5,
                IsBoss = false,
                XpReward = 12,
                GoldReward = 6
            },
            new Challenge
            {
                OrderNumber = 3,
                Lesson = lessons[2],
                MonsterName = "Stone Troll",
                MonsterHp = 30,
                MonsterAttack = 7,
                Difficulty = Difficulty.Medium,
                QuestionsToDefeat = 5,
                IsBoss = false,
                XpReward = 15,
                GoldReward = 8
            },
            new Challenge
            {
                OrderNumber = 4,
                Lesson = lessons[3],
                MonsterName = "Echo Specter",
                MonsterHp = 30,
                MonsterAttack = 7,
                Difficulty = Difficulty.Medium,
                QuestionsToDefeat = 5,
                IsBoss = false,
                XpReward = 15,
                GoldReward = 8
            },
            new Challenge
            {
                OrderNumber = 5,
                Lesson = lessons[4],
                MonsterName = "Logic Wisp",
                MonsterHp = 30,
                MonsterAttack = 8,
                Difficulty = Difficulty.Medium,
                QuestionsToDefeat = 5,
                IsBoss = false,
                XpReward = 18,
                GoldReward = 10
            },
            new Challenge
            {
                OrderNumber = 6,
                Lesson = lessons[5],
                MonsterName = "Cave Mimic",
                MonsterHp = 35,
                MonsterAttack = 9,
                Difficulty = Difficulty.Hard,
                QuestionsToDefeat = 6,
                IsBoss = false,
                XpReward = 22,
                GoldReward = 12
            },
            new Challenge
            {
                OrderNumber = 7,
                Lesson = lessons[6],
                MonsterName = "Iron Golem",
                MonsterHp = 35,
                MonsterAttack = 9,
                Difficulty = Difficulty.Hard,
                QuestionsToDefeat = 6,
                IsBoss = false,
                XpReward = 22,
                GoldReward = 12
            },
            new Challenge
            {
                OrderNumber = 8,
                Lesson = lessons[7],
                MonsterName = "Shadow Doppelganger",
                MonsterHp = 40,
                MonsterAttack = 10,
                Difficulty = Difficulty.Hard,
                QuestionsToDefeat = 6,
                IsBoss = false,
                XpReward = 25,
                GoldReward = 15
            },
            new Challenge
            {
                OrderNumber = 9,
                Lesson = lessons[8],
                MonsterName = "Twin Phantoms",
                MonsterHp = 40,
                MonsterAttack = 10,
                Difficulty = Difficulty.Hard,
                QuestionsToDefeat = 6,
                IsBoss = false,
                XpReward = 25,
                GoldReward = 15
            },
            new Challenge
            {
                OrderNumber = 10,
                Lesson = lessons[9],
                MonsterName = "Variable Dragon",
                MonsterHp = 60,
                MonsterAttack = 15,
                Difficulty = Difficulty.Hard,
                QuestionsToDefeat = 8,
                IsBoss = true,
                XpReward = 100,
                GoldReward = 50
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
