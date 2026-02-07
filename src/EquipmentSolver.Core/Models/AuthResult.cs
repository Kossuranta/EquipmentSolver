namespace EquipmentSolver.Core.Models;

/// <summary>
/// Result of an authentication operation (login/register/refresh).
/// </summary>
public class AuthResult
{
    public bool Succeeded { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public List<string> Errors { get; set; } = [];

    public static AuthResult Success(string accessToken, string refreshToken, DateTime expiresAt) => new()
    {
        Succeeded = true,
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        ExpiresAt = expiresAt
    };

    public static AuthResult Failure(params string[] errors) => new()
    {
        Succeeded = false,
        Errors = [.. errors]
    };
}
