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
    public IActionResult GetUsers()
        => Ok(_users.Users.Select(u => new
        {
            u.Id,
            u.Email,
            u.DisplayName,
            u.CreatedAtUtc
        }));

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

        return Ok(new { message = "SuperUser даден." });
    }

    [HttpPost("revoke-superuser")]
    public async Task<IActionResult> RevokeSuperUser([FromBody] RoleChangeRequest req)
    {
        var user = await _users.FindByIdAsync(req.UserId);
        if (user == null) return NotFound();

        if (await _users.IsInRoleAsync(user, "SuperUser"))
            await _users.RemoveFromRoleAsync(user, "SuperUser");

        return Ok(new { message = "SuperUser махнат." });
    }

    [HttpGet("emotions")]
    public async Task<IActionResult> GetEmotions()
        => Ok(await _db.EmotionCatalog.OrderBy(x => x.DisplayName).ToListAsync());

    [HttpPost("emotions")]
    public async Task<IActionResult> AddEmotion([FromBody] AddEmotionRequest req)
    {
        var item = new EmotionCatalogItem(req.Emotion, req.DisplayName, req.ColorHex, true);
        _db.EmotionCatalog.Add(item);
        await _db.SaveChangesAsync();
        return Ok(item);
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
}