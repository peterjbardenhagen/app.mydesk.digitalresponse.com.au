using System;
using System.Text.Json.Serialization;

namespace MyDesk.Browser.Models
{
    /// <summary>
    /// Represents a support ticket tracked by the browser.
    /// </summary>
    public class SupportTicket
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpper();

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("priority")]
        public string Priority { get; set; } = "Normal";

        [JsonPropertyName("status")]
        public string Status { get; set; } = "Submitted";

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [JsonPropertyName("category")]
        public string Category { get; set; } = "General";

        [JsonPropertyName("submittedBy")]
        public string SubmittedBy { get; set; } = string.Empty;

        [JsonIgnore]
        public string StatusIcon => Status switch
        {
            "Submitted" => "📤",
            "In Progress" => "🔄",
            "Resolved" => "✅",
            "Closed" => "🔒",
            _ => "📋"
        };

        [JsonIgnore]
        public string PriorityIcon => Priority switch
        {
            "Low" => "🟢",
            "Normal" => "🟡",
            "High" => "🔴",
            "Critical" => "⛔",
            _ => "🟡"
        };

        [JsonIgnore]
        public string ShortDate => CreatedAt.ToString("MMM dd, yyyy HH:mm");

        // Allowed priorities and statuses
        public static readonly string[] Priorities = { "Low", "Normal", "High", "Critical" };
        public static readonly string[] Statuses = { "Submitted", "In Progress", "Resolved", "Closed" };
        public static readonly string[] Categories = { "General", "Account", "Technical", "Billing", "Feature Request", "Bug Report" };
    }
}
