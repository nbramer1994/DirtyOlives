using System;

namespace DirtyOlives.Core.Models
{
    /// <summary>
    /// Result of a database connectivity or read/write check, shared between the
    /// server API and the WebAssembly client.
    /// </summary>
    public class DatabaseStatus
    {
        public bool IsHealthy { get; set; }

        /// <summary>Short headline shown in the toast.</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>EF Core provider name, e.g. Npgsql.EntityFrameworkCore.PostgreSQL.</summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>Host or file the connection points at. Never includes credentials.</summary>
        public string DataSource { get; set; } = string.Empty;

        /// <summary>Root cause message when <see cref="IsHealthy"/> is false.</summary>
        public string? Error { get; set; }

        public DateTimeOffset CheckedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
