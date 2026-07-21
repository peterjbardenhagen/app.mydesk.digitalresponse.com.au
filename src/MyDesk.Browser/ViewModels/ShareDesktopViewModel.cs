using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyDesk.Browser.Models;
using MyDesk.Browser.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace MyDesk.Browser.ViewModels
{
    public partial class ShareDesktopViewModel : ObservableObject
    {
        private readonly DesktopShareService _shareService;

        [ObservableProperty]
        private string _currentUrl = string.Empty;

        [ObservableProperty]
        private string _recipientEmail = string.Empty;

        [ObservableProperty]
        private bool _isMacBound = false;

        [ObservableProperty]
        private int _expiryHours = 1;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isCreating = false;

        [ObservableProperty]
        private DesktopShare? _lastShare = null;

        [ObservableProperty]
        private string _capturePreviewPath = string.Empty;

        [ObservableProperty]
        private string _shareMethod = "email"; // "email" or "clipboard"

        public ObservableCollection<DesktopShare> Shares { get; } = new();

        public int[] ExpiryOptions { get; } = { 1, 2, 4, 8, 24 };
        public string[] ShareMethods { get; } = { "email", "clipboard" };

        public ShareDesktopViewModel()
        {
            _shareService = new DesktopShareService();
            RefreshShares();
        }

        [RelayCommand]
        private async System.Threading.Tasks.Task ShareAsync()
        {
            if (string.IsNullOrWhiteSpace(CurrentUrl))
            {
                StatusMessage = "No URL to share.";
                return;
            }

            if (ShareMethod == "email" && string.IsNullOrWhiteSpace(RecipientEmail))
            {
                StatusMessage = "Please enter a recipient email address.";
                return;
            }

            IsCreating = true;
            StatusMessage = "Creating secure share link...";

            try
            {
                if (ShareMethod == "email")
                {
                    var share = _shareService.ShareViaEmail(CurrentUrl, RecipientEmail.Trim(), IsMacBound);
                    LastShare = share;
                    Shares.Insert(0, share);
                    StatusMessage = $"Share link created and sent to {RecipientEmail}!";
                    RecipientEmail = string.Empty;
                }
                else
                {
                    var share = _shareService.CreateShare(CurrentUrl, "clipboard", IsMacBound);
                    _shareService.CopyShareLink(share);
                    LastShare = share;
                    Shares.Insert(0, share);
                    StatusMessage = $"Share link copied to clipboard! Share ID: {share.Id}";
                }

                IsMacBound = false;

                // Auto-clear after 5s
                await System.Threading.Tasks.Task.Delay(5000);
                if (StatusMessage.Contains("Share"))
                    StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsCreating = false;
            }
        }

        [RelayCommand]
        private void RevokeShare(DesktopShare? share)
        {
            if (share != null)
            {
                _shareService.RevokeShare(share.Id);
                RefreshShares();
                StatusMessage = $"Share {share.Id} revoked.";
            }
        }

        [RelayCommand]
        private void CopyToClipboard(DesktopShare? share)
        {
            if (share != null)
            {
                _shareService.CopyShareLink(share);
                StatusMessage = $"Share link copied to clipboard!";
            }
        }

        [RelayCommand]
        private void SendEmail(DesktopShare? share)
        {
            if (share != null)
            {
                _shareService.SendShareLink(share);
                StatusMessage = $"Email opened with share link for {share.RecipientEmail}.";
            }
        }

        [RelayCommand]
        private void RefreshShares()
        {
            _shareService.CleanupExpired();
            Shares.Clear();
            foreach (var share in _shareService.Shares.OrderByDescending(s => s.CreatedAt))
            {
                Shares.Add(share);
            }
        }

        public void ClearStatus()
        {
            StatusMessage = string.Empty;
        }
    }
}
