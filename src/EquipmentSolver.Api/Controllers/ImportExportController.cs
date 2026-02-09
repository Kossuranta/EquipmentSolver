using System.Security.Claims;
using System.Text.Json;
using EquipmentSolver.Api.DTOs;
using EquipmentSolver.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EquipmentSolver.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
[EnableRateLimiting("api")]
public class ImportExportController : ControllerBase
{
    private readonly IImportExportService _importExportService;

    public ImportExportController(IImportExportService importExportService)
    {
        _importExportService = importExportService;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>
    /// Download a CSV template for equipment import.
    /// </summary>
    [HttpGet("profiles/{id:int}/equipment/csv-template")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCsvTemplate(int id)
    {
        var csv = await _importExportService.GenerateCsvTemplateAsync(id, UserId);
        if (csv is null)
            return NotFound();

        return File(
            System.Text.Encoding.UTF8.GetBytes(csv),
            "text/csv",
            "equipment-template.csv");
    }

    /// <summary>
    /// Bulk import equipment from parsed CSV data with slot/stat mappings.
    /// </summary>
    [HttpPost("profiles/{id:int}/equipment/import")]
    [ProducesResponseType(typeof(BulkImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BulkImportEquipment(int id, [FromBody] BulkEquipmentImportRequest request)
    {
        if (request.Items.Count == 0)
            return BadRequest(new ErrorResponse("No items to import."));

        var result = await _importExportService.BulkImportEquipmentAsync(id, UserId, request);
        if (result is null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Export a full profile as a portable JSON file.
    /// </summary>
    [HttpGet("profiles/{id:int}/export")]
    [ProducesResponseType(typeof(ProfileExportData), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportProfile(int id)
    {
        var data = await _importExportService.ExportProfileAsync(id, UserId);
        if (data is null)
            return NotFound();

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return File(
            System.Text.Encoding.UTF8.GetBytes(json),
            "application/json",
            "profile-export.json");
    }

    /// <summary>
    /// Import a profile from JSON, creating a new profile owned by the current user.
    /// </summary>
    [HttpPost("profiles/import")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportProfileAsNew([FromBody] ProfileExportData request)
    {
        if (request.Profile is null)
            return BadRequest(new ErrorResponse("Invalid profile data."));

        if (string.IsNullOrWhiteSpace(request.Profile.Name))
            return BadRequest(new ErrorResponse("Profile name is required."));

        var profile = await _importExportService.ImportProfileAsNewAsync(UserId, request);
        return StatusCode(StatusCodes.Status201Created, new { id = profile.Id, name = profile.Name });
    }

    /// <summary>
    /// Replace an existing profile with imported JSON data.
    /// </summary>
    [HttpPut("profiles/{id:int}/import")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplaceProfile(int id, [FromBody] ProfileExportData request)
    {
        if (request.Profile is null)
            return BadRequest(new ErrorResponse("Invalid profile data."));

        var profile = await _importExportService.ReplaceProfileAsync(id, UserId, request);
        if (profile is null)
            return NotFound();

        return Ok(new { id = profile.Id, name = profile.Name });
    }
}
