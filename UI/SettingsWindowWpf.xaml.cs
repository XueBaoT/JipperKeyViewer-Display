using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using System.Globalization;
using JipperKeyViewer.Config;
using JipperKeyViewer.Core;
using ComboBox = System.Windows.Controls.ComboBox;
using Button = System.Windows.Controls.Button;
using TextBox = System.Windows.Controls.TextBox;
using CheckBox = System.Windows.Controls.CheckBox;
using Brushes = System.Windows.Media.Brushes;
using Brush = System.Windows.Media.Brush;
using FontFamily = System.Windows.Media.FontFamily;
using Orientation = System.Windows.Controls.Orientation;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using Color = System.Windows.Media.Color;
using RadioButton = System.Windows.Controls.RadioButton;

namespace JipperKeyViewer.UI;


public partial class SettingsWindowWpf : Window
{
    private readonly AppConfig _cfg;
    private readonly Action _onChanged;
    private readonly Action _onExit;
    private readonly Action<int, int>? _onBeginCapture;
    private readonly Action<int>? _onResetKeyCount;
    private readonly Action? _onResetCounts;
    private readonly Action? _onResetResolution;


    private readonly TextBlock _toast;
    private readonly DispatcherTimer _toastTimer;
    private readonly DispatcherTimer _themeTimer = new() { Interval = TimeSpan.FromSeconds(1.5) };
    private bool _isDark = true;

    public SettingsWindowWpf(AppConfig cfg, Action onChanged, Action onExit,
        Action<int, int>? onBeginCapture = null, Action? onResetCounts = null,
        Action? onResetResolution = null, Action<int>? onResetKeyCount = null)
    {
        InitializeComponent();
        SourceInitialized += (_, _) => AppTheme.ApplyTitleBar(this, _isDark);
        _cfg = cfg;
        _onChanged = onChanged;
        _onExit = onExit;
        _onBeginCapture = onBeginCapture;
        _onResetCounts = onResetCounts;
        _onResetResolution = onResetResolution;
        _onResetKeyCount = onResetKeyCount;


        _toast = new TextBlock
        {
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
            Background = new SolidColorBrush(Color.FromArgb(200, 32, 34, 37)),
            Padding = new Thickness(10, 5, 10, 5),
            Visibility = Visibility.Collapsed,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = System.Windows.VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 8)
        };
        var grid = Content as Grid;
        var root = FindVisualChild<DockPanel>(this);
        if (root != null)
        {
            root.Children.Add(_toast);
            Grid.SetZIndex(_toast, 99);
        }
        _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _toastTimer.Tick += (_, _) => { _toast.Visibility = Visibility.Collapsed; _toastTimer.Stop(); };

        InitFontStyleMenu();
        cmbFont.SelectionChanged += FontComboChanged;
        cmbFont2.SelectionChanged += FontComboChanged;

        Tabs.SelectionChanged += AboutTabLangSync;
        BuildLayoutGroups();
        LoadValues();
        LoadProfiles();
        WireSliders();        BuildKeyRows();
        sldSafeArea.ValueChanged += (_, _) => { valSafeArea.Text = ((int)sldSafeArea.Value).ToString(); };

        chkPerKeyColor.Checked += (_, _) =>
        {
            _cfg.PerKeyColor = true;
            ApplyPerKeyEnabledUi();
            _onChanged?.Invoke();
        };
        chkPerKeyColor.Unchecked += (_, _) =>
        {
            _cfg.PerKeyColor = false;
            ApplyPerKeyEnabledUi();
            _onChanged?.Invoke();
        };

        chkKpsFollowGlobal.Checked += (_, _) =>
        {
            _cfg.KpsTotalFollowGlobal = true;
            ApplyKpsFollowUi();
            _onChanged?.Invoke();
        };
        chkKpsFollowGlobal.Unchecked += (_, _) =>
        {
            _cfg.KpsTotalFollowGlobal = false;
            ApplyKpsFollowUi();
            _onChanged?.Invoke();
        };
        BuildKeyTabRows();
        BuildPksPopup();

        Tabs.SelectionChanged += (_, _) => { SyncTabCells(); SyncLanguageUi(); };

        Loaded += (_, _) => SyncLanguageUi();


        _isDark = SystemIsDark();
        ApplyTheme(_isDark);
        _themeTimer.Tick += (_, _) =>
        {
            bool dark = SystemIsDark();
            if (dark != _isDark)
            {
                _isDark = dark;
                ApplyTheme(dark);
            }
        };
        _themeTimer.Start();


        Activated += (_, _) => KeepEnglishIme();
        KeepEnglishIme();
    }


    private static bool SystemIsDark()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int i && i == 0;
        }
        catch { return true; }
    }


    private void ApplyTheme(bool dark)
    {

        AppTheme.ApplyPalette(Resources, dark);
    }


    private void SaveClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Graphics.GraphicsLog.Write("保存按钮点击");
            ApplyAndSave();
            BuildKeyTabRows();
        BuildPerKeyColorRows();
            ShowToast("已保存并应用");
            Graphics.GraphicsLog.Write($"保存完成 RainEnabled={_cfg.RainEnabled} Layout={_cfg.KeyLayout}");
        }
        catch (Exception ex)
        {
            Graphics.GraphicsLog.Write($"保存异常: {ex}");
            ShowToast("保存失败: " + ex.Message);
        }
    }


    private enum ResetChoice { None, All, Resolution }

    private void ShowToast(string msg)
    {
        _toast.Text = msg;
        _toast.Visibility = Visibility.Visible;
        _toastTimer.Stop();
        _toastTimer.Start();
    }


    [System.Runtime.InteropServices.DllImport("imm32.dll")]
    private static extern nint ImmGetContext(nint hWnd);
    [System.Runtime.InteropServices.DllImport("imm32.dll")]
    private static extern bool ImmSetConversionStatus(nint hIMC, uint dwConversion, uint dwSentence);
    [System.Runtime.InteropServices.DllImport("imm32.dll")]
    private static extern bool ImmReleaseContext(nint hWnd, nint hIMC);

    private void KeepEnglishIme()
    {
        try
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            nint ctx = ImmGetContext(hwnd);
            if (ctx != nint.Zero)
            {

                ImmSetConversionStatus(ctx, 0, 0);
                ImmReleaseContext(hwnd, ctx);
            }
        }
        catch { }
    }


}
