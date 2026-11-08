using Newtonsoft.Json.Linq;
using JipperKeyViewer.Config;
using JipperKeyViewer.Core;

namespace JipperKeyViewer.Config.Import;


// Legacy Mod format adapter; keep compatibility fields out of AppConfig.
public static class ProfileImport
{

    public static bool Import(AppConfig cfg, string path, out string message)
    {
        message = "";
        JObject root;
        try
        {
            root = JObject.Parse(File.ReadAllText(path));
        }
        catch (Exception ex)
        {
            message = $"文件解析失败（不是有效的 JSON）：{ex.Message}";
            return false;
        }


        JObject data = root["Data"] is JObject d ? d : root;


        if (GetLayout(data, "KeyViewerStyle", out int layout))
            cfg.KeyLayout = layout;


        ReadKeyArray(data, "key8", a => cfg.Key8 = a);
        ReadKeyArray(data, "key10", a => cfg.Key10 = a);
        ReadKeyArray(data, "key12", a => cfg.Key12 = a);
        ReadKeyArray(data, "key14", a => cfg.Key14 = a);
        ReadKeyArray(data, "key16", a => cfg.Key16 = a);
        ReadKeyArray(data, "key20", a => cfg.Key20 = a);
        ReadKeyArray(data, "key24", a => cfg.Key24 = a);


        ReadKeyArray(data, "key108", a =>
        {
            if (a.Length == Layouts.Get(108).DefaultKeys().Length) cfg.Key108 = a;
        });
        ReadKeyArray(data, "key60", a => cfg.Key60 = a);

        ReadKeyArray(data, "GhostKey8", a => cfg.GhostKey8 = a);
        ReadKeyArray(data, "GhostKey10", a => cfg.GhostKey10 = a);
        ReadKeyArray(data, "GhostKey12", a => cfg.GhostKey12 = a);
        ReadKeyArray(data, "GhostKey14", a => cfg.GhostKey14 = a);
        ReadKeyArray(data, "GhostKey16", a => cfg.GhostKey16 = a);
        ReadKeyArray(data, "GhostKey20", a => cfg.GhostKey20 = a);
        ReadKeyArray(data, "GhostKey24", a => cfg.GhostKey24 = a);


        ReadTextArray(data, "key8Text", a => cfg.Key8Text = a);
        ReadTextArray(data, "key10Text", a => cfg.Key10Text = a);
        ReadTextArray(data, "key12Text", a => cfg.Key12Text = a);
        ReadTextArray(data, "key14Text", a => cfg.Key14Text = a);
        ReadTextArray(data, "key16Text", a => cfg.Key16Text = a);
        ReadTextArray(data, "key20Text", a => cfg.Key20Text = a);
        ReadTextArray(data, "key24Text", a => cfg.Key24Text = a);


        cfg.Enabled = BoolOf(data, "Enabled") ?? cfg.Enabled;
        cfg.DownLocation = BoolOf(data, "DownLocation") ?? cfg.DownLocation;
        cfg.KpsLabel = StrOf(data, "KpsLabel") ?? cfg.KpsLabel;
        cfg.TotalLabel = StrOf(data, "TotalLabel") ?? cfg.TotalLabel;


        cfg.PerKeyFontSizeEnabled = BoolOf(data, "EnablePerKeyTextSize") ?? cfg.PerKeyFontSizeEnabled;
        ReadFloatArray(data, "PerKeyFontSize", a =>
        {
            if (a.Length >= 42)
            {


                int n = cfg.CurrentKeys.Length;
                var sized = new float[n + 2];
                Array.Copy(a, sized, Math.Min(n, 40));
                if (n < sized.Length && a.Length > 40) sized[n] = a[40];
                if (n + 1 < sized.Length && a.Length > 41) sized[n + 1] = a[41];
                cfg.PerKeyFontSize = sized;
            }
            else cfg.PerKeyFontSize = a;
        });


        cfg.Background = ColorOf(data, "Background") ?? cfg.Background;
        cfg.BackgroundClicked = ColorOf(data, "BackgroundClicked") ?? cfg.BackgroundClicked;
        cfg.Outline = ColorOf(data, "Outline") ?? cfg.Outline;
        cfg.OutlineClicked = ColorOf(data, "OutlineClicked") ?? cfg.OutlineClicked;
        cfg.Text = ColorOf(data, "Text") ?? cfg.Text;
        cfg.TextClicked = ColorOf(data, "TextClicked") ?? cfg.TextClicked;
        cfg.KpsBackground = ColorOf(data, "KpsBackground") ?? cfg.KpsBackground;
        cfg.KpsOutline = ColorOf(data, "KpsOutline") ?? cfg.KpsOutline;
        cfg.KpsText = ColorOf(data, "KpsText") ?? cfg.KpsText;
        cfg.TotalBackground = ColorOf(data, "TotalBackground") ?? cfg.TotalBackground;
        cfg.TotalOutline = ColorOf(data, "TotalOutline") ?? cfg.TotalOutline;
        cfg.TotalText = ColorOf(data, "TotalText") ?? cfg.TotalText;


        ConfigMigration.NormalizeLoaded(cfg);
        message = "导入成功";
        return true;
    }


    private static bool GetLayout(JObject data, string name, out int value)
    {
        value = 0;
        if (data[name] is not JToken t) return false;


        if (t.Type is JTokenType.Integer or JTokenType.Float)
        {
            value = (int)t switch
            {
                0 => 12,
                1 => 16,
                2 => 20,
                3 => 10,
                4 => 8,
                5 => 14,
                6 => 24,
                7 => 108,
                _ => 0,
            };
            return value != 0;
        }


        string s = (t.Type == JTokenType.String ? (string)t! : t.ToString()).Trim();
        value = s switch
        {
            "0" => 12,
            "Key4" or "4" => 4,
            "Key5" or "5" => 5,
            "Key6" or "6" => 6,
            "Key7" or "7" => 7,
            "Key8" or "8" => 8,
            "Key9" or "9" => 9,
            "Key10" or "10" => 10,
            "Key12" or "12" => 12,
            "Key14" or "14" => 14,
            "Key16" or "16" => 16,
            "Key20" or "20" => 20,
            "Key24" or "24" => 24,
            "Key60" or "60" => 60,
            "Key75" or "75" => 75,
            "Key108" or "Full108" or "108" => 108,
            _ => 0,
        };
        return value != 0;
    }

    private static void ReadKeyArray(JObject data, string name, Action<int[]> set)
    {
        if (data[name] is JArray arr)
            set(arr.Select(KeyCodeValue).ToArray());
    }

    private static int KeyCodeValue(JToken t)
    {
        if (t.Type == JTokenType.Integer)
            return (int)t;

        string? s = (string?)t;
        if (s != null && Enum.TryParse(s, out KeyCode kc))
            return (int)kc;
        return 0;
    }

    private static void ReadTextArray(JObject data, string name, Action<string[]> set)
    {
        if (data[name] is JArray arr)
        {
            var list = arr.Select(x => (string?)x ?? "").ToArray();
            if (list.Any(x => x.Length > 0))
                set(list);
        }
    }

    private static bool? BoolOf(JObject data, string name) =>
        data[name] is JToken t && t.Type == JTokenType.Boolean ? (bool?)t : null;

    private static float? FloatOf(JObject data, string name)
    {
        if (data[name] is JToken t && (t.Type == JTokenType.Float || t.Type == JTokenType.Integer))
            return (float)t;
        return null;
    }

    private static string? StrOf(JObject data, string name) =>
        data[name] is JToken t && t.Type == JTokenType.String ? (string?)t : null;

    private static float[]? ColorOf(JObject data, string name)
    {
        if (data[name] is not JObject o) return null;
        if (o["r"] is not JToken r) return null;
        float R = (float)r;
        float G = o["g"] is JToken g ? (float)g : 0f;
        float B = o["b"] is JToken b ? (float)b : 0f;
        float A = o["a"] is JToken a ? (float)a : 1f;
        return new[] { R, G, B, A };
    }

    private static void ReadFloatArray(JObject data, string name, Action<float[]> set)
    {
        if (data[name] is JArray arr && arr.Count > 0)
            set(arr.Select(x => x.Type == JTokenType.Integer ? (float)(int)x : (float)x).ToArray());
    }
}
