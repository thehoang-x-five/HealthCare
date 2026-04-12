using System.Data;
using System.Text;
using HealthCare.Datas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HealthCare.Infrastructure
{
    /// <summary>
    /// Applies non-migration SQL artifacts that must exist in every environment.
    /// Keeps stored procedures, triggers, and check constraints aligned with the codebase.
    /// </summary>
    public sealed class DatabaseBootstrapper
    {
        private readonly DataContext _db;
        private readonly ILogger<DatabaseBootstrapper> _logger;
        private readonly string _scriptsRoot;

        public DatabaseBootstrapper(
            DataContext db,
            ILogger<DatabaseBootstrapper> logger,
            IWebHostEnvironment environment)
        {
            _db = db;
            _logger = logger;
            _scriptsRoot = Path.Combine(environment.ContentRootPath, "Scripts");
        }

        public async Task BootstrapAsync(CancellationToken cancellationToken = default)
        {
            await EnsureScriptArtifactsAsync(
                artifactKind: "routine",
                new[] { "sp_BookAppointment" },
                "sp_BookAppointment.sql",
                RoutineExistsAsync,
                cancellationToken);

            await EnsureScriptArtifactsAsync(
                artifactKind: "routine",
                new[] { "sp_GetAncestors", "sp_GetDescendants", "sp_IsDescendantOf" },
                "sp_Genealogy.sql",
                RoutineExistsAsync,
                cancellationToken);

            await EnsureScriptArtifactsAsync(
                artifactKind: "trigger",
                new[]
                {
                    "tr_LichHen_ValidateTransition",
                    "tr_KhoThuoc_PreventNegative",
                    "tr_DonThuoc_RollbackKho",
                    "tr_BenhNhan_ValidateParents_Insert",
                    "tr_BenhNhan_ValidateParents_Update",
                },
                "triggers.sql",
                TriggerExistsAsync,
                cancellationToken);

            await EnsureScriptArtifactsAsync(
                artifactKind: "constraint",
                new[]
                {
                    "chk_kho_thuoc_so_luong_non_negative",
                    "chk_lich_hen_trang_thai",
                    "chk_luot_kham_trang_thai",
                    "chk_don_thuoc_trang_thai",
                    "chk_hoa_don_trang_thai",
                },
                "constraints.sql",
                ConstraintExistsAsync,
                cancellationToken);
        }

        private async Task EnsureScriptArtifactsAsync(
            string artifactKind,
            IEnumerable<string> artifactNames,
            string scriptFile,
            Func<string, CancellationToken, Task<bool>> existsAsync,
            CancellationToken cancellationToken)
        {
            await ExecuteScriptFileAsync(scriptFile, cancellationToken);

            var missingArtifacts = new List<string>();
            foreach (var artifactName in artifactNames.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!await existsAsync(artifactName, cancellationToken))
                {
                    missingArtifacts.Add(artifactName);
                }
            }

            if (missingArtifacts.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Bootstrap script '{scriptFile}' did not produce expected {artifactKind}(s): {string.Join(", ", missingArtifacts)}");
            }

            _logger.LogInformation(
                "Applied bootstrap SQL script {ScriptFile} and verified {ArtifactKind}(s): {ArtifactNames}",
                scriptFile,
                artifactKind,
                string.Join(", ", artifactNames));
        }

        private async Task<bool> RoutineExistsAsync(string routineName, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.ROUTINES
                WHERE ROUTINE_SCHEMA = DATABASE()
                  AND ROUTINE_NAME = @name;
                """;

            return await ExistsAsync(sql, routineName, cancellationToken);
        }

        private async Task<bool> TriggerExistsAsync(string triggerName, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.TRIGGERS
                WHERE TRIGGER_SCHEMA = DATABASE()
                  AND TRIGGER_NAME = @name;
                """;

            return await ExistsAsync(sql, triggerName, cancellationToken);
        }

        private async Task<bool> ConstraintExistsAsync(string constraintName, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                WHERE CONSTRAINT_SCHEMA = DATABASE()
                  AND CONSTRAINT_NAME = @name;
                """;

            return await ExistsAsync(sql, constraintName, cancellationToken);
        }

        private async Task<bool> ExistsAsync(string sql, string name, CancellationToken cancellationToken)
        {
            var connection = _db.Database.GetDbConnection();

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var command = connection.CreateCommand();
            command.CommandText = sql;

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@name";
            parameter.Value = name;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result) > 0;
        }

        private async Task ExecuteScriptFileAsync(string fileName, CancellationToken cancellationToken)
        {
            var fullPath = Path.Combine(_scriptsRoot, fileName);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"SQL bootstrap script not found: {fullPath}");
            }

            var script = await File.ReadAllTextAsync(fullPath, cancellationToken);
            var commands = SplitSqlStatements(script);

            foreach (var commandText in commands)
            {
                await ExecuteNonQueryAsync(commandText, cancellationToken);
            }
        }

        private async Task ExecuteNonQueryAsync(string sql, CancellationToken cancellationToken)
        {
            var connection = _db.Database.GetDbConnection();

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static IReadOnlyList<string> SplitSqlStatements(string script)
        {
            var commands = new List<string>();
            var delimiter = ";";
            var builder = new StringBuilder();

            using var reader = new StringReader(script);
            string? line;

            while ((line = reader.ReadLine()) is not null)
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("DELIMITER ", StringComparison.OrdinalIgnoreCase))
                {
                    delimiter = trimmed["DELIMITER ".Length..].Trim();
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(line);

                if (!trimmed.EndsWith(delimiter, StringComparison.Ordinal))
                {
                    continue;
                }

                var command = builder.ToString();
                var delimiterIndex = command.LastIndexOf(delimiter, StringComparison.Ordinal);
                if (delimiterIndex >= 0)
                {
                    command = command[..delimiterIndex];
                }

                if (!string.IsNullOrWhiteSpace(command))
                {
                    commands.Add(command.Trim());
                }

                builder.Clear();
            }

            if (!string.IsNullOrWhiteSpace(builder.ToString()))
            {
                commands.Add(builder.ToString().Trim());
            }

            return commands;
        }
    }
}
