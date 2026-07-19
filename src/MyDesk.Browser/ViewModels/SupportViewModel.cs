using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyDesk.Browser.Models;
using MyDesk.Browser.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace MyDesk.Browser.ViewModels
{
    public partial class SupportViewModel : ObservableObject
    {
        private readonly SupportTicketService _ticketService;

        [ObservableProperty]
        private string _subject = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string _selectedPriority = "Normal";

        [ObservableProperty]
        private string _selectedCategory = "Technical";

        [ObservableProperty]
        private string _submittedBy = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isSubmitting = false;

        [ObservableProperty]
        private bool _showNewTicket = true;

        [ObservableProperty]
        private SupportTicket? _selectedTicket = null;

        public ObservableCollection<SupportTicket> Tickets { get; } = new();
        public string[] Priorities => SupportTicket.Priorities;
        public string[] Categories => SupportTicket.Categories;

        public SupportViewModel(string currentUser)
        {
            _ticketService = new SupportTicketService();
            _submittedBy = currentUser;
            RefreshTickets();
        }

        [RelayCommand]
        private async System.Threading.Tasks.Task SubmitAsync()
        {
            if (string.IsNullOrWhiteSpace(Subject) || string.IsNullOrWhiteSpace(Description))
            {
                StatusMessage = "Please fill in both subject and description.";
                return;
            }

            IsSubmitting = true;
            StatusMessage = "Submitting support request...";

            try
            {
                var ticket = _ticketService.Submit(
                    Subject.Trim(),
                    Description.Trim(),
                    SelectedPriority,
                    SelectedCategory,
                    string.IsNullOrWhiteSpace(SubmittedBy) ? "MyDesk Browser User" : SubmittedBy);

                Tickets.Insert(0, ticket);
                StatusMessage = $"Ticket #{ticket.Id} submitted successfully! Your email client has opened with the details.";
                Subject = string.Empty;
                Description = string.Empty;

                // Auto-clear status after 5 seconds
                await System.Threading.Tasks.Task.Delay(5000);
                if (StatusMessage.StartsWith("Ticket #"))
                    StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsSubmitting = false;
            }
        }

        [RelayCommand]
        private void ViewTicket(SupportTicket? ticket)
        {
            if (ticket != null)
            {
                SelectedTicket = ticket;
            }
        }

        [RelayCommand]
        private void NewTicket()
        {
            SelectedTicket = null;
            ShowNewTicket = true;
        }

        [RelayCommand]
        private void RefreshTickets()
        {
            Tickets.Clear();
            foreach (var ticket in _ticketService.Tickets.OrderByDescending(t => t.CreatedAt))
            {
                Tickets.Add(ticket);
            }
        }

        public void ClearStatus()
        {
            StatusMessage = string.Empty;
        }

        partial void OnSelectedTicketChanged(SupportTicket? value)
        {
            if (value != null)
            {
                ShowNewTicket = false;
            }
        }
    }
}
