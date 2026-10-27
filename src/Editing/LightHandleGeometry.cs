using UnityEngine;

namespace phosphorite
{
    public static class LightHandleGeometry
    {
        internal static Vector3[] Vertices()
        {
            return new[]
            {
                Vector3.up, Vector3.down, Vector3.left, Vector3.forward, Vector3.right, Vector3.back,
                Vector3.left * 0.18f, Vector3.right * 0.18f,
                Vector3.up * 0.18f, Vector3.down * 0.18f,
                Vector3.forward * 0.18f, Vector3.back * 0.18f
            };
        }

        internal static int[] Indices()
        {
            return new[] { 0,2, 0,3, 0,4, 0,5, 1,2, 1,3, 1,4, 1,5, 2,3, 3,4, 4,5, 5,2, 6,7, 8,9, 10,11 };
        }
    }
}
