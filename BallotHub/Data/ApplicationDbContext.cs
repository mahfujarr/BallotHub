using BallotHub.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BallotHub.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Election> Elections => Set<Election>();
        public DbSet<Candidate> Candidates => Set<Candidate>();
        public DbSet<Vote> Votes => Set<Vote>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Vote>()
                .HasIndex(vote => new { vote.ElectionId, vote.UserId })
                .IsUnique();

            builder.Entity<Candidate>()
                .HasOne(candidate => candidate.Election)
                .WithMany(election => election.Candidates)
                .HasForeignKey(candidate => candidate.ElectionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Vote>()
                .HasOne(vote => vote.Election)
                .WithMany(election => election.Votes)
                .HasForeignKey(vote => vote.ElectionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Vote>()
                .HasOne(vote => vote.Candidate)
                .WithMany(candidate => candidate.Votes)
                .HasForeignKey(vote => vote.CandidateId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Vote>()
                .HasOne(vote => vote.User)
                .WithMany()
                .HasForeignKey(vote => vote.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}