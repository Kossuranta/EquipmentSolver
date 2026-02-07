using System.ComponentModel.DataAnnotations;

namespace EquipmentSolver.Api.DTOs.Auth;

public class LoginRequest
{
    [Required]
    public string Username { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}
