using System.Security.Claims;
using EquipmentSolver.Api.DTOs;
using EquipmentSolver.Api.DTOs.Profiles;
using EquipmentSolver.Core.Entities;
using EquipmentSolver.Core.Interfaces;
using EquipmentSolver.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EquipmentSolver.Api.Controllers;

[ApiController]
[Route("api/v1/profiles/{profileId:int}/stat-types")]
[Authorize]
public class StatTypesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IGameProfileService _profileService;

    public StatTypesController(AppDbContext db, IGameProfileService profileService)
    {
        _db = db;
        _profileService = profileService;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>
    /// Get all stat types for a profile.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<StatTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatTypes(int profileId)
    {
        var profile = await _profileService.GetProfileAsync(profileId, UserId);
        if (profile is null)
            return NotFound();

        var statTypes = await _db.StatTypes
            .Where(st => st.ProfileId == profileId)
            .OrderBy(st => st.DisplayName)
            .Select(st => new StatTypeDto { Id = st.Id, Name = st.Name, DisplayName = st.DisplayName })
            .ToListAsync();

        return Ok(statTypes);
    }

    /// <summary>
    /// Add a new stat type to a profile.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(StatTypeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateStatType(int profileId, [FromBody] CreateStatTypeRequest request)
    {
        if (!await _profileService.IsOwnerAsync(profileId, UserId))
            return Forbid();

        var exists = await _db.StatTypes
            .AnyAsync(st => st.ProfileId == profileId && st.Name == request.Name);
        if (exists)
            return BadRequest(new ErrorResponse("A stat type with this name already exists in the profile."));

        var statType = new StatType
        {
            ProfileId = profileId,
            Name = request.Name,
            DisplayName = request.DisplayName
        };

        _db.StatTypes.Add(statType);
        await _db.SaveChangesAsync();
        await TouchProfile(profileId);

        var dto = new StatTypeDto { Id = statType.Id, Name = statType.Name, DisplayName = statType.DisplayName };
        return Created($"api/v1/profiles/{profileId}/stat-types/{statType.Id}", dto);
    }

    /// <summary>
    /// Update a stat type.
    /// </summary>
    [HttpPut("{statTypeId:int}")]
    [ProducesResponseType(typeof(StatTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatType(int profileId, int statTypeId, [FromBody] UpdateStatTypeRequest request)
    {
        if (!await _profileService.IsOwnerAsync(profileId, UserId))
            return Forbid();

        var statType = await _db.StatTypes
            .FirstOrDefaultAsync(st => st.Id == statTypeId && st.ProfileId == profileId);
        if (statType is null)
            return NotFound();

        // Check for duplicate name (excluding self)
        var exists = await _db.StatTypes
            .AnyAsync(st => st.ProfileId == profileId && st.Name == request.Name && st.Id != statTypeId);
        if (exists)
            return BadRequest(new ErrorResponse("A stat type with this name already exists in the profile."));

        statType.Name = request.Name;
        statType.DisplayName = request.DisplayName;
        await _db.SaveChangesAsync();
        await TouchProfile(profileId);

        return Ok(new StatTypeDto { Id = statType.Id, Name = statType.Name, DisplayName = statType.DisplayName });
    }

    /// <summary>
    /// Delete a stat type (cascades to equipment stats, solver constraints/priorities).
    /// </summary>
    [HttpDelete("{statTypeId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStatType(int profileId, int statTypeId)
    {
        if (!await _profileService.IsOwnerAsync(profileId, UserId))
            return Forbid();

        var statType = await _db.StatTypes
            .FirstOrDefaultAsync(st => st.Id == statTypeId && st.ProfileId == profileId);
        if (statType is null)
            return NotFound();

        _db.StatTypes.Remove(statType);
        await _db.SaveChangesAsync();
        await TouchProfile(profileId);

        return NoContent();
    }

    private async Task TouchProfile(int profileId)
    {
        var profile = await _db.GameProfiles.FindAsync(profileId);
        if (profile is not null)
        {
            profile.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}
