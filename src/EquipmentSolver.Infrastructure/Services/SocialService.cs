using EquipmentSolver.Core.Entities;
using EquipmentSolver.Core.Interfaces;
using EquipmentSolver.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EquipmentSolver.Infrastructure.Services;

/// <summary>
/// Manages social features: visibility, browse, vote, copy, use.
/// </summary>
public class SocialService : ISocialService
{
    private readonly AppDbContext _db;
    private readonly ILogger<SocialService> _logger;

    public SocialService(AppDbContext db, ILogger<SocialService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> SetVisibilityAsync(int profileId, string userId, bool isPublic)
    {
        var profile = await _db.GameProfiles
            .FirstOrDefaultAsync(p => p.Id == profileId && p.OwnerId == userId);

        if (profile is null)
            return false;

        profile.IsPublic = isPublic;
        profile.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Profile {ProfileId} visibility set to {IsPublic} by user {UserId}",
            profileId, isPublic, userId);
        return true;
    }

    /// <inheritdoc />
    public async Task<(List<GameProfile> Profiles, int TotalCount)> BrowseAsync(
        string? search, int? igdbGameId, string sortBy, int page, int pageSize, string userId)
    {
        var query = _db.GameProfiles
            .Where(p => p.IsPublic)
            .Include(p => p.Owner)
            .Include(p => p.Slots)
            .Include(p => p.StatTypes)
            .Include(p => p.Equipment)
            .AsQueryable();

        // Filter by game
        if (igdbGameId.HasValue)
            query = query.Where(p => p.IgdbGameId == igdbGameId.Value);

        // Text search (profile name or creator username)
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(searchLower) ||
                p.Owner.UserName!.ToLower().Contains(searchLower));
        }

        var totalCount = await query.CountAsync();

        // Sort
        query = sortBy?.ToLower() switch
        {
            "votes" => query.OrderByDescending(p => p.VoteScore).ThenByDescending(p => p.UpdatedAt),
            "usage" => query.OrderByDescending(p => p.UsageCount).ThenByDescending(p => p.UpdatedAt),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            "name" => query.OrderBy(p => p.Name).ThenByDescending(p => p.UpdatedAt),
            "creator" => query.OrderBy(p => p.Owner.UserName).ThenByDescending(p => p.UpdatedAt),
            _ => query.OrderByDescending(p => p.VoteScore).ThenByDescending(p => p.UpdatedAt)
        };

        // Paginate
        var profiles = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (profiles, totalCount);
    }

    /// <inheritdoc />
    public async Task<GameProfile?> GetPublicProfileAsync(int profileId, string userId)
    {
        var profile = await _db.GameProfiles
            .Include(p => p.Owner)
            .Include(p => p.Slots.OrderBy(s => s.SortOrder))
            .Include(p => p.StatTypes)
            .Include(p => p.Equipment)
                .ThenInclude(e => e.Stats)
            .Include(p => p.Equipment)
                .ThenInclude(e => e.SlotCompatibilities)
            .Include(p => p.SolverPresets)
                .ThenInclude(sp => sp.Constraints)
            .Include(p => p.SolverPresets)
                .ThenInclude(sp => sp.Priorities)
            .Include(p => p.PatchNotes.OrderByDescending(pn => pn.Date))
            .FirstOrDefaultAsync(p => p.Id == profileId);

        if (profile is null)
            return null;

        // Allow access if public, owned by user, or user is using it
        if (profile.IsPublic || profile.OwnerId == userId)
            return profile;

        var isUsing = await _db.ProfileUsages
            .AnyAsync(u => u.UserId == userId && u.ProfileId == profileId);

        return isUsing ? profile : null;
    }

    /// <inheritdoc />
    public async Task<(bool Success, int NewScore, string? Error)> VoteAsync(int profileId, string userId, int vote)
    {
        if (vote is < -1 or > 1)
            return (false, 0, "Vote must be -1, 0, or +1.");

        var profile = await _db.GameProfiles
            .FirstOrDefaultAsync(p => p.Id == profileId && p.IsPublic);

        if (profile is null)
            return (false, 0, "Profile not found or not public.");

        if (profile.OwnerId == userId)
            return (false, profile.VoteScore, "You cannot vote on your own profile.");

        var existingVote = await _db.ProfileVotes
            .FirstOrDefaultAsync(v => v.UserId == userId && v.ProfileId == profileId);

        if (vote == 0)
        {
            // Remove vote
            if (existingVote is not null)
            {
                profile.VoteScore -= existingVote.Vote;
                _db.ProfileVotes.Remove(existingVote);
            }
        }
        else if (existingVote is null)
        {
            // New vote
            _db.ProfileVotes.Add(new ProfileVote
            {
                UserId = userId,
                ProfileId = profileId,
                Vote = vote
            });
            profile.VoteScore += vote;
        }
        else
        {
            // Change vote
            profile.VoteScore += vote - existingVote.Vote;
            existingVote.Vote = vote;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("User {UserId} voted {Vote} on profile {ProfileId}. New score: {Score}",
            userId, vote, profileId, profile.VoteScore);

        return (true, profile.VoteScore, null);
    }

    /// <inheritdoc />
    public async Task<int?> GetUserVoteAsync(int profileId, string userId)
    {
        var vote = await _db.ProfileVotes
            .FirstOrDefaultAsync(v => v.UserId == userId && v.ProfileId == profileId);
        return vote?.Vote;
    }

    /// <inheritdoc />
    public async Task<GameProfile?> CopyProfileAsync(int profileId, string userId)
    {
        var source = await _db.GameProfiles
            .Include(p => p.Slots.OrderBy(s => s.SortOrder))
            .Include(p => p.StatTypes)
            .Include(p => p.Equipment)
                .ThenInclude(e => e.Stats)
            .Include(p => p.Equipment)
                .ThenInclude(e => e.SlotCompatibilities)
            .Include(p => p.SolverPresets)
                .ThenInclude(sp => sp.Constraints)
            .Include(p => p.SolverPresets)
                .ThenInclude(sp => sp.Priorities)
            .FirstOrDefaultAsync(p => p.Id == profileId && p.IsPublic);

        if (source is null)
            return null;

        // Create new profile
        var newProfile = new GameProfile
        {
            OwnerId = userId,
            Name = $"{source.Name} (Copy)",
            GameName = source.GameName,
            IgdbGameId = source.IgdbGameId,
            GameCoverUrl = source.GameCoverUrl,
            Description = source.Description,
            Version = "0.1.0",
            IsPublic = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.GameProfiles.Add(newProfile);
        await _db.SaveChangesAsync(); // Get new profile ID

        // Map old IDs to new entities
        var slotMap = new Dictionary<int, EquipmentSlot>();
        var statTypeMap = new Dictionary<int, StatType>();

        // Copy slots
        foreach (var slot in source.Slots)
        {
            var newSlot = new EquipmentSlot
            {
                ProfileId = newProfile.Id,
                Name = slot.Name,
                SortOrder = slot.SortOrder
            };
            _db.EquipmentSlots.Add(newSlot);
            slotMap[slot.Id] = newSlot;
        }
        await _db.SaveChangesAsync(); // Get slot IDs

        // Copy stat types
        foreach (var statType in source.StatTypes)
        {
            var newStatType = new StatType
            {
                ProfileId = newProfile.Id,
                DisplayName = statType.DisplayName
            };
            _db.StatTypes.Add(newStatType);
            statTypeMap[statType.Id] = newStatType;
        }
        await _db.SaveChangesAsync(); // Get stat type IDs

        // Copy equipment
        foreach (var equip in source.Equipment)
        {
            var newEquip = new Equipment
            {
                ProfileId = newProfile.Id,
                Name = equip.Name
            };
            _db.Equipment.Add(newEquip);
            await _db.SaveChangesAsync(); // Get equipment ID

            // Copy slot compatibilities
            foreach (var sc in equip.SlotCompatibilities)
            {
                if (slotMap.TryGetValue(sc.SlotId, out var newSlot))
                {
                    _db.EquipmentSlotCompatibilities.Add(new EquipmentSlotCompatibility
                    {
                        EquipmentId = newEquip.Id,
                        SlotId = newSlot.Id
                    });
                }
            }

            // Copy stats
            foreach (var stat in equip.Stats)
            {
                if (statTypeMap.TryGetValue(stat.StatTypeId, out var newStatType))
                {
                    _db.EquipmentStats.Add(new EquipmentStat
                    {
                        EquipmentId = newEquip.Id,
                        StatTypeId = newStatType.Id,
                        Value = stat.Value
                    });
                }
            }
        }

        // Copy solver presets
        foreach (var preset in source.SolverPresets)
        {
            var newPreset = new SolverPreset
            {
                ProfileId = newProfile.Id,
                Name = preset.Name
            };
            _db.SolverPresets.Add(newPreset);
            await _db.SaveChangesAsync(); // Get preset ID

            foreach (var constraint in preset.Constraints)
            {
                if (statTypeMap.TryGetValue(constraint.StatTypeId, out var newStatType))
                {
                    _db.SolverConstraints.Add(new SolverConstraint
                    {
                        PresetId = newPreset.Id,
                        StatTypeId = newStatType.Id,
                        Operator = constraint.Operator,
                        Value = constraint.Value
                    });
                }
            }

            foreach (var priority in preset.Priorities)
            {
                if (statTypeMap.TryGetValue(priority.StatTypeId, out var newStatType))
                {
                    _db.SolverPriorities.Add(new SolverPriority
                    {
                        PresetId = newPreset.Id,
                        StatTypeId = newStatType.Id,
                        Weight = priority.Weight
                    });
                }
            }
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("User {UserId} copied profile {SourceId} to new profile {NewId}",
            userId, profileId, newProfile.Id);

        return newProfile;
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? Error)> StartUsingAsync(int profileId, string userId)
    {
        var profile = await _db.GameProfiles
            .FirstOrDefaultAsync(p => p.Id == profileId && p.IsPublic);

        if (profile is null)
            return (false, "Profile not found or not public.");

        if (profile.OwnerId == userId)
            return (false, "You already own this profile.");

        var existing = await _db.ProfileUsages
            .AnyAsync(u => u.UserId == userId && u.ProfileId == profileId);

        if (existing)
            return (false, "You are already using this profile.");

        _db.ProfileUsages.Add(new ProfileUsage
        {
            UserId = userId,
            ProfileId = profileId,
            StartedAt = DateTime.UtcNow
        });

        profile.UsageCount++;
        await _db.SaveChangesAsync();

        _logger.LogInformation("User {UserId} started using profile {ProfileId}", userId, profileId);
        return (true, null);
    }

    /// <inheritdoc />
    public async Task<bool> StopUsingAsync(int profileId, string userId)
    {
        var usage = await _db.ProfileUsages
            .FirstOrDefaultAsync(u => u.UserId == userId && u.ProfileId == profileId);

        if (usage is null)
            return false;

        _db.ProfileUsages.Remove(usage);

        var profile = await _db.GameProfiles.FindAsync(profileId);
        if (profile is not null)
        {
            profile.UsageCount = Math.Max(0, profile.UsageCount - 1);

            // Clean up user state for this profile
            var equipStates = await _db.UserEquipmentStates
                .Where(s => s.UserId == userId && s.Equipment.ProfileId == profileId)
                .ToListAsync();
            _db.UserEquipmentStates.RemoveRange(equipStates);

            var slotStates = await _db.UserSlotStates
                .Where(s => s.UserId == userId && s.Slot.ProfileId == profileId)
                .ToListAsync();
            _db.UserSlotStates.RemoveRange(slotStates);
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("User {UserId} stopped using profile {ProfileId}", userId, profileId);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> IsUsingAsync(int profileId, string userId)
    {
        return await _db.ProfileUsages
            .AnyAsync(u => u.UserId == userId && u.ProfileId == profileId);
    }
}
