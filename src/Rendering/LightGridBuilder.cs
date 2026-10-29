using System;
using System.Collections.Generic;
using UnityEngine;

namespace phosphorite
{
    public static class LightGridBuilder
    {
        public static LightRenderer.GridData Build(IList<LightRenderer.RenderLight> lights, int globals, LightRenderer.GridData reuse)
        {
            var data = reuse ?? new LightRenderer.GridData();
            Array.Clear(data.counts, 0, data.counts.Length);
            Array.Clear(data.cursors, 0, data.cursors.Length);
            if (data.bounds.Length < lights.Count) data.bounds = new LightRenderer.CellBounds[LightValues.GrowCapacity(lights.Count, 128)];
            if (globals == lights.Count)
            {
                data.origin = Vector3.zero;
                data.cellSize = 16;
                return data;
            }
            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity), max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            for (int i = globals; i < lights.Count; i++)
            {
                var item = lights[i];
                var p = item.position;
                float radius = (float) Math.Sqrt(item.radiusSquared) + (Math.Abs(p.x) + Math.Abs(p.y) + Math.Abs(p.z)) / 2048f + 0.001f;
                item.gridRadius = radius;
                item.gridRadiusSquared = radius * radius;
                min.x = Math.Min(min.x, p.x - radius);
                min.y = Math.Min(min.y, p.y - radius);
                min.z = Math.Min(min.z, p.z - radius);
                max.x = Math.Max(max.x, p.x + radius);
                max.y = Math.Max(max.y, p.y + radius);
                max.z = Math.Max(max.z, p.z + radius);
            }
            float span = Math.Max(max.x - min.x, Math.Max(max.y - min.y, max.z - min.z));
            data.cellSize = Math.Max(1f, (float) Math.Ceiling((span + 0.01f) / (NativeLighting.GridDimension - 1)));
            data.origin = new Vector3((float) Math.Floor(min.x / data.cellSize) * data.cellSize, (float) Math.Floor(min.y / data.cellSize) * data.cellSize, (float) Math.Floor(min.z / data.cellSize) * data.cellSize);
            int peak = 0;
            for (int i = globals; i < lights.Count; i++)
            {
                var p = lights[i].position;
                float radius = lights[i].gridRadius;
                var start = p - new Vector3(radius, radius, radius) - data.origin;
                var end = p + new Vector3(radius, radius, radius) - data.origin;
                var bounds = new LightRenderer.CellBounds
                {
                    x0 = ClampCell(start.x, data.cellSize),
                    x1 = ClampCell(end.x, data.cellSize),
                    y0 = ClampCell(start.y, data.cellSize),
                    y1 = ClampCell(end.y, data.cellSize),
                    z0 = ClampCell(start.z, data.cellSize),
                    z1 = ClampCell(end.z, data.cellSize)
                };
                data.bounds[i] = bounds;
                for (int z = bounds.z0; z <= bounds.z1; z++) for (int y = bounds.y0; y <= bounds.y1; y++) for (int x = bounds.x0; x <= bounds.x1; x++)
                {
                    if (!Intersects(lights[i], data, x, y, z)) continue;
                    int cell = x + NativeLighting.GridDimension * (y + NativeLighting.GridDimension * z);
                    uint count = ++data.counts[cell];
                    if (count > peak) peak = (int) count;
                }
            }
            int capacity = LightValues.GrowCapacity(peak, NativeLighting.NativeCellCapacity);
            if (capacity > data.cellCapacity)
            {
                data.cellCapacity = capacity;
                data.indices = new uint[checked(data.counts.Length * capacity)];
            }
            for (int i = globals; i < lights.Count; i++)
            {
                var bounds = data.bounds[i];
                for (int z = bounds.z0; z <= bounds.z1; z++) for (int y = bounds.y0; y <= bounds.y1; y++) for (int x = bounds.x0; x <= bounds.x1; x++)
                {
                    if (!Intersects(lights[i], data, x, y, z)) continue;
                    int cell = x + NativeLighting.GridDimension * (y + NativeLighting.GridDimension * z);
                    data.indices[cell * data.cellCapacity + data.cursors[cell]++] = (uint) i;
                }
            }
            return data;
        }

        public static LightRenderer.GridData BuildView(LightRenderer.GridData source, int[] slots, LightRenderer.GridData reuse)
        {
            var data = reuse ?? new LightRenderer.GridData();
            data.origin = source.origin;
            data.cellSize = source.cellSize;
            int peak = 0;
            for (int cell = 0; cell < source.counts.Length; cell++)
            {
                int count = 0, offset = cell * source.cellCapacity;
                for (int i = 0; i < source.counts[cell]; i++) if (slots[source.indices[offset + i]] >= 0) count++;
                data.counts[cell] = (uint) count;
                peak = Math.Max(peak, count);
            }
            data.cellCapacity = LightValues.GrowCapacity(peak, NativeLighting.NativeCellCapacity);
            int size = checked(data.counts.Length * data.cellCapacity);
            if (data.indices.Length < size) data.indices = new uint[size];
            for (int cell = 0; cell < source.counts.Length; cell++)
            {
                int offset = cell * data.cellCapacity, count = 0;
                for (int i = 0; i < source.counts[cell]; i++)
                {
                    int slot = slots[source.indices[cell * source.cellCapacity + i]];
                    if (slot >= 0) data.indices[offset + count++] = (uint) slot;
                }
            }
            return data;
        }

        static int ClampCell(float position, float size)
        {
            return Math.Max(0, Math.Min(NativeLighting.GridDimension - 1, (int) Math.Floor(position / size)));
        }

        static bool Intersects(LightRenderer.RenderLight light, LightRenderer.GridData data, int x, int y, int z)
        {
            var min = data.origin + new Vector3(x * data.cellSize, y * data.cellSize, z * data.cellSize);
            var p = light.position;
            float dx = Math.Max(0, Math.Max(min.x - p.x, p.x - min.x - data.cellSize)), dy = Math.Max(0, Math.Max(min.y - p.y, p.y - min.y - data.cellSize)), dz = Math.Max(0, Math.Max(min.z - p.z, p.z - min.z - data.cellSize));
            return dx * dx + dy * dy + dz * dz <= light.gridRadiusSquared + 0.0001f;
        }
    }
}
