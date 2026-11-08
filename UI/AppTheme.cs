using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace JipperKeyViewer.UI;


internal static class AppTheme
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);


    public static bool IsSystemDark()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int i && i == 0;
        }
        catch { return false; }
    }


    public static void ApplyPalette(ResourceDictionary res, bool dark)
    {
        void Set(string k, byte r, byte g, byte b) => res[k] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));

        if (dark)
        {
            Set("Bg", 0x16, 0x18, 0x1A); Set("Panel", 0x20, 0x22, 0x25); Set("Panel2", 0x23, 0x25, 0x29);
            Set("Ctrl", 0x2F, 0x32, 0x35); Set("CtrlH", 0x3A, 0x3D, 0x41);
            Set("Line", 0x3A, 0x3D, 0x41); Set("Line2", 0x45, 0x48, 0x4D);
            Set("Text", 0xFF, 0xFF, 0xFF); Set("Dim", 0xFF, 0xFF, 0xFF);
            Set("Alias", 0x9A, 0xA0, 0xA6);
            Set("Acc", 0x4A, 0x90, 0xD9); Set("AccH", 0x3D, 0x6E, 0xA5); Set("Sel", 0x36, 0x58, 0x7F);
            Set("Red", 0xE0, 0x60, 0x60); Set("Green", 0x6F, 0xBF, 0x73); Set("BodyBg", 0x1C, 0x1E, 0x21);
        }
        else
        {
            Set("Bg", 0xF3, 0xF4, 0xF6); Set("Panel", 0xFF, 0xFF, 0xFF); Set("Panel2", 0xEC, 0xED, 0xEF);
            Set("Ctrl", 0xFF, 0xFF, 0xFF); Set("CtrlH", 0xE5, 0xE7, 0xEB);
            Set("Line", 0xE5, 0xE7, 0xEB); Set("Line2", 0xD1, 0xD5, 0xDB);
            Set("Text", 0x1F, 0x29, 0x37); Set("Dim", 0x37, 0x41, 0x51);
            Set("Alias", 0x6E, 0x76, 0x81);
            Set("Acc", 0x3B, 0x82, 0xF6); Set("AccH", 0x25, 0x63, 0xEB); Set("Sel", 0xDB, 0xEA, 0xFE);
            Set("Red", 0xDC, 0x26, 0x26); Set("Green", 0x16, 0xA3, 0x4A); Set("BodyBg", 0xF8, 0xFA, 0xFC);
        }
    }


    public static void ApplyTitleBar(Window window, bool dark)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;
            int v = dark ? 1 : 0;
            if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref v, sizeof(int)) != 0)
                _ = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref v, sizeof(int));
        }
        catch {  }
    }
}
