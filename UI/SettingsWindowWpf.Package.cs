using System.IO;
using System.Windows;
using JipperKeyViewer.Config;
using JipperKeyViewer.Core;

namespace JipperKeyViewer.UI;


public partial class SettingsWindowWpf
{


    private void ExportPackageClick(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "导出程序配置",
            Filter = "JipperKeyViewer 程序配置 (*.jkv)|*.jkv",
            FileName = $"{ProfileManager.GetCurrentProfile()}.jkv"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            using (var zip = System.IO.Compression.ZipFile.Open(dlg.FileName, System.IO.Compression.ZipArchiveMode.Create))
            {

                var nameEntry = zip.CreateEntry("name.txt");
                using (var nsw = new StreamWriter(nameEntry.Open()))
                    nsw.Write(ProfileManager.GetCurrentProfile());
                var entry = zip.CreateEntry("config.json");
                using (var sw = new StreamWriter(entry.Open()))
                    sw.Write(Newtonsoft.Json.JsonConvert.SerializeObject(_cfg, Newtonsoft.Json.Formatting.Indented));
                if (Directory.Exists(AppConfig.FontsDir))
                    foreach (var f in Directory.GetFiles(AppConfig.FontsDir))
                    {
                        var fe = zip.CreateEntry("fonts/" + Path.GetFileName(f));
                        using (var src = File.OpenRead(f))
                        using (var dst = fe.Open())
                            src.CopyTo(dst);
                    }
            }
            ShowToast($"分享包已导出：{dlg.FileName}");
        }
        catch (Exception ex) { ShowToast($"导出失败: {ex.Message}"); }
    }


    private void ImportPackageClick(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "导入程序配置",
            Filter = "JipperKeyViewer 程序配置 (*.jkv)|*.jkv|所有文件 (*.*)|*.*"
        };
        if (dlg.ShowDialog() != true) return;
        var result = Config.Import.PackageImporter.TryImport(dlg.FileName);
        if (result.Duplicate)
        {

            PlaySystemAlert();
            var dupDlg = new ConfirmDialog("导入配置", "已有相同名称的配置项！", "info.png", false, "确定")
            {
                Owner = this,
            };
            dupDlg.ShowDialog();
            return;
        }
        if (!result.Ok)
        {

            PlayForegroundAlert();
            var errDlg = new ConfirmDialog("导入配置", "配置文件已损坏或无效的文件！", "error.png", false, "确定")
            {
                Owner = this,
            };
            errDlg.ShowDialog();
            return;
        }
        try
        {
            string profileName = result.ProfileName!;
            ProfileManager.LoadProfileInto(_cfg, profileName);
            ProfileManager.SetCurrentProfile(profileName);
            LoadValues();
            ApplyAndSave();
            LoadProfiles();
            ShowToast($"分享包导入成功，方案“{profileName}”已应用");
        }
        catch (Exception ex) { ShowToast($"导入失败: {ex.Message}"); }
    }

    private void OpenFontsClick(object sender, RoutedEventArgs e)
    {
        try { System.Diagnostics.Process.Start("explorer.exe", AppConfig.FontsDir); }
        catch (Exception ex) { ShowToast($"无法打开字体文件夹: {ex.Message}"); }
    }

}
