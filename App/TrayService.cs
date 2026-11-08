using System.Drawing;
using System.Windows.Forms;
using JipperKeyViewer.Config;
using JipperKeyViewer.Graphics;
using JipperKeyViewer.UI;

namespace JipperKeyViewer.App;


internal sealed class TrayService : IDisposable
{
    private NotifyIcon? _tray;
    private ContextMenuStrip? _menu;
    private Icon? _icon;
    private Func<AppConfig?> _config = () => null;
    private Action _showSettings = () => { };
    private Action _exit = () => { };
    private Action<bool> _applyOverlay = _ => { };

    public void Init(Icon icon, Func<AppConfig?> config, Action showSettings, Action exit, Action<bool> applyOverlay)
    {
        _icon = icon;
        _config = config;
        _showSettings = showSettings;
        _exit = exit;
        _applyOverlay = applyOverlay;

        _menu = new ContextMenuStrip();
        _tray = new NotifyIcon
        {
            Icon = icon,
            Text = "JipperKeyViewer",
            Visible = true,
            ContextMenuStrip = _menu,
        };
        RefreshText();
    }


    public void RefreshText()
    {
        if (_menu == null) return;
        _menu.Items.Clear();
        _menu.Items.Add(UiLanguage.S("设置"), null, (_, _) => _showSettings());

        AppConfig? cfg = _config();
        var miOverlay = new ToolStripMenuItem(UiLanguage.S("全局覆盖层"))
        {
            Checked = cfg != null && cfg.OverlayMode,
            CheckOnClick = false,
        };
        miOverlay.Click += (_, _) =>
        {
            AppConfig? current = _config();
            if (current == null) return;
            current.OverlayMode = !current.OverlayMode;
            try { current.Save(); } catch { }
            _applyOverlay(current.OverlayMode);
            miOverlay.Checked = current.OverlayMode;
            GraphicsLog.Write($"托盘切换覆盖层：OverlayMode={current.OverlayMode}");
        };
        _menu.Items.Add(miOverlay);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(UiLanguage.S("退出"), null, (_, _) => _exit());
    }


    public void ShowAppMenu(Point screenPos)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(UiLanguage.S("设置"), null, (_, _) => _showSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(UiLanguage.S("退出"), null, (_, _) => _exit());


        menu.Show(screenPos);
    }

    public void Dispose()
    {
        if (_tray != null)
        {
            _tray.Visible = false;
            _tray.Dispose();
            _tray = null;
        }
        _menu?.Dispose();
        _menu = null;
        _icon?.Dispose();
        _icon = null;
    }
}
