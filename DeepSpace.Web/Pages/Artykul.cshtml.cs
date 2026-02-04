using DeepSpace.Core.Entities;
using DeepSpace.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DeepSpace.Web.Pages;

public class ArtykulModel : PageModel
{
    private readonly AppDbContext _context;

    public ArtykulModel(AppDbContext context)
    {
        _context = context;
    }

    public Article? Article { get; set; }
    public List<Tag> Tags { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        Article = await _context.Articles
            .Include(a => a.Category)
            .Include(a => a.ArticleTags)
            .ThenInclude(at => at.Tag)
            .FirstOrDefaultAsync(a => a.Slug == slug && a.IsPublished);

        if (Article == null)
            return NotFound();

        Tags = Article.ArticleTags.Select(at => at.Tag).ToList();

        return Page();
    }
}
