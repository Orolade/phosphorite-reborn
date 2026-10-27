using System;
using UnityEngine;

namespace phosphorite
{
    public sealed class PortableLightInstance : IDisposable
    {
        readonly GameObject root;
        readonly HatcheryEventFlashlight flashlight;
        readonly GRToolLantern lantern;
        readonly LocalLanternGrip grip;
        readonly GameLight[] lights;
        readonly bool[] registered;

        internal PortableLightInstance(GameObject root, bool isFlashlight)
        {
            this.root = root;
            lights = root.GetComponentsInChildren<GameLight>(true);
            registered = new bool[lights.Length];
            if (isFlashlight)
            {
                flashlight = root.GetComponent<HatcheryEventFlashlight>();
                PortableLightHooks.Register(flashlight);
            }
            else
            {
                lantern = root.GetComponentInChildren<GRToolLantern>(true);
                grip = new LocalLanternGrip(root, lantern);
                PortableLightHooks.Register(lantern, grip);
            }
        }

        internal bool IsHeld(bool right)
        {
            return flashlight != null ? right && root.activeInHierarchy : grip != null && grip.IsHeld && grip.RightHand == right;
        }

        internal void Recall(bool right)
        {
            if (grip != null) grip.Recall(right);
        }

        internal void Update()
        {
            if (root == null) return;
            if (flashlight != null)
            {
                var rig = GorillaTagger.Instance == null ? null : GorillaTagger.Instance.offlineVRRig;
                bool available = rig != null && rig.rightHandTransform != null && NativePortableAccess.HandValid(true);
                if (available && root.transform.parent != rig.rightHandTransform)
                {
                    root.transform.SetParent(rig.rightHandTransform, false);
                    root.transform.localPosition = Vector3.zero;
                    root.transform.localRotation = Quaternion.identity;
                    root.transform.localScale = Vector3.one;
                }
                if (root.activeSelf != available) root.SetActive(available);
            }
            else if (grip != null) grip.Update();
            UpdateLights();
        }

        void UpdateLights()
        {
            bool changed = false;
            for (int i = 0; i < lights.Length; i++)
            {
                var light = lights[i];
                bool visible = light != null && light.gameObject.activeInHierarchy;
                if (visible)
                {
                    light.cachedPosition = light.transform.position;
                    light.UpdateCachedLightColorAndIntensity();
                    if (!registered[i]) LightRenderer.RegisterTransient(light);
                    changed = true;
                }
                else if (registered[i]) LightRenderer.UnregisterTransient(light);
                registered[i] = visible;
            }
            if (changed) LightRenderer.OnLightChanged();
        }

        public void Dispose()
        {
            for (int i = 0; i < lights.Length; i++)
                if (registered[i]) { LightRenderer.UnregisterTransient(lights[i]); registered[i] = false; }
            if (grip != null) grip.Dispose();
            if (root != null) root.SetActive(false);
            if (flashlight != null) PortableLightHooks.Unregister(flashlight);
            if (lantern != null) PortableLightHooks.Unregister(lantern);
            if (root != null) UnityEngine.Object.Destroy(root);
        }
    }
}
