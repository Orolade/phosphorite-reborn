using System.Collections.Generic;
using UnityEngine;

namespace phosphorite
{
    public partial class LightingManager : MonoBehaviour
    {
        public static readonly List<GameLight> lights = new List<GameLight>();
        public static Color ambient = Color.white;
        public static LightingManager Runner;
        public static bool PendingAutoLoad;
        static bool ambientApplied;

        void Awake() { Runner = this; }

        void Update()
        {
            var manager = GameLightingManager.instance;
            if (manager == null)
            {
                ambientApplied = false;
                return;
            }
            if (!ambientApplied)
            {
                manager.SetAmbientLightDynamic(ambient);
                ambientApplied = true;
            }
            if (!manager.IsDynamicLightingEnabled) manager.SetCustomDynamicLightingEnabled(true);
            manager.maxUseTestLights = NativeLighting.NativeLightCapacity;
            if (PendingAutoLoad)
            {
                PendingAutoLoad = false;
                LoadLights();
            }
            lights.RemoveAll(light => light == null);
        }

        public static void SetAmbient(Color color)
        {
            ambient = color;
            var manager = GameLightingManager.instance;
            if (manager != null) manager.SetAmbientLightDynamic(color);
        }

        public static void ReapplyAmbient() { SetAmbient(ambient); }

        public static GameLight AddLight(Vector3 position, float intensity, Color color)
        {
            var lightObject = new GameObject("phosphorite_light");
            lightObject.SetActive(false);
            lightObject.transform.position = position;
            DontDestroyOnLoad(lightObject);
            var unityLight = lightObject.AddComponent<Light>();
            unityLight.type = LightType.Point;
            unityLight.intensity = Mathf.Abs(intensity);
            unityLight.color = color;
            var gameLight = lightObject.AddComponent<GameLight>();
            gameLight.light = unityLight;
            gameLight.negativeLight = intensity < 0;
            lightObject.SetActive(true);
            lights.Add(gameLight);
            LightRenderer.OnLightAdded(gameLight);
            return gameLight;
        }

        public static void RemoveLight(GameLight light)
        {
            LightRenderer.OnLightRemoved(light);
            if (light == null) return;
            lights.Remove(light);
            Destroy(light.gameObject);
        }

        public static void ClearLights()
        {
            LightRenderer.OnLightsCleared();
            foreach (var light in lights.ToArray()) RemoveLight(light);
            lights.Clear();
        }

        public static float GetIntensity(GameLight light)
        {
            return light.negativeLight ? -light.light.intensity : light.light.intensity;
        }

        public static void SetIntensity(GameLight light, float intensity)
        {
            light.negativeLight = intensity < 0;
            light.light.intensity = Mathf.Abs(intensity);
            light.UpdateCachedLightColorAndIntensity();
            LightRenderer.OnLightChanged();
        }

        public static void SetColor(GameLight light, Color color)
        {
            light.light.color = color;
            light.UpdateCachedLightColorAndIntensity();
            LightRenderer.OnLightChanged();
        }
    }
}
