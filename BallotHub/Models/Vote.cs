namespace BallotHub.Models;

public class Vote
{
    public int Id { get; set; }
    public int ElectionId { get; set; }
    public int PositionId { get; set; }
    public int CandidateId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CastAt { get; set; } = DateTime.UtcNow;

    public Election Election { get; set; } = null!;
    public Position Position { get; set; } = null!;
    public Candidate Candidate { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}