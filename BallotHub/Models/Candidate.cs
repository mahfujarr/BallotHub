using System.ComponentModel.DataAnnotations;

namespace BallotHub.Models;

public class Candidate
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Biography { get; set; } = string.Empty;

    public int ElectionId { get; set; }
    public Election Election { get; set; } = null!;
    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
}