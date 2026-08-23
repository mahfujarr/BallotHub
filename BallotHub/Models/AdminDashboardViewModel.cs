namespace BallotHub.Models;

public class AdminDashboardViewModel
{
    public int TotalElections { get; set; }
    public int PublishedElections { get; set; }
    public int ActiveElections { get; set; }
    public int TotalCandidates { get; set; }
    public int TotalVotes { get; set; }
    public int TotalVoters { get; set; }
    public IReadOnlyList<AdminElectionSummaryViewModel> Elections { get; set; } = [];
}

public class AdminElectionSummaryViewModel
{
    public int ElectionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public ElectionStatus Status { get; set; }
    public bool IsPublished { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int PositionCount { get; set; }
    public int CandidateCount { get; set; }
    public int VoteCount { get; set; }
    public int DistinctVoterCount { get; set; }
}
