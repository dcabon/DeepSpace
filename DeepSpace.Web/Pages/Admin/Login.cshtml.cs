using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DeepSpace.Web.Pages.Admin;

public class LoginModel : PageModel
{
    private readonly IConfiguration _configuration;

    public LoginModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string? Error { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var adminUsername = _configuration["Admin:Username"];
        var adminPassword = _configuration["Admin:Password"];

        if (Username == adminUsername && Password == adminPassword)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, Username),
                new(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(claims, "AdminCookie");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("AdminCookie", principal);

            return Redirect("/admin");
        }

        Error = "Nieprawidłowa nazwa użytkownika lub hasło";
        return Page();
    }
}
