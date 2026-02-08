using System.ComponentModel.DataAnnotations;

namespace EquipmentSolver.Api.DTOs.Profiles;

public class CreateProfileRequest
{
    [Required]
    [StringLength(200)]
    public string GameName { get; set; } = null!;

    [Required]
    public int IgdbGameId { get; set; }

    public string? GameCoverUrl { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}
