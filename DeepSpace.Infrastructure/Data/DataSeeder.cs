using DeepSpace.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeepSpace.Infrastructure.Data;

public static class DataSeeder
{
    public static void Seed(AppDbContext context)
    {
        if (context.Categories.Any()) return;

        var categories = new List<Category>
        {
            new() { Name = "Astronomia", Slug = "astronomia", Description = "Obserwacje nieba, gwiazdy, galaktyki", },
            new() { Name = "Fizyka", Slug = "fizyka", Description = "Prawa rządzące wszechświatem" },
            new() { Name = "Misje kosmiczne", Slug = "misje-kosmiczne", Description = "Sondy, łaziki, załogowe loty" },
            new() { Name = "Układ Słoneczny", Slug = "uklad-sloneczny", Description = "Planety, księżyce, asteroidy" }
        };
        context.Categories.AddRange(categories);
        context.SaveChanges();

        var tags = new List<Tag>
        {
            new() { Name = "NASA", Slug = "nasa" },
            new() { Name = "ESA", Slug = "esa" },
            new() { Name = "James Webb", Slug = "james-webb" },
            new() { Name = "Mars", Slug = "mars" },
            new() { Name = "Czarne dziury", Slug = "czarne-dziury" }
        };
        context.Tags.AddRange(tags);
        context.SaveChanges();

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
            CategoryId = 1,
            PublishedAt = DateTime.UtcNow,
            IsPublished = true,
            IsActive = true
        };
        context.Articles.Add(article);
        context.SaveChanges();

        var articleTags = new List<ArticleTag>
        {
            new() { ArticleId = article.Id, TagId = tags[0].Id },
            new() { ArticleId = article.Id, TagId = tags[2].Id }
        };
        context.ArticleTags.AddRange(articleTags);
        context.SaveChanges();
    }
}
