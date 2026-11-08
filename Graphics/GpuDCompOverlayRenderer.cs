using Vortice.DCommon;
using Vortice.Direct2D1;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace JipperKeyViewer.Graphics;


// Overlay path: layered + composition swapchain + DComp.
// The composition buffer cannot be a D2D target, so draw offscreen,
// CopyResource into the current buffer, then Present(1).
public sealed class GpuDCompOverlayRenderer : GpuRenderer
{
    private ID3D11Texture2D? _offscreen;
    private IDXGISwapChain3? _swap3;
    private IDCompositionDevice? _dcomp;
    private IDCompositionTarget? _dcompTarget;
    private IDCompositionVisual? _dcompVisual;

    public GpuDCompOverlayRenderer()
    {
        DCompOverlay = true;
    }

    protected override void CreateDCompTargets()
    {
        ReleaseDCompTargets();
        if (Device == null) throw new InvalidOperationException("D3D 设备为空");
        int w = PixelWidth, h = PixelHeight;
        if (w <= 0 || h <= 0) throw new InvalidOperationException("覆盖层尺寸无效");

        using IDXGIDevice dxgi = Device.QueryInterface<IDXGIDevice>();
        using IDXGIAdapter adapter = dxgi.GetAdapter();
        IDXGIFactory2 factory = adapter.GetParent<IDXGIFactory2>();

        var desc = new SwapChainDescription1
        {
            Width = (uint)w,
            Height = (uint)h,
            Format = Format.B8G8R8A8_UNorm,
            Stereo = false,
            SampleDescription = new SampleDescription(1, 0),
            BufferUsage = Usage.RenderTargetOutput,
            BufferCount = 2,
            Scaling = Scaling.Stretch,
            SwapEffect = SwapEffect.FlipSequential,
            AlphaMode = Vortice.DXGI.AlphaMode.Premultiplied,
            Flags = SwapChainFlags.None,
        };
        SwapChain = factory.CreateSwapChainForComposition(Device, desc);
        _swap3 = SwapChain.QueryInterface<IDXGISwapChain3>();

        _dcomp = DirectComp.CreateDevice(dxgi.NativePointer);
        if (_dcomp == null) throw new InvalidOperationException("DCompositionCreateDevice 失败");
        int hr1 = _dcomp.CreateTargetForHwnd(WindowHandle, true, out IDCompositionTarget target);
        int hr2 = _dcomp.CreateVisual(out IDCompositionVisual visual);
        _dcompTarget = target;
        _dcompVisual = visual;
        int hrc = _dcompVisual.SetContent(SwapChain.NativePointer);
        nint vp = System.Runtime.InteropServices.Marshal.GetIUnknownForObject(_dcompVisual);
        int hrr = _dcompTarget.SetRoot(vp);
        System.Runtime.InteropServices.Marshal.Release(vp);
        int hrcm = _dcomp.Commit();
        GraphicsLog.Write($"DComp: target=0x{hr1:X8} visual=0x{hr2:X8} content=0x{hrc:X8} root=0x{hrr:X8} commit=0x{hrcm:X8}");
        if (hr1 != 0 || hr2 != 0 || hrc != 0 || hrr != 0 || hrcm != 0)
            throw new InvalidOperationException("DComp 目标/视觉创建失败");

        var texDesc = new Texture2DDescription
        {
            Width = (uint)w,
            Height = (uint)h,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.B8G8R8A8_UNorm,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.RenderTarget,
            CPUAccessFlags = CpuAccessFlags.None,
            MiscFlags = ResourceOptionFlags.None,
        };
        _offscreen = Device.CreateTexture2D(texDesc);
        using IDXGISurface surface = _offscreen.QueryInterface<IDXGISurface>();
        var props = new BitmapProperties1(
            new PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied),
            96f, 96f, BitmapOptions.Target | BitmapOptions.CannotDraw);
        Target = Context!.CreateBitmapFromDxgiSurface(surface, props);
        Context.Target = Target;
        GraphicsLog.Write($"DComp 覆盖层已就绪：{w}x{h} premul flipseq（离屏 D2D → GPU copy → Present(1)）");
    }

    protected override void ReleaseDCompTargets()
    {
        try { if (Context != null) Context.Target = null; } catch { }
        Target?.Dispose();
        Target = null;
        _offscreen?.Dispose();
        _offscreen = null;
        _dcompVisual = null;
        _dcompTarget = null;
        _dcomp = null;
        _swap3?.Dispose();
        _swap3 = null;
        SwapChain?.Dispose();
        SwapChain = null;
    }

    protected override void PresentDComp()
    {
        if (Context == null || SwapChain == null || _swap3 == null || _offscreen == null)
            throw new InvalidOperationException("DComp 覆盖层资源未就绪");
        Context.EndDraw();
        ID3D11DeviceContext ctx = D3DDeviceContext!;
        uint idx = _swap3.CurrentBackBufferIndex;
        using ID3D11Texture2D buffer = SwapChain.GetBuffer<ID3D11Texture2D>(idx);
        ctx.CopyResource(buffer, _offscreen);
        SwapChain.Present(1, PresentFlags.None);
    }
}
