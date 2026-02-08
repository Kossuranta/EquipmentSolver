using System.ComponentModel.DataAnnotations;

namespace EquipmentSolver.Api.DTOs.Profiles;

public class UpdateProfileRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string GameName { get; set; } = null!;

    [Required]
    public int IgdbGameId { get; set; }

    public string? GameCoverUrl { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}
