using System;
using UnityEngine;
using GorillaLocomotion;
using BananaOS;

namespace phosphorite
{
    public static class LightEditorTool
    {
        static EditorMode lightTool;
        static GameLight moveSource;
        static bool moveAimed;
        static readonly TriggerPress toolPress = new TriggerPress();
        static bool toolSkipFrame;
        public enum EditorMode
        {
            Off,
            Select,
            Move
        }

        public static EditorMode Mode
        {
            get
            {
                return lightTool;
            }
        }

        public static bool IsMoving
        {
            get
            {
                return lightTool == EditorMode.Move && IsSelectedPage() && LightEditor.IsEditable(moveSource);
            }
        }

        internal static GameLight MoveSource
        {
            get
            {
                return moveSource;
            }
        }

        static bool IsSelectedPage()
        {
            var watch = MonkeWatch.Instance;
            return watch != null && watch.displayingPage is LightEditorPage;
        }

        internal static bool IsPageVisible()
        {
            var watch = MonkeWatch.Instance;
            return IsSelectedPage() && watch.watchActive;
        }

        public static bool SyncPage()
        {
            bool open = IsSelectedPage();
            if (lightTool == EditorMode.Move)
            {
                if (open && LightEditor.IsEditable(moveSource)) return open;
                CancelMove();
            }
            if (open && lightTool != EditorMode.Select) SetMode(EditorMode.Select);
            else if (!open && lightTool == EditorMode.Select) SetMode(EditorMode.Off);
            return open;
        }

        public static void SetMode(EditorMode mode)
        {
            if (mode == EditorMode.Select && !IsSelectedPage()) mode = EditorMode.Off;
            if (mode == EditorMode.Move && (!IsSelectedPage() || !LightEditor.IsEditable(moveSource))) mode = EditorMode.Off;
            if (mode != EditorMode.Move)
            {
                moveSource = null;
                moveAimed = false;
            }
            lightTool = mode;
            toolPress.Reset();
            toolSkipFrame = true;
            if (!LightSpawner.IsActive && ToolPointer.Exists) ToolPointer.HidePointer();
        }

        public static bool BeginMove()
        {
            if (!IsPageVisible() || !LightEditor.IsEditable(LightEditor.Selection)) return false;
            moveSource = LightEditor.Selection;
            moveAimed = false;
            SetMode(EditorMode.Move);
            LightEditor.ClearStatus();
            return true;
        }

        public static void ToggleMove()
        {
            if (lightTool == EditorMode.Move) CancelMove();
            else BeginMove();
        }

        static bool CommitMove(Vector3 destination)
        {
            if (!IsMoving)
            {
                CancelMove();
                return false;
            }
            var light = moveSource;
            CancelMove();
            light.transform.position = destination;
            LightRenderer.OnLightAdded(light);
            LightEditor.SetSelection(light);
            LightEditor.SaveChange();
            LightEditor.RefreshScreen();
            return true;
        }

        public static void CancelMove()
        {
            moveSource = null;
            moveAimed = false;
            SetMode(IsSelectedPage() ? EditorMode.Select : EditorMode.Off);
        }

        static bool HandleInput(bool aiming, float trigger, bool surface, GameLight hover, Vector3 destination)
        {
            if (lightTool == EditorMode.Off) return false;
            bool moving = lightTool == EditorMode.Move;
            if (moving)
            {
                if (!aiming && moveAimed)
                {
                    CancelMove();
                    LightEditor.RefreshScreen();
                    return false;
                }
                if (aiming) moveAimed = true;
            }
            if (toolSkipFrame)
            {
                toolSkipFrame = false;
                toolPress.Reset();
                return false;
            }
            bool valid = moving ? surface : hover != null;
            if (!toolPress.Step(aiming, trigger, valid)) return false;
            if (moving) return CommitMove(destination);
            if (!IsSelectedPage()) return false;
            LightEditor.SelectLight(hover);
            return true;
        }

        public static void Update()
        {
            bool pageOpen = SyncPage();
            var player = GTPlayer.Instance;
            float scale = player == null ? 1 : player.scale;
            float radius = LightHandles.Radius * scale;
            if (lightTool == EditorMode.Off) return;
            var input = ControllerInputPoller.instance;
            if (player == null || input == null || !input.RightHandValid || GameLightingManager.instance == null || LightingManager.Runner == null)
            {
                if (lightTool == EditorMode.Move) CancelMove();
                toolPress.Reset();
                ToolPointer.HidePointer();
                if (pageOpen) LightHandles.Draw(LightEditor.GetSelectedLight(), null, radius, false, Vector3.zero);
                return;
            }
            bool aiming = input.rightGrab || input.rightControllerGripFloat > 0.5f;
            float trigger = Math.Max(input.rightControllerIndexFloat, input.rightControllerTriggerButton ? 1f : 0f);
            if (!aiming || PortableLights.IsHandOccupied(true))
            {
                HandleInput(false, trigger, false, null, Vector3.zero);
                ToolPointer.HidePointer();
                LightHandles.Draw(LightEditor.GetSelectedLight(), null, radius, false, Vector3.zero);
                return;
            }
            var hand = player.GetControllerTransform(false);
            if (hand == null)
            {
                if (lightTool == EditorMode.Move) CancelMove();
                toolPress.Reset();
                ToolPointer.HidePointer();
                return;
            }
            var origin = hand.position;
            var direction = -hand.up;
            Ray ray = new Ray(origin, direction);
            RaycastHit hit;
            bool surface = SurfaceRaycaster.Raycast(ray, out hit);
            float maximum = surface ? Math.Min(SurfaceRaycaster.GunMaxDistance, hit.distance + 0.02f * scale) : SurfaceRaycaster.GunMaxDistance;
            bool moving = lightTool == EditorMode.Move;
            var hover = moving ? null : LightHandles.Pick(ray, radius, maximum);
            bool valid = moving ? surface : hover != null;
            Vector3 destination = surface ? SurfaceRaycaster.SurfacePosition(hit.point, hit.normal, scale) : origin + direction * SurfaceRaycaster.GunMaxDistance;
            Vector3 end = hover != null ? hover.transform.position : destination;
            ToolPointer.ShowPointer(origin, end, valid, Color.green, scale, surface: surface || hover != null);
            LightHandles.Draw(LightEditor.GetSelectedLight(), hover, radius, moving && surface, destination);
            HandleInput(true, trigger, surface, hover, destination);
        }

        internal static void SuppressInput()
        {
            toolPress.Reset();
            toolSkipFrame = true;
        }

        internal static void ClearTarget()
        {
            moveSource = null;
        }

        internal static void Dispose()
        {
            moveSource = null;
            lightTool = EditorMode.Off;
            toolPress.Reset();
        }
    }
}
