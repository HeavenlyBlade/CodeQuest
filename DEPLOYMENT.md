# CodeQuest — Deployment Guide

## Stack
- **Hosting**: Render.com (free tier, Docker)
- **Database**: Neon.tech (free tier, PostgreSQL)
- **Keep-alive**: UptimeRobot (free, pings every 5 min)
- **CI/CD**: GitHub → Render auto-deploy on push to `main`

---

## Step 1 — Set up Neon database

1. Sign up at https://neon.tech (free, no credit card)
2. Create a new project: name it `codequest`
3. Neon creates a default database. Go to **Connection Details**
4. Copy the **connection string** — it looks like:
   ```
   postgresql://user:password@ep-xxx.us-east-2.aws.neon.tech/neondb?sslmode=require
   ```
5. Keep this — you will paste it into Render

---

## Step 2 — Push to GitHub

1. Create a new GitHub repo (public or private)
2. Push the CodeQuest project:
   ```bash
   git init
   git add .
   git commit -m "Initial commit"
   git remote add origin https://github.com/YOUR_USERNAME/codequest.git
   git push -u origin main
   ```

---

## Step 3 — Deploy on Render

1. Sign up at https://render.com (free)
2. Click **New** → **Blueprint**
3. Connect your GitHub repo
4. Render reads `render.yaml` automatically
5. Before deploying, set the environment variables marked `sync: false`:
   - `ConnectionStrings__DefaultConnection` → paste your Neon connection string
   - `TeacherSeedPassword` → choose a strong password (e.g. `Teacher@Secure2024!`)
6. Click **Apply** — Render builds the Docker image and deploys
7. First deploy takes ~3-5 minutes (Docker build + DB migration + seeding)
8. Your app is live at: `https://codequest.onrender.com` (or similar)

---

## Step 4 — Set up UptimeRobot (prevent sleep)

Render free tier sleeps after 15 minutes of inactivity.
UptimeRobot pings every 5 minutes to keep it awake.

1. Sign up at https://uptimerobot.com (free)
2. Click **Add New Monitor**
3. Monitor Type: **HTTP(s)**
4. Friendly Name: `CodeQuest`
5. URL: `https://your-app.onrender.com`
6. Monitoring Interval: **5 minutes**
7. Click **Create Monitor**

UptimeRobot also emails you if the app goes down.

---

## Environment Variables Reference

| Variable | Where to set | Example value |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | Render Dashboard | `postgresql://user:pass@host/db?sslmode=require` |
| `TeacherSeedPassword` | Render Dashboard | `Teacher@Secure2024!` |
| `ASPNETCORE_ENVIRONMENT` | render.yaml (already set) | `Production` |

---

## Local Development

For local development with PostgreSQL:
1. Install PostgreSQL locally or use a local Neon branch
2. Update `appsettings.Development.json` with your local connection string
3. Run migrations: `dotnet ef database update`
4. Run: `dotnet run`

Alternatively, to use SQLite locally:
1. Swap the connection string in `appsettings.Development.json` to:
   `Data Source=codequest.db`
2. Change `UseNpgsql` to `UseSqlite` in Program.cs temporarily
3. **Do not commit this change**

---

## Redeployment

Every `git push` to `main` triggers an automatic redeploy on Render.
The app runs `db.Database.Migrate()` on startup — new migrations apply automatically.

## Troubleshooting

- **App crashes on startup**: Check Render logs. Most likely cause: `ConnectionStrings__DefaultConnection` not set.
- **Teacher login fails**: Check `TeacherSeedPassword` env var is set in Render Dashboard.
- **Render shows loading page for ~1 minute**: Normal cold start after UptimeRobot gap or first visit.
