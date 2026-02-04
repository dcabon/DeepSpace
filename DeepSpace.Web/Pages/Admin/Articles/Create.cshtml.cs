using DeepSpace.Core.Entities;
using DeepSpace.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DeepSpace.Web.Pages.Admin.Articles;

public class CreateModel : PageModel
{
    private readonly AppDbContext _context;

    public CreateModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Article Article { get; set; } = new();

    public SelectList Categories { get; set; } = null!;

    public async Task OnGetAsync()
    {
        Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Article.Slug = GenerateSlug(Article.Title);
        Article.CreatedAt = DateTime.UtcNow;
        
        if (Article.IsPublished && Article.PublishedAt == default)
        {
            Article.PublishedAt = DateTime.UtcNow;
        }

        _context.Articles.Add(Article);
        await _context.SaveChangesAsync();

        return RedirectToPage("/Admin/Articles/Index");
    }

    private string GenerateSlug(string title)
    {
        var slug = title.ToLower()
            .Replace("ą", "a").Replace("ć", "c").Replace("ę", "e")
            .Replace("ł", "l").Replace("ń", "n").Replace("ó", "o")
            .Replace("ś", "s").Replace("ź", "z").Replace("ż", "z");
        
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-");
        
        return slug.Trim('-');
    }
}
