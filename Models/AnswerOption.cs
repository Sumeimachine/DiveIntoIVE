namespace DiveIntoIVE.Models;

public class AnswerOption
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsCorrect { get; set; }

    public QuizQuestion Question { get; set; } = null!;
}
