using MyDesk.Browser.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MyDesk.Browser.Services
{
    /// <summary>
    /// Manages desktop share sessions: creation, token generation, capture, and email.
    /// </summary>
    public class DesktopShareService
    {
        private readonly string _storagePath;
        private readonly string _baseUrl = "https://app.mydesk.digitalresponse.com.au";
        private List<DesktopShare> _shares = new();

        public DesktopShareService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dir = Path.Combine(appData, "MyDesk", "Browser", "DesktopShares");
            Directory.CreateDirectory(dir);
            _storagePath = Path.Combine(dir, "shares.json");
            _capturesDir = Path.Combine(dir, "captures");
            Directory.CreateDirectory(_capturesDir);
            Load();
        }

        private readonly string _capturesDir;

        public IReadOnlyList<DesktopShare> Shares => _shares.AsReadOnly();

        /// <summary>
        /// Creates a new share session with an encrypted token.
        /// </summary>
        public DesktopShare CreateShare(string sharedUrl, string recipientEmail, bool macBound = false, int expiryHours = 1)
        {
            var token = DesktopShare.GenerateToken();
            var encryptedToken = ShareTokenHelper.EncryptToken(token);

            var share = new DesktopShare
            {
                SharedUrl = sharedUrl,
                RecipientEmail = recipientEmail,
                Token = encryptedToken,
                IsMacBound = macBound,
                ExpiresAt = DateTime.Now.AddHours(expiryHours),
                Status = "Active",
            };

            _shares.Add(share);
            Save();
            return share;
        }

        /// <summary>
        /// Creates a share and sends the link via email.
        /// </summary>
        public DesktopShare ShareViaEmail(string sharedUrl, string recipientEmail, bool macBound = false, int expiryHours = 1)
        {
            var share = CreateShare(sharedUrl, recipientEmail, macBound, expiryHours);
            SendShareLink(share);
            return share;
        }

        /// <summary>
        /// Opens the default email client with share link details pre-filled.
        /// </summary>
        public void SendShareLink(DesktopShare share)
        {
            try
            {
                var shareUrl = DesktopShare.BuildShareUrl(share.Token, _baseUrl);
                var body = new StringBuilder();
                body.AppendLine($"Here is a shared view of MyDesk for you.");
                body.AppendLine();
                body.AppendLine($"Share Link: {shareUrl}");
                body.AppendLine();
                body.AppendLine($"This link expires on: {share.ExpiryDate}");
                if (share.IsMacBound)
                {
                    body.AppendLine("This share is bound to a specific device for security.");
                }
                body.AppendLine();
                body.AppendLine($"Shared via MyDesk Browser");

                var subject = Uri.EscapeDataString("[MyDesk] Shared Desktop View");
                var bodyEncoded = Uri.EscapeDataString(body.ToString());
                var mailto = $"mailto:{share.RecipientEmail}?subject={subject}&body={bodyEncoded}";

                Process.Start(new ProcessStartInfo
                {
                    FileName = mailto,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to send share email: {ex.Message}");
            }
        }

        /// <summary>
        /// Copies the share link to the clipboard.
        /// </summary>
        public void CopyShareLink(DesktopShare share)
        {
            var shareUrl = DesktopShare.BuildShareUrl(share.Token, _baseUrl);
            Clipboard.SetText(shareUrl);
        }

        /// <summary>
        /// Revokes an active share.
        /// </summary>
        public void RevokeShare(string shareId)
        {
            var share = _shares.FirstOrDefault(s => s.Id == shareId);
            if (share != null)
            {
                share.Status = "Revoked";
                Save();
            }
        }

        /// <summary>
        /// Cleans up expired shares.
        /// </summary>
        public void CleanupExpired()
        {
            var expired = _shares.Where(s => s.IsExpired).ToList();
            foreach (var share in expired)
            {
                share.Status = "Expired";
            }
            if (expired.Any()) Save();
        }

        /// <summary>
        /// Captures the current window content as a PNG screenshot.
        /// Returns the file path of the captured image.
        /// </summary>
        public string? CaptureScreenshot(FrameworkElement? target)
        {
            if (target == null) return null;

            try
            {
                var renderTarget = new RenderTargetBitmap(
                    (int)target.ActualWidth,
                    (int)target.ActualHeight,
                    96, 96,
                    PixelFormats.Pbgra32);

                renderTarget.Render(target);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderTarget));

                var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                var filePath = Path.Combine(_capturesDir, $"share-{timestamp}.png");
                using var stream = File.OpenWrite(filePath);
                encoder.Save(stream);

                return filePath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Screenshot capture failed: {ex.Message}");
                return null;
            }
        }

        private void Load()
        {
            try
            {
                if (File.Exists(_storagePath))
                {
                    var json = File.ReadAllText(_storagePath);
                    _shares = JsonSerializer.Deserialize<List<DesktopShare>>(json) ?? new List<DesktopShare>();
                }
            }
            catch
            {
                _shares = new List<DesktopShare>();
            }
        }

        private void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(_shares, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_storagePath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save shares: {ex.Message}");
            }
        }
    }
}
