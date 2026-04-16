namespace DiveIntoIVE.Models;

public class QuizAttempt
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public int UserId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int Score { get; set; }
    public int TotalQuestions { get; set; }

    public Quiz Quiz { get; set; } = null!;
    public User User { get; set; } = null!;
    public List<QuizAttemptAnswer> Answers { get; set; } = new();
}
