using EquipmentSolver.Api.DTOs.Profiles;

namespace EquipmentSolver.Api.DTOs.Social;

/// <summary>
/// Full public profile detail with social metadata.
/// </summary>
public class PublicProfileDetailResponse
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
    public bool IsOwner { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Current user's vote: +1, -1, or null.
    /// </summary>
    public int? UserVote { get; set; }

    /// <summary>
    /// Whether the current user is using this profile.
    /// </summary>
    public bool IsUsing { get; set; }

    public List<SlotDto> Slots { get; set; } = [];
    public List<StatTypeDto> StatTypes { get; set; } = [];
    public List<EquipmentDto> Equipment { get; set; } = [];
    public List<SolverPresetDto> SolverPresets { get; set; } = [];
    public List<PatchNoteDto> PatchNotes { get; set; } = [];
}

public class SolverPresetDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public List<SolverPresetConstraintDto> Constraints { get; set; } = [];
    public List<SolverPresetPriorityDto> Priorities { get; set; } = [];
}

public class SolverPresetConstraintDto
{
    public int StatTypeId { get; set; }
    public string Operator { get; set; } = null!;
    public double Value { get; set; }
}

public class SolverPresetPriorityDto
{
    public int StatTypeId { get; set; }
    public double Weight { get; set; }
}

public class PatchNoteDto
{
    public int Id { get; set; }
    public string Version { get; set; } = null!;
    public DateTime Date { get; set; }
    public string Content { get; set; } = null!;
}
