using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BallotHub.Models;

public class Election
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Starts at")]
    public DateTime StartDate { get; set; }

    [Display(Name = "Ends at")]
    public DateTime EndDate { get; set; }

    public bool IsPublished { get; set; }

    [NotMapped]
    public ElectionStatus Status
    {
        get
        {
            var now = DateTime.UtcNow;
            if (now < StartDate.ToUniversalTime())
                return ElectionStatus.Upcoming;

            return now <= EndDate.ToUniversalTime()
                ? ElectionStatus.Active
                : ElectionStatus.Finished;
        }
    }

    public ICollection<Candidate> Candidates { get; set; } = new List<Candidate>();
    public ICollection<Position> Positions { get; set; } = new List<Position>();
    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
}

public enum ElectionStatus
{
    Upcoming,
    Active,
    Finished
}