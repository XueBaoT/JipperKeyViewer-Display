using System.IO.Compression;
using JipperKeyViewer.Config;
using Newtonsoft.Json;

namespace JipperKeyViewer.Config.Import;


// .jkv share-package adapter; file/zip work only.
public static class PackageImporter
{


    public static (bool Ok, string? ProfileName, string? Error, bool Duplicate) TryImport(string jkvPath)
    {
        try
        {
            using var zip = ZipFile.OpenRead(jkvPath);

            long total = 0;
            long count = 0;
            foreach (var z in zip.Entries)
            {
                count++;
                if (count > 4096) return (false, null, "包内条目过多（超过 4096）。", false);
                total += z.Length;
                if (total > 512L * 1024L * 1024L) return (false, null, "包过大（展开超过 512MB 上限）。", false);
                if (z.Length > 512L * 1024L * 1024L) return (false, null, "包内单个文件过大。", false);
            }
            var cfgEntry = zip.GetEntry("config.json")
                ?? throw new InvalidOperationException("包内缺少 config.json。");
            string json;
            using (var sr = new StreamReader(cfgEntry.Open())) json = sr.ReadToEnd();
            AppConfig? loaded;
            try
            {
                loaded = JsonConvert.DeserializeObject<AppConfig>(json);
            }
            catch
            {
                return (false, null, "config.json 无法解析。", false);
            }
            if (loaded == null) return (false, null, "config.json 内容为空或无效。", false);
            ConfigMigration.NormalizeLoaded(loaded);


            string rawName = (ReadNameEntry(zip) ?? "").Trim();
            if (rawName.Length == 0) rawName = Path.GetFileNameWithoutExtension(jkvPath).Trim();

            if (IsReservedName(rawName)
                || ProfileManager.GetProfileNames()
                    .Any(e => e.Equals(rawName, StringComparison.OrdinalIgnoreCase)))
                return (false, null, null, true);

            if (!IsUsableName(rawName))
            {
                rawName = "Custom";
                if (ProfileManager.GetProfileNames()
                    .Any(e => e.Equals(rawName, StringComparison.OrdinalIgnoreCase)))
                    return (false, null, null, true);
            }
            string profileName = rawName;

            ProfileManager.SaveProfile(loaded, profileName);


            bool importedFonts = false;
            foreach (var z in zip.Entries)
            {
                if (!z.FullName.StartsWith("fonts/", StringComparison.OrdinalIgnoreCase) || z.FullName.Length <= 6) continue;
                string fn = Path.GetFileName(z.FullName);
                if (string.IsNullOrEmpty(fn)) continue;
                using (var src = z.Open())
                using (var dst = File.Create(Path.Combine(AppConfig.FontsDir, fn)))
                    src.CopyTo(dst);
                importedFonts = true;
            }
            if (importedFonts) Graphics.FontManager.Reload();

            return (true, profileName, null, false);
        }
        catch (InvalidDataException) { return (false, null, "文件不是有效的分享包（ZIP 已损坏）。", false); }
        catch (Exception ex) { return (false, null, ex.Message, false); }
    }


    private static bool IsReservedName(string name)
        => name.Equals(ProfileManager.DefaultProfile, StringComparison.OrdinalIgnoreCase)
        || name.Equals("meta", StringComparison.OrdinalIgnoreCase);


    private static bool IsUsableName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        return name.All(c => !Path.GetInvalidFileNameChars().Contains(c));
    }


    private static string? ReadNameEntry(ZipArchive zip)
    {
        try
        {
            var e = zip.GetEntry("name.txt");
            if (e == null) return null;
            using var sr = new StreamReader(e.Open());
            string s = sr.ReadToEnd().Trim();
            return s.Length == 0 ? null : s;
        }
        catch { return null; }
    }
}
