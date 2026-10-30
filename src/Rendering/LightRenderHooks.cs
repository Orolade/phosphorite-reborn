using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace phosphorite
{
    public static class LightRenderHooks
    {
        static Harmony harmony;
        public static void PullPrefix(GameLightingManager __instance, ref int __0)
        {
            __0 = NativeLighting.NativeRefreshCount(__instance.maxUseTestLights);
        }

        public static bool SyncPrefix(GameLightingManager __instance)
        {
            return !LightRenderer.Sync(__instance);
        }

        public static void DynamicPrefix(GameLightingManager __instance, ref bool __0)
        {
            if (Plugin.Host != null && __instance == GameLightingManager.instance) __0 = true;
        }

        internal static void BeforeCamera(Camera camera)
        {
            if (GameLightingManager.instance != null) LightRenderer.RenderCamera(camera);
        }

        internal static void BeforeSrpCamera(ScriptableRenderContext context, Camera camera)
        {
            BeforeCamera(camera);
        }

        static void PatchPrefix(MethodBase original, string prefix)
        {
            harmony.Patch(original, prefix: new HarmonyMethod(typeof(LightRenderHooks), prefix));
        }

        internal static void Initialize()
        {
            LightBuffers.Initialize();
            harmony = new Harmony("com.brokenstone.gorillatag.phosphorite.lighting");
            try
            {
                PatchPrefix(typeof(GameLightingManager).GetMethod("SortLights"), nameof(SyncPrefix));
                PatchPrefix(typeof(GameLightingManager).GetMethod("RefreshLightData", GameAccess.InstanceFields), nameof(SyncPrefix));
                PatchPrefix(typeof(GameLightingManager).GetMethod("PullLightData"), nameof(PullPrefix));
                PatchPrefix(typeof(GameLightingManager).GetMethod("SetCustomDynamicLightingEnabled"), nameof(DynamicPrefix));
                Camera.onPreCull += BeforeCamera;
                RenderPipelineManager.beginCameraRendering += BeforeSrpCamera;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal static void Unpatch()
        {
            if (harmony != null) harmony.UnpatchSelf();
            harmony = null;
        }

        internal static void Dispose()
        {
            Camera.onPreCull -= BeforeCamera;
            RenderPipelineManager.beginCameraRendering -= BeforeSrpCamera;
            Unpatch();
        }
    }
}
