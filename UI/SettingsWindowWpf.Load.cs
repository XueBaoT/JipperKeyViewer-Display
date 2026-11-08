using System.Windows.Controls;
using JipperKeyViewer.Config;
using ComboBox = System.Windows.Controls.ComboBox;
using ComboBoxItem = System.Windows.Controls.ComboBoxItem;
using Slider = System.Windows.Controls.Slider;
using TextBlock = System.Windows.Controls.TextBlock;

namespace JipperKeyViewer.UI;


public partial class SettingsWindowWpf
{

    private void LoadValues()
    {
        var c = _cfg;
        chkThousand.IsChecked = c.ThousandSeparator;
        chkDown.IsChecked = c.DownLocation;
        chkOverlay.IsChecked = c.OverlayMode;
        _pendingHotkey = c.SettingsHotkey;
        UpdateHotkeyButton();
        sldSafeArea.Value = Math.Clamp(c.SafeArea, 0, 12); valSafeArea.Text = ((int)sldSafeArea.Value).ToString();
        chkStdWidth.IsChecked = c.StandardKeyWidth;
        chkHideMainCount.IsChecked = c.HideMainKeyCount;
        chkPerKeyKps.IsChecked = c.PerKeyKps;
        chkStreamer.IsChecked = c.StreamerMode;
        chkHideLabel.IsChecked = c.HideKpsTotalLabel;
        sldKeyFont.Value = c.KeyFontSize;


        _curLayout = c.KeyLayout;
        SyncTabCells();


        SetSel(cmbLanguage, c.Language);
        cmbLanguage.SelectionChanged += (_, _) =>
        {
            if (_langSyncing) return;
            if (cmbLanguage.SelectedItem is ComboBoxItem li)
            {

                _cfg.Language = UiLanguage.Normalize(li.Content?.ToString());
            }
            SyncLanguageUi();
        };
        ReloadFontComboBoxes();
        SetFontStyleFlags(c.FontStyleFlags);


        cmbEasing.Items.Clear();
        foreach (string n in Core.KvEasing.Names)
        {
            var item = new ComboBoxItem { Tag = n, Content = BuildEasingItem(n) };
            cmbEasing.Items.Add(item);
        }
        cmbEasing.SelectedIndex = Core.KvEasing.IndexOf(c.Easing);
        chkPressAnim.IsChecked = c.EnablePressAnimation;
        sldPressScale.Value = c.PressAnimationScale;
        sldPressDur.Value = c.PressAnimationDuration;
        chkPressRain.IsChecked = c.EnablePressAnimationOnRain;


        chkGlowEnable.IsChecked = c.GlowEnabled;
        chkGlowFollow.IsChecked = c.GlowFollowColor;
        sldGlowSize.Value = c.GlowSize;
        sldGlowOp.Value = c.GlowOpacity;
        chkBgGrad.IsChecked = c.BgGradientEnabled;
        chkTextGrad.IsChecked = c.TextGradientEnabled;
        chkOutlineGrad.IsChecked = c.OutlineGradientEnabled;
        chkTextOutline.IsChecked = c.TextOutline;
        sldOutlineW.Value = c.TextOutlineWidth * 100;
        chkTextShadow.IsChecked = c.TextShadow;
        sldShadowX.Value = c.ShadowOffsetX;
        sldShadowY.Value = c.ShadowOffsetY;
        sldShadowSoft.Value = c.ShadowSoftness;


        txtKpsLabel.Text = c.KpsLabel;
        txtTotalLabel.Text = c.TotalLabel;

        LoadRainValues(c);

        LoadColorValues(c);
    }

    private static void SetSel(ComboBox cb, string value)
    {
        for (int i = 0; i < cb.Items.Count; i++)
            if ((cb.Items[i] as ComboBoxItem)?.Content?.ToString() == value) { cb.SelectedIndex = i; return; }
        cb.SelectedIndex = 0;
    }

    private static void SetArr(Slider s, TextBlock t, float[] arr, int idx, string fmt)
    {
        if (arr == null || idx >= arr.Length) return;
        s.Value = arr[idx];
        t.Text = arr[idx].ToString(fmt);
    }

    private static void SetArr(Slider s, TextBlock t, float v, string fmt)
    {
        s.Value = v;
        t.Text = v.ToString(fmt);
    }


}
