using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using Tunkoz.Services;
using CoreRect = Tunkoz.Core.Models.Rectangle;

namespace Tunkoz.Views
{
    public partial class HighlightOverlay : Window
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public HighlightOverlay()
        {
            InitializeComponent();
            ShowActivated = false;
            SourceInitialized += OnSourceInitialized;
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            // Click-through and never activated. Applied before the window is first shown.
            var hwnd = new WindowInteropHelper(this).Handle;
            var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
        }

        /// <summary>Outlines a QR given in physical screen pixels, then fades out (~300 ms).</summary>
        public void ShowAroundQR(CoreRect? bounds)
        {
            if (bounds is not { } rect || rect.Width <= 0 || rect.Height <= 0)
            {
                Close();
                return;
            }

            WindowPlacementHelper.PlaceOverRect(this, rect, paddingPx: 12);
            Show();

            var fadeOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                BeginTime = TimeSpan.FromMilliseconds(250),
                Duration = TimeSpan.FromMilliseconds(300)
            };
            fadeOut.Completed += (s, e) => Close();
            BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
