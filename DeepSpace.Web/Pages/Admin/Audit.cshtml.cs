using DeepSpace.Core.Entities;
using DeepSpace.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DeepSpace.Web.Pages.Admin;

public class AuditModel : PageModel
{
    private const int PageSize = 50;
    private const int MaxDisplayedValueLength = 160;

    private readonly AppDbContext _context;

    public AuditModel(AppDbContext context)
    {
        _context = context;
    }

    public List<AdminAuditEvent> Events { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public bool HasNextPage { get; set; }

    public async Task OnGetAsync(int p = 1)
    {
        CurrentPage = Math.Max(1, p);

        var events = await _context.AdminAuditEvents
            .AsNoTracking()
            .Include(e => e.Changes)
            .OrderByDescending(e => e.Id)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize + 1)
            .ToListAsync();

        HasNextPage = events.Count > PageSize;
        Events = events.Take(PageSize).ToList();
    }

    public static string DisplayValue(string? value)
    {
        if (value == null)
        {
            return "—";
        }

        return value.Length > MaxDisplayedValueLength ? value[..MaxDisplayedValueLength] + "…" : value;
    }
}
