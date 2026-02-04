using DeepSpace.Core.Entities;
using DeepSpace.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DeepSpace.Web.Pages.Admin.Articles;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _context;

    public DeleteModel(AppDbContext context)
    {
        _context = context;
    }

    public Article Article { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var article = await _context.Articles
            .Include(a => a.Category)
            .FirstOrDefaultAsync(a => a.Id == id);
        
        if (article == null)
            return NotFound();

        Article = article;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var article = await _context.Articles.FindAsync(id);
        
        if (article != null)
        {
            _context.Articles.Remove(article);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("/Admin/Articles/Index");
    }
}
