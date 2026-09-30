using System.ComponentModel.DataAnnotations;

namespace DirtyOlives.Core.Models
{
    /// <summary>
    /// A person using the app, identified by first name only.
    /// This is identification for separating ratings, not authentication.
    /// </summary>
    public class AppUser
    {
        /// <summary>Default user that pre-existing ratings belong to.</summary>
        public const int DefaultUserId = MartiniRating.DefaultUserId;

        /// <summary>Assigned by the app rather than the database so the same schema works on SQLite and PostgreSQL.</summary>
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}
