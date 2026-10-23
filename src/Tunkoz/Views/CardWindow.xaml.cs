using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Tunkoz.Core.Models;
using Tunkoz.Services;
using CoreRect = Tunkoz.Core.Models.Rectangle;

namespace Tunkoz.Views
{
    public partial class CardWindow : Window
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private QrResult? _result;
        private DispatcherTimer? _dismissTimer;
        private int _timeoutSeconds = 8;

        public CardWindow()
        {
            InitializeComponent();
            ShowActivated = false;
            SourceInitialized += OnSourceInitialized;
            PreviewKeyDown += OnPreviewKeyDown;
            MouseLeave += OnMouseLeave;
            MouseEnter += OnMouseEnter;
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            // Applied before first show so the card never takes focus
            // (taking focus would dismiss the app's native context menu).
            IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);
        }

        public void DisplayResult(QrResult result, int cursorX, int cursorY)
        {
            _result = result;
            PopulateUI(result);

            // Show off-screen first so SizeToContent gives us the real size, then place it.
            Left = -32000;
            Top = -32000;
            Show();
            UpdateLayout();
            PositionWindow(cursorX, cursorY, result.BoundingBox);
            StartDismissTimer();
        }

        private void PopulateUI(QrResult result)
        {
            var payload = result.Payload;
            string typeIcon = payload.Type switch
            {
                PayloadType.Url => "🔗",
                PayloadType.WiFi => "📶",
                PayloadType.VCard => "👤",
                PayloadType.Tel => "☎️",
                PayloadType.Mailto => "✉️",
                PayloadType.Sms => "💬",
                PayloadType.Geo => "📍",
                PayloadType.Text => "📋",
                _ => "❓"
            };

            TypeIcon.Text = typeIcon;

            switch (payload.Type)
            {
                case PayloadType.Url:
                    DisplayUrlCard(payload);
                    break;
                case PayloadType.WiFi:
                    DisplayWiFiCard(payload);
                    break;
                case PayloadType.VCard:
                    DisplayVCardCard(payload);
                    break;
                case PayloadType.Tel:
                    DisplayTelCard(payload);
                    break;
                case PayloadType.Mailto:
                    DisplayMailtoCard(payload);
                    break;
                case PayloadType.Sms:
                    DisplaySmsCard(payload);
                    break;
                case PayloadType.Geo:
                    DisplayGeoCard(payload);
                    break;
                case PayloadType.Text:
                    DisplayTextCard(payload);
                    break;
            }
        }

        private void DisplayUrlCard(Payload payload)
        {
            var safety = payload.Safety;
            DomainOrTitle.Text = safety?.DisplayUrl ?? payload.Content;
            ContentPreview.Text = safety?.RegistrableDomain ?? "";

            if (safety?.Severity == SafetySeverity.Blocked)
            {
                DangerBanner.Visibility = Visibility.Visible;
                DangerText.Text = "This link cannot be opened for security reasons.";
                PrimaryActionButton.IsEnabled = false;
                PrimaryActionButton.Content = "Cannot Open";
                SecondaryActionButton.Content = "Copy";
                SecondaryActionButton.Click -= OnSecondaryAction;
                SecondaryActionButton.Click += (s, e) => CopyToClipboard(payload.Content);
            }
            else if (safety?.Severity == SafetySeverity.Warning)
            {
                WarningBanner.Visibility = Visibility.Visible;
                WarningText.Text = GetSafetyWarningText(safety.ReasonKey ?? "");
                PrimaryActionButton.Content = "Open";
                SecondaryActionButton.Content = "Copy";
                PrimaryActionButton.Click -= OnPrimaryAction;
                PrimaryActionButton.Click += (s, e) => OpenUrl(payload.Content);
                SecondaryActionButton.Click -= OnSecondaryAction;
                SecondaryActionButton.Click += (s, e) => CopyToClipboard(payload.Content);
            }
            else
            {
                PrimaryActionButton.Content = "Open";
                SecondaryActionButton.Content = "Copy";
                PrimaryActionButton.Click -= OnPrimaryAction;
                PrimaryActionButton.Click += (s, e) => OpenUrl(payload.Content);
                SecondaryActionButton.Click -= OnSecondaryAction;
                SecondaryActionButton.Click += (s, e) => CopyToClipboard(payload.Content);
            }
        }

        private void DisplayWiFiCard(Payload payload)
        {
            var meta = payload.Metadata ?? new Dictionary<string, string>();
            string ssid = meta.TryGetValue("ssid", out var s) ? s : "Network";
            DomainOrTitle.Text = ssid;
            ContentPreview.Text = "Wi-Fi Network";

            PrimaryActionButton.Content = "Copy Password";
            SecondaryActionButton.Content = "Copy Name";
            PrimaryActionButton.Click -= OnPrimaryAction;
            PrimaryActionButton.Click += (s, e) =>
            {
                if (meta.TryGetValue("password", out var pwd))
                    CopyToClipboard(pwd);
            };
            SecondaryActionButton.Click -= OnSecondaryAction;
            SecondaryActionButton.Click += (s, e) => CopyToClipboard(ssid);
        }

        private void DisplayVCardCard(Payload payload)
        {
            var meta = payload.Metadata ?? new Dictionary<string, string>();
            string name = meta.TryGetValue("name", out var n) ? n : "Contact";
            DomainOrTitle.Text = name;

            var tel = meta.TryGetValue("tel", out var t) ? t : "";
            var email = meta.TryGetValue("email", out var em) ? em : "";
            ContentPreview.Text = $"{(tel != "" ? tel + " " : "")}{(email != "" ? email : "")}".Trim();

            PrimaryActionButton.Content = "Save as .vcf";
            SecondaryActionButton.Content = "Copy";
            PrimaryActionButton.Click -= OnPrimaryAction;
            PrimaryActionButton.Click += (s, e) => SaveVCard(payload);
            SecondaryActionButton.Click -= OnSecondaryAction;
            SecondaryActionButton.Click += (s, e) => CopyToClipboard(payload.Content);
        }

        private void DisplayTelCard(Payload payload)
        {
            DomainOrTitle.Text = "Phone Number";
            ContentPreview.Text = payload.Content.Replace("tel:", "");

            PrimaryActionButton.Content = "Call";
            SecondaryActionButton.Content = "Copy";
            PrimaryActionButton.Click -= OnPrimaryAction;
            PrimaryActionButton.Click += (s, e) => OpenUrl(payload.Content);
            SecondaryActionButton.Click -= OnSecondaryAction;
            SecondaryActionButton.Click += (s, e) => CopyToClipboard(payload.Content.Replace("tel:", ""));
        }

        private void DisplayMailtoCard(Payload payload)
        {
            DomainOrTitle.Text = "Email Address";
            ContentPreview.Text = payload.Content.Replace("mailto:", "");

            PrimaryActionButton.Content = "Email";
            SecondaryActionButton.Content = "Copy";
            PrimaryActionButton.Click -= OnPrimaryAction;
            PrimaryActionButton.Click += (s, e) => OpenUrl(payload.Content);
            SecondaryActionButton.Click -= OnSecondaryAction;
            SecondaryActionButton.Click += (s, e) => CopyToClipboard(payload.Content.Replace("mailto:", ""));
        }

        private void DisplaySmsCard(Payload payload)
        {
            DomainOrTitle.Text = "SMS";
            ContentPreview.Text = payload.Content.Replace("smsto:", "");

            PrimaryActionButton.Content = "Send SMS";
            SecondaryActionButton.Content = "Copy";
            PrimaryActionButton.Click -= OnPrimaryAction;
            PrimaryActionButton.Click += (s, e) => OpenUrl(payload.Content);
            SecondaryActionButton.Click -= OnSecondaryAction;
            SecondaryActionButton.Click += (s, e) => CopyToClipboard(payload.Content.Replace("smsto:", ""));
        }

        private void DisplayGeoCard(Payload payload)
        {
            DomainOrTitle.Text = "Location";
            ContentPreview.Text = payload.Content.Replace("geo:", "");

            PrimaryActionButton.Content = "Open Maps";
            SecondaryActionButton.Content = "Copy";
            PrimaryActionButton.Click -= OnPrimaryAction;
            PrimaryActionButton.Click += (s, e) => OpenUrl(payload.Content);
            SecondaryActionButton.Click -= OnSecondaryAction;
            SecondaryActionButton.Click += (s, e) => CopyToClipboard(payload.Content);
        }

        private void DisplayTextCard(Payload payload)
        {
            DomainOrTitle.Text = "Text";
            ContentPreview.Text = payload.Content;

            PrimaryActionButton.Visibility = Visibility.Collapsed;
            SecondaryActionButton.Content = "Copy";
            SecondaryActionButton.Click -= OnSecondaryAction;
            SecondaryActionButton.Click += (s, e) => CopyToClipboard(payload.Content);
        }

        private string GetSafetyWarningText(string reasonKey) => reasonKey switch
        {
            "plain_http" => "This link uses HTTP (unencrypted). Only proceed if you trust the source.",
            "punycode_domain" => "This domain uses international characters. Verify the displayed domain.",
            "raw_ip_host" => "This link points to a raw IP address. Proceed with caution.",
            "known_shortener" => "This is a shortened URL. The actual destination is hidden.",
            "suspicious_pattern" => "This URL contains suspicious patterns. Proceed with caution.",
            _ => "Exercise caution with this link."
        };

        private void CopyToClipboard(string text)
        {
            try
            {
                Clipboard.SetText(text);
                ShowCopiedFeedback();
            }
            catch
            {
                // Silently fail if clipboard access denied
            }
        }

        private void ShowCopiedFeedback()
        {
            SecondaryActionButton.Content = "Copied ✓";
            SecondaryActionButton.IsEnabled = false;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            timer.Tick += (s, e) =>
            {
                SecondaryActionButton.Content = "Copy";
                SecondaryActionButton.IsEnabled = true;
                timer.Stop();
            };
            timer.Start();
        }

        private void OpenUrl(string uri)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = uri,
                    UseShellExecute = true
                });
                Close();
            }
            catch
            {
                // Silently fail if cannot open
            }
        }

        private void SaveVCard(Payload payload)
        {
            // Placeholder: in a full implementation, show a save dialog
            CopyToClipboard(payload.Content);
        }

        private void PositionWindow(int cursorX, int cursorY, CoreRect? qrBounds)
        {
            WindowPlacementHelper.PositionWindowSmartly(this, cursorX, cursorY, qrBounds);
        }

        private void StartDismissTimer()
        {
            _dismissTimer?.Stop();
            _dismissTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };

            int elapsed = 0;
            _dismissTimer.Tick += (s, e) =>
            {
                elapsed += 100;
                DismissProgress.Value = Math.Max(0, 100 - (elapsed / (_timeoutSeconds * 1000.0) * 100));

                if (elapsed >= _timeoutSeconds * 1000)
                {
                    _dismissTimer?.Stop();
                    Close();
                }
            };
            _dismissTimer.Start();
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            _dismissTimer?.Stop();
            DismissProgress.Value = DismissProgress.Value; // Pause animation
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            StartDismissTimer();
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        }

        private void OnPrimaryAction(object sender, RoutedEventArgs e)
        {
        }

        private void OnSecondaryAction(object sender, RoutedEventArgs e)
        {
        }

        protected override void OnClosed(EventArgs e)
        {
            _dismissTimer?.Stop();
            base.OnClosed(e);
        }
    }
}
