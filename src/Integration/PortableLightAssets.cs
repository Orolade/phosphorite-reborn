using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace phosphorite
{
    public static class PortableLightAssets
    {
        static PortableLightTemplate flashlight, lantern;

        internal static void Initialize()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            try { FindFlashlight(); FindLantern(); }
            catch (Exception e) { Plugin.Log("Portable model: " + e.Message, true); }
        }

        static void FindFlashlight()
        {
            if (flashlight != null) return;
            foreach (var source in Resources.FindObjectsOfTypeAll<HatcheryEventFlashlight>())
            {
                if (PortableLightHooks.Owns(source) || !PortableLightTemplate.CanCopy(source)) continue;
                var candidate = PortableLightTemplate.FromFlashlight(source);
                if (candidate == null) continue;
                flashlight = candidate;
                return;
            }
        }

        static void FindLantern()
        {
            if (lantern != null) return;
            foreach (var source in Resources.FindObjectsOfTypeAll<GRToolLantern>())
            {
                if (PortableLightHooks.Owns(source) || !PortableLightTemplate.CanCopy(source)) continue;
                lantern = PortableLightTemplate.FromLantern(source);
                return;
            }
        }

        internal static PortableLightInstance Create(bool isFlashlight)
        {
            if (isFlashlight) FindFlashlight(); else FindLantern();
            var template = isFlashlight ? flashlight : lantern;
            return template == null ? null : template.CreateInstance();
        }

        internal static void Dispose()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (flashlight != null) flashlight.Dispose();
            if (lantern != null) lantern.Dispose();
            flashlight = null;
            lantern = null;
        }
    }
}
