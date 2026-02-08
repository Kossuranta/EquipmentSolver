using System.ComponentModel.DataAnnotations;

namespace EquipmentSolver.Api.DTOs.Profiles;

public class CreatePatchNoteRequest
{
    /// <summary>
    /// New version in major.minor.patch format (e.g., "1.0.0").
    /// </summary>
    [Required]
    [RegularExpression(@"^\d{1,3}\.\d{1,3}\.\d{1,3}$", ErrorMessage = "Version must be in major.minor.patch format (0-999 each).")]
    public string Version { get; set; } = null!;

    /// <summary>
    /// Description of what changed.
    /// </summary>
    [Required]
    [StringLength(5000)]
    public string Content { get; set; } = null!;
}

public class PatchNoteResponse
{
    public int Id { get; set; }
    public string Version { get; set; } = null!;
    public DateTime Date { get; set; }
    public string Content { get; set; } = null!;
}
