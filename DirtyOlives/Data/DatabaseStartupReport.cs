using DirtyOlives.Core.Models;

namespace DirtyOlives.Data
{
    /// <summary>
    /// Captures the outcome of the database check performed once during application
    /// startup so it can be surfaced to the UI later.
    /// </summary>
    public class DatabaseStartupReport
    {
        public DatabaseStatus Status { get; private set; } = new()
        {
            IsHealthy = false,
            Message = "Database has not been checked yet."
        };

        public void RecordSuccess(string provider, string dataSource)
        {
            Status = new DatabaseStatus
            {
                IsHealthy = true,
                Message = "Database connected at startup.",
                Provider = provider,
                DataSource = dataSource,
                CheckedAt = DateTimeOffset.UtcNow
            };
        }

        public void RecordFailure(string provider, string dataSource, Exception exception)
        {
            // GetBaseException unwraps RetryLimitExceededException so the real
            // Npgsql/socket failure is what actually reaches the user.
            var root = exception.GetBaseException();

            Status = new DatabaseStatus
            {
                IsHealthy = false,
                Message = "Database connection failed at startup.",
                Provider = provider,
                DataSource = dataSource,
                Error = $"{root.GetType().Name}: {root.Message}",
                CheckedAt = DateTimeOffset.UtcNow
            };
        }
    }
}
