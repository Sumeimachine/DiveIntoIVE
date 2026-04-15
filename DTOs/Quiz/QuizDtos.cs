namespace DiveIntoIVE.DTOs.Quiz;

public record QuizUpsertDto(
    string Title,
    string Slug,
    string? Description,
    string Type,
    bool IsGraded,
    bool IsActive,
    DateTime? StartAt,
    DateTime? EndAt
);

public record QuestionUpsertDto(
    string Prompt,
    string? ImageUrl,
    string? AudioUrl,
    string? Explanation,
    int SortOrder,
    int Points,
    bool IsActive
);

public record AnswerOptionUpsertDto(
    string Text,
    string? ImageUrl,
    int SortOrder,
    bool IsCorrect
);

public record ReorderQuestionDto(int SortOrder);

public record PublishQuizDto(bool IsPublished);

public record SubmitQuizAttemptDto(List<SubmitQuizAnswerDto> Answers);

public record SubmitQuizAnswerDto(int QuestionId, int AnswerOptionId);

public record QuizOptionViewDto(int Id, string Text, string? ImageUrl, int SortOrder);

public record QuizQuestionViewDto(
    int Id,
    string Prompt,
    string? ImageUrl,
    string? AudioUrl,
    string? Explanation,
    int SortOrder,
    int Points,
    List<QuizOptionViewDto> Options
);

public record QuizViewDto(
    int Id,
    string Title,
    string Slug,
    string? Description,
    string Type,
    bool IsGraded,
    bool IsPublished,
    bool IsActive,
    DateTime? StartAt,
    DateTime? EndAt,
    List<QuizQuestionViewDto> Questions
);

public record QuizLeaderboardEntryDto(
    int Rank,
    int UserId,
    string Username,
    int TotalScore,
    int TotalCorrectAnswers,
    int AttemptsCount
);
