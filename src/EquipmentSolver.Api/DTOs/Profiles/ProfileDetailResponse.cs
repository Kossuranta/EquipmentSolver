namespace EquipmentSolver.Api.DTOs.Profiles;

/// <summary>
/// Full profile detail including slots, stat types, and equipment.
/// </summary>
public class ProfileDetailResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string GameName { get; set; } = null!;
    public int IgdbGameId { get; set; }
    public string? GameCoverUrl { get; set; }
    public string? Description { get; set; }
    public string Version { get; set; } = null!;
    public bool IsPublic { get; set; }
    public int VoteScore { get; set; }
    public int UsageCount { get; set; }
    public bool IsOwner { get; set; }
    public string OwnerName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<SlotDto> Slots { get; set; } = [];
    public List<StatTypeDto> StatTypes { get; set; } = [];
    public List<EquipmentDto> Equipment { get; set; } = [];
}

public class SlotDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int SortOrder { get; set; }
}

public class StatTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
}

public class EquipmentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public List<int> CompatibleSlotIds { get; set; } = [];
    public List<EquipmentStatDto> Stats { get; set; } = [];
}

public class EquipmentStatDto
{
    public int StatTypeId { get; set; }
    public double Value { get; set; }
}
