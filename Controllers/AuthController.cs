using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DiveIntoIVE.Data;
using DiveIntoIVE.Models;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DiveIntoIVE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;

        public AuthController(AppDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // REGISTER
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User userDto)
        {
            if (await _context.Users.AnyAsync(u => u.Username == userDto.Username))
                return BadRequest("Username already exists.");

            if (await _context.Users.AnyAsync(u => u.Email == userDto.Email))
                return BadRequest("Email already registered.");

            var hashedPassword = HashPassword(userDto.Password);

            var verificationToken = Guid.NewGuid().ToString();

            var user = new User
            {
                Username = userDto.Username,
                Password = hashedPassword,
                Email = userDto.Email,
                Role = "User",
                EmailVerificationToken = verificationToken,
                EmailVerificationExpiry = DateTime.UtcNow.AddHours(24),
                IsEmailVerified = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await _emailService.SendVerificationEmail(user.Email, verificationToken);

            return Ok(new
            {
                message = "User registered successfully. Please check your email to verify your account."
            });
        }

        // LOGIN
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User login)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(
                u =>
                //u.Username == login.Username || u.Email == login.Username
                u.Username == login.Username
                    );

            if (user == null)
                return Unauthorized("Invalid username/email or password.");

            if (!VerifyPassword(login.Password, user.Password))
                return Unauthorized("Invalid username/email or password.");

            if (!user.IsEmailVerified)
                return Unauthorized("Please verify your email before logging in.");

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                token = token,
                username = user.Username,
                role = user.Role
            });
        }

        // VERIFY EMAIL
        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail(string token)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.EmailVerificationToken == token);

            if (user == null)
                return BadRequest("Invalid token.");

            if (user.EmailVerificationExpiry < DateTime.UtcNow)
                return BadRequest("Verification token expired.");

            user.IsEmailVerified = true;
            user.EmailVerificationToken = null;
            user.EmailVerificationExpiry = null;

            await _context.SaveChangesAsync();

            return Ok("Email verified successfully. You can now login.");
        }

        // PASSWORD HASH
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private bool VerifyPassword(string entered, string storedHash)
        {
            return HashPassword(entered) == storedHash;
        }

        // JWT TOKEN
        private string GenerateJwtToken(User user)
        {
            var jwtSettings = HttpContext.RequestServices
                .GetRequiredService<IConfiguration>()
                .GetSection("Jwt");

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["Key"])
            );

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            );

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(
                    Convert.ToDouble(jwtSettings["DurationInMinutes"])
                ),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}   