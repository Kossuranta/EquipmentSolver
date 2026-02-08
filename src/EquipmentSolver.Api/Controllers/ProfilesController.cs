using System.Security.Claims;
using EquipmentSolver.Api.DTOs;
using EquipmentSolver.Api.DTOs.Profiles;
using EquipmentSolver.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentSolver.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ProfilesController : ControllerBase
{
    private readonly IGameProfileService _profileService;

    public ProfilesController(IGameProfileService profileService)
    {
        _profileService = profileService;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>
    /// Get all profiles owned by or used by the current user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ProfileResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyProfiles()
    {
        var profiles = await _profileService.GetUserProfilesAsync(UserId);

        var response = profiles.Select(p => new ProfileResponse
        {
            Id = p.Id,
            GameName = p.GameName,
            IgdbGameId = p.IgdbGameId,
            GameCoverUrl = p.GameCoverUrl,
            Description = p.Description,
            Version = p.Version,
            IsPublic = p.IsPublic,
            VoteScore = p.VoteScore,
            UsageCount = p.UsageCount,
            IsOwner = p.OwnerId == UserId,
            OwnerName = p.Owner?.UserName ?? "You",
            SlotCount = p.Slots.Count,
            StatTypeCount = p.StatTypes.Count,
            EquipmentCount = p.Equipment.Count,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        }).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Get a single profile with full detail (slots, stat types, equipment).
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProfileDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(int id)
    {
        var profile = await _profileService.GetProfileAsync(id, UserId);
        if (profile is null)
            return NotFound();

        var response = new ProfileDetailResponse
        {
            Id = profile.Id,
            GameName = profile.GameName,
            IgdbGameId = profile.IgdbGameId,
            GameCoverUrl = profile.GameCoverUrl,
            Description = profile.Description,
            Version = profile.Version,
            IsPublic = profile.IsPublic,
            VoteScore = profile.VoteScore,
            UsageCount = profile.UsageCount,
            IsOwner = profile.OwnerId == UserId,
            OwnerName = profile.Owner?.UserName ?? "Unknown",
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt,
            Slots = profile.Slots.Select(s => new SlotDto
            {
                Id = s.Id,
                Name = s.Name,
                SortOrder = s.SortOrder
            }).ToList(),
            StatTypes = profile.StatTypes.Select(st => new StatTypeDto
            {
                Id = st.Id,
                Name = st.Name,
                DisplayName = st.DisplayName
            }).ToList(),
            Equipment = profile.Equipment.Select(e => new EquipmentDto
            {
                Id = e.Id,
                Name = e.Name,
                CompatibleSlotIds = e.SlotCompatibilities.Select(sc => sc.SlotId).ToList(),
                Stats = e.Stats.Select(es => new EquipmentStatDto
                {
                    StatTypeId = es.StatTypeId,
                    Value = es.Value
                }).ToList()
            }).ToList()
        };

        return Ok(response);
    }

    /// <summary>
    /// Create a new game profile.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProfile([FromBody] CreateProfileRequest request)
    {
        var profile = await _profileService.CreateProfileAsync(
            UserId, request.GameName, request.IgdbGameId, request.GameCoverUrl, request.Description);

        var response = new ProfileResponse
        {
            Id = profile.Id,
            GameName = profile.GameName,
            IgdbGameId = profile.IgdbGameId,
            GameCoverUrl = profile.GameCoverUrl,
            Description = profile.Description,
            Version = profile.Version,
            IsPublic = profile.IsPublic,
            VoteScore = profile.VoteScore,
            UsageCount = profile.UsageCount,
            IsOwner = true,
            OwnerName = "You",
            SlotCount = 0,
            StatTypeCount = 0,
            EquipmentCount = 0,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };

        return CreatedAtAction(nameof(GetProfile), new { id = profile.Id }, response);
    }

    /// <summary>
    /// Update profile metadata. Only the owner can update.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateProfileRequest request)
    {
        if (!await _profileService.IsOwnerAsync(id, UserId))
            return Forbid();

        var profile = await _profileService.UpdateProfileAsync(
            id, UserId, request.GameName, request.IgdbGameId, request.GameCoverUrl, request.Description);

        if (profile is null)
            return NotFound();

        var response = new ProfileResponse
        {
            Id = profile.Id,
            GameName = profile.GameName,
            IgdbGameId = profile.IgdbGameId,
            GameCoverUrl = profile.GameCoverUrl,
            Description = profile.Description,
            Version = profile.Version,
            IsPublic = profile.IsPublic,
            VoteScore = profile.VoteScore,
            UsageCount = profile.UsageCount,
            IsOwner = true,
            OwnerName = "You",
            SlotCount = 0,
            StatTypeCount = 0,
            EquipmentCount = 0,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Delete a profile. Only the owner can delete.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteProfile(int id)
    {
        if (!await _profileService.IsOwnerAsync(id, UserId))
            return Forbid();

        var deleted = await _profileService.DeleteProfileAsync(id, UserId);
        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
