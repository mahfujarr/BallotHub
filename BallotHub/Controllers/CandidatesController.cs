using BallotHub.Data;
using BallotHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BallotHub.Controllers;

[Authorize]
public class CandidatesController : Controller
{
    private readonly ApplicationDbContext _db;

    public CandidatesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var candidates = await _db.Candidates
            .Include(candidate => candidate.Election)
            .Include(candidate => candidate.Position)
            .Where(candidate => candidate.Election.IsPublished || User.IsInRole("Administrator"))
            .OrderBy(candidate => candidate.Election.Title)
            .ThenBy(candidate => candidate.Position!.Name)
            .ThenBy(candidate => candidate.Name)
            .ToListAsync();

        return View(candidates);
    }
}