using JipperKeyViewer.App;

namespace JipperKeyViewer;


internal static partial class Program
{
    private static readonly PerformanceMonitor Perf = new();

    private static void RunMainLoop()
    {

        long lastTick = Environment.TickCount64;
        int frameCount = 0;
        long fpsLogTick = lastTick;
        while (true)
        {
            while (PeekMessageW(out MSG msg, IntPtr.Zero, 0, 0, PM_REMOVE))
            {
                if (msg.message == WM_QUIT) return;
                TranslateMessage(ref msg);
                DispatchMessageW(ref msg);
            }


            if (_captureArmed && _hook != null)
            {
                var cap = _hook.TakeCaptureResult(out int capVk);
                if (cap != null)
                {
                    _captureArmed = false;
                    if (cap == true) ApplyCapture(capVk);
                    else NotifySettingsCapture(false, 0);
                }
            }


            RenderFrame();
            frameCount++;
            Perf.Frame(_hook?.Kps ?? 0, _hook?.TotalCount ?? 0,
                _hook?.KpsQueueLength ?? 0, _hook?.PerKeyQueueLength ?? 0);


            long now = Environment.TickCount64;
            if (now - fpsLogTick >= 60000)
            {
                int fps = (int)Math.Round(frameCount * 1000.0 / (now - fpsLogTick));
                Graphics.GraphicsLog.Write($"主循环帧率：{fps} fps，窗口客户区 {Window.ClientW}x{Window.ClientH}");
                frameCount = 0;
                fpsLogTick = now;
            }
        }

    }


    private static void RenderFrame()
    {
        if (_cfg == null || _renderer == null || !_renderer.Initialized || _keyRenderer == null) return;
        _keyRenderer.Render(_renderer, _cfg.Opacity);
        _renderer?.Present();
    }

}
