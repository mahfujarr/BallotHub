using BallotHub.Data;
using BallotHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BallotHub.Controllers;

[Authorize]
public class ResultsController : Controller
{
    private readonly ApplicationDbContext _db;

    public ResultsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var now = DateTime.Now;
        var elections = await _db.Elections
            .Where(election => election.EndDate < now &&
                (election.IsPublished || User.IsInRole("Administrator")))
            .OrderByDescending(election => election.EndDate)
            .ToListAsync();
        return View(elections);
    }

    public async Task<IActionResult> Details(int id)
    {
        var election = await _db.Elections
            .Include(item => item.Positions)
                .ThenInclude(position => position.Candidates)
            .SingleOrDefaultAsync(item => item.Id == id &&
                (item.IsPublished || User.IsInRole("Administrator")));
        if (election == null)
            return NotFound();

        if (election.EndDate >= DateTime.Now)
            return NotFound();

        var counts = await _db.Votes
            .Where(vote => vote.ElectionId == id)
            .GroupBy(vote => new { vote.PositionId, vote.CandidateId })
            .Select(group => new { group.Key.PositionId, group.Key.CandidateId, VoteCount = group.Count() })
            .ToListAsync();

        var totalVotesCast = await _db.Votes
            .Where(vote => vote.ElectionId == id)
            .CountAsync();

        var totalVoters = await _db.Votes
            .Where(vote => vote.ElectionId == id)
            .Select(vote => vote.UserId)
            .Distinct()
            .CountAsync();

        var model = new ElectionResultsViewModel
        {
            Election = election,
            TotalVotesCast = totalVotesCast,
            TotalVoters = totalVoters,
            GeneratedAtUtc = DateTime.UtcNow,
            Positions = election.Positions
                .Select(position => new PositionResult
                {
                    Position = position,
                    Candidates = position.Candidates
                        .Where(candidate => candidate.ElectionId == id && candidate.PositionId == position.Id)
                        .Select(candidate => new CandidateResult
                        {
                            Candidate = candidate,
                            VoteCount = counts
                                .Where(item => item.PositionId == position.Id && item.CandidateId == candidate.Id)
                                .Select(item => item.VoteCount)
                                .FirstOrDefault()
                        })
                        .OrderByDescending(item => item.VoteCount)
                        .ToList()
                })
                .ToList()
        };

        foreach (var positionResult in model.Positions)
        {
            positionResult.TotalVotes = positionResult.Candidates.Sum(candidate => candidate.VoteCount);

            foreach (var candidate in positionResult.Candidates)
            {
                candidate.VotePercentage = positionResult.TotalVotes == 0
                    ? 0
                    : candidate.VoteCount * 100.0 / positionResult.TotalVotes;
            }

            var maxVotes = positionResult.Candidates
                .Select(candidate => candidate.VoteCount)
                .DefaultIfEmpty(0)
                .Max();

            var winners = maxVotes == 0
                ? []
                : positionResult.Candidates
                    .Where(candidate => candidate.VoteCount == maxVotes)
                    .ToList();

            foreach (var winner in winners)
                winner.IsWinner = true;

            positionResult.IsTie = winners.Count > 1;
            positionResult.WinnerDisplay = winners.Count switch
            {
                0 => "No winner",
                1 => winners[0].Candidate.Name,
                _ => string.Join(", ", winners.Select(winner => winner.Candidate.Name))
            };
        }

        return View(model);
    }
}