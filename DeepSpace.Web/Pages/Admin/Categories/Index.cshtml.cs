using DeepSpace.Core.Entities;
using DeepSpace.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
namespace DeepSpace.Web.Pages.Admin.Categories;
[Authorize(AuthenticationSchemes = "AdminCookie")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public List<Category> Categories { get; set; } = new();

    public async Task OnGetAsync()
    {
        Categories = await _context.Categories
            .Include(c => c.Articles.Where(a => a.IsActive))
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<IActionResult> OnGetEditAsync(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
            return NotFound();

        return new JsonResult(category);
    }

    public async Task<IActionResult> OnPostCreateAsync(Category category)
    {
        category.Slug = GenerateSlug(category.Name);
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostEditAsync(Category category)
    {
        var existing = await _context.Categories.FindAsync(category.Id);
        if (existing == null)
            return NotFound();

        existing.Name = category.Name;
        existing.Slug = GenerateSlug(category.Name);
        existing.Description = category.Description;

        await _context.SaveChangesAsync();

        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var category = await _context.Categories
            .Include(c => c.Articles)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category != null)
        {
            if (category.Articles.Any(a => a.IsActive))
            {
                return new JsonResult(new { success = false, message = "Nie można usunąć kategorii z aktywnymi artykułami" });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }

        return new JsonResult(new { success = true });
    }

    private string GenerateSlug(string name)
    {
        var slug = name.ToLower()
            .Replace("ą", "a").Replace("ć", "c").Replace("ę", "e")
            .Replace("ł", "l").Replace("ń", "n").Replace("ó", "o")
            .Replace("ś", "s").Replace("ź", "z").Replace("ż", "z");

        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-");

        return slug.Trim('-');
    }
}
