using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Tunkoz.Core.Models;
using Tunkoz.Services;
using CoreRect = Tunkoz.Core.Models.Rectangle;
using Path = System.Windows.Shapes.Path;

namespace Tunkoz.Views;

/// <summary>
/// The QR result card from the Claude Design artifact: 336 px, 16 px padding, 8 px radius.
/// It never takes focus (so the app's own context menu stays open) and auto-dismisses after 8 s,
/// paused while hovered.
/// </summary>
public partial class CardWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const double ShadowMargin = 16;
    private static readonly TimeSpan DismissAfter = TimeSpan.FromSeconds(8);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private Action? _primaryAction;
    private Action? _secondaryAction;
    private AnimationClock? _dismissClock;
    private bool _closing;

    public CardWindow()
    {
        InitializeComponent();
        SourceInitialized += OnSourceInitialized;
        ClipHost.SizeChanged += (_, e) =>
            ClipHost.Clip = new RectangleGeometry(new Rect(e.NewSize), 7, 7);
        MouseEnter += (_, _) => _dismissClock?.Controller?.Pause();
        MouseLeave += (_, _) => _dismissClock?.Controller?.Resume();
        CloseButton.ToolTip = Loc.T("Card.Close");
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);
    }

    public void DisplayResult(QrResult result, int cursorX, int cursorY)
    {
        Populate(result.Payload);

        // Show off-screen first so SizeToContent produces the real height, then place it.
        Left = -32000;
        Top = -32000;
        Show();
        UpdateLayout();
        WindowPlacementHelper.PositionWindowSmartly(this, cursorX, cursorY, result.BoundingBox, ShadowMargin);
        StartDismissTimer();
    }

    // ---------------------------------------------------------------- states

    private void Populate(Payload payload)
    {
        switch (payload.Type)
        {
            case PayloadType.Url: ShowUrl(payload); break;
            case PayloadType.WiFi: ShowWifi(payload); break;
            case PayloadType.VCard: ShowContact(payload); break;
            case PayloadType.Tel: ShowSimple(payload, "Icon.Phone", "Tel.Type", StripScheme(payload.Content, "tel:"), "Tel.Open"); break;
            case PayloadType.Mailto: ShowSimple(payload, "Icon.Mail", "Mail.Type", StripScheme(payload.Content, "mailto:"), "Mail.Open"); break;
            case PayloadType.Sms: ShowSimple(payload, "Icon.Message", "Sms.Type", StripScheme(payload.Content, "smsto:"), "Sms.Open"); break;
            case PayloadType.Geo: ShowSimple(payload, "Icon.Pin", "Geo.Type", StripScheme(payload.Content, "geo:"), "Geo.Open"); break;
            default: ShowText(payload); break;
        }
    }

    private void ShowUrl(Payload payload)
    {
        var url = payload.Content;
        var safety = payload.Safety;
        Uri.TryCreate(url, UriKind.Absolute, out var uri);

        var blocked = safety?.Severity == SafetySeverity.Blocked || safety?.ReasonKey == "dangerous_scheme";
        if (blocked)
        {
            SetTile("Danger", "Icon.Blocked");
            TypeLabel.Text = Loc.T("Card.BlockedType");
            TitleText.Text = uri != null ? uri.Scheme + ":" : url.Split(':')[0] + ":";
            SubtitleText.Text = url;
            ShowBanner("Danger", "Icon.Blocked", Loc.T("Card.Blocked.Title"),
                Loc.T(safety?.ReasonKey == "dangerous_scheme" ? "Card.Dangerous.Body" : "Card.Blocked.Body"));
            SetPrimary("Icon.Copy", Loc.T("Card.CopyText"), () => Copy(url, PrimaryButton, PrimaryIcon, PrimaryText));
            HideSecondary();
            return;
        }

        var domain = !string.IsNullOrWhiteSpace(safety?.RegistrableDomain) ? safety!.RegistrableDomain : uri?.Host ?? url;
        TitleText.Text = safety?.DecodedDisplay is { Length: > 0 } decoded && safety.ReasonKey == "punycode_domain" ? decoded : domain;
        SubtitleText.Text = url;

        var isHttps = string.Equals(uri?.Scheme, "https", StringComparison.OrdinalIgnoreCase);
        TypeLabel.Text = Loc.T("Card.WebLink");
        SchemePanel.Visibility = Visibility.Visible;
        SchemeLock.Visibility = isHttps ? Visibility.Visible : Visibility.Collapsed;
        SchemeText.Text = uri?.Scheme.ToUpperInvariant() ?? "";

        if (safety?.Severity == SafetySeverity.Warning)
        {
            SetTile("Warning", "Icon.Link");
            if (safety.ReasonKey == "known_shortener" || safety.IsShortener)
            {
                SchemeLock.Visibility = Visibility.Collapsed;
                SchemeText.Text = Loc.T("Card.Shortened");
            }

            var key = safety.ReasonKey ?? "default";
            var title = Loc.T($"Warn.{key}.Title");
            if (title == $"Warn.{key}.Title") key = "default";
            ShowBanner("Warning", "Icon.Warning", Loc.T($"Warn.{key}.Title"), Loc.T($"Warn.{key}.Body"));

            // Risky: copying is the primary action, opening is secondary
            SetPrimary("Icon.Copy", Loc.T("Card.CopyLink"), () => Copy(url, PrimaryButton, PrimaryIcon, PrimaryText));
            SetSecondary("Icon.OpenExternal", Loc.T("Card.OpenAnyway"), () => Open(url));
        }
        else
        {
            SetTile("Info", "Icon.Link");
            SetPrimary("Icon.OpenExternal", Loc.T("Card.OpenLink"), () => Open(url));
            SetSecondary("Icon.Copy", Loc.T("Card.CopyLink"), () => Copy(url, SecondaryButton, SecondaryIcon, SecondaryText));
        }
    }

    private void ShowWifi(Payload payload)
    {
        var meta = payload.Metadata ?? new Dictionary<string, string>();
        var ssid = meta.GetValueOrDefault("ssid", payload.Content);
        var password = meta.GetValueOrDefault("password", "");
        var auth = meta.GetValueOrDefault("type", "");

        SetTile("Info", "Icon.Wifi");
        TypeLabel.Text = string.IsNullOrEmpty(auth) || auth.Equals("nopass", StringComparison.OrdinalIgnoreCase)
            ? Loc.T("Wifi.Type")
            : $"{Loc.T("Wifi.Type")} · {auth.ToUpperInvariant()}";
        TitleText.Text = ssid;
        SubtitleText.Visibility = Visibility.Collapsed;

        if (!string.IsNullOrEmpty(password))
        {
            var masked = new string('•', Math.Clamp(password.Length, 8, 14));
            var value = MakeText(masked, mono: true, size: 14);
            var eye = MakeIconButton("Icon.Eye", Loc.T("Wifi.ShowPassword"));
            var shown = false;
            eye.Click += (_, _) =>
            {
                shown = !shown;
                value.Text = shown ? password : masked;
                eye.ToolTip = Loc.T(shown ? "Wifi.HidePassword" : "Wifi.ShowPassword");
            };
            AddInsetRow(Loc.T("Wifi.Password"), value, eye);

            SetPrimary("Icon.Copy", Loc.T("Wifi.CopyPassword"), () => Copy(password, PrimaryButton, PrimaryIcon, PrimaryText));
            SetSecondary("Icon.Copy", Loc.T("Wifi.CopyName"), () => Copy(ssid, SecondaryButton, SecondaryIcon, SecondaryText));
        }
        else
        {
            SetPrimary("Icon.Copy", Loc.T("Wifi.CopyName"), () => Copy(ssid, PrimaryButton, PrimaryIcon, PrimaryText));
            HideSecondary();
        }
    }

    private void ShowContact(Payload payload)
    {
        var meta = payload.Metadata ?? new Dictionary<string, string>();
        var name = meta.GetValueOrDefault("fn", meta.GetValueOrDefault("name", Loc.T("Contact.Unnamed")));
        var org = meta.GetValueOrDefault("org", "");
        var tel = meta.GetValueOrDefault("tel", "");
        var email = meta.GetValueOrDefault("email", "");

        SetTile("Info", "Icon.Person");
        TypeLabel.Text = Loc.T("Contact.Type");
        TitleText.Text = name;
        SubtitleText.FontFamily = (FontFamily)FindResource("Tunkoz.Font.UI");
        SubtitleText.Text = org;
        if (string.IsNullOrEmpty(org))
            SubtitleText.Visibility = Visibility.Collapsed;

        if (!string.IsNullOrEmpty(tel))
        {
            var copyTel = MakeIconButton("Icon.Copy", Loc.T("Contact.CopyPhone"));
            copyTel.Click += (_, _) => Copy(tel, null, null, null);
            AddInsetRow(null, MakeText(tel, mono: false, size: 14), copyTel);
        }
        if (!string.IsNullOrEmpty(email))
        {
            var copyMail = MakeIconButton("Icon.Copy", Loc.T("Contact.CopyEmail"));
            copyMail.Click += (_, _) => Copy(email, null, null, null);
            AddInsetRow(null, MakeText(email, mono: false, size: 14), copyMail);
        }

        SetPrimary("Icon.Download", Loc.T("Contact.Save"), () => SaveVCard(payload.Content, name));
        HideSecondary();
    }

    private void ShowSimple(Payload payload, string icon, string typeKey, string value, string openKey)
    {
        SetTile("Info", icon);
        TypeLabel.Text = Loc.T(typeKey);
        TitleText.Text = value;
        SubtitleText.Visibility = Visibility.Collapsed;
        SetPrimary("Icon.OpenExternal", Loc.T(openKey), () => Open(payload.Content));
        SetSecondary("Icon.Copy", Loc.T("Common.Copy"), () => Copy(value, SecondaryButton, SecondaryIcon, SecondaryText));
    }

    private void ShowText(Payload payload)
    {
        var text = payload.Content;
        SetTile("Info", "Icon.Text");
        TypeLabel.Text = Loc.F("Text.Type", text.Length);
        TitleText.Visibility = Visibility.Collapsed;
        SubtitleText.Visibility = Visibility.Collapsed;
        BodyText.Text = text;
        BodyText.Visibility = Visibility.Visible;
        SetPrimary("Icon.Copy", Loc.T("Card.CopyText"), () => Copy(text, PrimaryButton, PrimaryIcon, PrimaryText));
        HideSecondary();
    }

    // ---------------------------------------------------------------- building blocks

    private void SetTile(string kind, string iconKey)
    {
        TypeTile.SetResourceReference(Border.BackgroundProperty, $"Tunkoz.Brush.Tile.{kind}.Background");
        TypeIcon.SetResourceReference(TextElement.ForegroundProperty, $"Tunkoz.Brush.Tile.{kind}.Foreground");
        TypeIcon.Data = (Geometry)FindResource(iconKey);
    }

    private void ShowBanner(string kind, string iconKey, string title, string body)
    {
        Banner.Visibility = Visibility.Visible;
        Banner.SetResourceReference(Border.BackgroundProperty, $"Tunkoz.Brush.{kind}.Background");
        Banner.SetResourceReference(Border.BorderBrushProperty, $"Tunkoz.Brush.{kind}.Stroke");
        BannerIcon.Data = (Geometry)FindResource(iconKey);
        BannerIcon.SetResourceReference(TextElement.ForegroundProperty, $"Tunkoz.Brush.{kind}.Icon");
        BannerTitle.Text = title;
        BannerTitle.SetResourceReference(TextBlock.ForegroundProperty, $"Tunkoz.Brush.{kind}.Foreground");
        BannerBody.Text = body;
        BannerBody.SetResourceReference(TextBlock.ForegroundProperty, $"Tunkoz.Brush.{kind}.Body");
    }

    private void SetPrimary(string iconKey, string label, Action action)
    {
        PrimaryIcon.Data = (Geometry)FindResource(iconKey);
        PrimaryText.Text = label;
        _primaryAction = action;
    }

    private void SetSecondary(string iconKey, string label, Action action)
    {
        SecondaryIcon.Data = (Geometry)FindResource(iconKey);
        SecondaryText.Text = label;
        _secondaryAction = action;
    }

    private void HideSecondary() => SecondaryButton.Visibility = Visibility.Collapsed;

    private TextBlock MakeText(string text, bool mono, double size)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = size,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            FontFamily = (FontFamily)FindResource(mono ? "Tunkoz.Font.Mono" : "Tunkoz.Font.UI"),
        };
        tb.SetResourceReference(TextBlock.ForegroundProperty, "Tunkoz.Brush.Text.Primary");
        return tb;
    }

    private Button MakeIconButton(string iconKey, string tooltip)
    {
        var path = new Path { Data = (Geometry)FindResource(iconKey), Style = (Style)FindResource("TunkozIcon") };
        return new Button
        {
            Style = (Style)FindResource("TunkozGhostIconButton"),
            Content = path,
            ToolTip = tooltip,
        };
    }

    /// <summary>40 px inset row: optional 40 px label, value, trailing 32 px icon button.</summary>
    private void AddInsetRow(string? label, TextBlock value, Button action)
    {
        InsetHost.Visibility = Visibility.Visible;

        if (InsetRows.Children.Count > 0)
        {
            var sep = new Border { Height = 1, Margin = new Thickness(12, 0, 12, 0) };
            sep.SetResourceReference(Border.BackgroundProperty, "Tunkoz.Brush.Stroke.Inset");
            InsetRows.Children.Add(sep);
        }

        var row = new Grid { Height = 40, Margin = new Thickness(12, 0, 4, 0) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        if (label != null)
        {
            var lbl = new TextBlock
            {
                Text = label,
                Width = 50,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = (FontFamily)FindResource("Tunkoz.Font.UI"),
            };
            lbl.SetResourceReference(TextBlock.ForegroundProperty, "Tunkoz.Brush.Text.Secondary");
            row.Children.Add(lbl);
        }

        Grid.SetColumn(value, 1);
        row.Children.Add(value);
        Grid.SetColumn(action, 2);
        action.VerticalAlignment = VerticalAlignment.Center;
        row.Children.Add(action);
        InsetRows.Children.Add(row);
    }

    // ---------------------------------------------------------------- actions

    private void OnPrimaryClick(object sender, RoutedEventArgs e) => _primaryAction?.Invoke();

    private void OnSecondaryClick(object sender, RoutedEventArgs e) => _secondaryAction?.Invoke();

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    /// <summary>Copies text and shows the inline "Copied" state on the button that did it (no toast).</summary>
    private void Copy(string text, Button? button, Path? icon, TextBlock? label)
    {
        try
        {
            Clipboard.SetText(text);
        }
        catch
        {
            return; // clipboard busy; nothing to confirm
        }

        if (button == null || icon == null || label == null)
            return;

        var oldIcon = icon.Data;
        var oldText = label.Text;
        icon.Data = (Geometry)FindResource("Icon.Check");
        label.Text = Loc.T("Card.Copied");
        button.SetResourceReference(BackgroundProperty, "Tunkoz.Brush.Success.Background");
        button.SetResourceReference(BorderBrushProperty, "Tunkoz.Brush.Success.Stroke");
        button.SetResourceReference(ForegroundProperty, "Tunkoz.Brush.Success.Foreground");
        button.BorderThickness = new Thickness(1);

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.6) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            icon.Data = oldIcon;
            label.Text = oldText;
            button.ClearValue(BackgroundProperty);
            button.ClearValue(BorderBrushProperty);
            button.ClearValue(ForegroundProperty);
            button.ClearValue(BorderThicknessProperty);
        };
        timer.Start();
    }

    private void Open(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
        }
        catch
        {
            // Nothing registered for this scheme; keep the card open so the user can copy instead
            return;
        }
        Close();
    }

    private void SaveVCard(string content, string name)
    {
        _dismissClock?.Controller?.Pause();
        var safeName = string.Concat(name.Split(System.IO.Path.GetInvalidFileNameChars())).Trim();
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = string.IsNullOrEmpty(safeName) ? "contact" : safeName,
            DefaultExt = ".vcf",
            Filter = "vCard (*.vcf)|*.vcf",
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var vcf = content.StartsWith("BEGIN:VCARD", StringComparison.OrdinalIgnoreCase)
                    ? content
                    : MeCardToVCard(content, name);
                File.WriteAllText(dialog.FileName, vcf);
                Close();
                return;
            }
            catch
            {
                // fall through and keep the card
            }
        }
        _dismissClock?.Controller?.Resume();
    }

    private static string MeCardToVCard(string mecard, string name)
    {
        string Field(string key)
        {
            var i = mecard.IndexOf(key + ":", StringComparison.OrdinalIgnoreCase);
            if (i < 0) return "";
            var start = i + key.Length + 1;
            var end = mecard.IndexOf(';', start);
            return (end < 0 ? mecard[start..] : mecard[start..end]).Trim();
        }

        var lines = new List<string> { "BEGIN:VCARD", "VERSION:3.0", $"FN:{name}" };
        var tel = Field("TEL");
        var email = Field("EMAIL");
        if (tel.Length > 0) lines.Add($"TEL:{tel}");
        if (email.Length > 0) lines.Add($"EMAIL:{email}");
        lines.Add("END:VCARD");
        return string.Join("\r\n", lines) + "\r\n";
    }

    private static string StripScheme(string value, string scheme) =>
        value.StartsWith(scheme, StringComparison.OrdinalIgnoreCase) ? value[scheme.Length..] : value;

    // ---------------------------------------------------------------- dismiss

    private void StartDismissTimer()
    {
        var anim = new DoubleAnimation(1, 0, new Duration(DismissAfter));
        _dismissClock = anim.CreateClock();
        _dismissClock.Completed += (_, _) =>
        {
            if (!_closing) Close();
        };
        DismissScale.ApplyAnimationClock(ScaleTransform.ScaleXProperty, _dismissClock);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _closing = true;
        _dismissClock?.Controller?.Stop();
        base.OnClosed(e);
    }
}
