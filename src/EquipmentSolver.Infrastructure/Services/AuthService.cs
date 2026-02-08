using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EquipmentSolver.Core.Entities;
using EquipmentSolver.Core.Interfaces;
using EquipmentSolver.Core.Models;
using EquipmentSolver.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EquipmentSolver.Infrastructure.Services;

/// <summary>
/// Handles authentication using ASP.NET Identity + JWT tokens.
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;
    private readonly JwtSettings _jwtSettings;

    public AuthService(UserManager<ApplicationUser> userManager, AppDbContext db, IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _db = db;
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

    /// <summary>
    /// Deletes a user account and all associated data. Manually cleans up records
    /// from other users that reference this user's profiles (NoAction FK relationships).
    /// </summary>
    public async Task<bool> DeleteAccountAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return false;

        // Get IDs of all profiles owned by this user
        var ownedProfileIds = await _db.GameProfiles
            .Where(p => p.OwnerId == userId)
            .Select(p => p.Id)
            .ToListAsync();

        if (ownedProfileIds.Count > 0)
        {
            // Clean up other users' votes on this user's profiles (Profile FK is NoAction)
            var otherVotes = await _db.ProfileVotes
                .Where(v => ownedProfileIds.Contains(v.ProfileId) && v.UserId != userId)
                .ToListAsync();
            _db.ProfileVotes.RemoveRange(otherVotes);

            // Clean up other users' usage records for this user's profiles (Profile FK is NoAction)
            var otherUsages = await _db.ProfileUsages
                .Where(u => ownedProfileIds.Contains(u.ProfileId) && u.UserId != userId)
                .ToListAsync();
            _db.ProfileUsages.RemoveRange(otherUsages);

            // Get IDs of all equipment and slots in owned profiles
            var ownedEquipmentIds = await _db.Equipment
                .Where(e => ownedProfileIds.Contains(e.ProfileId))
                .Select(e => e.Id)
                .ToListAsync();

            var ownedSlotIds = await _db.EquipmentSlots
                .Where(s => ownedProfileIds.Contains(s.ProfileId))
                .Select(s => s.Id)
                .ToListAsync();

            // Clean up other users' equipment states (Equipment FK is NoAction)
            if (ownedEquipmentIds.Count > 0)
            {
                var otherEquipStates = await _db.UserEquipmentStates
                    .Where(s => ownedEquipmentIds.Contains(s.EquipmentId) && s.UserId != userId)
                    .ToListAsync();
                _db.UserEquipmentStates.RemoveRange(otherEquipStates);
            }

            // Clean up other users' slot states (Slot FK is NoAction)
            if (ownedSlotIds.Count > 0)
            {
                var otherSlotStates = await _db.UserSlotStates
                    .Where(s => ownedSlotIds.Contains(s.SlotId) && s.UserId != userId)
                    .ToListAsync();
                _db.UserSlotStates.RemoveRange(otherSlotStates);
            }

            // Clean up EquipmentStats (StatType FK is NoAction) and SlotCompatibilities (Slot FK is NoAction)
            // These cascade from Equipment/Slot deletion via their Equipment FK (Cascade),
            // but EquipmentStat→StatType and EquipmentSlotCompatibility→Slot are NoAction.
            // Since we're deleting the Equipment/Slots (which cascade from Profile), these
            // will be cleaned up by the Equipment cascade. No manual cleanup needed.

            await _db.SaveChangesAsync();
        }

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
