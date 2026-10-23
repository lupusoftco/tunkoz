using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace Tunkoz.Services;

/// <summary>
/// Observes right-clicks system-wide via a low-level mouse hook.
/// The hook runs on a dedicated thread with its own Win32 message loop
/// (required: LL hooks are only called while the installing thread pumps messages).
/// The callback does no work besides posting to a channel and calling CallNextHookEx.
/// </summary>
public class MouseHookService : IDisposable
{
    private const int WH_MOUSE_LL = 14;
    private const int WM_RBUTTONUP = 0x0205;
    private const uint WM_QUIT = 0x0012;
    private const uint LLMHF_INJECTED = 0x00000001;

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

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
        public uint lPrivate;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostThreadMessage(uint idThread, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    private IntPtr _hookHandle = IntPtr.Zero;
    private LowLevelMouseProc? _hookProc; // kept alive so the GC doesn't collect the delegate
    private readonly Channel<MouseClickEventArgs> _clickChannel;
    private Thread? _hookThread;
    private volatile uint _hookThreadId;
    private volatile bool _running;
    private bool _consumerStarted;
    private readonly Queue<(int x, int y, uint time)> _recentClicks = new();
    private const int DebounceMs = 100;

    public event EventHandler<MouseClickEventArgs>? RightClickDetected;

    public MouseHookService()
    {
        _clickChannel = Channel.CreateUnbounded<MouseClickEventArgs>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
    }

    public void Start()
    {
        if (_running)
            return;

        _running = true;
        _hookThread = new Thread(HookThreadMain)
        {
            Name = "MouseHookThread",
            IsBackground = true
        };
        _hookThread.Start();

        if (!_consumerStarted)
        {
            _consumerStarted = true;
            _ = Task.Run(ConsumeClicksAsync);
        }
    }

    public void Stop()
    {
        if (!_running)
            return;

        _running = false;
        var threadId = _hookThreadId;
        if (threadId != 0)
            PostThreadMessage(threadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);

        _hookThread?.Join(1000);
        _hookThread = null;
        _hookThreadId = 0;
    }

    private void HookThreadMain()
    {
        _hookThreadId = GetCurrentThreadId();
        _hookProc = MouseHookCallback;

        var hModule = GetModuleHandle(null);
        _hookHandle = SetWindowsHookEx(WH_MOUSE_LL, _hookProc, hModule, 0);

        if (_hookHandle == IntPtr.Zero)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to set mouse hook, error {Marshal.GetLastWin32Error()}");
            _running = false;
            return;
        }

        // Standard message loop. GetMessage returns 0 on WM_QUIT, -1 on error.
        while (GetMessage(out _, IntPtr.Zero, 0, 0) > 0)
        {
            // No windows on this thread; nothing to translate or dispatch.
        }

        UnhookWindowsHookEx(_hookHandle);
        _hookHandle = IntPtr.Zero;
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_RBUTTONUP)
        {
            try
            {
                var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

                if ((hookStruct.flags & LLMHF_INJECTED) == 0 &&
                    !IsClickDuplicate(hookStruct.pt.x, hookStruct.pt.y, hookStruct.time))
                {
                    _clickChannel.Writer.TryWrite(new MouseClickEventArgs
                    {
                        X = hookStruct.pt.x,
                        Y = hookStruct.pt.y,
                        Timestamp = DateTime.UtcNow
                    });
                }
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
        while (_recentClicks.Count > 0 && unchecked(time - _recentClicks.Peek().time) > 200)
            _recentClicks.Dequeue();

        foreach (var click in _recentClicks)
        {
            if (click.x == x && click.y == y && unchecked(time - click.time) < DebounceMs)
                return true;
        }

        _recentClicks.Enqueue((x, y, time));
        return false;
    }

    private async Task ConsumeClicksAsync()
    {
        await foreach (var click in _clickChannel.Reader.ReadAllAsync())
        {
            try
            {
                RightClickDetected?.Invoke(this, click);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Right-click handler error: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        Stop();
        _clickChannel.Writer.TryComplete();
    }
}

public class MouseClickEventArgs : EventArgs
{
    public int X { get; set; }
    public int Y { get; set; }
    public DateTime Timestamp { get; set; }
}
