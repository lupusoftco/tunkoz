using System.Windows;
using System.Windows.Controls;
using Tunkoz.Services;

namespace Tunkoz.Views;

public partial class SettingsWindow : Window
{
    private bool _loading;

    public SettingsWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => ThemeService.ApplyTitleBar(this);
        Loc.Instance.LanguageChanged += OnUiLanguageChanged;
        FillLanguages();
    }

    private void FillLanguages()
    {
        _loading = true;
        LanguageBox.Items.Clear();
        LanguageBox.Items.Add(new ComboBoxItem { Content = Loc.T("Settings.Language.System"), Tag = "system" });
        LanguageBox.Items.Add(new ComboBoxItem { Content = "Türkçe", Tag = "tr" });
        LanguageBox.Items.Add(new ComboBoxItem { Content = "English", Tag = "en" });
        foreach (ComboBoxItem item in LanguageBox.Items)
        {
            if ((string)item.Tag == SettingsStore.Current.Language)
                LanguageBox.SelectedItem = item;
        }
        LanguageBox.SelectedIndex = LanguageBox.SelectedIndex < 0 ? 0 : LanguageBox.SelectedIndex;
        _loading = false;
    }

    private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || LanguageBox.SelectedItem is not ComboBoxItem item)
            return;

        SettingsStore.Current.Language = (string)item.Tag;
        SettingsStore.Save();
        Loc.Instance.Language = SettingsStore.EffectiveLanguage;
    }

    private void OnUiLanguageChanged(object? sender, EventArgs e)
    {
        // Re-label the "Windows language" entry in the new language
        if (LanguageBox.Items.Count > 0 && LanguageBox.Items[0] is ComboBoxItem first)
            first.Content = Loc.T("Settings.Language.System");
    }

    private void OnMinimize(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnClose(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        Loc.Instance.LanguageChanged -= OnUiLanguageChanged;
        base.OnClosed(e);
    }
}
