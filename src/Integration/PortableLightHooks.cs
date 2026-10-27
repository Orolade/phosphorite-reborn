using System;
using System.Collections.Generic;
using HarmonyLib;

namespace phosphorite
{
    public static class PortableLightHooks
    {
        static readonly HashSet<HatcheryEventFlashlight> flashlights = new HashSet<HatcheryEventFlashlight>();
        static readonly Dictionary<GRToolLantern, LocalLanternGrip> lanterns = new Dictionary<GRToolLantern, LocalLanternGrip>();
        static readonly Dictionary<GameEntity, LocalLanternGrip> entities = new Dictionary<GameEntity, LocalLanternGrip>();
        static readonly HashSet<GRTool> tools = new HashSet<GRTool>();
        static Harmony harmony;

        internal static bool Owns(HatcheryEventFlashlight item) { return item != null && flashlights.Contains(item); }
        internal static bool Owns(GRToolLantern item) { return item != null && lanterns.ContainsKey(item); }
        internal static void Register(HatcheryEventFlashlight item) { flashlights.Add(item); }
        internal static void Register(GRToolLantern item, LocalLanternGrip grip)
        {
            lanterns.Add(item, grip);
            entities.Add(item.gameEntity, grip);
            tools.Add(item.tool);
        }
        internal static void Unregister(HatcheryEventFlashlight item) { flashlights.Remove(item); }
        internal static void Unregister(GRToolLantern item)
        {
            lanterns.Remove(item);
            entities.Remove(item.gameEntity);
            tools.Remove(item.tool);
        }

        public static void FlashlightSlicePrefix(HatcheryEventFlashlight __instance)
        {
            if (flashlights.Contains(__instance)) NativePortableAccess.RefillFlashlight(__instance);
        }

        public static bool FlashlightEnergyPrefix(HatcheryEventFlashlight __instance, ref float __result)
        {
            if (!flashlights.Contains(__instance)) return true;
            __result = 10f;
            return false;
        }

        public static void LanternUpdatePrefix(GRToolLantern __instance)
        {
            if (!lanterns.ContainsKey(__instance)) return;
            int full = Math.Max(1, __instance.tool.GetEnergyMax());
            if (__instance.tool.energy == full) return;
            __instance.tool.energy = full;
            __instance.tool.RefreshMeters();
        }

        public static bool LanternHeldPrefix(GRToolLantern __instance, ref bool __result)
        {
            LocalLanternGrip grip;
            if (!lanterns.TryGetValue(__instance, out grip)) return true;
            __result = grip.IsHeld;
            return false;
        }

        public static bool LanternLastHeldPrefix(GRToolLantern __instance, ref bool __result)
        {
            if (!lanterns.ContainsKey(__instance)) return true;
            __result = true;
            return false;
        }

        public static bool LanternButtonPrefix(GRToolLantern __instance, ref bool __result)
        {
            LocalLanternGrip grip;
            if (!lanterns.TryGetValue(__instance, out grip)) return true;
            __result = grip.IsHeld && NativePortableAccess.Trigger(grip.RightHand);
            return false;
        }

        public static bool EntityStatePrefix(GameEntity __instance, long __1)
        {
            if (!entities.ContainsKey(__instance)) return true;
            __instance.SetState(__1);
            return false;
        }

        public static bool ToolLocalPrefix(GRTool __instance)
        {
            return !tools.Contains(__instance);
        }

        public static bool HapticPrefix(AbilityHaptic __instance, GameEntity __0)
        {
            LocalLanternGrip grip;
            if (__0 == null || !entities.TryGetValue(__0, out grip)) return true;
            var tagger = GorillaTagger.Instance;
            if (grip.IsHeld && tagger != null) tagger.StartVibration(!grip.RightHand, __instance.strength, __instance.duration);
            return false;
        }

        public static bool EmptyHandPrefix(int __1)
        {
            return !PortableLights.IsHandOccupied(__1 == GamePlayer.RIGHT_HAND);
        }

        static void Patch(Type type, string name, string prefix, Type[] parameters = null)
        {
            var method = parameters == null ? type.GetMethod(name, GameAccess.InstanceFields) : type.GetMethod(name, GameAccess.InstanceFields, null, parameters, null);
            harmony.Patch(method, prefix: new HarmonyMethod(typeof(PortableLightHooks), prefix));
        }

        internal static void Initialize()
        {
            if (harmony != null) return;
            try
            {
                harmony = new Harmony("com.brokenstone.gorillatag.phosphorite.portable");
                Patch(typeof(HatcheryEventFlashlight), "SliceUpdate", nameof(FlashlightSlicePrefix));
                Patch(typeof(HatcheryEventFlashlight), "MaxEnergy", nameof(FlashlightEnergyPrefix));
                Patch(typeof(GRToolLantern), "Update", nameof(LanternUpdatePrefix));
                Patch(typeof(GRToolLantern), "IsHeld", nameof(LanternHeldPrefix));
                Patch(typeof(GRToolLantern), "IsHeldLocal", nameof(LanternHeldPrefix));
                Patch(typeof(GRToolLantern), "WasLastHeldLocal", nameof(LanternLastHeldPrefix));
                Patch(typeof(GRToolLantern), "IsButtonHeld", nameof(LanternButtonPrefix));
                Patch(typeof(GameEntity), "RequestState", nameof(EntityStatePrefix), new[] { typeof(GameEntityId), typeof(long) });
                Patch(typeof(GRTool), "UseEnergy", nameof(ToolLocalPrefix));
                Patch(typeof(GRTool), "GrabbedByPlayer", nameof(ToolLocalPrefix));
                Patch(typeof(AbilityHaptic), "PlayIfHeldLocal", nameof(HapticPrefix));
                Patch(typeof(GamePlayerLocal), "UpdateHandEmpty", nameof(EmptyHandPrefix));
            }
            catch { Dispose(); throw; }
        }

        internal static void Dispose()
        {
            if (harmony != null) harmony.UnpatchSelf();
            harmony = null;
            flashlights.Clear();
            lanterns.Clear();
            entities.Clear();
            tools.Clear();
        }
    }
}
