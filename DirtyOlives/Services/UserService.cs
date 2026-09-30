using DirtyOlives.Core.Models;
using DirtyOlives.Data;
using Microsoft.EntityFrameworkCore;

namespace DirtyOlives.Services
{
    /// <summary>
    /// Maps first names onto stable user ids. There is no authentication here;
    /// picking a name simply selects whose ratings you are looking at.
    /// </summary>
    public class UserService
    {
        private readonly MartiniDbContext _db;

        public UserService(MartiniDbContext db) => _db = db;

        public async Task<List<AppUser>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Users
                .AsNoTracking()
                .OrderBy(u => u.Name)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Returns the existing user with this name, or creates one. Name matching is
        /// case-insensitive so "nick" and "Nick" resolve to the same user id.
        /// </summary>
        public async Task<AppUser> GetOrCreateAsync(string name, CancellationToken cancellationToken = default)
        {
            var trimmed = (name ?? string.Empty).Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                throw new ArgumentException("A name is required.", nameof(name));
            }

            var existing = await _db.Users
                .FirstOrDefaultAsync(u => u.Name.ToLower() == trimmed.ToLower(), cancellationToken);

            if (existing is not null)
            {
                return existing;
            }

            // Ids are app-assigned; DefaultUserId is reserved for pre-existing ratings.
            var maxId = await _db.Users.AnyAsync(cancellationToken)
                ? await _db.Users.MaxAsync(u => u.Id, cancellationToken)
                : AppUser.DefaultUserId;

            var user = new AppUser
            {
                Id = maxId + 1,
                Name = trimmed
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync(cancellationToken);

            return user;
        }

        /// <summary>Makes sure the owner of any pre-existing ratings has a name to log in with.</summary>
        public async Task EnsureDefaultUserAsync(string name, CancellationToken cancellationToken = default)
        {
            if (await _db.Users.AnyAsync(u => u.Id == AppUser.DefaultUserId, cancellationToken))
            {
                return;
            }

            _db.Users.Add(new AppUser { Id = AppUser.DefaultUserId, Name = name });
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
