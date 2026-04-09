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
            await EnsureRoutineScriptAsync(
                new[] { "sp_BookAppointment" },
                "sp_BookAppointment.sql",
                cancellationToken);

            await EnsureRoutineScriptAsync(
                new[] { "sp_GetAncestors", "sp_GetDescendants", "sp_IsDescendantOf" },
                "sp_Genealogy.sql",
                cancellationToken);

            await EnsureTriggerScriptAsync(
                new[]
                {
                    "tr_LichHen_ValidateTransition",
                    "tr_KhoThuoc_PreventNegative",
                    "tr_DonThuoc_RollbackKho",
                },
                "triggers.sql",
                cancellationToken);

            await EnsureCheckConstraintAsync(
                "chk_kho_thuoc_so_luong_non_negative",
                "ALTER TABLE kho_thuoc ADD CONSTRAINT chk_kho_thuoc_so_luong_non_negative CHECK (SoLuongTon >= 0);",
                cancellationToken);

            await EnsureCheckConstraintAsync(
                "chk_lich_hen_trang_thai",
                "ALTER TABLE lich_hen_kham ADD CONSTRAINT chk_lich_hen_trang_thai CHECK (TrangThai IN ('dang_cho', 'da_xac_nhan', 'da_checkin', 'da_huy'));",
                cancellationToken);

            await EnsureCheckConstraintAsync(
                "chk_luot_kham_trang_thai",
                "ALTER TABLE luot_kham_benh ADD CONSTRAINT chk_luot_kham_trang_thai CHECK (TrangThai IN ('dang_thuc_hien', 'hoan_tat', 'da_huy'));",
                cancellationToken);

            await EnsureCheckConstraintAsync(
                "chk_don_thuoc_trang_thai",
                "ALTER TABLE don_thuoc ADD CONSTRAINT chk_don_thuoc_trang_thai CHECK (TrangThai IN ('da_ke', 'cho_phat', 'da_phat', 'da_huy'));",
                cancellationToken);

            await EnsureCheckConstraintAsync(
                "chk_hoa_don_trang_thai",
                "ALTER TABLE hoa_don_thanh_toan ADD CONSTRAINT chk_hoa_don_trang_thai CHECK (TrangThai IN ('chua_thu', 'da_thu', 'da_huy'));",
                cancellationToken);
        }

        private async Task EnsureRoutineScriptAsync(
            IEnumerable<string> routineNames,
            string scriptFile,
            CancellationToken cancellationToken)
        {
            await ExecuteScriptFileAsync(scriptFile, cancellationToken);
        }

        private async Task EnsureTriggerScriptAsync(
            IEnumerable<string> triggerNames,
            string scriptFile,
            CancellationToken cancellationToken)
        {
            await ExecuteScriptFileAsync(scriptFile, cancellationToken);
        }

        private async Task EnsureCheckConstraintAsync(
            string constraintName,
            string sql,
            CancellationToken cancellationToken)
        {
            if (await ConstraintExistsAsync(constraintName, cancellationToken))
            {
                return;
            }

            try
            {
                await ExecuteNonQueryAsync(sql, cancellationToken);
                _logger.LogInformation("Applied database constraint {ConstraintName}", constraintName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not apply database constraint {ConstraintName}", constraintName);
            }
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

            _logger.LogInformation("Applied bootstrap SQL script {ScriptFile}", fileName);
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
