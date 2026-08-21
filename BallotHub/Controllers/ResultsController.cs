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
            .Include(item => item.Candidates)
            .SingleOrDefaultAsync(item => item.Id == id && item.IsPublished);
        if (election == null)
            return NotFound();

        var counts = await _db.Votes
            .Where(vote => vote.ElectionId == id)
            .GroupBy(vote => vote.CandidateId)
            .Select(group => new { CandidateId = group.Key, VoteCount = group.Count() })
            .ToDictionaryAsync(item => item.CandidateId, item => item.VoteCount);

        var model = new ElectionResultsViewModel
        {
            Election = election,
            Candidates = election.Candidates
                .Select(candidate => new CandidateResult
                {
                    Candidate = candidate,
                    VoteCount = counts.GetValueOrDefault(candidate.Id)
                })
                .OrderByDescending(item => item.VoteCount)
                .ToList()
        };
        return View(model);
    }
}