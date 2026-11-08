using System.Runtime.InteropServices;

namespace JipperKeyViewer;


internal static partial class Program
{

    private static nint WindowProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        switch (msg)
        {
            case WM_HOTKEY:

                if (SettingsHotkey.IsSettingsHotkey((int)wParam)) ShowSettings();
                return IntPtr.Zero;
            case WM_DESTROY:
                if (_recreatingWindow) return IntPtr.Zero;
                PostQuitMessage(0);
                return IntPtr.Zero;
            case WM_SIZE:


                {
                    int cw = (int)(lParam & 0xFFFF);
                    int ch = (int)((lParam >> 16) & 0xFFFF);
                    if (cw > 0 && ch > 0) Window.SetClientSize(cw, ch);
                    if (cw > 0 && ch > 0)
                    {
                        _renderer?.Resize(cw, ch);
                        RenderFrame();
                    }
                    return IntPtr.Zero;
                }
            case WM_MOVE:

                if (_recreatingWindow) return IntPtr.Zero;
                int mx = (short)(lParam & 0xFFFF);
                int my = (short)((lParam >> 16) & 0xFFFF);
                if (Math.Abs(mx) > 30000 || Math.Abs(my) > 30000) return IntPtr.Zero;
                Window.SetPosition(mx, my);
                return IntPtr.Zero;
            case WM_NCHITTEST:

                if (_overlayOn) return new nint(HTTRANSPARENT);
                break;
            case WM_MOUSEACTIVATE:

                if (_overlayOn) return new nint(MA_NOACTIVATE);
                break;
            case WM_LBUTTONDOWN:

                if (_overlayOn) return IntPtr.Zero;

                ReleaseCapture();
                SendMessageW(hWnd, WM_NCLBUTTONDOWN, new nint(HTCAPTION), IntPtr.Zero);
                return IntPtr.Zero;
            case WM_APP_REFRESH:

                OnSettingsChanged();
                return IntPtr.Zero;
            case WM_COPYDATA:

                {
                    if (lParam != IntPtr.Zero)
                    {
                        var cds = Marshal.PtrToStructure<COPYDATASTRUCT>(lParam);
                        if (cds.dwData.ToInt64() == 1 && cds.lpData != IntPtr.Zero)
                        {
                            string path = Marshal.PtrToStringUni(cds.lpData) ?? "";
                            if (Path.GetExtension(path).Equals(".jkv", StringComparison.OrdinalIgnoreCase))
                            {
                                ImportPackageFromDrop(path);
                                return new nint(1);
                            }
                        }
                    }
                    return IntPtr.Zero;
                }
            case WM_DROPFILES:

                {
                    nint hDrop = wParam;
                    uint len = DragQueryFile(hDrop, 0, null, 0);
                    if (len > 0)
                    {
                        var sb = new System.Text.StringBuilder((int)len + 2);
                        if (DragQueryFile(hDrop, 0, sb, (uint)sb.Capacity) > 0)
                        {
                            string dropped = sb.ToString();
                            if (Path.GetExtension(dropped).Equals(".jkv", StringComparison.OrdinalIgnoreCase))
                                ImportPackageFromDrop(dropped);
                        }
                    }
                    DragFinish(hDrop);
                    return IntPtr.Zero;
                }
            case WM_CONTEXTMENU:

                {
                    int sx = (short)(lParam & 0xFFFF);
                    int sy = (short)((lParam >> 16) & 0xFFFF);
                    Tray.ShowAppMenu(new System.Drawing.Point(sx, sy));
                    return IntPtr.Zero;
                }
        }
        return DefWindowProcW(hWnd, msg, wParam, lParam);
    }

}
