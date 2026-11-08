using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using JipperKeyViewer.App;
using JipperKeyViewer.Config;
using JipperKeyViewer.Core;
using JipperKeyViewer.Graphics;
using JipperKeyViewer.Render;
using JipperKeyViewer.UI;

namespace JipperKeyViewer;


internal static partial class Program
{

    private delegate nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam);

    private const uint WS_POPUP = 0x80000000;
    private const uint WS_VISIBLE = 0x10000000;
    private const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const uint WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_TOPMOST = 0x00000008;
    private const int SWP_NOSIZE = 0x0001;
    private const int SWP_NOMOVE = 0x0002;
    private const int SWP_NOZORDER = 0x0004;
    private const int SWP_NOACTIVATE = 0x0010;
    private const uint WM_DESTROY = 0x0002;
    private const uint WM_MOVE = 0x0003;
    private const uint WM_SIZE = 0x0005;
    private const uint WM_LBUTTONDOWN = 0x0201;
    private const uint WM_CONTEXTMENU = 0x007B;
    private const uint WM_NCLBUTTONDOWN = 0x00A1;
    private const uint WM_QUIT = 0x0012;
    private const uint WM_DROPFILES = 0x0233;
    private const uint WM_COPYDATA = 0x004A;
    private const uint WM_APP_REFRESH = 0x8001;
    private const uint WM_HOTKEY = 0x0312;
    private const uint PM_REMOVE = 0x0001;
    private const int HTCAPTION = 2;
    private const int CW_USEDEFAULT = unchecked((int)0x80000000);
    private const int SW_RESTORE = 9;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEX
    {
        public uint cbSize;
        public uint style;
        public nint lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public nint hInstance;
        public nint hIcon;
        public nint hCursor;
        public nint hbrBackground;
        public string lpszMenuName;
        public string lpszClassName;
        public nint hIconSm;
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

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [DllImport("user32.dll")]
    private static extern bool AdjustWindowRectEx(ref RECT rect, uint style, bool menu, int exStyle);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassExW(ref WNDCLASSEX wc);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowExW(
        int exStyle, string className, string windowName, uint style,
        int x, int y, int w, int h, nint parent, nint menu, nint instance, nint param);

    [DllImport("user32.dll")]
    private static extern nint DefWindowProcW(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandleW(string? name);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern void PostQuitMessage(int exitCode);

    [DllImport("user32.dll")]
    private static extern bool PeekMessageW(out MSG msg, nint hWnd, uint filterMin, uint filterMax, uint flags);
    [DllImport("user32.dll")]
    private static extern bool PostMessageW(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("imm32.dll")]
    private static extern bool ImmAssociateContext(nint hWnd, nint hIMC);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref MSG msg);

    [DllImport("user32.dll")]
    private static extern nint DispatchMessageW(ref MSG msg);

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern nint SendMessageW(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint SendMessageW(nint hWnd, uint msg, nint wParam, ref COPYDATASTRUCT lParam);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(nint hWnd, nint after, int x, int y, int cx, int cy, uint flags);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(nint hWnd, out RECT rect);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(nint hWnd, out RECT rect);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(nint hWnd, ref POINT pt);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int ExtractIconExW(string szFile, int nIconIndex, out nint phiconLarge, out nint phiconSmall, uint nIcons);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint FindWindowW(string? className, string? windowName);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);


    [DllImport("shell32.dll")]
    private static extern void DragAcceptFiles(nint hWnd, bool fAccept);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern uint DragQueryFile(nint hDrop, uint iFile, System.Text.StringBuilder? lpszFile, uint cch);

    [DllImport("shell32.dll")]
    private static extern void DragFinish(nint hDrop);


    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int wEventId, uint uFlags, nint dwItem1, nint dwItem2);
    private const int SHCNE_ASSOCCHANGED = 0x08000000;
    private const int SHCNE_FLUSH = 0x1000;
    private const uint SHCNF_IDLIST = 0x0;


    [StructLayout(LayoutKind.Sequential)]
    private struct COPYDATASTRUCT
    {
        public nint dwData;
        public uint cbData;
        public nint lpData;
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);


    private static nint LoadExeIconLarge()
    {
        try
        {
            string exePath = Environment.ProcessPath ?? string.Empty;
            if (exePath.Length > 0 && ExtractIconExW(exePath, 0, out nint big, out nint small, 1) > 0 && big != IntPtr.Zero)
                return big;
        }
        catch (Exception ex) { Graphics.GraphicsLog.Write("提取 exe 图标失败: " + ex.Message); }
        return IntPtr.Zero;
    }


    private static Stream? OpenAppIconStream()
    {
        try
        {
            var asm = typeof(Program).Assembly;
            string? res = asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("app.ico", StringComparison.OrdinalIgnoreCase));
            return res == null ? null : asm.GetManifestResourceStream(res);
        }
        catch (Exception ex) { Graphics.GraphicsLog.Write("打开内嵌图标失败: " + ex.Message); return null; }
    }


    private static System.Drawing.Icon LoadTrayIcon()
    {
        try
        {
            using var s = OpenAppIconStream();
            if (s != null) return new System.Drawing.Icon(s);
        }
        catch (Exception ex) { Graphics.GraphicsLog.Write("加载托盘图标失败: " + ex.Message); }
        return SystemIcons.Application;
    }


    private static System.Windows.Media.Imaging.BitmapFrame? LoadSettingsIcon()
    {
        try
        {
            using var s = OpenAppIconStream();
            if (s != null) return BitmapFrame.Create(s, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        }
        catch (Exception ex) { Graphics.GraphicsLog.Write("加载设置窗口图标失败: " + ex.Message); }
        return null;
    }

    private static readonly WndProc WndProcDelegate = WindowProc;
    private static readonly string ClassName = "JipperKeyViewerOverlay";

    private static nint _hwnd;
    private static KeyboardHook? _hook;
    private static AppConfig? _cfg;
    private static KeyRenderer? _keyRenderer;
    private static TransparentRenderer? _renderer;


    private static readonly WindowState Window = new();
    private static readonly HotkeyService SettingsHotkey = new();

    private static volatile int _captureKind = -1;
    private static volatile int _captureIndex = -1;
    private static volatile bool _captureArmed;
    private static readonly TrayService Tray = new();

    private static void WriteCrashLog(Exception? ex)
    {
        try
        {
            string dir = Path.GetDirectoryName(Environment.ProcessPath) ?? Environment.CurrentDirectory;
            string log = Path.Combine(dir, "JipperKeyViewer-crash.log");
            string text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\n" +
                          $"ProcessPath={Environment.ProcessPath}\nOS={Environment.OSVersion}";
            File.AppendAllText(log, text + "\n" + new string('-', 60) + "\n");
        }
        catch { }
    }


    private static (int W, int H) ComputeOuterSize(int clientW, int clientH)
    {
        RECT rc = new() { Left = 0, Top = 0, Right = clientW, Bottom = clientH };
        AdjustWindowRectEx(ref rc, WS_OVERLAPPEDWINDOW, false, 0);
        return (rc.Right - rc.Left, rc.Bottom - rc.Top);
    }


    private static void ApplyFontToRenderer()
    {
        Graphics.FontManager.Load();
        var fam = Graphics.FontManager.Get(_cfg!.FontName);
        if (fam == null)
        {

            fam = Graphics.FontManager.Get(Graphics.FontManager.MapleStoryDisplay);
        }
        _renderer!.ConfigFontName = _cfg.FontName;
        _renderer.DefaultFamily = fam;
        _renderer.TextFontFamily = fam;
        _renderer.ClearTextSprites();
    }

    private static void OnExit()
    {
        if (_settings != null)
        {
            try { _settings.Dispatcher.Invoke(() => _settings.Close()); } catch { }
        }
        SaveCounts();
        Window.Save(_cfg);
        DestroyWindow(_hwnd);
    }


    private static void SaveCounts()
    {
        if (_cfg == null || _hook == null) return;
        _cfg.KeyCounts = _hook.ExportKeyCounts();
        _cfg.TotalCount = _hook.TotalCount;
    }


    private static void OnResetCounts()
    {
        if (_hook == null) return;
        _hook.ResetCounts();
        if (_cfg != null)
        {
            _cfg.KeyCounts?.Clear();
            _cfg.TotalCount = 0;
            try { _cfg.Save(); }
            catch (Exception ex) { Graphics.GraphicsLog.Write($"重置计数持久化失败: {ex}"); }
        }

        if (_hwnd != IntPtr.Zero)
            PostMessageW(_hwnd, WM_APP_REFRESH, IntPtr.Zero, IntPtr.Zero);
    }


    private static void OnResetKeyCount(int keyCode)
    {
        var kc = (Core.KeyCode)keyCode;
        int vk = kc.ToVk();
        if (vk == 0) return;
        _hook?.ResetKeyCount(kc);
        if (_cfg != null)
        {
            _cfg.KeyCounts?.Remove(vk);
            _cfg.TotalCount = Math.Max(0, _hook?.TotalCount ?? 0);
            try { _cfg.Save(); }
            catch (Exception ex) { Graphics.GraphicsLog.Write($"重置单键计数持久化失败: {ex}"); }
        }
        if (_hwnd != IntPtr.Zero)
            PostMessageW(_hwnd, WM_APP_REFRESH, IntPtr.Zero, IntPtr.Zero);
    }


    private static void OnResetResolution()
    {
        if (_hwnd == IntPtr.Zero || _cfg == null) return;
        (int ow, int oh) = ComputeOuterSize(800, 600);
        SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, ow, oh,
            (uint)(SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE));

        int nh = _keyRenderer?.WindowHeight ?? 0;
            if (_overlayOn) return;
        if (nh > Window.ClientH && Window.ClientW > 0)
        {
            int curW = Window.ClientW;
            if (GetWindowRect(_hwnd, out RECT wr)) curW = wr.Right - wr.Left;
            (_, int outerH) = ComputeOuterSize(Window.ClientW, nh);
            SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, curW, outerH,
                (uint)(SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE));
        }
        Graphics.GraphicsLog.Write($"窗口分辨率已重置：客户区 {Window.ClientW}×{Window.ClientH}");
    }


}
