using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace JipperKeyViewer.Core;


public sealed class KeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int LLKHF_INJECTED = 0x10;
    private const int LLKHF_UP = 0x80;

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public nuint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public nint hwnd;
        public uint message;
        public nint wParam;
        public nint lParam;
        public uint time;
        public int ptX;
        public int ptY;
    }

    private delegate nint LowLevelKeyboardProc(int nCode, nint wParam, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookExW(int idHook, LowLevelKeyboardProc lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetMessageW(out MSG msg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref MSG msg);

    [DllImport("user32.dll")]
    private static extern nint DispatchMessageW(ref MSG msg);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostThreadMessageW(uint idThread, uint msg, nint wParam, nint lParam);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint GetModuleHandleW(string? name);

    private const uint WM_QUIT = 0x0012;

    private readonly LowLevelKeyboardProc _proc;
    private nint _hook = 0;
    private volatile bool _enabled = true;

    private Thread? _hookThread;
    private uint _hookThreadId;


    public readonly ConcurrentDictionary<int, bool> Pressed = new();


    private readonly ConcurrentQueue<long> _kpsTimes = new();


    private long _totalCount;


    private readonly ConcurrentDictionary<int, long> _keyCounts = new();


    private readonly ConcurrentDictionary<int, ConcurrentQueue<long>> _keyPressTimes = new();
    private volatile bool _trackPerKeyKps;


    public void SetPerKeyKpsEnabled(bool enabled)
    {
        _trackPerKeyKps = enabled;
        if (enabled) return;
        foreach (var q in _keyPressTimes.Values)
            while (q.TryDequeue(out _)) { }
        _keyPressTimes.Clear();
    }


    private volatile bool _capturing;
    private volatile bool _captureDone;
    private volatile bool _captureCancelled;
    private volatile int _captureVk;


    private volatile HashSet<int> _registeredVks = new();


    public event Action<KeyCode>? KeyDown;
    public event Action<KeyCode>? KeyUp;


    public event Action<int, bool>? GhostKeyChanged;


    private volatile Dictionary<int, int> _ghostVks = new();
    private readonly ConcurrentDictionary<int, bool> _ghostPressed = new();


    public void RegisterGhostKeys(IEnumerable<KeyCode> ghostKeys)
    {
        var map = new Dictionary<int, int>();
        int idx = 0;
        foreach (var k in ghostKeys)
        {
            int vk = k.ToVk();
            if (vk != 0 && !map.ContainsKey(vk)) map[vk] = idx;
            idx++;
        }
        _ghostVks = map;
        _ghostPressed.Clear();
    }

    public KeyboardHook()
    {
        _proc = HookProc;
    }


    public void RegisterKeys(IEnumerable<KeyCode> keys)
    {
        var set = new HashSet<int>();
        foreach (var k in keys)
        {
            int vk = k.ToVk();
            if (vk != 0) set.Add(vk);
        }
        _registeredVks = set;

        foreach (var vk in Pressed.Keys.ToList())
            if (!set.Contains(vk)) Pressed.TryRemove(vk, out _);
    }

    public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            if (!value) Pressed.Clear();
        }
    }


    public bool Install()
    {
        if (_hook != 0) return true;
        _hookThread = new Thread(HookThreadMain)
        {
            IsBackground = true,
            Name = "KeyboardHook",
        };
        _hookThread.Start();

        for (int i = 0; i < 100 && _hook == 0 && _hookThread.IsAlive; i++)
            Thread.Sleep(10);
        return _hook != 0;
    }

    private void HookThreadMain()
    {
        _hookThreadId = GetCurrentThreadId();
        nint mod = GetModuleHandleW(null);
        _hook = SetWindowsHookExW(WH_KEYBOARD_LL, _proc, mod, 0);
        if (_hook == 0)
            return;


        while (GetMessageW(out MSG msg, IntPtr.Zero, 0, 0) > 0)
        {
            TranslateMessage(ref msg);
            DispatchMessageW(ref msg);
        }
        _hook = 0;
    }

    public void Uninstall()
    {
        if (_hook != 0)
        {
            UnhookWindowsHookEx(_hook);
            _hook = 0;
        }
        if (_hookThread != null && _hookThreadId != 0)
        {
            PostThreadMessageW(_hookThreadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            _hookThread = null;
            _hookThreadId = 0;
        }
        Pressed.Clear();
    }


    // Runs on the hook thread: keep it allocation-light and never render here.
    // Windows waits for this callback before delivering keys to the game.
    private nint HookProc(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0 && _enabled)
        {
            KBDLLHOOKSTRUCT kbd = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            bool isUp = (kbd.flags & LLKHF_UP) != 0;
            int vk = (int)kbd.vkCode;


            if (_capturing)
            {
                if (!isUp)
                {
                    if (vk == 0x1B) { _captureCancelled = true; _captureDone = true; }
                    else { _captureVk = vk; _captureDone = true; }
                }
                return CallNextHookEx(_hook, nCode, wParam, lParam);
            }

            KeyCode kc = KeyCodeHelper.FromVk(vk);


            if (kc == KeyCode.None || !_registeredVks.Contains(vk))
            {
                if (_ghostVks.TryGetValue(vk, out int gi))
                {
                    bool nowGhost = !isUp;
                    bool prevGhost = _ghostPressed.TryGetValue(vk, out bool gp) && gp;
                    if (nowGhost != prevGhost)
                    {
                        _ghostPressed[vk] = nowGhost;
                        GhostKeyChanged?.Invoke(gi, nowGhost);
                    }
                }
            }


            if (kc != KeyCode.None && _registeredVks.Contains(vk))
            {
                bool nowPressed = !isUp;
                bool prev = Pressed.TryGetValue(vk, out bool p) && p;
                if (nowPressed != prev)
                {
                    Pressed[vk] = nowPressed;
                    if (nowPressed)
                    {
                        _kpsTimes.Enqueue(Environment.TickCount64);
                        if (_trackPerKeyKps)
                            _keyPressTimes.GetOrAdd(vk, _ => new ConcurrentQueue<long>())
                                .Enqueue(Environment.TickCount64);
                        Interlocked.Increment(ref _totalCount);
                        _keyCounts.AddOrUpdate(vk, 1L, (_, c) => c + 1L);
                        KeyDown?.Invoke(kc);
                    }
                    else
                    {
                        KeyUp?.Invoke(kc);
                    }
                }
            }
        }
        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }


    public int Kps
    {
        get
        {
            long now = Environment.TickCount64;
            while (_kpsTimes.TryPeek(out long t) && now - t > 1000)
                _kpsTimes.TryDequeue(out _);
            return _kpsTimes.Count;
        }
    }


    public long TotalCount => Interlocked.Read(ref _totalCount);


    public int KpsQueueLength => _kpsTimes.Count;


    public int PerKeyQueueLength
    {
        get
        {
            int total = 0;
            foreach (var q in _keyPressTimes.Values) total += q.Count;
            return total;
        }
    }


    public long GetKeyCount(KeyCode key)
    {
        int vk = key.ToVk();
        if (vk == 0) return 0;
        return _keyCounts.TryGetValue(vk, out long c) ? c : 0L;
    }


    public int GetPerKeyKps(KeyCode key)
    {
        int vk = key.ToVk();
        if (vk == 0) return 0;
        if (_keyPressTimes.TryGetValue(vk, out var q))
        {
            long now = Environment.TickCount64;
            while (q.TryPeek(out long t) && now - t > 1000)
                q.TryDequeue(out _);
            return q.Count;
        }
        return 0;
    }


    public void RestoreCounts(Dictionary<int, long> counts, long total)
    {
        if (counts == null) return;
        foreach (var kv in counts)
        {
            if (kv.Value > 0) _keyCounts[kv.Key] = kv.Value;
        }
        Interlocked.Exchange(ref _totalCount, Math.Max(0L, total));
    }


    public void ResetCounts()
    {
        _keyCounts.Clear();
        Interlocked.Exchange(ref _totalCount, 0L);
    }


    public void ResetKeyCount(KeyCode key)
    {
        int vk = key.ToVk();
        if (vk == 0) return;
        if (_keyCounts.TryRemove(vk, out long removed) && removed > 0)
            Interlocked.Add(ref _totalCount, -removed);
    }


    public Dictionary<int, long> ExportKeyCounts()
    {
        var d = new Dictionary<int, long>();
        foreach (var kv in _keyCounts)
        {
            if (kv.Value > 0) d[kv.Key] = kv.Value;
        }
        return d;
    }


    public bool IsCapturing => _capturing;


    public void BeginCapture()
    {
        _captureVk = 0;
        _captureDone = false;
        _captureCancelled = false;
        _capturing = true;
    }


    public void CancelCapture()
    {
        _capturing = false;
        _captureDone = false;
    }


    public bool? TakeCaptureResult(out int vk)
    {
        vk = 0;
        if (!_captureDone) return null;
        _capturing = false;
        if (_captureCancelled) return false;
        vk = _captureVk;
        return true;
    }

    public void Dispose() => Uninstall();
}
