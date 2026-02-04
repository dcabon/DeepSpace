using DeepSpace.Core.Entities;
using DeepSpace.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace DeepSpace.Web.Pages.Admin.Tags;
[Authorize(AuthenticationSchemes = "AdminCookie")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public List<Tag> Tags { get; set; } = new();

    public async Task OnGetAsync()
    {
        Tags = await _context.Tags
            .Include(t => t.ArticleTags)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<IActionResult> OnGetEditAsync(int id)
    {
        var tag = await _context.Tags.FindAsync(id);
        if (tag == null)
            return NotFound();

        return new JsonResult(tag);
    }

    public async Task<IActionResult> OnPostCreateAsync(Tag tag)
    {
        tag.Slug = GenerateSlug(tag.Name);
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();

        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostEditAsync(Tag tag)
    {
        var existing = await _context.Tags.FindAsync(tag.Id);
        if (existing == null)
            return NotFound();

        existing.Name = tag.Name;
        existing.Slug = GenerateSlug(tag.Name);

        await _context.SaveChangesAsync();

        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var tag = await _context.Tags
            .Include(t => t.ArticleTags)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tag != null)
        {
            _context.ArticleTags.RemoveRange(tag.ArticleTags);
            _context.Tags.Remove(tag);
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
