using DeepSpace.Core.Entities;
using DeepSpace.Infrastructure.Data;
using DeepSpace.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DeepSpace.Web.Pages.Admin;

public class LogoutModel : PageModel
{
    private readonly AppDbContext _context;

    public LogoutModel(AppDbContext context)
    {
        _context = context;
    }

    // Wylogowanie tylko przez POST z tokenem antiforgery, żeby obca strona nie mogła wylogować admina linkiem.
    public IActionResult OnGet() => Redirect("/admin");

    public async Task<IActionResult> OnPostAsync()
    {
        _context.AdminAuditEvents.Add(new AdminAuditEvent
        {
            AdminLogin = User.Identity?.Name ?? string.Empty,
            Action = AdminAuditActions.Logout,
            EntityType = "Session",
            RequestPath = HttpAuditActorProvider.DescribeRequest(HttpContext),
            IpAddress = HttpAuditActorProvider.DescribeIp(HttpContext)
        });
        await _context.SaveChangesAsync();

        await HttpContext.SignOutAsync("AdminCookie");
        return Redirect("/");
    }
}
