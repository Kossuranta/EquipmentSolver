using EquipmentSolver.Core.Entities;
using EquipmentSolver.Core.Interfaces;
using EquipmentSolver.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EquipmentSolver.Infrastructure.Services;

/// <summary>
/// Manages game profile CRUD operations.
/// </summary>
public class GameProfileService : IGameProfileService
{
    private readonly AppDbContext _db;
    private readonly ILogger<GameProfileService> _logger;

    public GameProfileService(AppDbContext db, ILogger<GameProfileService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<GameProfile>> GetUserProfilesAsync(string userId)
    {
        // Owned profiles
        var owned = await _db.GameProfiles
            .Where(p => p.OwnerId == userId)
            .Include(p => p.Slots)
            .Include(p => p.StatTypes)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync();

        // Profiles the user is "using" (linked read-only)
        var usedProfileIds = await _db.ProfileUsages
            .Where(u => u.UserId == userId)
            .Select(u => u.ProfileId)
            .ToListAsync();

        var used = usedProfileIds.Count > 0
            ? await _db.GameProfiles
                .Where(p => usedProfileIds.Contains(p.Id))
                .Include(p => p.Slots)
                .Include(p => p.StatTypes)
                .Include(p => p.Owner)
                .OrderByDescending(p => p.UpdatedAt)
                .ToListAsync()
            : [];

        return [.. owned, .. used];
    }

    /// <inheritdoc />
    public async Task<GameProfile?> GetProfileAsync(int profileId, string userId)
    {
        var profile = await _db.GameProfiles
            .Include(p => p.Slots.OrderBy(s => s.SortOrder))
            .Include(p => p.StatTypes)
            .Include(p => p.Equipment)
                .ThenInclude(e => e.Stats)
            .Include(p => p.Equipment)
                .ThenInclude(e => e.SlotCompatibilities)
            .Include(p => p.Owner)
            .FirstOrDefaultAsync(p => p.Id == profileId);

        if (profile is null)
            return null;

        // Allow access if user owns it or profile is public or user is "using" it
        if (profile.OwnerId == userId || profile.IsPublic)
            return profile;

        var isUsing = await _db.ProfileUsages
            .AnyAsync(u => u.UserId == userId && u.ProfileId == profileId);

        return isUsing ? profile : null;
    }

    /// <inheritdoc />
    public async Task<GameProfile> CreateProfileAsync(
        string userId, string name, string gameName, int igdbGameId, string? gameCoverUrl, string? description)
    {
        var profile = new GameProfile
        {
            OwnerId = userId,
            Name = name,
            GameName = gameName,
            IgdbGameId = igdbGameId,
            GameCoverUrl = gameCoverUrl,
            Description = description,
            Version = "0.1.0",
            IsPublic = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.GameProfiles.Add(profile);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Profile {ProfileId} '{ProfileName}' created by user {UserId} for game {Game}",
            profile.Id, name, userId, gameName);

        return profile;
    }

    /// <inheritdoc />
    public async Task<GameProfile?> UpdateProfileAsync(
        int profileId, string userId, string name, string gameName, int igdbGameId, string? gameCoverUrl, string? description)
    {
        var profile = await _db.GameProfiles
            .FirstOrDefaultAsync(p => p.Id == profileId && p.OwnerId == userId);

        if (profile is null)
            return null;

        profile.Name = name;
        profile.GameName = gameName;
        profile.IgdbGameId = igdbGameId;
        profile.GameCoverUrl = gameCoverUrl;
        profile.Description = description;
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Profile {ProfileId} updated by user {UserId}", profileId, userId);
        return profile;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteProfileAsync(int profileId, string userId)
    {
        var profile = await _db.GameProfiles
            .FirstOrDefaultAsync(p => p.Id == profileId && p.OwnerId == userId);

        if (profile is null)
            return false;

        _db.GameProfiles.Remove(profile);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Profile {ProfileId} deleted by user {UserId}", profileId, userId);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> IsOwnerAsync(int profileId, string userId)
    {
        return await _db.GameProfiles.AnyAsync(p => p.Id == profileId && p.OwnerId == userId);
    }
}
