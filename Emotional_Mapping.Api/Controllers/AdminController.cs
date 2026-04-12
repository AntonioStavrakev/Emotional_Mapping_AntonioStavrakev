using Emotional_Mapping.Domain.Entities;
using Emotional_Mapping.Domain.Enums;
using Emotional_Mapping.Infrastructure.Data;
using Emotional_Mapping.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Emotional_Mapping.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<IdentityRole> _roles;
    private readonly AppDbContext _db;

    public AdminController(
        UserManager<ApplicationUser> users,
        RoleManager<IdentityRole> roles,
        AppDbContext db)
    {
        _users = users;
        _roles = roles;
        _db = db;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _users.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAtUtc)
            .Select(u => new
        {
            u.Id,
            u.Email,
            u.DisplayName,
            u.CreatedAtUtc
        })
        .ToListAsync();

        var superUserRoleId = await _roles.Roles
            .Where(r => r.Name == "SuperUser")
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        var superUserIds = string.IsNullOrWhiteSpace(superUserRoleId)
            ? new HashSet<string>()
            : (await _db.UserRoles
                .Where(x => x.RoleId == superUserRoleId)
                .Select(x => x.UserId)
                .ToListAsync())
            .ToHashSet();

        return Ok(users.Select(u => new
        {
            u.Id,
            u.Email,
            u.DisplayName,
            u.CreatedAtUtc,
            isSuperUser = superUserIds.Contains(u.Id)
        }));
    }

    [HttpGet("roles")]
    public IActionResult GetRoles()
        => Ok(_roles.Roles.Select(r => r.Name));

    [HttpPost("grant-superuser")]
    public async Task<IActionResult> GrantSuperUser([FromBody] RoleChangeRequest req)
    {
        var user = await _users.FindByIdAsync(req.UserId);
        if (user == null) return NotFound();

        if (!await _users.IsInRoleAsync(user, "SuperUser"))
            await _users.AddToRoleAsync(user, "SuperUser");

        return Ok(new { message = "Premium достъпът е активиран." });
    }

    [HttpPost("revoke-superuser")]
    public async Task<IActionResult> RevokeSuperUser([FromBody] RoleChangeRequest req)
    {
        var user = await _users.FindByIdAsync(req.UserId);
        if (user == null) return NotFound();

        if (await _users.IsInRoleAsync(user, "SuperUser"))
            await _users.RemoveFromRoleAsync(user, "SuperUser");

        return Ok(new { message = "Premium достъпът е премахнат." });
    }

    [HttpGet("emotions")]
    public async Task<IActionResult> GetEmotions()
        => Ok(await _db.EmotionCatalog.OrderBy(x => x.DisplayName).ToListAsync());

    [HttpPost("emotions")]
    public async Task<IActionResult> AddEmotion([FromBody] AddEmotionRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.DisplayName))
            return BadRequest(new { message = "Въведи име на емоцията." });

        if (!Enum.IsDefined(typeof(EmotionType), req.Emotion))
            return BadRequest(new { message = "Невалидна enum стойност за емоция." });

        if (await _db.EmotionCatalog.AnyAsync(x => x.Emotion == req.Emotion))
            return BadRequest(new { message = "Тази enum емоция вече съществува в каталога." });

        var item = new EmotionCatalogItem(req.Emotion, req.DisplayName, req.ColorHex, true);
        _db.EmotionCatalog.Add(item);
        await _db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpGet("points")]
    public async Task<IActionResult> GetPoints()
    {
        var points = await _db.EmotionalPoints
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(500)
            .Select(p => new
            {
                p.Id,
                p.UserId,
                emotion = p.Emotion.ToString(),
                p.Intensity,
                p.IsApproved,
                p.CreatedAtUtc
            })
            .ToListAsync();

        // Resolve user emails
        var userIds = points.Select(p => p.UserId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        var emailMap = new Dictionary<string, string>();
        foreach (var uid in userIds)
        {
            var user = await _users.FindByIdAsync(uid!);
            if (user?.Email != null)
                emailMap[uid!] = user.Email;
        }

        var result = points.Select(p => new
        {
            p.Id,
            p.UserId,
            userEmail = p.UserId != null && emailMap.ContainsKey(p.UserId) ? emailMap[p.UserId] : null,
            p.emotion,
            p.Intensity,
            p.IsApproved,
            p.CreatedAtUtc
        });

        return Ok(result);
    }

    [HttpGet("places/pending")]
    public async Task<IActionResult> GetPendingPlaces()
    {
        var places = await _db.Places
            .AsNoTracking()
            .Include(x => x.City)
            .Include(x => x.District)
            .Where(x => !x.IsApproved)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.Name,
                type = x.Type.ToString(),
                cityName = x.City.Name,
                districtName = x.District != null ? x.District.Name : "—",
                x.Address,
                x.Description,
                lat = x.Location.Lat,
                lng = x.Location.Lng,
                x.CreatedAtUtc,
                x.Source
            })
            .ToListAsync();

        return Ok(places);
    }

    [HttpPost("places/{id:guid}/approve")]
    public async Task<IActionResult> ApprovePlace(Guid id)
    {
        var place = await _db.Places.FirstOrDefaultAsync(x => x.Id == id);
        if (place == null) return NotFound();

        place.Approve();
        await _db.SaveChangesAsync();

        return Ok(new { message = "Мястото е одобрено." });
    }

    [HttpDelete("places/{id:guid}")]
    public async Task<IActionResult> DeletePlace(Guid id)
    {
        var place = await _db.Places.FirstOrDefaultAsync(x => x.Id == id);
        if (place == null) return NotFound();

        _db.Places.Remove(place);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        var userCount = await _db.Users.CountAsync();

        var superUserRoleId = await _roles.Roles
            .Where(r => r.Name == "SuperUser")
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        var superUserCount = string.IsNullOrWhiteSpace(superUserRoleId)
            ? 0
            : await _db.UserRoles.CountAsync(x => x.RoleId == superUserRoleId);

        var pointsCount = await _db.EmotionalPoints.CountAsync();

        var zone = ResolveSofiaTimeZone();
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
        var localStart = DateTime.SpecifyKind(localNow.Date, DateTimeKind.Unspecified);
        var localEnd = localStart.AddDays(1).AddTicks(-1);
        var todayStartUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, zone);
        var todayEndUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, zone);

        var requestsToday = await _db.MapRequests.CountAsync(x =>
            x.CreatedAtUtc >= todayStartUtc &&
            x.CreatedAtUtc <= todayEndUtc);

        return Ok(new
        {
            users = userCount,
            superUsers = superUserCount,
            aiRequestsToday = requestsToday,
            points = pointsCount
        });
    }

    [HttpDelete("points/{id:guid}")]
    public async Task<IActionResult> DeletePoint(Guid id)
    {
        var point = await _db.EmotionalPoints.FirstOrDefaultAsync(x => x.Id == id);
        if (point == null) return NotFound();

        _db.EmotionalPoints.Remove(point);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    public class RoleChangeRequest
    {
        public string UserId { get; set; } = "";
    }

    public class AddEmotionRequest
    {
        public EmotionType Emotion { get; set; }
        public string DisplayName { get; set; } = "";
        public string ColorHex { get; set; } = "#FF0000";
    }

    private static TimeZoneInfo ResolveSofiaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Sofia");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Local;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Local;
        }
    }
}
