using DiveIntoIVE.Data;
using DiveIntoIVE.Models;
using DiveIntoIVE.DTOs.Auth;
using DiveIntoIVE.Services.Interfaces;

using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

public class AuthService : IAuthService
{

    private const int DailyLoginReward = 1;
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext context, IEmailService emailService, IConfiguration config)
    {
        _context = context;
        _emailService = emailService;
        _config = config;
    }

    // ------------------------------------------------------------
    // Generates a secure random refresh token
    // ------------------------------------------------------------
    private string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    private static bool HasClaimedDailyRewardToday(User user)
    {
        return user.LastDailyRewardClaimedAtUtc?.Date == DateTime.UtcNow.Date;
    }

    // ------------------------------------------------------------
    // USER REGISTRATION
    // Creates user, hashes password, generates email verification
    // ------------------------------------------------------------
    public async Task RegisterAsync(RegisterDto dto)
    {
        // Prevent duplicate username
        if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
            throw new Exception("Username already exists.");

        // Prevent duplicate email
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            throw new Exception("Email already registered.");

        // Generate verification token
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        var user = new User
        {
            Username = dto.Username,
            Password = BCrypt.Net.BCrypt.HashPassword(dto.Password), // secure password hashing
            Email = dto.Email,
            Role = "User",

            EmailVerificationToken = token,
            EmailVerificationExpiry = DateTime.UtcNow.AddHours(24),
            IsEmailVerified = false
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Send verification email
        await _emailService.SendVerificationEmailAsync(user.Email, token);
    }

    // ------------------------------------------------------------
    // LOGIN
    // Handles:
    // - account lockout
    // - password verification
    // - refresh token session creation
    // ------------------------------------------------------------
    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u =>
                (u.Username == dto.Username || u.Email == dto.Username)
            && u.IsEmailVerified);

        if (user == null)
            throw new Exception("Invalid username or password.");

        // Check account lockout
        if (user.LockoutEnd != null && user.LockoutEnd > DateTime.UtcNow)
            throw new Exception("Account locked. Try again later.");

        // Validate password
        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
        {
            user.FailedLoginAttempts++;

            // Lock account after 5 failed attempts
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                user.FailedLoginAttempts = 0;
            }

            await _context.SaveChangesAsync();

            throw new Exception("Invalid username or password.");
        }

        // Reset login attempt counter
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;

        var dailyRewardClaimedToday = HasClaimedDailyRewardToday(user);
        if (!dailyRewardClaimedToday)
        {
            user.CurrencyBalance += DailyLoginReward;
            user.LastDailyRewardClaimedAtUtc = DateTime.UtcNow;
            dailyRewardClaimedToday = true;
        }

        // Generate JWT access token
        var jwt = GenerateJwt(user);

        // Generate refresh token
        var refreshToken = GenerateRefreshToken();

        // Store refresh token in session table
        var tokenEntity = new UserRefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            Device = "Browser", // later you can read user-agent
            Expires = DateTime.UtcNow.AddDays(7),
            Revoked = false
        };

        _context.UserRefreshTokens.Add(tokenEntity);

        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            Token = jwt,
            RefreshToken = refreshToken,
            Username = user.Username,
            Role = user.Role,
            CurrencyBalance = user.CurrencyBalance,
            DailyRewardClaimedToday = dailyRewardClaimedToday
        };
    }

    // ------------------------------------------------------------
    // EMAIL VERIFICATION
    // ------------------------------------------------------------
    public async Task VerifyEmailAsync(string token)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.EmailVerificationToken == token);

        if (user == null)
            throw new Exception("Invalid token.");

        if (user.EmailVerificationExpiry < DateTime.UtcNow)
            throw new Exception("Token expired.");

        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationExpiry = null;

        await _context.SaveChangesAsync();
    }

    // ------------------------------------------------------------
    // JWT GENERATION
    // Creates short-lived access token
    // ------------------------------------------------------------
    private string GenerateJwt(User user)
    {
        var jwt = _config.GetSection("Jwt");

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwt["Key"]!)
        );

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                Convert.ToDouble(jwt["DurationInMinutes"])
            ),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ------------------------------------------------------------
    // PASSWORD RESET REQUEST
    // Sends reset email
    // ------------------------------------------------------------
    public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null)
            throw new Exception("User with this email does not exist.");

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        user.PasswordResetToken = token;
        user.PasswordResetExpiry = DateTime.UtcNow.AddHours(1);

        await _context.SaveChangesAsync();

        await _emailService.SendPasswordResetEmailAsync(user.Email, token);
    }

    // ------------------------------------------------------------
    // PASSWORD RESET EXECUTION
    // ------------------------------------------------------------
    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.PasswordResetToken == dto.Token);

        if (user == null)
            throw new Exception("Invalid reset token.");

        if (user.PasswordResetExpiry < DateTime.UtcNow)
            throw new Exception("Reset token expired.");

        user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

        user.PasswordResetToken = null;
        user.PasswordResetExpiry = null;

        await _context.SaveChangesAsync();
    }

    // ------------------------------------------------------------
    // REFRESH TOKEN
    // Rotates refresh token for security
    // ------------------------------------------------------------
    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto dto)
    {
        var token = await _context.UserRefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == dto.RefreshToken);

        if (token == null)
            throw new Exception("Invalid refresh token");

        if (token.Revoked)
            throw new Exception("Token revoked");

        if (token.Expires < DateTime.UtcNow)
            throw new Exception("Token expired");

        // Generate new access token
        var newJwt = GenerateJwt(token.User);

        // Rotate refresh token
        token.Revoked = true;

        var newRefreshToken = GenerateRefreshToken();

        _context.UserRefreshTokens.Add(new UserRefreshToken
        {
            UserId = token.UserId,
            Token = newRefreshToken,
            Device = token.Device,
            Expires = DateTime.UtcNow.AddDays(7)
        });

        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            Token = newJwt,
            RefreshToken = newRefreshToken,
            Username = token.User.Username,
            Role = token.User.Role,
            CurrencyBalance = token.User.CurrencyBalance,
            DailyRewardClaimedToday = HasClaimedDailyRewardToday(token.User)
        };
    }

    // ------------------------------------------------------------
    // LOGOUT
    // Revokes a specific session refresh token
    // ------------------------------------------------------------
    public async Task LogoutAsync(LogoutDto dto)
    {
        var token = await _context.UserRefreshTokens
            .FirstOrDefaultAsync(t => t.Token == dto.RefreshToken);

        if (token == null)
            return;

        token.Revoked = true;

        await _context.SaveChangesAsync();
    }

    public async Task ResendVerificationEmailAsync(string email)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
            throw new Exception("User not found.");

        if (user.IsEmailVerified)
            throw new Exception("Email already verified.");

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        user.EmailVerificationToken = token;
        user.EmailVerificationExpiry = DateTime.UtcNow.AddHours(24);

        await _context.SaveChangesAsync();

        await _emailService.SendVerificationEmailAsync(user.Email, token);
    }
}
