using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace MyDesk.Shared.Services;

public class WorkflowApprovalService
{
    private readonly HttpClient _http;
    private readonly ILogger<WorkflowApprovalService> _logger;

    public WorkflowApprovalService(HttpClient http, ILogger<WorkflowApprovalService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<List<WorkflowDto>> GetWorkflowsAsync()
    {
        try
        {
            var response = await _http.GetFromJsonAsync<WorkflowsResponse>("/api/approval/workflows");
            return response?.Workflows ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching workflows");
            throw;
        }
    }

    public async Task<ApprovalSubmitResponse> SubmitExpenseForApprovalAsync(int expenseId)
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"/api/expenses/{expenseId}/submit-for-approval", new { });
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApprovalSubmitResponse>(json) ?? new ApprovalSubmitResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting expense {Id} for approval", expenseId);
            throw;
        }
    }

    public async Task<ApprovalSubmitResponse> SubmitTimesheetForApprovalAsync(int timesheetId)
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"/api/timesheets/{timesheetId}/submit-for-approval", new { });
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApprovalSubmitResponse>(json) ?? new ApprovalSubmitResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting timesheet {Id} for approval", timesheetId);
            throw;
        }
    }

    public async Task<List<PendingApprovalDto>> GetPendingApprovalsAsync()
    {
        try
        {
            var response = await _http.GetFromJsonAsync<PendingApprovalsResponse>("/api/approval/pending");
            return response?.Approvals ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pending approvals");
            throw;
        }
    }

    public async Task<ApprovalActionResponse> ApproveAsync(int requestId, string? comments = null)
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"/api/approval/requests/{requestId}/approve", new { comments });
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApprovalActionResponse>(json) ?? new ApprovalActionResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving request {Id}", requestId);
            throw;
        }
    }

    public async Task<ApprovalActionResponse> RejectAsync(int requestId, string reason)
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"/api/approval/requests/{requestId}/reject", new { reason });
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApprovalActionResponse>(json) ?? new ApprovalActionResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting request {Id}", requestId);
            throw;
        }
    }

    public async Task<List<ApprovalHistoryDto>> GetHistoryAsync(int requestId)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<HistoryResponse>($"/api/approval/requests/{requestId}/history");
            return response?.Actions ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching history for request {Id}", requestId);
            throw;
        }
    }

    // DTOs
    public class WorkflowsResponse
    {
        public List<WorkflowDto> Workflows { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class WorkflowDto
    {
        public int Id { get; set; }
        public string ModuleType { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public bool IsDefault { get; set; }
        public int ApprovalLevels { get; set; }
        public string CreatedAt { get; set; } = "";
    }

    public class ApprovalSubmitResponse
    {
        public string Message { get; set; } = "";
        public int RequestId { get; set; }
    }

    public class PendingApprovalsResponse
    {
        public List<PendingApprovalDto> Approvals { get; set; } = new();
    }

    public class PendingApprovalDto
    {
        public int RequestId { get; set; }
        public int ModuleId { get; set; }
        public string ModuleType { get; set; } = "";
        public string SubmitterName { get; set; } = "";
        public DateTime SubmittedAt { get; set; }
        public int CurrentLevel { get; set; }
        public int ApprovalLevels { get; set; }
    }

    public class ApprovalActionResponse
    {
        public string Message { get; set; } = "";
        public bool FinalApproval { get; set; }
        public int? NextLevel { get; set; }
    }

    public class HistoryResponse
    {
        public List<ApprovalHistoryDto> Actions { get; set; } = new();
        public int Timeline { get; set; }
    }

    public class ApprovalHistoryDto
    {
        public int Level { get; set; }
        public string Action { get; set; } = "";
        public string? Comments { get; set; }
        public string ApprovedBy { get; set; } = "";
        public string ActionAt { get; set; } = "";
    }
}
