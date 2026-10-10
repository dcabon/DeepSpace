using DeepSpace.Core.Entities;
using DeepSpace.Infrastructure.Data;
using DeepSpace.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace DeepSpace.Web.Pages.Admin;

public class LoginModel : PageModel
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;

    public LoginModel(IConfiguration configuration, AppDbContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(string? username, string? password)
    {
        var adminUsername = _configuration["Admin:Username"];
        var adminPassword = _configuration["Admin:Password"];

        // Brak konfiguracji nie może oznaczać "pusty login pasuje do pustego loginu".
        var isConfigured = !string.IsNullOrEmpty(adminUsername) && !string.IsNullOrEmpty(adminPassword);
        var usernameMatches = FixedTimeEquals(username, adminUsername);
        var passwordMatches = FixedTimeEquals(password, adminPassword);

        if (isConfigured && usernameMatches && passwordMatches)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, adminUsername!),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(claims, "AdminCookie");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("AdminCookie", principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

            await RecordSessionEventAsync(AdminAuditActions.LoginSucceeded, adminUsername!);

            return Redirect("/admin");
        }

        await RecordSessionEventAsync(AdminAuditActions.LoginFailed, username ?? string.Empty);

        ErrorMessage = "Nieprawidłowy login lub hasło";
        return Page();
    }

    private async Task RecordSessionEventAsync(string action, string login)
    {
        _context.AdminAuditEvents.Add(new AdminAuditEvent
        {
            AdminLogin = login.Length > 100 ? login[..100] : login,
            Action = action,
            EntityType = "Session",
            RequestPath = HttpAuditActorProvider.DescribeRequest(HttpContext),
            IpAddress = HttpAuditActorProvider.DescribeIp(HttpContext)
        });
        await _context.SaveChangesAsync();
    }

    private static bool FixedTimeEquals(string? left, string? right)
    {
        var leftHash = SHA256.HashData(Encoding.UTF8.GetBytes(left ?? string.Empty));
        var rightHash = SHA256.HashData(Encoding.UTF8.GetBytes(right ?? string.Empty));
        return CryptographicOperations.FixedTimeEquals(leftHash, rightHash);
    }
}
