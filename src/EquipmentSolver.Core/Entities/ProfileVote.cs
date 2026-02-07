namespace EquipmentSolver.Core.Entities;

/// <summary>
/// A user's vote on a public profile (+1 or -1).
/// </summary>
public class ProfileVote
{
    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;

    public int ProfileId { get; set; }
    public GameProfile Profile { get; set; } = null!;

    /// <summary>
    /// +1 for upvote, -1 for downvote.
    /// </summary>
    public int Vote { get; set; }
}
