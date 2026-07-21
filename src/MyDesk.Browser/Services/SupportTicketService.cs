using MyDesk.Browser.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace MyDesk.Browser.Services
{
    /// <summary>
    /// Manages support ticket creation, local persistence, and email submission.
    /// </summary>
    public class SupportTicketService
    {
        private readonly string _storagePath;
        private readonly string _supportEmail = "peter@bardenhagen.xyz";
        private List<SupportTicket> _tickets = new();

        public SupportTicketService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dir = Path.Combine(appData, "MyDesk", "Browser", "SupportTickets");
            Directory.CreateDirectory(dir);
            _storagePath = Path.Combine(dir, "tickets.json");
            Load();
        }

        public IReadOnlyList<SupportTicket> Tickets => _tickets.AsReadOnly();

        /// <summary>
        /// Creates a new support ticket and submits it via email.
        /// </summary>
        public SupportTicket Submit(string subject, string description, string priority, string category, string submittedBy)
        {
            var ticket = new SupportTicket
            {
                Subject = subject,
                Description = description,
                Priority = priority,
                Category = category,
                SubmittedBy = submittedBy,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Status = "Submitted"
            };

            _tickets.Add(ticket);
            Save();
            SendEmail(ticket);

            return ticket;
        }

        /// <summary>
        /// Updates the status of an existing ticket.
        /// </summary>
        public void UpdateStatus(string ticketId, string newStatus)
        {
            var ticket = _tickets.FirstOrDefault(t => t.Id == ticketId);
            if (ticket != null)
            {
                ticket.Status = newStatus;
                ticket.UpdatedAt = DateTime.Now;
                Save();
            }
        }

        /// <summary>
        /// Opens the default email client with the ticket details pre-filled.
        /// </summary>
        private void SendEmail(SupportTicket ticket)
        {
            try
            {
                var body = new StringBuilder();
                body.AppendLine($"Support Ticket #{ticket.Id}");
                body.AppendLine();
                body.AppendLine($"Category: {ticket.Category}");
                body.AppendLine($"Priority: {ticket.Priority}");
                body.AppendLine($"Submitted: {ticket.ShortDate}");
                body.AppendLine($"Submitted By: {ticket.SubmittedBy}");
                body.AppendLine();
                body.AppendLine("--- Description ---");
                body.AppendLine(ticket.Description);
                body.AppendLine();
                body.AppendLine("--- System Info ---");
                body.AppendLine($"App: MyDesk Browser");
                body.AppendLine($"OS: {Environment.OSVersion}");

                var subject = Uri.EscapeDataString($"[Support] {ticket.Subject} (T#{ticket.Id})");
                var bodyEncoded = Uri.EscapeDataString(body.ToString());
                var mailto = $"mailto:{_supportEmail}?subject={subject}&body={bodyEncoded}";

                Process.Start(new ProcessStartInfo
                {
                    FileName = mailto,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to send support email: {ex.Message}");
            }
        }

        private void Load()
        {
            try
            {
                if (File.Exists(_storagePath))
                {
                    var json = File.ReadAllText(_storagePath);
                    _tickets = JsonSerializer.Deserialize<List<SupportTicket>>(json) ?? new List<SupportTicket>();
                }
            }
            catch
            {
                _tickets = new List<SupportTicket>();
            }
        }

        private void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(_tickets, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_storagePath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save tickets: {ex.Message}");
            }
        }
    }
}
