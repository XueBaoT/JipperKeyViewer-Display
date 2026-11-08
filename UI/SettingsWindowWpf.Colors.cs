using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JipperKeyViewer.Config;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace JipperKeyViewer.UI;


public partial class SettingsWindowWpf
{

    private static readonly Dictionary<string, string> ColorTargets = new()
    {
        ["btnBg"] = "Background", ["btnBgC"] = "BackgroundClicked",
        ["btnOut"] = "Outline", ["btnOutC"] = "OutlineClicked",
        ["btnTxt"] = "Text", ["btnTxtC"] = "TextClicked",
        ["btnRain1"] = "Rain1Color", ["btnRain2"] = "Rain2Color", ["btnRain3"] = "Rain3Color",
        ["btnKpsBg"] = "KpsBackground", ["btnKpsOut"] = "KpsOutline", ["btnKpsTxt"] = "KpsText",
        ["btnTotalBg"] = "TotalBackground", ["btnTotalOut"] = "TotalOutline", ["btnTotalTxt"] = "TotalText",
        ["btn108Bg"] = "P108Background", ["btn108BgC"] = "P108BackgroundClicked",
        ["btn108Out"] = "P108Outline", ["btn108OutC"] = "P108OutlineClicked",
        ["btn108Txt"] = "P108Text", ["btn108TxtC"] = "P108TextClicked",
        ["btnGlowColor"] = "GlowColor",
        ["btnBgTop"] = "BgGradientTop", ["btnBgBottom"] = "BgGradientBottom",
        ["btnBgTopC"] = "BgGradientClickedTop", ["btnBgBottomC"] = "BgGradientClickedBottom",
        ["btnTextLeft"] = "TextGradientLeft", ["btnTextRight"] = "TextGradientRight",
        ["btnTextLeftC"] = "TextGradientClickedLeft", ["btnTextRightC"] = "TextGradientClickedRight",
        ["btnOutTop"] = "OutlineGradientTop", ["btnOutBottom"] = "OutlineGradientBottom",
        ["btnOutTopC"] = "OutlineGradientClickedTop", ["btnOutBottomC"] = "OutlineGradientClickedBottom",
        ["btnOutlineColor"] = "TextOutlineColor", ["btnShadowColor"] = "TextShadowColor",
        ["btnRainSh1"] = "RainShadowColorRow1", ["btnRainSh2"] = "RainShadowColorRow2",
        ["btnRainSh3"] = "RainShadowColorRow3",
        ["btnRainOl1"] = "RainOutlineColorRow1", ["btnRainOl2"] = "RainOutlineColorRow2",
        ["btnRainOl3"] = "RainOutlineColorRow3",
        ["btnGhostRain1"] = "GhostRainColor1", ["btnGhostRain2"] = "GhostRainColor2",
        ["btnGhostRain3"] = "GhostRainColor3",
        ["btnGRSh1"] = "GhostRainShadowColorRow1", ["btnGRSh2"] = "GhostRainShadowColorRow2",
        ["btnGRSh3"] = "GhostRainShadowColorRow3",
        ["btnGROl1"] = "GhostRainOutlineColorRow1", ["btnGROl2"] = "GhostRainOutlineColorRow2",
        ["btnGROl3"] = "GhostRainOutlineColorRow3",
    };

    private void LoadColorValues(AppConfig c)
    {

        chkPerKeyColor.IsChecked = c.PerKeyColor;
        ApplyPerKeyEnabledUi();
        chkKpsFollowGlobal.IsChecked = c.KpsTotalFollowGlobal;
        ApplyKpsFollowUi();
        BuildPerKeyColorRows();
        chk108Unified.IsChecked = c.P108UnifiedColor;
        chkPksEnable.IsChecked = c.PerKeyFontSizeEnabled;
        UpdatePksRows();


        foreach (var kv in ColorTargets)
        {
            var btn = FindName(kv.Key) as Button;
            if (btn == null) continue;
            var prop = c.GetType().GetProperty(kv.Value);
            if (prop?.GetValue(c) is float[] arr && arr.Length >= 3)
            {
                btn.Background = new SolidColorBrush(Color.FromArgb(
                    arr.Length > 3 ? (byte)Math.Clamp((int)(arr[3] * 255f), 0, 255) : (byte)255,
                    (byte)Math.Clamp((int)(arr[0] * 255f), 0, 255),
                    (byte)Math.Clamp((int)(arr[1] * 255f), 0, 255),
                    (byte)Math.Clamp((int)(arr[2] * 255f), 0, 255)));
            }
        }
    }

    private void PickColor(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || !ColorTargets.TryGetValue(btn.Name, out var field)) return;
        var prop = _cfg.GetType().GetProperty(field);
        if (prop?.GetValue(_cfg) is not float[] arr || arr.Length < 3) return;

        var initial = System.Drawing.Color.FromArgb(
            arr.Length > 3 ? Math.Clamp((int)(arr[3] * 255f), 0, 255) : 255,
            Math.Clamp((int)(arr[0] * 255f), 0, 255),
            Math.Clamp((int)(arr[1] * 255f), 0, 255),
            Math.Clamp((int)(arr[2] * 255f), 0, 255));


        var dlg = new ColorPickerWindow(initial) { Owner = this };
        if (dlg.ShowDialog() != true) return;

        var cd = dlg.ResultColor;
        var f = new float[4] { cd.R / 255f, cd.G / 255f, cd.B / 255f, cd.A / 255f };
        prop.SetValue(_cfg, f);
        btn.Background = new SolidColorBrush(Color.FromArgb(cd.A, cd.R, cd.G, cd.B));
    }

}
