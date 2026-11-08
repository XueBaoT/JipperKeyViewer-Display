using JipperKeyViewer.UI;

namespace JipperKeyViewer;


internal static partial class Program
{
    private static UI.SettingsWindowWpf? _settings;
    private static Thread? _settingsThread;
    private static readonly object SettingsLock = new();


    private static void ShowSettings()
    {
        lock (SettingsLock)
        {
            if (_settingsThread != null && _settingsThread.IsAlive)
            {
                try
                {
                    _settings?.Dispatcher.Invoke(() =>
                    {
                        _settings.WindowState = System.Windows.WindowState.Normal;
                        _settings.Activate();
                    });
                }
                catch (Exception ex) { WriteCrashLog(ex); }
                return;
            }
        }
        _settingsThread = new Thread(() =>
        {
            try
            {


                var win = new UI.SettingsWindowWpf(_cfg!,
                    () => PostMessageW(_hwnd, WM_APP_REFRESH, IntPtr.Zero, IntPtr.Zero),
                    OnExit,
                    OnBeginCapture,
                    OnResetCounts,
                    OnResetResolution,
                    OnResetKeyCount);
                win.Icon = LoadSettingsIcon();
                _settings = win;
                win.Closed += (_, _) =>
                {

                    _settings = null;
                    if (_captureArmed)
                    {
                        _captureArmed = false;
                        _hook?.CancelCapture();
                    }
                };
                Graphics.GraphicsLog.Write("设置界面已创建并显示");
                win.ShowDialog();
            }
            catch (Exception ex)
            {
                _settings = null;
                WriteCrashLog(ex);
            }
        });
        _settingsThread.SetApartmentState(ApartmentState.STA);
        _settingsThread.IsBackground = true;
        _settingsThread.Start();
    }

}
