using JipperKeyViewer.Config;
using JipperKeyViewer.Core;
using JipperKeyViewer.Graphics;

namespace JipperKeyViewer;


internal static partial class Program
{

    private static void OnBeginCapture(int kind, int index)
    {
        _captureKind = kind;
        _captureIndex = index;
        _captureArmed = true;
        _hook?.BeginCapture();
        Graphics.GraphicsLog.Write($"改键捕获开始：kind={kind} index={index}");
    }


    private static void ApplyCapture(int vk)
    {
        int kind = _captureKind, index = _captureIndex;


        var kc = KeyCodeHelper.FromVk(vk);
        if (kc == KeyCode.None)
        {
            NotifySettingsCapture(false, 0);
            return;
        }
        var keys = _cfg!.CurrentKeys;
        if (kind == 2)
        {

            if (index >= 0 && index < keys.Length)
            {
                _cfg.SetCurrentGhostKey(index, (int)kc);
                try { _cfg.Save(); }
                catch (Exception ex) { Graphics.GraphicsLog.Write("补拍键保存失败：" + ex.Message); }
                OnSettingsChanged();
                NotifySettingsCapture(true, (int)kc);
                Graphics.GraphicsLog.Write($"补拍键绑定完成 index={index} → vk={vk} KeyCode={(int)kc}");
            }
            else
            {
                NotifySettingsCapture(false, 0);
            }
            return;
        }
        if (keys != null && index >= 0 && index < keys.Length)
        {
            keys[index] = (int)kc;
            try { _cfg.Save(); }
            catch (Exception ex) { Graphics.GraphicsLog.Write("改键保存失败：" + ex.Message); }
            OnSettingsChanged();
            NotifySettingsCapture(true, (int)kc);
            Graphics.GraphicsLog.Write($"改键完成：kind={kind} index={index} → vk={vk} KeyCode={(int)kc}");
        }
        else
        {
            NotifySettingsCapture(false, 0);
        }
    }


    private static void NotifySettingsCapture(bool ok, int vk)
    {
        int kind = _captureKind, index = _captureIndex;
        if (_settings != null)
        {
            try
            {
                _settings.Dispatcher.Invoke(() => _settings.OnCaptureDone(kind, index, ok, vk));
            }
            catch (Exception ex) { WriteCrashLog(ex); }
        }
    }

    private static void OnSettingsChanged()
    {


        if (_keyRenderer == null || _renderer == null || _hwnd == IntPtr.Zero) return;

        if (_captureArmed)
        {
            _captureArmed = false;
            _hook?.CancelCapture();
            NotifySettingsCapture(false, 0);
        }
        Graphics.GraphicsLog.Write($"设置应用：RainEnabled={_cfg!.RainEnabled} Layout={_cfg.KeyLayout}");
        SettingsHotkey.Register(_cfg!.SettingsHotkey);
        ApplyOverlayMode(_cfg!.OverlayMode);
        UI.UiLanguage.SetCurrent(_cfg.Language);
        Tray.RefreshText();
        _keyRenderer.RefreshLayout();
        _hook?.RegisterKeys(_cfg!.CurrentKeys.Select(k => (KeyCode)k));
        ApplyFontToRenderer();


        int nh = _keyRenderer.WindowHeight;
        if (!_overlayOn && nh > Window.ClientH && Window.ClientW > 0)
        {
            int curW = Window.ClientW;
            if (GetWindowRect(_hwnd, out RECT wr)) curW = wr.Right - wr.Left;
            (_, int outerH) = ComputeOuterSize(Window.ClientW, nh);
            SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, curW, outerH,
                (uint)(SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE));
        }
    }

}
