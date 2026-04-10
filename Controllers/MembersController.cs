using DiveIntoIVE.Data;
using DiveIntoIVE.DTOs.Members;
using DiveIntoIVE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiveIntoIVE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MembersController : ControllerBase
    {
        private readonly AppDbContext _context;

        private static readonly List<MemberProfile> DefaultProfiles = new()
        {
            new() { MemberKey = "yujin", Name = "Yujin", PhotoUrl = "/images/members/yujin.jpg", Tagline = "Leader • Charisma Core", Bio = "Yujin brings confidence, sharp stage control, and warm leadership energy to IVE.", Accent = "#9F7AEA" },
            new() { MemberKey = "wonyoung", Name = "Wonyoung", PhotoUrl = "/images/members/wonyoung.jpg", Tagline = "Center • Elegance Icon", Bio = "Wonyoung is known for polished visuals, graceful delivery, and standout center presence.", Accent = "#F687B3" },
            new() { MemberKey = "liz", Name = "Liz", PhotoUrl = "/images/members/liz.jpg", Tagline = "Main Vocal • Honey Tone", Bio = "Liz delivers rich vocal color and emotional tone that shapes IVE's sound identity.", Accent = "#63B3ED" },
            new() { MemberKey = "gaeul", Name = "Gaeul", PhotoUrl = "/images/members/gaeul.jpg", Tagline = "Main Dancer • Chic Flow", Bio = "Gaeul balances precision and calm confidence with clean, stylish performance details.", Accent = "#B794F4" },
            new() { MemberKey = "rei", Name = "Rei", PhotoUrl = "/images/members/rei.jpg", Tagline = "Rapper • Creative Spark", Bio = "Rei adds playful personality and unique rap color that energizes every stage.", Accent = "#ED64A6" },
            new() { MemberKey = "leeseo", Name = "Leeseo", PhotoUrl = "/images/members/leeseo.jpg", Tagline = "Maknae • Bright Power", Bio = "Leeseo brings youthful brightness and dynamic momentum to IVE's team chemistry.", Accent = "#38B2AC" },
        };

        public MembersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var savedProfiles = await _context.MemberProfiles
                .AsNoTracking()
                .ToListAsync();

            if (savedProfiles.Count == 0)
            {
                return Ok(DefaultProfiles.Select(ToResponse));
            }

            var profilesByKey = savedProfiles.ToDictionary(profile => profile.MemberKey, profile => profile);

            var merged = DefaultProfiles.Select(defaultProfile =>
            {
                if (profilesByKey.TryGetValue(defaultProfile.MemberKey, out var persisted))
                {
                    return ToResponse(persisted);
                }

                return ToResponse(defaultProfile);
            });

            return Ok(merged);
        }

        [HttpGet("{memberKey}")]
        public async Task<IActionResult> GetByKey(string memberKey)
        {
            var profile = await _context.MemberProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.MemberKey == memberKey);

            if (profile != null)
            {
                return Ok(ToResponse(profile));
            }

            var fallback = DefaultProfiles.FirstOrDefault(item => item.MemberKey == memberKey);
            if (fallback == null)
            {
                return NotFound("Member profile not found.");
            }

            return Ok(ToResponse(fallback));
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{memberKey}")]
        public async Task<IActionResult> Update(string memberKey, UpdateMemberProfileDto dto)
        {
            var existing = await _context.MemberProfiles
                .FirstOrDefaultAsync(item => item.MemberKey == memberKey);

            if (existing == null)
            {
                existing = new MemberProfile
                {
                    MemberKey = memberKey
                };
                _context.MemberProfiles.Add(existing);
            }

            existing.Name = dto.Name;
            existing.PhotoUrl = dto.PhotoUrl;
            existing.Tagline = dto.Tagline;
            existing.Bio = dto.Bio;
            existing.Accent = dto.Accent;
            existing.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(ToResponse(existing));
        }

        private static object ToResponse(MemberProfile profile)
        {
            return new
            {
                id = profile.MemberKey,
                name = profile.Name,
                photoUrl = profile.PhotoUrl,
                tagline = profile.Tagline,
                bio = profile.Bio,
                accent = profile.Accent,
                updatedAtUtc = profile.UpdatedAtUtc
            };
        }
    }
}
