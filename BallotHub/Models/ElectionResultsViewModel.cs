namespace BallotHub.Models;

public class ElectionResultsViewModel
{
    public Election Election { get; set; } = null!;
    public IReadOnlyList<PositionResult> Positions { get; set; } = [];
}

public class PositionResult
{
    public Position Position { get; set; } = null!;
    public IReadOnlyList<CandidateResult> Candidates { get; set; } = [];
}

public class CandidateResult
{
    public Candidate Candidate { get; set; } = null!;
    public int VoteCount { get; set; }
}