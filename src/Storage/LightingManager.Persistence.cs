using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;

namespace phosphorite
{
    public partial class LightingManager
    {
        public class Vec3Data { public float x, y, z; }
        public class ColorData { public float r, g, b, a; }
        public class LightEntry
        {
            public Vec3Data pos;
            public float intensity;
            public ColorData color;
        }
        public class LightSettings
        {
            public ColorData ambientColor;
            public List<LightEntry> lights;
        }

        public static string PluginDirectory { get { return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); } }
        public static string DataPath { get { return Path.Combine(PluginDirectory, "data.json"); } }

        public static bool SaveLights()
        {
            try
            {
                var settings = new LightSettings();
                var ambientColor = Shader.GetGlobalColor("_GT_GameLight_Ambient_Color");
                settings.ambientColor = new ColorData { r = ambientColor.r, g = ambientColor.g, b = ambientColor.b, a = ambientColor.a };
                settings.lights = new List<LightEntry>();
                foreach (var light in lights)
                {
                    if (light == null || light.light == null) continue;
                    var position = light.transform.position;
                    var color = light.light.color;
                    settings.lights.Add(new LightEntry
                    {
                        pos = new Vec3Data { x = position.x, y = position.y, z = position.z },
                        intensity = GetIntensity(light),
                        color = new ColorData { r = color.r, g = color.g, b = color.b, a = color.a }
                    });
                }
                Directory.CreateDirectory(Path.GetDirectoryName(DataPath));
                if (File.Exists(DataPath) && !File.Exists(DataPath + ".bak")) File.Copy(DataPath, DataPath + ".bak");
                File.WriteAllText(DataPath, JsonConvert.SerializeObject(settings, Formatting.Indented));
                Debug.Log("[phosphorite] Saved " + settings.lights.Count + " lights to " + DataPath);
                return true;
            }
            catch (Exception error)
            {
                Debug.LogError("[phosphorite] Failed to save lights: " + error);
                return false;
            }
        }

        public static bool LoadLights()
        {
            try
            {
                if (!File.Exists(DataPath))
                {
                    Debug.LogWarning("[phosphorite] data.json not found at " + DataPath);
                    return false;
                }
                var settings = JsonConvert.DeserializeObject<LightSettings>(File.ReadAllText(DataPath));
                if (settings == null) return false;
                if (settings.ambientColor != null)
                {
                    var color = settings.ambientColor;
                    foreach (float value in new[] { color.r, color.g, color.b, color.a })
                        if (float.IsNaN(value) || float.IsInfinity(value)) throw new InvalidDataException("Invalid ambient color");
                }
                if (settings.lights != null) foreach (var entry in settings.lights)
                {
                    if (entry == null || entry.pos == null || entry.color == null) continue;
                    foreach (float value in new[] { entry.pos.x, entry.pos.y, entry.pos.z, entry.intensity, entry.color.r, entry.color.g, entry.color.b, entry.color.a })
                        if (float.IsNaN(value) || float.IsInfinity(value)) throw new InvalidDataException("Invalid light data");
                }
                ClearLights();
                if (settings.ambientColor != null)
                    SetAmbient(new Color(settings.ambientColor.r, settings.ambientColor.g, settings.ambientColor.b, settings.ambientColor.a));
                if (settings.lights != null) foreach (var entry in settings.lights)
                {
                    if (entry == null || entry.pos == null || entry.color == null) continue;
                    AddLight(new Vector3(entry.pos.x, entry.pos.y, entry.pos.z), entry.intensity, new Color(entry.color.r, entry.color.g, entry.color.b, entry.color.a));
                }
                Debug.Log("[phosphorite] Loaded " + lights.Count + " lights from " + DataPath);
                return true;
            }
            catch (Exception error)
            {
                Debug.LogError("[phosphorite] Failed to load lights: " + error);
                return false;
            }
        }
    }
}
