using System.Text.RegularExpressions;
using DeepSpace.Core.Entities;
using DeepSpace.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DeepSpace.Web.Pages.Admin.Quizzes;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public List<Quiz> Quizzes { get; set; } = new();

    public async Task OnGetAsync()
    {
        Quizzes = await _context.Quizzes
            .Include(q => q.Questions)
            .Where(q => q.IsActive)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync(string title, string? description, string? imageUrl, bool isPublished)
    {
        var slug = GenerateSlug(title);
        
        // Upewnij się, że slug jest unikalny
        var baseSlug = slug;
        var counter = 1;
        while (await _context.Quizzes.AnyAsync(q => q.Slug == slug))
        {
            slug = $"{baseSlug}-{counter++}";
        }

        var quiz = new Quiz
        {
            Title = title,
            Slug = slug,
            Description = description,
            ImageUrl = imageUrl,
            IsPublished = isPublished
        };

        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync();

        return RedirectToPage("/Admin/Quizzes/Edit", new { id = quiz.Id });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var quiz = await _context.Quizzes.FindAsync(id);
        if (quiz != null)
        {
            quiz.IsActive = false;
            await _context.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleFeaturedAsync(int id)
    {
        var quiz = await _context.Quizzes.FindAsync(id);
        if (quiz != null)
        {
            // Jeśli włączamy wyróżnienie, wyłącz inne
            if (!quiz.IsFeatured)
            {
                var otherFeatured = await _context.Quizzes
                    .Where(q => q.IsFeatured && q.Id != id)
                    .ToListAsync();
                foreach (var q in otherFeatured)
                {
                    q.IsFeatured = false;
                }
            }
            
            quiz.IsFeatured = !quiz.IsFeatured;
            await _context.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    private string GenerateSlug(string title)
    {
        var slug = title.ToLowerInvariant();
        slug = Regex.Replace(slug, @"[ąà]", "a");
        slug = Regex.Replace(slug, @"[ćç]", "c");
        slug = Regex.Replace(slug, @"[ęè]", "e");
        slug = Regex.Replace(slug, @"[ł]", "l");
        slug = Regex.Replace(slug, @"[ńñ]", "n");
        slug = Regex.Replace(slug, @"[óò]", "o");
        slug = Regex.Replace(slug, @"[ś]", "s");
        slug = Regex.Replace(slug, @"[żź]", "z");
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');
        return slug;
    }
}
