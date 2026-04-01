public class AuthResponseDto
{
    public string Token { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string Role { get; set; } = default!;
}