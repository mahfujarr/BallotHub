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
        public DbSet<Position> Positions => Set<Position>();
        public DbSet<Vote> Votes => Set<Vote>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Vote>()
                .HasIndex(vote => new { vote.ElectionId, vote.PositionId, vote.UserId })
                .IsUnique();

            builder.Entity<Position>()
                .HasOne(position => position.Election)
                .WithMany(election => election.Positions)
                .HasForeignKey(position => position.ElectionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Candidate>()
                .HasOne(candidate => candidate.Election)
                .WithMany(election => election.Candidates)
                .HasForeignKey(candidate => candidate.ElectionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Candidate>()
                .HasOne(candidate => candidate.Position)
                .WithMany(position => position.Candidates)
                .HasForeignKey(candidate => candidate.PositionId)
                .OnDelete(DeleteBehavior.Restrict);

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
                .HasOne(vote => vote.Position)
                .WithMany()
                .HasForeignKey(vote => vote.PositionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Vote>()
                .HasOne(vote => vote.User)
                .WithMany()
                .HasForeignKey(vote => vote.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}