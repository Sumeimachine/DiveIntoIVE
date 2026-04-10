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


}
