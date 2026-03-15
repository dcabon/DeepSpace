using System.Text.RegularExpressions;
using DeepSpace.Core.Entities;
using DeepSpace.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DeepSpace.Web.Pages.Admin.Quizzes;

public class EditModel : PageModel
{
    private readonly AppDbContext _context;

    public EditModel(AppDbContext context)
    {
        _context = context;
    }

    public Quiz? Quiz { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Quiz = await _context.Quizzes
            .Include(q => q.Questions.OrderBy(qu => qu.OrderIndex))
            .ThenInclude(q => q.Answers.OrderBy(a => a.OrderIndex))
            .FirstOrDefaultAsync(q => q.Id == id && q.IsActive);

        if (Quiz == null)
            return NotFound();

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateQuizAsync(int id, string title, string slug, string? description, string? imageUrl, bool isPublished)
    {
        var quiz = await _context.Quizzes.FindAsync(id);
        if (quiz == null) return NotFound();

        quiz.Title = title;
        quiz.Slug = slug;
        quiz.Description = description;
        quiz.ImageUrl = imageUrl;
        quiz.IsPublished = isPublished;

        await _context.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostSaveQuestionAsync(int quizId, int questionId, string questionText, string? questionImageUrl, string[] answers, int correctAnswer)
    {
        var quiz = await _context.Quizzes.Include(q => q.Questions).FirstOrDefaultAsync(q => q.Id == quizId);
        if (quiz == null) return NotFound();

        QuizQuestion question;

        if (questionId == 0)
        {
            // Nowe pytanie
            question = new QuizQuestion
            {
                QuizId = quizId,
                QuestionText = questionText,
                ImageUrl = questionImageUrl,
                OrderIndex = quiz.Questions.Count
            };
            _context.QuizQuestions.Add(question);
        }
        else
        {
            // Edycja istniejącego
            question = await _context.QuizQuestions
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.Id == questionId);
            
            if (question == null) return NotFound();

            question.QuestionText = questionText;
            question.ImageUrl = questionImageUrl;

            // Usuń stare odpowiedzi
            _context.QuizAnswers.RemoveRange(question.Answers);
        }

        await _context.SaveChangesAsync();

        // Dodaj odpowiedzi
        for (int i = 0; i < answers.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(answers[i]))
            {
                var answer = new QuizAnswer
                {
                    QuestionId = question.Id,
                    AnswerText = answers[i],
                    IsCorrect = i == correctAnswer,
                    OrderIndex = i
                };
                _context.QuizAnswers.Add(answer);
            }
        }

        await _context.SaveChangesAsync();
        return RedirectToPage(new { id = quizId });
    }

    public async Task<IActionResult> OnPostDeleteQuestionAsync(int questionId)
    {
        var question = await _context.QuizQuestions
            .Include(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == questionId);

        if (question != null)
        {
            var quizId = question.QuizId;
            _context.QuizAnswers.RemoveRange(question.Answers);
            _context.QuizQuestions.Remove(question);
            await _context.SaveChangesAsync();

            // Przenumeruj pytania
            var remainingQuestions = await _context.QuizQuestions
                .Where(q => q.QuizId == quizId)
                .OrderBy(q => q.OrderIndex)
                .ToListAsync();

            for (int i = 0; i < remainingQuestions.Count; i++)
            {
                remainingQuestions[i].OrderIndex = i;
            }
            await _context.SaveChangesAsync();

            return RedirectToPage(new { id = quizId });
        }

        return RedirectToPage("/Admin/Quizzes/Index");
    }
}
