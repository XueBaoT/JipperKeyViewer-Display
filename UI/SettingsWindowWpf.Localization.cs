using System.Windows.Controls;
using JipperKeyViewer.Config;

namespace JipperKeyViewer.UI;


public partial class SettingsWindowWpf
{


    private bool _langSyncing;


    private static readonly string[] OverlayTipLines =
    {
        "用于将KeyViewer叠加在屏幕上的覆盖层选项：",
        "启用并保存：在当前KeyViewer显示窗口的位置立即重建一个透明背景的全局覆盖层，窗口位置锁定并强制置顶，支持无边框以及独占全屏。",
        "启用后：OBS内的游戏捕获会失效，且覆盖层会穿透所有对窗口的交互操作。",
        "使用“按键”页面中已配置的快捷键可快速打开设置UI关闭此选项，或在系统右下角托盘快速关闭。",
        "请在启用此选项之前，把窗口的大小与位置调整到需要的位置，再启用此选项。",
    };


    private void UpdateOverlayTip()
    {
        try
        {
            if (tbOverlayTip == null) return;
            var lines = new System.Text.StringBuilder();
            foreach (string line in OverlayTipLines)
            {
                if (lines.Length > 0) lines.Append('\n');
                lines.Append(UiLanguage.S(line));
            }
            tbOverlayTip.Text = lines.ToString();
        }
        catch (Exception ex)
        {
            Graphics.GraphicsLog.Write("刷新覆盖层说明失败：" + ex.Message);
        }
    }

    private void SyncLanguageUi()
    {
        if (_langSyncing) return;
        _langSyncing = true;
        try
        {
            string lang = UiLanguage.Normalize(_cfg.Language);
            _cfg.Language = lang;

            int want = 1;
            for (int i = 0; i < cmbLanguage.Items.Count; i++)
            {
                if ((cmbLanguage.Items[i] as ComboBoxItem)?.Content?.ToString() == lang) { want = i; break; }
            }
            if (cmbLanguage.SelectedIndex != want) cmbLanguage.SelectedIndex = want;

            UiLanguage.Apply(this, lang);
            UpdateOverlayTip();
            Title = UiLanguage.S("配置项");
            Graphics.GraphicsLog.Write($"语言同步：配置={lang} 选择器索引={cmbLanguage.SelectedIndex} 文案语言={UiLanguage.Current}");
        }
        catch (Exception ex)
        {
            Graphics.GraphicsLog.Write("语言同步异常：" + ex.Message);
        }
        finally
        {
            _langSyncing = false;
        }
    }
}
