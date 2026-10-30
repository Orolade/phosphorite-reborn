using System;
using UnityEngine;
using GorillaLocomotion;
using BananaOS;

namespace phosphorite
{
    public static class LightSpawner
    {
        static float flashUntil;
        static readonly TriggerPress press = new TriggerPress();
        static bool skipFrame, pageActive, pickingLight, pickAimed;
        public static bool IsActive
        {
            get
            {
                var watch = MonkeWatch.Instance;
                return watch != null && watch.displayingPage is LightSpawnerPage;
            }
        }

        public static bool IsPicking { get { return pickingLight && IsActive; } }

        public static void TogglePick()
        {
            if (pickingLight)
            {
                CancelPick();
                return;
            }
            var watch = MonkeWatch.Instance;
            if (!SyncPage() || !watch.watchActive) return;
            pickingLight = true;
            pickAimed = false;
            flashUntil = 0;
            SuppressInput();
            watch.UpdateScreen();
        }

        public static void CancelPick()
        {
            if (!pickingLight) return;
            pickingLight = false;
            pickAimed = false;
            SuppressInput();
            ToolPointer.HidePointer();
            var watch = MonkeWatch.Instance;
            if (watch != null) watch.UpdateScreen();
        }

        static bool PickLight(GameLight light)
        {
            if (!LightEditor.IsEditable(light)) return false;
            float intensity = LightEditor.ReadIntensity(light);
            var color = light.light.color;
            LightSpawnerSettings.SetIntensity(intensity);
            LightSpawnerSettings.SetColor(color);
            CancelPick();
            return true;
        }

        static bool HandleInput(bool aiming, float trigger, bool surface, Vector3 destination, GameLight hover)
        {
            if (!IsActive) return false;
            if (pickingLight)
            {
                if (!aiming && pickAimed)
                {
                    CancelPick();
                    return false;
                }
                if (aiming) pickAimed = true;
            }
            if (skipFrame)
            {
                skipFrame = false;
                press.Reset();
                return false;
            }
            bool valid = pickingLight ? LightEditor.IsEditable(hover) : surface;
            if (!press.Step(aiming, trigger, valid)) return false;
            if (pickingLight) return PickLight(hover);
            PlaceLight(destination);
            return true;
        }

        static void PlaceLight(Vector3 position)
        {
            LightingManager.AddLight(position, LightSpawnerSettings.Intensity, LightSpawnerSettings.Color);
            flashUntil = Time.unscaledTime + 0.15f;
            if (ModSettings.AutoSave) LightingManager.SaveLights();
        }

        public static void Update()
        {
            bool active = SyncPage();
            if (!active)
            {
                if (LightEditorTool.Mode == LightEditorTool.EditorMode.Off) ToolPointer.HidePointer();
                press.Reset();
                return;
            }
            var player = GTPlayer.Instance;
            var input = ControllerInputPoller.instance;
            if (player == null || input == null || !input.RightHandValid || GameLightingManager.instance == null || LightingManager.Runner == null)
            {
                CancelPick();
                ToolPointer.HidePointer();
                press.Reset();
                return;
            }
            bool aiming = input.rightGrab || input.rightControllerGripFloat > 0.5f;
            float trigger = Math.Max(input.rightControllerIndexFloat, input.rightControllerTriggerButton ? 1f : 0f);
            float scale = player.scale;
            float radius = LightHandles.Radius * scale;
            if (!aiming || PortableLights.IsHandOccupied(true))
            {
                HandleInput(false, trigger, false, Vector3.zero, null);
                ToolPointer.HidePointer();
                if (IsPicking) LightHandles.Draw(null, null, radius, false, Vector3.zero);
                return;
            }
            var hand = player.GetControllerTransform(false);
            if (hand == null)
            {
                CancelPick();
                press.Reset();
                ToolPointer.HidePointer();
                return;
            }
            Vector3 origin = hand.position, direction = -hand.up;
            RaycastHit hit;
            var ray = new Ray(origin, direction);
            bool target = SurfaceRaycaster.Raycast(ray, out hit);
            float maximum = target ? Math.Min(SurfaceRaycaster.GunMaxDistance, hit.distance + 0.02f * scale) : SurfaceRaycaster.GunMaxDistance;
            var hover = pickingLight ? LightHandles.Pick(ray, radius, maximum) : null;
            Vector3 end = target ? SurfaceRaycaster.SurfacePosition(hit.point, hit.normal, scale) : origin + direction * SurfaceRaycaster.GunMaxDistance;
            Color pointerColor = !target ? new Color(0.45f, 0.45f, 0.45f, 1) : Time.unscaledTime < flashUntil ? Color.green : LightSpawnerSettings.Color;
            if (pickingLight)
            {
                if (hover != null) end = hover.transform.position;
                pointerColor = hover != null ? Color.green : new Color(0.45f, 0.45f, 0.45f, 1);
                LightHandles.Draw(null, hover, radius, false, Vector3.zero);
            }
            ToolPointer.ShowPointer(origin, end, pickingLight ? hover != null : target, pointerColor, scale, surface: target || hover != null);
            HandleInput(true, trigger, target, end, hover);
        }

        public static bool SyncPage()
        {
            bool active = IsActive;
            if (active != pageActive)
            {
                pageActive = active;
                if (!active) CancelPick();
                SuppressInput();
                if (!active) ToolPointer.HidePointer();
            }
            return active;
        }

        internal static void SuppressInput()
        {
            press.Reset();
            skipFrame = true;
        }
    }
}
