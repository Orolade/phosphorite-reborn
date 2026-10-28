using System;
using UnityEngine;
using GorillaLocomotion;
using BananaOS;

namespace phosphorite
{
    public static class LightSpawner
    {
        static string status = "";
        static float flashUntil, retryAfter;
        static readonly TriggerPress press = new TriggerPress();
        static bool skipFrame, pageActive;
        public static bool IsActive
        {
            get
            {
                var watch = BananaOS.MonkeWatch.Instance;
                return watch != null && watch.displayingPage is LightSpawnerPage;
            }
        }

        public static float Intensity { get { return LightSpawnerSettings.Intensity; } }
        public static Color LightColor { get { return LightSpawnerSettings.Color; } }

        public static Color DisplayColor
        {
            get
            {
                return LightColor;
            }
        }

        static void PlaceLight(Vector3 position)
        {
            var light = LightingManager.AddLight(position, Intensity, LightColor);
            if (light == null)
            {
                status = "Could not create the light.";
                return;
            }
            flashUntil = Time.unscaledTime + 0.15f;
            status = "Placed " + position.ToString("F1");
            if (ModSettings.AutoSave)
            {
                if (LightingManager.SaveLights()) status += "; saved.";
                else
                {
                    status += "; save failed.";
                    Plugin.Log("Light Spawner: SaveLights failed.", true);
                }
            }
            Plugin.Log("Light Spawner: " + status, false);
        }

        public static void Update()
        {
            bool active = SyncPage();
            if (skipFrame)
            {
                skipFrame = false;
                ToolPointer.HidePointer();
                press.Reset();
                return;
            }
            if (!active || Plugin.Host == null)
            {
                if (LightEditorTool.Mode == LightEditorTool.EditorMode.Off) ToolPointer.HidePointer();
                press.Reset();
                return;
            }
            if (Time.unscaledTime < retryAfter)
            {
                ToolPointer.HidePointer();
                press.Reset();
                return;
            }
            try
            {
                var player = GTPlayer.Instance;
                var input = ControllerInputPoller.instance;
                if (player == null || input == null || !input.RightHandValid || GameLightingManager.instance == null || LightingManager.Runner == null)
                {
                    ToolPointer.HidePointer();
                    press.Reset();
                    status = "Enter a map with the right controller active.";
                    return;
                }
                bool aiming = input.rightGrab || input.rightControllerGripFloat > 0.5f;
                float trigger = Math.Max(input.rightControllerIndexFloat, input.rightControllerTriggerButton ? 1f : 0f);
                if (!aiming || PortableLights.IsHandOccupied(true))
                {
                    press.Step(false, trigger, false);
                    ToolPointer.HidePointer();
                    return;
                }
                var hand = player.GetControllerTransform(false);
                if (hand == null)
                {
                    press.Reset();
                    ToolPointer.HidePointer();
                    return;
                }
                Vector3 origin = hand.position, direction = -hand.up;
                float scale = player.scale;
                if (!LightValues.IsFinite(scale) || scale <= 0) scale = 1;
                if (!LightValues.IsFinite(origin.x) || !LightValues.IsFinite(origin.y) || !LightValues.IsFinite(origin.z) || !LightValues.IsFinite(direction.x) || !LightValues.IsFinite(direction.y) || !LightValues.IsFinite(direction.z))
                {
                    ToolPointer.HidePointer();
                    press.Reset();
                    return;
                }
                RaycastHit hit;
                bool target = SurfaceRaycaster.Raycast(new Ray(origin, direction), out hit), ready = LightValues.IsValidIntensity(Intensity);
                Vector3 end = target ? SurfaceRaycaster.SurfacePosition(hit.point, hit.normal, scale) : origin + direction * SurfaceRaycaster.GunMaxDistance;
                Color pointerColor = !target ? new Color(0.45f, 0.45f, 0.45f, 1) : !ready ? Color.red : Time.unscaledTime < flashUntil ? Color.green : LightColor;
                ToolPointer.ShowPointer(origin, end, target, pointerColor, scale);
                if (press.Step(true, trigger, target && ready)) PlaceLight(end);
            }
            catch (Exception e)
            {
                ToolPointer.HidePointer();
                press.Reset();
                retryAfter = Time.unscaledTime + 2f;
                status = "Light Spawner error; see LogOutput.log.";
                Plugin.Log("Light Spawner: " + e, true);
            }
        }

        public static bool SyncPage()
        {
            bool active = IsActive;
            if (active != pageActive)
            {
                pageActive = active;
                press.Reset();
                status = "";
                if (!active) ToolPointer.HidePointer();
            }
            return active;
        }

        public static void SetIntensity(float intensity)
        {
            LightSpawnerSettings.SetIntensity(intensity);
        }

        public static void SetColor(Color color)
        {
            LightSpawnerSettings.SetColor(color);
        }

        public static void ChangeChannel(int channel, int direction)
        {
            var color = DisplayColor;
            if (channel == 0) color.r = LightValues.StepChannel(LightValues.ToLevel(color.r), direction) / 9f;
            else if (channel == 1) color.g = LightValues.StepChannel(LightValues.ToLevel(color.g), direction) / 9f;
            else color.b = LightValues.StepChannel(LightValues.ToLevel(color.b), direction) / 9f;
            SetColor(color);
        }

        internal static void SuppressInput()
        {
            press.Reset();
            skipFrame = true;
        }
    }
}
