namespace DiveIntoIVE.Models;

public class QuizAttemptAnswer
{
    public int Id { get; set; }
    public int QuizAttemptId { get; set; }
    public int QuestionId { get; set; }
    public int AnswerOptionId { get; set; }
    public bool IsCorrect { get; set; }
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;

    public QuizAttempt Attempt { get; set; } = null!;
    public QuizQuestion Question { get; set; } = null!;
    public AnswerOption AnswerOption { get; set; } = null!;
}
