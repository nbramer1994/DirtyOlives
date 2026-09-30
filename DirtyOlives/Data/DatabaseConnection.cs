using Npgsql;

namespace DirtyOlives.Data
{
    /// <summary>
    /// Helpers for working with the configured database connection string.
    /// Supports both Npgsql keyword strings and the postgres:// URI form that
    /// hosts such as Neon and Render hand out.
    /// </summary>
    public static class DatabaseConnection
    {
        public static bool IsPostgres(string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return false;
            }

            return connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
                || connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
                || connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Converts a postgres:// URI into the keyword form Npgsql expects, and leaves
        /// keyword connection strings untouched.
        /// </summary>
        public static string Normalize(string connectionString)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
                && !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            {
                return connectionString;
            }

            var uri = new Uri(connectionString);
            var userInfo = uri.UserInfo.Split(':', 2);

            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.IsDefaultPort ? 5432 : uri.Port,
                Database = uri.AbsolutePath.Trim('/'),
                Username = Uri.UnescapeDataString(userInfo[0]),
                Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
                SslMode = SslMode.Require
            };

            return builder.ConnectionString;
        }
    }
}
