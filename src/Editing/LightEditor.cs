using System;
using System.Collections.Generic;
using UnityEngine;
using BananaOS;

namespace phosphorite
{
    public static class LightEditor
    {
        static GameLight selectedLight;
        static readonly Dictionary<GameLight, float> offIntensity = new Dictionary<GameLight, float>();
        static string editorStatus = "";
        public static string Status
        {
            get
            {
                return editorStatus;
            }
        }

        public static int LightCount
        {
            get
            {
                int count = 0;
                foreach (var light in LightingManager.lights) if (IsMapLight(light)) count++;
                return count;
            }
        }

        public static int SelectedIndex
        {
            get
            {
                var selected = GetSelectedLight();
                if (selected == null) return -1;
                int index = 0;
                foreach (var light in LightingManager.lights) if (IsMapLight(light))
                {
                    if (light == selected) return index;
                    index++;
                }
                return -1;
            }
        }

        public static bool HasSelection
        {
            get
            {
                return GetSelectedLight() != null;
            }
        }

        public static bool SelectedEnabled
        {
            get
            {
                var light = GetSelectedLight();
                return light != null && LightingManager.GetIntensity(light) != 0;
            }
        }

        public static float Intensity
        {
            get
            {
                var light = GetSelectedLight();
                if (light == null) return 1f;
                float value = LightingManager.GetIntensity(light), saved;
                return value != 0 ? value : offIntensity.TryGetValue(light, out saved) ? saved : 1f;
            }
        }

        public static Color LightColor
        {
            get
            {
                var light = GetSelectedLight();
                return light == null ? Color.white : light.light.color;
            }
        }

        internal static GameLight Selection
        {
            get
            {
                return selectedLight;
            }
        }

        internal static bool IsEditable(GameLight light)
        {
            return IsMapLight(light) && LightingManager.lights.Contains(light);
        }

        internal static bool IsMapLight(GameLight light)
        {
            return light != null && light.light != null && MapLighting.IsVisible(light);
        }

        internal static void RefreshScreen()
        {
            if (ReferenceEquals(Plugin.Host, null)) return;
            var watch = MonkeWatch.Instance;
            if (watch != null) watch.UpdateScreen();
        }

        internal static GameLight GetSelectedLight()
        {
            if (LightEditorTool.Mode == LightEditorTool.EditorMode.Move && IsEditable(LightEditorTool.MoveSource))
            {
                selectedLight = LightEditorTool.MoveSource;
                return LightEditorTool.MoveSource;
            }
            if (IsEditable(selectedLight)) return selectedLight;
            selectedLight = null;
            Vector3 center = Vector3.zero;
            bool located = !ReferenceEquals(Plugin.Host, null) && GameAccess.TryGetPlayerPosition(out center);
            float best = float.PositiveInfinity;
            foreach (var light in LightingManager.lights) if (IsMapLight(light))
            {
                float distance = located ? (light.transform.position - center).sqrMagnitude : 0;
                if (selectedLight == null || distance < best)
                {
                    selectedLight = light;
                    best = distance;
                }
            }
            return selectedLight;
        }

        internal static void SelectLight(GameLight light)
        {
            if (!IsEditable(light)) return;
            selectedLight = light;
            editorStatus = "";
            RefreshScreen();
        }

        public static void CycleLight(int direction)
        {
            if (LightEditorTool.Mode == LightEditorTool.EditorMode.Move) LightEditorTool.CancelMove();
            var current = GetSelectedLight();
            int count = LightingManager.lights.Count;
            if (count == 0) return;
            int index = current == null ? (direction < 0 ? 0 : -1) : LightingManager.lights.IndexOf(current);
            for (int i = 0; i < count; i++)
            {
                index = (index + (direction < 0 ? -1 : 1) + count) % count;
                var light = LightingManager.lights[index];
                if (IsMapLight(light))
                {
                    SelectLight(light);
                    return;
                }
            }
        }

        internal static void SaveChange()
        {
            editorStatus = "";
            if (!ReferenceEquals(Plugin.Host, null) && ModSettings.AutoSave && !LightingManager.SaveLights()) editorStatus = "Save failed";
        }

        public static void SetSelectedEnabled(bool enabled)
        {
            var light = GetSelectedLight();
            if (light == null || enabled == SelectedEnabled) return;
            if (enabled) LightingManager.SetIntensity(light, Intensity);
            else
            {
                offIntensity[light] = LightingManager.GetIntensity(light);
                LightingManager.SetIntensity(light, 0);
            }
            SaveChange();
        }

        public static void ChangeIntensity(int direction)
        {
            var light = GetSelectedLight();
            if (light == null) return;
            float value = LightValues.StepSignedIntensity(Intensity, direction);
            if (SelectedEnabled) LightingManager.SetIntensity(light, value);
            else offIntensity[light] = value;
            SaveChange();
        }

        public static void ChangeChannel(int channel, int direction)
        {
            var light = GetSelectedLight();
            if (light == null) return;
            LightingManager.SetColor(light, LightValues.StepColor(light.light.color, channel, direction));
            SaveChange();
        }

        public static void DeleteSelected()
        {
            if (!LightEditorTool.IsPageVisible() || !IsEditable(selectedLight)) return;
            var light = selectedLight;
            if (LightEditorTool.Mode == LightEditorTool.EditorMode.Move) LightEditorTool.CancelMove();
            offIntensity.Remove(light);
            LightingManager.RemoveLight(light);
            ResetSelection(false);
            SaveChange();
            RefreshScreen();
        }

        internal static void ClearSelection()
        {
            ResetSelection(true);
        }

        internal static void ResetSelection(bool forgetIntensity)
        {
            selectedLight = null;
            LightEditorTool.ClearTarget();
            if (forgetIntensity) offIntensity.Clear();
        }

        public static void Dispose()
        {
            selectedLight = null;
            offIntensity.Clear();
            LightEditorTool.Dispose();
            LightHandles.Dispose();
        }

        internal static void SetSelection(GameLight light)
        {
            selectedLight = light;
        }

        internal static void OnLightsCleared()
        {
            selectedLight = null;
            LightEditorTool.ClearTarget();
            offIntensity.Clear();
        }

        internal static void ClearStatus()
        {
            editorStatus = "";
        }
    }
}
