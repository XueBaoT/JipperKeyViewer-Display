using System.Runtime.InteropServices;
using JipperKeyViewer.Config;

namespace JipperKeyViewer;


internal static partial class Program
{

    private static string? _pendingPackage;
    private static string? _pendingPackageError;


    private static void ForwardPackageToExistingInstance(string path)
    {
        try
        {
            nint hwnd = FindWindowW(ClassName, null);
            if (hwnd == IntPtr.Zero) hwnd = FindWindowW(null, "JipperKey Viewer Display");
            if (hwnd == IntPtr.Zero) return;
            nint mem = Marshal.StringToHGlobalUni(path);
            try
            {
                var cds = new COPYDATASTRUCT
                {
                    dwData = new nint(1),
                    cbData = (uint)((path.Length + 1) * 2),
                    lpData = mem,
                };
                SendMessageW(hwnd, WM_COPYDATA, IntPtr.Zero, ref cds);
                Graphics.GraphicsLog.Write($"已转交 .jkv 打开请求到已有实例：{path}");
            }
            finally { Marshal.FreeHGlobal(mem); }
        }
        catch (Exception ex) { Graphics.GraphicsLog.Write("转交 .jkv 打开请求失败：" + ex.Message); }
    }


    private static void EnsurePackageIcon()
    {
        try
        {
            string root = Path.GetDirectoryName(Environment.ProcessPath)
                          ?? Environment.CurrentDirectory;
            string dir = Path.Combine(root, "JipperKeyViewer");
            Directory.CreateDirectory(dir);
            string ico = Path.Combine(dir, "jkv.ico");
            if (File.Exists(ico) && new FileInfo(ico).Length > 0) return;
            var asm = typeof(Program).Assembly;
            string? res = asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("jkv.ico", StringComparison.OrdinalIgnoreCase));
            if (res == null) { Graphics.GraphicsLog.Write("未找到内嵌的 jkv.ico 资源"); return; }
            using var src = asm.GetManifestResourceStream(res);
            if (src == null) { Graphics.GraphicsLog.Write("内嵌 jkv.ico 资源流为空"); return; }
            using var dst = File.Create(ico);
            src.CopyTo(dst);
            Graphics.GraphicsLog.Write($"已释放 .jkv 关联图标到 {ico}");
        }
        catch (Exception ex) { Graphics.GraphicsLog.Write("释放 .jkv 图标失败：" + ex.Message); }
    }


    private static void RegisterJkvAssociation()
    {
        try
        {
            string exe = Environment.ProcessPath ?? string.Empty;
            if (exe.Length == 0) return;
            string dir = Path.GetDirectoryName(exe) ?? string.Empty;
            string ico = Path.Combine(dir, "JipperKeyViewer", "jkv.ico");
            string progId = "JipperKeyViewer.jkvfile";
            string command = $"\"{exe}\" \"%1\"";

            string iconValue = File.Exists(ico) ? $"\"{ico}\",0" : $"\"{exe}\",0";
            bool changed = false;

            using (var classes = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Classes"))
            {

                using (var ext = classes.CreateSubKey(".jkv"))
                {
                    string cur = ext.GetValue("") as string ?? "";
                    if (!cur.Equals(progId, StringComparison.OrdinalIgnoreCase))
                    { ext.SetValue("", progId); changed = true; }
                }

                using (var pi = classes.CreateSubKey(progId))
                using (var di = pi.CreateSubKey("DefaultIcon"))
                {
                    string cur = di.GetValue("") as string ?? "";
                    if (!cur.Equals(iconValue, StringComparison.OrdinalIgnoreCase))
                    { di.SetValue("", iconValue); changed = true; }
                }

                using (var pi = classes.CreateSubKey(progId))
                using (var shell = pi.CreateSubKey("shell"))
                using (var open = shell.CreateSubKey("open"))
                using (var cmd = open.CreateSubKey("command"))
                {
                    string cur = cmd.GetValue("") as string ?? "";
                    if (!cur.Equals(command, StringComparison.OrdinalIgnoreCase))
                    { cmd.SetValue("", command); changed = true; }
                }
            }
            if (changed)
            {

                SHChangeNotify(SHCNE_ASSOCCHANGED | SHCNE_FLUSH, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
                Graphics.GraphicsLog.Write("已注册 .jkv 文件关联并刷新 Explorer（图标 + 打开方式）");
            }
        }
        catch (Exception ex) { Graphics.GraphicsLog.Write("注册 .jkv 文件关联失败：" + ex.Message); }
    }


    private static void ImportPackageFromDrop(string path)
    {
        var r = Config.Import.PackageImporter.TryImport(path);
        if (r.Duplicate)
        {

            Graphics.GraphicsLog.Write($"导入分享包失败：方案重名 {path}");
            PlaySystemAlert();
            var dupDlg = new UI.ConfirmDialog("导入配置", "已有相同名称的配置项！", "info.png", false, "确定");
            if (_hwnd != IntPtr.Zero)
                _ = new System.Windows.Interop.WindowInteropHelper(dupDlg) { Owner = _hwnd };
            dupDlg.ShowDialog();
            return;
        }
        if (!r.Ok)
        {
            Graphics.GraphicsLog.Write($"拖放导入分享包失败：{r.Error}");
            ShowImportErrorDialog();
            return;
        }
        try
        {
            ProfileManager.LoadProfileInto(_cfg!, r.ProfileName!);
            ProfileManager.SetCurrentProfile(r.ProfileName!);
            _cfg!.Save();
            _hook?.RestoreCounts(_cfg.KeyCounts ?? new(), _cfg.TotalCount);
            OnSettingsChanged();

            if (_settings != null)
            {
                try { _settings.Dispatcher.Invoke(() => _settings.RefreshAfterImport()); } catch { }
            }
            Graphics.GraphicsLog.Write($"拖放导入分享包成功：{r.ProfileName}");
        }
        catch (Exception ex) { WriteCrashLog(ex); }
    }


    private static void ShowImportErrorDialog()
    {
        try
        {
            PlayForegroundAlert();
            var dlg = new UI.ConfirmDialog("导入配置", "配置文件已损坏或无效的文件！", "error.png", false, "确定");
            if (_hwnd != IntPtr.Zero)
                _ = new System.Windows.Interop.WindowInteropHelper(dlg) { Owner = _hwnd };
            dlg.ShowDialog();
        }
        catch (Exception ex) { WriteCrashLog(ex); }
    }


    private static readonly System.Media.SoundPlayer _sysForegroundPlayer =
        new(@"C:\Windows\Media\Windows Foreground.wav");

    private static void PlayForegroundAlert()
    {
        try { _sysForegroundPlayer.Play(); }
        catch { }
    }


    private static readonly System.Media.SoundPlayer _sysAlertPlayer =
        new(@"C:\Windows\Media\Windows Background.wav");

    private static void PlaySystemAlert()
    {
        try { _sysAlertPlayer.Play(); }
        catch { }
    }}
