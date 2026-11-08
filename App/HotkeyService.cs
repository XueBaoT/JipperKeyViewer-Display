using System.Runtime.InteropServices;
using JipperKeyViewer.Core;
using JipperKeyViewer.Graphics;

namespace JipperKeyViewer.App;


internal sealed class HotkeyService
{
    public const int SettingsId = 0x4A51;
    private nint _hwnd;

    public void Attach(nint hwnd) => _hwnd = hwnd;


    public void Register(int vk)
    {
        if (_hwnd == IntPtr.Zero) return;
        UnregisterHotKey(_hwnd, SettingsId);
        if (vk <= 0)
        {
            GraphicsLog.Write("设置热键：已关闭（配置为 0）");
            return;
        }
        bool ok = RegisterHotKey(_hwnd, SettingsId, 0, (uint)vk);
        if (ok)
        {
            string name = KeyCodeHelper.FromVk(vk).ToLabel();
            GraphicsLog.Write($"设置热键：注册成功 VK=0x{vk:X2}（{name}）");
        }
        else
        {
            int err = Marshal.GetLastWin32Error();
            GraphicsLog.Write($"设置热键：注册失败 VK=0x{vk:X2} 错误码={err}" +
                (err == 1409 ? "（该组合已被其它程序占用）" : ""));
        }
    }

    public bool IsSettingsHotkey(int id) => _hwnd != IntPtr.Zero && id == SettingsId;

    public void Unregister()
    {
        if (_hwnd != IntPtr.Zero) UnregisterHotKey(_hwnd, SettingsId);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(nint hWnd, int id);
}
