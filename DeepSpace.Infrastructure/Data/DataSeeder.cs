using DeepSpace.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeepSpace.Infrastructure.Data;

public static class DataSeeder
{
    public static void Seed(AppDbContext context)
    {
        EnsureCategories(context);
        EnsureTags(context);
        EnsureSiteSettings(context);
        EnsureSampleArticle(context);
    }

    private static void EnsureCategories(AppDbContext context)
    {
        var categories = new List<Category>
        {
            new() { Name = "Astronomia", Slug = "astronomia", Description = "Obserwacje nieba, gwiazdy, galaktyki" },
            new() { Name = "Fizyka", Slug = "fizyka", Description = "Prawa rządzące wszechświatem" },
            new() { Name = "Misje kosmiczne", Slug = "misje-kosmiczne", Description = "Sondy, łaziki, załogowe loty" },
            new() { Name = "Układ Słoneczny", Slug = "uklad-sloneczny", Description = "Planety, księżyce, asteroidy" }
        };

        var existingSlugs = context.Categories
            .Select(c => c.Slug)
            .ToHashSet();

        var missingCategories = categories
            .Where(c => !existingSlugs.Contains(c.Slug))
            .ToList();

        if (missingCategories.Count > 0)
        {
            context.Categories.AddRange(missingCategories);
            context.SaveChanges();
        }
    }

    private static void EnsureTags(AppDbContext context)
    {
        var tags = new List<Tag>
        {
            new() { Name = "NASA", Slug = "nasa" },
            new() { Name = "ESA", Slug = "esa" },
            new() { Name = "James Webb", Slug = "james-webb" },
            new() { Name = "Mars", Slug = "mars" },
            new() { Name = "Czarne dziury", Slug = "czarne-dziury" },
            new() { Name = "Space", Slug = "space" },
            new() { Name = "Physics", Slug = "physics" },
            new() { Name = "Astronomy", Slug = "astronomy" }
        };

        var existingSlugs = context.Tags
            .Select(t => t.Slug)
            .ToHashSet();

        var missingTags = tags
            .Where(t => !existingSlugs.Contains(t.Slug))
            .ToList();

        if (missingTags.Count > 0)
        {
            context.Tags.AddRange(missingTags);
            context.SaveChanges();
        }
    }

    private static void EnsureSiteSettings(AppDbContext context)
    {
        if (context.SiteSettings.Any())
        {
            return;
        }

        context.SiteSettings.Add(new SiteSettings());
        context.SaveChanges();
    }

    private static void EnsureSampleArticle(AppDbContext context)
    {
        if (context.Articles.Any())
        {
            return;
        }

        var astronomyCategory = context.Categories
            .AsNoTracking()
            .FirstOrDefault(c => c.Slug == "astronomia");

        if (astronomyCategory == null)
        {
            return;
        }

        var article = new Article
        {
            Title = "James Webb odkrywa najstarszą galaktykę",
            Slug = "james-webb-odkrywa-najstarsza-galaktyke",
            Summary = "Teleskop Jamesa Webba zidentyfikował galaktykę powstałą zaledwie 300 milionów lat po Wielkim Wybuchu.",
            Content = @"<p>Kosmiczny Teleskop Jamesa Webba (JWST) po raz kolejny przesunął granice naszej wiedzy o wczesnym wszechświecie. Najnowsze obserwacje ujawniły galaktykę JADES-GS-z14-0, która powstała około 300 milionów lat po Wielkim Wybuchu.</p>

<p>To odkrycie jest przełomowe z kilku powodów. Po pierwsze, galaktyka jest znacznie jaśniejsza niż przewidywały modele teoretyczne dla tak wczesnego okresu. Po drugie, widoczne są w niej ślady tlenu, co sugeruje, że procesy formowania gwiazd rozpoczęły się jeszcze wcześniej.</p>

<p>Dane zostały potwierdzone przez niezależne zespoły badawcze i opublikowane w Nature Astronomy.</p>",
            SourceUrl = "https://www.nasa.gov/james-webb-space-telescope",
            SourceName = "NASA",
            ImageUrl = "/images/placeholder.jpg",
            CategoryId = astronomyCategory.Id,
            PublishedAt = DateTime.UtcNow,
            IsPublished = true,
            IsActive = true
        };

        context.Articles.Add(article);
        context.SaveChanges();

        var articleTagSlugs = new[] { "nasa", "james-webb" };
        var tags = context.Tags
            .Where(t => articleTagSlugs.Contains(t.Slug))
            .ToList();

        if (tags.Count == 0)
        {
            return;
        }

        context.ArticleTags.AddRange(tags.Select(tag => new ArticleTag
        {
            ArticleId = article.Id,
            TagId = tag.Id
        }));
        context.SaveChanges();
    }
}
