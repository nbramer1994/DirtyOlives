using DirtyOlives.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DirtyOlives.Data
{
    public class MartiniDbContext : DbContext
    {
        public MartiniDbContext(DbContextOptions<MartiniDbContext> options) : base(options)
        {
        }

        public DbSet<MartiniRating> Ratings => Set<MartiniRating>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var rating = modelBuilder.Entity<MartiniRating>();

            rating.HasKey(r => r.Id);
            rating.HasIndex(r => r.UserId);
            rating.Property(r => r.GlassStyle).HasConversion<string>();
            rating.Property(r => r.Location).HasMaxLength(200);
            rating.Property(r => r.OliveType).HasMaxLength(200);
            rating.Property(r => r.Vodka).HasMaxLength(200);

            // DateRated is a calendar date, but it maps to timestamptz on PostgreSQL,
            // which rejects any DateTime that is not Kind=Utc. DateTime.Today yields
            // Kind=Local, so normalise the Kind without shifting the date itself.
            rating.Property(r => r.DateRated).HasConversion(
                value => DateTime.SpecifyKind(value, DateTimeKind.Utc),
                value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

            // Calculated, presentation-only members are never persisted.
            rating.Ignore(r => r.FinalRating);
            rating.Ignore(r => r.CalculatedRating);
            rating.Ignore(r => r.IsManuallyRated);
            rating.Ignore(r => r.GlassStyleDisplay);
            rating.Ignore(r => r.Summary);
        }
    }
}
