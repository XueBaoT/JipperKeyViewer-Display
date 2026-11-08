using System.Drawing;
using System.Drawing.Text;
using System.Reflection;
using System.Runtime.InteropServices;

namespace JipperKeyViewer.Graphics;


public static class FontManager
{
    private static PrivateFontCollection _pfc = new();
    private static readonly Dictionary<string, FontFamily> _byDisplay = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<(string Display, string Family)> _list = new();
    private static bool _loaded;
    private static GCHandle _embeddedFont;

    public const string MapleStoryDisplay = "MapleStory";
    public const string CjkDisplay = "CJK (Default)";


    public static void Load()
    {
        if (_loaded) return;
        _loaded = true;
        try { LoadEmbeddedMapleStory(); } catch (Exception ex) { GraphicsLog.Write("内嵌 MapleStory 加载失败：" + ex.Message); }
        try { ScanCustomFonts(); } catch (Exception ex) { GraphicsLog.Write("fonts/ 目录扫描失败：" + ex.Message); }
        Version++;
    }


    public static void Reload()
    {
        try
        {
            if (_embeddedFont.IsAllocated) _embeddedFont.Free();
            _pfc.Dispose();
            _pfc = new PrivateFontCollection();
            _byDisplay.Clear();
            _list.Clear();
            _loaded = false;
            Load();
        }
        catch (Exception ex) { GraphicsLog.Write("字体重载失败：" + ex.Message); }
    }


    public static int Version { get; private set; }


    public static IReadOnlyList<(string Display, string Family)> List => _list;

    public static string DefaultFamily
    {
        get
        {
            if (_list.Count > 0 && !string.IsNullOrEmpty(_list[0].Family)) return _list[0].Family;
            return "Segoe UI";
        }
    }


    public static FontFamily? Get(string displayName)
    {
        Load();
        if (string.IsNullOrEmpty(displayName)) return null;
        if (_byDisplay.TryGetValue(displayName, out var fam)) return fam;

        try
        {
            if (System.Drawing.FontFamily.Families.Any(f => f.Name.Equals(displayName, StringComparison.OrdinalIgnoreCase)))
                return new FontFamily(displayName);
        }
        catch { }
        return null;
    }

    private static void LoadEmbeddedMapleStory()
    {
        var asm = Assembly.GetExecutingAssembly();
        string? res = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("MAPLESTORY_OTF_BOLD.OTF", StringComparison.OrdinalIgnoreCase));
        if (res == null) { GraphicsLog.Write("内嵌字体资源未找到"); return; }
        using var stream = asm.GetManifestResourceStream(res);
        if (stream == null) return;
        byte[] data = new byte[stream.Length];
        stream.ReadExactly(data, 0, data.Length);


        _embeddedFont = GCHandle.Alloc(data, GCHandleType.Pinned);
        _pfc.AddMemoryFont(_embeddedFont.AddrOfPinnedObject(), data.Length);

        if (_pfc.Families.Length > 0)
            Register(_pfc.Families[0], MapleStoryDisplay, "MAPLESTORY_OTF_BOLD.OTF");
    }

    private static void ScanCustomFonts()
    {
        string dir = Config.AppConfig.FontsDir;
        var files = Directory.GetFiles(dir, "*.otf")
            .Concat(Directory.GetFiles(dir, "*.ttf"))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            try
            {
                string name = Path.GetFileNameWithoutExtension(file);
                string display = name.Equals("cjkFonts-regular-normalized", StringComparison.OrdinalIgnoreCase)
                    ? CjkDisplay : name;


                int before = _pfc.Families.Length;
                _pfc.AddFontFile(file);
                for (int i = before; i < _pfc.Families.Length; i++)
                    Register(_pfc.Families[i], display, Path.GetFileName(file));
            }
            catch (Exception ex)
            {
                GraphicsLog.Write($"字体加载失败 {file}：{ex.Message}");
            }
        }
    }

    private static void Register(FontFamily fam, string display, string source)
    {
        if (fam == null || string.IsNullOrEmpty(display)) return;
        if (_byDisplay.ContainsKey(display)) return;
        try
        {
            _byDisplay[display] = fam;
            _list.Add((display, fam.Name));
        }
        catch (Exception ex)
        {
            GraphicsLog.Write($"字体族登记失败 {source}：{ex.Message}");
        }
    }
}
