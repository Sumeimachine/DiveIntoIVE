namespace DiveIntoIVE.Models;

public class QuizQuestion
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? AudioUrl { get; set; }
    public string? Explanation { get; set; }
    public int SortOrder { get; set; }
    public int Points { get; set; } = 1;
    public bool IsActive { get; set; } = true;

    public Quiz Quiz { get; set; } = null!;
    public List<AnswerOption> Options { get; set; } = new();
}
