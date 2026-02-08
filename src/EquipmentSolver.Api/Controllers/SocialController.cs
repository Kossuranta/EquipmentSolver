using System.Security.Claims;
using EquipmentSolver.Api.DTOs;
using EquipmentSolver.Api.DTOs.Profiles;
using EquipmentSolver.Api.DTOs.Social;
using EquipmentSolver.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EquipmentSolver.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
[EnableRateLimiting("api")]
public class SocialController : ControllerBase
{
    private readonly ISocialService _socialService;

    public SocialController(ISocialService socialService)
    {
        _socialService = socialService;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>
    /// Set profile visibility (public/private). Only the owner can do this.
    /// </summary>
    [HttpPut("profiles/{id:int}/visibility")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetVisibility(int id, [FromBody] SetVisibilityRequest request)
    {
        var result = await _socialService.SetVisibilityAsync(id, UserId, request.IsPublic);
        return result ? NoContent() : NotFound();
    }

    /// <summary>
    /// Browse/search public profiles with filtering, sorting, and pagination.
    /// </summary>
    [HttpGet("browse")]
    [ProducesResponseType(typeof(BrowseProfilesResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Browse(
        [FromQuery] string? search,
        [FromQuery] int? gameId,
        [FromQuery] string sort = "votes",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 50) pageSize = 20;

        var (profiles, totalCount) = await _socialService.BrowseAsync(
            search, gameId, sort, page, pageSize, UserId);

        // Get user votes and usage for all returned profiles
        var profileIds = profiles.Select(p => p.Id).ToList();
        var userVotes = new Dictionary<int, int>();
        var userUsages = new HashSet<int>();

        foreach (var profile in profiles)
        {
            var vote = await _socialService.GetUserVoteAsync(profile.Id, UserId);
            if (vote.HasValue)
                userVotes[profile.Id] = vote.Value;

            if (await _socialService.IsUsingAsync(profile.Id, UserId))
                userUsages.Add(profile.Id);
        }

        var response = new BrowseProfilesResponse
        {
            Items = profiles.Select(p => new BrowseProfileItem
            {
                Id = p.Id,
                Name = p.Name,
                GameName = p.GameName,
                IgdbGameId = p.IgdbGameId,
                GameCoverUrl = p.GameCoverUrl,
                Description = p.Description,
                Version = p.Version,
                VoteScore = p.VoteScore,
                UsageCount = p.UsageCount,
                OwnerName = p.Owner?.UserName ?? "Unknown",
                SlotCount = p.Slots.Count,
                StatTypeCount = p.StatTypes.Count,
                EquipmentCount = p.Equipment.Count,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                UserVote = userVotes.GetValueOrDefault(p.Id, 0) == 0 ? null : userVotes[p.Id],
                IsUsing = userUsages.Contains(p.Id),
                IsOwner = p.OwnerId == UserId
            }).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };

        return Ok(response);
    }

    /// <summary>
    /// Get public profile detail.
    /// </summary>
    [HttpGet("browse/{id:int}")]
    [ProducesResponseType(typeof(PublicProfileDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublicProfile(int id)
    {
        var profile = await _socialService.GetPublicProfileAsync(id, UserId);
        if (profile is null)
            return NotFound();

        var userVote = await _socialService.GetUserVoteAsync(id, UserId);
        var isUsing = await _socialService.IsUsingAsync(id, UserId);

        var response = new PublicProfileDetailResponse
        {
            Id = profile.Id,
            Name = profile.Name,
            GameName = profile.GameName,
            IgdbGameId = profile.IgdbGameId,
            GameCoverUrl = profile.GameCoverUrl,
            Description = profile.Description,
            Version = profile.Version,
            VoteScore = profile.VoteScore,
            UsageCount = profile.UsageCount,
            OwnerName = profile.Owner?.UserName ?? "Unknown",
            IsOwner = profile.OwnerId == UserId,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt,
            UserVote = userVote,
            IsUsing = isUsing,
            Slots = profile.Slots.Select(s => new SlotDto
            {
                Id = s.Id,
                Name = s.Name,
                SortOrder = s.SortOrder
            }).ToList(),
            StatTypes = profile.StatTypes.Select(st => new StatTypeDto
            {
                Id = st.Id,
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
            }).ToList(),
            SolverPresets = profile.SolverPresets.Select(sp => new SolverPresetDto
            {
                Id = sp.Id,
                Name = sp.Name,
                Constraints = sp.Constraints.Select(c => new SolverPresetConstraintDto
                {
                    StatTypeId = c.StatTypeId,
                    Operator = c.Operator,
                    Value = c.Value
                }).ToList(),
                Priorities = sp.Priorities.Select(p => new SolverPresetPriorityDto
                {
                    StatTypeId = p.StatTypeId,
                    Weight = p.Weight
                }).ToList()
            }).ToList(),
            PatchNotes = profile.PatchNotes.Select(pn => new PatchNoteDto
            {
                Id = pn.Id,
                Version = pn.Version,
                Date = pn.Date,
                Content = pn.Content
            }).ToList()
        };

        return Ok(response);
    }

    /// <summary>
    /// Vote on a public profile (+1 upvote, -1 downvote, 0 remove vote).
    /// </summary>
    [HttpPost("browse/{id:int}/vote")]
    [ProducesResponseType(typeof(VoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Vote(int id, [FromBody] VoteRequest request)
    {
        var (success, newScore, error) = await _socialService.VoteAsync(id, UserId, request.Vote);

        if (!success)
            return BadRequest(new ErrorResponse { Errors = [error!] });

        return Ok(new VoteResponse { NewScore = newScore, UserVote = request.Vote });
    }

    /// <summary>
    /// Copy a public profile to the current user's account (deep clone).
    /// </summary>
    [HttpPost("browse/{id:int}/copy")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CopyProfile(int id)
    {
        var newProfile = await _socialService.CopyProfileAsync(id, UserId);
        if (newProfile is null)
            return NotFound();

        return StatusCode(StatusCodes.Status201Created, new { id = newProfile.Id, name = newProfile.Name });
    }

    /// <summary>
    /// Start using a public profile (linked, read-only).
    /// </summary>
    [HttpPost("browse/{id:int}/use")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartUsing(int id)
    {
        var (success, error) = await _socialService.StartUsingAsync(id, UserId);

        if (!success)
            return BadRequest(new ErrorResponse { Errors = [error!] });

        return NoContent();
    }

    /// <summary>
    /// Stop using a public profile.
    /// </summary>
    [HttpDelete("browse/{id:int}/use")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StopUsing(int id)
    {
        var result = await _socialService.StopUsingAsync(id, UserId);
        return result ? NoContent() : NotFound();
    }
}
