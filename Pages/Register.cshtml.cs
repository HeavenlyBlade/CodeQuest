using CodeQuest.Data;
using CodeQuest.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CodeQuest.Pages;

public class RegisterModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly GameDbContext _db;

    public RegisterModel(UserManager<IdentityUser> userManager, GameDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    [BindProperty] public string HeroName { get; set; } = string.Empty;
    [BindProperty] public string Email { get; set; } = string.Empty;
    [BindProperty] public string Password { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        // Create the Identity user
        var user = new IdentityUser
        {
            UserName = Email,
            Email = Email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, Password);
        if (!result.Succeeded)
        {
            ErrorMessage = string.Join(" ", result.Errors.Select(e => e.Description));
            return Page();
        }

        // Assign Student role
        await _userManager.AddToRoleAsync(user, "Student");

        // Create the linked Player record
        var player = new Player
        {
            UserId = user.Id,
            HeroName = HeroName
        };
        _db.Players.Add(player);
        await _db.SaveChangesAsync();

        return RedirectToPage("/Login");
    }
}
