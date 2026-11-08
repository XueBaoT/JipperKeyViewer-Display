using System.Diagnostics;

namespace JipperKeyViewer;


internal static partial class Program
{
    private static Mutex? _singleInstance;

    [STAThread]
    private static int Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) => WriteCrashLog(e.ExceptionObject as Exception);

        if (args.Length > 0 && !args[0].StartsWith("--", StringComparison.Ordinal))
            _pendingPackage = args[0];
        try
        {


            bool isRestart = args.Any(a => string.Equals(a, "--restart", StringComparison.OrdinalIgnoreCase));
        _singleInstance = new Mutex(true, "JipperKeyViewer_SingleInstance_Mutex", out bool createdNew);
        if (!createdNew && isRestart)
        {

            for (int i = 0; i < 25 && !createdNew; i++)
            {
                System.Threading.Thread.Sleep(200);
                _singleInstance.Dispose();
                _singleInstance = new Mutex(true, "JipperKeyViewer_SingleInstance_Mutex", out createdNew);
            }
            Graphics.GraphicsLog.Write($"重启子进程获取互斥体：{createdNew}");
        }
            var procs = System.Diagnostics.Process.GetProcessesByName("JipperKeyViewer");
            if (!createdNew)
            {


                Graphics.GraphicsLog.Write($"检测到已有实例（互斥占用），转交打开请求并呼出已有窗口到前台");
                if (!string.IsNullOrEmpty(_pendingPackage))
                    ForwardPackageToExistingInstance(_pendingPackage);
                BringExistingWindowToFront();
                return 0;
            }
            if (procs.Length > 1)
            {


                if (!string.IsNullOrEmpty(_pendingPackage))
                    ForwardPackageToExistingInstance(_pendingPackage);
                if (BringExistingWindowToFront())
                    return 0;
            }
            return Run();
        }
        catch (Exception ex)
        {
            WriteCrashLog(ex);
            return 1;
        }
    }


    private static bool BringExistingWindowToFront()
    {
        try
        {
            nint hwnd = FindWindowW(ClassName, null);
            if (hwnd == IntPtr.Zero)
                hwnd = FindWindowW(null, "JipperKey Viewer Display");
            if (hwnd == IntPtr.Zero)
            {
                Graphics.GraphicsLog.Write("未找到已有实例的窗口句柄");
                return false;
            }
            if (IsIconic(hwnd)) ShowWindow(hwnd, SW_RESTORE);
            SetForegroundWindow(hwnd);
            Graphics.GraphicsLog.Write("已呼出已有窗口到前台");
            return true;
        }
        catch (Exception ex)
        {
            Graphics.GraphicsLog.Write($"呼出已有窗口异常: {ex.Message}");
            return false;
        }
    }

}
