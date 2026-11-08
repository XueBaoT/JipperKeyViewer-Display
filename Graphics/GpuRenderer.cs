using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.DCommon;
using Vortice.Direct2D1;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace JipperKeyViewer.Graphics;


public class GpuRenderer : TransparentRenderer
{
    private ID2D1Factory1? _factory;
    private ID2D1Device? _d2dDevice;
    protected ID2D1DeviceContext? Context;
    protected ID2D1Bitmap1? Target;
    private ID2D1SolidColorBrush? _solid;
    private readonly Dictionary<byte[], ID2D1Bitmap> _textures = new();
    private readonly Dictionary<(uint Top, uint Bottom), ID2D1LinearGradientBrush> _keyGradients = new();
    private readonly Dictionary<uint, ID2D1LinearGradientBrush> _rainGradients = new();
    private readonly Dictionary<(int W, int H, int PadKey, uint Color, int RadiusKey), ID2D1Bitmap> _glows = new();
    private bool _ready;

    protected bool DCompOverlay;

    public override void Init(nint hwnd, int width, int height)
    {
        if (DCompOverlay)
        {

            LayeredMode = true;
            base.Init(hwnd, width, height);
            try
            {
                EnsureD3DDevice();
                CreateD2DDevice();
                CreateDCompTargets();
                _solid = Context!.CreateSolidColorBrush(new Color4(1f, 1f, 1f, 1f));
                _ready = true;
                GraphicsLog.Write("DComp 覆盖层已启用（layered + composition swapchain + Present(1)）");
            }
            catch (Exception ex)
            {
                _ready = false;
                ReleaseDCompTargets();
                GraphicsLog.Write("DComp 覆盖层初始化失败，回退 CPU layered：" + ex.Message);
                DisposeD2D();
            }
            return;
        }

        base.Init(hwnd, width, height);
        if (LayeredMode) return;
        try
        {
            CreateD2D();
            _ready = true;
            GraphicsLog.Write("GPU 后端已启用（D2D1 / HWND 交换链）");
        }
        catch (Exception ex)
        {
            _ready = false;
            GraphicsLog.Write("GPU 后端初始化失败，回退 CPU：" + ex.Message);
            DisposeD2D();
        }
    }

    protected void CreateD2DDevice()
    {
        if (Device == null)
            throw new InvalidOperationException("D3D 设备为空");

        _factory = D2D1.D2D1CreateFactory<ID2D1Factory1>();
        using IDXGIDevice dxgi = Device.QueryInterface<IDXGIDevice>();
        _d2dDevice = _factory.CreateDevice(dxgi);
        Context = _d2dDevice.CreateDeviceContext(DeviceContextOptions.None);
        Context.AntialiasMode = AntialiasMode.PerPrimitive;
    }

    private void CreateD2D()
    {
        if (Device == null || SwapChain == null)
            throw new InvalidOperationException("D3D 设备/交换链为空");

        CreateD2DDevice();
        CreateTarget();
        _solid = Context!.CreateSolidColorBrush(new Color4(1f, 1f, 1f, 1f));
    }


    private void CreateTarget()
    {
        using IDXGISurface surface = SwapChain!.GetBuffer<IDXGISurface>(0);
        var props = new BitmapProperties1(
            new PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied),
            96f, 96f, BitmapOptions.Target | BitmapOptions.CannotDraw);
        Target = Context!.CreateBitmapFromDxgiSurface(surface, props);
        Context.Target = Target;
    }


    protected virtual void CreateDCompTargets() => throw new NotSupportedException();
    protected virtual void ReleaseDCompTargets() { }


    private void ReleaseTarget()
    {
        try { if (Context != null) Context.Target = null; } catch { }
        Target?.Dispose();
        Target = null;
    }

    public override void Resize(int width, int height)
    {
        if (DCompOverlay)
        {
            ReleaseDCompTargets();
            ClearTextures();
            ClearGlows();
            base.Resize(width, height);
            if (!_ready) return;
            try
            {
                CreateDCompTargets();
            }
            catch (Exception ex)
            {
                _ready = false;
                ReleaseDCompTargets();
                GraphicsLog.Write("DComp 覆盖层重建失败，回退 CPU：" + ex.Message);
                DisposeD2D();
            }
            return;
        }


        ReleaseTarget();
        ClearTextures();
        ClearGlows();
        base.Resize(width, height);
        if (!_ready) return;
        try
        {
            CreateTarget();
        }
        catch (Exception ex)
        {
            _ready = false;
            GraphicsLog.Write("GPU 重建目标失败，回退 CPU：" + ex.Message);
            DisposeD2D();
        }
    }

    public override void BeginFrame(Color4 clear)
    {
        if (!_ready || Context == null)
        {
            base.BeginFrame(clear);
            return;
        }
        Context.BeginDraw();
        Context.Clear(clear);
    }

    public override void Present()
    {
        if (!_ready || Context == null)
        {
            base.Present();
            return;
        }

        if (DCompOverlay)
        {
            if (SwapChain == null)
            {
                base.Present();
                return;
            }
            try
            {
                PresentDComp();
            }
            catch (Exception ex)
            {
                _ready = false;
                ReleaseDCompTargets();
                GraphicsLog.Write("DComp 覆盖层 Present 失败，回退 CPU：" + ex.Message);
                DisposeD2D();
                base.Present();
            }
            return;
        }

        if (SwapChain == null)
        {
            base.Present();
            return;
        }
        try
        {
            Context.EndDraw();
            SwapChain.Present(1, PresentFlags.None);
        }
        catch (Exception ex)
        {
            _ready = false;
            GraphicsLog.Write("GPU Present 失败，回退 CPU：" + ex.Message);
            DisposeD2D();
            base.Present();
        }
    }


    protected virtual void PresentDComp()
    {
        Context!.EndDraw();
        SwapChain!.Present(1, PresentFlags.None);
    }


    private void SetColor(Color4 c)
    {
        if (_solid != null) _solid.Color = c;
    }

    public override void FillRect(Rect r, Color4 color)
    {
        if (!_ready) { base.FillRect(r, color); return; }
        SetColor(color);
        Context!.FillRectangle(r, _solid!);
    }

    public override void FillRectBlend(Rect r, Color4 color)
    {
        if (!_ready) { base.FillRectBlend(r, color); return; }
        SetColor(color);
        Context!.FillRectangle(r, _solid!);
    }

    public override void DrawRect(Rect r, Color4 color, float stroke = 1f)
    {
        if (!_ready) { base.DrawRect(r, color, stroke); return; }
        SetColor(color);
        Context!.DrawRectangle(r, _solid!, stroke);
    }

    public override void FillRoundRect(Rect r, Color4 color, float radius)
    {
        if (!_ready) { base.FillRoundRect(r, color, radius); return; }
        SetColor(color);
        Context!.FillRoundedRectangle(ToRounded(r, radius), _solid!);
    }

    public override void DrawRoundRect(Rect r, Color4 color, float radius, float stroke = 1f)
    {
        if (!_ready) { base.DrawRoundRect(r, color, radius, stroke); return; }
        SetColor(color);
        Context!.DrawRoundedRectangle(ToRounded(r, radius), _solid!, stroke);
    }

    public override void FillRoundRectGradient(Rect r, Color4 top, Color4 bottom, float radius)
    {
        if (!_ready) { base.FillRoundRectGradient(r, top, bottom, radius); return; }
        ID2D1LinearGradientBrush brush = GetKeyGradient(top, bottom);
        brush.StartPoint = new Vector2(r.X, r.Y);
        brush.EndPoint = new Vector2(r.X, r.Y + r.Height);
        Context!.FillRoundedRectangle(ToRounded(r, radius), brush);
    }

    public override void DrawRoundRectGradient(Rect r, Color4 top, Color4 bottom, float radius, float stroke = 1f)
    {
        if (!_ready) { base.DrawRoundRectGradient(r, top, bottom, radius, stroke); return; }
        ID2D1LinearGradientBrush brush = GetKeyGradient(top, bottom);
        brush.StartPoint = new Vector2(r.X, r.Y);
        brush.EndPoint = new Vector2(r.X, r.Y + r.Height);
        Context!.DrawRoundedRectangle(ToRounded(r, radius), brush, stroke);
    }

    public override void FillGlow(Rect r, float pad, Color4 color, float radius = 0f)
    {
        if (!_ready) { base.FillGlow(r, pad, color, radius); return; }
        if (pad <= 0f || color.A <= 0f) return;

        int x0 = (int)Math.Floor(r.X - pad);
        int y0 = (int)Math.Floor(r.Y - pad);
        int x1 = (int)Math.Ceiling(r.X + r.Width + pad);
        int y1 = (int)Math.Ceiling(r.Y + r.Height + pad);
        int w = x1 - x0, h = y1 - y0;
        if (w <= 0 || h <= 0) return;

        int padKey = (int)Math.Round(pad * 4f);
        int radiusKey = (int)Math.Round(radius * 4f);
        uint ck = Pack(color);
        var key = (w, h, padKey, ck, radiusKey);
        if (!_glows.TryGetValue(key, out ID2D1Bitmap? bmp))
        {
            if (_glows.Count >= 128) ClearGlows();
            bmp = CreateGlowBitmap(r, pad, color, radius, x0, y0, w, h);
            _glows[key] = bmp;
        }
        Context!.DrawBitmap(bmp, new Rect(x0, y0, w, h), 1f,
            BitmapInterpolationMode.Linear, new Rect(0, 0, w, h));
    }

    public override void DrawRainBar(Rect r, Color4 color, float alpha, float dNear, float dFar, float trackH, float fadePx)
    {
        if (!_ready) { base.DrawRainBar(r, color, alpha, dNear, dFar, trackH, fadePx); return; }
        float aF = color.A * alpha;
        if (aF <= 0f || r.Height <= 0f) return;


        float span = r.Height;
        float dSpan = dFar - dNear;
        if (fadePx <= 0.01f || dSpan <= 0.001f)
        {
            SetColor(new Color4(color.R, color.G, color.B, aF));
            Context!.FillRectangle(r, _solid!);
            return;
        }

        float yTop = r.Y + (dFar - trackH) / dSpan * span;
        float yFade = r.Y + (dFar - (trackH - fadePx)) / dSpan * span;
        if (yFade <= yTop + 0.01f)
        {
            SetColor(new Color4(color.R, color.G, color.B, aF));
            Context!.FillRectangle(r, _solid!);
            return;
        }

        ID2D1LinearGradientBrush brush = GetRainGradient(color);
        brush.Opacity = aF;
        brush.StartPoint = new Vector2(r.X, yTop);
        brush.EndPoint = new Vector2(r.X, yFade);
        Context!.FillRectangle(r, brush);
    }

    private static RoundedRectangle ToRounded(Rect r, float radius)
        => new(new RectangleF(r.X, r.Y, r.Width, r.Height), radius, radius);

    private ID2D1LinearGradientBrush GetKeyGradient(Color4 top, Color4 bottom)
    {
        var key = (Pack(top), Pack(bottom));
        if (_keyGradients.TryGetValue(key, out ID2D1LinearGradientBrush? brush)) return brush;
        var stops = new[] { new GradientStop(0f, top), new GradientStop(1f, bottom) };
        using ID2D1GradientStopCollection collection = Context!.CreateGradientStopCollection(stops);
        var props = new LinearGradientBrushProperties(Vector2.Zero, new Vector2(0f, 1f));
        brush = Context.CreateLinearGradientBrush(props, collection);
        _keyGradients[key] = brush;
        return brush;
    }

    private ID2D1LinearGradientBrush GetRainGradient(Color4 color)
    {
        uint key = ((uint)ToByte(color.R) << 16) | ((uint)ToByte(color.G) << 8) | ToByte(color.B);
        if (_rainGradients.TryGetValue(key, out ID2D1LinearGradientBrush? brush)) return brush;
        var opaque = new Color4(color.R, color.G, color.B, 1f);
        var transparent = new Color4(color.R, color.G, color.B, 0f);
        var stops = new[] { new GradientStop(0f, transparent), new GradientStop(1f, opaque) };
        using ID2D1GradientStopCollection collection = Context!.CreateGradientStopCollection(stops);
        var props = new LinearGradientBrushProperties(Vector2.Zero, new Vector2(0f, 1f));
        brush = Context.CreateLinearGradientBrush(props, collection);
        _rainGradients[key] = brush;
        return brush;
    }

    private ID2D1Bitmap1 CreateGlowBitmap(Rect r, float pad, Color4 color, float radius, int x0, int y0, int w, int h)
    {
        float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f;
        float hw = r.Width / 2f, hh = r.Height / 2f;
        float rad = Math.Clamp(radius, 0f, Math.Min(hw, hh));
        float ix = hw - rad, iy = hh - rad;
        float invPad = 1f / pad;
        byte cr = ToByte(color.R), cg = ToByte(color.G), cb = ToByte(color.B);
        var px = new byte[w * h * 4];
        for (int y = 0; y < h; y++)
        {
            float py = y0 + y + 0.5f;
            float qy = Math.Abs(py - cy) - iy;
            float oy = MathF.Max(qy, 0f);
            for (int x = 0; x < w; x++)
            {
                float pxx = x0 + x + 0.5f;
                float qx = Math.Abs(pxx - cx) - ix;
                float ox = MathF.Max(qx, 0f);
                float dist = MathF.Sqrt(ox * ox + oy * oy) + MathF.Min(MathF.Max(qx, qy), 0f) - rad;
                if (dist <= 0f) continue;
                float a = color.A * GlowFalloff(1f - dist * invPad);
                if (a <= 0.002f) continue;
                int p = (y * w + x) * 4;
                px[p] = cb;
                px[p + 1] = cg;
                px[p + 2] = cr;
                px[p + 3] = (byte)Math.Clamp((int)Math.Round(a * 255f), 0, 255);
            }
        }
        return UploadStraight(px, w, h);
    }


    protected override void BlitSprite(byte[] px, int w, int h, int dx, int dy)
    {
        if (!_ready || Context == null)
        {
            base.BlitSprite(px, w, h, dx, dy);
            return;
        }
        ID2D1Bitmap bmp = GetTexture(px, w, h);
        Context.DrawBitmap(bmp, new Rect(dx, dy, w, h), 1f,
            BitmapInterpolationMode.Linear, new Rect(0, 0, w, h));
    }

    private ID2D1Bitmap GetTexture(byte[] px, int w, int h)
    {
        if (_textures.TryGetValue(px, out ID2D1Bitmap? cached)) return cached;
        ID2D1Bitmap bmp = UploadStraight(px, w, h);
        _textures[px] = bmp;
        return bmp;
    }


    private ID2D1Bitmap1 UploadStraight(byte[] px, int w, int h)
    {
        var pre = new byte[w * h * 4];
        for (int i = 0; i < w * h; i++)
        {
            int p = i * 4;
            byte a = px[p + 3];
            pre[p] = (byte)(px[p] * a / 255);
            pre[p + 1] = (byte)(px[p + 1] * a / 255);
            pre[p + 2] = (byte)(px[p + 2] * a / 255);
            pre[p + 3] = a;
        }

        GCHandle pin = GCHandle.Alloc(pre, GCHandleType.Pinned);
        try
        {
            var props = new BitmapProperties1(
                new PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied));
            return Context!.CreateBitmap(
                new SizeI(w, h), pin.AddrOfPinnedObject(), (uint)(w * 4), props);
        }
        finally
        {
            pin.Free();
        }
    }

    private void ClearGlows()
    {
        foreach (ID2D1Bitmap b in _glows.Values) b.Dispose();
        _glows.Clear();
    }

    private static byte ToByte(float v) => (byte)Math.Clamp((int)Math.Round(v * 255f), 0, 255);

    private static uint Pack(Color4 c)
        => ((uint)ToByte(c.R) << 24) | ((uint)ToByte(c.G) << 16) | ((uint)ToByte(c.B) << 8) | ToByte(c.A);

    private void ClearTextures()
    {
        foreach (ID2D1Bitmap b in _textures.Values) b.Dispose();
        _textures.Clear();
    }

    public override void ClearTextSprites()
    {
        ClearTextures();
        base.ClearTextSprites();
    }

    protected override void OnSpriteEvicted(byte[] pixels)
    {
        if (_textures.Remove(pixels, out ID2D1Bitmap? bmp))
            bmp.Dispose();
    }

    private void DisposeD2D()
    {
        ClearTextures();
        ClearGlows();
        foreach (ID2D1LinearGradientBrush b in _keyGradients.Values) b.Dispose();
        _keyGradients.Clear();
        foreach (ID2D1LinearGradientBrush b in _rainGradients.Values) b.Dispose();
        _rainGradients.Clear();
        try { if (Context != null) Context.Target = null; } catch { }
        Target?.Dispose(); Target = null;
        _solid?.Dispose(); _solid = null;
        Context?.Dispose(); Context = null;
        _d2dDevice?.Dispose(); _d2dDevice = null;
        _factory?.Dispose(); _factory = null;
    }

    public override void Dispose()
    {
        if (DCompOverlay) ReleaseDCompTargets();
        DisposeD2D();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
