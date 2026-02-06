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
    public List<ArticleSection> Sections { get; set; } = new();
    public List<RelatedArticleDto> RelatedArticles { get; set; } = new();

    public class RelatedArticleDto
    {
        public Article Article { get; set; } = null!;
        public int SharedTagsCount { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        Article = await _context.Articles
            .Include(a => a.Category)
            .Include(a => a.ArticleTags)
            .ThenInclude(at => at.Tag)
            .Include(a => a.Sections.OrderBy(s => s.OrderIndex))
            .FirstOrDefaultAsync(a => a.Slug == slug && a.IsPublished && a.IsActive);

        if (Article == null)
            return NotFound();

        Tags = Article.ArticleTags.Select(at => at.Tag).ToList();
        Sections = Article.Sections.OrderBy(s => s.OrderIndex).ToList();

        // Pobierz powiązane artykuły
        var currentTagIds = Tags.Select(t => t.Id).ToList();

        if (currentTagIds.Any())
        {
            var relatedWithTags = await _context.Articles
                .Include(a => a.Category)
                .Include(a => a.ArticleTags)
                .ThenInclude(at => at.Tag)
                .Where(a => a.Id != Article.Id && a.IsPublished && a.IsActive)
                .Where(a => a.ArticleTags.Any(at => currentTagIds.Contains(at.TagId)))
                .ToListAsync();

            RelatedArticles = relatedWithTags
                .Select(a => new RelatedArticleDto
                {
                    Article = a,
                    SharedTagsCount = a.ArticleTags.Count(at => currentTagIds.Contains(at.TagId))
                })
                .OrderByDescending(r => r.SharedTagsCount)
                .ThenByDescending(r => r.Article.PublishedAt)
                .Take(3)
                .ToList();
        }

        if (RelatedArticles.Count < 3)
        {
            var existingIds = RelatedArticles.Select(r => r.Article.Id).ToList();
            existingIds.Add(Article.Id);

            var additionalArticles = await _context.Articles
                .Include(a => a.Category)
                .Where(a => !existingIds.Contains(a.Id) && a.IsPublished && a.IsActive)
                .OrderByDescending(a => a.PublishedAt)
                .Take(3 - RelatedArticles.Count)
                .ToListAsync();

            RelatedArticles.AddRange(additionalArticles.Select(a => new RelatedArticleDto
            {
                Article = a,
                SharedTagsCount = 0
            }));
        }

        return Page();
    }
}
