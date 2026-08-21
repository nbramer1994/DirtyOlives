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

            // Calculated, presentation-only members are never persisted.
            rating.Ignore(r => r.FinalRating);
            rating.Ignore(r => r.CalculatedRating);
            rating.Ignore(r => r.IsManuallyRated);
            rating.Ignore(r => r.GlassStyleDisplay);
            rating.Ignore(r => r.Summary);
        }
    }
}
