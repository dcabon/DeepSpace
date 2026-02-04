using DeepSpace.Core.Entities;
using DeepSpace.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DeepSpace.Web.Pages;

public class TagModel : PageModel
{
    private readonly AppDbContext _context;

    public TagModel(AppDbContext context)
    {
        _context = context;
    }

    public Tag Tag { get; set; } = null!;
    public List<Article> Articles { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var tag = await _context.Tags
            .FirstOrDefaultAsync(t => t.Slug == slug);

        if (tag == null)
            return NotFound();

        Tag = tag;

        Articles = await _context.ArticleTags
            .Include(at => at.Article)
            .ThenInclude(a => a.Category)
            .Where(at => at.TagId == tag.Id && at.Article.IsPublished && at.Article.IsActive)
            .Select(at => at.Article)
            .OrderByDescending(a => a.PublishedAt)
            .ToListAsync();

        return Page();
    }
}
