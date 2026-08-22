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
        var elections = await _db.Elections
            .Where(election => election.IsPublished && election.EndDate < DateTime.Now)
            .OrderByDescending(election => election.EndDate)
            .ToListAsync();
        return View(elections);
    }

    public async Task<IActionResult> Details(int id)
    {
        var election = await _db.Elections
            .Include(item => item.Positions)
                .ThenInclude(position => position.Candidates)
            .SingleOrDefaultAsync(item => item.Id == id && item.IsPublished);
        if (election == null)
            return NotFound();

        var counts = await _db.Votes
            .Where(vote => vote.ElectionId == id)
            .GroupBy(vote => new { vote.PositionId, vote.CandidateId })
            .Select(group => new { group.Key.PositionId, group.Key.CandidateId, VoteCount = group.Count() })
            .ToListAsync();

        var model = new ElectionResultsViewModel
        {
            Election = election,
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
        return View(model);
    }
}