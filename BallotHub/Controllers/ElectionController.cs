using BallotHub.Data;
using BallotHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BallotHub.Controllers;

[Authorize]
public class ElectionController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ElectionController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var elections = await _db.Elections
            .Include(election => election.Candidates)
            .Where(election => election.IsPublished || User.IsInRole("Administrator"))
            .OrderByDescending(election => election.StartDate)
            .ToListAsync();

        return View(elections);
    }

    public async Task<IActionResult> Details(int id)
    {
        var election = await _db.Elections
            .Include(item => item.Candidates)
            .SingleOrDefaultAsync(item => item.Id == id &&
                (item.IsPublished || User.IsInRole("Administrator")));

        return election == null ? NotFound() : View(election);
    }

    [Authorize(Roles = "Administrator")]
    [HttpGet]
    public IActionResult Create() => View(new Election
    {
        StartDate = DateTime.Now,
        EndDate = DateTime.Now.AddDays(7)
    });

    [Authorize(Roles = "Administrator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Election election)
    {
        ValidateDates(election);
        if (!ModelState.IsValid)
            return View(election);

        _db.Elections.Add(election);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrator")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var election = await _db.Elections.FindAsync(id);
        return election == null ? NotFound() : View(election);
    }

    [Authorize(Roles = "Administrator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Election input)
    {
        if (id != input.Id)
            return BadRequest();

        ValidateDates(input);
        if (!ModelState.IsValid)
            return View(input);

        var election = await _db.Elections.FindAsync(id);
        if (election == null)
            return NotFound();

        election.Title = input.Title;
        election.Description = input.Description;
        election.StartDate = input.StartDate;
        election.EndDate = input.EndDate;
        election.IsPublished = input.IsPublished;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Administrator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int id)
    {
        var election = await _db.Elections.FindAsync(id);
        if (election == null)
            return NotFound();

        var now = DateTime.Now;
        if (election.EndDate <= now)
        {
            TempData["ElectionError"] = "A finished election cannot be started again.";
            return RedirectToAction(nameof(Details), new { id });
        }

        election.StartDate = now;
        election.IsPublished = true;
        await _db.SaveChangesAsync();
        TempData["ElectionMessage"] = "The election is now active.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Administrator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> End(int id)
    {
        var election = await _db.Elections.FindAsync(id);
        if (election == null)
            return NotFound();

        var now = DateTime.Now;
        if (now < election.StartDate)
        {
            TempData["ElectionError"] = "An upcoming election cannot be ended before it starts.";
            return RedirectToAction(nameof(Details), new { id });
        }

        election.EndDate = now;
        await _db.SaveChangesAsync();
        TempData["ElectionMessage"] = "The election has ended.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Administrator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var election = await _db.Elections.FindAsync(id);
        if (election == null)
            return NotFound();

        _db.Elections.Remove(election);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCandidate(int electionId, Candidate candidate)
    {
        var electionExists = await _db.Elections.AnyAsync(election => election.Id == electionId);
        if (!electionExists)
            return NotFound();

        if (!ModelState.IsValid)
            return RedirectToAction(nameof(Details), new { id = electionId });

        candidate.ElectionId = electionId;
        _db.Candidates.Add(candidate);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = electionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Vote(int electionId, int candidateId)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return Challenge();

        var election = await _db.Elections.FindAsync(electionId);
        var candidateExists = await _db.Candidates.AnyAsync(candidate =>
            candidate.Id == candidateId && candidate.ElectionId == electionId);

        if (election == null || !election.IsPublished ||
            DateTime.UtcNow < election.StartDate.ToUniversalTime() ||
            DateTime.UtcNow > election.EndDate.ToUniversalTime() || !candidateExists)
        {
            TempData["ElectionError"] = "This election is not open for voting.";
            return RedirectToAction(nameof(Details), new { id = electionId });
        }

        if (await _db.Votes.AnyAsync(vote => vote.ElectionId == electionId && vote.UserId == userId))
        {
            TempData["ElectionError"] = "You have already voted in this election.";
            return RedirectToAction(nameof(Details), new { id = electionId });
        }

        _db.Votes.Add(new Vote
        {
            ElectionId = electionId,
            CandidateId = candidateId,
            UserId = userId
        });
        await _db.SaveChangesAsync();
        TempData["ElectionMessage"] = "Your vote was recorded.";
        return RedirectToAction(nameof(Details), new { id = electionId });
    }

    private void ValidateDates(Election election)
    {
        if (election.EndDate <= election.StartDate)
            ModelState.AddModelError(nameof(Election.EndDate), "The end date must be after the start date.");
    }
}