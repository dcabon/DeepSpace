using DeepSpace.Core.Entities;
using DeepSpace.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DeepSpace.Web.Pages.Admin.Articles;

public class EditModel : PageModel
{
    private readonly AppDbContext _context;

    public EditModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Article Article { get; set; } = null!;

    public SelectList Categories { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var article = await _context.Articles.FindAsync(id);
        
        if (article == null)
            return NotFound();

        Article = article;
        Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
        
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var articleToUpdate = await _context.Articles.FindAsync(Article.Id);
        
        if (articleToUpdate == null)
            return NotFound();

        articleToUpdate.Title = Article.Title;
        articleToUpdate.Slug = GenerateSlug(Article.Title);
        articleToUpdate.Summary = Article.Summary;
        articleToUpdate.Content = Article.Content;
        articleToUpdate.CategoryId = Article.CategoryId;
        articleToUpdate.SourceName = Article.SourceName;
        articleToUpdate.SourceUrl = Article.SourceUrl;
        articleToUpdate.ImageUrl = Article.ImageUrl;
        articleToUpdate.UpdatedAt = DateTime.UtcNow;

        if (Article.IsPublished && !articleToUpdate.IsPublished)
        {
            articleToUpdate.PublishedAt = DateTime.UtcNow;
        }
        articleToUpdate.IsPublished = Article.IsPublished;

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
