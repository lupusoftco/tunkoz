using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace Tunkoz.Services;

public class MouseHookService : IDisposable
{
    private const int WH_MOUSE_LL = 14;
    private const int WM_RBUTTONUP = 0x0205;
    private const int LLMHF_INJECTED = 0x00000001;

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    private IntPtr _hookHandle = IntPtr.Zero;
    private LowLevelMouseProc? _hookProc;
    private readonly Channel<MouseClickEventArgs> _clickChannel;
    private Thread? _hookThread;
    private volatile bool _running = false;
    private readonly Queue<(int x, int y, uint time)> _recentClicks = new();
    private const int DebounceMs = 100;

    public event EventHandler<MouseClickEventArgs>? RightClickDetected;

    public MouseHookService()
    {
        _clickChannel = Channel.CreateUnbounded<MouseClickEventArgs>();
    }

    public void Start()
    {
        if (_running)
            return;

        _running = true;
        _hookThread = new Thread(SetupHook)
        {
            Name = "MouseHookThread",
            IsBackground = false
        };
        _hookThread.Start();

        // Start consumer task
        _ = ConsumeClicksAsync();
    }

    public void Stop()
    {
        _running = false;
        _hookThread?.Join(1000);
    }

    private void SetupHook()
    {
        _hookProc = MouseHookCallback;

        using var kernel = GetModuleHandle(null);
        _hookHandle = SetWindowsHookEx(WH_MOUSE_LL, _hookProc, kernel, 0);

        if (_hookHandle == IntPtr.Zero)
        {
            System.Diagnostics.Debug.WriteLine("Failed to set mouse hook");
            _running = false;
            return;
        }

        // Message loop for this thread
        while (_running)
        {
            Thread.Sleep(10);
        }

        if (_hookHandle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_RBUTTONUP)
        {
            try
            {
                var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

                // Ignore injected events
                if ((hookStruct.flags & LLMHF_INJECTED) != 0)
                    return CallNextHookEx(_hookHandle, nCode, wParam, lParam);

                // Debounce: ignore clicks within 100ms of last click at same position
                if (IsClickDuplicate(hookStruct.pt.x, hookStruct.pt.y, hookStruct.time))
                    return CallNextHookEx(_hookHandle, nCode, wParam, lParam);

                var args = new MouseClickEventArgs
                {
                    X = hookStruct.pt.x,
                    Y = hookStruct.pt.y,
                    Timestamp = DateTime.UtcNow
                };

                // Post to channel without blocking the hook
                _ = _clickChannel.Writer.TryWrite(args);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Mouse hook error: {ex.Message}");
            }
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private bool IsClickDuplicate(int x, int y, uint time)
    {
        lock (_recentClicks)
        {
            // Remove old clicks (older than 200ms)
            while (_recentClicks.Count > 0)
            {
                var oldest = _recentClicks.Peek();
                if (unchecked(time - oldest.Item3) > 200)
                {
                    _recentClicks.Dequeue();
                }
                else
                {
                    break;
                }
            }

            // Check for duplicate
            foreach (var click in _recentClicks)
            {
                if (click.x == x && click.y == y && unchecked(time - click.Item3) < DebounceMs)
                {
                    return true;
                }
            }

            _recentClicks.Enqueue((x, y, time));
            return false;
        }
    }

    private async Task ConsumeClicksAsync()
    {
        await foreach (var click in _clickChannel.Reader.ReadAllAsync())
        {
            RightClickDetected?.Invoke(this, click);
        }
    }

    public void Dispose()
    {
        Stop();
        _clickChannel.Writer.Complete();
    }
}

public class MouseClickEventArgs : EventArgs
{
    public int X { get; set; }
    public int Y { get; set; }
    public DateTime Timestamp { get; set; }
}
