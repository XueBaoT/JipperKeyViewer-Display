using System.Windows.Controls;
using JipperKeyViewer.Config;
using JipperKeyViewer.Core;
using ComboBoxItem = System.Windows.Controls.ComboBoxItem;

namespace JipperKeyViewer.UI;


public partial class SettingsWindowWpf
{

    private void ApplyAndSave()
    {
        var c = _cfg;
        c.ThousandSeparator = chkThousand.IsChecked == true;
        c.DownLocation = chkDown.IsChecked == true;
        c.OverlayMode = chkOverlay.IsChecked == true;
        c.SettingsHotkey = _pendingHotkey;
        c.SafeArea = (int)Math.Round(sldSafeArea.Value);
        c.StandardKeyWidth = chkStdWidth.IsChecked == true;
        c.HideMainKeyCount = chkHideMainCount.IsChecked == true;
        c.PerKeyKps = chkPerKeyKps.IsChecked == true;
        c.StreamerMode = chkStreamer.IsChecked == true;
        c.HideKpsTotalLabel = chkHideLabel.IsChecked == true;
        c.KeyFontSize = (float)sldKeyFont.Value;
        c.EnablePressAnimation = chkPressAnim.IsChecked == true;
        c.PressAnimationScale = (float)sldPressScale.Value;
        c.PressAnimationDuration = (float)sldPressDur.Value;
        c.EnablePressAnimationOnRain = chkPressRain.IsChecked == true;
        if (cmbEasing.SelectedItem is ComboBoxItem ei && ei.Tag is string easeName)
            c.Easing = Core.KvEasing.Normalize(easeName);

        if (cmbLanguage.SelectedItem is ComboBoxItem li) c.Language = li.Content?.ToString() ?? "中文";
        if (cmbFont.SelectedItem is ComboBoxItem fi) c.FontName = fi.Content?.ToString() ?? "MapleStory";
        if (cmbFont2.SelectedItem is ComboBoxItem f2) c.FontName = f2.Content?.ToString() ?? "MapleStory";
        c.FontStyleFlags = ReadFontStyleFlags();


        c.KeyLayout = _curLayout;

        c.GlowEnabled = chkGlowEnable.IsChecked == true;
        c.GlowFollowColor = chkGlowFollow.IsChecked == true;
        c.GlowSize = (float)sldGlowSize.Value;
        c.GlowOpacity = (int)sldGlowOp.Value;
        c.BgGradientEnabled = chkBgGrad.IsChecked == true;
        c.TextGradientEnabled = chkTextGrad.IsChecked == true;
        c.OutlineGradientEnabled = chkOutlineGrad.IsChecked == true;
        c.TextOutline = chkTextOutline.IsChecked == true;
        c.TextOutlineWidth = (float)(sldOutlineW.Value / 100.0);
        c.TextShadow = chkTextShadow.IsChecked == true;
        c.ShadowOffsetX = (float)sldShadowX.Value;
        c.ShadowOffsetY = (float)sldShadowY.Value;
        c.ShadowSoftness = (float)sldShadowSoft.Value;

        c.KpsLabel = string.IsNullOrWhiteSpace(txtKpsLabel.Text) ? "KPS" : txtKpsLabel.Text;
        c.TotalLabel = string.IsNullOrWhiteSpace(txtTotalLabel.Text) ? "Total" : txtTotalLabel.Text;
        c.PerKeyColor = chkPerKeyColor.IsChecked == true;
        c.KpsTotalFollowGlobal = chkKpsFollowGlobal.IsChecked == true;
        c.P108UnifiedColor = chk108Unified.IsChecked == true;
        c.PerKeyFontSizeEnabled = chkPksEnable.IsChecked == true;

        if (!c.PerKeyFontSizeEnabled && c.PerKeyFontSize != null)
            Array.Clear(c.PerKeyFontSize, 0, c.PerKeyFontSize.Length);

        SaveRainValues(c);

        try { c.Save(); } catch (Exception ex) { ShowToast($"保存失败: {ex.Message}"); }
        Graphics.GraphicsLog.Write("ApplyAndSave 完成，开始通知主线程刷新");
        try { _onChanged(); }
        catch (Exception ex) { Graphics.GraphicsLog.Write($"通知主线程刷新异常: {ex}"); }
    }

    private static float[] Arr3(Slider a, Slider b, Slider c2) =>
        new[] { (float)a.Value, (float)b.Value, (float)c2.Value };}
