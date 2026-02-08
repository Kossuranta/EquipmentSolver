namespace EquipmentSolver.Core.Models;

/// <summary>
/// IGDB/Twitch API configuration settings bound from appsettings.
/// </summary>
public class IgdbSettings
{
    public const string SectionName = "Igdb";

    public string ClientId { get; set; } = null!;
    public string ClientSecret { get; set; } = null!;
}
