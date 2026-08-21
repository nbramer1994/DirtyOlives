using System.Data;
using DirtyOlives.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DirtyOlives.Data
{
    /// <summary>
    /// The database is created with EnsureCreated, which never updates an existing file.
    /// This adds any optional columns that were introduced after the database was first created.
    /// </summary>
    public static class SqliteSchemaUpdater
    {
        public static void EnsureOptionalColumns(MartiniDbContext db)
        {
            ArgumentNullException.ThrowIfNull(db);

            var entity = db.Model.FindEntityType(typeof(MartiniRating));
            var table = entity?.GetTableName();

            if (entity is null || string.IsNullOrEmpty(table))
            {
                return;
            }

            var existingColumns = GetColumns(db, table);

            if (existingColumns.Count == 0)
            {
                return;
            }

            foreach (var property in entity.GetProperties())
            {
                var column = property.GetColumnName();

                // Only nullable columns can be added to a table that already has rows.
                if (string.IsNullOrEmpty(column) || existingColumns.Contains(column) || !property.IsNullable)
                {
                    continue;
                }

                var columnType = property.GetColumnType() ?? "TEXT";
                db.Database.ExecuteSqlRaw($"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {columnType} NULL");
            }
        }

        private static HashSet<string> GetColumns(MartiniDbContext db, string table)
        {
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var connection = db.Database.GetDbConnection();
            var shouldClose = connection.State != ConnectionState.Open;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = $"PRAGMA table_info(\"{table}\")";

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    columns.Add(reader.GetString(1));
                }
            }
            finally
            {
                if (shouldClose)
                {
                    connection.Close();
                }
            }

            return columns;
        }
    }
}
