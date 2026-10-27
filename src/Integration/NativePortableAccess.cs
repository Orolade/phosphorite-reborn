using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR;

namespace phosphorite
{
    public static class NativePortableAccess
    {
        delegate void TestBounds(Vector3 position, Transform frame, Vector3 segment, Bounds bounds, float speedSlop, float reach, GameEntity entity, bool alternate, ref float distance, ref GameEntity best, ref Vector3 point, ref bool usedAlternate);
        static readonly FieldInfo charge = typeof(HatcheryEventFlashlight).GetField("currentEnergy", GameAccess.InstanceFields);
        static readonly FieldInfo chargeTime = typeof(HatcheryEventFlashlight).GetField("lastUpdated", GameAccess.InstanceFields);
        static readonly FieldInfo brightness = typeof(HatcheryEventFlashlight).GetField("startingBrightness", GameAccess.InstanceFields);
        static readonly TestBounds testBounds = (TestBounds) Delegate.CreateDelegate(typeof(TestBounds), typeof(GameEntityManager).GetMethod("_TryGrabLocal_TestBounds", BindingFlags.Static | BindingFlags.NonPublic));

        internal static void RefillFlashlight(HatcheryEventFlashlight light)
        {
            charge.SetValue(light, 10f);
            chargeTime.SetValue(light, Time.time);
        }

        internal static void CopyFlashlightBrightness(HatcheryEventFlashlight source, Transform[] lights)
        {
            float value = (float) brightness.GetValue(source);
            if (!LightValues.IsFinite(value) || value <= 0) value = lights[0].GetComponent<Light>().intensity;
            foreach (var light in lights) light.GetComponent<Light>().intensity = value;
        }

        internal static bool HandValid(bool right)
        {
            var input = ControllerInputPoller.instance;
            return input != null && (right ? input.RightHandValid : input.LeftHandValid);
        }

        internal static Transform Hand(bool right)
        {
            if (!HandValid(right)) return null;
            var local = GamePlayerLocal.instance;
            if (local != null && local.gamePlayer != null) return local.gamePlayer.GetHandTransform(right ? GamePlayer.RIGHT_HAND : GamePlayer.LEFT_HAND);
            var rig = GorillaTagger.Instance == null ? null : GorillaTagger.Instance.offlineVRRig;
            return rig == null ? null : right ? rig.rightHandTransform : rig.leftHandTransform;
        }

        internal static bool Grip(bool right)
        {
            var interactor = EquipmentInteractor.instance;
            return HandValid(right) && interactor != null && (right ? interactor.isRightGrabbing : interactor.isLeftGrabbing) && ControllerInputPoller.GetGrab(right ? XRNode.RightHand : XRNode.LeftHand);
        }

        internal static bool Trigger(bool right)
        {
            var rig = GorillaTagger.Instance == null ? null : GorillaTagger.Instance.offlineVRRig;
            return HandValid(right) && rig != null && (right ? rig.rightIndex.calcT : rig.leftIndex.calcT) > 0.25f;
        }

        internal static bool HandFree(bool right)
        {
            if (PortableLights.IsHandOccupied(right)) return false;
            var local = GamePlayerLocal.instance;
            if (local != null && local.gamePlayer != null && (local.gamePlayer.IsGrabbingDisabled() || local.gamePlayer.GetGrabbedGameEntity(right ? GamePlayer.RIGHT_HAND : GamePlayer.LEFT_HAND) != null)) return false;
            var interactor = EquipmentInteractor.instance;
            return interactor == null || !(right ? interactor.disableRightGrab || interactor.rightHandHeldEquipment != null : interactor.disableLeftGrab || interactor.leftHandHeldEquipment != null);
        }

        internal static bool CanReach(GameEntity entity, MeshFilter[] meshes, Vector3 position, out Vector3 point)
        {
            float distance = float.MaxValue;
            GameEntity best = null;
            point = position;
            bool alternate = false;
            float reach = Math.Max(0.03f, entity.pickupRangeFromSurface);
            foreach (var mesh in meshes)
            {
                if (mesh == null || mesh.sharedMesh == null || !mesh.gameObject.activeInHierarchy) continue;
                testBounds(position, mesh.transform, Vector3.zero, mesh.sharedMesh.bounds, 0, reach, entity, false, ref distance, ref best, ref point, ref alternate);
            }
            return best == entity && distance <= reach;
        }

        internal static Vector3 ReleaseVelocity(bool right, bool angular)
        {
            var local = GamePlayerLocal.instance;
            if (local == null) return Vector3.zero;
            int hand = right ? GamePlayer.RIGHT_HAND : GamePlayer.LEFT_HAND;
            var velocity = angular ? local.GetHandAngularVelocity(hand) : local.GetHandVelocity(hand);
            return LightValues.IsFinitePoint(velocity) ? velocity : Vector3.zero;
        }
    }
}
