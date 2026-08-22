namespace BallotHub.Models;

public class BallotViewModel
{
    public int ElectionId { get; set; }
    public string ElectionTitle { get; set; } = string.Empty;
    public IReadOnlyList<PositionBallotViewModel> Positions { get; set; } = [];
}

public class PositionBallotViewModel
{
    public int PositionId { get; set; }
    public string PositionName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IReadOnlyList<CandidateOptionViewModel> Candidates { get; set; } = [];
    public int? SelectedCandidateId { get; set; }
}

public class CandidateOptionViewModel
{
    public int CandidateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Biography { get; set; } = string.Empty;
}

public class BallotSubmissionViewModel
{
    public int ElectionId { get; set; }
    public List<BallotSelectionViewModel> Selections { get; set; } = [];
}

public class BallotSelectionViewModel
{
    public int PositionId { get; set; }
    public int CandidateId { get; set; }
}