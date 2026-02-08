using EquipmentSolver.Api.DTOs;
using EquipmentSolver.Core.Interfaces;
using EquipmentSolver.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EquipmentSolver.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[EnableRateLimiting("api")]
public class GamesController : ControllerBase
{
    private readonly IIgdbService _igdbService;

    public GamesController(IIgdbService igdbService)
    {
        _igdbService = igdbService;
    }

    /// <summary>
    /// Search for games via IGDB. Results are cached for 24 hours.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<GameSearchResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return BadRequest(new ErrorResponse("Search query must be at least 2 characters."));

        var results = await _igdbService.SearchGamesAsync(q, limit);
        return Ok(results);
    }
}
