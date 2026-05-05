using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace MyDesk.Shared.Services;

/// <summary>
/// Runs ad-hoc SQL migration files at startup.
///
/// Scans <c>src/Deployment/Migration/</c> (relative to the running content root)
/// for any <c>*.sql</c> file, sorts by filename, executes each file against the
/// active SQL database, and deletes the file on success. A failed file is left
/// in place (with the exception logged) and execution continues with the next
/// file so a single bad migration doesn't block unrelated ones — though the
/// usual deploy contract is "fix the failing file before next startup".
///
/// Implementation notes:
///   * SQL Server's batch separator <c>GO</c> is not recognised by
///     <c>SqlCommand.ExecuteNonQuery</c>, so each file is split on lines that
///     are exactly <c>GO</c> (ignoring whitespace) and each batch is executed
///     separately.
///   * Each file is wrapped in <see cref="TenantImpersonation.SystemBypass"/> so
///     the SQL Row-Level Security policies don't reject schema changes during
///     pre-tenant startup.
///   * The runner uses <see cref="DatabaseService.ExecuteNonQueryAsync"/> for
///     SESSION_CONTEXT consistency with the rest of the app.
/// </summary>
public class MigrationRunnerService
{
    private readonly DatabaseService _db;
    private readonly ILogger<MigrationRunnerService> _logger;

    public MigrationRunnerService(DatabaseService db, ILogger<MigrationRunnerService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Tries a small list of plausible relative paths for the migration directory.
    /// </summary>
    private static string? ResolveMigrationDirectory(string contentRoot)
    {
        var candidates = new[]
        {
            Path.Combine(contentRoot, "..", "Deployment", "Migration"),                // src/MyDesk.Web → src/Deployment/Migration
            Path.Combine(contentRoot, "..", "..", "Deployment", "Migration"),          // bin/Debug case
            Path.Combine(contentRoot, "..", "..", "..", "Deployment", "Migration"),    // bin/Debug/net10.0
            Path.Combine(contentRoot, "Deployment", "Migration"),                      // root layout
            Path.Combine(contentRoot, "Migration"),                                    // bare layout
        };
        foreach (var c in candidates)
        {
            try
            {
                var full = Path.GetFullPath(c);
                if (Directory.Exists(full)) return full;
            }
            catch { /* keep trying */ }
        }
        return null;
    }

    /// <summary>
    /// Runs every <c>*.sql</c> file in the resolved migration directory in name order,
    /// then deletes any that ran without error.
    /// </summary>
    public async Task RunPendingAsync(string contentRoot)
    {
        var dir = ResolveMigrationDirectory(contentRoot);
        if (dir is null)
        {
            _logger.LogDebug("Migration runner: no Deployment/Migration directory found relative to {Root} — skipping.", contentRoot);
            return;
        }

        var files = Directory.EnumerateFiles(dir, "*.sql", SearchOption.TopDirectoryOnly)
            .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (files.Count == 0)
        {
            _logger.LogDebug("Migration runner: no .sql files in {Dir} — nothing to run.", dir);
            return;
        }

        _logger.LogInformation("Migration runner: found {Count} pending migration file(s) in {Dir}.", files.Count, dir);

        // Bypass tenant isolation for the duration — these are schema changes.
        using var _ = TenantImpersonation.SystemBypass();

        int succeeded = 0, failed = 0;
        foreach (var path in files)
        {
            var name = Path.GetFileName(path);
            try
            {
                var text = await File.ReadAllTextAsync(path);
                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogInformation("Migration {Name}: empty file — deleting.", name);
                    SafeDelete(path, name);
                    succeeded++;
                    continue;
                }

                foreach (var batch in SplitOnGo(text))
                {
                    if (string.IsNullOrWhiteSpace(batch)) continue;
                    await _db.ExecuteNonQueryAsync(batch);
                }

                _logger.LogInformation("Migration {Name}: applied successfully.", name);
                SafeDelete(path, name);
                succeeded++;
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogError(ex,
                    "Migration {Name}: FAILED — file left in place so it can be inspected/fixed and retried on next startup.",
                    name);
                // Continue with subsequent files; the contract is per-file.
            }
        }

        _logger.LogInformation("Migration runner: done. Succeeded={Ok} Failed={Bad}.", succeeded, failed);
    }

    /// <summary>
    /// Split a SQL script on standalone <c>GO</c> separator lines (case-insensitive),
    /// matching SSMS / sqlcmd semantics. Comments and code are preserved.
    /// </summary>
    private static IEnumerable<string> SplitOnGo(string sql)
    {
        // Match "GO" on a line by itself (optional whitespace + optional trailing count).
        var rx = new Regex(@"^\s*GO\s*(\d+)?\s*(--.*)?$",
                           RegexOptions.IgnoreCase | RegexOptions.Multiline);
        var parts = rx.Split(sql);
        foreach (var p in parts)
        {
            // Regex.Split also returns the captured groups — skip pure-numeric / whitespace ones.
            if (string.IsNullOrWhiteSpace(p)) continue;
            if (int.TryParse(p.Trim(), out _)) continue;
            yield return p;
        }
    }

    private void SafeDelete(string path, string name)
    {
        try { File.Delete(path); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Migration {Name}: applied but file delete failed (will be retried on next start).", name);
        }
    }
}
