using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JipperKeyViewer.Config;
using JipperKeyViewer.Core;
using Button = System.Windows.Controls.Button;
using TextBox = System.Windows.Controls.TextBox;
using Color = System.Windows.Media.Color;

namespace JipperKeyViewer.UI;


public partial class SettingsWindowWpf
{
    private readonly List<(int Kind, int Index, Button Btn, bool IsBind)> _keySlotBtns = new();
    private Button? _captureBtn;
    private Button? _selectedTextBtn;


    private void BuildKeyTabRows()
    {
        _keySlotBtns.Clear();
        bindRow1.Children.Clear(); bindRow2.Children.Clear(); bindRow3.Children.Clear();
        textRow1.Children.Clear(); textRow2.Children.Clear(); textRow3.Children.Clear();
        ghostRow1.Children.Clear(); ghostRow2.Children.Clear(); ghostRow3.Children.Clear();
        _captureBtn = null;
        _selectedTextBtn = null;
        txtBindHint.Text = UiLanguage.S("点击按键开始改键，随后按下要绑定的键（Esc 取消）");

        if (Config.Layouts.Get(_cfg.KeyLayout).IsFullKeyboard)
        {
            txtBindHint.Text = UiLanguage.S("全键盘（108K / 75K / 60K）不支持改键与自定义文本");
            txtGhostHint.Text = UiLanguage.S("全键盘（108K / 75K / 60K）不支持补拍键");
            return;
        }
        txtGhostHint.Text = UiLanguage.S("点击按键开始绑定补拍键（Esc 取消）；补拍键本身不计入统计");
        var keys = _cfg.CurrentKeys;
        var texts = _cfg.CurrentKeyTexts;
        var ghostKeys = _cfg.EnsureCurrentGhostKeys(keys.Length);
        var rows = Render.LayoutGeometry.GetLayoutRows(_cfg.KeyLayout, _cfg.StandardKeyWidth);
        var bindPanels = new[] { bindRow1, bindRow2, bindRow3 };
        var textPanels = new[] { textRow1, textRow2, textRow3 };
        var ghostPanels = new[] { ghostRow1, ghostRow2, ghostRow3 };
        for (int r = 0; r < rows.Length && r < 3; r++)
        {
            foreach (int idx in rows[r])
            {
                if (idx < 0 || idx >= keys.Length) continue;
                string name = ((KeyCode)keys[idx]).ToLabel();
                string display = idx < texts.Length && !string.IsNullOrEmpty(texts[idx]) ? texts[idx] : name;
                bindPanels[r].Children.Add(MakeSlotButton(name, 0, idx, true, BindSlotClick));
                textPanels[r].Children.Add(MakeSlotButton(display, 0, idx, false, TextSlotClick));

                string gLabel = idx < ghostKeys.Length && ghostKeys[idx] != 0
                    ? ((KeyCode)ghostKeys[idx]).ToLabel() : "+";
                ghostPanels[r].Children.Add(MakeSlotButton(gLabel, 2, idx, true, BindSlotClick));
            }
        }
    }

    private Button MakeSlotButton(string label, int kind, int index, bool isBind, RoutedEventHandler click)
    {
        var b = new Button
        {
            Content = label,
            Width = 58,
            Height = 26,
            Margin = new Thickness(0, 0, 4, 0),
            Padding = new Thickness(2, 0, 2, 0),
            FontSize = 12,
            Tag = (kind, index)
        };
        b.SetResourceReference(Button.BackgroundProperty, "Ctrl");
        b.SetResourceReference(Button.ForegroundProperty, "Text");
        b.Click += click;
        _keySlotBtns.Add((kind, index, b, isBind));
        return b;
    }


    private void BindSlotClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button b || b.Tag is not (int kind, int idx)) return;
        if (_captureBtn != null) RestoreSlotBtnStyle(_captureBtn);
        _captureBtn = b;
        HighlightSlotBtn(b);
        txtBindHint.Text = UiLanguage.S("请按下要绑定的键…（Esc 取消）");
        _onBeginCapture?.Invoke(kind, idx);
    }


    private void TextSlotClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button b || b.Tag is not (int kind, int idx)) return;
        if (_selectedTextBtn != null) RestoreSlotBtnStyle(_selectedTextBtn);
        _selectedTextBtn = b;
        HighlightSlotBtn(b);
        var texts = _cfg.CurrentKeyTexts;
        txtKeyText.Text = idx < texts.Length ? texts[idx] ?? "" : "";
    }


    private void SaveKeyTextClick(object sender, RoutedEventArgs e)
    {
        if (_selectedTextBtn == null || _selectedTextBtn.Tag is not (int kind, int idx))
        {
            ShowToast("请先选择要修改文本的按键");
            return;
        }
        SetSlotText(kind, idx, txtKeyText.Text ?? "");
    }


    private void ResetKeyTextClick(object sender, RoutedEventArgs e)
    {
        if (_selectedTextBtn == null || _selectedTextBtn.Tag is not (int kind, int idx))
        {
            ShowToast("请先选择要修改文本的按键");
            return;
        }
        SetSlotText(kind, idx, "");
        txtKeyText.Text = "";
    }

    private void SetSlotText(int kind, int idx, string value)
    {


        var texts = _cfg.EnsureCurrentKeyTexts(_cfg.CurrentKeys.Length);
        if (idx >= texts.Length)
        {
            ShowToast("槽位超出范围");
            return;
        }
        texts[idx] = value;
        try
        {
            _cfg.Save();
            _onChanged();

            foreach (var (k, i, b, isBind) in _keySlotBtns)
            {
                if (!isBind && k == kind && i == idx)
                {
                    b.Content = DisplaySlotLabel(kind, idx);
                    break;
                }
            }
            ShowToast("已保存");
        }
        catch (Exception ex)
        {
            ShowToast("保存失败: " + ex.Message);
        }
    }


    private string DisplaySlotLabel(int kind, int idx)
    {
        var kk = _cfg.CurrentKeys;
        var tt = _cfg.CurrentKeyTexts;
        string n2 = idx < kk.Length ? ((KeyCode)kk[idx]).ToLabel() : "";
        return idx < tt.Length && !string.IsNullOrEmpty(tt[idx]) ? tt[idx] : n2;
    }

    private static void HighlightSlotBtn(Button b) => b.Background = new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xD9));
    private static void RestoreSlotBtnStyle(Button b) => b.SetResourceReference(Button.BackgroundProperty, "Ctrl");


    public void OnCaptureDone(int kind, int index, bool ok, int vk)
    {
        foreach (var (k, i, b, isBind) in _keySlotBtns)
        {
            if (isBind && k == kind && i == index)
            {
                var keys = _cfg.CurrentKeys;
                if (ok && i < keys.Length)
                {
                    b.Content = ((KeyCode)keys[i]).ToLabel();
                }
                if (b == _captureBtn) RestoreSlotBtnStyle(b);
                break;
            }
        }
        foreach (var (k, i, b, isBind) in _keySlotBtns)
        {
            if (!isBind && k == kind && i == index)
            {
                b.Content = DisplaySlotLabel(kind, index);
                break;
            }
        }
        _captureBtn = null;
        txtBindHint.Text = UiLanguage.S("点击按键开始改键，随后按下要绑定的键（Esc 取消）");
        ShowToast(ok ? "已绑定" : "已取消改键");
    }

}
