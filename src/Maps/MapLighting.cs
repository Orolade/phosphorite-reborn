using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using GorillaLocomotion;

namespace phosphorite
{
    public static class MapLighting
    {
        static readonly Dictionary<GameLight, MapOwner> mapOwners = new Dictionary<GameLight, MapOwner>();
        static readonly List<GameLight> deadMapLights = new List<GameLight>();
        static readonly HashSet<GameLight> liveMapLights = new HashSet<GameLight>();
        static ZoneGraphBSP mapGraph;
        static bool mapScopeEnabled, mapScopeReady;
        static GTZone lightMap = GTZone.none;
        static int mapFrame = -1;
        static float mapRetryAfter;
        static readonly Vector3[] playerMapOffsets =
        {
            Vector3.zero,
            new Vector3(0.35f, 0, 0),
            new Vector3(-0.35f, 0, 0),
            new Vector3(0, 0, 0.35f),
            new Vector3(0, 0, -0.35f),
            new Vector3(0, -0.5f, 0)
        };
        static readonly GTZone[] playerMapSamples = new GTZone[6];
        static readonly Vector3[] mapBoundaryOffsets =
        {
            new Vector3(0.1f, 0, 0),
            new Vector3(-0.1f, 0, 0),
            new Vector3(0, 0.1f, 0),
            new Vector3(0, -0.1f, 0),
            new Vector3(0, 0, 0.1f),
            new Vector3(0, 0, -0.1f)
        };
        class MapOwner
        {
            public Vector3 position;
            public GTZone zone;
        }

        public static GTZone Normalize(GTZone zone)
        {
            if (zone == GTZone.cityNoBuildings || zone == GTZone.cityWithSkyJungle) return GTZone.city;
            if (zone == GTZone.forestWithCity) return GTZone.forest;
            if (zone == GTZone.ghostReactorTunnel || zone == GTZone.ghostReactorDrill) return GTZone.ghostReactor;
            return zone;
        }

        public static bool Matches(GTZone source, GTZone player)
        {
            return player != GTZone.none && source != GTZone.none && Normalize(source) == Normalize(player);
        }

        internal static void Initialize()
        {
            mapScopeEnabled = true;
            ZoneEntityBSP.onPlayerZoneChange += OnZoneChanged;
        }

        static void OnZoneChanged(VRRig rig, GTZone previous, GTZone next)
        {
            if (rig != null && rig.isOfflineVRRig) mapFrame = -1;
        }

        public static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            mapFrame = -1;
            mapOwners.Clear();
        }

        static GTZone GetPlayerMap()
        {
            var tagger = GorillaTagger.Instance;
            GTZone rig = GTZone.none;
            if (tagger != null && tagger.offlineVRRig != null && tagger.offlineVRRig.zoneEntity != null) rig = Normalize(tagger.offlineVRRig.zoneEntity.currentZone);
            Vector3 center;
            if (GameAccess.TryGetPlayerPosition(out center))
            {
                float scale = GorillaLocomotion.GTPlayer.Instance == null ? 1 : GorillaLocomotion.GTPlayer.Instance.scale;
                if (!LightValues.IsFinite(scale) || scale <= 0) scale = 1;
                return ResolvePlayerMap(center, scale, rig);
            }
            if (IsMapActive(rig)) return rig;
            return IsMapActive(lightMap) ? lightMap : GetOnlyActiveMap();
        }

        static bool IsMapActive(GTZone zone)
        {
            zone = Normalize(zone);
            if (zone == GTZone.none) return false;
            var manager = ZoneManagement.instance;
            if (manager == null || manager.activeZones == null) return true;
            foreach (var active in manager.activeZones) if (Normalize(active) == zone) return true;
            return false;
        }

        static GTZone GetOnlyActiveMap()
        {
            var manager = ZoneManagement.instance;
            if (manager == null || manager.activeZones == null) return GTZone.none;
            GTZone result = GTZone.none;
            foreach (var active in manager.activeZones)
            {
                var zone = Normalize(active);
                if (zone == GTZone.none) continue;
                if (result != GTZone.none && result != zone) return GTZone.none;
                result = zone;
            }
            return result;
        }

        static GTZone ResolvePlayerMap(Vector3 center, float scale, GTZone rig)
        {
            if (mapGraph == null || !LightValues.IsFinitePoint(center)) return IsMapActive(lightMap) ? lightMap : GTZone.none;
            bool keep = IsMapActive(lightMap);
            GTZone candidate = GTZone.none;
            int best = 0;
            for (int i = 0; i < playerMapOffsets.Length; i++)
            {
                var node = mapGraph.FindZoneAtPoint(center + playerMapOffsets[i] * scale);
                var zone = node == null ? GTZone.none : Normalize(node.zoneId);
                if (!IsMapActive(zone)) zone = GTZone.none;
                playerMapSamples[i] = zone;
                if (keep && zone == lightMap) return lightMap;
            }
            for (int i = 0; i < playerMapSamples.Length; i++)
            {
                var zone = playerMapSamples[i];
                if (zone == GTZone.none) continue;
                int votes = 0;
                for (int j = 0; j < playerMapSamples.Length; j++) if (playerMapSamples[j] == zone) votes++;
                if (votes > best)
                {
                    best = votes;
                    candidate = zone;
                }
            }
            if (candidate != GTZone.none && (!keep || best >= 2)) return candidate;
            if (keep) return lightMap;
            if (IsMapActive(rig)) return rig;
            return GetOnlyActiveMap();
        }

        static GTZone FindMap(Vector3 position)
        {
            if (mapGraph == null || !LightValues.IsFinitePoint(position)) return GTZone.none;
            var surface = MapSurfaceLookup.NearPosition(position);
            if (surface != GTZone.none) return surface;
            var node = mapGraph.FindZoneAtPoint(position);
            if (node != null && node.zoneId != GTZone.none) return Normalize(node.zoneId);
            foreach (var offset in mapBoundaryOffsets)
            {
                node = mapGraph.FindZoneAtPoint(position + offset);
                if (node != null && node.zoneId != GTZone.none) return Normalize(node.zoneId);
            }
            return MapZoneGeometry.Nearest(mapGraph, position);
        }

        static MapOwner GetOwner(GameLight light)
        {
            MapOwner owner;
            var position = light.transform.position;
            if (!mapOwners.TryGetValue(light, out owner) || owner.position != position)
            {
                owner = new MapOwner
                {
                    position = position,
                    zone = FindMap(position)
                };
                mapOwners[light] = owner;
            }
            return owner;
        }

        internal static bool IsVisible(GameLight light)
        {
            return !mapScopeEnabled || (mapScopeReady && light != null && Matches(GetOwner(light).zone, lightMap));
        }

        internal static void ApplyVisibility(GameLight light)
        {
            if (!mapScopeEnabled || light == null || light.light == null || GameLightingManager.instance == null) return;
            bool visible = IsVisible(light);
            if (light.gameObject.activeSelf != visible)
            {
                light.gameObject.SetActive(visible);
                LightRenderer.OnLightChanged();
            }
        }

        static void SetCurrentMap(GTZone zone)
        {
            zone = Normalize(zone);
            if (lightMap != zone)
            {
                LightEditor.ResetSelection(false);
                mapOwners.Clear();
                lightMap = zone;
                mapRetryAfter = 0;
            }
            mapScopeReady = mapGraph != null && zone != GTZone.none;
            liveMapLights.Clear();
            foreach (var light in LightingManager.lights)
            {
                ApplyVisibility(light);
                if (light != null) liveMapLights.Add(light);
            }
            deadMapLights.Clear();
            foreach (var pair in mapOwners) if (pair.Key == null || !liveMapLights.Contains(pair.Key)) deadMapLights.Add(pair.Key);
            foreach (var dead in deadMapLights) mapOwners.Remove(dead);
        }

        internal static void Update()
        {
            if (!mapScopeEnabled || Time.unscaledTime < mapRetryAfter) return;
            if (LightingManager.Runner == null || GameLightingManager.instance == null)
            {
                mapScopeReady = false;
                return;
            }
            if (mapFrame == Time.frameCount) return;
            mapFrame = Time.frameCount;
            try
            {
                var graph = ZoneGraphBSP.Instance;
                if (graph != mapGraph)
                {
                    mapGraph = graph;
                    mapOwners.Clear();
                }
                if (graph == null || !graph.HasCompiledTree())
                {
                    SetCurrentMap(GTZone.none);
                    return;
                }
                var next = GetPlayerMap();
                SetCurrentMap(next);
            }
            catch (Exception e)
            {
                mapRetryAfter = Time.unscaledTime + 2f;
                Plugin.Log("Map lights: " + e, true);
            }
        }

        internal static void OnLightsCleared()
        {
            mapOwners.Clear();
            mapFrame = -1;
        }

        internal static void ClearCurrentMap()
        {
            if (!mapScopeEnabled)
            {
                LightingManager.ClearLights();
                return;
            }
            foreach (var light in LightingManager.lights.ToArray()) if (IsVisible(light)) LightingManager.RemoveLight(light);
        }

        internal static void Dispose()
        {
            ZoneEntityBSP.onPlayerZoneChange -= OnZoneChanged;
            mapScopeEnabled = false;
            mapScopeReady = false;
            mapOwners.Clear();
            if (GameLightingManager.instance != null) foreach (var light in LightingManager.lights) if (light != null && !light.gameObject.activeSelf) light.gameObject.SetActive(true);
        }

    }
}
