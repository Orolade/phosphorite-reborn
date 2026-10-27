using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace phosphorite
{
    public static class LightHandles
    {
        static Mesh handleMesh, handleBatchMesh;
        static Material handleMaterial, selectedMaterial, hoverMaterial;
        static Vector3[] handleUnitVertices;
        static int[] handleUnitIndices;
        static readonly List<Vector3> handleVertices = new List<Vector3>();
        static readonly List<int> handleIndices = new List<int>();
        public const float Radius = 0.25f;
        public static bool Raycast(Vector3 origin, Vector3 direction, Vector3 center, float radius, float maximum, out float distance)
        {
            distance = 0;
            if (!LightValues.IsFinitePoint(origin) || !LightValues.IsFinitePoint(direction) || !LightValues.IsFinitePoint(center) || !LightValues.IsFinite(radius) || !LightValues.IsFinite(maximum) || radius <= 0 || maximum < 0) return false;
            double x = (double) origin.x - center.x, y = (double) origin.y - center.y, z = (double) origin.z - center.z;
            double a = (double) direction.x * direction.x + (double) direction.y * direction.y + (double) direction.z * direction.z;
            if (a < 1e-12) return false;
            double b = x * direction.x + y * direction.y + z * direction.z, c = x * x + y * y + z * z - (double) radius * radius;
            if (c <= 0) return true;
            double discriminant = b * b - a * c;
            if (discriminant < 0) return false;
            double near = (-b - Math.Sqrt(discriminant)) / a;
            if (near < 0) return false;
            distance = (float)(near * Math.Sqrt(a));
            return LightValues.IsFinite(distance) && distance <= maximum;
        }

        static void Initialize()
        {
            if (handleMesh != null && handleMaterial != null) return;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) throw new InvalidOperationException("Light handle shader unavailable");
            handleMaterial = new Material(shader)
            {
                name = "phosphorite_light_handles", hideFlags = HideFlags.HideAndDontSave
            };
            handleMaterial.color = Color.white;
            selectedMaterial = new Material(handleMaterial);
            selectedMaterial.color = new Color(1, 0.72f, 0.18f, 1);
            hoverMaterial = new Material(handleMaterial);
            var vertices = LightHandleGeometry.Vertices();
            var indices = LightHandleGeometry.Indices();
            handleUnitVertices = vertices;
            handleUnitIndices = indices;
            handleMesh = new Mesh
            {
                name = "phosphorite_light_handle_mesh",
                hideFlags = HideFlags.HideAndDontSave
            };
            handleMesh.vertices = vertices;
            handleMesh.SetIndices(indices, MeshTopology.Lines, 0);
            handleMesh.RecalculateBounds();
            handleBatchMesh = new Mesh
            {
                name = "phosphorite_light_handle_batch",
                hideFlags = HideFlags.HideAndDontSave
            };
            handleBatchMesh.indexFormat = IndexFormat.UInt32;
            handleBatchMesh.MarkDynamic();
        }

        static Matrix4x4 Matrix(Vector3 position, float radius)
        {
            return Matrix4x4.TRS(position, Quaternion.identity, Vector3.one * radius);
        }

        internal static void Draw(GameLight hover, float radius, bool preview, Vector3 previewPosition)
        {
            Initialize();
            var selected = LightEditor.GetSelectedLight();
            handleVertices.Clear();
            foreach (var light in LightingManager.lights) if (LightEditor.IsMapLight(light) && light != selected && light != hover)
            {
                var position = light.transform.position;
                if (!LightValues.IsFinitePoint(position)) continue;
                foreach (var vertex in handleUnitVertices) handleVertices.Add(position + vertex * radius);
            }
            if (handleVertices.Count > 0)
            {
                if (handleIndices.Count != handleVertices.Count / handleUnitVertices.Length * handleUnitIndices.Length)
                {
                    handleIndices.Clear();
                    for (int offset = 0; offset < handleVertices.Count; offset += handleUnitVertices.Length) foreach (int index in handleUnitIndices) handleIndices.Add(offset + index);
                }
                handleBatchMesh.Clear();
                handleBatchMesh.SetVertices(handleVertices);
                handleBatchMesh.SetIndices(handleIndices, MeshTopology.Lines, 0);
                Graphics.DrawMesh(handleBatchMesh, Matrix4x4.identity, handleMaterial, 2, null, 0, null, ShadowCastingMode.Off, false, null, LightProbeUsage.Off, null);
            }
            if (selected != null && selected != hover) Graphics.DrawMesh(handleMesh, Matrix(selected.transform.position, radius), selectedMaterial, 2, null, 0, null, ShadowCastingMode.Off, false, null, LightProbeUsage.Off, null);
            hoverMaterial.color = new Color(0.1f, 0.85f, 1f, 1);
            if (hover != null) Graphics.DrawMesh(handleMesh, Matrix(hover.transform.position, radius), hoverMaterial, 2, null, 0, null, ShadowCastingMode.Off, false, null, LightProbeUsage.Off, null);
            if (preview) Graphics.DrawMesh(handleMesh, Matrix(previewPosition, radius), selectedMaterial, 2, null, 0, null, ShadowCastingMode.Off, false, null, LightProbeUsage.Off, null);
        }

        internal static void Dispose()
        {
            if (handleMesh != null) UnityEngine.Object.Destroy(handleMesh);
            if (handleBatchMesh != null) UnityEngine.Object.Destroy(handleBatchMesh);
            if (handleMaterial != null) UnityEngine.Object.Destroy(handleMaterial);
            if (selectedMaterial != null) UnityEngine.Object.Destroy(selectedMaterial);
            if (hoverMaterial != null) UnityEngine.Object.Destroy(hoverMaterial);
            handleMesh = null;
            handleBatchMesh = null;
            handleMaterial = null;
            selectedMaterial = null;
            hoverMaterial = null;
            handleUnitVertices = null;
            handleUnitIndices = null;
            handleVertices.Clear();
            handleIndices.Clear();
        }
    }
}
