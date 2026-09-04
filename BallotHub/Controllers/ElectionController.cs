using BallotHub.Data;
using BallotHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BallotHub.Controllers;

[Authorize]
public class ElectionController : Controller
{
    private readonly ApplicationDbContext _db;

    public ElectionController(ApplicationDbContext db)
    {
        _db = db;
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
            .Include(item => item.Positions)
            .Include(item => item.Candidates)
                .ThenInclude(candidate => candidate.Position)
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
        var election = await _db.Elections
            .Include(item => item.Candidates)
            .Include(item => item.Positions)
            .SingleOrDefaultAsync(item => item.Id == id);

        if (election == null)
            return NotFound();

        var candidateIds = election.Candidates.Select(candidate => candidate.Id).ToList();
        if (candidateIds.Count > 0)
        {
            var relatedVotes = await _db.Votes
                .Where(vote => candidateIds.Contains(vote.CandidateId) || vote.ElectionId == id)
                .ToListAsync();

            if (relatedVotes.Count > 0)
                _db.Votes.RemoveRange(relatedVotes);
        }

        if (election.Positions.Any())
            _db.Positions.RemoveRange(election.Positions);

        if (election.Candidates.Any())
            _db.Candidates.RemoveRange(election.Candidates);

        _db.Elections.Remove(election);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePosition(int electionId, Position position)
    {
        var election = await _db.Elections.FindAsync(electionId);
        if (election == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(position.Name))
        {
            TempData["ElectionError"] = "Position name is required.";
            return RedirectToAction(nameof(Details), new { id = electionId });
        }

        position.ElectionId = electionId;
        _db.Positions.Add(position);
        await _db.SaveChangesAsync();
        TempData["ElectionMessage"] = "The position has been created.";
        return RedirectToAction(nameof(Details), new { id = electionId });
    }

    [Authorize(Roles = "Administrator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCandidate(int electionId, Candidate candidate)
    {
        var election = await _db.Elections
            .Include(item => item.Positions)
            .SingleOrDefaultAsync(item => item.Id == electionId);

        if (election == null)
            return NotFound();

        if (!candidate.PositionId.HasValue || !election.Positions.Any(position => position.Id == candidate.PositionId.Value))
        {
            TempData["ElectionError"] = "Select a valid position for this candidate.";
            return RedirectToAction(nameof(Details), new { id = electionId });
        }

        var candidateName = (candidate.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(candidateName))
        {
            TempData["ElectionError"] = "Please provide a valid candidate name.";
            return RedirectToAction(nameof(Details), new { id = electionId });
        }

        candidate.Name = candidateName;
        candidate.ElectionId = electionId;
        _db.Candidates.Add(candidate);
        await _db.SaveChangesAsync();
        TempData["ElectionMessage"] = "The candidate has been added.";
        return RedirectToAction(nameof(Details), new { id = electionId });
    }

    [Authorize(Roles = "Administrator")]
    [HttpGet]
    public async Task<IActionResult> EditCandidate(int id)
    {
        var candidate = await _db.Candidates
            .Include(item => item.Position)
            .SingleOrDefaultAsync(item => item.Id == id);

        if (candidate == null)
            return NotFound();

        var election = await _db.Elections
            .Include(item => item.Positions)
            .SingleOrDefaultAsync(item => item.Id == candidate.ElectionId);

        if (election == null)
            return NotFound();

        ViewBag.ElectionId = election.Id;
        ViewBag.Positions = election.Positions.OrderBy(position => position.Name).ToList();
        return View(candidate);
    }

    [Authorize(Roles = "Administrator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCandidate(int id, Candidate candidate)
    {
        var existingCandidate = await _db.Candidates
            .SingleOrDefaultAsync(item => item.Id == id);

        if (existingCandidate == null)
            return NotFound();

        var election = await _db.Elections
            .Include(item => item.Positions)
            .SingleOrDefaultAsync(item => item.Id == existingCandidate.ElectionId);

        if (election == null)
            return NotFound();

        if (!candidate.PositionId.HasValue || !election.Positions.Any(position => position.Id == candidate.PositionId.Value))
        {
            TempData["ElectionError"] = "Select a valid position for this candidate.";
            return RedirectToAction(nameof(Details), new { id = existingCandidate.ElectionId });
        }

        var candidateName = (candidate.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(candidateName))
        {
            TempData["ElectionError"] = "Please provide a valid candidate name.";
            return RedirectToAction(nameof(Details), new { id = existingCandidate.ElectionId });
        }

        existingCandidate.Name = candidateName;
        existingCandidate.Biography = candidate.Biography;
        existingCandidate.PositionId = candidate.PositionId;

        await _db.SaveChangesAsync();
        TempData["ElectionMessage"] = "The candidate has been updated.";
        return RedirectToAction(nameof(Details), new { id = existingCandidate.ElectionId });
    }

    [Authorize(Roles = "Administrator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCandidate(int id)
    {
        var candidate = await _db.Candidates.FindAsync(id);
        if (candidate == null)
            return NotFound();

        var electionId = candidate.ElectionId;
        _db.Candidates.Remove(candidate);
        await _db.SaveChangesAsync();
        TempData["ElectionMessage"] = "The candidate has been deleted.";
        return RedirectToAction(nameof(Details), new { id = electionId });
    }

    [Authorize(Roles = "Administrator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignCandidateToPosition(int candidateId, int positionId)
    {
        var candidate = await _db.Candidates
            .SingleOrDefaultAsync(item => item.Id == candidateId);

        if (candidate == null)
            return NotFound();

        var election = await _db.Elections
            .Include(item => item.Positions)
            .SingleOrDefaultAsync(item => item.Id == candidate.ElectionId);

        if (election == null)
            return NotFound();

        if (!election.Positions.Any(position => position.Id == positionId))
        {
            TempData["ElectionError"] = "That position does not belong to this election.";
            return RedirectToAction(nameof(Details), new { id = candidate.ElectionId });
        }

        candidate.PositionId = positionId;
        await _db.SaveChangesAsync();
        TempData["ElectionMessage"] = "The candidate has been assigned to the selected position.";
        return RedirectToAction(nameof(Details), new { id = candidate.ElectionId });
    }

    private void ValidateDates(Election election)
    {
        if (election.EndDate <= election.StartDate)
            ModelState.AddModelError(nameof(Election.EndDate), "The end date must be after the start date.");
    }
}