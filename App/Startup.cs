using System.IO;
using System.Runtime.InteropServices;
using JipperKeyViewer.App;
using JipperKeyViewer.Config;
using JipperKeyViewer.Core;
using JipperKeyViewer.Graphics;
using JipperKeyViewer.Render;
using JipperKeyViewer.UI;

namespace JipperKeyViewer;


internal static partial class Program
{
    private static int Run()
    {
        Graphics.GraphicsLog.Write($"程序启动 进程ID={Environment.ProcessId} 可执行路径={Environment.ProcessPath}");

        Config.GlobalSettings.Load();
        _cfg = AppConfig.Load();
        Graphics.GraphicsLog.Write($"配置加载完成：布局={_cfg.KeyLayout} 不透明度={_cfg.Opacity} 记忆窗口={_cfg.WindowW}x{_cfg.WindowH} 语言={_cfg.Language}（全局）");
        Graphics.GraphicsLog.Write($"键雨配置：启用={_cfg.RainEnabled} 淡出开关={_cfg.EnableRainFade} 淡出时长={_cfg.RainFadeDuration:0.000}s 顶部渐隐={_cfg.EnableRainGradient} 渐隐px={_cfg.RainFadePx} 速度1/2/3={_cfg.RainSpeedRow1}/{_cfg.RainSpeedRow2}/{_cfg.RainSpeedRow3} 高度1/2/3={_cfg.RainHeightRow1}/{_cfg.RainHeightRow2}/{_cfg.RainHeightRow3} 宽1/2/3={_cfg.RainWidthRow1}/{_cfg.RainWidthRow2}/{_cfg.RainWidthRow3} startY1/2/3={_cfg.RainStartYRow1}/{_cfg.RainStartYRow2}/{_cfg.RainStartYRow3} 排启用1/2/3={_cfg.EnableRainForRow1}/{_cfg.EnableRainForRow2}/{_cfg.EnableRainForRow3}");


        if (!string.IsNullOrEmpty(_pendingPackage)
            && Path.GetExtension(_pendingPackage).Equals(".jkv", StringComparison.OrdinalIgnoreCase)
            && File.Exists(_pendingPackage))
        {
            var pkg = Config.Import.PackageImporter.TryImport(_pendingPackage);
            if (pkg.Ok)
            {
                try
                {
                    ProfileManager.LoadProfileInto(_cfg, pkg.ProfileName!);
                    ProfileManager.SetCurrentProfile(pkg.ProfileName!);
                    _cfg.Save();
                    Graphics.GraphicsLog.Write($"命令行导入分享包成功：{pkg.ProfileName}");
                }
                catch (Exception ex) { WriteCrashLog(ex); }
            }
            else
            {
                _pendingPackageError = pkg.Error;
                Graphics.GraphicsLog.Write($"命令行导入分享包失败：{pkg.Error}");
            }
        }


        _hook = new KeyboardHook();
        if (!_hook.Install())
            throw new InvalidOperationException("全局键盘钩子安装失败（SetWindowsHookEx 返回 0）。");
        _hook.RegisterKeys(_cfg.CurrentKeys.Select(k => (KeyCode)k));
        Graphics.GraphicsLog.Write($"键盘钩子已安装，注册 {_cfg.CurrentKeys.Length} 个键位");

        _hook.RestoreCounts(_cfg.KeyCounts ?? new(), _cfg.TotalCount);
        Graphics.GraphicsLog.Write($"计数恢复：Total={_cfg.TotalCount} 键数={_cfg.KeyCounts?.Count ?? 0}");


        _keyRenderer = new KeyRenderer(_cfg, _hook);

        int x = CW_USEDEFAULT, y = CW_USEDEFAULT;
        int w, h;
        bool overlayAtStart = _cfg.OverlayMode;

        if (_cfg.WindowW <= 0 || _cfg.WindowH <= 0)
            Graphics.GraphicsLog.Write($"窗口尺寸无效（{_cfg.WindowW}x{_cfg.WindowH}），使用默认 800x600");
        int clientW = Math.Max(_cfg.WindowW > 0 ? _cfg.WindowW : 800, 240);
        int clientH = Math.Max(_cfg.WindowH > 0 ? _cfg.WindowH : 600, 160);
        int clientX = _cfg.WindowX >= 0 ? _cfg.WindowX : CW_USEDEFAULT;
        int clientY = _cfg.WindowY >= 0 ? _cfg.WindowY : CW_USEDEFAULT;
        WindowState.SanitizePosition(ref clientX, ref clientY);
        if (overlayAtStart)
        {

            w = clientW; h = clientH; x = clientX; y = clientY;
        }
        else
        {

            RECT adj = new() { Left = 0, Top = 0, Right = clientW, Bottom = clientH };
            AdjustWindowRectEx(ref adj, (uint)WS_OVERLAPPEDWINDOW, false, 0);
            w = adj.Right - adj.Left;
            h = adj.Bottom - adj.Top;
            x = clientX + adj.Left;
            y = clientY + adj.Top;
        }

        WNDCLASSEX wc = new()
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(WndProcDelegate),
            hInstance = GetModuleHandleW(null),
            hIcon = LoadExeIconLarge(),
            lpszClassName = ClassName,
        };
        RegisterClassExW(ref wc);


        bool startOverlay = overlayAtStart;


        int initExStyle = startOverlay
            ? (int)(WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE
                    | (uint)WS_EX_LAYERED | (uint)WS_EX_TRANSPARENT)
            : 0;
        _hwnd = CreateWindowExW(
            initExStyle,
            ClassName, "Jipper Key Viewer Display",
            startOverlay ? (WS_POPUP | WS_VISIBLE) : (WS_OVERLAPPEDWINDOW | WS_VISIBLE),
            x, y, w, h,
            IntPtr.Zero, IntPtr.Zero, wc.hInstance, IntPtr.Zero);
        if (_hwnd == IntPtr.Zero)
            throw new InvalidOperationException($"创建窗口失败，GetLastError=0x{Marshal.GetLastWin32Error():X8}。");


        if (GetClientRect(_hwnd, out RECT cr)) { Window.ClientW = cr.Right - cr.Left; Window.ClientH = cr.Bottom - cr.Top; }
        else { Window.ClientW = w; Window.ClientH = h; }
        _renderer = RendererFactory.Create(_cfg.OverlayMode);
        _renderer.LayeredMode = _cfg.OverlayMode;
        _renderer.Init(_hwnd, Window.ClientW, Window.ClientH);
        Graphics.GraphicsLog.Write($"渲染后端：{_renderer.GetType().Name}（GPU={_renderer is Graphics.GpuRenderer}）");
        Graphics.GraphicsLog.Write("GPU 为默认后端；JKV_GPU=0 可强制 CPU，初始化失败自动回退 CPU。");
        ApplyFontToRenderer();


        ImmAssociateContext(_hwnd, IntPtr.Zero);
        Graphics.GraphicsLog.Write($"渲染器初始化完成：内容基准={_keyRenderer.WindowWidth}x{_keyRenderer.WindowHeight} 客户区={Window.ClientW}x{Window.ClientH}");


        DragAcceptFiles(_hwnd, true);


        EnsurePackageIcon();
        RegisterJkvAssociation();


        if (!string.IsNullOrEmpty(_pendingPackageError))
        {
            ShowImportErrorDialog();
            _pendingPackageError = null;
        }


        Tray.Init(LoadTrayIcon(), () => _cfg, ShowSettings, OnExit, ApplyOverlayMode);
        SettingsHotkey.Attach(_hwnd);
        SettingsHotkey.Register(_cfg.SettingsHotkey);

        _overlayOn = _cfg.OverlayMode;

        UI.UiLanguage.SetCurrent(_cfg.Language);


        RunMainLoop();

        Tray.Dispose();


        SaveCounts();
        Window.Save(_cfg);
        _renderer?.Dispose();
        _hook?.Uninstall();
        return 0;
    }

}
