using System;
using UnityEngine;
using GorillaLocomotion;

namespace phosphorite
{
    public static class SurfaceRaycaster
    {
        internal const float GunMaxDistance = 500f;
        static readonly RaycastHit[] gunHits = new RaycastHit[64];
        public static Vector3 SurfacePosition(Vector3 point, Vector3 normal, float scale)
        {
            return point + normal * (0.04f * scale);
        }

        public static bool IsCloserHit(float distance, int colliderId, float currentDistance, int currentId)
        {
            return LightValues.IsFinite(distance) && distance >= 0 && (distance < currentDistance || (distance == currentDistance && colliderId < currentId));
        }

        internal static bool Raycast(Ray ray, out RaycastHit target)
        {
            return Raycast(ray, GunMaxDistance, out target);
        }

        internal static bool Raycast(Ray ray, float maximum, out RaycastHit target)
        {
            target = default(RaycastHit);
            int count = Physics.RaycastNonAlloc(ray, gunHits, maximum, ~0, QueryTriggerInteraction.Ignore);
            var hits = gunHits;
            if (count == gunHits.Length)
            {
                hits = Physics.RaycastAll(ray, maximum, ~0, QueryTriggerInteraction.Ignore);
                count = hits.Length;
            }
            bool found = false;
            float best = float.PositiveInfinity;
            int bestId = int.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                var collider = hit.collider;
                if (!IsWorldSurface(collider)) continue;
                if (!LightValues.IsFinite(hit.point.x) || !LightValues.IsFinite(hit.point.y) || !LightValues.IsFinite(hit.point.z) || !LightValues.IsFinite(hit.normal.x) || !LightValues.IsFinite(hit.normal.y) || !LightValues.IsFinite(hit.normal.z)) continue;
                int id = collider.GetInstanceID();
                if (!IsCloserHit(hit.distance, id, best, bestId)) continue;
                found = true;
                best = hit.distance;
                bestId = id;
                target = hit;
            }
            return found;
        }

        internal static bool IsWorldSurface(Collider collider)
        {
            if (collider == null || collider.isTrigger || !collider.enabled) return false;
            if (collider.GetComponentInParent<VRRig>(true) != null || collider.GetComponentInParent<GorillaTagger>(true) != null || collider.GetComponentInParent<GTPlayer>(true) != null) return false;
            return !ToolPointer.Contains(collider.transform);
        }
    }
}
