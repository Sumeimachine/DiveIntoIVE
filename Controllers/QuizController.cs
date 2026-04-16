using System.Security.Claims;
using DiveIntoIVE.Data;
using DiveIntoIVE.DTOs.Quiz;
using DiveIntoIVE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiveIntoIVE.Controllers;

[ApiController]
[Authorize]
[Route("api/quizzes")]
public class QuizController : ControllerBase
{
    private readonly AppDbContext _context;

    public QuizController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveQuiz([FromQuery] string type = "daily")
    {
        var now = DateTime.UtcNow;
        var quiz = await _context.Quizzes
            .Include(x => x.Questions.OrderBy(q => q.SortOrder))
            .ThenInclude(q => q.Options.OrderBy(o => o.SortOrder))
            .Where(x => x.IsActive && x.IsPublished && x.Type == type
                        && (!x.StartAt.HasValue || x.StartAt <= now)
                        && (!x.EndAt.HasValue || x.EndAt >= now))
            .OrderByDescending(x => x.StartAt)
            .FirstOrDefaultAsync();

        if (quiz is null)
            return Ok(new { message = "No active quiz yet." });

        return Ok(MapQuiz(quiz));
    }

    [HttpGet("{quizId:int}")]
    public async Task<IActionResult> GetQuizById(int quizId)
    {
        var quiz = await _context.Quizzes
            .Include(x => x.Questions.OrderBy(q => q.SortOrder))
            .ThenInclude(q => q.Options.OrderBy(o => o.SortOrder))
            .FirstOrDefaultAsync(x => x.Id == quizId && x.IsPublished && x.IsActive);

        if (quiz is null)
            return NotFound("Quiz not found.");

        return Ok(MapQuiz(quiz));
    }

    [HttpPost("{quizId:int}/submit")]
    public async Task<IActionResult> SubmitAttempt(int quizId, [FromBody] SubmitQuizAttemptDto dto)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized("User identity is invalid.");

        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
            .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(x => x.Id == quizId && x.IsPublished && x.IsActive);

        if (quiz is null)
            return NotFound("Quiz not found.");

        if (quiz.Type == "daily")
        {
            var hasExistingAttempt = await _context.QuizAttempts
                .AnyAsync(x => x.QuizId == quizId && x.UserId == userId);

            if (hasExistingAttempt)
                return BadRequest("Daily quiz can only be answered once.");
        }

        var attempt = new QuizAttempt
        {
            QuizId = quizId,
            UserId = userId,
            StartedAt = DateTime.UtcNow,
            TotalQuestions = quiz.Questions.Count
        };

        var score = 0;
        foreach (var answer in dto.Answers)
        {
            var question = quiz.Questions.FirstOrDefault(x => x.Id == answer.QuestionId);
            var option = question?.Options.FirstOrDefault(x => x.Id == answer.AnswerOptionId);
            var isCorrect = option?.IsCorrect == true;

            if (isCorrect)
                score += question?.Points ?? 0;

            attempt.Answers.Add(new QuizAttemptAnswer
            {
                QuestionId = answer.QuestionId,
                AnswerOptionId = answer.AnswerOptionId,
                IsCorrect = isCorrect,
                AnsweredAt = DateTime.UtcNow
            });
        }

        attempt.Score = score;
        attempt.CompletedAt = DateTime.UtcNow;

        var currencyAwarded = 0;
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user is null)
            return NotFound("User not found.");

        if (quiz.IsGraded)
        {
            currencyAwarded = attempt.Answers.Count(x => x.IsCorrect);
            user.CurrencyBalance += currencyAwarded;
        }

        _context.QuizAttempts.Add(attempt);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            attempt.Id,
            attempt.Score,
            attempt.TotalQuestions,
            correctAnswers = attempt.Answers.Count(x => x.IsCorrect),
            currencyAwarded,
            currencyBalance = user.CurrencyBalance
        });
    }

    [HttpGet("leaderboard")]
    public async Task<IActionResult> GetLeaderboard([FromQuery] bool graded = true, [FromQuery] int top = 20)
    {
        var normalizedTop = Math.Clamp(top, 1, 100);

        var attempts = await _context.QuizAttempts
            .AsNoTracking()
            .Where(attempt => attempt.Quiz.IsPublished && attempt.Quiz.IsActive && attempt.Quiz.IsGraded == graded)
            .Select(attempt => new
            {
                attempt.UserId,
                Username = attempt.User.Username,
                attempt.Score,
                CorrectAnswers = attempt.Answers.Count(answer => answer.IsCorrect)
            })
            .ToListAsync();

        var payload = attempts
            .GroupBy(attempt => new { attempt.UserId, attempt.Username })
            .Select(group => new
            {
                group.Key.UserId,
                group.Key.Username,
                TotalScore = group.Sum(x => x.Score),
                TotalCorrectAnswers = group.Sum(x => x.CorrectAnswers),
                AttemptsCount = group.Count()
            })
            .OrderByDescending(x => x.TotalScore)
            .ThenByDescending(x => x.TotalCorrectAnswers)
            .ThenBy(x => x.Username)
            .Take(normalizedTop)
            .Select((entry, index) => new QuizLeaderboardEntryDto(
                index + 1,
                entry.UserId,
                entry.Username,
                entry.TotalScore,
                entry.TotalCorrectAnswers,
                entry.AttemptsCount
            ))
            .ToList();

        return Ok(payload);
    }

    [HttpGet("attempts/{attemptId:int}")]
    public async Task<IActionResult> GetAttempt(int attemptId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized("User identity is invalid.");

        var attempt = await _context.QuizAttempts
            .Include(x => x.Answers)
            .FirstOrDefaultAsync(x => x.Id == attemptId && x.UserId == userId);

        if (attempt is null)
            return NotFound("Attempt not found.");

        return Ok(attempt);
    }

    private static QuizViewDto MapQuiz(Quiz quiz)
    {
        return new QuizViewDto(
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
            quiz.Questions
                .Where(question => question.IsActive)
                .OrderBy(question => question.SortOrder)
                .Select(question => new QuizQuestionViewDto(
                    question.Id,
                    question.Prompt,
                    question.ImageUrl,
                    question.AudioUrl,
                    question.Explanation,
                    question.SortOrder,
                    question.Points,
                    question.Options
                        .OrderBy(option => option.SortOrder)
                        .Select(option => new QuizOptionViewDto(option.Id, option.Text, option.ImageUrl, option.SortOrder))
                        .ToList()
                ))
                .ToList()
        );
    }
}
