using System.Runtime.InteropServices;
using JipperKeyViewer.App;
using JipperKeyViewer.Config;
using JipperKeyViewer.Graphics;

namespace JipperKeyViewer;


internal static partial class Program
{

    private static bool _overlayOn;

    private const uint WS_EX_TOOLWINDOW = 0x00000080;
    private const uint WS_EX_NOACTIVATE = 0x08000000;
    private const uint WM_MOUSEACTIVATE = 0x0021;
    private const uint WM_NCHITTEST = 0x0084;
    private const int MA_NOACTIVATE = 3;
    private const int HTTRANSPARENT = -1;


    private static void EnsureTopmost()
    {
        if (_hwnd == IntPtr.Zero) return;
        SetWindowPos(_hwnd, new IntPtr(-1) , 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    }


    private static void ApplyOverlayMode(bool on)
    {
        if (_hwnd == IntPtr.Zero || _cfg == null || _renderer == null) return;
        if (on == _overlayOn) return;


        RebuildWindowForOverlay(on);
        _overlayOn = on;
        Window.Save(_cfg);
        Graphics.GraphicsLog.Write($"覆盖层已{(on ? "启用" : "关闭")}（客户区锚定，不退出程序）客户区={Window.ClientW}x{Window.ClientH}");
    }

    private static bool _recreatingWindow;


    private static void GraphicsLog_(string msg) => Graphics.GraphicsLog.Write("覆盖层：" + msg);

    private static void RebuildWindowForOverlay(bool on)
    {
        if (_hwnd == IntPtr.Zero || _cfg == null || _renderer == null) return;

        GetClientRect(_hwnd, out RECT cr);
        int cw = cr.Right - cr.Left, ch = cr.Bottom - cr.Top;
        if (cw <= 0 || ch <= 0)
        {
            cw = _cfg.WindowW > 0 ? _cfg.WindowW : 800;
            ch = _cfg.WindowH > 0 ? _cfg.WindowH : 600;
            Graphics.GraphicsLog.Write($"重建窗口：客户区尺寸无效，回退到 {cw}x{ch}");
        }


        POINT org = default;
        bool okClient = ClientToScreen(_hwnd, ref org);
        if (!okClient || (org.X == 0 && org.Y == 0 && cw > 0 && Window.X != 0))
        {
            GraphicsLog_("ClientToScreen 失败或返回 0,0，回退到记忆位置");
            org.X = _cfg.WindowX; org.Y = _cfg.WindowY;
        }
        int sanX = org.X, sanY = org.Y;
        WindowState.SanitizePosition(ref sanX, ref sanY);
        org.X = sanX; org.Y = sanY;

        _recreatingWindow = true;
        DestroyWindow(_hwnd);
        _hwnd = IntPtr.Zero;
        _recreatingWindow = false;

        int exStyle;
        uint style;
        int w, h, x, y;
        if (on)
        {

            exStyle = (int)(WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE
                            | (uint)WS_EX_LAYERED | (uint)WS_EX_TRANSPARENT);
            style = WS_POPUP | WS_VISIBLE;
            x = org.X; y = org.Y; w = cw; h = ch;
        }
        else
        {
            exStyle = 0;
            style = WS_OVERLAPPEDWINDOW | WS_VISIBLE;
            RECT adj = new() { Left = 0, Top = 0, Right = cw, Bottom = ch };
            AdjustWindowRectEx(ref adj, (uint)WS_OVERLAPPEDWINDOW, false, 0);
            w = adj.Right - adj.Left;
            h = adj.Bottom - adj.Top;
            x = org.X + adj.Left;
            y = org.Y + adj.Top;
        }

        _hwnd = CreateWindowExW(exStyle, ClassName, "Jipper Key Viewer Display", style,
            x, y, w, h, IntPtr.Zero, IntPtr.Zero, GetModuleHandleW(null), IntPtr.Zero);
        if (_hwnd == IntPtr.Zero)
        {
            Graphics.GraphicsLog.Write($"重建窗口失败：GetLastError=0x{Marshal.GetLastWin32Error():X8}");
            return;
        }

        try { _renderer.Dispose(); } catch { }
        _renderer = RendererFactory.Create(on);
        _renderer.LayeredMode = on;
        if (GetClientRect(_hwnd, out RECT cr2)) { Window.ClientW = cr2.Right - cr2.Left; Window.ClientH = cr2.Bottom - cr2.Top; }
        _renderer.Init(_hwnd, Window.ClientW, Window.ClientH);

        DragAcceptFiles(_hwnd, true);
        SettingsHotkey.Attach(_hwnd);
        SettingsHotkey.Register(_cfg!.SettingsHotkey);
        if (on) EnsureTopmost();

        POINT org2 = default; ClientToScreen(_hwnd, ref org2);
        Graphics.GraphicsLog.Write($"窗口已重建：overlay={on} 客户区={Window.ClientW}x{Window.ClientH} 原点 {org.X},{org.Y} -> {org2.X},{org2.Y}");
    }


}
