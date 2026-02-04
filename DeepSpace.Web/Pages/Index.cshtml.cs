using DeepSpace.Core.Entities;
using DeepSpace.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DeepSpace.Web.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;
    private const int PageSize = 10;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public List<Article> Articles { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }

    public async Task OnGetAsync(int page = 1)
    {
        CurrentPage = page < 1 ? 1 : page;

        var query = _context.Articles
            .Include(a => a.Category)
            .Where(a => a.IsPublished && a.IsActive)
            .OrderByDescending(a => a.PublishedAt);

        var totalArticles = await query.CountAsync();
        TotalPages = (int)Math.Ceiling(totalArticles / (double)PageSize);

        Articles = await query
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        Categories = await _context.Categories
            .Include(c => c.Articles.Where(a => a.IsActive))
            .ToListAsync();
    }
}
