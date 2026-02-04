using DeepSpace.Core.Entities;
using DeepSpace.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DeepSpace.Web.Pages;

public class SzukajModel : PageModel
{
    private readonly AppDbContext _context;

    public SzukajModel(AppDbContext context)
    {
        _context = context;
    }

    public string Query { get; set; } = string.Empty;
    public List<Article> Articles { get; set; } = new();

    public async Task OnGetAsync(string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return;

        Query = q.Trim();

        Articles = await _context.Articles
            .Include(a => a.Category)
            .Where(a => a.IsPublished && a.IsActive &&
                        (a.Title.ToLower().Contains(Query.ToLower()) ||
                         a.Summary.ToLower().Contains(Query.ToLower()) ||
                         a.Content.ToLower().Contains(Query.ToLower())))
            .OrderByDescending(a => a.PublishedAt)
            .Take(20)
            .ToListAsync();
    }
}
