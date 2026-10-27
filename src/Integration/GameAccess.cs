using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace phosphorite
{
    public static class GameAccess
    {
        internal const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        internal static readonly FieldInfo gameLightsField = typeof(GameLightingManager).GetField("gameLights", GameAccess.InstanceFields);
        static readonly FieldInfo zonesField = typeof(ZoneManagement).GetField("zones", GameAccess.InstanceFields);

        internal static ZoneData[] GetZones()
        {
            var manager = ZoneManagement.instance;
            return manager == null || zonesField == null ? null : zonesField.GetValue(manager) as ZoneData[];
        }
        internal static List<GameLight> GetLights(GameLightingManager manager)
        {
            return manager == null || gameLightsField == null ? null : gameLightsField.GetValue(manager) as List<GameLight>;
        }

        internal static Camera GetPlayerCamera()
        {
            var player = GorillaTagger.Instance;
            var camera = player != null && player.mainCamera != null ? player.mainCamera.GetComponent<Camera>() : null;
            return camera != null ? camera : Camera.main;
        }

        internal static bool TryGetPlayerPosition(out Vector3 center)
        {
            var player = GorillaTagger.Instance;
            if (player != null && player.headCollider != null)
            {
                center = player.headCollider.transform.position;
                return true;
            }
            if (player != null && player.mainCamera != null)
            {
                center = player.mainCamera.transform.position;
                return true;
            }
            var camera = Camera.main;
            if (camera != null)
            {
                center = camera.transform.position;
                return true;
            }
            center = default(Vector3);
            return false;
        }

        public static bool IsMapObject(GameObject go)
        {
            if (go == null || go.name == "phosphorite_light" || go.name == "LightingManager" || !go.scene.IsValid() || !go.scene.isLoaded || go.scene.name == "DontDestroyOnLoad" || !go.activeInHierarchy) return false;
            if (go.GetComponentInParent<VRRig>(true) != null || go.GetComponentInParent<GorillaTagger>(true) != null || go.GetComponentInParent<Camera>(true) != null || go.GetComponentInParent<ParticleSystem>(true) != null || go.GetComponentInParent<Rigidbody>(true) != null) return false;
            var renderer = go.GetComponent<Renderer>();
            return !(renderer is ParticleSystemRenderer) && !(renderer is TrailRenderer) && !(renderer is LineRenderer) && !(renderer is SkinnedMeshRenderer);
        }
    }
}
