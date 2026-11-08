using JipperKeyViewer.Graphics;

namespace JipperKeyViewer.App;


internal static class RendererFactory
{
    public static TransparentRenderer Create(bool overlay)
    {
        if (Environment.GetEnvironmentVariable("JKV_GPU") == "0")
            return new TransparentRenderer();
        return overlay ? new GpuDCompOverlayRenderer() : new GpuRenderer();
    }
}
