namespace DiveIntoIVE.Models;

public class UserEventRewardClaim
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int EventRewardId { get; set; }
    public DateTime ClaimedAtUtc { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public EventReward EventReward { get; set; } = null!;
}
