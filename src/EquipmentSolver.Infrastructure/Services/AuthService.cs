using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EquipmentSolver.Core.Entities;
using EquipmentSolver.Core.Interfaces;
using EquipmentSolver.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EquipmentSolver.Infrastructure.Services;

/// <summary>
/// Handles authentication using ASP.NET Identity + JWT tokens.
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtSettings _jwtSettings;

    public AuthService(UserManager<ApplicationUser> userManager, IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<AuthResult> RegisterAsync(string username, string password)
    {
        var existingUser = await _userManager.FindByNameAsync(username);
        if (existingUser is not null)
            return AuthResult.Failure("Username is already taken.");

        var user = new ApplicationUser
        {
            UserName = username,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return AuthResult.Failure([.. result.Errors.Select(e => e.Description)]);

        return GenerateTokens(user);
    }

    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        var user = await _userManager.FindByNameAsync(username);
        if (user is null)
            return AuthResult.Failure("Invalid username or password.");

        var valid = await _userManager.CheckPasswordAsync(user, password);
        if (!valid)
            return AuthResult.Failure("Invalid username or password.");

        return GenerateTokens(user);
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
    {
        // Validate the refresh token by finding the user it belongs to
        var user = _userManager.Users.FirstOrDefault(u =>
            u.SecurityStamp == refreshToken);

        if (user is null)
            return AuthResult.Failure("Invalid refresh token.");

        // Rotate the security stamp to invalidate old refresh tokens
        await _userManager.UpdateSecurityStampAsync(user);

        return GenerateTokens(user);
    }

    public async Task<bool> DeleteAccountAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return false;

        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded;
    }

    private AuthResult GenerateTokens(ApplicationUser user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        // Use security stamp as refresh token (simple approach for solo project)
        var refreshToken = user.SecurityStamp ?? Guid.NewGuid().ToString();

        return AuthResult.Success(accessToken, refreshToken, expiresAt);
    }
}
