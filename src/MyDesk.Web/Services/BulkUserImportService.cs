using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MyDesk.Shared.Services;

namespace MyDesk.Web.Services;

public class BulkUserImportService
{
    private readonly DatabaseService _db;
    private readonly ComplianceAuditService _audit;

    public BulkUserImportService(DatabaseService db, ComplianceAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<BulkImportResult> ImportUsersAsync(int tenantId, int importedById, Stream csvStream, string filename)
    {
        var result = new BulkImportResult { Filename = filename };
        var errors = new List<string>();

        try
        {
            using (var reader = new StreamReader(csvStream, Encoding.UTF8))
            {
                var lines = new List<string>();
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }

                if (lines.Count < 2)
                {
                    result.Status = "Failed";
                    result.ErrorMessage = "CSV must contain header and at least one data row";
                    return result;
                }

                var headers = ParseCsvLine(lines[0]);
                ValidateHeaders(headers, out var errorMsg);
                if (!string.IsNullOrEmpty(errorMsg))
                {
                    result.Status = "Failed";
                    result.ErrorMessage = errorMsg;
                    return result;
                }

                result.TotalRows = lines.Count - 1;

                for (int i = 1; i < lines.Count; i++)
                {
                    try
                    {
                        var values = ParseCsvLine(lines[i]);
                        var user = MapCsvToUser(headers, values, i + 1);

                        await ValidateAndCreateUserAsync(tenantId, user);
                        result.SuccessfulRows++;
                    }
                    catch (Exception ex)
                    {
                        result.FailedRows++;
                        errors.Add($"Row {i + 1}: {ex.Message}");
                    }
                }

                result.Status = result.FailedRows == 0 ? "Success" : "PartialSuccess";
                if (errors.Count > 0)
                    result.ErrorMessage = string.Join("\n", errors.Take(50));
            }

            await LogBulkImportAsync(tenantId, importedById, result);
        }
        catch (Exception ex)
        {
            result.Status = "Failed";
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var ch in line)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (ch == ',' && !inQuotes)
            {
                values.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }

        values.Add(current.ToString().Trim());
        return values;
    }

    private void ValidateHeaders(List<string> headers, out string? error)
    {
        error = null;

        var required = new[] { "Email", "FirstName", "LastName" };
        foreach (var req in required)
        {
            if (!headers.Any(h => h.Equals(req, StringComparison.OrdinalIgnoreCase)))
            {
                error = $"Missing required column: {req}";
                return;
            }
        }
    }

    private BulkImportUser MapCsvToUser(List<string> headers, List<string> values, int rowNumber)
    {
        var user = new BulkImportUser();

        for (int i = 0; i < headers.Count && i < values.Count; i++)
        {
            var header = headers[i];
            var value = values[i];

            switch (header.ToLower())
            {
                case "email":
                    user.Email = value;
                    break;
                case "firstname":
                    user.FirstName = value;
                    break;
                case "lastname":
                    user.LastName = value;
                    break;
                case "departmentid":
                    if (int.TryParse(value, out int deptId))
                        user.DepartmentId = deptId;
                    break;
                case "teamid":
                    if (int.TryParse(value, out int teamId))
                        user.TeamId = teamId;
                    break;
                case "role":
                    user.Role = value;
                    break;
            }
        }

        user.Validate();
        return user;
    }

    private async Task ValidateAndCreateUserAsync(int tenantId, BulkImportUser user)
    {
        // Check for existing user
        var existing = await _db.QueryAsync(
            "SELECT UserId FROM Users WHERE TenantId = @TenantId AND Email = @Email",
            new() { ["TenantId"] = tenantId, ["Email"] = user.Email });

        if (existing.Rows.Count > 0)
            throw new InvalidOperationException($"User with email {user.Email} already exists");

        // Create user
        await _db.ExecuteNonQueryAsync(
            @"INSERT INTO Users (TenantId, Email, [Name], [Status], PrimaryDepartmentId, PrimaryTeamId)
              VALUES (@TenantId, @Email, @Name, 'Active', @DeptId, @TeamId)",
            new()
            {
                ["TenantId"] = tenantId,
                ["Email"] = user.Email,
                ["Name"] = $"{user.FirstName} {user.LastName}".Trim(),
                ["DeptId"] = (object?)user.DepartmentId ?? DBNull.Value,
                ["TeamId"] = (object?)user.TeamId ?? DBNull.Value
            });

        // Get created user ID for team membership
        var dtId = await _db.QueryAsync(
            "SELECT MAX(UserId) as Id FROM Users WHERE TenantId = @TenantId AND Email = @Email",
            new() { ["TenantId"] = tenantId, ["Email"] = user.Email });

        int userId = (int)dtId.Rows[0]["Id"];

        // Add to team if specified
        if (user.TeamId.HasValue)
        {
            await _db.ExecuteNonQueryAsync(
                @"INSERT INTO TeamMembers (TenantId, TeamId, UserId, [Role])
                  VALUES (@TenantId, @TeamId, @UserId, @Role)",
                new()
                {
                    ["TenantId"] = tenantId,
                    ["TeamId"] = user.TeamId.Value,
                    ["UserId"] = userId,
                    ["Role"] = user.Role ?? "Member"
                });
        }
    }

    private async Task LogBulkImportAsync(int tenantId, int importedById, BulkImportResult result)
    {
        await _db.ExecuteNonQueryAsync(
            @"INSERT INTO BulkUserImportLog (TenantId, ImportedById, Filename, TotalRows,
                                            SuccessfulRows, FailedRows, [Status], ErrorMessage)
              VALUES (@TenantId, @ImportedById, @Filename, @Total, @Success, @Failed, @Status, @Error)",
            new()
            {
                ["TenantId"] = tenantId,
                ["ImportedById"] = importedById,
                ["Filename"] = result.Filename,
                ["Total"] = result.TotalRows,
                ["Success"] = result.SuccessfulRows,
                ["Failed"] = result.FailedRows,
                ["Status"] = result.Status,
                ["Error"] = (object?)result.ErrorMessage ?? DBNull.Value
            });

        await _audit.LogAsync("BulkUserImportCompleted", "System", new
        {
            tenantId,
            importedById,
            filename = result.Filename,
            totalRows = result.TotalRows,
            successfulRows = result.SuccessfulRows,
            failedRows = result.FailedRows,
            status = result.Status
        });
    }
}

public class BulkImportUser
{
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public int? DepartmentId { get; set; }
    public int? TeamId { get; set; }
    public string? Role { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Email))
            throw new ArgumentException("Email is required");
        if (string.IsNullOrWhiteSpace(FirstName))
            throw new ArgumentException("FirstName is required");
        if (string.IsNullOrWhiteSpace(LastName))
            throw new ArgumentException("LastName is required");

        if (!Regex.IsMatch(Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            throw new ArgumentException("Invalid email format");
    }
}

public class BulkImportResult
{
    public string Filename { get; set; } = "";
    public int TotalRows { get; set; }
    public int SuccessfulRows { get; set; }
    public int FailedRows { get; set; }
    public string Status { get; set; } = "Pending";
    public string? ErrorMessage { get; set; }
}
