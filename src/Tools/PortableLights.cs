using System;
using UnityEngine;

namespace phosphorite
{
    public static class PortableLights
    {
        static PortableLightInstance flashlight, lantern;
        static bool lanternRight;
        static string status = "";
        const string lanternRestricted = "Lantern is disabled in Ghost Reactor";

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
            if (LanternHandInReactor())
            {
                status = lanternRestricted;
                return;
            }
            if (lantern != null) lantern.Recall(lanternRight);
        }

        static bool PlayerInReactor()
        {
            var tagger = GorillaTagger.Instance;
            var rig = tagger == null ? null : tagger.offlineVRRig;
            return rig != null && rig.zoneEntity != null && MapLighting.Normalize(rig.zoneEntity.currentZone) == GTZone.ghostReactor;
        }

        static bool LanternHandInReactor()
        {
            if (PlayerInReactor()) return true;
            var hand = NativePortableAccess.Hand(lanternRight);
            return hand != null && MapLighting.IsGhostReactor(hand.position);
        }

        public static void SetEnabled(bool isFlashlight, bool enabled)
        {
            status = "";
            if (!isFlashlight && enabled && LanternHandInReactor())
            {
                status = lanternRestricted;
                return;
            }
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
            if (lantern != null)
            {
                lantern.Update();
                if (PlayerInReactor() || lantern.InGhostReactor)
                {
                    SetEnabled(false, false);
                    status = lanternRestricted;
                }
            }
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
