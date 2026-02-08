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
[Route("api/v1/profiles/{profileId:int}/patch-notes")]
[Authorize]
public class PatchNotesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IGameProfileService _profileService;

    public PatchNotesController(AppDbContext db, IGameProfileService profileService)
    {
        _db = db;
        _profileService = profileService;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>
    /// Get all patch notes for a profile (newest first).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<PatchNoteResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPatchNotes(int profileId)
    {
        var profile = await _profileService.GetProfileAsync(profileId, UserId);
        if (profile is null)
            return NotFound();

        var notes = await _db.ProfilePatchNotes
            .Where(n => n.ProfileId == profileId)
            .OrderByDescending(n => n.Date)
            .Select(n => new PatchNoteResponse
            {
                Id = n.Id,
                Version = n.Version,
                Date = n.Date,
                Content = n.Content
            })
            .ToListAsync();

        return Ok(notes);
    }

    /// <summary>
    /// Create a patch note and bump the profile version.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PatchNoteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePatchNote(int profileId, [FromBody] CreatePatchNoteRequest request)
    {
        if (!await _profileService.IsOwnerAsync(profileId, UserId))
            return Forbid();

        // Validate version components are 0-999
        var parts = request.Version.Split('.');
        foreach (var part in parts)
        {
            if (int.TryParse(part, out var num) && (num < 0 || num > 999))
                return BadRequest(new ErrorResponse("Each version component must be between 0 and 999."));
        }

        var profile = await _db.GameProfiles.FindAsync(profileId);
        if (profile is null)
            return NotFound();

        // Update profile version
        profile.Version = request.Version;
        profile.UpdatedAt = DateTime.UtcNow;

        var note = new ProfilePatchNote
        {
            ProfileId = profileId,
            Version = request.Version,
            Date = DateTime.UtcNow,
            Content = request.Content
        };

        _db.ProfilePatchNotes.Add(note);
        await _db.SaveChangesAsync();

        var response = new PatchNoteResponse
        {
            Id = note.Id,
            Version = note.Version,
            Date = note.Date,
            Content = note.Content
        };

        return Created($"api/v1/profiles/{profileId}/patch-notes/{note.Id}", response);
    }

    /// <summary>
    /// Delete a patch note. Only the owner can delete.
    /// </summary>
    [HttpDelete("{noteId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePatchNote(int profileId, int noteId)
    {
        if (!await _profileService.IsOwnerAsync(profileId, UserId))
            return Forbid();

        var note = await _db.ProfilePatchNotes
            .FirstOrDefaultAsync(n => n.Id == noteId && n.ProfileId == profileId);
        if (note is null)
            return NotFound();

        _db.ProfilePatchNotes.Remove(note);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
