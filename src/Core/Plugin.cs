using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace phosphorite
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        static MonoBehaviour host;
        static ManualLogSource logger;

        internal static MonoBehaviour Host { get { return host; } }

        void Awake()
        {
            host = this;
            logger = Logger;
            ModSettings.Configure(Config);
            LightSpawnerSettings.Configure(Config);
            SceneLighting.Configure(Config);
            MapLighting.Initialize();
            PortableLightAssets.Initialize();
            LightRenderHooks.Initialize();
            SceneManager.sceneLoaded += MapLighting.OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            WatchPages.Register();
        }

        void Update()
        {
            if (LightingManager.Runner == null && GameLightingManager.instance != null)
            {
                gameObject.AddComponent<LightingManager>();
                LightingManager.PendingAutoLoad = ModSettings.AutoLoad;
            }
            WatchPages.Update();
            MapLighting.Update();
            SceneLighting.Update();
        }

        void LateUpdate()
        {
            LightSpawner.Update();
            LightEditorTool.Update();
            PortableLights.Update();
        }

        void OnSceneUnloaded(Scene scene) { LightingManager.ReapplyAmbient(); }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= MapLighting.OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            PortableLights.Dispose();
            ToolPointer.Dispose();
            LightEditor.Dispose();
            LightingManager.ClearLights();
            LightRenderer.Dispose();
            if (LightingManager.Runner != null) Destroy(LightingManager.Runner);
            LightingManager.Runner = null;
            host = null;
            logger = null;
        }

        internal static void Log(string message, bool error)
        {
            if (logger != null)
            {
                if (error) logger.LogError(message);
                else logger.LogMessage(message);
            }
            else if (error) Debug.LogError("[phosphorite] " + message);
            else Debug.Log("[phosphorite] " + message);
        }
    }
}
