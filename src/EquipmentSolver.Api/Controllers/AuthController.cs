using System.Security.Claims;
using EquipmentSolver.Api.DTOs;
using EquipmentSolver.Api.DTOs.Auth;
using EquipmentSolver.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EquipmentSolver.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Register a new user account.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request.Username, request.Password);

        if (!result.Succeeded)
            return BadRequest(new ErrorResponse([.. result.Errors]));

        return Ok(new AuthResponse
        {
            AccessToken = result.AccessToken!,
            RefreshToken = result.RefreshToken!,
            ExpiresAt = result.ExpiresAt!.Value
        });
    }

    /// <summary>
    /// Log in with username and password.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request.Username, request.Password);

        if (!result.Succeeded)
            return Unauthorized(new ErrorResponse([.. result.Errors]));

        return Ok(new AuthResponse
        {
            AccessToken = result.AccessToken!,
            RefreshToken = result.RefreshToken!,
            ExpiresAt = result.ExpiresAt!.Value
        });
    }

    /// <summary>
    /// Refresh an access token using a refresh token.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken);

        if (!result.Succeeded)
            return Unauthorized(new ErrorResponse([.. result.Errors]));

        return Ok(new AuthResponse
        {
            AccessToken = result.AccessToken!,
            RefreshToken = result.RefreshToken!,
            ExpiresAt = result.ExpiresAt!.Value
        });
    }

    /// <summary>
    /// Delete the authenticated user's account. Cascades to all owned data.
    /// </summary>
    [Authorize]
    [HttpDelete("account")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var deleted = await _authService.DeleteAccountAsync(userId);
        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
