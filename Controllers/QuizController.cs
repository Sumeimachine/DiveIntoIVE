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
        var userIdResult = TryGetCurrentUserId();
        if (userIdResult is null)
            return Unauthorized("User identity is invalid.");

        var userId = userIdResult.Value;
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

        var attemptCount = await _context.QuizAttempts.CountAsync(x => x.QuizId == quiz.Id && x.UserId == userId);
        return Ok(MapQuiz(quiz, attemptCount));
    }

    [HttpGet("active-list")]
    public async Task<IActionResult> GetActiveQuizzes()
    {
        var userIdResult = TryGetCurrentUserId();
        if (userIdResult is null)
            return Unauthorized("User identity is invalid.");

        var userId = userIdResult.Value;
        var now = DateTime.UtcNow;

        var quizzes = await _context.Quizzes
            .Include(x => x.Questions.OrderBy(q => q.SortOrder))
            .ThenInclude(q => q.Options.OrderBy(o => o.SortOrder))
            .Where(x => x.IsActive && x.IsPublished
                        && (!x.StartAt.HasValue || x.StartAt <= now)
                        && (!x.EndAt.HasValue || x.EndAt >= now))
            .OrderByDescending(x => x.StartAt)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();

        if (!quizzes.Any())
            return Ok(new { message = "No active quizzes yet." });

        var quizIds = quizzes.Select(x => x.Id).ToList();
        var attemptCountsByQuiz = await _context.QuizAttempts
            .Where(x => x.UserId == userId && quizIds.Contains(x.QuizId))
            .GroupBy(x => x.QuizId)
            .Select(group => new
            {
                QuizId = group.Key,
                AttemptCount = group.Count()
            })
            .ToDictionaryAsync(x => x.QuizId, x => x.AttemptCount);

        var payload = quizzes
            .Select(quiz => MapQuiz(quiz, attemptCountsByQuiz.GetValueOrDefault(quiz.Id, 0)))
            .ToList();

        return Ok(payload);
    }

    [HttpGet("{quizId:int}")]
    public async Task<IActionResult> GetQuizById(int quizId)
    {
        var userIdResult = TryGetCurrentUserId();
        if (userIdResult is null)
            return Unauthorized("User identity is invalid.");

        var userId = userIdResult.Value;
        var quiz = await _context.Quizzes
            .Include(x => x.Questions.OrderBy(q => q.SortOrder))
            .ThenInclude(q => q.Options.OrderBy(o => o.SortOrder))
            .FirstOrDefaultAsync(x => x.Id == quizId && x.IsPublished && x.IsActive);

        if (quiz is null)
            return NotFound("Quiz not found.");

        var attemptCount = await _context.QuizAttempts.CountAsync(x => x.QuizId == quiz.Id && x.UserId == userId);
        return Ok(MapQuiz(quiz, attemptCount));
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

        var hasScoredAttempt = quiz.Type == "daily" && await _context.QuizAttempts
            .AnyAsync(x => x.QuizId == quizId && x.UserId == userId);

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

        var isScoreCounted = !hasScoredAttempt;

        if (quiz.IsGraded && isScoreCounted)
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
            currencyBalance = user.CurrencyBalance,
            isScoreCounted,
            message = isScoreCounted
                ? "Score counted successfully."
                : "Retake submitted. Score and rewards are locked after your first attempt."
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
                attempt.Id,
                attempt.QuizId,
                attempt.UserId,
                Username = attempt.User.Username,
                QuizType = attempt.Quiz.Type,
                attempt.CompletedAt,
                attempt.Score,
                CorrectAnswers = attempt.Answers.Count(answer => answer.IsCorrect)
            })
            .ToListAsync();

        var countedAttempts = attempts
            .GroupBy(attempt => new { attempt.UserId, attempt.QuizId })
            .SelectMany(group =>
            {
                var quizType = group.First().QuizType;
                if (quizType == "daily")
                {
                    var firstAttempt = group
                        .OrderBy(attempt => attempt.CompletedAt ?? DateTime.MaxValue)
                        .ThenBy(attempt => attempt.Id)
                        .Take(1);
                    return firstAttempt;
                }

                return group;
            })
            .ToList();

        var payload = countedAttempts
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

    private static QuizViewDto MapQuiz(Quiz quiz, int attemptCount)
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
                .ToList(),
            attemptCount > 0,
            attemptCount
        );
    }

    private int? TryGetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
