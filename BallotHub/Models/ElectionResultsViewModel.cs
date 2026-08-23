namespace BallotHub.Models;

public class ElectionResultsViewModel
{
    public Election Election { get; set; } = null!;
    public IReadOnlyList<PositionResult> Positions { get; set; } = [];
    public int TotalVotesCast { get; set; }
    public int TotalVoters { get; set; }
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}

public class PositionResult
{
    public Position Position { get; set; } = null!;
    public IReadOnlyList<CandidateResult> Candidates { get; set; } = [];
    public int TotalVotes { get; set; }
    public bool IsTie { get; set; }
    public string WinnerDisplay { get; set; } = "No winner";
}

public class CandidateResult
{
    public Candidate Candidate { get; set; } = null!;
    public int VoteCount { get; set; }
    public double VotePercentage { get; set; }
    public bool IsWinner { get; set; }
}