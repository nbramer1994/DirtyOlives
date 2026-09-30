using System.Data;
using DirtyOlives.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DirtyOlives.Data
{
    /// <summary>
    /// The database is created with EnsureCreated, which never updates an existing database.
    /// This adds any optional columns that were introduced after it was first created.
    /// Works against both SQLite and PostgreSQL.
    /// </summary>
    public static class DatabaseSchemaUpdater
    {
        public static void EnsureOptionalColumns(MartiniDbContext db)
        {
            ArgumentNullException.ThrowIfNull(db);

            EnsureUsersTable(db);

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

                var columnType = property.GetColumnType() ?? "text";
                db.Database.ExecuteSqlRaw($"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {columnType} NULL");
            }
        }

        /// <summary>
        /// EnsureCreated only builds the schema for a brand new database, so the Users
        /// table has to be created explicitly on databases that already existed.
        /// The Id is app-assigned, which keeps this SQL valid on SQLite and PostgreSQL alike.
        /// </summary>
        private static void EnsureUsersTable(MartiniDbContext db)
        {
            db.Database.ExecuteSqlRaw(
                """
                CREATE TABLE IF NOT EXISTS "Users" (
                    "Id" integer NOT NULL,
                    "Name" character varying(100) NOT NULL,
                    CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
                )
                """);

            db.Database.ExecuteSqlRaw(
                """CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Name" ON "Users" ("Name")""");
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

                if (db.Database.IsSqlite())
                {
                    command.CommandText = $"PRAGMA table_info(\"{table}\")";

                    using var sqliteReader = command.ExecuteReader();
                    while (sqliteReader.Read())
                    {
                        columns.Add(sqliteReader.GetString(1));
                    }

                    return columns;
                }

                command.CommandText = "SELECT column_name FROM information_schema.columns WHERE table_name = @table";

                var parameter = command.CreateParameter();
                parameter.ParameterName = "table";
                parameter.Value = table;
                command.Parameters.Add(parameter);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    columns.Add(reader.GetString(0));
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
