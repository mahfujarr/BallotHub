using BallotHub.Data;
using BallotHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BallotHub.Controllers;

[Authorize]
public class VotingController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public VotingController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var elections = await _db.Elections
            .Where(election => election.IsPublished)
            .OrderByDescending(election => election.StartDate)
            .ToListAsync();

        return View(elections.Where(election => election.Status == ElectionStatus.Active).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Ballot(int electionId)
    {
        var election = await GetActiveElection(electionId);
        if (election == null)
            return NotFound();

        if (!await HasExistingVote(electionId))
            return View(await BuildBallot(election));

        TempData["VotingError"] = "You have already submitted a vote for this election.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(BallotSubmissionViewModel submission)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return Challenge();

        var election = await GetActiveElection(submission.ElectionId);
        if (election == null)
            return VotingError("This election is not open for voting.");

        var positions = await _db.Positions
            .Where(position => position.ElectionId == submission.ElectionId)
            .OrderBy(position => position.Name)
            .ToListAsync();

        if (positions.Count == 0 || submission.Selections.Count != positions.Count ||
            submission.Selections.Select(selection => selection.PositionId).Distinct().Count() != positions.Count)
        {
            return VotingError("Select exactly one candidate for every position.", submission.ElectionId);
        }

        var positionIds = positions.Select(position => position.Id).ToHashSet();
        if (submission.Selections.Any(selection => !positionIds.Contains(selection.PositionId)))
            return VotingError("One or more submitted positions are invalid.", submission.ElectionId);

        var candidateIds = submission.Selections.Select(selection => selection.CandidateId).ToList();
        var candidates = await _db.Candidates
            .Where(candidate => candidate.ElectionId == submission.ElectionId && candidateIds.Contains(candidate.Id))
            .ToListAsync();
        var candidateById = candidates.ToDictionary(candidate => candidate.Id);

        if (submission.Selections.Any(selection =>
                !candidateById.TryGetValue(selection.CandidateId, out var candidate) ||
                candidate.PositionId != selection.PositionId))
        {
            return VotingError("One or more selected candidates do not belong to their position.", submission.ElectionId);
        }

        if (await HasExistingVote(submission.ElectionId, userId))
            return VotingError("You have already submitted a vote for this election.", submission.ElectionId);

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            _db.Votes.AddRange(submission.Selections.Select(selection => new Vote
            {
                ElectionId = submission.ElectionId,
                PositionId = selection.PositionId,
                CandidateId = selection.CandidateId,
                UserId = userId
            }));

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException exception) when (IsDuplicateVote(exception))
        {
            await transaction.RollbackAsync();
            return VotingError("You have already submitted a vote for one or more positions.", submission.ElectionId);
        }

        TempData["VotingMessage"] = "Your ballot was submitted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<Election?> GetActiveElection(int electionId)
    {
        var election = await _db.Elections.SingleOrDefaultAsync(item =>
            item.Id == electionId && item.IsPublished);
        return election?.Status == ElectionStatus.Active ? election : null;
    }

    private async Task<bool> HasExistingVote(int electionId)
    {
        var userId = _userManager.GetUserId(User);
        return userId != null && await HasExistingVote(electionId, userId);
    }

    private Task<bool> HasExistingVote(int electionId, string userId) =>
        _db.Votes.AnyAsync(vote => vote.ElectionId == electionId && vote.UserId == userId);

    private async Task<BallotViewModel> BuildBallot(Election election)
    {
        var positions = await _db.Positions
            .Where(position => position.ElectionId == election.Id)
            .Include(position => position.Candidates)
            .OrderBy(position => position.Name)
            .ToListAsync();

        return new BallotViewModel
        {
            ElectionId = election.Id,
            ElectionTitle = election.Title,
            Positions = positions.Select(position => new PositionBallotViewModel
            {
                PositionId = position.Id,
                PositionName = position.Name,
                Description = position.Description,
                Candidates = position.Candidates
                    .Where(candidate => candidate.ElectionId == election.Id && candidate.PositionId == position.Id)
                    .OrderBy(candidate => candidate.Name)
                    .Select(candidate => new CandidateOptionViewModel
                    {
                        CandidateId = candidate.Id,
                        Name = candidate.Name,
                        Biography = candidate.Biography
                    })
                    .ToList()
            }).ToList()
        };
    }

    private IActionResult VotingError(string message, int? electionId = null)
    {
        TempData["VotingError"] = message;
        return electionId.HasValue
            ? RedirectToAction(nameof(Ballot), new { electionId })
            : RedirectToAction(nameof(Index));
    }

    private static bool IsDuplicateVote(DbUpdateException exception) =>
        exception.InnerException is SqlException sqlException &&
        (sqlException.Number == 2601 || sqlException.Number == 2627);
}