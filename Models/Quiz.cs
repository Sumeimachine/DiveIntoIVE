namespace DiveIntoIVE.Models;

public class Quiz
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = "trivia";
    public bool IsGraded { get; set; } = false;
    public bool IsPublished { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<QuizQuestion> Questions { get; set; } = new();
    public List<QuizAttempt> Attempts { get; set; } = new();
}
