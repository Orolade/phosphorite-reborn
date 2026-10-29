using UnityEngine;

namespace phosphorite
{
    public static class LightHandleGeometry
    {
        const int Segments = 16;
        const int Rings = 8;

        internal static Vector3[] Vertices()
        {
            var vertices = new Vector3[2 + (Rings - 1) * Segments];
            vertices[0] = Vector3.up;
            vertices[vertices.Length - 1] = Vector3.down;
            for (int ring = 1; ring < Rings; ring++)
            {
                float latitude = Mathf.PI * ring / Rings;
                float width = Mathf.Sin(latitude);
                float height = Mathf.Cos(latitude);
                for (int segment = 0; segment < Segments; segment++)
                {
                    float longitude = 2 * Mathf.PI * segment / Segments;
                    vertices[1 + (ring - 1) * Segments + segment] = new Vector3(width * Mathf.Cos(longitude), height, width * Mathf.Sin(longitude));
                }
            }
            return vertices;
        }

        internal static int[] Indices()
        {
            var indices = new int[6 * Segments * (Rings - 1)];
            int index = 0;
            for (int segment = 0; segment < Segments; segment++)
            {
                int next = (segment + 1) % Segments;
                indices[index++] = 0;
                indices[index++] = 1 + next;
                indices[index++] = 1 + segment;
                for (int ring = 0; ring < Rings - 2; ring++)
                {
                    int top = 1 + ring * Segments;
                    int bottom = top + Segments;
                    indices[index++] = top + segment;
                    indices[index++] = top + next;
                    indices[index++] = bottom + segment;
                    indices[index++] = top + next;
                    indices[index++] = bottom + next;
                    indices[index++] = bottom + segment;
                }
                indices[index++] = 1 + (Rings - 1) * Segments;
                indices[index++] = 1 + (Rings - 2) * Segments + segment;
                indices[index++] = 1 + (Rings - 2) * Segments + next;
            }
            return indices;
        }
    }
}
