using System.ComponentModel.DataAnnotations;

namespace EquipmentSolver.Api.DTOs.Social;

/// <summary>
/// Request to set profile visibility.
/// </summary>
public class SetVisibilityRequest
{
    public bool IsPublic { get; set; }
}

/// <summary>
/// Request to vote on a profile.
/// </summary>
public class VoteRequest
{
    /// <summary>
    /// +1 for upvote, -1 for downvote, 0 to remove vote.
    /// </summary>
    [Range(-1, 1)]
    public int Vote { get; set; }
}

/// <summary>
/// Response after voting.
/// </summary>
public class VoteResponse
{
    public int NewScore { get; set; }
    public int UserVote { get; set; }
}
