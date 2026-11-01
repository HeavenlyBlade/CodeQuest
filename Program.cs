using CodeQuest.Components;
using CodeQuest.Data;
using CodeQuest.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Render injects PORT env var — bind Kestrel to it
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// ---- Database ----
builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---- Identity ----
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<GameDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/login";
});

// ---- Application services ----
builder.Services.AddScoped<GameService>();
// Singleton: ScriptOptions are created once; ExecuteAsync is thread-safe.
builder.Services.AddSingleton<CodeExecutionService>();
builder.Services.AddHttpContextAccessor();

// ---- Authorization ----
builder.Services.AddAuthorization();

// ---- Razor Pages (needed for SSR Login/Register/Logout) ----
builder.Services.AddRazorPages();

// ---- Blazor ----
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// ---- DB migrate + seed at startup ----
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    db.Database.Migrate();
    DbSeeder.Seed(db);
    await DbSeeder.SeedUsersAsync(scope.ServiceProvider);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Database migration/seeding failed. Check connection string.");
    // Re-throw so the app fails fast with a clear error rather than serving broken pages
    throw;
}

// ---- HTTP pipeline ----
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapStaticAssets();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
