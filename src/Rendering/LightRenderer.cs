using System;
using System.Collections.Generic;
using UnityEngine;

namespace phosphorite
{
    public static class LightRenderer
    {
        static bool dirty = true;
        static bool recovering;
        static bool failureReported;
        static int lastFrame = -1, lastRegistered = -1, activeCount, activeGlobals;
        static float fallbackUntil;
        static GameLightingManager syncedManager;
        static readonly Dictionary<GameLight, RenderLight> entries = new Dictionary<GameLight, RenderLight>();
        static readonly List<RenderLight> rendered = new List<RenderLight>();
        static readonly HashSet<GameLight> modLights = new HashSet<GameLight>(), seen = new HashSet<GameLight>();
        static readonly HashSet<GameLight> transientLights = new HashSet<GameLight>();
        static readonly List<GameLight> deadEntries = new List<GameLight>();
        static GridData grid, viewGrid;
        static readonly LightView view = new LightView();
        static int[] viewSlots = new int[0];
        static Camera renderingCamera;
        static GridKey[] gridKeys = new GridKey[0];
        static int previousGridCount = -1, previousGridGlobals = -1;
        public sealed class RenderLight
        {
            public GameLight light;
            public int id;
            public bool global;
            internal bool custom;
            public Vector3 position;
            public Vector4 color;
            public float range, radiusSquared;
            internal float gridRadius, gridRadiusSquared;
        }

        internal struct CellBounds
        {
            public int x0, x1, y0, y1, z0, z1;
        }

        struct GridKey
        {
            public int id;
            public Vector3 position;
            public float radiusSquared;
        }

        public sealed class GridData
        {
            public Vector3 origin;
            public float cellSize = 16;
            public int cellCapacity = NativeLighting.NativeCellCapacity;
            public readonly uint[] counts = new uint[NativeLighting.GridDimension * NativeLighting.GridDimension * NativeLighting.GridDimension];
            public uint[] indices = new uint[NativeLighting.GridDimension * NativeLighting.GridDimension * NativeLighting.GridDimension * NativeLighting.NativeCellCapacity];
            internal int[] cursors = new int[NativeLighting.GridDimension * NativeLighting.GridDimension * NativeLighting.GridDimension];
            internal CellBounds[] bounds = new CellBounds[0];
        }

        internal static bool IsRecovering
        {
            get
            {
                return recovering;
            }
        }

        public static void OnLightAdded(GameLight light)
        {
            if (light == null || light.light == null) return;
            light.cachedPosition = light.transform.position;
            light.UpdateCachedLightColorAndIntensity();
            MapLighting.ApplyVisibility(light);
            dirty = true;
        }

        public static void OnLightChanged()
        {
            dirty = true;
        }

        internal static void RegisterTransient(GameLight light)
        {
            if (light != null && light.light != null && transientLights.Add(light)) dirty = true;
        }

        internal static void UnregisterTransient(GameLight light)
        {
            transientLights.Remove(light);
            dirty = true;
            if (ReferenceEquals(light, null)) return;
            entries.Remove(light);
            if (light == null) return;
            var manager = GameLightingManager.instance;
            var list = GameAccess.GetLights(manager);
            if (list != null && list.Contains(light)) manager.RemoveGameLight(light, false);
            light.lightId = -1;
            if (light.light != null) light.light.enabled = false;
        }

        public static void OnLightRemoved(GameLight light)
        {
            dirty = true;
            if (light == null) return;
            entries.Remove(light);
            if (light.light != null) light.light.intensity = 0;
            var manager = GameLightingManager.instance;
            var list = GameAccess.GetLights(manager);
            if (list != null && list.Contains(light)) manager.RemoveGameLight(light, false);
        }

        public static void OnLightsCleared()
        {
            dirty = true;
            entries.Clear();
        }

        internal static bool TrySync(GameLightingManager manager)
        {
            if (recovering || Time.unscaledTime < fallbackUntil) return false;
            try
            {
                bool synced = Sync(manager);
                if (synced) failureReported = false;
                return synced;
            }
            catch (Exception e)
            {
                dirty = true;
                lastFrame = -1;
                fallbackUntil = Time.unscaledTime + 2f;
                RestoreNativeLighting(manager);
                if (!failureReported)
                {
                    Plugin.Log("Lighting update: " + e, true);
                    failureReported = true;
                }
                return false;
            }
        }

        static bool Sync(GameLightingManager manager)
        {
            MapLighting.Update();
            var list = GameAccess.GetLights(manager);
            if (list == null) return false;
            if (!NativeLighting.Ready(manager)) return false;
            if (!dirty && syncedManager == manager && lastFrame == Time.frameCount && lastRegistered == list.Count)
            {
                UpdateView(false, false);
                LightBuffers.BindExtendedLighting(viewGrid, activeCount, activeGlobals);
                return true;
            }
            if (syncedManager != manager) entries.Clear();
            modLights.Clear();
            foreach (var light in LightingManager.lights) if (light != null) modLights.Add(light);
            seen.Clear();
            foreach (var light in list) if (light != null) seen.Add(light);
            foreach (var light in modLights) if (!seen.Contains(light) && light.light != null && light.gameObject.activeInHierarchy && NativeLighting.Initialized(light))
            {
                light.lightId = -1;
                light.light.enabled = true;
                light.lightId = manager.AddGameLight(light, false);
            }
            foreach (var light in transientLights) if (light != null && light.light != null && light.gameObject.activeInHierarchy && !seen.Contains(light))
            {
                light.lightId = -1;
                light.light.enabled = true;
                light.lightId = manager.AddGameLight(light, false);
            }
            rendered.Clear();
            seen.Clear();
            foreach (var light in list)
            {
                if (light == null || light.light == null || !seen.Add(light)) continue;
                bool custom = modLights.Contains(light);
                bool transient = transientLights.Contains(light);
                if (!light.gameObject.activeInHierarchy || (!light.enabled && !transient) || (custom && !MapLighting.IsVisible(light))) continue;
                RenderLight item;
                if (!entries.TryGetValue(light, out item))
                {
                    item = new RenderLight
                    {
                        light = light,
                        id = light.GetInstanceID()
                    };
                    entries.Add(light, item);
                }
                light.cachedPosition = light.transform.position;
                light.UpdateCachedLightColorAndIntensity();
                Vector3 position = light.cachedPosition;
                var color = light.cachedColorAndIntensity;
                if (!LightValues.IsFinitePoint(position) || !LightValues.IsFinite(color.x) || !LightValues.IsFinite(color.y) || !LightValues.IsFinite(color.z) || !LightValues.IsFinite(color.w) || !LightValues.IsFinite(light.range) || light.range <= 0) continue;
                if (color.x == 0 && color.y == 0 && color.z == 0) continue;
                item.position = position;
                item.custom = custom || transient;
                item.color = color;
                item.range = light.range;
                item.radiusSquared = 1f / light.range;
                item.global = transient || item.radiusSquared >= 400f;
                rendered.Add(item);
            }
            rendered.Sort(delegate(RenderLight a, RenderLight b)
            {
                int c = b.global.CompareTo(a.global);
                return c != 0 ? c : a.id.CompareTo(b.id);
            });
            int globals = 0;
            while (globals < rendered.Count && rendered[globals].global) globals++;
            bool rebuild = GridChanged(globals);
            if (rebuild)
            {
                grid = LightGridBuilder.Build(rendered, globals, grid);
                RememberGrid(globals);
            }
            UpdateView(rebuild, true);
            LightBuffers.BindExtendedLighting(viewGrid, activeCount, activeGlobals);
            manager.maxUseTestLights = NativeLighting.NativeLightCapacity;
            syncedManager = manager;
            lastFrame = Time.frameCount;
            lastRegistered = list.Count;
            dirty = false;
            if (entries.Count > list.Count + 64)
            {
                deadEntries.Clear();
                foreach (var pair in entries) if (pair.Key == null || !seen.Contains(pair.Key)) deadEntries.Add(pair.Key);
                foreach (var dead in deadEntries) entries.Remove(dead);
            }
            return true;
        }

        static void UpdateView(bool rebuild, bool uploadLights)
        {
            view.Capture(renderingCamera != null ? renderingCamera : GameAccess.GetPlayerCamera());
            if (viewSlots.Length < rendered.Count) viewSlots = new int[LightValues.GrowCapacity(rendered.Count, 128)];
            int count = 0, globals = 0;
            bool changed = rebuild;
            for (int i = 0; i < rendered.Count; i++)
            {
                var item = rendered[i];
                int slot = !item.custom || view.Intersects(item.position, item.radiusSquared) ? count++ : -1;
                if (slot >= 0 && item.global) globals++;
                if (viewSlots[i] != slot) changed = true;
                viewSlots[i] = slot;
            }
            changed |= activeCount != count || activeGlobals != globals;
            if (!changed && !uploadLights) return;
            LightBuffers.UploadLights(rendered, viewSlots, count);
            if (changed)
            {
                viewGrid = LightGridBuilder.BuildView(grid, viewSlots, viewGrid);
                LightBuffers.UploadGrid(viewGrid);
            }
            activeCount = count;
            activeGlobals = globals;
        }

        static bool GridChanged(int globals)
        {
            if (grid == null || previousGridCount != rendered.Count || previousGridGlobals != globals) return true;
            for (int i = globals; i < rendered.Count; i++)
            {
                var item = rendered[i];
                var previous = gridKeys[i];
                if (previous.id != item.id || previous.radiusSquared != item.radiusSquared || !previous.position.Equals(item.position)) return true;
            }
            return false;
        }

        static void RememberGrid(int globals)
        {
            if (gridKeys.Length < rendered.Count) gridKeys = new GridKey[LightValues.GrowCapacity(rendered.Count, 128)];
            for (int i = globals; i < rendered.Count; i++)
            {
                var item = rendered[i];
                gridKeys[i] = new GridKey
                {
                    id = item.id,
                    position = item.position,
                    radiusSquared = item.radiusSquared
                };
            }
            previousGridCount = rendered.Count;
            previousGridGlobals = globals;
        }

        static void RestoreNativeLighting(GameLightingManager manager)
        {
            recovering = true;
            try
            {
                NativeLighting.BindBuffers(manager);
                if (manager == null) return;
                previousGridCount = -1;
                manager.SortLights();
                manager.Tick();
                var list = GameAccess.GetLights(manager);
                activeCount = list == null ? 0 : Math.Min(NativeLighting.NativeLightCapacity, list.Count);
            }
            catch (Exception e)
            {
                if (!failureReported) Plugin.Log("Lighting reset: " + e, true);
            }
            finally
            {
                recovering = false;
            }
        }

        static void ReleaseExtendedLighting()
        {
            LightBuffers.Dispose();
            grid = null;
            viewGrid = null;
            viewSlots = new int[0];
            gridKeys = new GridKey[0];
            previousGridCount = -1;
            previousGridGlobals = -1;
            activeCount = 0;
            activeGlobals = 0;
            lastFrame = -1;
            dirty = true;
            failureReported = false;
        }

        public static void Dispose()
        {
            LightRenderHooks.Dispose();
            if (LightBuffers.Allocated) RestoreNativeLighting(GameLightingManager.instance);
            ReleaseExtendedLighting();
            entries.Clear();
            transientLights.Clear();
            syncedManager = null;
        }

        internal static void RenderCamera(Camera camera)
        {
            renderingCamera = camera;
            try
            {
                TrySync(GameLightingManager.instance);
            }
            finally
            {
                renderingCamera = null;
            }
        }
    }
}
