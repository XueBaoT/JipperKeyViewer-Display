using Newtonsoft.Json.Linq;

namespace JipperKeyViewer.Config;


// Legacy config migration and normalization; AppConfig.Load delegates here.
internal static class ConfigMigration
{

    public static void MigrateLegacyGlobal(string raw)
    {
        try
        {
            var legacy = JObject.Parse(raw);
            if (legacy[nameof(AppConfig.SettingsHotkey)] != null)
                GlobalSettings.MigrateHotkey((int)legacy[nameof(AppConfig.SettingsHotkey)]!);
            if (legacy[nameof(AppConfig.OverlayMode)] != null)
                GlobalSettings.MigrateOverlayMode((bool)legacy[nameof(AppConfig.OverlayMode)]!);
        }
        catch { }
    }


    public static void NormalizeLoaded(AppConfig cfg)
    {
        if (cfg.Key8 == null || cfg.Key8.Length == 0) cfg.FillDefaultKeys();
        else cfg.EnsureAllKeyArrays();
    }
}
