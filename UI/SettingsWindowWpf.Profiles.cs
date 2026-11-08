using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using JipperKeyViewer.Config;
using JipperKeyViewer.Core;
using Microsoft.Win32;
using ComboBox = System.Windows.Controls.ComboBox;
using Button = System.Windows.Controls.Button;
using TextBox = System.Windows.Controls.TextBox;
using CheckBox = System.Windows.Controls.CheckBox;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Orientation = System.Windows.Controls.Orientation;

namespace JipperKeyViewer.UI;


public partial class SettingsWindowWpf
{


    public void RefreshAfterImport()
    {
        LoadProfiles();
        LoadValues();
    }


    private void LoadProfiles()
    {
        string current = ProfileManager.GetCurrentProfile();

        if (txSelectedProfile != null)
            txSelectedProfile.Text = UiLanguage.S("已选中配置方案：") + current;
        spProfiles.Children.Clear();
        var selBg = new SolidColorBrush(Color.FromArgb(0x40, 0x3D, 0x6E, 0xA5));
        foreach (var name in ProfileManager.GetProfileNames())
        {
            bool isCur = name == current;
            var border = new Border
            {
                Background = isCur ? selBg : Brushes.Transparent,
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4, 3, 4, 3),
                Margin = new Thickness(0, 0, 0, 2),
                Tag = name,
            };
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            var chk = new CheckBox
            {
                IsChecked = isCur,

                IsHitTestVisible = false,
                VerticalAlignment = VerticalAlignment.Center,
                Focusable = false,
                Tag = name,
            };
            var tb = new TextBlock
            {
                Text = name,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 0, 0),
            };
            row.Children.Add(chk);
            row.Children.Add(tb);
            border.Child = row;
            border.MouseLeftButtonUp += (_, _) => SelectProfile(name);
            spProfiles.Children.Add(border);
        }
        expConfig.Header = $"配置: {current}";
    }


    private void SelectProfile(string name)
    {
        try
        {
            string current = ProfileManager.GetCurrentProfile();
            if (name == current) return;
            ProfileManager.SaveProfile(_cfg, current);
            ProfileManager.LoadProfileInto(_cfg, name);
            ProfileManager.SetCurrentProfile(name);
            LoadValues();
            ApplyAndSave();
            LoadProfiles();
            ShowToast($"已切换到配置项“{name}”");
        }
        catch (Exception ex) { ShowToast($"切换配置项失败: {ex.Message}"); }
    }


    private void ConfigNewClick(object sender, RoutedEventArgs e)
    {
        try
        {
            string name = MakeUniqueName("Custom Config");
            ProfileManager.SaveProfile(_cfg, name);
            ProfileManager.SetCurrentProfile(name);
            ApplyAndSave();
            LoadProfiles();
            BeginInlineRename(name, isNew: true);
        }
        catch (Exception ex) { ShowToast($"新建失败: {ex.Message}"); }
    }


    private static readonly System.Media.SoundPlayer _sysAlertPlayer =
        new(@"C:\Windows\Media\Windows Background.wav");


    private static readonly System.Media.SoundPlayer _sysForegroundPlayer =
        new(@"C:\Windows\Media\Windows Foreground.wav");

    private static void PlaySystemAlert()
    {
        try { _sysAlertPlayer.Play(); }
        catch { }
    }

    private static void PlayForegroundAlert()
    {
        try { _sysForegroundPlayer.Play(); }
        catch { }
    }


    private void ConfigRenameClick(object sender, RoutedEventArgs e)
    {
        string current = ProfileManager.GetCurrentProfile();
        if (current == ProfileManager.DefaultProfile)
        {

            PlaySystemAlert();
            var dlg = new ConfirmDialog("重命名", "Default方案不可被编辑！", "error.png", false, "确定")
            {
                Owner = this,
            };
            dlg.ShowDialog();
            return;
        }
        BeginInlineRename(current, isNew: false);
    }


    private void ConfigDeleteClick(object sender, RoutedEventArgs e)
    {
        string current = ProfileManager.GetCurrentProfile();
        if (current == ProfileManager.DefaultProfile)
        {

            PlaySystemAlert();
            var dlg = new ConfirmDialog("删除配置项", "Default方案不可被编辑！", "error.png", false, "确定")
            {
                Owner = this,
            };
            dlg.ShowDialog();
            return;
        }


        PlaySystemAlert();
        var dlgDel = new ConfirmDialog("删除配置项", UiLanguage.S("是否删除配置项") + "\n“" + current + "”" + UiLanguage.S("？"), "warning.png", true, "是")
        {
            Owner = this,
        };
        if (dlgDel.ShowDialog() != true || !dlgDel.Confirmed) return;
        try
        {
            ProfileManager.DeleteProfile(current);
            LoadValues();
            ApplyAndSave();
            LoadProfiles();
            ShowToast($"配置项“{current}”已删除");
        }
        catch (Exception ex) { ShowToast($"删除失败: {ex.Message}"); }
    }


    private static string MakeUniqueName(string baseName)
    {
        var names = ProfileManager.GetProfileNames();
        if (!names.Any(n => n.Equals(baseName, StringComparison.OrdinalIgnoreCase))) return baseName;
        for (int i = 2; ; i++)
        {
            string cand = $"{baseName} {i}";
            if (!names.Any(n => n.Equals(cand, StringComparison.OrdinalIgnoreCase))) return cand;
        }
    }


    private void BeginInlineRename(string name, bool isNew)
    {
        foreach (var child in spProfiles.Children)
        {
            if (child is not Border b || (string)b.Tag != name) continue;
            if (b.Child is not StackPanel row) continue;
            var tb = row.Children.OfType<TextBlock>().FirstOrDefault();
            if (tb == null) return;
            var txt = new TextBox
            {
                Text = tb.Text,
                FontSize = 13,
                Width = 150,
                VerticalContentAlignment = VerticalAlignment.Center,
            };
            row.Children.Remove(tb);
            row.Children.Insert(1, txt);
            txt.Focus();
            txt.SelectAll();
            bool done = false;
            txt.KeyDown += (_, ev) =>
            {
                if (done) return;
                if (ev.Key == System.Windows.Input.Key.Enter) { done = true; CommitInlineRename(txt, name, isNew); }
                else if (ev.Key == System.Windows.Input.Key.Escape) { done = true; CancelInlineRename(row, txt, name, isNew); }
            };
            txt.LostFocus += (_, _) => { if (!done) { done = true; CommitInlineRename(txt, name, isNew); } };
            return;
        }
    }


    private void CommitInlineRename(TextBox txt, string oldName, bool isNew)
    {
        var row = txt.Parent as StackPanel;
        var border = row?.Parent as Border;
        string newName = txt.Text?.Trim() ?? "";

        row?.Children.Remove(txt);
        var tb = new TextBlock
        {
            Text = newName.Length == 0 ? oldName : newName,
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(6, 0, 0, 0),
        };
        row?.Children.Insert(1, tb);

        if (newName.Length == 0) { ShowToast("名称不能为空。"); LoadProfiles(); return; }

        bool duplicate = ProfileManager.GetProfileNames()
            .Any(n => !n.Equals(oldName, StringComparison.OrdinalIgnoreCase)
                   && n.Equals(newName, StringComparison.OrdinalIgnoreCase));
        if (duplicate)
        {

            PlaySystemAlert();
            var dlg = new ConfirmDialog("重命名", "已有相同名称的配置项！", "info.png", false, "确定")
            {
                Owner = this,
            };
            dlg.ShowDialog();
            LoadProfiles();
            return;
        }
        if (newName.Equals(oldName, StringComparison.OrdinalIgnoreCase)) { LoadProfiles(); return; }
        if (!ProfileManager.IsValidName(newName, out string err)) { ShowToast(err); LoadProfiles(); return; }
        try
        {
            if (isNew && oldName == newName) { LoadProfiles(); return; }
            ProfileManager.RenameProfile(oldName, newName);
            LoadProfiles();
            ShowToast($"配置项已重命名为“{newName}”");
        }
        catch (Exception ex) { ShowToast($"重命名失败: {ex.Message}"); }
    }


    private void CancelInlineRename(StackPanel row, TextBox txt, string oldName, bool isNew)
    {
        row.Children.Remove(txt);
        var tb = new TextBlock
        {
            Text = oldName,
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(6, 0, 0, 0),
        };
        row.Children.Insert(1, tb);
        if (isNew)
        {
            try
            {
                ProfileManager.DeleteProfile(oldName);
                ProfileManager.SetCurrentProfile(ProfileManager.DefaultProfile);
                LoadValues();
                ApplyAndSave();
            }
            catch { }
        }
        LoadProfiles();
    }

}
