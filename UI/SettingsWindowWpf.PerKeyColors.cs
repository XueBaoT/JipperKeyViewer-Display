using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JipperKeyViewer.Config;
using JipperKeyViewer.Core;
using Button = System.Windows.Controls.Button;
using TextBlock = System.Windows.Controls.TextBlock;
using Color = System.Windows.Media.Color;

namespace JipperKeyViewer.UI;


public partial class SettingsWindowWpf
{


    private int _pkSel = -1;
    private readonly List<Button> _pkKeyBtns = new();

    private Button[] PkSwatches => new[] { btnPkBg, btnPkBgC, btnPkOut, btnPkOutC, btnPkTxt, btnPkTxtC, btnPkRain };


    private float[][] PkArray(int ch) => ch switch
    {
        0 => _cfg.PerKeyBackground,
        1 => _cfg.PerKeyBackgroundClicked,
        2 => _cfg.PerKeyOutline,
        3 => _cfg.PerKeyOutlineClicked,
        4 => _cfg.PerKeyText,
        5 => _cfg.PerKeyTextClicked,
        _ => _cfg.PerKeyRainColor,
    };


    private void BuildPerKeyColorRows()
    {
        _pkKeyBtns.Clear();
        pkcRow1.Children.Clear(); pkcRow2.Children.Clear(); pkcRow3.Children.Clear();

        bool full = Config.Layouts.Get(_cfg.KeyLayout).IsFullKeyboard;
        if (full)
        {

            _pkSel = -1;
            ApplyPerKeyEnabledUi();
            return;
        }

        int n = _cfg.CurrentKeys.Length + 2;
        _cfg.EnsurePerKeyColors(n);

        var rows = Render.LayoutGeometry.GetLayoutRows(_cfg.KeyLayout, _cfg.StandardKeyWidth);
        var panels = new[] { pkcRow1, pkcRow2, pkcRow3 };
        for (int r = 0; r < rows.Length && r < 3; r++)
        {
            foreach (int idx in rows[r])
            {
                if (idx < 0 || idx >= _cfg.CurrentKeys.Length) continue;
                panels[r].Children.Add(MakePkKeyButton(idx, ((KeyCode)_cfg.CurrentKeys[idx]).ToLabel()));
            }
        }


        if (_pkSel < 0 || _pkSel >= n) _pkSel = rows.Length > 0 && rows[0].Length > 0 ? rows[0][0] : 0;
        SelectPerKey(_pkSel);
        ApplyPerKeyEnabledUi();
    }


    private void ApplyPerKeyEnabledUi()
    {
        bool full = Config.Layouts.Get(_cfg.KeyLayout).IsFullKeyboard;
        bool on = chkPerKeyColor.IsChecked == true;
        var gray = new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80));

        txtPkUnsupported.Visibility = full ? Visibility.Visible : Visibility.Collapsed;
        txtPkDisabled.Visibility = (!full && !on) ? Visibility.Visible : Visibility.Collapsed;
        pkcBody.Visibility = (!full && on) ? Visibility.Visible : Visibility.Collapsed;
        if (!full && on) txtPkHeader.ClearValue(TextBlock.ForegroundProperty);
        else txtPkHeader.Foreground = gray;
    }

    private Button MakePkKeyButton(int idx, string label)
    {
        var b = new Button
        {
            Content = label,
            Width = 52,
            Height = 26,
            Margin = new Thickness(0, 0, 4, 4),
            Padding = new Thickness(2, 0, 2, 0),
            FontSize = 12,
            Tag = idx,
        };
        b.Background = new SolidColorBrush(ToMediaColor(PkColorOf(0, idx)));
        b.Click += (_, _) => SelectPerKey((int)b.Tag);
        _pkKeyBtns.Add(b);
        return b;
    }


    private float[] PkColorOf(int ch, int idx)
    {
        var arr = PkArray(ch);
        if (arr != null && idx >= 0 && idx < arr.Length && arr[idx] != null && arr[idx].Length >= 3) return arr[idx];
        return ch switch
        {
            0 => _cfg.Background, 1 => _cfg.BackgroundClicked,
            2 => _cfg.Outline, 3 => _cfg.OutlineClicked,
            4 => _cfg.Text, 5 => _cfg.TextClicked,
            _ => _cfg.Rain1Color,
        };
    }

    private static Color ToMediaColor(float[] c) => Color.FromArgb(
        c.Length > 3 ? (byte)Math.Clamp((int)(c[3] * 255f), 0, 255) : (byte)255,
        (byte)Math.Clamp((int)(c[0] * 255f), 0, 255),
        (byte)Math.Clamp((int)(c[1] * 255f), 0, 255),
        (byte)Math.Clamp((int)(c[2] * 255f), 0, 255));

    private void SelectPerKey(int idx)
    {
        _pkSel = idx;
        var keys = _cfg.CurrentKeys;
        string name = idx >= 0 && idx < keys.Length ? ((KeyCode)keys[idx]).ToLabel()
            : idx == keys.Length ? "KPS" : idx == keys.Length + 1 ? "Total" : "?";
        txtPkKey.Text = $"#{idx}  {name}";
        for (int ch = 0; ch < PkSwatches.Length; ch++)
            PkSwatches[ch].Background = new SolidColorBrush(ToMediaColor(PkColorOf(ch, idx)));
        foreach (var b in _pkKeyBtns)
            b.BorderBrush = (int)b.Tag == idx ? new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xD9)) : null;
        foreach (var b in _pkKeyBtns)
            b.BorderThickness = new Thickness((int)b.Tag == idx ? 2 : 0);
    }


    private void PickPerKeyColor(object sender, RoutedEventArgs e)
    {
        if (_pkSel < 0 || sender is not Button btn || btn.Tag is not string t || !int.TryParse(t, out int ch)) return;
        var arr = PkArray(ch);
        if (arr == null || _pkSel >= arr.Length) return;

        var cur = PkColorOf(ch, _pkSel);
        var initial = System.Drawing.Color.FromArgb(
            cur.Length > 3 ? Math.Clamp((int)(cur[3] * 255f), 0, 255) : 255,
            Math.Clamp((int)(cur[0] * 255f), 0, 255),
            Math.Clamp((int)(cur[1] * 255f), 0, 255),
            Math.Clamp((int)(cur[2] * 255f), 0, 255));
        var dlg = new ColorPickerWindow(initial) { Owner = this };
        if (dlg.ShowDialog() != true) return;

        var cd = dlg.ResultColor;
        arr[_pkSel] = new[] { cd.R / 255f, cd.G / 255f, cd.B / 255f, cd.A / 255f };
        SelectPerKey(_pkSel);
    }

    private void PkResetColorClick(object sender, RoutedEventArgs e)
    {
        if (_pkSel < 0) return;
        _cfg.ResetPerKeyColorsAt(_pkSel);
        SelectPerKey(_pkSel);
        ShowToast("已把该键颜色恢复为全局配色");
    }

    private void PkResetCountClick(object sender, RoutedEventArgs e)
    {
        if (_pkSel < 0) return;
        var keys = _cfg.CurrentKeys;
        if (_pkSel >= keys.Length) { ShowToast("KPS / Total 槽没有独立计数"); return; }
        _onResetKeyCount?.Invoke(keys[_pkSel]);
        ShowToast($"已重置 {((KeyCode)keys[_pkSel]).ToLabel()} 的计数");
    }

    private void ToastStub(object sender, RoutedEventArgs e)
    {
        var tag = sender is FrameworkElement fe && fe.Tag is string s && s.Length > 0 ? s : null;
        ShowToast(tag ?? "此功能将在后续阶段开放");
    }

}
