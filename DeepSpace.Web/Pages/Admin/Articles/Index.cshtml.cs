using DeepSpace.Core.Entities;
using DeepSpace.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DeepSpace.Web.Pages.Admin.Articles;

[Authorize(AuthenticationSchemes = "AdminCookie")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public List<Article> Articles { get; set; } = new();
    public SelectList Categories { get; set; } = null!;

    public async Task OnGetAsync()
    {
        Articles = await _context.Articles
            .Include(a => a.Category)
            .Include(a => a.ArticleTags)
            .ThenInclude(at => at.Tag)
            .Where(a => a.IsActive)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
    }

    public async Task<IActionResult> OnGetEditAsync(int id)
    {
        var article = await _context.Articles
            .Include(a => a.ArticleTags)
            .ThenInclude(at => at.Tag)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (article == null)
            return NotFound();

        var tags = string.Join(", ", article.ArticleTags.Select(at => at.Tag.Name));

        return new JsonResult(new
        {
            article.Id,
            article.Title,
            article.Summary,
            article.Content,
            article.CategoryId,
            article.SourceName,
            article.SourceUrl,
            article.ImageUrl,
            article.IsPublished,
            Tags = tags
        });
    }

    public async Task<IActionResult> OnPostCreateAsync(Article article, string? tags)
    {
        article.Slug = GenerateSlug(article.Title);
        article.CreatedAt = DateTime.UtcNow;
        article.IsActive = true;

        if (article.IsPublished)
            article.PublishedAt = DateTime.UtcNow;

        _context.Articles.Add(article);
        await _context.SaveChangesAsync();

        await UpdateArticleTags(article.Id, tags);

        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostEditAsync(Article article, string? tags)
    {
        var existing = await _context.Articles.FindAsync(article.Id);
        if (existing == null)
            return NotFound();

        existing.Title = article.Title;
        existing.Slug = GenerateSlug(article.Title);
        existing.Summary = article.Summary;
        existing.Content = article.Content;
        existing.CategoryId = article.CategoryId;
        existing.SourceName = article.SourceName;
        existing.SourceUrl = article.SourceUrl;
        existing.ImageUrl = article.ImageUrl;
        existing.UpdatedAt = DateTime.UtcNow;

        if (article.IsPublished && !existing.IsPublished)
            existing.PublishedAt = DateTime.UtcNow;

        existing.IsPublished = article.IsPublished;

        await _context.SaveChangesAsync();

        await UpdateArticleTags(existing.Id, tags);

        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var article = await _context.Articles.FindAsync(id);
        if (article != null)
        {
            article.IsActive = false;
            await _context.SaveChangesAsync();
        }

        return new JsonResult(new { success = true });
    }

    private async Task UpdateArticleTags(int articleId, string? tagsInput)
    {
        // Usuń istniejące powiązania
        var existingTags = await _context.ArticleTags
            .Where(at => at.ArticleId == articleId)
            .ToListAsync();
        _context.ArticleTags.RemoveRange(existingTags);

        if (string.IsNullOrWhiteSpace(tagsInput))
        {
            await _context.SaveChangesAsync();
            return;
        }

        // Parsuj tagi
        var tagNames = tagsInput
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .Distinct()
            .ToList();

        foreach (var tagName in tagNames)
        {
            var slug = GenerateSlug(tagName);

            // Znajdź lub utwórz tag
            var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Slug == slug);
            if (tag == null)
            {
                tag = new Tag { Name = tagName, Slug = slug };
                _context.Tags.Add(tag);
                await _context.SaveChangesAsync();
            }

            // Dodaj powiązanie
            _context.ArticleTags.Add(new ArticleTag
            {
                ArticleId = articleId,
                TagId = tag.Id
            });
        }

        await _context.SaveChangesAsync();
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
