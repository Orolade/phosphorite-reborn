using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace phosphorite
{
    public static class LightHandles
    {
        static Mesh handleMesh;
        static Material handleMaterial, outlineMaterial;
        static Vector3[] handleUnitVertices;
        static int[] handleUnitIndices;
        static readonly Dictionary<Color32, MarkerBatch> batches = new Dictionary<Color32, MarkerBatch>();
        static readonly List<Color32> unusedColors = new List<Color32>();
        static MaterialPropertyBlock markerColor;
        public const float Radius = 0.25f;

        class MarkerBatch
        {
            readonly Mesh mesh = new Mesh { name = "phosphorite_light_markers", hideFlags = HideFlags.HideAndDontSave, indexFormat = IndexFormat.UInt32 };
            readonly List<Vector3> vertices = new List<Vector3>();
            readonly List<int> indices = new List<int>();
            readonly MaterialPropertyBlock color = new MaterialPropertyBlock();
            int vertexCount;

            public MarkerBatch(Color32 tint)
            {
                mesh.MarkDynamic();
                color.SetColor("_BaseColor", tint);
                color.SetColor("_Color", tint);
            }

            public bool Empty { get { return vertices.Count == 0; } }
            public void Clear() { vertices.Clear(); }

            public void Add(Vector3 position, float radius)
            {
                foreach (var vertex in handleUnitVertices) vertices.Add(position + vertex * radius);
            }

            public void Draw()
            {
                if (vertexCount != vertices.Count)
                {
                    mesh.Clear();
                    indices.Clear();
                    for (int offset = 0; offset < vertices.Count; offset += handleUnitVertices.Length)
                        foreach (int index in handleUnitIndices) indices.Add(offset + index);
                    mesh.SetVertices(vertices);
                    mesh.SetIndices(indices, MeshTopology.Triangles, 0);
                    vertexCount = vertices.Count;
                }
                else
                {
                    mesh.SetVertices(vertices);
                    mesh.RecalculateBounds();
                }
                Graphics.DrawMesh(mesh, Matrix4x4.identity, handleMaterial, 2, null, 0, color, ShadowCastingMode.Off, false, null, LightProbeUsage.Off, null);
            }

            public void Dispose() { UnityEngine.Object.Destroy(mesh); }
        }

        public static bool Raycast(Vector3 origin, Vector3 direction, Vector3 center, float radius, float maximum, out float distance)
        {
            distance = 0;
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
            return distance <= maximum;
        }

        internal static GameLight Pick(Ray ray, float radius, float maximum)
        {
            GameLight chosen = null;
            float nearest = float.PositiveInfinity;
            int nearestId = int.MaxValue;
            foreach (var light in LightingManager.lights) if (LightEditor.IsMapLight(light))
            {
                float distance;
                if (!Raycast(ray.origin, ray.direction, light.transform.position, radius, maximum, out distance)) continue;
                int id = light.GetInstanceID();
                if (SurfaceRaycaster.IsCloserHit(distance, id, nearest, nearestId))
                {
                    chosen = light;
                    nearest = distance;
                    nearestId = id;
                }
            }
            return chosen;
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
            markerColor = new MaterialPropertyBlock();
            outlineMaterial = new Material(handleMaterial);
            outlineMaterial.name = "phosphorite_light_marker_outline";
            if (outlineMaterial.HasProperty("_Cull")) outlineMaterial.SetInt("_Cull", (int) CullMode.Front);
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
            handleMesh.SetIndices(indices, MeshTopology.Triangles, 0);
            handleMesh.RecalculateBounds();
        }

        static Matrix4x4 Matrix(Vector3 position, float radius)
        {
            return Matrix4x4.TRS(position, Quaternion.identity, Vector3.one * radius);
        }

        internal static void Draw(GameLight selected, GameLight hover, float radius, bool preview, Vector3 previewPosition)
        {
            Initialize();
            foreach (var batch in batches.Values) batch.Clear();
            foreach (var light in LightingManager.lights) if (LightEditor.IsMapLight(light) && light != selected && light != hover)
            {
                var position = light.transform.position;
                Color32 color = light.light.color;
                color.a = 255;
                MarkerBatch batch;
                if (!batches.TryGetValue(color, out batch))
                {
                    batch = new MarkerBatch(color);
                    batches.Add(color, batch);
                }
                batch.Add(position, radius * 0.2f);
            }
            unusedColors.Clear();
            foreach (var pair in batches)
            {
                if (pair.Value.Empty)
                {
                    pair.Value.Dispose();
                    unusedColors.Add(pair.Key);
                }
                else pair.Value.Draw();
            }
            foreach (var color in unusedColors) batches.Remove(color);
            if (selected != null && selected != hover) DrawMarker(selected.transform.position, selected.light.color, radius * 0.24f);
            if (hover != null) DrawMarker(hover.transform.position, hover.light.color, radius * 0.28f);
            if (preview) DrawMarker(previewPosition, selected == null ? Color.white : selected.light.color, radius * 0.24f);
        }

        static void DrawMarker(Vector3 position, Color color, float radius)
        {
            color.a = 1;
            markerColor.SetColor("_BaseColor", color);
            markerColor.SetColor("_Color", color);
            Graphics.DrawMesh(handleMesh, Matrix(position, radius), handleMaterial, 2, null, 0, markerColor, ShadowCastingMode.Off, false, null, LightProbeUsage.Off, null);
            if (outlineMaterial.HasProperty("_Cull"))
                Graphics.DrawMesh(handleMesh, Matrix(position, radius * 1.22f), outlineMaterial, 2, null, 0, null, ShadowCastingMode.Off, false, null, LightProbeUsage.Off, null);
        }

        internal static void Dispose()
        {
            if (handleMesh != null) UnityEngine.Object.Destroy(handleMesh);
            if (handleMaterial != null) UnityEngine.Object.Destroy(handleMaterial);
            if (outlineMaterial != null) UnityEngine.Object.Destroy(outlineMaterial);
            foreach (var batch in batches.Values) batch.Dispose();
            batches.Clear();
            unusedColors.Clear();
            handleMesh = null;
            handleMaterial = null;
            outlineMaterial = null;
            markerColor = null;
            handleUnitVertices = null;
            handleUnitIndices = null;
        }
    }
}
