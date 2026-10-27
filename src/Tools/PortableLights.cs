using System;
using UnityEngine;

namespace phosphorite
{
    public static class PortableLights
    {
        static PortableLightInstance flashlight, lantern;
        static bool lanternRight;
        static string status = "";

        public static string Status { get { return status; } }
        public static bool FlashlightEnabled { get { return flashlight != null; } }
        public static bool LanternEnabled { get { return lantern != null; } }
        public static bool LanternRight { get { return lanternRight; } }

        public static bool IsHandOccupied(bool right)
        {
            return flashlight != null && flashlight.IsHeld(right) || lantern != null && lantern.IsHeld(right);
        }

        public static void SetLanternHand(bool right)
        {
            lanternRight = right;
        }

        public static void RecallLantern()
        {
            status = "";
            if (lantern != null) lantern.Recall(lanternRight);
        }

        public static void SetEnabled(bool isFlashlight, bool enabled)
        {
            status = "";
            var item = isFlashlight ? flashlight : lantern;
            if ((item != null) == enabled) return;
            try
            {
                if (enabled)
                {
                    item = PortableLightAssets.Create(isFlashlight);
                    if (item == null)
                    {
                        status = isFlashlight ? "Flashlight model is not loaded" : "Load Ghost Reactor to cache the lantern";
                        return;
                    }
                }
                else
                {
                    item.Dispose();
                    item = null;
                }
                if (isFlashlight) flashlight = item;
                else
                {
                    lantern = item;
                    if (item != null) item.Recall(lanternRight);
                }
            }
            catch (Exception e)
            {
                status = "Could not create the local item";
                Plugin.Log("Portable light: " + e, true);
            }
        }

        public static void Update()
        {
            if (flashlight != null) flashlight.Update();
            if (lantern != null) lantern.Update();
        }

        public static void Dispose()
        {
            if (flashlight != null) flashlight.Dispose();
            if (lantern != null) lantern.Dispose();
            flashlight = null;
            lantern = null;
            PortableLightAssets.Dispose();
            PortableLightHooks.Dispose();
        }
    }
}
