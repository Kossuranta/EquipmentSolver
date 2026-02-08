using EquipmentSolver.Core.Entities;

namespace EquipmentSolver.Core.Interfaces;

/// <summary>
/// Manages game profile CRUD operations.
/// </summary>
public interface IGameProfileService
{
    /// <summary>
    /// Get all profiles owned by or used by a user.
    /// </summary>
    Task<List<GameProfile>> GetUserProfilesAsync(string userId);

    /// <summary>
    /// Get a single profile by ID. Returns null if not found or not accessible.
    /// </summary>
    Task<GameProfile?> GetProfileAsync(int profileId, string userId);

    /// <summary>
    /// Create a new game profile.
    /// </summary>
    Task<GameProfile> CreateProfileAsync(string userId, string name, string gameName, int igdbGameId, string? gameCoverUrl, string? description);

    /// <summary>
    /// Update profile metadata (name, description, game info). Only the owner can update.
    /// </summary>
    Task<GameProfile?> UpdateProfileAsync(int profileId, string userId, string name, string gameName, int igdbGameId, string? gameCoverUrl, string? description);

    /// <summary>
    /// Delete a profile. Only the owner can delete.
    /// </summary>
    Task<bool> DeleteProfileAsync(int profileId, string userId);

    /// <summary>
    /// Check if the user is the owner of a profile.
    /// </summary>
    Task<bool> IsOwnerAsync(int profileId, string userId);
}
