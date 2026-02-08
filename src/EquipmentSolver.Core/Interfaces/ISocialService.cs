using EquipmentSolver.Core.Entities;

namespace EquipmentSolver.Core.Interfaces;

/// <summary>
/// Manages social features: visibility, browse, vote, copy, use.
/// </summary>
public interface ISocialService
{
    /// <summary>
    /// Toggle profile visibility (public/private). Only the owner can do this.
    /// </summary>
    Task<bool> SetVisibilityAsync(int profileId, string userId, bool isPublic);

    /// <summary>
    /// Browse/search public profiles with filtering, sorting, and pagination.
    /// </summary>
    Task<(List<GameProfile> Profiles, int TotalCount)> BrowseAsync(
        string? search, int? igdbGameId, string sortBy, int page, int pageSize, string userId);

    /// <summary>
    /// Get public profile detail for viewing. Returns null if not found or not accessible.
    /// </summary>
    Task<GameProfile?> GetPublicProfileAsync(int profileId, string userId);

    /// <summary>
    /// Vote on a public profile. Vote is +1 (upvote), -1 (downvote), or 0 (remove vote).
    /// </summary>
    Task<(bool Success, int NewScore, string? Error)> VoteAsync(int profileId, string userId, int vote);

    /// <summary>
    /// Get the current user's vote on a profile (null if not voted).
    /// </summary>
    Task<int?> GetUserVoteAsync(int profileId, string userId);

    /// <summary>
    /// Copy a public profile to the user's own account (deep clone, unlinked).
    /// </summary>
    Task<GameProfile?> CopyProfileAsync(int profileId, string userId);

    /// <summary>
    /// Start using a public profile (linked read-only).
    /// </summary>
    Task<(bool Success, string? Error)> StartUsingAsync(int profileId, string userId);

    /// <summary>
    /// Stop using a public profile.
    /// </summary>
    Task<bool> StopUsingAsync(int profileId, string userId);

    /// <summary>
    /// Check if the user is currently using a profile.
    /// </summary>
    Task<bool> IsUsingAsync(int profileId, string userId);
}
