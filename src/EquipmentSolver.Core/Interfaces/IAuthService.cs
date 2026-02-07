using EquipmentSolver.Core.Models;

namespace EquipmentSolver.Core.Interfaces;

/// <summary>
/// Handles user registration, login, and token operations.
/// </summary>
public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string username, string password);
    Task<AuthResult> LoginAsync(string username, string password);
    Task<AuthResult> RefreshTokenAsync(string refreshToken);
    Task<bool> DeleteAccountAsync(string userId);
}
