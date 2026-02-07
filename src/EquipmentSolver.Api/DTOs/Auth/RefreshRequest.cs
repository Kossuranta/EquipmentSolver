using System.ComponentModel.DataAnnotations;

namespace EquipmentSolver.Api.DTOs.Auth;

public class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = null!;
}
