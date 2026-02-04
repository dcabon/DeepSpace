using DeepSpace.Core.Entities;
using DeepSpace.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DeepSpace.Web.Pages;

public class KategoriaModel : PageModel
{
    private readonly AppDbContext _context;

    public KategoriaModel(AppDbContext context)
    {
        _context = context;
    }

    public Category Category { get; set; } = null!;
    public List<Article> Articles { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Slug == slug);

        if (category == null)
            return NotFound();

        Category = category;

        Articles = await _context.Articles
            .Include(a => a.Category)
            .Where(a => a.CategoryId == category.Id && a.IsPublished && a.IsActive)
            .OrderByDescending(a => a.PublishedAt)
            .ToListAsync();

        return Page();
    }
}
