using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace phosphorite
{
    public static class NativeLighting
    {
        public const int NativeLightCapacity = 100, NativeCellCapacity = 10, GridDimension = 16;
        static readonly FieldInfo packedBuffer = GameField("lightDataBuffer"), legacyBuffer = GameField("lightDataBufferLegacy"), countsBuffer = GameField("gridCountsBuffer"), indicesBuffer = GameField("gridIndicesBuffer"), initializedField = typeof(GameLight).GetField("initialized", GameAccess.InstanceFields);
        static FieldInfo GameField(string name)
        {
            return typeof(GameLightingManager).GetField(name, GameAccess.InstanceFields);
        }

        public static void CheckLayout()
        {
            foreach (var field in new[]
            {
                GameAccess.gameLightsField,
                packedBuffer,
                legacyBuffer,
                countsBuffer,
                indicesBuffer,
                initializedField
            }) if (field == null) throw new MissingFieldException("Missing lighting field");
            var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var pair in new[]
            {
                new KeyValuePair<string, int>("MAX_VERTEX_LIGHTS", NativeLightCapacity),
                new KeyValuePair<string, int>("GRID_CELL_CAPACITY", NativeCellCapacity),
                new KeyValuePair<string, int>("GRID_MAX_DIM", GridDimension)
            })
            {
                var field = typeof(GameLightingManager).GetField(pair.Key, flags);
                if (field == null || (int) field.GetRawConstantValue() != pair.Value) throw new NotSupportedException("Unsupported lighting constant: " + pair.Key);
            }
            foreach (var pair in new[]
            {
                new KeyValuePair<string, Type>("LightDataPacked", typeof(LightBuffers.PackedLight)),
                new KeyValuePair<string, Type>("LightDataLegacy", typeof(LightBuffers.LegacyLight))
            })
            {
                var native = typeof(GameLightingManager).GetNestedType(pair.Key, BindingFlags.Public | BindingFlags.NonPublic);
                if (native == null || Marshal.SizeOf(native) != Marshal.SizeOf(pair.Value)) throw new NotSupportedException("Unsupported light format: " + pair.Key);
                foreach (var field in pair.Value.GetFields(BindingFlags.Public | BindingFlags.Instance)) if (Marshal.OffsetOf(native, field.Name) != Marshal.OffsetOf(pair.Value, field.Name)) throw new NotSupportedException("Unsupported light field: " + field.Name);
            }
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
