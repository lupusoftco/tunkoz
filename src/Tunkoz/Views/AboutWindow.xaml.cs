using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace Tunkoz.Views
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnGitHubClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/lupusoftco/tunkoz",
                    UseShellExecute = true
                });
            }
            catch
            {
                // Silently fail if cannot open
            }
        }
    }
}
