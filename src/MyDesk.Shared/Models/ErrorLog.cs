using System;

namespace MyDesk.Shared.Models;

public class ErrorLog
{
    public int ErrorLogId { get; set; }
    public DateTime ErrorDate { get; set; }
    public string Severity { get; set; } = "Error";
    public string? ExceptionType { get; set; }
    public string Message { get; set; } = "";
    public string? StackTrace { get; set; }
    public string? InnerException { get; set; }
    public string? RequestUrl { get; set; }
    public string? HttpMethod { get; set; }
    public string? UserAgent { get; set; }
    public string? IPAddress { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? CorrelationId { get; set; }
    public string? Source { get; set; }
    public bool IsResolved { get; set; }
    public string? ResolvedBy { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime CreatedAt { get; set; }
}
