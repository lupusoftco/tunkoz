using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Animation;

namespace Tunkoz.Views
{
    public partial class HighlightOverlay : Window
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_TRANSPARENT = 0x00000020;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public HighlightOverlay()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);
        }

        public void ShowAroundQR(System.Drawing.Rectangle bounds)
        {
            Left = bounds.X - 1;
            Top = bounds.Y - 1;
            Width = bounds.Width + 2;
            Height = bounds.Height + 2;

            Show();
            FadeOutAndClose();
        }

        private void FadeOutAndClose()
        {
            var fadeOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(300)
            };

            fadeOut.Completed += (s, e) =>
            {
                Close();
            };

            BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
