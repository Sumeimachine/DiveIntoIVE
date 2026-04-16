using System.Security.Claims;
using DiveIntoIVE.Data;
using DiveIntoIVE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiveIntoIVE.Controllers;

[ApiController]
[Authorize]
[Route("api/event-rewards")]
public class EventRewardsController : ControllerBase
{
    private readonly AppDbContext _context;

    public EventRewardsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveEventReward()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized("User identity is invalid.");

        var now = DateTime.UtcNow;

        var activeEvents = await _context.EventRewards
            .AsNoTracking()
            .Where(eventReward => eventReward.IsActive
                                  && eventReward.StartAtUtc <= now
                                  && eventReward.EndAtUtc >= now)
            .OrderByDescending(eventReward => eventReward.StartAtUtc)
            .ToListAsync();

        if (activeEvents.Count == 0)
            return Ok(new { message = "No active event reward." });

        var claimedIds = await _context.UserEventRewardClaims
            .Where(claim => claim.UserId == userId)
            .Select(claim => claim.EventRewardId)
            .ToListAsync();

        var nextEvent = activeEvents.FirstOrDefault(eventReward => !claimedIds.Contains(eventReward.Id));
        if (nextEvent is null)
            return Ok(new { message = "No unclaimed active event reward." });

        return Ok(new
        {
            nextEvent.Id,
            nextEvent.Title,
            nextEvent.Message,
            nextEvent.Points,
            nextEvent.StartAtUtc,
            nextEvent.EndAtUtc
        });
    }

    [HttpPost("{eventRewardId:int}/claim")]
    public async Task<IActionResult> ClaimEventReward(int eventRewardId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized("User identity is invalid.");

        var now = DateTime.UtcNow;

        var eventReward = await _context.EventRewards
            .FirstOrDefaultAsync(item => item.Id == eventRewardId && item.IsActive && item.StartAtUtc <= now && item.EndAtUtc >= now);

        if (eventReward is null)
            return NotFound("Event reward is not available.");

        var alreadyClaimed = await _context.UserEventRewardClaims
            .AnyAsync(claim => claim.UserId == userId && claim.EventRewardId == eventRewardId);

        if (alreadyClaimed)
            return BadRequest("Event reward already claimed.");

        var user = await _context.Users.FirstOrDefaultAsync(item => item.Id == userId);
        if (user is null)
            return NotFound("User not found.");

        _context.UserEventRewardClaims.Add(new UserEventRewardClaim
        {
            UserId = userId,
            EventRewardId = eventRewardId,
            ClaimedAtUtc = DateTime.UtcNow
        });

        user.CurrencyBalance += Math.Max(0, eventReward.Points);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            eventReward.Id,
            eventReward.Title,
            eventReward.Points,
            currencyBalance = user.CurrencyBalance
        });
    }

    [Authorize(Roles = "Super-Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAllEventRewards()
    {
        var events = await _context.EventRewards
            .AsNoTracking()
            .OrderByDescending(item => item.StartAtUtc)
            .Select(item => new
            {
                item.Id,
                item.Title,
                item.Message,
                item.Points,
                item.IsActive,
                item.StartAtUtc,
                item.EndAtUtc,
                ClaimsCount = item.Claims.Count
            })
            .ToListAsync();

        return Ok(events);
    }

    [Authorize(Roles = "Super-Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateEventReward([FromBody] UpsertEventRewardDto dto)
    {
        if (dto.EndAtUtc <= dto.StartAtUtc)
            return BadRequest("End date must be after start date.");

        var eventReward = new EventReward
        {
            Title = dto.Title.Trim(),
            Message = dto.Message.Trim(),
            Points = Math.Max(0, dto.Points),
            IsActive = dto.IsActive,
            StartAtUtc = dto.StartAtUtc,
            EndAtUtc = dto.EndAtUtc,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _context.EventRewards.Add(eventReward);
        await _context.SaveChangesAsync();

        return Ok(eventReward);
    }

    [Authorize(Roles = "Super-Admin")]
    [HttpPut("{eventRewardId:int}")]
    public async Task<IActionResult> UpdateEventReward(int eventRewardId, [FromBody] UpsertEventRewardDto dto)
    {
        if (dto.EndAtUtc <= dto.StartAtUtc)
            return BadRequest("End date must be after start date.");

        var eventReward = await _context.EventRewards.FirstOrDefaultAsync(item => item.Id == eventRewardId);
        if (eventReward is null)
            return NotFound("Event reward not found.");

        eventReward.Title = dto.Title.Trim();
        eventReward.Message = dto.Message.Trim();
        eventReward.Points = Math.Max(0, dto.Points);
        eventReward.IsActive = dto.IsActive;
        eventReward.StartAtUtc = dto.StartAtUtc;
        eventReward.EndAtUtc = dto.EndAtUtc;
        eventReward.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(eventReward);
    }

    [Authorize(Roles = "Super-Admin")]
    [HttpDelete("{eventRewardId:int}")]
    public async Task<IActionResult> DeleteEventReward(int eventRewardId)
    {
        var eventReward = await _context.EventRewards.FirstOrDefaultAsync(item => item.Id == eventRewardId);
        if (eventReward is null)
            return NotFound("Event reward not found.");

        _context.EventRewards.Remove(eventReward);
        await _context.SaveChangesAsync();
        return Ok("Event reward deleted.");
    }
}

public record UpsertEventRewardDto(
    string Title,
    string Message,
    int Points,
    bool IsActive,
    DateTime StartAtUtc,
    DateTime EndAtUtc
);
