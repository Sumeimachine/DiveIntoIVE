namespace DiveIntoIVE.Models;

public class EventReward
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int Points { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<UserEventRewardClaim> Claims { get; set; } = new();
}
