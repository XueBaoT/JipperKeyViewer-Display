using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using JipperKeyViewer.Config;
using JipperKeyViewer.Core;
using Button = System.Windows.Controls.Button;
using RadioButton = System.Windows.Controls.RadioButton;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using Color = System.Windows.Media.Color;

namespace JipperKeyViewer.UI;


public partial class SettingsWindowWpf
{

    private int _pendingHotkey;
    private bool _hotkeyCapturing;


    private void UpdateHotkeyButton()
    {
        btnSettingsHotkey.Content = _pendingHotkey > 0
            ? Core.KeyCodeHelper.FromVk(_pendingHotkey).ToLabel()
            : "未设置";
        txtSettingsHotkeyHint.Text = UiLanguage.S("点击后按下要设置的键（Esc 取消）");
    }


    private void SettingsHotkeyClick(object sender, RoutedEventArgs e)
    {
        _hotkeyCapturing = true;
        txtSettingsHotkeyHint.Text = UiLanguage.S("请按下要设置的键…（Esc 取消）");
        Focus();
        System.Windows.Input.Keyboard.Focus(this);
    }


    protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        if (!_hotkeyCapturing) { base.OnPreviewKeyDown(e); return; }

        e.Handled = true;
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            _hotkeyCapturing = false;
            UpdateHotkeyButton();
            return;
        }
        var key = e.Key == System.Windows.Input.Key.System
            ? (e.SystemKey != System.Windows.Input.Key.None ? e.SystemKey : e.ImeProcessedKey)
            : e.Key;
        int vk = System.Windows.Input.KeyInterop.VirtualKeyFromKey(key);
        if (vk <= 0 || vk == 0x07 || vk == 0xFF) return;
        _pendingHotkey = vk;
        _hotkeyCapturing = false;
        UpdateHotkeyButton();
    }

    private void LayoutChecked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton tb || tb.IsChecked != true) return;
        _curLayout = int.Parse((string)tb.Tag);

        SyncLayoutButtons(_curLayout);
        UpdatePksRows();
    }


    private static readonly (int Flag, string Label)[] FontStyleOptions =
    {
        (1, "Bold 粗体"), (2, "Italic 斜体"), (4, "Underline 下划线"),
        (8, "Lowercase 小写"), (16, "Uppercase 大写"), (32, "SmallCaps 小型大写"),
        (64, "Strikethrough 删除线"), (128, "Superscript 上标"), (256, "Subscript 下标"),
    };


    private void InitFontStyleMenu()
    {
        spFontStyle.Children.Clear();
        foreach (var (flag, label) in FontStyleOptions)
        {
            var cb = new System.Windows.Controls.CheckBox
            {
                Content = label,
                Tag = flag,
                Margin = new Thickness(6, 4, 6, 4),
                Style = (Style)FindResource("Toggle"),
            };
            cb.Checked += FontStyleFlagChecked;
            cb.Unchecked += FontStyleFlagChecked;
            spFontStyle.Children.Add(cb);
        }
        UpdateFontStyleSummary();
    }


    private void UpdateFontStyleSummary()
    {
        if (txtFontStyleSummary == null) return;
        int flags = ReadFontStyleFlags();
        if (flags == 0) { txtFontStyleSummary.Text = UiLanguage.S("默认"); return; }
        var parts = new List<string>();
        foreach (var (flag, label) in FontStyleOptions)
            if ((flags & flag) != 0) parts.Add(label.Split(' ')[0]);
        txtFontStyleSummary.Text = string.Join(", ", parts);
    }


    private void ReloadFontComboBoxes()
    {
        Graphics.FontManager.Load();
        string cur = (cmbFont.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? _cfg.FontName;
        cmbFont.Items.Clear();
        cmbFont2.Items.Clear();

        foreach (string builtin in new[] { "MapleStory", "Arial", "Segoe UI" })
        {
            cmbFont.Items.Add(new ComboBoxItem { Content = builtin });
            cmbFont2.Items.Add(new ComboBoxItem { Content = builtin });
        }

        foreach (var f in Graphics.FontManager.List)
        {
            if (f.Display.Equals("MapleStory", StringComparison.OrdinalIgnoreCase)
                || f.Display.Equals("Arial", StringComparison.OrdinalIgnoreCase)
                || f.Display.Equals("Segoe UI", StringComparison.OrdinalIgnoreCase)) continue;
            cmbFont.Items.Add(new ComboBoxItem { Content = f.Display });
            cmbFont2.Items.Add(new ComboBoxItem { Content = f.Display });
        }
        SetSel(cmbFont, cur);
        SetSel(cmbFont2, cur);
    }


    private void RefreshFontsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Graphics.FontManager.Reload();
            ReloadFontComboBoxes();
            ShowToast("字体列表已刷新");
        }
        catch (Exception ex)
        {
            ShowToast("字体刷新失败: " + ex.Message);
        }
    }


    private bool _syncingFontCombo;


    private void FontComboChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingFontCombo) return;
        _syncingFontCombo = true;
        try
        {
            if (ReferenceEquals(sender, cmbFont) && cmbFont.SelectedIndex >= 0)
                cmbFont2.SelectedIndex = cmbFont.SelectedIndex;
            else if (ReferenceEquals(sender, cmbFont2) && cmbFont2.SelectedIndex >= 0)
                cmbFont.SelectedIndex = cmbFont2.SelectedIndex;
        }
        finally { _syncingFontCombo = false; }
    }

    private void FontStyleFlagChecked(object sender, RoutedEventArgs e)
    {


        if (sender is not System.Windows.Controls.CheckBox cb) return;
        if (!TryGetFlag(cb, out int flag)) return;
        if (cb.IsChecked == true)
        {

            foreach (System.Windows.Controls.CheckBox o in spFontStyle.Children)
            {
                if (o == cb || !TryGetFlag(o, out int of)) continue;
                bool sameGroup = ((flag & (8 | 16 | 32)) != 0 && (of & (8 | 16 | 32)) != 0)
                              || ((flag & (128 | 256)) != 0 && (of & (128 | 256)) != 0);
                if (sameGroup && o.IsChecked == true) o.IsChecked = false;
            }
        }
        UpdateFontStyleSummary();
    }


    private void SetFontStyleFlags(int flags)
    {
        foreach (System.Windows.Controls.CheckBox cb in spFontStyle.Children)
            if (TryGetFlag(cb, out int f)) cb.IsChecked = (flags & f) != 0;
        UpdateFontStyleSummary();
    }


    private int ReadFontStyleFlags()
    {
        int flags = 0;
        foreach (System.Windows.Controls.CheckBox cb in spFontStyle.Children)
            if (TryGetFlag(cb, out int f) && cb.IsChecked == true) flags |= f;
        return flags;
    }


    private static bool TryGetFlag(System.Windows.Controls.CheckBox cb, out int flag)
    {
        if (cb.Tag is int i) { flag = i; return true; }
        if (cb.Tag is string s && int.TryParse(s, out int v)) { flag = v; return true; }
        flag = 0;
        return false;
    }


    private void ApplyKpsFollowUi()
    {
        bool follow = chkKpsFollowGlobal.IsChecked == true;
        var gray = new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80));

        if (follow)
        {
            txtKpsHeader.Foreground = gray;
            txtTotalHeader.Foreground = gray;
        }
        else
        {
            txtKpsHeader.ClearValue(TextBlock.ForegroundProperty);
            txtTotalHeader.ClearValue(TextBlock.ForegroundProperty);
        }

        kpsBody.Visibility = follow ? Visibility.Collapsed : Visibility.Visible;
        totalBody.Visibility = follow ? Visibility.Collapsed : Visibility.Visible;
        txtKpsFollow.Visibility = follow ? Visibility.Visible : Visibility.Collapsed;
        txtTotalFollow.Visibility = follow ? Visibility.Visible : Visibility.Collapsed;
    }
    private void MinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void ResetCountsClick(object sender, RoutedEventArgs e)
    {
        try
        {

            if (ShowConfirmDialog("重置计数", "是否重置按键计数？") != true) return;
            _onResetCounts?.Invoke();
            ShowToast("计数已重置");
            Graphics.GraphicsLog.Write("重置计数确认完成");
        }
        catch (Exception ex)
        {
            Graphics.GraphicsLog.Write($"重置计数异常: {ex}");
            ShowToast("重置失败: " + ex.Message);
        }
    }

    private void ResetKpsLabel(object sender, RoutedEventArgs e) => txtKpsLabel.Text = "KPS";
    private void ResetTotalLabel(object sender, RoutedEventArgs e) => txtTotalLabel.Text = "Total";


    private void ImportProfileClick(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择原 Mod 配置文件（KeyViewerSettings / ProfileData JSON）",
            Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*"
        };
        if (dlg.ShowDialog() != true) return;
        if (!Config.Import.ProfileImport.Import(_cfg, dlg.FileName, out var msg))
        {
            ShowToast(msg);
            return;
        }
        LoadValues();
        ShowToast($"{msg}，点击“保存”应用");
    }

    private void OpenConfigClick(object sender, RoutedEventArgs e)
    {
        try { System.Diagnostics.Process.Start("explorer.exe", AppConfig.ConfigDir); }
        catch (Exception ex) { ShowToast($"无法打开配置文件夹: {ex.Message}"); }
    }

}
