using DeepSpace.Core.Entities;
using DeepSpace.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DeepSpace.Web.Pages;

public class QuizyModel : PageModel
{
    private readonly AppDbContext _context;

    public QuizyModel(AppDbContext context)
    {
        _context = context;
    }

    public List<Quiz> Quizzes { get; set; } = new();

    public async Task OnGetAsync()
    {
        Quizzes = await _context.Quizzes
            .Include(q => q.Questions)
            .Where(q => q.IsPublished && q.IsActive)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
    }
}
