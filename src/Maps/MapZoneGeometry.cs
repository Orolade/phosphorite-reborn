using System;
using UnityEngine;

namespace phosphorite
{
    public static class MapZoneGeometry
    {
        internal static float DistanceSquared(Matrix4x4 worldToBox, Vector3 position)
        {
            var point = worldToBox.MultiplyPoint3x4(position);
            float distance = 0;
            for (int axis = 0; axis < 3; axis++)
            {
                var row = worldToBox.GetRow(axis);
                float length = (float) Math.Sqrt(row.x * row.x + row.y * row.y + row.z * row.z);
                if (length < 0.000001f) return float.PositiveInfinity;
                float gap = Math.Max(0, Math.Abs(point[axis]) - 1) / length;
                distance += gap * gap;
            }
            return distance;
        }

        internal static GTZone Nearest(ZoneGraphBSP graph, Vector3 position)
        {
            var tree = graph == null ? null : graph.GetBSPTree();
            if (tree == null || tree.matrices == null || tree.zones == null) return GTZone.none;
            GTZone result = GTZone.none;
            float best = float.PositiveInfinity;
            bool ambiguous = false;
            foreach (var volume in tree.matrices)
            {
                if (volume.zoneIndex < 0 || volume.zoneIndex >= tree.zones.Length) continue;
                var definition = tree.zones[volume.zoneIndex];
                if (definition == null) continue;
                var zone = MapLighting.Normalize(definition.zoneId);
                if (zone == GTZone.none) continue;
                float distance = DistanceSquared(volume.matrix, position);
                if (distance < best - 0.000001f)
                {
                    best = distance;
                    result = zone;
                    ambiguous = false;
                }
                else if (Math.Abs(distance - best) <= 0.000001f && zone != result) ambiguous = true;
            }
            return ambiguous ? GTZone.none : result;
        }
    }
}
