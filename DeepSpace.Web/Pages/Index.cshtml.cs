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
    public Article? FeaturedInGridArticle { get; set; }
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

        // Pobierz wyróżniony artykuł w gridzie
        FeaturedInGridArticle = await _context.Articles
            .Include(a => a.Category)
            .Where(a => a.IsPublished && a.IsActive && a.IsFeaturedInGrid)
            .FirstOrDefaultAsync();

        // Pobierz 6 najnowszych artykułów (bez wyróżnionego hero i wyróżnionego w gridzie)
        var excludeIds = new List<int>();
        if (FeaturedArticle != null) excludeIds.Add(FeaturedArticle.Id);
        if (FeaturedInGridArticle != null) excludeIds.Add(FeaturedInGridArticle.Id);

        // Jeśli jest wyróżniony w gridzie, pobierz 4 (1 obok + 3 poniżej), inaczej 6
        var takeCount = FeaturedInGridArticle != null ? 4 : 6;
        
        LatestArticles = await _context.Articles
            .Include(a => a.Category)
            .Where(a => a.IsPublished && a.IsActive && !excludeIds.Contains(a.Id))
            .OrderByDescending(a => a.PublishedAt)
            .Take(takeCount)
            .ToListAsync();

        // Pobierz ID artykułów już wyświetlonych
        var displayedIds = LatestArticles.Select(a => a.Id).ToList();
        if (FeaturedArticle != null) displayedIds.Add(FeaturedArticle.Id);
        if (FeaturedInGridArticle != null) displayedIds.Add(FeaturedInGridArticle.Id);

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
