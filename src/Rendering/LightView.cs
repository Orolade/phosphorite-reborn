using System;
using UnityEngine;

namespace phosphorite
{
    public sealed class LightView
    {
        readonly Plane[] left = new Plane[6], right = new Plane[6];
        bool available, stereo;
        public void Capture(Camera camera)
        {
            available = camera != null;
            if (!available) return;
            stereo = camera.stereoEnabled;
            if (stereo)
            {
                GeometryUtility.CalculateFrustumPlanes(camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left) * camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left), left);
                GeometryUtility.CalculateFrustumPlanes(camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right) * camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right), right);
            }
            else GeometryUtility.CalculateFrustumPlanes(camera.cullingMatrix, left);
        }

        public bool Intersects(Vector3 position, float radiusSquared)
        {
            if (!available) return true;
            float padding = 0.5f + (Math.Abs(position.x) + Math.Abs(position.y) + Math.Abs(position.z)) / 2048f;
            return Intersects(left, position, radiusSquared, padding) || (stereo && Intersects(right, position, radiusSquared, padding));
        }

        static bool Intersects(Plane[] planes, Vector3 position, float radiusSquared, float padding)
        {
            foreach (var plane in planes)
            {
                float distance = plane.GetDistanceToPoint(position) + padding;
                if (distance < 0 && distance * distance > radiusSquared) return false;
            }
            return true;
        }
    }
}
