using System;
using UnityEngine;

namespace phosphorite
{
    public static class MapSurfaceLookup
    {
        const float SurfaceRadius = 0.25f;
        static readonly Collider[] nearby = new Collider[32];

        internal static GTZone FromCollider(Collider surface)
        {
            if (!SurfaceRaycaster.IsWorldSurface(surface)) return GTZone.none;
            var zones = GameAccess.GetZones();
            if (zones == null) return GTZone.none;
            for (var node = surface.transform; node != null; node = node.parent)
            {
                GTZone result = GTZone.none;
                foreach (var data in zones)
                {
                    if (data == null || data.rootGameObjects == null) continue;
                    foreach (var root in data.rootGameObjects) if (root != null && root.transform == node)
                    {
                        var zone = MapLighting.Normalize(data.zone);
                        if (zone == GTZone.none) continue;
                        if (result != GTZone.none && result != zone) return GTZone.none;
                        result = zone;
                    }
                }
                if (result != GTZone.none) return result;
            }
            return GTZone.none;
        }

        internal static GTZone NearPosition(Vector3 position)
        {
            if (GameAccess.GetZones() == null) return GTZone.none;
            int count = Physics.OverlapSphereNonAlloc(position, SurfaceRadius, nearby, ~0, QueryTriggerInteraction.Ignore);
            var surfaces = count == nearby.Length ? Physics.OverlapSphere(position, SurfaceRadius, ~0, QueryTriggerInteraction.Ignore) : nearby;
            if (surfaces != nearby) count = surfaces.Length;
            GTZone bestZone = GTZone.none;
            float best = float.PositiveInfinity;
            bool ambiguous = false;
            for (int i = 0; i < count; i++)
            {
                var surface = surfaces[i];
                var zone = FromCollider(surface);
                if (zone == GTZone.none) continue;
                float distance = (surface.ClosestPoint(position) - position).sqrMagnitude;
                if (distance > SurfaceRadius * SurfaceRadius) continue;
                if (distance < best - 0.000001f)
                {
                    best = distance;
                    bestZone = zone;
                    ambiguous = false;
                }
                else if (Math.Abs(distance - best) <= 0.000001f && zone != bestZone) ambiguous = true;
            }
            Array.Clear(nearby, 0, nearby.Length);
            return ambiguous ? GTZone.none : bestZone;
        }
    }
}
