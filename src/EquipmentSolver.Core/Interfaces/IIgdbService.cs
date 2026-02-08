using EquipmentSolver.Core.Models;

namespace EquipmentSolver.Core.Interfaces;

/// <summary>
/// Proxies game search requests to the IGDB API with caching.
/// </summary>
public interface IIgdbService
{
    /// <summary>
    /// Search for games by name. Results are cached with stale-while-revalidate:
    /// fresh for 24h, background-refreshed until 72h, then expired.
    /// </summary>
    Task<List<GameSearchResult>> SearchGamesAsync(string query, int limit = 20);
}
