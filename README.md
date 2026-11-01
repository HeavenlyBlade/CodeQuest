# CodeQuest 🗡️

A gamified C# learn-to-code RPG built with Blazor Web App (Interactive Server, .NET 10).  
Students read story-based lessons, then battle monsters by answering C# questions.  
Correct answers damage the monster; wrong answers hurt the hero.

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

---

## Setup

```bash
# 1. Clone or copy the project
git clone <repo-url>
cd CodeQuest
```

### Add the teacher seed password

Open (or create) `appsettings.Development.json` and make sure it contains:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "TeacherSeedPassword": "Teacher@1234"
}
```

> **Note:** Change `Teacher@1234` to any password that meets the policy (6+ chars, at least one digit).

---

## Run

```bash
dotnet run
```

On first run the app will:
1. Apply EF Core migrations (creates `codequest.db`)
2. Seed the **Variables Valley** zone with 3 lessons, 3 monsters, and 18 questions
3. Create roles `Student` and `Teacher`
4. Create the teacher account `teacher@codequest.local`

Navigate to `https://localhost:PORT` (port shown in console output).

---

## Accounts

| Role    | Email                       | Password        |
|---------|-----------------------------|-----------------|
| Teacher | teacher@codequest.local     | Teacher@1234    |
| Student | Register via `/register`    | Your choice     |

---

## Student flow

1. Go to `/register`, pick a hero name and create your account.
2. From the **World Map** (`/map`), click **Enter Zone** on Variables Valley.
3. Read all three lessons and click **Finish Lesson** on each.
4. Once all lessons are done, the **Slime** unlocks — click **Fight!**
5. Answer questions correctly to damage the monster; wrong answers cost you HP.
6. Defeat Slime → Goblin → Variable Dragon (boss) in order.
7. Beating the boss unlocks the next zone (when added by the teacher).
8. Check the **Leaderboard** (`/leaderboard`) to see how you rank.

---

## Teacher flow

Log in as `teacher@codequest.local`, then use the sidebar links:

| Page | What you can do |
|---|---|
| `/teacher/zones` | Create zones, edit name / description / order |
| `/teacher/lessons` | Add lessons (title, story, explanation, code example, order) |
| `/teacher/monsters` | Add monsters, set HP / ATK / difficulty / questions-to-defeat |
| `/teacher/questions` | Add questions, toggle active/inactive, delete (if no attempts) |
| `/teacher/stats` | View per-zone / per-difficulty accuracy and top missed questions |

### How to add a zone, lesson, and question

1. **Zone** – Go to `/teacher/zones`, fill in the form at the top, click **Add Zone**.
2. **Lesson** – Go to `/teacher/lessons`, select the zone in the dropdown, fill in title / story / explanation / code example, click **Add Lesson**.
3. **Question** – Go to `/teacher/questions`, select the zone and difficulty, choose *Multiple Choice* or *Predict Output*, enter the text and correct answer, click **Add Question**.

> A monster needs at least `QuestionsToDefeat + 3` active questions in its zone/difficulty to keep battles interesting. The Questions page shows a warning if any monster has fewer than that.

---

## Project structure

```
CodeQuest/
├── Components/
│   ├── Layout/        # MainLayout, NavMenu
│   ├── Pages/         # Blazor pages (Map, Zone, Lesson, Battle, Leaderboard)
│   │   └── Teacher/   # Teacher admin pages
│   ├── App.razor
│   └── Routes.razor
├── Data/
│   ├── GameDbContext.cs
│   └── DbSeeder.cs
├── Models/            # EF entity classes + enums
├── Pages/             # SSR Razor Pages (Login, Register, Logout)
├── Services/
│   └── GameService.cs # Core game logic
├── wwwroot/
│   └── app.css        # Fantasy-RPG theme
├── appsettings.json
├── appsettings.Development.json   ← add TeacherSeedPassword here
└── Program.cs
```

---

## Game rules summary

- **Lesson gate** – all lessons in a zone must be finished before the first monster unlocks.
- **Monster order** – monsters unlock in sequence; defeat the previous one to fight the next.
- **Zone gate** – defeat the boss of zone N to unlock zone N+1.
- **Battle** – if you leave and return, your in-progress battle is resumed.
- **Rewards** – XP and gold are awarded only on the *first* win per monster.
- **Level-up** – XP threshold = Level × 50; extra XP carries over; each level-up gives +10 MaxHp.
