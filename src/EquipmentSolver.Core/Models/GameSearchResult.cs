namespace EquipmentSolver.Core.Models;

/// <summary>
/// A game returned from IGDB search.
/// </summary>
public class GameSearchResult
{
    public int IgdbId { get; set; }
    public string Name { get; set; } = null!;
    public string? CoverUrl { get; set; }
}
