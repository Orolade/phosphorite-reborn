namespace phosphorite
{
    public static class LightingFiles
    {
        static string lightingStatus = "";
        public static string FileStatus
        {
            get
            {
                return lightingStatus;
            }
        }

        public static void RunFileAction(int action)
        {
            lightingStatus = "";
            if (ReferenceEquals(Plugin.Host, null) || LightingManager.Runner == null || GameLightingManager.instance == null)
            {
                lightingStatus = "Not ready";
                return;
            }
            if (action == 0) lightingStatus = LightingManager.SaveLights() ? "Saved" : "Save failed";
            else if (action == 1)
            {
                LightEditor.ClearSelection();
                lightingStatus = LightingManager.LoadLights() ? "Loaded" : "Load failed";
            }
            else if (action == 2)
            {
                LightEditor.ClearSelection();
                MapLighting.ClearCurrentMap();
                lightingStatus = "Cleared";
                if (ModSettings.AutoSave && !LightingManager.SaveLights()) lightingStatus = "Save failed";
            }
        }
    }
}
