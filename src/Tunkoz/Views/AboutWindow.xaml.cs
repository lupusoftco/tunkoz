using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;
using Tunkoz.Services;

namespace Tunkoz.Views;

public partial class AboutWindow : Window
{
    private const string RepoUrl = "https://github.com/lupusoftco/tunkoz";

    public AboutWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => ThemeService.ApplyTitleBar(this);
        UpdateVersion();
        Loc.Instance.LanguageChanged += OnLanguageChanged;
    }

    private void UpdateVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        var text = version == null ? "" : $"{version.Major}.{version.Minor}.{version.Build}";
        VersionText.Text = Loc.F("About.Version", text);
    }

    private void OnLanguageChanged(object? sender, EventArgs e) => UpdateVersion();

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
        catch { /* no browser registered */ }
    }

    private void OnLink(object sender, RequestNavigateEventArgs e)
    {
        OpenUrl(e.Uri.AbsoluteUri);
        e.Handled = true;
    }

    private void OnGitHubClick(object sender, RoutedEventArgs e) => OpenUrl(RepoUrl);

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        Loc.Instance.LanguageChanged -= OnLanguageChanged;
        base.OnClosed(e);
    }
}
