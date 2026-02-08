namespace EquipmentSolver.Api.DTOs.Social;

/// <summary>
/// Paginated response for browsing public profiles.
/// </summary>
public class BrowseProfilesResponse
{
    public List<BrowseProfileItem> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// A public profile card shown in browse results.
/// </summary>
public class BrowseProfileItem
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string GameName { get; set; } = null!;
    public int IgdbGameId { get; set; }
    public string? GameCoverUrl { get; set; }
    public string? Description { get; set; }
    public string Version { get; set; } = null!;
    public int VoteScore { get; set; }
    public int UsageCount { get; set; }
    public string OwnerName { get; set; } = null!;
    public int SlotCount { get; set; }
    public int StatTypeCount { get; set; }
    public int EquipmentCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Current user's vote on this profile: +1, -1, or null (not voted).
    /// </summary>
    public int? UserVote { get; set; }

    /// <summary>
    /// Whether the current user is actively using this profile.
    /// </summary>
    public bool IsUsing { get; set; }

    /// <summary>
    /// Whether the current user owns this profile.
    /// </summary>
    public bool IsOwner { get; set; }
}
