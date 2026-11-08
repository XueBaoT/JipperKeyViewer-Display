using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace JipperKeyViewer.Config;


public static class GlobalSettings
{
    private const string DefaultLanguage = "中文";

    private static string _language = DefaultLanguage;
    private static int _safeArea = 2;
    private static int _settingsHotkey = 0x77;
    private static bool _hotkeyFromFile;
    private static bool _overlayMode;
    private static bool _overlayFromFile;


    public static string FilePath => Path.Combine(AppConfig.ConfigDir, "global.json");


    public static string Language
    {
        get => _language;
        set
        {
            string v = Normalize(value);
            if (v == _language) return;
            _language = v;
            Save();
        }
    }


    public static int SafeArea
    {
        get => _safeArea;
        set
        {
            int v = Math.Clamp(value, 0, 64);
            if (v == _safeArea) return;
            _safeArea = v;
            Save();
        }
    }


    public static int SettingsHotkey
    {
        get => _settingsHotkey;
        set
        {
            int v = Math.Clamp(value, 0, 0xFF);
            _hotkeyFromFile = true;
            if (v == _settingsHotkey) return;
            _settingsHotkey = v;
            Save();
        }
    }


    public static bool OverlayMode
    {
        get => _overlayMode;
        set
        {
            _overlayFromFile = true;
            if (value == _overlayMode) return;
            _overlayMode = value;
            Save();
        }
    }


    public static void MigrateOverlayMode(bool on)
    {
        if (_overlayFromFile) return;
        _overlayFromFile = true;
        if (!on) return;
        _overlayMode = true;
        Graphics.GraphicsLog.Write("已把旧的方案内覆盖层开关迁移到 global.json：true");
        Save();
    }


    public static void MigrateHotkey(int vk)
    {
        if (_hotkeyFromFile) return;
        if (vk <= 0 || vk > 0xFF) return;
        _settingsHotkey = vk;
        _hotkeyFromFile = true;
        Graphics.GraphicsLog.Write($"已把旧的方案内热键迁移到 global.json：VK=0x{vk:X2}");
        Save();
    }


    public static string Normalize(string? lang)
    {
        if (string.IsNullOrWhiteSpace(lang)) return DefaultLanguage;
        string v = lang.Trim();
        if (v.Equals("中文", StringComparison.OrdinalIgnoreCase) || v.Equals("zh", StringComparison.OrdinalIgnoreCase)
            || v.Equals("chinese", StringComparison.OrdinalIgnoreCase)) return "中文";
        if (v.Equals("English", StringComparison.OrdinalIgnoreCase) || v.Equals("en", StringComparison.OrdinalIgnoreCase))
            return "English";
        if (v == "한국어" || v.Equals("ko", StringComparison.OrdinalIgnoreCase)
            || v.Equals("korean", StringComparison.OrdinalIgnoreCase)) return "한국어";
        return DefaultLanguage;
    }


    public static void Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var jo = JObject.Parse(File.ReadAllText(FilePath));
                _language = Normalize((string?)jo[nameof(Language)]);
                if (jo[nameof(SafeArea)] != null) _safeArea = Math.Clamp((int)jo[nameof(SafeArea)]!, 0, 64);
                if (jo[nameof(OverlayMode)] != null)
                {
                    _overlayMode = (bool)jo[nameof(OverlayMode)]!;
                    _overlayFromFile = true;
                }
                if (jo[nameof(SettingsHotkey)] != null)
                {
                    _settingsHotkey = Math.Clamp((int)jo[nameof(SettingsHotkey)]!, 0, 0xFF);
                    _hotkeyFromFile = true;
                }
                return;
            }
            MigrateLegacyLanguage();
        }
        catch (Exception ex)
        {
            Graphics.GraphicsLog.Write("读取 global.json 失败：" + ex.Message);
        }
    }


    private static void MigrateLegacyLanguage()
    {
        try
        {
            string legacyPath = AppConfig.LegacyConfigPath;
            if (File.Exists(legacyPath))
            {
                var jo = JObject.Parse(File.ReadAllText(legacyPath));
                if (jo[nameof(Language)] != null)
                {
                    _language = Normalize((string?)jo[nameof(Language)]);
                    Graphics.GraphicsLog.Write($"已把旧的方案内语言迁移到 global.json：{_language}");
                }
            }
        }
        catch (Exception ex)
        {
            Graphics.GraphicsLog.Write("迁移旧语言设置失败：" + ex.Message);
        }
        Save();
    }

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(AppConfig.ConfigDir);
            var jo = new JObject
            {
                [nameof(Language)] = _language,
                [nameof(SafeArea)] = _safeArea,
                [nameof(SettingsHotkey)] = _settingsHotkey,
                [nameof(OverlayMode)] = _overlayMode,
            };
            File.WriteAllText(FilePath, jo.ToString(Newtonsoft.Json.Formatting.Indented));
        }
        catch (Exception ex)
        {
            Graphics.GraphicsLog.Write("写入 global.json 失败：" + ex.Message);
        }
    }
}
