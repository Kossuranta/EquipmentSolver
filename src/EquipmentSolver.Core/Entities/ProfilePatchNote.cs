namespace EquipmentSolver.Core.Entities;

/// <summary>
/// A patch note entry tied to a profile version change.
/// </summary>
public class ProfilePatchNote
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public GameProfile Profile { get; set; } = null!;

    /// <summary>
    /// Version string at time of the note (e.g., "1.2.0").
    /// </summary>
    public string Version { get; set; } = null!;

    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string Content { get; set; } = null!;
}
