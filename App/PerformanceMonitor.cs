using System.Diagnostics;
using JipperKeyViewer.Graphics;

namespace JipperKeyViewer.App;


// Emits one [PERF] line every five seconds; no PerformanceCounter.
internal sealed class PerformanceMonitor
{
    private const int IntervalMs = 5000;
    private readonly Process _process = Process.GetCurrentProcess();
    private readonly int _cores = Math.Max(1, Environment.ProcessorCount);
    private long _startWall;
    private long _lastWall;
    private double _lastCpuMs;
    private int _frames;

    public void Frame(int kps, long total, int kpsQueue, int perKeyQueue)
    {
        long now = Environment.TickCount64;
        if (_startWall == 0)
        {
            _startWall = now;
            _lastWall = now;
            _lastCpuMs = ReadCpuMs();
            return;
        }

        _frames++;
        long elapsed = now - _lastWall;
        if (elapsed < IntervalMs) return;

        try
        {
            _process.Refresh();
            double cpuMs = Math.Max(0, ReadCpuMs() - _lastCpuMs);
            double cpuPct = cpuMs / (elapsed * _cores) * 100.0;
            double fps = _frames * 1000.0 / elapsed;
            long ws = _process.WorkingSet64;
            long priv = _process.PrivateMemorySize64;
            long managed = GC.GetTotalMemory(false);
            int handles = _process.HandleCount;
            int threads = _process.Threads.Count;
            double uptime = (now - _startWall) / 1000.0;

            GraphicsLog.Write(FormattableString.Invariant(
                $"[PERF] up={uptime:F1}s cpu={cpuPct:F1}% cpu_ms={cpuMs:F0} ws={ws / 1048576.0:F1}MB private={priv / 1048576.0:F1}MB managed={managed / 1048576.0:F2}MB handles={handles} threads={threads} fps={fps:F1} kps={kps} total={total} kps_q={kpsQueue} perkey_q={perKeyQueue}"));
        }
        catch (Exception ex)
        {
            GraphicsLog.Write("[PERF] 采样失败：" + ex.Message);
        }
        finally
        {
            _lastWall = now;
            _lastCpuMs = ReadCpuMs();
            _frames = 0;
        }
    }

    private double ReadCpuMs()
    {
        try { return _process.TotalProcessorTime.TotalMilliseconds; }
        catch { return 0; }
    }
}
