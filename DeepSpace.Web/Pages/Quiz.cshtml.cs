using DeepSpace.Core.Entities;
using DeepSpace.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DeepSpace.Web.Pages;

public class QuizModel : PageModel
{
    private readonly AppDbContext _context;

    public QuizModel(AppDbContext context)
    {
        _context = context;
    }

    public Quiz? Quiz { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        Quiz = await _context.Quizzes
            .Include(q => q.Questions.OrderBy(qu => qu.OrderIndex))
            .ThenInclude(q => q.Answers.OrderBy(a => a.OrderIndex))
            .FirstOrDefaultAsync(q => q.Slug == slug && q.IsPublished && q.IsActive);

        if (Quiz == null)
            return NotFound();

        return Page();
    }
}
