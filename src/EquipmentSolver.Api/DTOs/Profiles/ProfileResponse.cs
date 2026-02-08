namespace EquipmentSolver.Api.DTOs.Profiles;

public class ProfileResponse
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
    public int SlotCount { get; set; }
    public int StatTypeCount { get; set; }
    public int EquipmentCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
