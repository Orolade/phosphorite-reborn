using BepInEx.Configuration;

namespace phosphorite
{
    public static class ModSettings
    {
        static ConfigEntry<bool> autoSave, autoLoad;

        internal static bool AutoSave { get { return autoSave == null || autoSave.Value; } }
        internal static bool AutoLoad { get { return autoLoad == null || autoLoad.Value; } }

        internal static void Configure(ConfigFile config)
        {
            autoLoad = config.Bind("General", "AutoLoadOnStart", true, "");
            autoSave = config.Bind("Scan", "AutoSave", true, "");
        }
    }
}
