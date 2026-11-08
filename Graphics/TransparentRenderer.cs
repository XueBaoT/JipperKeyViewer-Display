using System.IO;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using GdiGraphics = System.Drawing.Graphics;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace JipperKeyViewer.Graphics;


public enum TextAlign
{
    Center,
    Top,
    Bottom,
    Left,
    Right,
}


public struct TextStyleFx
{

    public static readonly TextStyleFx None = new();

    public bool Outline;
    public Color4 OutlineColor;

    public float OutlinePx;

    public bool Shadow;
    public Color4 ShadowColor;

    public float ShadowX;

    public float ShadowY;

    public float ShadowSoftPx;


    public bool TextGradient;
    public Color4 GradientLeft;
    public Color4 GradientRight;
}


public static class GraphicsLog
{
    private static readonly object Lock = new();

    public static void Write(string message)
    {
        try
        {
            string dir = Path.GetDirectoryName(Environment.ProcessPath) ?? Environment.CurrentDirectory;
            string log = Path.Combine(dir, "JipperKeyViewer-debug.log");
            lock (Lock)
                File.AppendAllText(log, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n");
        }
        catch { }
    }
}


// Normal mode keeps a Discard/Unspecified HWND swapchain and Present(1)
// for the OBS WGC capture contract; do not change this path to flip/NOREDIRECTION.
public class TransparentRenderer : IRenderBackend, IDisposable
{
    private ID3D11Device? _device;
    private ID3D11DeviceContext? _context;
    private IDXGISwapChain? _swapChain;


    public bool LayeredMode { get; set; }
    private IntPtr _dib, _dibBits, _memDC;
    private int _dibW, _dibH;
    private ID3D11Texture2D? _backBuffer;

    private nint _hwnd;
    private int _width, _height;


    private byte[] _pixels = Array.Empty<byte>();


    private byte[] _presentPixels = Array.Empty<byte>();


    private sealed class SpriteEntry
    {
        public int W;
        public int H;
        public byte[] Pixels = Array.Empty<byte>();
        public LinkedListNode<string>? Node;
    }

    private const int SpriteCacheCapacity = 256;
    private readonly Dictionary<string, SpriteEntry> _sprites = new();
    private readonly LinkedList<string> _spriteLru = new();

    public bool Initialized { get; private set; }

    private System.Drawing.FontFamily? _textFontFamily;
    private int _textFontVersion = -1;


    public System.Drawing.FontFamily? TextFontFamily
    {
        get
        {
            if (FontManager.Version != _textFontVersion)
            {
                _textFontVersion = FontManager.Version;
                _textFontFamily = FontManager.Get(_configFontName)
                    ?? FontManager.Get(FontManager.MapleStoryDisplay)
                    ?? _defaultFamily;
            }
            return _textFontFamily;
        }
        set
        {
            _textFontFamily = value;
            _textFontVersion = FontManager.Version;
        }
    }


    private string _configFontName = "";
    public string ConfigFontName { get => _configFontName; set { _configFontName = value; } }

    private System.Drawing.FontFamily? _defaultFamily;
    public System.Drawing.FontFamily? DefaultFamily { get => _defaultFamily; set => _defaultFamily = value; }

    public virtual void ClearTextSprites()
    {
        _sprites.Clear();
        _spriteLru.Clear();
        _textMetrics.Clear();
    }

    private bool TryGetSprite(string key, out (int W, int H, byte[] Pixels) value)
    {
        if (_sprites.TryGetValue(key, out SpriteEntry? entry))
        {
            if (entry.Node != null)
            {
                _spriteLru.Remove(entry.Node);
                _spriteLru.AddFirst(entry.Node);
            }
            value = (entry.W, entry.H, entry.Pixels);
            return true;
        }
        value = default;
        return false;
    }

    private void StoreSprite(string key, int w, int h, byte[] pixels)
    {
        if (_sprites.TryGetValue(key, out SpriteEntry? entry))
        {
            entry.W = w;
            entry.H = h;
            entry.Pixels = pixels;
            if (entry.Node != null)
            {
                _spriteLru.Remove(entry.Node);
                _spriteLru.AddFirst(entry.Node);
            }
            return;
        }

        var node = _spriteLru.AddFirst(key);
        _sprites[key] = new SpriteEntry { W = w, H = h, Pixels = pixels, Node = node };
        if (_sprites.Count <= SpriteCacheCapacity) return;

        LinkedListNode<string>? last = _spriteLru.Last;
        if (last == null) return;
        _spriteLru.RemoveLast();
        if (_sprites.Remove(last.Value, out SpriteEntry? evicted))
            OnSpriteEvicted(evicted.Pixels);
    }


    protected virtual void OnSpriteEvicted(byte[] pixels) { }

    public virtual void Init(nint hwnd, int width, int height)
    {
        _hwnd = hwnd;
        _width = width;
        _height = height;
        EnsurePixelCapacity(width, height);
        if (LayeredMode)
        {

            GraphicsLog.Write("分层窗口呈现已启用（无 D3D 交换链）");
        }
        else
        {
            CreateResources();
        }
        Initialized = true;
    }


    private void EnsurePixelCapacity(int width, int height)
    {
        int need = width * height * 4;
        if (_pixels == null || _pixels.Length < need)
            _pixels = new byte[need];
        if (_presentPixels == null || _presentPixels.Length < need)
            _presentPixels = new byte[need];
    }

    private void CreateResources()
    {
        _device = D3D11.D3D11CreateDevice(
            Vortice.Direct3D.DriverType.Hardware, DeviceCreationFlags.BgraSupport,
            Vortice.Direct3D.FeatureLevel.Level_11_0);
        _context = _device.ImmediateContext;


        using IDXGIDevice dxgiDevice = _device.QueryInterface<IDXGIDevice>();
        using IDXGIAdapter adapter = dxgiDevice.GetAdapter();
        IDXGIFactory2 factory = adapter.GetParent<IDXGIFactory2>();


        SwapChainDescription1 desc = new()
        {
            Width = (uint)_width,
            Height = (uint)_height,
            Format = Format.B8G8R8A8_UNorm,
            Stereo = false,
            SampleDescription = new SampleDescription(1, 0),
            BufferUsage = Usage.RenderTargetOutput,
            BufferCount = 2,
            Scaling = Scaling.Stretch,
            SwapEffect = SwapEffect.Discard,
            AlphaMode = Vortice.DXGI.AlphaMode.Unspecified,
            Flags = SwapChainFlags.None,
        };
        _swapChain = factory.CreateSwapChainForHwnd(_device, _hwnd, desc);
        _backBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0);
        GraphicsLog.Write("使用 HWND 交换链（无逐像素透明）");
    }


    public int ClientWidth => _width;
    public int ClientHeight => _height;


    protected ID3D11Device? Device => _device;
    protected ID3D11DeviceContext? D3DDeviceContext => _context;
    protected IDXGISwapChain? SwapChain { get => _swapChain; set => _swapChain = value; }
    protected nint WindowHandle => _hwnd;
    protected int PixelWidth => _width;
    protected int PixelHeight => _height;


    protected bool EnsureD3DDevice()
    {
        if (_device != null) return true;
        _device = D3D11.D3D11CreateDevice(
            Vortice.Direct3D.DriverType.Hardware, DeviceCreationFlags.BgraSupport,
            Vortice.Direct3D.FeatureLevel.Level_11_0);
        _context = _device.ImmediateContext;
        return true;
    }


    public virtual void Resize(int width, int height)
    {
        if (width <= 0 || height <= 0) return;
        if (LayeredMode)
        {
            _width = width;
            _height = height;
            EnsurePixelCapacity(width, height);
            _dibW = 0;
            return;
        }


        _width = width;
        _height = height;
        EnsurePixelCapacity(width, height);
        ClearTextSprites();

        if (_swapChain == null)
        {

            CreateResources();
            return;
        }


        _backBuffer?.Dispose();
        _backBuffer = null;
        try
        {
            _swapChain.ResizeBuffers(2, (uint)width, (uint)height, Format.B8G8R8A8_UNorm, SwapChainFlags.None);
            _backBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0);
            GraphicsLog.Write($"ResizeBuffers 成功：{width}x{height}");
        }
        catch (Exception ex)
        {
            GraphicsLog.Write($"ResizeBuffers 失败，回退整体重建：{ex.Message}");
            _backBuffer?.Dispose(); _backBuffer = null;
            _swapChain?.Dispose(); _swapChain = null;
            CreateResources();
        }
    }


    public virtual void BeginFrame(Color4 clear)
    {
        byte a = ToByte(clear.A);
        byte r = ToByte(clear.R);
        byte g = ToByte(clear.G);
        byte b = ToByte(clear.B);
        int n = _width * _height;
        for (int i = 0; i < n; i++)
        {
            _pixels[i * 4 + 0] = b;
            _pixels[i * 4 + 1] = g;
            _pixels[i * 4 + 2] = r;
            _pixels[i * 4 + 3] = a;
        }
    }


    public virtual void FillRect(Rect r, Color4 color)
    {
        int x0 = Clamp((int)Math.Floor(r.X), 0, _width);
        int y0 = Clamp((int)Math.Floor(r.Y), 0, _height);
        int x1 = Clamp((int)Math.Ceiling(r.X + r.Width), 0, _width);
        int y1 = Clamp((int)Math.Ceiling(r.Y + r.Height), 0, _height);
        if (x1 <= x0 || y1 <= y0) return;

        byte a = ToByte(color.A), r8 = ToByte(color.R), g8 = ToByte(color.G), b8 = ToByte(color.B);
        for (int y = y0; y < y1; y++)
        {
            int row = y * _width;
            for (int x = x0; x < x1; x++)
            {
                int p = (row + x) * 4;
                _pixels[p] = b8;
                _pixels[p + 1] = g8;
                _pixels[p + 2] = r8;
                _pixels[p + 3] = a;
            }
        }
    }


    public virtual void FillRectBlend(Rect r, Color4 color)
    {
        int x0 = Clamp((int)Math.Floor(r.X), 0, _width);
        int y0 = Clamp((int)Math.Floor(r.Y), 0, _height);
        int x1 = Clamp((int)Math.Ceiling(r.X + r.Width), 0, _width);
        int y1 = Clamp((int)Math.Ceiling(r.Y + r.Height), 0, _height);
        if (x1 <= x0 || y1 <= y0) return;

        float a = color.A;
        if (a <= 0f) return;
        byte cr = ToByte(color.R), cg = ToByte(color.G), cb = ToByte(color.B);
        if (a >= 1f)
        {
            FillRect(r, color);
            return;
        }
        float inv = 1f - a;
        for (int y = y0; y < y1; y++)
        {
            int row = y * _width;
            for (int x = x0; x < x1; x++)
            {
                int p = (row + x) * 4;
                float dstA = _pixels[p + 3] / 255f;
                _pixels[p] = (byte)(cb * a + _pixels[p] * inv);
                _pixels[p + 1] = (byte)(cg * a + _pixels[p + 1] * inv);
                _pixels[p + 2] = (byte)(cr * a + _pixels[p + 2] * inv);
                _pixels[p + 3] = (byte)((a + dstA * inv) * 255f);
            }
        }
    }


    public virtual void DrawRect(Rect r, Color4 color, float stroke = 1f)
    {
        float s = Math.Max(1f, stroke);

        FillRect(new Rect(r.X, r.Y, r.Width, s), color);
        FillRect(new Rect(r.X, r.Y + r.Height - s, r.Width, s), color);
        FillRect(new Rect(r.X, r.Y, s, r.Height), color);
        FillRect(new Rect(r.X + r.Width - s, r.Y, s, r.Height), color);
    }


    public virtual void FillRoundRect(Rect r, Color4 color, float radius)
    {
        int x0 = Clamp((int)Math.Floor(r.X), 0, _width);
        int y0 = Clamp((int)Math.Floor(r.Y), 0, _height);
        int x1 = Clamp((int)Math.Ceiling(r.X + r.Width), 0, _width);
        int y1 = Clamp((int)Math.Ceiling(r.Y + r.Height), 0, _height);
        if (x1 <= x0 || y1 <= y0) return;

        byte r8 = ToByte(color.R), g8 = ToByte(color.G), b8 = ToByte(color.B);
        float caF = color.A;
        float rad = Math.Max(0f, radius);
        float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f;
        float halfW = r.Width / 2f, halfH = r.Height / 2f;

        for (int y = y0; y < y1; y++)
        {
            int row = y * _width;
            for (int x = x0; x < x1; x++)
            {
                float d = RoundRectDist(x + 0.5f, y + 0.5f, cx, cy, halfW, halfH, rad);
                float cov = Math.Clamp(0.5f - d, 0f, 1f) * caF;
                if (cov <= 0f) continue;
                BlendPixelAt((row + x) * 4, r8, g8, b8, cov);
            }
        }
    }


    public virtual void DrawRoundRect(Rect r, Color4 color, float radius, float stroke = 1f)
    {
        float half = Math.Max(1f, stroke) / 2f;
        int x0 = Clamp((int)Math.Floor(r.X - half), 0, _width);
        int y0 = Clamp((int)Math.Floor(r.Y - half), 0, _height);
        int x1 = Clamp((int)Math.Ceiling(r.X + r.Width + half), 0, _width);
        int y1 = Clamp((int)Math.Ceiling(r.Y + r.Height + half), 0, _height);
        if (x1 <= x0 || y1 <= y0) return;

        byte r8 = ToByte(color.R), g8 = ToByte(color.G), b8 = ToByte(color.B);
        float caF = color.A;
        float rad = Math.Max(0f, radius);
        float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f;
        float halfW = r.Width / 2f, halfH = r.Height / 2f;

        for (int y = y0; y < y1; y++)
        {
            int row = y * _width;
            for (int x = x0; x < x1; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                float d = RoundRectDist(px, py, cx, cy, halfW, halfH, rad);

                float a1 = Math.Clamp(0.5f - (d - half), 0f, 1f);
                float a2 = Math.Clamp(0.5f - (d + half), 0f, 1f);
                float cov = a1 * (1f - a2) * caF;
                if (cov <= 0f) continue;
                BlendPixelAt((row + x) * 4, r8, g8, b8, cov);
            }
        }
    }


    private static bool InsideRoundRect(float px, float py, float cx, float cy, float halfW, float halfH, float radius)
    {
        float dx = Math.Abs(px - cx) - (halfW - radius);
        float dy = Math.Abs(py - cy) - (halfH - radius);
        float ax = Math.Max(dx, 0f), ay = Math.Max(dy, 0f);
        float dist = (float)Math.Sqrt(ax * ax + ay * ay) + Math.Min(Math.Max(dx, dy), 0f) - radius;
        return dist < 0f;
    }


    private static float RoundRectDist(float px, float py, float cx, float cy, float halfW, float halfH, float radius)
    {
        float dx = Math.Abs(px - cx) - (halfW - radius);
        float dy = Math.Abs(py - cy) - (halfH - radius);
        float ax = Math.Max(dx, 0f), ay = Math.Max(dy, 0f);
        return (float)Math.Sqrt(ax * ax + ay * ay) + Math.Min(Math.Max(dx, dy), 0f) - radius;
    }


    private void BlendPixelAt(int p, byte r8, byte g8, byte b8, float cov)
    {
        if (cov <= 0f) return;
        if (cov >= 1f)
        {
            _pixels[p] = b8; _pixels[p + 1] = g8; _pixels[p + 2] = r8; _pixels[p + 3] = 255;
            return;
        }
        float dstA = _pixels[p + 3] / 255f;
        float inv = 1f - cov;
        _pixels[p] = (byte)(b8 * cov + _pixels[p] * inv);
        _pixels[p + 1] = (byte)(g8 * cov + _pixels[p + 1] * inv);
        _pixels[p + 2] = (byte)(r8 * cov + _pixels[p + 2] * inv);
        _pixels[p + 3] = (byte)((cov + dstA * inv) * 255f);
    }


    public virtual void FillRoundRectGradient(Rect r, Color4 top, Color4 bottom, float radius)
    {
        int x0 = Clamp((int)Math.Floor(r.X), 0, _width);
        int y0 = Clamp((int)Math.Floor(r.Y), 0, _height);
        int x1 = Clamp((int)Math.Ceiling(r.X + r.Width), 0, _width);
        int y1 = Clamp((int)Math.Ceiling(r.Y + r.Height), 0, _height);
        if (x1 <= x0 || y1 <= y0) return;

        float rad = Math.Max(0f, radius);
        float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f;
        float halfW = r.Width / 2f, halfH = r.Height / 2f;
        float invH = r.Height > 0f ? 1f / r.Height : 0f;

        for (int y = y0; y < y1; y++)
        {
            float t = Math.Clamp((y + 0.5f - r.Y) * invH, 0f, 1f);
            byte a = ToByte(Lerp(top.A, bottom.A, t));
            byte r8 = ToByte(Lerp(top.R, bottom.R, t));
            byte g8 = ToByte(Lerp(top.G, bottom.G, t));
            byte b8 = ToByte(Lerp(top.B, bottom.B, t));
            int row = y * _width;
            for (int x = x0; x < x1; x++)
            {
                if (!InsideRoundRect(x + 0.5f, y + 0.5f, cx, cy, halfW, halfH, rad)) continue;
                int p = (row + x) * 4;
                _pixels[p] = b8;
                _pixels[p + 1] = g8;
                _pixels[p + 2] = r8;
                _pixels[p + 3] = a;
            }
        }
    }


    public virtual void DrawRoundRectGradient(Rect r, Color4 top, Color4 bottom, float radius, float stroke = 1f)
    {
        float half = Math.Max(1f, stroke) / 2f;
        int x0 = Clamp((int)Math.Floor(r.X - half), 0, _width);
        int y0 = Clamp((int)Math.Floor(r.Y - half), 0, _height);
        int x1 = Clamp((int)Math.Ceiling(r.X + r.Width + half), 0, _width);
        int y1 = Clamp((int)Math.Ceiling(r.Y + r.Height + half), 0, _height);
        if (x1 <= x0 || y1 <= y0) return;

        float rad = Math.Max(0f, radius);
        float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f;
        float halfW = r.Width / 2f, halfH = r.Height / 2f;
        float invH = r.Height > 0f ? 1f / r.Height : 0f;

        for (int y = y0; y < y1; y++)
        {
            float t = Math.Clamp((y + 0.5f - r.Y) * invH, 0f, 1f);
            byte r8 = ToByte(Lerp(top.R, bottom.R, t));
            byte g8 = ToByte(Lerp(top.G, bottom.G, t));
            byte b8 = ToByte(Lerp(top.B, bottom.B, t));
            int row = y * _width;
            for (int x = x0; x < x1; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                float d = RoundRectDist(px, py, cx, cy, halfW, halfH, rad);
                float a1 = Math.Clamp(0.5f - (d - half), 0f, 1f);
                float a2 = Math.Clamp(0.5f - (d + half), 0f, 1f);
                float caF = Lerp(top.A, bottom.A, Math.Clamp((y + 0.5f - r.Y) * invH, 0f, 1f));
                float covS = a1 * (1f - a2) * caF;
                if (covS <= 0f) continue;
                BlendPixelAt((row + x) * 4, r8, g8, b8, covS);
            }
        }
    }


    public virtual void FillGlow(Rect r, float pad, Color4 color, float radius = 0f)
    {
        if (pad <= 0f) return;
        float a0 = color.A;
        if (a0 <= 0f) return;
        int x0 = Clamp((int)Math.Floor(r.X - pad), 0, _width);
        int y0 = Clamp((int)Math.Floor(r.Y - pad), 0, _height);
        int x1 = Clamp((int)Math.Ceiling(r.X + r.Width + pad), 0, _width);
        int y1 = Clamp((int)Math.Ceiling(r.Y + r.Height + pad), 0, _height);
        if (x1 <= x0 || y1 <= y0) return;


        float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f;
        float hw = r.Width / 2f, hh = r.Height / 2f;
        float rad = Math.Clamp(radius, 0f, Math.Min(hw, hh));
        float ix = hw - rad, iy = hh - rad;
        float invPad = 1f / pad;
        byte cr = ToByte(color.R), cg = ToByte(color.G), cb = ToByte(color.B);

        for (int y = y0; y < y1; y++)
        {
            float py = y + 0.5f;
            float qy = Math.Abs(py - cy) - iy;
            float oy = MathF.Max(qy, 0f);
            float row = y * _width;
            for (int x = x0; x < x1; x++)
            {
                float px = x + 0.5f;
                float qx = Math.Abs(px - cx) - ix;
                float ox = MathF.Max(qx, 0f);
                float dist = MathF.Sqrt(ox * ox + oy * oy) + MathF.Min(MathF.Max(qx, qy), 0f) - rad;
                if (dist <= 0f) continue;

                float a = a0 * GlowFalloff(1f - dist * invPad);
                if (a <= 0.002f) continue;
                int p = (int)((row + x) * 4);
                float dstA = _pixels[p + 3] / 255f;
                float inv = 1f - a;
                _pixels[p] = (byte)(cb * a + _pixels[p] * inv);
                _pixels[p + 1] = (byte)(cg * a + _pixels[p + 1] * inv);
                _pixels[p + 2] = (byte)(cr * a + _pixels[p + 2] * inv);
                _pixels[p + 3] = (byte)((a + dstA * inv) * 255f);
            }
        }
    }


    protected static float GlowFalloff(float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;
        return t * t * (3f - 2f * t);
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    private static Color4 LerpColor(Color4 a, Color4 b, float t) => new(
        Lerp(a.R, b.R, t), Lerp(a.G, b.G, t), Lerp(a.B, b.B, t), Lerp(a.A, b.A, t));


    public virtual void DrawRainBar(Rect r, Color4 color, float alpha, float dNear, float dFar, float trackH, float fadePx)
    {
        int x0 = Clamp((int)Math.Floor(r.X), 0, _width);
        int y0 = Clamp((int)Math.Floor(r.Y), 0, _height);
        int x1 = Clamp((int)Math.Ceiling(r.X + r.Width), 0, _width);
        int y1 = Clamp((int)Math.Ceiling(r.Y + r.Height), 0, _height);
        if (x1 <= x0 || y1 <= y0) return;


        float aF = color.A * alpha;
        if (aF <= 0f) return;
        byte a = ToByte(aF);
        byte r8 = ToByte(color.R), g8 = ToByte(color.G), b8 = ToByte(color.B);
        float span = r.Height > 0f ? r.Height : 1f;
        float fadeStart = trackH - fadePx;
        float dSpan = dFar - dNear;

        for (int y = y0; y < y1; y++)
        {


            float t = (y + 0.5f - r.Y) / span;
            float d = dFar - dSpan * t;
            float alphaMul = AlphaAtD(d, fadeStart, trackH, fadePx);
            if (alphaMul <= 0f) continue;
            byte s = (byte)Math.Clamp((int)Math.Round(a * alphaMul), 0, 255);
            if (s == 0) continue;
            int row = y * _width;
            if (s == 255)
            {
                for (int x = x0; x < x1; x++)
                {
                    int p = (row + x) * 4;
                    _pixels[p] = b8; _pixels[p + 1] = g8; _pixels[p + 2] = r8; _pixels[p + 3] = 255;
                }
                continue;
            }

            float f = s / 255f;
            float inv = 1f - f;
            for (int x = x0; x < x1; x++)
            {
                int p = (row + x) * 4;
                float dstA = _pixels[p + 3] / 255f;
                _pixels[p] = (byte)(b8 * f + _pixels[p] * inv);
                _pixels[p + 1] = (byte)(g8 * f + _pixels[p + 1] * inv);
                _pixels[p + 2] = (byte)(r8 * f + _pixels[p + 2] * inv);
                _pixels[p + 3] = (byte)((f + dstA * inv) * 255f);
            }
        }
    }


    private static float AlphaAtD(float d, float fadeStartD, float trackH, float fade)
    {
        if (fade <= 0f || d <= fadeStartD) return 1f;
        if (d >= trackH) return 0f;
        return (trackH - d) / fade;
    }


    public void DrawText(string text, Rect rect, Color4 color, float fontSize, float maxWidth = 0f,
        TextAlign align = TextAlign.Center, int fontStyleFlags = 0, TextStyleFx fx = default,
        float? baselineY = null)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (fontSize <= 0) return;

        string t = TransformText(text, fontStyleFlags);
        float fs = fontSize;
        int dyShift = 0;

        if ((fontStyleFlags & (128 | 256)) != 0) fs *= 0.65f;
        if (maxWidth > 0f)
        {
            var (tw, _th, _) = RenderTextSprite(t, fs, color, fontStyleFlags);
            if (tw > maxWidth && tw > 0f)


                fs = Math.Max(1f, fontSize * (maxWidth / tw));
        }
        var (w, h, px) = RenderTextSprite(t, fs, color, fontStyleFlags, fx);


        var (pL, pT, pR, pB) = SpritePad(fx);
        int w0 = Math.Max(1, w - (pL - 1) - (pR - 1));
        int h0 = Math.Max(1, h - (pT - 1) - (pB - 1));
        int dx0, dy0;
        switch (align)
        {
            case TextAlign.Top:
                dx0 = (int)Math.Round(rect.X + (rect.Width - w0) / 2f);
                dy0 = (int)Math.Round(rect.Y);
                break;
            case TextAlign.Bottom:
                dx0 = (int)Math.Round(rect.X + (rect.Width - w0) / 2f);
                dy0 = (int)Math.Round(rect.Y + rect.Height - h0);
                break;
            case TextAlign.Left:
                dx0 = (int)Math.Round(rect.X);
                dy0 = (int)Math.Round(rect.Y + (rect.Height - h0) / 2f);
                break;
            case TextAlign.Right:
                dx0 = (int)Math.Round(rect.X + rect.Width - w0);
                dy0 = (int)Math.Round(rect.Y + (rect.Height - h0) / 2f);
                break;
            default:
                dx0 = (int)Math.Round(rect.X + (rect.Width - w0) / 2f);
                dy0 = (int)Math.Round(rect.Y + (rect.Height - h0) / 2f);
                break;
        }

        if (baselineY.HasValue)
        {
            var (_, ascent) = GetTextVerticalMetrics(fs, fontStyleFlags);
            dy0 = (int)Math.Round(baselineY.Value - ascent);
        }
        int dx = dx0 - (pL - 1);
        int dy = dy0 - (pT - 1);
        if ((fontStyleFlags & 128) != 0) dyShift = -(int)Math.Round(h0 * 0.45f);
        else if ((fontStyleFlags & 256) != 0) dyShift = (int)Math.Round(h0 * 0.25f);
        BlitSprite(px, w, h, dx, dy + dyShift);
    }


    private readonly Dictionary<(string Font, int Size100, int Style), (float LineHeight, float Ascent)> _textMetrics = new();


    public (float LineHeight, float Ascent) GetTextVerticalMetrics(float fontSize, int fontStyleFlags)
    {
        string fontName = TextFontFamily?.Name ?? "default";
        var key = (fontName, (int)Math.Round(fontSize * 100f), fontStyleFlags);
        if (_textMetrics.TryGetValue(key, out var cached)) return cached;

        float line = Math.Max(1f, fontSize * 1.2f);
        float ascent = fontSize * 0.8f;
        try
        {
            using var font = CreatePixelFont(fontSize, fontStyleFlags);
            line = font.GetHeight();
            FontFamily fam = font.FontFamily;
            FontStyle style = font.Style;
            float lineSpacing = fam.GetLineSpacing(style);
            float cellAscent = fam.GetCellAscent(style);
            if (lineSpacing > 0f)
                ascent = line * (cellAscent / lineSpacing);
        }
        catch
        {

        }
        var result = (line, ascent);
        _textMetrics[key] = result;
        return result;
    }

    private System.Drawing.Font CreatePixelFont(float fontSize, int fontStyleFlags)
    {
        FontFamily? family = TextFontFamily;
        if (family == null)
        {
            FontManager.Load();
            family = FontManager.Get(_configFontName) ?? _defaultFamily;
        }
        var st = ResolveFontStyle(fontStyleFlags);
        return family != null
            ? new System.Drawing.Font(family, fontSize, st, System.Drawing.GraphicsUnit.Pixel)
            : new System.Drawing.Font("Segoe UI", fontSize, st, System.Drawing.GraphicsUnit.Pixel);
    }


    private static string TransformText(string text, int flags)
    {
        if ((flags & 16) != 0) return text.ToUpperInvariant();
        if ((flags & 8) != 0) return text.ToLowerInvariant();
        if ((flags & 32) != 0)
        {
            if (text.Length <= 1) return text.ToUpperInvariant();
            return char.ToUpperInvariant(text[0]) + text.Substring(1).ToLowerInvariant();
        }
        return text;
    }


    private void BlendPixel(int px, int row, Color4 c, float alpha)
    {
        byte sa = ToByte(alpha);
        if (sa == 0) return;

        if (px < 0 || px >= _width) return;
        int y = row / _width;
        if (y < 0 || y >= _height) return;
        int p = row + px * 4;
        if (sa == 255)
        {
            _pixels[p] = ToByte(c.B); _pixels[p + 1] = ToByte(c.G);
            _pixels[p + 2] = ToByte(c.R); _pixels[p + 3] = 255;
            return;
        }
        float a = sa / 255f;
        float dstA = _pixels[p + 3] / 255f;
        float inv = 1f - a;
        _pixels[p] = (byte)(ToByte(c.B) * a + _pixels[p] * inv);
        _pixels[p + 1] = (byte)(ToByte(c.G) * a + _pixels[p + 1] * inv);
        _pixels[p + 2] = (byte)(ToByte(c.R) * a + _pixels[p + 2] * inv);
        _pixels[p + 3] = (byte)((a + dstA * inv) * 255f);
    }


    public virtual void Present()
    {
        if (LayeredMode)
        {

            PremultiplyPixels();
            PresentLayered();
            return;
        }
        if (_swapChain == null || _backBuffer == null || _context == null) return;
        try
        {


            PremultiplyPixels();


            int rowPitch = _width * 4;
            GCHandle pin = GCHandle.Alloc(_presentPixels, GCHandleType.Pinned);
            try
            {
                _context.UpdateSubresource(_backBuffer, 0, null, pin.AddrOfPinnedObject(), (uint)rowPitch, 0);
            }
            finally
            {
                pin.Free();
            }


            _swapChain.Present(1, PresentFlags.None);
        }
        catch (Exception ex)
        {
            GraphicsLog.Write("Present 失败：" + ex.Message);

            Resize(_width, _height);
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    private struct POINT32 { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)]
    private struct SIZE32 { public int CX, CY; }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct BLENDFUNCTION { public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat; }
    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public int biSize; public int biWidth; public int biHeight; public short biPlanes, biBitCount;
        public int biCompression, biSizeImage, biXPelsPerMeter, biYPelsPerMeter, biClrUsed, biClrImportant;
    }
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr h);
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr h, IntPtr dc);
    [System.Runtime.InteropServices.DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [System.Runtime.InteropServices.DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);
    [System.Runtime.InteropServices.DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
    [System.Runtime.InteropServices.DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr obj);
    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    private static extern IntPtr CreateDIBSection(IntPtr dc, ref BITMAPINFOHEADER bmi, uint usage, out IntPtr bits, IntPtr sect, uint offset);
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT32 { public int Left, Top, Right, Bottom; }
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, out RECT32 rect);

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr dstDc, ref POINT32 dst, ref SIZE32 size,
        IntPtr srcDc, ref POINT32 src, int colorKey, ref BLENDFUNCTION blend, uint flags);


    protected void PresentLayered()
    {        if (_hwnd == IntPtr.Zero || _width <= 0 || _height <= 0) return;
        try
        {
            if (_dib == IntPtr.Zero || _dibW != _width || _dibH != _height)
            {
                if (_dib != IntPtr.Zero) { DeleteObject(_dib); _dib = IntPtr.Zero; _dibBits = IntPtr.Zero; }
                if (_memDC != IntPtr.Zero) { DeleteDC(_memDC); _memDC = IntPtr.Zero; }
                var bmi = new BITMAPINFOHEADER
                {
                    biSize = Marshal.SizeOf<BITMAPINFOHEADER>(),
                    biWidth = _width,
                    biHeight = -_height,
                    biPlanes = 1,
                    biBitCount = 32,
                    biCompression = 0,
                };
                _dib = CreateDIBSection(IntPtr.Zero, ref bmi, 0, out _dibBits, IntPtr.Zero, 0);
                _memDC = CreateCompatibleDC(IntPtr.Zero);
                if (_dib == IntPtr.Zero || _memDC == IntPtr.Zero)
                {
                    GraphicsLog.Write("分层呈现：CreateDIBSection 失败");
                    return;
                }
                SelectObject(_memDC, _dib);
                _dibW = _width; _dibH = _height;
            }


            int bytes = _width * _height * 4;
            Marshal.Copy(_presentPixels, 0, _dibBits, bytes);

            IntPtr screenDc = GetDC(IntPtr.Zero);

            RECT32 wr; GetWindowRect(_hwnd, out wr);
            var dst = new POINT32 { X = wr.Left, Y = wr.Top };
            var size = new SIZE32 { CX = _width, CY = _height };
            var src = new POINT32 { X = 0, Y = 0 };
            var blend = new BLENDFUNCTION { BlendOp = 0, BlendFlags = 0, SourceConstantAlpha = 255, AlphaFormat = 1 };
            bool ok = UpdateLayeredWindow(_hwnd, screenDc, ref dst, ref size, _memDC, ref src, 0, ref blend, 0x02 );
            _ = ReleaseDC(IntPtr.Zero, screenDc);
            if (!ok && !_layeredErrLogged)
            {
                _layeredErrLogged = true;
                GraphicsLog.Write($"UpdateLayeredWindow 失败：{Marshal.GetLastWin32Error()}");
            }
        }
        catch (Exception ex) { GraphicsLog.Write("分层呈现异常：" + ex.Message); }
    }
    private bool _layeredErrLogged;


    private void PremultiplyPixels()
    {
        byte[] src = _pixels;
        byte[] dst = _presentPixels;
        int n = _width * _height;
        for (int i = 0; i < n; i++)
        {
            int p = i * 4;
            int a = src[p + 3];
            if (a == 255)
            {
                dst[p] = src[p];
                dst[p + 1] = src[p + 1];
                dst[p + 2] = src[p + 2];
                dst[p + 3] = 255;
            }
            else if (a == 0)
            {
                dst[p] = 0;
                dst[p + 1] = 0;
                dst[p + 2] = 0;
                dst[p + 3] = 0;
            }
            else
            {
                dst[p] = (byte)((src[p] * a + 127) / 255);
                dst[p + 1] = (byte)((src[p + 1] * a + 127) / 255);
                dst[p + 2] = (byte)((src[p + 2] * a + 127) / 255);
                dst[p + 3] = (byte)a;
            }
        }
    }


    private (int W, int H, byte[] Pixels) RenderTextSprite(string text, float fontSize, Color4 color, int fontStyleFlags = 0,
        TextStyleFx fx = default)
    {
        string fam = TextFontFamily?.Name ?? "default";

        string key = $"{fam}|{text}|{fontSize:F1}|{color}|{fontStyleFlags}|{fx.Outline}|{fx.OutlineColor}|{fx.OutlinePx:F1}|{fx.Shadow}|{fx.ShadowColor}|{fx.ShadowX:F1}|{fx.ShadowY:F1}|{fx.ShadowSoftPx:F1}|{fx.TextGradient}|{fx.GradientLeft}|{fx.GradientRight}";
        if (TryGetSprite(key, out var cached)) return cached;

        using var font = CreatePixelFont(fontSize, fontStyleFlags);
        SizeF sz;
        using (var measure = GdiGraphics.FromImage(new System.Drawing.Bitmap(1, 1)))
        {
            measure.TextRenderingHint = TextRenderingHint.AntiAlias;
            sz = measure.MeasureString(text, font);
        }


        if (!fx.Outline && !fx.Shadow)
        {
            int w0 = Math.Max(1, (int)Math.Ceiling(sz.Width) + 1);
            int h0 = Math.Max(1, (int)Math.Ceiling(sz.Height) + 1);
            byte[] px0 = new byte[w0 * h0 * 4];
            using (var bmp = new System.Drawing.Bitmap(w0, h0, PixelFormat.Format32bppArgb))
            {
                using (var g = GdiGraphics.FromImage(bmp))
                {
                    g.Clear(System.Drawing.Color.Transparent);
                    g.TextRenderingHint = TextRenderingHint.AntiAlias;
                    using var brush = new System.Drawing.SolidBrush(ToGdiColor(color));
                    g.DrawString(text, font, brush, 1f, 1f);
                }
                var d = bmp.LockBits(new System.Drawing.Rectangle(0, 0, w0, h0), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                Marshal.Copy(d.Scan0, px0, 0, px0.Length);
                bmp.UnlockBits(d);
            }
            StoreSprite(key, w0, h0, px0);
            return (w0, h0, px0);
        }


        var (padL, padT, padR, padB) = SpritePad(fx);
        int w = Math.Max(1, (int)Math.Ceiling(sz.Width) + padL + padR);
        int h = Math.Max(1, (int)Math.Ceiling(sz.Height) + padT + padB);


        int ss = fontSize >= 48f ? 1 : fontSize >= 28f ? 2 : 3;
        int w2 = w * ss, h2 = h * ss;
        byte[] big = new byte[w2 * h2 * 4];


        byte[] body;
        using (var ssFont = CreatePixelFont(fontSize * ss, fontStyleFlags))
            body = RenderBodyMaskArray(text, ssFont, w2, h2, padL * ss, padT * ss);


        if (fx.Shadow)
        {
            byte[] shadow = body;
            if (fx.ShadowSoftPx > 0.5f)
            {
                shadow = (byte[])body.Clone();
                BoxBlurAlpha(shadow, w2, h2, Math.Max(1, (int)Math.Round(fx.ShadowSoftPx * ss)));
            }
            ComposeLayer(big, shadow, w2, h2,
                (int)Math.Round(fx.ShadowX * ss), (int)Math.Round(fx.ShadowY * ss), fx.ShadowColor, alphaKeep: true);
        }


        if (fx.Outline && fx.OutlinePx > 0.5f)
        {
            byte[] outline = (byte[])body.Clone();
            DilateAlpha(outline, w2, h2, Math.Max(1, (int)Math.Round(fx.OutlinePx * ss)));
            ComposeLayer(big, outline, w2, h2, 0, 0, fx.OutlineColor);
        }


        if (fx.TextGradient)
            ComposeLayerGradient(big, body, w2, h2, padL * ss, Math.Max(padL * ss + 1, w2 - padR * ss),
                fx.GradientLeft, fx.GradientRight);
        else
            ComposeLayer(big, body, w2, h2, 0, 0, color);


        byte[] px = ss == 1 ? big : DownsampleStraight(big, w2, h2, ss);

        StoreSprite(key, w, h, px);
        return (w, h, px);
    }


    private static byte[] DownsampleStraight(byte[] src, int w2, int h2, int ss)
    {
        int w = w2 / ss, h = h2 / ss;
        var dst = new byte[w * h * 4];
        int samples = ss * ss;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int sumA = 0, sumR = 0, sumG = 0, sumB = 0;
                for (int sy = 0; sy < ss; sy++)
                {
                    int row = (y * ss + sy) * w2;
                    for (int sx = 0; sx < ss; sx++)
                    {
                        int p = (row + x * ss + sx) * 4;
                        int a = src[p + 3];
                        if (a == 0) continue;
                        sumA += a;
                        sumB += src[p] * a;
                        sumG += src[p + 1] * a;
                        sumR += src[p + 2] * a;
                    }
                }
                if (sumA == 0) continue;
                int dp = (y * w + x) * 4;
                dst[dp] = (byte)(sumB / sumA);
                dst[dp + 1] = (byte)(sumG / sumA);
                dst[dp + 2] = (byte)(sumR / sumA);
                dst[dp + 3] = (byte)Math.Min(255, sumA / samples);
            }
        }
        return dst;
    }


    private static byte[] RenderBodyMaskArray(string text, System.Drawing.Font font, int w, int h, float x, float y)
    {
        var arr = new byte[w * h * 4];
        using (var mk = new System.Drawing.Bitmap(w, h, PixelFormat.Format32bppArgb))
        {
            using (var gm = GdiGraphics.FromImage(mk))
            {
                gm.Clear(System.Drawing.Color.Transparent);
                gm.TextRenderingHint = TextRenderingHint.AntiAlias;
                using (var white = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                    gm.DrawString(text, font, white, x, y);
            }
            var d = mk.LockBits(new System.Drawing.Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(d.Scan0, arr, 0, arr.Length);
            mk.UnlockBits(d);
        }
        return arr;
    }


    private static void ComposeLayer(byte[] dst, byte[] mask, int w, int h, int ox, int oy, Color4 c, bool alphaKeep = false)
    {
        byte cr = ToByte(c.R), cg = ToByte(c.G), cb = ToByte(c.B), ca = ToByte(c.A);
        float caF = ca / 255f;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int sx = x - ox, sy = y - oy;
                byte a = (sx < 0 || sx >= w || sy < 0 || sy >= h) ? (byte)0 : mask[(sy * w + sx) * 4 + 3];
                if (a == 0) continue;
                float sa = (a / 255f) * caF;
                int di = (y * w + x) * 4;
                float da = dst[di + 3] / 255f;
                float outA = sa + da * (1f - sa);
                if (outA <= 1e-6f) continue;
                float inv = 1f - sa;
                dst[di] = (byte)Math.Round((cb * sa + dst[di] * da * inv) / outA);
                dst[di + 1] = (byte)Math.Round((cg * sa + dst[di + 1] * da * inv) / outA);
                dst[di + 2] = (byte)Math.Round((cr * sa + dst[di + 2] * da * inv) / outA);
                dst[di + 3] = alphaKeep
                    ? (byte)Math.Round(sa * 255f)
                    : (byte)Math.Round(outA * 255f);
            }
    }


    private static void ComposeLayerGradient(byte[] dst, byte[] mask, int w, int h, int x0, int x1,
        Color4 left, Color4 right)
    {
        if (w <= 0 || h <= 0) return;
        float span = Math.Max(1f, x1 - x0);
        var cr = new byte[w];
        var cg = new byte[w];
        var cb = new byte[w];
        var ca = new byte[w];
        for (int x = 0; x < w; x++)
        {
            float t = Math.Clamp((x - x0) / span, 0f, 1f);
            cr[x] = ToByte(Lerp(left.R, right.R, t));
            cg[x] = ToByte(Lerp(left.G, right.G, t));
            cb[x] = ToByte(Lerp(left.B, right.B, t));
            ca[x] = ToByte(Lerp(left.A, right.A, t));
        }
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                byte a = mask[(y * w + x) * 4 + 3];
                if (a == 0) continue;
                float sa = (a / 255f) * (ca[x] / 255f);
                if (sa <= 0f) continue;
                int di = (y * w + x) * 4;
                float da = dst[di + 3] / 255f;
                float outA = sa + da * (1f - sa);
                if (outA <= 1e-6f) continue;
                float inv = 1f - sa;
                dst[di] = (byte)Math.Round((cb[x] * sa + dst[di] * da * inv) / outA);
                dst[di + 1] = (byte)Math.Round((cg[x] * sa + dst[di + 1] * da * inv) / outA);
                dst[di + 2] = (byte)Math.Round((cr[x] * sa + dst[di + 2] * da * inv) / outA);
                dst[di + 3] = (byte)Math.Round(outA * 255f);
            }
    }


    private static (int PadL, int PadT, int PadR, int PadB) SpritePad(TextStyleFx fx)
    {
        float outPx = fx.Outline ? Math.Max(0f, fx.OutlinePx) : 0f;
        float shX = fx.Shadow ? fx.ShadowX : 0f;
        float shY = fx.Shadow ? fx.ShadowY : 0f;
        float soft = fx.Shadow ? Math.Max(0f, fx.ShadowSoftPx) : 0f;
        return ((int)Math.Ceiling(Math.Max(Math.Max(outPx, Math.Max(0f, -shX)), soft)) + 1,
                (int)Math.Ceiling(Math.Max(Math.Max(outPx, Math.Max(0f, -shY)), soft)) + 1,
                (int)Math.Ceiling(Math.Max(Math.Max(outPx, Math.Max(0f, shX)), soft)) + 1,
                (int)Math.Ceiling(Math.Max(Math.Max(outPx, Math.Max(0f, shY)), soft)) + 1);
    }


    private static void DilateAlpha(byte[] px, int w, int h, int radius)
    {
        if (radius <= 0 || w <= 0 || h <= 0) return;
        int r2 = radius * radius;
        float[] src = new float[w * h];
        for (int i = 0; i < w * h; i++) src[i] = px[i * 4 + 3];
        float[] dst = new float[w * h];
        for (int y = 0; y < h; y++)
        {
            int yLo = Math.Max(0, y - radius), yHi = Math.Min(h - 1, y + radius);
            for (int x = 0; x < w; x++)
            {
                float m = 0f;
                for (int sy = yLo; sy <= yHi; sy++)
                {
                    int dy = sy - y;

                    int dxMax = (int)MathF.Sqrt(r2 - dy * dy);
                    int xLo = Math.Max(0, x - dxMax), xHi = Math.Min(w - 1, x + dxMax);
                    int row = sy * w;
                    for (int sx = xLo; sx <= xHi; sx++)
                    {
                        float v = src[row + sx];
                        if (v > m) m = v;
                    }
                }
                dst[y * w + x] = m;
            }
        }
        for (int i = 0; i < w * h; i++)
            px[i * 4 + 3] = (byte)Math.Clamp((int)Math.Round(dst[i]), 0, 255);
    }


    private static void BoxBlurAlpha(byte[] px, int w, int h, int radius)
    {
        if (radius <= 0 || w <= 0 || h <= 0) return;
        float[] a = new float[w * h];
        for (int i = 0; i < w * h; i++) a[i] = px[i * 4 + 3];
        float[] tmp = new float[w * h];

        for (int y = 0; y < h; y++)
        {
            float sum = 0f; int cnt = 0;
            for (int x = 0; x <= radius && x < w; x++) { sum += a[y * w + x]; cnt++; }
            for (int x = 0; x < w; x++)
            {
                tmp[y * w + x] = cnt > 0 ? sum / cnt : 0f;
                int outX = x - radius;
                if (outX >= 0) { sum -= a[y * w + outX]; cnt--; }
                int inX = x + radius + 1;
                if (inX < w) { sum += a[y * w + inX]; cnt++; }
            }
        }

        for (int x = 0; x < w; x++)
        {
            float sum = 0f; int cnt = 0;
            for (int y = 0; y <= radius && y < h; y++) { sum += tmp[y * w + x]; cnt++; }
            for (int y = 0; y < h; y++)
            {
                a[y * w + x] = cnt > 0 ? sum / cnt : 0f;
                int outY = y - radius;
                if (outY >= 0) { sum -= tmp[outY * w + x]; cnt--; }
                int inY = y + radius + 1;
                if (inY < h) { sum += tmp[inY * w + x]; cnt++; }
            }
        }
        for (int i = 0; i < w * h; i++)
            px[i * 4 + 3] = (byte)Math.Clamp((int)Math.Round(a[i]), 0, 255);
    }


    private static System.Drawing.FontStyle ResolveFontStyle(int flags)
    {
        var st = System.Drawing.FontStyle.Regular;
        if ((flags & 1) != 0) st |= System.Drawing.FontStyle.Bold;
        if ((flags & 2) != 0) st |= System.Drawing.FontStyle.Italic;
        if ((flags & 4) != 0) st |= System.Drawing.FontStyle.Underline;
        if ((flags & 64) != 0) st |= System.Drawing.FontStyle.Strikeout;
        return st;
    }


    protected virtual void BlitSprite(byte[] px, int w, int h, int dx, int dy)
        => Blit(px, w, h, dx, dy);

    private void Blit(byte[] src, int sw, int sh, int dx, int dy)
    {
        for (int y = 0; y < sh; y++)
        {
            int sy = dy + y;
            if (sy < 0 || sy >= _height) continue;
            int dstRow = sy * _width;
            int srcRow = y * sw;
            for (int x = 0; x < sw; x++)
            {
                int sx = dx + x;
                if (sx < 0 || sx >= _width) continue;
                int sp = (srcRow + x) * 4;
                int dp = (dstRow + sx) * 4;

                byte sa = src[sp + 3];
                if (sa == 0) continue;
                if (sa == 255)
                {
                    _pixels[dp] = src[sp];
                    _pixels[dp + 1] = src[sp + 1];
                    _pixels[dp + 2] = src[sp + 2];
                    _pixels[dp + 3] = 255;
                    continue;
                }

                float a = sa / 255f;
                float dstA = _pixels[dp + 3] / 255f;
                float inv = 1f - a;
                _pixels[dp] = (byte)(src[sp] * a + _pixels[dp] * inv);
                _pixels[dp + 1] = (byte)(src[sp + 1] * a + _pixels[dp + 1] * inv);
                _pixels[dp + 2] = (byte)(src[sp + 2] * a + _pixels[dp + 2] * inv);


                _pixels[dp + 3] = (byte)((a + dstA * inv) * 255f);
            }
        }
    }

    private void ReleaseRender()
    {
        _backBuffer?.Dispose(); _backBuffer = null;
        _swapChain?.Dispose(); _swapChain = null;
        _context?.Dispose(); _context = null;
        _device?.Dispose(); _device = null;
        ClearTextSprites();
    }


    public virtual void Dispose()
    {
        ReleaseRender();
        Initialized = false;
        GC.SuppressFinalize(this);
    }


    private static byte ToByte(float v) => (byte)Math.Clamp((int)Math.Round(v * 255f), 0, 255);

    private static int Clamp(int v, int lo, int hi) => Math.Max(lo, Math.Min(hi, v));

    private static System.Drawing.Color ToGdiColor(Color4 c) => System.Drawing.Color.FromArgb(ToByte(c.A), ToByte(c.R), ToByte(c.G), ToByte(c.B));
}
