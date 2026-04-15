using DiveIntoIVE.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace DiveIntoIVE.Services.Interfaces;
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }


    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        await _authService.RegisterAsync(dto);
        return Ok("Registered. Please verify email.");
    }

    [EnableRateLimiting("loginLimiter")]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        return Ok(result);
    }
    //[HttpPost("login")]
    //public async Task<IActionResult> Login(LoginDto dto)
    //{
    //    var result = await _authService.LoginAsync(dto);
    //    return Ok(result);
    //}

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        await _authService.VerifyEmailAsync(token);
        return Ok("Email verified.");
    }
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        await _authService.ForgotPasswordAsync(dto);
        return Ok("Password reset email sent.");
    }
    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification(ResendVerificationDto dto)
    {
        await _authService.ResendVerificationEmailAsync(dto.Email);
        return Ok("Verification email sent.");
    }
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        await _authService.ResetPasswordAsync(dto);
        return Ok("Password successfully reset.");
    }
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutDto dto)
    {
        await _authService.LogoutAsync(dto);
        return Ok("Logged out successfully.");
    }

    [EnableRateLimiting("loginLimiter")]
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken(RefreshTokenDto dto)
    {
        var result = await _authService.RefreshTokenAsync(dto);
        return Ok(result);
    }
    //[HttpPost("refresh-token")]
    //public async Task<IActionResult> RefreshToken(RefreshTokenDto dto)
    //{
    //    var result = await _authService.RefreshTokenAsync(dto);
    //    return Ok(result);
    //}

}
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using DiveIntoIVE.Data;
//using DiveIntoIVE.Models;
//using System.Security.Cryptography;
//using System.Text;
//using Microsoft.IdentityModel.Tokens;
//using System.IdentityModel.Tokens.Jwt;
//using System.Security.Claims;

//namespace DiveIntoIVE.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class AuthController : ControllerBase
//    {
//        private readonly AppDbContext _context;
//        private readonly EmailService _emailService;

//        public AuthController(AppDbContext context, EmailService emailService)
//        {
//            _context = context;
//            _emailService = emailService;
//        }

//        // REGISTER
//        [HttpPost("register")]
//        public async Task<IActionResult> Register([FromBody] User userDto)
//        {
//            if (await _context.Users.AnyAsync(u => u.Username == userDto.Username))
//                return BadRequest("Username already exists.");

//            if (await _context.Users.AnyAsync(u => u.Email == userDto.Email))
//                return BadRequest("Email already registered.");

//            var hashedPassword = HashPassword(userDto.Password);

//            var verificationToken = Guid.NewGuid().ToString();

//            var user = new User
//            {
//                Username = userDto.Username,
//                Password = hashedPassword,
//                Email = userDto.Email,
//                Role = "User",
//                EmailVerificationToken = verificationToken,
//                EmailVerificationExpiry = DateTime.UtcNow.AddHours(24),
//                IsEmailVerified = false
//            };

//            _context.Users.Add(user);
//            await _context.SaveChangesAsync();

//            await _emailService.SendVerificationEmail(user.Email, verificationToken);

//            return Ok(new
//            {
//                message = "User registered successfully. Please check your email to verify your account."
//            });
//        }

//        // LOGIN
//        [HttpPost("login")]
//        public async Task<IActionResult> Login([FromBody] User login)
//        {
//            var user = await _context.Users
//                .FirstOrDefaultAsync(
//                u =>
//                //u.Username == login.Username || u.Email == login.Username
//                u.Username == login.Username
//                    );

//            if (user == null)
//                return Unauthorized("Invalid username/email or password.");

//            if (!VerifyPassword(login.Password, user.Password))
//                return Unauthorized("Invalid username/email or password.");

//            if (!user.IsEmailVerified)
//                return Unauthorized("Please verify your email before logging in.");

//            var token = GenerateJwtToken(user);

//            return Ok(new
//            {
//                token = token,
//                username = user.Username,
//                role = user.Role
//            });
//        }

//        // VERIFY EMAIL
//        [HttpGet("verify-email")]
//        public async Task<IActionResult> VerifyEmail(string token)
//        {
//            var user = await _context.Users
//                .FirstOrDefaultAsync(u => u.EmailVerificationToken == token);

//            if (user == null)
//                return BadRequest("Invalid token.");

//            if (user.EmailVerificationExpiry < DateTime.UtcNow)
//                return BadRequest("Verification token expired.");

//            user.IsEmailVerified = true;
//            user.EmailVerificationToken = null;
//            user.EmailVerificationExpiry = null;

//            await _context.SaveChangesAsync();

//            return Ok("Email verified successfully. You can now login.");
//        }

//        // PASSWORD HASH
//        private string HashPassword(string password)
//        {
//            using var sha256 = SHA256.Create();
//            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
//            return Convert.ToBase64String(bytes);
//        }

//        private bool VerifyPassword(string entered, string storedHash)
//        {
//            return HashPassword(entered) == storedHash;
//        }

//        // JWT TOKEN
//        private string GenerateJwtToken(User user)
//        {
//            var jwtSettings = HttpContext.RequestServices
//                .GetRequiredService<IConfiguration>()
//                .GetSection("Jwt");

//            var key = new SymmetricSecurityKey(
//                Encoding.UTF8.GetBytes(jwtSettings["Key"])
//            );

//            var credentials = new SigningCredentials(
//                key,
//                SecurityAlgorithms.HmacSha256
//            );

//            var claims = new[]
//            {
//                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
//                new Claim(ClaimTypes.Name, user.Username),
//                new Claim(ClaimTypes.Role, user.Role)
//            };

//            var token = new JwtSecurityToken(
//                issuer: jwtSettings["Issuer"],
//                audience: jwtSettings["Audience"],
//                claims: claims,
//                expires: DateTime.Now.AddMinutes(
//                    Convert.ToDouble(jwtSettings["DurationInMinutes"])
//                ),
//                signingCredentials: credentials
//            );

//            return new JwtSecurityTokenHandler().WriteToken(token);
//        }
//    }
//}   