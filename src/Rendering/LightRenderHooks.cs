using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace phosphorite
{
    public static class LightRenderHooks
    {
        static Harmony harmony;
        static bool hooks;
        public static void PullPrefix(GameLightingManager __instance, ref int __0)
        {
            __0 = NativeLighting.NativeRefreshCount(__instance.maxUseTestLights);
        }

        public static bool SortPrefix(GameLightingManager __instance)
        {
            return !hooks || LightRenderer.IsRecovering || !LightRenderer.TrySync(__instance);
        }

        public static bool RefreshPrefix(GameLightingManager __instance)
        {
            return !hooks || LightRenderer.IsRecovering || !LightRenderer.TrySync(__instance);
        }

        public static void DynamicPrefix(GameLightingManager __instance, ref bool __0)
        {
            if (hooks && Plugin.Host != null && __instance == GameLightingManager.instance) __0 = true;
        }

        internal static void BeforeCamera(Camera camera)
        {
            if (hooks && GameLightingManager.instance != null) LightRenderer.RenderCamera(camera);
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
            try
            {
                NativeLighting.CheckLayout();
                LightBuffers.Initialize();
                harmony = new Harmony("com.brokenstone.gorillatag.phosphorite.lighting");
                PatchPrefix(typeof(GameLightingManager).GetMethod("SortLights"), nameof(SortPrefix));
                PatchPrefix(typeof(GameLightingManager).GetMethod("RefreshLightData", GameAccess.InstanceFields), nameof(RefreshPrefix));
                PatchPrefix(typeof(GameLightingManager).GetMethod("PullLightData"), nameof(PullPrefix));
                PatchPrefix(typeof(GameLightingManager).GetMethod("SetCustomDynamicLightingEnabled"), nameof(DynamicPrefix));
                hooks = true;
                Camera.onPreCull += BeforeCamera;
                RenderPipelineManager.beginCameraRendering += BeforeSrpCamera;
            }
            catch (Exception e)
            {
                hooks = false;
                Unpatch();
                Plugin.Log("Lighting init: " + e, true);
            }
        }

        internal static void Unpatch()
        {
            if (harmony != null) try
            {
                harmony.UnpatchSelf();
            }
            catch
            {
            }
            harmony = null;
        }

        internal static void Dispose()
        {
            hooks = false;
            Camera.onPreCull -= BeforeCamera;
            RenderPipelineManager.beginCameraRendering -= BeforeSrpCamera;
            Unpatch();
        }
    }
}
