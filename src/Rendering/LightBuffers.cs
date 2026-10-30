using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace phosphorite
{
    public static class LightBuffers
    {
        static GraphicsBuffer extendedPacked, extendedLegacy, extendedCounts, extendedIndices;
        static PackedLight[] packedLights;
        static LegacyLight[] legacyLights;
        static int useId, globalId, packedId, legacyId, countsId, indicesId, originId, invCellId, dimsId, cellCapacityId;
        [StructLayout(LayoutKind.Sequential)]
        public struct PackedLight
        {
            public uint posXY, posZW, colorRG, colorBA;
            public float range;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LegacyLight
        {
            public Vector4 position, color, direction;
        }

        internal static bool Allocated
        {
            get
            {
                return extendedPacked != null;
            }
        }

        public static uint PackLightHalf2(float x, float y)
        {
            return (uint) Mathf.FloatToHalf(x) | ((uint) Mathf.FloatToHalf(y) << 16);
        }

        internal static void EnsureLightBuffers(int count)
        {
            if (extendedPacked != null && extendedLegacy != null && packedLights.Length >= count) return;
            int size = LightValues.GrowCapacity(count, 128);
            GraphicsBuffer packed = null, legacy = null;
            try
            {
                packed = new GraphicsBuffer(GraphicsBuffer.Target.Structured, size, Marshal.SizeOf(typeof(PackedLight)));
                legacy = new GraphicsBuffer(GraphicsBuffer.Target.Structured, size, Marshal.SizeOf(typeof(LegacyLight)));
            }
            catch
            {
                if (packed != null) packed.Dispose();
                if (legacy != null) legacy.Dispose();
                throw;
            }
            ReleaseBuffer(ref extendedPacked);
            ReleaseBuffer(ref extendedLegacy);
            extendedPacked = packed;
            extendedLegacy = legacy;
            packedLights = new PackedLight[size];
            legacyLights = new LegacyLight[size];
        }

        internal static void EnsureGridBuffers(LightRenderer.GridData grid)
        {
            if (extendedCounts == null) extendedCounts = new GraphicsBuffer(GraphicsBuffer.Target.Structured, grid.counts.Length, 4);
            int size = grid.counts.Length * grid.cellCapacity;
            if (extendedIndices != null && extendedIndices.count >= size) return;
            var replacement = new GraphicsBuffer(GraphicsBuffer.Target.Structured, size, 4);
            ReleaseBuffer(ref extendedIndices);
            extendedIndices = replacement;
        }

        internal static void BindExtendedLighting(LightRenderer.GridData grid, int count, int globals)
        {
            Shader.SetGlobalBuffer(packedId, extendedPacked);
            Shader.SetGlobalBuffer(legacyId, extendedLegacy);
            Shader.SetGlobalBuffer(countsId, extendedCounts);
            Shader.SetGlobalBuffer(indicesId, extendedIndices);
            Shader.SetGlobalVector(originId, new Vector4(grid.origin.x, grid.origin.y, grid.origin.z, 0));
            Shader.SetGlobalFloat(invCellId, 1f / grid.cellSize);
            Shader.SetGlobalVector(dimsId, new Vector4(NativeLighting.GridDimension, NativeLighting.GridDimension, NativeLighting.GridDimension, 0));
            Shader.SetGlobalInteger(cellCapacityId, grid.cellCapacity);
            Shader.SetGlobalInteger(globalId, globals);
            Shader.SetGlobalInteger(useId, count);
        }

        internal static void ReleaseBuffer(ref GraphicsBuffer buffer)
        {
            if (buffer == null) return;
            buffer.Dispose();
            buffer = null;
        }

        internal static void Initialize()
        {
            useId = Shader.PropertyToID("_GT_GameLight_UseMaxLights");
            globalId = Shader.PropertyToID("_GT_GameLight_GlobalLightCount");
            packedId = Shader.PropertyToID("_GT_GameLight_LightsPacked");
            legacyId = Shader.PropertyToID("_GT_GameLight_Lights");
            countsId = Shader.PropertyToID("_GT_LightGridCounts");
            indicesId = Shader.PropertyToID("_GT_LightGridIndices");
            originId = Shader.PropertyToID("_GT_LightGrid_Origin");
            invCellId = Shader.PropertyToID("_GT_LightGrid_InvCellSize");
            dimsId = Shader.PropertyToID("_GT_LightGrid_Dims");
            cellCapacityId = Shader.PropertyToID("_GT_LightGrid_CellCapacity");
        }

        internal static void UploadLights(IList<LightRenderer.RenderLight> lights, int[] slots, int count)
        {
            EnsureLightBuffers(count);
            for (int i = 0; i < lights.Count; i++)
            {
                int slot = slots[i];
                if (slot < 0) continue;
                var item = lights[i];
                var p = item.position;
                var c = item.color;
                packedLights[slot] = new PackedLight
                {
                    posXY = PackLightHalf2(p.x, p.y),
                    posZW = PackLightHalf2(p.z, 1),
                    colorRG = PackLightHalf2(c.x, c.y),
                    colorBA = PackLightHalf2(c.z, c.w),
                    range = item.range
                };
                legacyLights[slot] = new LegacyLight
                {
                    position = new Vector4(p.x, p.y, p.z, 1),
                    color = c,
                    direction = Vector4.zero
                };
            }
            if (count > 0)
            {
                extendedPacked.SetData(packedLights, 0, 0, count);
                extendedLegacy.SetData(legacyLights, 0, 0, count);
            }
        }

        internal static void UploadGrid(LightRenderer.GridData grid)
        {
            EnsureGridBuffers(grid);
            extendedCounts.SetData(grid.counts);
            extendedIndices.SetData(grid.indices, 0, 0, grid.counts.Length * grid.cellCapacity);
        }

        internal static void Dispose()
        {
            ReleaseBuffer(ref extendedPacked);
            ReleaseBuffer(ref extendedLegacy);
            ReleaseBuffer(ref extendedCounts);
            ReleaseBuffer(ref extendedIndices);
            packedLights = null;
            legacyLights = null;
        }

        internal static void ResetCounts()
        {
            Shader.SetGlobalInteger(useId, 0);
            Shader.SetGlobalInteger(globalId, 0);
        }

        internal static void BindNative(GraphicsBuffer packed, GraphicsBuffer legacy, GraphicsBuffer counts, GraphicsBuffer indices)
        {
            if (packed != null) Shader.SetGlobalBuffer(packedId, packed);
            if (legacy != null) Shader.SetGlobalBuffer(legacyId, legacy);
            if (counts != null) Shader.SetGlobalBuffer(countsId, counts);
            if (indices != null) Shader.SetGlobalBuffer(indicesId, indices);
            Shader.SetGlobalInteger(cellCapacityId, NativeLighting.NativeCellCapacity);
        }
    }
}
