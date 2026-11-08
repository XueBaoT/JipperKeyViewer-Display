using Newtonsoft.Json;

namespace JipperKeyViewer.Config;


public static class ProfileManager
{
    public const string DefaultProfile = "Default";

    public static string ProfilesDir => Path.Combine(AppConfig.ConfigDir, "profiles");

    public static string MetaPath => Path.Combine(ProfilesDir, "meta.json");

    private static string RootConfigPath => Path.Combine(AppConfig.ConfigDir, "config.json");

    private sealed class Meta
    {
        public string Current { get; set; } = DefaultProfile;
    }


    public static string ProfileFilePath(string name)
        => name == DefaultProfile
            ? RootConfigPath
            : Path.Combine(ProfilesDir, SanitizeFileName(name) + ".json");


    public static string GetCurrentProfile()
    {
        try
        {
            if (File.Exists(MetaPath))
            {
                var m = JsonConvert.DeserializeObject<Meta>(File.ReadAllText(MetaPath));
                if (m != null && !string.IsNullOrEmpty(m.Current)
                    && (m.Current == DefaultProfile || File.Exists(ProfileFilePath(m.Current))))
                    return m.Current;
            }
        }
        catch { }
        return DefaultProfile;
    }


    public static void SetCurrentProfile(string name)
    {
        Directory.CreateDirectory(ProfilesDir);
        File.WriteAllText(MetaPath, JsonConvert.SerializeObject(new Meta { Current = name }, Formatting.Indented));
    }


    public static List<string> GetProfileNames()
    {
        var names = new List<string> { DefaultProfile };
        try
        {
            if (Directory.Exists(ProfilesDir))
            {
                foreach (var f in Directory.GetFiles(ProfilesDir, "*.json"))
                {
                    string n = Path.GetFileNameWithoutExtension(f);
                    if (n.Equals("meta", StringComparison.OrdinalIgnoreCase)) continue;
                    names.Add(n);
                }
            }
        }
        catch { }
        return names;
    }


    public static bool IsValidName(string name, out string error)
    {
        name = (name ?? "").Trim();
        if (name.Length == 0) { error = "方案名不能为空。"; return false; }
        if (name.Equals(DefaultProfile, StringComparison.OrdinalIgnoreCase)) { error = "“Default”为内置方案名，请换一个名称。"; return false; }
        if (name.Equals("meta", StringComparison.OrdinalIgnoreCase)) { error = "“meta”为保留名称，请换一个名称。"; return false; }
        foreach (char c in Path.GetInvalidFileNameChars())
            if (name.Contains(c)) { error = "方案名包含非法字符（< > : \" / \\ | ? *）。"; return false; }
        if (GetProfileNames().Any(n => n.Equals(name, StringComparison.OrdinalIgnoreCase)))
        { error = $"方案“{name}”已存在，请换一个名称。"; return false; }
        error = "";
        return true;
    }


    public static void SaveProfile(AppConfig cfg, string name)
    {
        Directory.CreateDirectory(ProfilesDir);
        File.WriteAllText(ProfileFilePath(name), JsonConvert.SerializeObject(cfg, Formatting.Indented));
    }


    public static void LoadProfileInto(AppConfig target, string name)
    {
        string path = ProfileFilePath(name);
        if (!File.Exists(path)) throw new InvalidOperationException($"方案文件不存在：{path}");
        var loaded = JsonConvert.DeserializeObject<AppConfig>(File.ReadAllText(path));
        if (loaded == null) throw new InvalidOperationException($"方案文件无法解析：{path}");
        ConfigMigration.NormalizeLoaded(loaded);
        foreach (var prop in typeof(AppConfig).GetProperties(
                     System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            if (prop.CanWrite)
                prop.SetValue(target, prop.GetValue(loaded));
    }


    public static void RenameProfile(string oldName, string newName)
    {
        if (oldName == DefaultProfile) throw new InvalidOperationException("内置 Default 方案不允许重命名。");
        string oldPath = ProfileFilePath(oldName);
        if (!File.Exists(oldPath)) throw new InvalidOperationException($"方案文件不存在：{oldPath}");
        File.Move(oldPath, ProfileFilePath(newName));
        if (GetCurrentProfile() == oldName) SetCurrentProfile(newName);
    }


    public static void DeleteProfile(string name)
    {
        if (name == DefaultProfile) throw new InvalidOperationException("内置 Default 方案不允许删除。");
        string path = ProfileFilePath(name);
        if (File.Exists(path)) File.Delete(path);
        if (GetCurrentProfile() == name) SetCurrentProfile(DefaultProfile);
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }
}
