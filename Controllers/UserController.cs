using DiveIntoIVE.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DiveIntoIVE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var profile = new
            {
                username,
                role,
                currencyBalance = 0,
                dailyRewardClaimedToday = false
            };

            if (!int.TryParse(userIdString, out var userId))
            {
                return Ok(profile);
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return Ok(profile);
            }

            return Ok(new
            {
                username,
                role,
                currencyBalance = user.CurrencyBalance,
                dailyRewardClaimedToday = user.LastDailyRewardClaimedAtUtc?.Date == DateTime.UtcNow.Date
            });
        }
    }
}
