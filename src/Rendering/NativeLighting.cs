using System;
using System.Reflection;
using UnityEngine;

namespace phosphorite
{
    public static class NativeLighting
    {
        public const int NativeLightCapacity = GameLightingManager.MAX_VERTEX_LIGHTS, NativeCellCapacity = 10, GridDimension = 16;
        static readonly FieldInfo packedBuffer = GameField("lightDataBuffer"), legacyBuffer = GameField("lightDataBufferLegacy"), countsBuffer = GameField("gridCountsBuffer"), indicesBuffer = GameField("gridIndicesBuffer"), initializedField = typeof(GameLight).GetField("initialized", GameAccess.InstanceFields);
        static FieldInfo GameField(string name)
        {
            return typeof(GameLightingManager).GetField(name, GameAccess.InstanceFields);
        }

        public static int NativeRefreshCount(int count)
        {
            return Math.Max(1, Math.Min(NativeLightCapacity, count));
        }

        internal static bool Ready(GameLightingManager manager)
        {
            return packedBuffer.GetValue(manager) != null && legacyBuffer.GetValue(manager) != null && countsBuffer.GetValue(manager) != null && indicesBuffer.GetValue(manager) != null;
        }

        internal static bool Initialized(GameLight light)
        {
            return (bool) initializedField.GetValue(light);
        }

        internal static void BindBuffers(GameLightingManager manager)
        {
            LightBuffers.ResetCounts();
            if (manager == null) return;
            LightBuffers.BindNative(packedBuffer.GetValue(manager) as GraphicsBuffer, legacyBuffer.GetValue(manager) as GraphicsBuffer, countsBuffer.GetValue(manager) as GraphicsBuffer, indicesBuffer.GetValue(manager) as GraphicsBuffer);
            manager.maxUseTestLights = NativeLightCapacity;
        }
    }
}
