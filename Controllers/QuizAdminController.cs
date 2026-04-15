using DiveIntoIVE.Data;
using DiveIntoIVE.DTOs.Quiz;
using DiveIntoIVE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiveIntoIVE.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Super-Admin")]
[Route("api/admin/quizzes")]
public class QuizAdminController : ControllerBase
{
    private readonly AppDbContext _context;

    public QuizAdminController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var quizzes = await _context.Quizzes
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Slug,
                x.Type,
                x.IsGraded,
                x.IsPublished,
                x.IsActive,
                x.StartAt,
                x.EndAt,
                QuestionCount = x.Questions.Count
            })
            .ToListAsync();

        return Ok(quizzes);
    }

    [HttpGet("{quizId:int}")]
    public async Task<IActionResult> GetById(int quizId)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions.OrderBy(question => question.SortOrder))
            .ThenInclude(question => question.Options.OrderBy(option => option.SortOrder))
            .FirstOrDefaultAsync(q => q.Id == quizId);

        if (quiz is null)
            return NotFound("Quiz not found.");

        var view = new
        {
            quiz.Id,
            quiz.Title,
            quiz.Slug,
            quiz.Description,
            quiz.Type,
            quiz.IsGraded,
            quiz.IsPublished,
            quiz.IsActive,
            quiz.StartAt,
            quiz.EndAt,
            Questions = quiz.Questions
                .OrderBy(question => question.SortOrder)
                .Select(question => new
                {
                    question.Id,
                    question.Prompt,
                    question.ImageUrl,
                    question.AudioUrl,
                    question.Explanation,
                    question.SortOrder,
                    question.Points,
                    Options = question.Options
                        .OrderBy(option => option.SortOrder)
                        .Select(option => new
                        {
                            option.Id,
                            option.Text,
                            option.ImageUrl,
                            option.SortOrder,
                            option.IsCorrect
                        })
                        .ToList()
                })
                .ToList()
        };

        return Ok(view);
    }

    [HttpPost]
    public async Task<IActionResult> CreateQuiz([FromBody] QuizUpsertDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Slug))
            return BadRequest("Title and slug are required.");

        var slug = dto.Slug.Trim().ToLowerInvariant();
        if (await _context.Quizzes.AnyAsync(x => x.Slug == slug))
            return BadRequest("Slug already exists.");

        if (dto.StartAt.HasValue && dto.EndAt.HasValue && dto.StartAt >= dto.EndAt)
            return BadRequest("StartAt must be before EndAt.");

        var quiz = new Quiz
        {
            Title = dto.Title.Trim(),
            Slug = slug,
            Description = dto.Description?.Trim(),
            Type = dto.Type.Trim().ToLowerInvariant(),
            IsGraded = dto.IsGraded,
            IsActive = dto.IsActive,
            IsPublished = false,
            StartAt = dto.StartAt,
            EndAt = dto.EndAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync();

        return Ok(quiz);
    }

    [HttpPut("{quizId:int}")]
    public async Task<IActionResult> UpdateQuiz(int quizId, [FromBody] QuizUpsertDto dto)
    {
        var quiz = await _context.Quizzes.FindAsync(quizId);
        if (quiz is null)
            return NotFound("Quiz not found.");

        var slug = dto.Slug.Trim().ToLowerInvariant();
        if (await _context.Quizzes.AnyAsync(x => x.Slug == slug && x.Id != quizId))
            return BadRequest("Slug already exists.");

        if (dto.StartAt.HasValue && dto.EndAt.HasValue && dto.StartAt >= dto.EndAt)
            return BadRequest("StartAt must be before EndAt.");

        quiz.Title = dto.Title.Trim();
        quiz.Slug = slug;
        quiz.Description = dto.Description?.Trim();
        quiz.Type = dto.Type.Trim().ToLowerInvariant();
        quiz.IsGraded = dto.IsGraded;
        quiz.IsActive = dto.IsActive;
        quiz.StartAt = dto.StartAt;
        quiz.EndAt = dto.EndAt;
        quiz.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(quiz);
    }

    [HttpDelete("{quizId:int}")]
    public async Task<IActionResult> DeleteQuiz(int quizId)
    {
        var quiz = await _context.Quizzes.FindAsync(quizId);
        if (quiz is null)
            return NotFound("Quiz not found.");

        _context.Quizzes.Remove(quiz);
        await _context.SaveChangesAsync();
        return Ok("Quiz deleted.");
    }

    [HttpPatch("{quizId:int}/publish")]
    public async Task<IActionResult> SetPublishStatus(int quizId, [FromBody] PublishQuizDto dto)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
            .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == quizId);

        if (quiz is null)
            return NotFound("Quiz not found.");

        if (dto.IsPublished)
        {
            var validationError = ValidateQuizForPublish(quiz);
            if (validationError is not null)
                return BadRequest(validationError);
        }

        quiz.IsPublished = dto.IsPublished;
        quiz.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new
        {
            quiz.Id,
            quiz.IsPublished,
            quiz.UpdatedAt
        });
    }

    [HttpPatch("{quizId:int}/activate")]
    public async Task<IActionResult> SetActiveStatus(int quizId, [FromBody] bool isActive)
    {
        var quiz = await _context.Quizzes.FindAsync(quizId);
        if (quiz is null)
            return NotFound("Quiz not found.");

        quiz.IsActive = isActive;
        quiz.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            quiz.Id,
            quiz.IsActive,
            quiz.UpdatedAt
        });
    }

    [HttpPost("{quizId:int}/questions")]
    public async Task<IActionResult> CreateQuestion(int quizId, [FromBody] QuestionUpsertDto dto)
    {
        if (!await _context.Quizzes.AnyAsync(q => q.Id == quizId))
            return NotFound("Quiz not found.");

        var question = new QuizQuestion
        {
            QuizId = quizId,
            Prompt = dto.Prompt.Trim(),
            ImageUrl = dto.ImageUrl,
            AudioUrl = dto.AudioUrl,
            Explanation = dto.Explanation,
            SortOrder = dto.SortOrder,
            Points = Math.Max(1, dto.Points),
            IsActive = dto.IsActive
        };

        _context.QuizQuestions.Add(question);
        await _context.SaveChangesAsync();
        return Ok(question);
    }

    [HttpPut("questions/{questionId:int}")]
    public async Task<IActionResult> UpdateQuestion(int questionId, [FromBody] QuestionUpsertDto dto)
    {
        var question = await _context.QuizQuestions.FindAsync(questionId);
        if (question is null)
            return NotFound("Question not found.");

        question.Prompt = dto.Prompt.Trim();
        question.ImageUrl = dto.ImageUrl;
        question.AudioUrl = dto.AudioUrl;
        question.Explanation = dto.Explanation;
        question.SortOrder = dto.SortOrder;
        question.Points = Math.Max(1, dto.Points);
        question.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();
        return Ok(question);
    }

    [HttpDelete("questions/{questionId:int}")]
    public async Task<IActionResult> DeleteQuestion(int questionId)
    {
        var question = await _context.QuizQuestions.FindAsync(questionId);
        if (question is null)
            return NotFound("Question not found.");

        _context.QuizQuestions.Remove(question);
        await _context.SaveChangesAsync();
        return Ok("Question deleted.");
    }

    [HttpPatch("questions/{questionId:int}/reorder")]
    public async Task<IActionResult> ReorderQuestion(int questionId, [FromBody] ReorderQuestionDto dto)
    {
        var question = await _context.QuizQuestions.FindAsync(questionId);
        if (question is null)
            return NotFound("Question not found.");

        question.SortOrder = dto.SortOrder;
        await _context.SaveChangesAsync();
        return Ok(question);
    }

    [HttpPost("questions/{questionId:int}/options")]
    public async Task<IActionResult> CreateOption(int questionId, [FromBody] AnswerOptionUpsertDto dto)
    {
        if (!await _context.QuizQuestions.AnyAsync(q => q.Id == questionId))
            return NotFound("Question not found.");

        if (dto.IsCorrect)
        {
            var existingCorrect = await _context.AnswerOptions
                .Where(x => x.QuestionId == questionId && x.IsCorrect)
                .ToListAsync();

            foreach (var option in existingCorrect)
                option.IsCorrect = false;
        }

        var optionToCreate = new AnswerOption
        {
            QuestionId = questionId,
            Text = dto.Text.Trim(),
            ImageUrl = dto.ImageUrl,
            SortOrder = dto.SortOrder,
            IsCorrect = dto.IsCorrect
        };

        _context.AnswerOptions.Add(optionToCreate);
        await _context.SaveChangesAsync();
        return Ok(optionToCreate);
    }

    [HttpPut("options/{optionId:int}")]
    public async Task<IActionResult> UpdateOption(int optionId, [FromBody] AnswerOptionUpsertDto dto)
    {
        var option = await _context.AnswerOptions.FindAsync(optionId);
        if (option is null)
            return NotFound("Option not found.");

        if (dto.IsCorrect)
        {
            var siblingOptions = await _context.AnswerOptions
                .Where(x => x.QuestionId == option.QuestionId && x.Id != optionId && x.IsCorrect)
                .ToListAsync();

            foreach (var sibling in siblingOptions)
                sibling.IsCorrect = false;
        }

        option.Text = dto.Text.Trim();
        option.ImageUrl = dto.ImageUrl;
        option.SortOrder = dto.SortOrder;
        option.IsCorrect = dto.IsCorrect;

        await _context.SaveChangesAsync();
        return Ok(option);
    }

    [HttpDelete("options/{optionId:int}")]
    public async Task<IActionResult> DeleteOption(int optionId)
    {
        var option = await _context.AnswerOptions.FindAsync(optionId);
        if (option is null)
            return NotFound("Option not found.");

        _context.AnswerOptions.Remove(option);
        await _context.SaveChangesAsync();
        return Ok("Option deleted.");
    }

    private static string? ValidateQuizForPublish(Quiz quiz)
    {
        if (quiz.Questions.Count == 0)
            return "Quiz needs at least 1 question before publishing.";

        foreach (var question in quiz.Questions)
        {
            if (question.Options.Count < 2)
                return $"Question {question.Id} needs at least 2 options.";

            var correctCount = question.Options.Count(option => option.IsCorrect);
            if (correctCount != 1)
                return $"Question {question.Id} must have exactly 1 correct option.";
        }

        return null;
    }
}
