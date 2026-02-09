using DeepSpace.Core.Entities;
using DeepSpace.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

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
    public List<Category> Categories { get; set; } = new();

    public async Task OnGetAsync()
    {
        Articles = await _context.Articles
            .Include(a => a.Category)
            .Include(a => a.ArticleTags)
            .ThenInclude(at => at.Tag)
            .Where(a => a.IsActive)
            .OrderByDescending(a => a.PublishedAt)
            .ToListAsync();

        Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<IActionResult> OnGetArticleAsync(int id)
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
            id = article.Id,
            title = article.Title,
            summary = article.Summary,
            content = article.Content,
            categoryId = article.CategoryId,
            tags = tags,
            sourceName = article.SourceName,
            sourceUrl = article.SourceUrl,
            imageUrl = article.ImageUrl,
            isPublished = article.IsPublished
        });
    }

    public async Task<IActionResult> OnPostCreateAsync(string title, string summary, string content, 
        int categoryId, string? tags, string? sourceName, string? sourceUrl, string? imageUrl, bool isPublished = false)
    {
        var article = new Article
        {
            Title = title,
            Slug = GenerateSlug(title),
            Summary = summary,
            Content = content,
            CategoryId = categoryId,
            SourceName = sourceName,
            SourceUrl = sourceUrl,
            ImageUrl = imageUrl,
            IsPublished = isPublished,
            IsActive = true,
            PublishedAt = isPublished ? DateTime.UtcNow : DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Articles.Add(article);
        await _context.SaveChangesAsync();

        await UpdateArticleTags(article.Id, tags);

        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostEditAsync(int id, string title, string summary, string content,
        int categoryId, string? tags, string? sourceName, string? sourceUrl, string? imageUrl, bool isPublished = false)
    {
        var article = await _context.Articles.FindAsync(id);
        if (article == null)
            return NotFound();

        article.Title = title;
        article.Slug = GenerateSlug(title);
        article.Summary = summary;
        article.Content = content;
        article.CategoryId = categoryId;
        article.SourceName = sourceName;
        article.SourceUrl = sourceUrl;
        article.ImageUrl = imageUrl;
        article.IsPublished = isPublished;
        article.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await UpdateArticleTags(id, tags);

        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var article = await _context.Articles.FindAsync(id);
        if (article == null)
            return NotFound();

        article.IsActive = false;
        await _context.SaveChangesAsync();

        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostToggleFeaturedAsync(int id)
    {
        var article = await _context.Articles.FindAsync(id);
        if (article == null)
            return NotFound();

        // Jeśli włączamy wyróżnienie, wyłącz inne
        if (!article.IsFeaturedInGrid)
        {
            var otherFeatured = await _context.Articles
                .Where(a => a.IsFeaturedInGrid && a.Id != id)
                .ToListAsync();
            foreach (var a in otherFeatured)
            {
                a.IsFeaturedInGrid = false;
            }
        }

        article.IsFeaturedInGrid = !article.IsFeaturedInGrid;
        await _context.SaveChangesAsync();

        return RedirectToPage();
    }

    private async Task UpdateArticleTags(int articleId, string? tagsString)
    {
        // Remove existing tags
        var existingTags = await _context.ArticleTags.Where(at => at.ArticleId == articleId).ToListAsync();
        _context.ArticleTags.RemoveRange(existingTags);

        if (string.IsNullOrWhiteSpace(tagsString))
        {
            await _context.SaveChangesAsync();
            return;
        }

        var tagNames = tagsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .Distinct();

        foreach (var tagName in tagNames)
        {
            var slug = GenerateSlug(tagName);
            var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Slug == slug);

            if (tag == null)
            {
                tag = new Tag { Name = tagName, Slug = slug };
                _context.Tags.Add(tag);
                await _context.SaveChangesAsync();
            }

            _context.ArticleTags.Add(new ArticleTag { ArticleId = articleId, TagId = tag.Id });
        }

        await _context.SaveChangesAsync();
    }

    private string GenerateSlug(string text)
    {
        var slug = text.ToLower()
            .Replace("ą", "a").Replace("ć", "c").Replace("ę", "e")
            .Replace("ł", "l").Replace("ń", "n").Replace("ó", "o")
            .Replace("ś", "s").Replace("ź", "z").Replace("ż", "z");

        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = slug.Trim('-');

        return slug;
    }
}
