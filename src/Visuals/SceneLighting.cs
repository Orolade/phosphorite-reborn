using System;
using BepInEx.Configuration;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace phosphorite
{
    public static class SceneLighting
    {
        static bool bakedDisabled;
        static ConfigEntry<bool> bakedConfig;
        static float nextBakedRefresh;
        static readonly Dictionary<Renderer, BakedState> bakedStates = new Dictionary<Renderer, BakedState>();
        static readonly Dictionary<int, Texture2DArray> neutralArrays = new Dictionary<int, Texture2DArray>();
        static readonly List<Renderer> deadRenderers = new List<Renderer>();
        class SavedBlock
        {
            public int slot;
            public Material material;
            public MaterialPropertyBlock original;
            public bool empty;
        }

        class BakedState
        {
            public Renderer renderer;
            public int index, realtime;
            public LightProbeUsage probes;
            public readonly List<SavedBlock> blocks = new List<SavedBlock>();
        }

        public static Color AmbientColor
        {
            get
            {
                return LightingManager.ambient;
            }
        }

        public static bool BakedEnabled
        {
            get
            {
                return !bakedDisabled;
            }
        }

        internal static void Configure(ConfigFile config)
        {
            try
            {
                bakedConfig = config.Bind("Visuals", "DisableBakedLighting", false, "");
                bakedDisabled = bakedConfig.Value;
            }
            catch (Exception e)
            {
                Plugin.Log("Lighting config: " + e.Message, true);
            }
        }

        static Texture2DArray GetNeutralLightmap(Texture original)
        {
            var array = original as Texture2DArray;
            int depth = array == null ? 1 : Math.Max(1, array.depth);
            Texture2DArray neutral;
            if (neutralArrays.TryGetValue(depth, out neutral) && neutral != null) return neutral;
            neutral = new Texture2DArray(1, 1, depth, TextureFormat.RGBA32, false, true);
            neutral.name = "phosphorite_neutral_baked_lightmap";
            neutral.wrapMode = TextureWrapMode.Clamp;
            for (int i = 0; i < depth; i++) neutral.SetPixels(new[]
            {
                Color.white
            }, i);
            neutral.Apply(false, true);
            neutralArrays[depth] = neutral;
            return neutral;
        }

        static void Apply()
        {
            if (!bakedDisabled) return;
            try
            {
                var renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var renderer in renderers)
                {
                    if (renderer == null || !GameAccess.IsMapObject(renderer.gameObject)) continue;
                    BakedState state;
                    if (!bakedStates.TryGetValue(renderer, out state))
                    {
                        state = new BakedState
                        {
                            renderer = renderer,
                            index = renderer.lightmapIndex,
                            realtime = renderer.realtimeLightmapIndex,
                            probes = renderer.lightProbeUsage
                        };
                        bakedStates.Add(renderer, state);
                        var materials = renderer.sharedMaterials;
                        for (int slot = 0; slot < materials.Length; slot++)
                        {
                            var material = materials[slot];
                            if (material == null) continue;
                            bool dayNight = material.HasProperty("_DayNightLightmapArray"), plain = material.HasProperty("_Lightmap");
                            if (!dayNight && !plain) continue;
                            var original = new MaterialPropertyBlock();
                            renderer.GetPropertyBlock(original, slot);
                            var block = new MaterialPropertyBlock();
                            renderer.GetPropertyBlock(block, slot);
                            if (block.isEmpty) renderer.GetPropertyBlock(block);
                            if (dayNight)
                            {
                                var source = material.GetTexture("_DayNightLightmapArray");
                                if (source == null) source = Shader.GetGlobalTexture("_DayNightLightmapArray");
                                block.SetTexture("_DayNightLightmapArray", GetNeutralLightmap(source));
                            }
                            if (plain) block.SetTexture("_Lightmap", Texture2D.whiteTexture);
                            state.blocks.Add(new SavedBlock
                            {
                                slot = slot,
                                material = material,
                                original = original,
                                empty = original.isEmpty
                            });
                            renderer.SetPropertyBlock(block, slot);
                        }
                    }
                    renderer.lightmapIndex = -1;
                    renderer.realtimeLightmapIndex = -1;
                    renderer.lightProbeUsage = LightProbeUsage.Off;
                }
                deadRenderers.Clear();
                foreach (var pair in bakedStates) if (pair.Key == null) deadRenderers.Add(pair.Key);
                foreach (var dead in deadRenderers) bakedStates.Remove(dead);
            }
            catch (Exception e)
            {
                Plugin.Log("Lightmaps: " + e, true);
            }
            nextBakedRefresh = Time.unscaledTime + 1f;
        }

        internal static void Update()
        {
            if (bakedDisabled && Time.unscaledTime >= nextBakedRefresh) Apply();
        }

        internal static void Restore()
        {
            foreach (var state in bakedStates.Values)
            {
                var renderer = state.renderer;
                if (renderer == null) continue;
                try
                {
                    renderer.lightmapIndex = state.index;
                    renderer.realtimeLightmapIndex = state.realtime;
                    renderer.lightProbeUsage = state.probes;
                    var materials = renderer.sharedMaterials;
                    foreach (var block in state.blocks) if (block.slot < materials.Length && materials[block.slot] == block.material) renderer.SetPropertyBlock(block.empty ? null : block.original, block.slot);
                }
                catch (Exception e)
                {
                    Plugin.Log("Lightmap restore: " + e.Message, true);
                }
            }
            bakedStates.Clear();
            foreach (var neutral in neutralArrays.Values) if (neutral != null) UnityEngine.Object.Destroy(neutral);
            neutralArrays.Clear();
        }

        public static void SetAmbient(Color color)
        {
            color = new Color(LightValues.ToByte(color.r) / 255f, LightValues.ToByte(color.g) / 255f, LightValues.ToByte(color.b) / 255f, 1);
            if (LightValues.SameAmbient(LightingManager.ambient, color)) return;
            LightingManager.SetAmbient(color);
            if (!ReferenceEquals(Plugin.Host, null))
            {
                if (ModSettings.AutoSave) LightingManager.SaveLights();
            }
        }

        public static void ChangeAmbient(int channel, int direction)
        {
            var color = AmbientColor;
            if (channel == 0) color.r = LightValues.StepChannel(LightValues.ToLevel(color.r), direction) / 9f;
            else if (channel == 1) color.g = LightValues.StepChannel(LightValues.ToLevel(color.g), direction) / 9f;
            else color.b = LightValues.StepChannel(LightValues.ToLevel(color.b), direction) / 9f;
            SetAmbient(color);
        }

        public static void SetBakedEnabled(bool enabled)
        {
            bool disabled = !enabled;
            if (bakedDisabled == disabled) return;
            bakedDisabled = disabled;
            if (bakedConfig != null) bakedConfig.Value = disabled;
            if (!ReferenceEquals(Plugin.Host, null))
            {
                if (disabled) Apply();
                else Restore();
            }
        }
    }
}
