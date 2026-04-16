using System.Security.Claims;
using DiveIntoIVE.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiveIntoIVE.Controllers;

[ApiController]
[Authorize(Roles = "Super-Admin")]
[Route("api/super-admin/users")]
public class SuperAdminController : ControllerBase
{
    private readonly AppDbContext _context;

    public SuperAdminController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(user => user.Username)
            .Select(user => new
            {
                user.Id,
                user.Username,
                user.Email,
                user.Role,
                user.IsEmailVerified
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPatch("{userId:int}/role")]
    public async Task<IActionResult> SetRole(int userId, [FromBody] SetUserRoleDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user is null)
            return NotFound("User not found.");

        var role = dto.Role.Trim();
        if (role != "User" && role != "Admin")
            return BadRequest("Role must be either User or Admin.");

        var requesterIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(requesterIdValue, out var requesterId) && requesterId == userId)
            return BadRequest("You cannot change your own role.");

        user.Role = role;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            user.Id,
            user.Username,
            user.Role
        });
    }
}

public record SetUserRoleDto(string Role);
