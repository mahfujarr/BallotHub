using BallotHub.Data;
using BallotHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BallotHub.Controllers;

[Authorize(Roles = "Administrator")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;

    public AdminController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Dashboard()
    {
        var elections = await _db.Elections
            .Include(election => election.Positions)
            .Include(election => election.Candidates)
            .Include(election => election.Votes)
            .OrderByDescending(election => election.StartDate)
            .ToListAsync();

        var model = new AdminDashboardViewModel
        {
            TotalElections = elections.Count,
            PublishedElections = elections.Count(election => election.IsPublished),
            ActiveElections = elections.Count(election => election.Status == ElectionStatus.Active),
            TotalCandidates = elections.Sum(election => election.Candidates.Count),
            TotalVotes = elections.Sum(election => election.Votes.Count),
            TotalVoters = elections
                .SelectMany(election => election.Votes)
                .Select(vote => vote.UserId)
                .Distinct()
                .Count(),
            Elections = elections.Select(election => new AdminElectionSummaryViewModel
            {
                ElectionId = election.Id,
                Title = election.Title,
                Status = election.Status,
                IsPublished = election.IsPublished,
                StartDate = election.StartDate,
                EndDate = election.EndDate,
                PositionCount = election.Positions.Count,
                CandidateCount = election.Candidates.Count,
                VoteCount = election.Votes.Count,
                DistinctVoterCount = election.Votes
                    .Select(vote => vote.UserId)
                    .Distinct()
                    .Count()
            }).ToList()
        };

        return View(model);
    }
}
