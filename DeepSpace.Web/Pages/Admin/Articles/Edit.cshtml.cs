using System.Text.RegularExpressions;
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
    public List<ArticleSection> Sections { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var article = await _context.Articles
            .Include(a => a.Sections.OrderBy(s => s.OrderIndex))
            .FirstOrDefaultAsync(a => a.Id == id);
        
        if (article == null)
            return NotFound();

        Article = article;
        Sections = article.Sections.OrderBy(s => s.OrderIndex).ToList();
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

        return RedirectToPage(new { id = Article.Id });
    }

    public async Task<IActionResult> OnPostSaveSectionAsync(int articleId, int sectionId, string sectionTitle, string sectionContent)
    {
        var article = await _context.Articles
            .Include(a => a.Sections)
            .FirstOrDefaultAsync(a => a.Id == articleId);
        
        if (article == null) return NotFound();

        ArticleSection section;
        var slug = GenerateSlug(sectionTitle);

        if (sectionId == 0)
        {
            // Nowa sekcja
            section = new ArticleSection
            {
                ArticleId = articleId,
                Title = sectionTitle,
                Slug = slug,
                Content = sectionContent ?? "",
                OrderIndex = article.Sections.Count
            };
            _context.ArticleSections.Add(section);
        }
        else
        {
            // Edycja istniejącej
            section = await _context.ArticleSections.FindAsync(sectionId);
            if (section == null) return NotFound();

            section.Title = sectionTitle;
            section.Slug = slug;
            section.Content = sectionContent ?? "";
        }

        await _context.SaveChangesAsync();
        return RedirectToPage(new { id = articleId });
    }

    public async Task<IActionResult> OnPostDeleteSectionAsync(int sectionId)
    {
        var section = await _context.ArticleSections.FindAsync(sectionId);
        if (section == null) return NotFound();

        var articleId = section.ArticleId;
        _context.ArticleSections.Remove(section);
        await _context.SaveChangesAsync();

        // Przenumeruj sekcje
        var remainingSections = await _context.ArticleSections
            .Where(s => s.ArticleId == articleId)
            .OrderBy(s => s.OrderIndex)
            .ToListAsync();

        for (int i = 0; i < remainingSections.Count; i++)
        {
            remainingSections[i].OrderIndex = i;
        }
        await _context.SaveChangesAsync();

        return RedirectToPage(new { id = articleId });
    }

    private string GenerateSlug(string title)
    {
        var slug = title.ToLower()
            .Replace("ą", "a").Replace("ć", "c").Replace("ę", "e")
            .Replace("ł", "l").Replace("ń", "n").Replace("ó", "o")
            .Replace("ś", "s").Replace("ź", "z").Replace("ż", "z");
        
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        
        return slug.Trim('-');
    }
}
