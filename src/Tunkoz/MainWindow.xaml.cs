using System.Windows;
using Tunkoz.Services;
using Tunkoz.Views;

namespace Tunkoz;

public partial class MainWindow : Window
{
    private readonly QrDetectionPipeline _pipeline;
    private DebugConsoleWindow? _debugConsole;

    public MainWindow()
    {
        InitializeComponent();
        _pipeline = new QrDetectionPipeline();
        _pipeline.Start();
    }

    private void OnDebugClick(object sender, RoutedEventArgs e)
    {
        if (_debugConsole?.IsVisible == true)
        {
            _debugConsole.Focus();
        }
        else
        {
            _debugConsole = new DebugConsoleWindow(_pipeline);
            _debugConsole.Show();
        }
    }

    private void OnExitClick(object sender, RoutedEventArgs e)
    {
        _debugConsole?.Close();
        _pipeline.Dispose();
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _pipeline.Dispose();
        base.OnClosed(e);
    }
}
