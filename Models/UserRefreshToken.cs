namespace DiveIntoIVE.Models;

public class UserRefreshToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = default!;

    public string Token { get; set; } = default!;

    public string Device { get; set; } = "Unknown";

    public DateTime Expires { get; set; }

    public bool Revoked { get; set; }
}