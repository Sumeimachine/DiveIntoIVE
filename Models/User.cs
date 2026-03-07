namespace DiveIntoIVE.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Username { get; set; } = "";

        public string Password { get; set; } = "";

        public string Email { get; set; } = "";

        public string Role { get; set; } = "User";

        public bool IsEmailVerified { get; set; } = false;

        public string? EmailVerificationToken { get; set; }

        public DateTime? EmailVerificationExpiry { get; set; }
    }
}