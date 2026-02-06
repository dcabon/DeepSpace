using DeepSpace.Core.Entities;
using DeepSpace.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DeepSpace.Web.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public Article? FeaturedArticle { get; set; }
    public List<Article> LatestArticles { get; set; } = new();
    public List<CategoryWithArticles> CategoriesWithArticles { get; set; } = new();
    public Quiz? FeaturedQuiz { get; set; }

    public class CategoryWithArticles
    {
        public Category Category { get; set; } = null!;
        public List<Article> Articles { get; set; } = new();
    }

    public async Task OnGetAsync()
    {
        // Pobierz wyróżniony quiz
        FeaturedQuiz = await _context.Quizzes
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.IsFeatured && q.IsPublished && q.IsActive);

        // Pobierz wyróżniony artykuł (najnowszy)
        FeaturedArticle = await _context.Articles
            .Include(a => a.Category)
            .Where(a => a.IsPublished && a.IsActive)
            .OrderByDescending(a => a.PublishedAt)
            .FirstOrDefaultAsync();

        // Pobierz 6 najnowszych artykułów (bez wyróżnionego)
        var featuredId = FeaturedArticle?.Id ?? 0;
        LatestArticles = await _context.Articles
            .Include(a => a.Category)
            .Where(a => a.IsPublished && a.IsActive && a.Id != featuredId)
            .OrderByDescending(a => a.PublishedAt)
            .Take(6)
            .ToListAsync();

        // Pobierz ID artykułów już wyświetlonych
        var displayedIds = LatestArticles.Select(a => a.Id).ToList();
        if (FeaturedArticle != null) displayedIds.Add(FeaturedArticle.Id);

        // Pobierz wszystkie kategorie
        var categories = await _context.Categories.ToListAsync();

        // Dla każdej kategorii pobierz artykuły osobno
        foreach (var category in categories)
        {
            var categoryArticles = await _context.Articles
                .Include(a => a.Category)
                .Where(a => a.IsPublished && a.IsActive && a.CategoryId == category.Id && !displayedIds.Contains(a.Id))
                .OrderByDescending(a => a.PublishedAt)
                .Take(6)
                .ToListAsync();

            if (categoryArticles.Any())
            {
                CategoriesWithArticles.Add(new CategoryWithArticles
                {
                    Category = category,
                    Articles = categoryArticles
                });
            }
        }
    }
}
