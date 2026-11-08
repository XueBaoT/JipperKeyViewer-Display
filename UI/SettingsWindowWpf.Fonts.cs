using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using JipperKeyViewer.Config;
using JipperKeyViewer.Core;
using Button = System.Windows.Controls.Button;
using RadioButton = System.Windows.Controls.RadioButton;
using TextBlock = System.Windows.Controls.TextBlock;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;
using Orientation = System.Windows.Controls.Orientation;

namespace JipperKeyViewer.UI;


public partial class SettingsWindowWpf
{

    private void BuildKeyRows()
    {
        var keys = _cfg.CurrentKeys;

        int perRow = keys.Length <= 8 ? keys.Length : keys.Length - 4;
        FillRow(bindRow1, keys, 0, perRow);
        FillRow(bindRow2, keys, perRow, keys.Length);
        FillRow(bindRow3, keys, 0, 0);


        FillRow(textRow1, keys, 0, perRow);
        FillRow(textRow2, keys, perRow, keys.Length);
        FillRow(textRow3, keys, 0, 0);

    }

    private void FillRow(StackPanel panel, int[] keys, int start, int count)
    {
        panel.Children.Clear();
        for (int i = start; i < start + count && i < keys.Length; i++)
        {
            var btn = new Button { Content = ((KeyCode)keys[i]).ToLabel(), Style = (Style)FindResource("Kbtn"), Margin = new Thickness(0, 0, 5, 0) };
            btn.Click += ToastStub;
            panel.Children.Add(btn);
        }
    }


    private Popup _pksPopup = null!;
    private Slider _pksSld = null!;
    private TextBlock _pksNum = null!;
    private int _pksEditIdx = -1;


    private void UpdatePksRows()
    {
        bool isFull = _curLayout is 75 or 108 or 60;
        expPks.Visibility = isFull ? Visibility.Collapsed : Visibility.Visible;
        if (isFull) return;

        if (pksBody != null)
            pksBody.Visibility = chkPksEnable.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        var keys = _cfg.CurrentKeys;
        int n = keys.Length;
        _cfg.EnsurePerKeyFontSize(n + 2);
        int perRow = n <= 8 ? n : n - 4;
        PksFillRow(pksRow1, keys, 0, perRow);
        PksFillRow(pksRow2, keys, perRow, n);
        PksFillRow(pksRow3, keys, 0, 0);
        if (btnPksKps != null) { btnPksKps.Tag = n; btnPksKps.Content = "KPS"; }
        if (btnPksTotal != null) { btnPksTotal.Tag = n + 1; btnPksTotal.Content = "Total"; }
    }

    private void PksFillRow(StackPanel panel, int[] keys, int start, int count)
    {
        panel.Children.Clear();
        for (int i = start; i < start + count && i < keys.Length; i++)
        {
            var btn = new Button { Content = ((KeyCode)keys[i]).ToLabel(), Style = (Style)FindResource("Kbtn"), Margin = new Thickness(0, 0, 5, 0) };
            btn.Tag = i;
            btn.Click += PksSlotClick;
            panel.Children.Add(btn);
        }
    }


    private void PksSlotClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not int idx) return;
        if (chkPksEnable.IsChecked != true) { ShowToast("请先勾选“启用”"); return; }
        var arr = _cfg.PerKeyFontSize;
        if (idx < 0 || idx >= arr.Length) return;
        _pksEditIdx = idx;
        _pksSld.Value = arr[idx];
        _pksPopup.PlacementTarget = btn;
        _pksPopup.Placement = PlacementMode.Bottom;
        _pksPopup.IsOpen = true;
    }

    private void BuildPksPopup()
    {
        _pksPopup = new Popup { AllowsTransparency = true, StaysOpen = false, PopupAnimation = PopupAnimation.Slide };
        var border = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(12, 8, 12, 8),
        };


        border.SetResourceReference(Border.BackgroundProperty, "Ctrl");
        border.SetResourceReference(Border.BorderBrushProperty, "Line2");
        var row = new StackPanel { Orientation = Orientation.Horizontal };


        var tf = new Typeface(new FontFamily("Consolas"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        double defW = new FormattedText("默认", CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight,
            tf, 14, Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip).Width;
        var numHost = new Grid { Width = defW + 2, ClipToBounds = true, Margin = new Thickness(0, 0, 8, 0) };
        _pksNum = new TextBlock
        {
            FontFamily = new FontFamily("Consolas"),
            FontSize = 14,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _pksNum.SetResourceReference(TextBlock.ForegroundProperty, "Text");
        _pksSld = new Slider { Minimum = 0, Maximum = 72, Style = (Style)FindResource("Slider") };


        _pksSld.ValueChanged += (_, _) =>
        {
            UpdatePksValText(_pksSld.Value);
            if (_pksEditIdx < 0) return;
            var arr = _cfg.PerKeyFontSize;
            if (_pksEditIdx >= arr.Length) return;
            float v = (float)Math.Round(_pksSld.Value);
            if (Math.Abs(arr[_pksEditIdx] - v) < 0.001f) return;
            arr[_pksEditIdx] = v;
        };
        numHost.Children.Add(_pksNum);
        row.Children.Add(numHost);
        row.Children.Add(_pksSld);
        border.Child = row;
        _pksPopup.Child = border;
        _pksPopup.Closed += (_, _) => _pksEditIdx = -1;
    }


    private void UpdatePksValText(double val)
    {
        if (_pksNum == null) return;
        float v = (float)Math.Round(val);
        _pksNum.Text = v <= 0 ? "默认" : v.ToString();
    }


    private void PksResetClick(object sender, RoutedEventArgs e)
    {
        bool? ok = ShowConfirmDialog("重置按键大小", "确认要重置所有按键到默认大小？");
        if (ok != true) return;
        var arr = _cfg.PerKeyFontSize;
        Array.Clear(arr, 0, arr.Length);
        ShowToast("已重置为全局字号");
    }


    private void PksEnableChanged(object sender, RoutedEventArgs e)
    {
        if (pksBody == null) return;
        pksBody.Visibility = chkPksEnable.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }


}
