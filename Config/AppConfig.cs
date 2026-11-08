using Newtonsoft.Json;
using Vortice.Mathematics;

namespace JipperKeyViewer.Config;


public partial class AppConfig
{

    public bool Enabled { get; set; } = true;


    [Newtonsoft.Json.JsonIgnore]
    public bool OverlayMode
    {
        get => GlobalSettings.OverlayMode;
        set => GlobalSettings.OverlayMode = value;
    }


    [Newtonsoft.Json.JsonIgnore]
    public int SettingsHotkey
    {
        get => GlobalSettings.SettingsHotkey;
        set => GlobalSettings.SettingsHotkey = value;
    }
    public float Opacity { get; set; } = 1.0f;


    public int KeyLayout { get; set; } = 16;


    public int WindowX { get; set; } = -1;
    public int WindowY { get; set; } = -1;
    public int WindowW { get; set; } = -1;
    public int WindowH { get; set; } = -1;


    public Dictionary<int, long> KeyCounts { get; set; } = new();
    public long TotalCount { get; set; } = 0;
    public bool ShowKps { get; set; } = true;
    public bool ShowTotal { get; set; } = true;
    public string KpsLabel { get; set; } = "KPS";
    public string TotalLabel { get; set; } = "Total";
    public bool DownLocation { get; set; } = true;
    public float KeyFontSize { get; set; } = 20f;


    public int[] Key8 { get; set; } = Array.Empty<int>();
    public int[] Key10 { get; set; } = Array.Empty<int>();
    public int[] Key12 { get; set; } = Array.Empty<int>();
    public int[] Key14 { get; set; } = Array.Empty<int>();
    public int[] Key16 { get; set; } = Array.Empty<int>();
    public int[] Key20 { get; set; } = Array.Empty<int>();
    public int[] Key24 { get; set; } = Array.Empty<int>();

    public int[] Key4 { get; set; } = Array.Empty<int>();

    public int[] Key5 { get; set; } = Array.Empty<int>();

    public int[] Key6 { get; set; } = Array.Empty<int>();

    public int[] Key7 { get; set; } = Array.Empty<int>();

    public int[] Key9 { get; set; } = Array.Empty<int>();

    private static readonly int[] DefaultKey108 = {
        27, 282, 283, 284, 285, 286, 287, 288, 289, 290, 291, 292, 293, 316, 302, 19, 317,
        96, 49, 50, 51, 52, 53, 54, 55, 56, 57, 48, 45, 61, 8,
        9, 113, 119, 101, 114, 116, 121, 117, 105, 111, 112, 91, 93, 92,
        301, 97, 115, 100, 102, 103, 104, 106, 107, 108, 59, 222, 13,
        304, 122, 120, 99, 118, 98, 110, 109, 44, 46, 47, 303,
        306, 309, 308, 32, 307, 310, 322, 305,
        271, 127, 36, 35, 33, 34,
        38, 37, 40, 39,
        300, 267, 268, 269, 263, 264, 265, 270, 260, 261, 262, 257, 258, 259, 256, 266, 13,
    };
    public int[] Key108 { get; set; } = CreateDefault108();
    public static int[] CreateDefault108() => (int[])DefaultKey108.Clone();


    private static readonly int[] DefaultKey75 = {
        27, 282, 283, 284, 285, 286, 287, 288, 289, 290, 291, 292, 293, 316,
        96, 49, 50, 51, 52, 53, 54, 55, 56, 57, 48, 45, 61, 8, 127,
        9, 113, 119, 101, 114, 116, 121, 117, 105, 111, 112, 91, 93, 92, 36,
        301, 97, 115, 100, 102, 103, 104, 106, 107, 108, 59, 222, 13, 33,
        304, 122, 120, 99, 118, 98, 110, 109, 44, 46, 47, 303, 38, 34,
        306, 309, 308, 32, 307, 322, 305, 37, 40, 39,
    };
    public int[] Key75 { get; set; } = CreateDefault75();
    public static int[] CreateDefault75() => (int[])DefaultKey75.Clone();


    private static readonly int[] DefaultKey60 = {
        96, 49, 50, 51, 52, 53, 54, 55, 56, 57, 48, 45, 61, 8,
        9, 113, 119, 101, 114, 116, 121, 117, 105, 111, 112, 91, 93, 92,
        301, 97, 115, 100, 102, 103, 104, 106, 107, 108, 59, 222, 13,
        304, 122, 120, 99, 118, 98, 110, 109, 44, 46, 47, 303,
        306, 309, 308, 32, 307, 0, 322, 305,
    };
    public int[] Key60 { get; set; } = CreateDefault60();
    public static int[] CreateDefault60() => (int[])DefaultKey60.Clone();


    public int[] GhostKey8 { get; set; } = Array.Empty<int>();
    public int[] GhostKey10 { get; set; } = Array.Empty<int>();
    public int[] GhostKey12 { get; set; } = Array.Empty<int>();
    public int[] GhostKey14 { get; set; } = Array.Empty<int>();
    public int[] GhostKey16 { get; set; } = Array.Empty<int>();
    public int[] GhostKey20 { get; set; } = Array.Empty<int>();
    public int[] GhostKey24 { get; set; } = Array.Empty<int>();


    public string[] Key8Text { get; set; } = Array.Empty<string>();
    public string[] Key10Text { get; set; } = Array.Empty<string>();
    public string[] Key12Text { get; set; } = Array.Empty<string>();
    public string[] Key14Text { get; set; } = Array.Empty<string>();
    public string[] Key4Text { get; set; } = Array.Empty<string>();
    public string[] Key5Text { get; set; } = Array.Empty<string>();
    public string[] Key6Text { get; set; } = Array.Empty<string>();
    public string[] Key7Text { get; set; } = Array.Empty<string>();
    public string[] Key9Text { get; set; } = Array.Empty<string>();
    public string[] Key16Text { get; set; } = Array.Empty<string>();
    public string[] Key20Text { get; set; } = Array.Empty<string>();
    public string[] Key24Text { get; set; } = Array.Empty<string>();


    public float[] Background { get; set; } = { 0.5607843f, 0.2352941f, 1.0f, 0.5f };
    public float[] BackgroundClicked { get; set; } = { 1f, 1f, 1f, 1f };
    public float[] Outline { get; set; } = { 0.5529412f, 0.2431373f, 1.0f, 1f };
    public float[] OutlineClicked { get; set; } = { 1f, 1f, 1f, 1f };
    public float[] Text { get; set; } = { 1f, 1f, 1f, 1f };
    public float[] TextClicked { get; set; } = { 0f, 0f, 0f, 1f };


    public string FontFamily { get; set; } = "Segoe UI";


    [Newtonsoft.Json.JsonIgnore]
    public string Language
    {
        get => GlobalSettings.Language;
        set => GlobalSettings.Language = value;
    }


    [Newtonsoft.Json.JsonIgnore]
    public int SafeArea
    {
        get => GlobalSettings.SafeArea;
        set => GlobalSettings.SafeArea = value;
    }
    public bool ThousandSeparator { get; set; } = false;
    public string FontName { get; set; } = "";
    public int FontStyleFlags { get; set; } = 0;
    public bool FontStyleBold { get; set; } = false;
    public bool FontStyleItalic { get; set; } = false;
    public bool FontStyleUnderline { get; set; } = false;
    public bool FontStyleLower { get; set; } = false;
    public bool FontStyleUpper { get; set; } = false;
    public bool FontStyleSmallCaps { get; set; } = false;
    public bool FontStyleStrike { get; set; } = false;
    public bool FontStyleSuper { get; set; } = false;
    public bool FontStyleSub { get; set; } = false;


    public bool StandardKeyWidth { get; set; } = false;
    public bool PerKeyFontSizeEnabled { get; set; } = false;


    public float[] PerKeyFontSize { get; set; } = Array.Empty<float>();


    public float[][] PerKeyBackground { get; set; } = Array.Empty<float[]>();
    public float[][] PerKeyBackgroundClicked { get; set; } = Array.Empty<float[]>();
    public float[][] PerKeyOutline { get; set; } = Array.Empty<float[]>();
    public float[][] PerKeyOutlineClicked { get; set; } = Array.Empty<float[]>();
    public float[][] PerKeyText { get; set; } = Array.Empty<float[]>();
    public float[][] PerKeyTextClicked { get; set; } = Array.Empty<float[]>();
    public float[][] PerKeyRainColor { get; set; } = Array.Empty<float[]>();


    public bool HideMainKeyCount { get; set; } = false;
    public bool PerKeyKps { get; set; } = false;
    public bool StreamerMode { get; set; } = false;
    public bool HideKpsTotalLabel { get; set; } = false;

    public bool GlowEnabled { get; set; } = false;

    public bool GlowFollowColor { get; set; } = true;

    public float GlowSize { get; set; } = 20f;

    public int GlowOpacity { get; set; } = 70;

    public float[] GlowColor { get; set; } = { 1f, 1f, 1f, 1f };
    public bool BgGradientEnabled { get; set; } = false;
    public float[] BgGradientTop { get; set; } = { 1f, 1f, 1f, 1f };
    public float[] BgGradientBottom { get; set; } = { 0f, 0f, 0f, 1f };
    public float[] BgGradientClickedTop { get; set; } = { 1f, 1f, 1f, 1f };
    public float[] BgGradientClickedBottom { get; set; } = { 0f, 0f, 0f, 1f };
    public bool TextGradientEnabled { get; set; } = false;
    public float[] TextGradientLeft { get; set; } = { 1f, 1f, 1f, 1f };
    public float[] TextGradientRight { get; set; } = { 1f, 1f, 1f, 1f };
    public float[] TextGradientClickedLeft { get; set; } = { 1f, 1f, 1f, 1f };
    public float[] TextGradientClickedRight { get; set; } = { 1f, 1f, 1f, 1f };
    public bool OutlineGradientEnabled { get; set; } = false;
    public float[] OutlineGradientTop { get; set; } = { 1f, 1f, 1f, 1f };
    public float[] OutlineGradientBottom { get; set; } = { 0f, 0f, 0f, 1f };
    public float[] OutlineGradientClickedTop { get; set; } = { 1f, 1f, 1f, 1f };
    public float[] OutlineGradientClickedBottom { get; set; } = { 0f, 0f, 0f, 1f };
    public bool TextOutline { get; set; } = false;
    public float[] TextOutlineColor { get; set; } = { 0f, 0f, 0f, 1f };
    public float TextOutlineWidth { get; set; } = 0.2f;
    public bool TextShadow { get; set; } = true;
    public float[] TextShadowColor { get; set; } = { 0f, 0f, 0f, 0.5f };
    public float ShadowOffsetX { get; set; } = 1f;
    public float ShadowOffsetY { get; set; } = -1f;
    public float ShadowSoftness { get; set; } = 0f;


    public string Easing { get; set; } = "linear";

    public bool EnablePressAnimation { get; set; } = true;

    public float PressAnimationScale { get; set; } = 0.95f;

    public float PressAnimationDuration { get; set; } = 80f;

    public bool EnablePressAnimationOnRain { get; set; } = false;


    public bool RainEnabled { get; set; } = true;
    public bool EnableRainForRow1 { get; set; } = true;
    public bool EnableRainForRow2 { get; set; } = true;
    public bool EnableRainForRow3 { get; set; } = true;
    public float RainSpeedRow1 { get; set; } = 100f;
    public float RainSpeedRow2 { get; set; } = 100f;
    public float RainSpeedRow3 { get; set; } = 100f;
    public float RainHeightRow1 { get; set; } = 275f;
    public float RainHeightRow2 { get; set; } = 275f;
    public float RainHeightRow3 { get; set; } = 275f;
    public float RainWidthRow1 { get; set; } = 50f;
    public float RainWidthRow2 { get; set; } = 40f;
    public float RainWidthRow3 { get; set; } = 30f;
    public float RainStartYRow1 { get; set; } = -223f;
    public float RainStartYRow2 { get; set; } = -169f;
    public float RainStartYRow3 { get; set; } = -115f;
    public bool EnableRainFade { get; set; } = true;
    public float RainFadeDuration { get; set; } = 0.5f;
    public bool EnableRainGradient { get; set; } = true;
    public float RainFadePx { get; set; } = 40f;
    public float[] Rain1Color { get; set; } = { 0.5137255f, 0.1254902f, 0.8588235f, 1f };
    public float[] Rain2Color { get; set; } = { 1f, 1f, 1f, 1f };
    public float[] Rain3Color { get; set; } = { 1f, 0f, 1f, 1f };


    public bool EnableRainShadowRow1 { get; set; } = false;
    public bool EnableRainShadowRow2 { get; set; } = false;
    public bool EnableRainShadowRow3 { get; set; } = false;
    public float[] RainShadowColorRow1 { get; set; } = { 0f, 0f, 0f, 0.35f };
    public float[] RainShadowColorRow2 { get; set; } = { 0f, 0f, 0f, 0.35f };
    public float[] RainShadowColorRow3 { get; set; } = { 0f, 0f, 0f, 0.35f };
    public float RainShadowOffsetXRow1 { get; set; } = 3f;
    public float RainShadowOffsetYRow1 { get; set; } = -3f;
    public float RainShadowOffsetXRow2 { get; set; } = 3f;
    public float RainShadowOffsetYRow2 { get; set; } = -3f;
    public float RainShadowOffsetXRow3 { get; set; } = 3f;
    public float RainShadowOffsetYRow3 { get; set; } = -3f;


    public bool EnableRainOutlineRow1 { get; set; } = false;
    public bool EnableRainOutlineRow2 { get; set; } = false;
    public bool EnableRainOutlineRow3 { get; set; } = false;
    public float[] RainOutlineColorRow1 { get; set; } = { 1f, 1f, 1f, 0.5f };
    public float[] RainOutlineColorRow2 { get; set; } = { 1f, 1f, 1f, 0.5f };
    public float[] RainOutlineColorRow3 { get; set; } = { 1f, 1f, 1f, 0.5f };
    public float RainOutlineWidthRow1 { get; set; } = 2f;
    public float RainOutlineWidthRow2 { get; set; } = 2f;
    public float RainOutlineWidthRow3 { get; set; } = 2f;


    public bool EnableGhostRain { get; set; } = false;
    public float[] GhostRainColor1 { get; set; } = { 1f, 1f, 1f, 0.6f };
    public float[] GhostRainColor2 { get; set; } = { 1f, 1f, 1f, 0.6f };
    public float[] GhostRainColor3 { get; set; } = { 1f, 1f, 1f, 0.6f };
    public float GhostRainStartYRow1 { get; set; } = -223f;
    public float GhostRainStartYRow2 { get; set; } = -169f;
    public float GhostRainStartYRow3 { get; set; } = -115f;


    public bool EnableGhostRainShadowRow1 { get; set; } = false;
    public bool EnableGhostRainShadowRow2 { get; set; } = false;
    public bool EnableGhostRainShadowRow3 { get; set; } = false;
    public float[] GhostRainShadowColorRow1 { get; set; } = { 0f, 0f, 0f, 0.35f };
    public float[] GhostRainShadowColorRow2 { get; set; } = { 0f, 0f, 0f, 0.35f };
    public float[] GhostRainShadowColorRow3 { get; set; } = { 0f, 0f, 0f, 0.35f };
    public float GhostRainShadowOffsetXRow1 { get; set; } = 3f;
    public float GhostRainShadowOffsetYRow1 { get; set; } = -3f;
    public float GhostRainShadowOffsetXRow2 { get; set; } = 3f;
    public float GhostRainShadowOffsetYRow2 { get; set; } = -3f;
    public float GhostRainShadowOffsetXRow3 { get; set; } = 3f;
    public float GhostRainShadowOffsetYRow3 { get; set; } = -3f;


    public bool EnableGhostRainOutlineRow1 { get; set; } = false;
    public bool EnableGhostRainOutlineRow2 { get; set; } = false;
    public bool EnableGhostRainOutlineRow3 { get; set; } = false;
    public float[] GhostRainOutlineColorRow1 { get; set; } = { 1f, 1f, 1f, 0.5f };
    public float[] GhostRainOutlineColorRow2 { get; set; } = { 1f, 1f, 1f, 0.5f };
    public float[] GhostRainOutlineColorRow3 { get; set; } = { 1f, 1f, 1f, 0.5f };
    public float GhostRainOutlineWidthRow1 { get; set; } = 2f;
    public float GhostRainOutlineWidthRow2 { get; set; } = 2f;
    public float GhostRainOutlineWidthRow3 { get; set; } = 2f;


    public bool PerKeyColor { get; set; } = false;
    public float[] KpsBackground { get; set; } = { 0.5607843f, 0.2352941f, 1.0f, 0.1960784f };
    public float[] KpsOutline { get; set; } = { 0.5529412f, 0.2431373f, 1.0f, 1f };
    public float[] KpsText { get; set; } = { 1f, 1f, 1f, 1f };
    public float[] TotalBackground { get; set; } = { 0.5607843f, 0.2352941f, 1.0f, 0.1960784f };
    public float[] TotalOutline { get; set; } = { 0.5529412f, 0.2431373f, 1.0f, 1f };
    public float[] TotalText { get; set; } = { 1f, 1f, 1f, 1f };


    public bool KpsTotalFollowGlobal { get; set; } = true;


    [Newtonsoft.Json.JsonIgnore] public float[] EffKpsBackground => KpsTotalFollowGlobal ? Background : KpsBackground;
    [Newtonsoft.Json.JsonIgnore] public float[] EffKpsOutline => KpsTotalFollowGlobal ? Outline : KpsOutline;
    [Newtonsoft.Json.JsonIgnore] public float[] EffKpsText => KpsTotalFollowGlobal ? Text : KpsText;
    [Newtonsoft.Json.JsonIgnore] public float[] EffTotalBackground => KpsTotalFollowGlobal ? Background : TotalBackground;
    [Newtonsoft.Json.JsonIgnore] public float[] EffTotalOutline => KpsTotalFollowGlobal ? Outline : TotalOutline;
    [Newtonsoft.Json.JsonIgnore] public float[] EffTotalText => KpsTotalFollowGlobal ? Text : TotalText;
    public bool P108UnifiedColor { get; set; } = true;
    public float[] P108Background { get; set; } = { 0.5607843f, 0.2352941f, 1.0f, 0.5f };
    public float[] P108BackgroundClicked { get; set; } = { 1f, 1f, 1f, 1f };
    public float[] P108Outline { get; set; } = { 0.5529412f, 0.2431373f, 1.0f, 1f };
    public float[] P108OutlineClicked { get; set; } = { 1f, 1f, 1f, 1f };
    public float[] P108Text { get; set; } = { 1f, 1f, 1f, 1f };
    public float[] P108TextClicked { get; set; } = { 0f, 0f, 0f, 1f };

    public static string ConfigDir
    {
        get
        {
            string root = Path.GetDirectoryName(Environment.ProcessPath)
                          ?? Environment.CurrentDirectory;

            string dir = Path.Combine(root, "JipperKeyViewer", "config");
            try { Directory.CreateDirectory(dir); }
            catch (Exception ex)
            {

                throw new IOException(
                    $"无法创建配置目录 {dir}（安装目录可能只读）。请将程序安装到可写目录（如 D:\\ 或用户目录）。\n详情：{ex.Message}");
            }
            return dir;
        }
    }

    public static string FontsDir
    {
        get
        {
            string root = Path.GetDirectoryName(Environment.ProcessPath)
                          ?? Environment.CurrentDirectory;
            string dir = Path.Combine(root, "JipperKeyViewer", "fonts");
            try { Directory.CreateDirectory(dir); }
            catch (Exception ex)
            {
                throw new IOException($"无法创建字体目录 {dir}。请将程序安装到可写目录。\n详情：{ex.Message}");
            }
            return dir;
        }
    }

    private static string ConfigPath => Path.Combine(ConfigDir, "config.json");


    internal static string LegacyConfigPath => CurrentConfigPath;

    private static string CurrentConfigPath
        => ProfileManager.GetCurrentProfile() == ProfileManager.DefaultProfile
            ? ConfigPath
            : ProfileManager.ProfileFilePath(ProfileManager.GetCurrentProfile());

    public static AppConfig Load()
    {
        try
        {
            string path = CurrentConfigPath;
            if (File.Exists(path))
            {
                string raw = File.ReadAllText(path);
                ConfigMigration.MigrateLegacyGlobal(raw);
                var cfg = JsonConvert.DeserializeObject<AppConfig>(raw);
                if (cfg != null)
                {
                    ConfigMigration.NormalizeLoaded(cfg);
                    return cfg;
                }
            }
        }
        catch (Exception ex)
        {

            try { File.Copy(CurrentConfigPath, CurrentConfigPath + ".bak", true); } catch { }
            try { File.WriteAllText(CurrentConfigPath + ".broken", ex.ToString()); } catch { }
        }
        var fresh = new AppConfig();
        fresh.FillDefaultKeys();
        fresh.Save();
        return fresh;
    }

    public void Save()
    {
        string path = CurrentConfigPath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
    }


    public void EnsureAllKeyArrays()
    {

        bool missing = false;
        foreach (var def in Layouts.All)
        {
            var keys = def.GetKeys(this);
            if (keys != null && keys.Length > 0) continue;

            var defKeys = def.DefaultKeys();
            if (defKeys.Length > 0) def.SetKeys(this, defKeys);
            else missing = true;
        }
        if (missing)
        {

            var backup = Key8;
            FillDefaultKeys();
            if (backup != null && backup.Length > 0) Key8 = backup;
        }


        if (Key75 != null && Key75.Length == 82 && Key75[74] == (int)Core.KeyCode.LeftControl)
            Key75[74] = (int)Core.KeyCode.LeftAlt;
    }


    public void FillDefaultKeys()
    {

        foreach (var def in Layouts.All) def.SetKeys(this, def.DefaultKeys());
    }


    public void ResetToDefaults()
    {
        var fresh = new AppConfig();
        fresh.FillDefaultKeys();
        foreach (var prop in typeof(AppConfig).GetProperties(
                     System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            if (prop.CanWrite)
                prop.SetValue(this, prop.GetValue(fresh));
        }
    }


    public int[] CurrentGhostKeys => KeyLayout switch
    {
        108 or 75 or 60 => Array.Empty<int>(),
        10 => GhostKey10,
        12 => GhostKey12,
        14 => GhostKey14,
        16 => GhostKey16,
        20 => GhostKey20,
        24 => GhostKey24,
        _ => GhostKey8,
    };


    public int[] EnsureCurrentGhostKeys(int count)
    {
        if (Layouts.Get(KeyLayout).IsFullKeyboard) return Array.Empty<int>();
        var arr = CurrentGhostKeys;
        if (arr.Length >= count) return arr;
        var ext = new int[count];
        Array.Copy(arr, ext, arr.Length);
        switch (KeyLayout)
        {
            case 10: GhostKey10 = ext; break;
            case 12: GhostKey12 = ext; break;
            case 14: GhostKey14 = ext; break;
            case 16: GhostKey16 = ext; break;
            case 20: GhostKey20 = ext; break;
            case 24: GhostKey24 = ext; break;
            default: GhostKey8 = ext; break;
        }
        return ext;
    }


    public void SetCurrentGhostKey(int index, int keyCode)
    {
        var arr = EnsureCurrentGhostKeys(CurrentKeys.Length);
        if (index < 0 || index >= arr.Length) return;
        arr[index] = keyCode;
    }


    public int[] CurrentKeys => Layouts.Get(KeyLayout).GetKeys(this);


    public string[] CurrentKeyTexts => Layouts.Get(KeyLayout).GetTexts(this);


    public string[] EnsureCurrentKeyTexts(int count)
    {
        var def = Layouts.Get(KeyLayout);
        var arr = def.GetTexts(this);
        if (def.IsFullKeyboard) return arr;
        if (arr.Length >= count) return arr;
        var ext = new string[count];
        Array.Copy(arr, ext, arr.Length);
        def.SetTexts(this, ext);
        return ext;
    }

    public Color4 C(Color4 def) => new(Background[0], Background[1], Background[2], Background[3]);
    public Color4 ToColor(float[] v) => v.Length >= 4
        ? new Color4(v[0], v[1], v[2], v[3])
        : v.Length == 3 ? new Color4(v[0], v[1], v[2], 1f) : new Color4(1, 1, 1, 1);


    public static bool Matches(float[] a, params float[] b)
        => a != null && b != null && a.Length == b.Length && a.Zip(b).All(p => p.First == p.Second);
}
