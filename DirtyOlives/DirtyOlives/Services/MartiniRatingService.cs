using DirtyOlives.Core.Models;
using DirtyOlives.Data;
using Microsoft.EntityFrameworkCore;

namespace DirtyOlives.Services
{
    /// <summary>
    /// Reads and writes martini ratings for a single user from the SQLite database.
    /// </summary>
    public class MartiniRatingService
    {
        private readonly MartiniDbContext _db;

        public MartiniRatingService(MartiniDbContext db) => _db = db;

        public async Task<List<MartiniRating>> GetForUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _db.Ratings
                .AsNoTracking()
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.DateRated)
                .ToListAsync(cancellationToken);
        }

        public async Task<MartiniRating> AddAsync(MartiniRating rating, CancellationToken cancellationToken = default)
        {
            if (rating.Id == Guid.Empty)
            {
                rating.Id = Guid.NewGuid();
            }

            if (rating.UserId <= 0)
            {
                rating.UserId = MartiniRating.DefaultUserId;
            }

            _db.Ratings.Add(rating);
            await _db.SaveChangesAsync(cancellationToken);
            return rating;
        }

        public async Task<bool> DeleteAsync(Guid id, int userId, CancellationToken cancellationToken = default)
        {
            var existing = await _db.Ratings
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, cancellationToken);

            if (existing is null)
            {
                return false;
            }

            _db.Ratings.Remove(existing);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
