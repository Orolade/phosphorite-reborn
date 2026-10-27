using System;
using System.Collections.Generic;
using UnityEngine;

namespace phosphorite
{
    public sealed class PortableLightTemplate : IDisposable
    {
        readonly GameObject root;
        readonly bool flashlight;

        PortableLightTemplate(string name, bool flashlight)
        {
            this.flashlight = flashlight;
            root = new GameObject("phosphorite_" + name + "_template");
            root.SetActive(false);
            root.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(root);
        }

        internal static bool CanCopy(HatcheryEventFlashlight source)
        {
            if (source == null || source.flashlight == null || source.lightStart == null || source.lightsParent == null || source.lights == null || source.lights.Length == 0) return false;
            var rig = source.GetComponentInParent<VRRig>(true);
            if (rig == null || rig.rightHandTransform == null) return false;
            var scale = rig.rightHandTransform.lossyScale;
            if (!LightValues.IsFinitePoint(scale) || scale.x == 0 || scale.y == 0 || scale.z == 0) return false;
            if (rig.rightHandTransform.IsChildOf(source.flashlight) || rig.rightHandTransform.IsChildOf(source.lightsParent) || rig.rightHandTransform.IsChildOf(source.lightStart)) return false;
            foreach (var light in source.lights) if (light == null || light.GetComponent<GameLight>() == null || light.GetComponent<Light>() == null) return false;
            return source.flashlight.GetComponentsInChildren<Renderer>(true).Length > 0;
        }

        internal static bool CanCopy(GRToolLantern source)
        {
            if (source == null || source.gameEntity == null || source.tool == null || source.gameLight == null || source.attributes == null) return false;
            var frame = source.gameEntity.transform;
            if (source.gameLight.gameObject == frame.gameObject || frame.GetComponentsInChildren<GameEntity>(true).Length != 1 || frame.GetComponentsInChildren<GRTool>(true).Length != 1) return false;
            if (source.tool.gameEntity != source.gameEntity || source.tool.attributes == null || source.gameLight.light == null || frame.GetComponent<Rigidbody>() == null) return false;
            if (!source.transform.IsChildOf(frame) || !source.gameLight.transform.IsChildOf(frame) || !source.attributes.transform.IsChildOf(frame) || !source.tool.transform.IsChildOf(frame)) return false;
            if (source.tool.upgradeSlots == null) return false;
            foreach (var slot in source.tool.upgradeSlots) if (slot.installedItem != null) return false;
            return frame.GetComponentsInChildren<Renderer>(true).Length > 0 && frame.GetComponent<GameGrabbable>() != null;
        }

        internal static PortableLightTemplate FromFlashlight(HatcheryEventFlashlight source)
        {
            var template = new PortableLightTemplate("flashlight", true);
            try
            {
                var frame = source.GetComponentInParent<VRRig>(true).rightHandTransform;
                var copies = new Dictionary<Transform, Transform>();
                var branches = new List<Transform> { source.flashlight, source.lightsParent, source.lightStart };
                foreach (var light in source.lights) branches.Add(light);
                foreach (var branch in branches)
                {
                    bool nested = false;
                    foreach (var other in branches) if (other != branch && branch.IsChildOf(other)) { nested = true; break; }
                    if (nested || copies.ContainsKey(branch)) continue;
                    var clone = UnityEngine.Object.Instantiate(branch.gameObject, template.root.transform, false);
                    clone.transform.localPosition = frame.InverseTransformPoint(branch.position);
                    clone.transform.localRotation = Quaternion.Inverse(frame.rotation) * branch.rotation;
                    clone.transform.localScale = Divide(branch.lossyScale, frame.lossyScale);
                    MapBranch(branch, clone.transform, copies);
                    Strip(clone, false);
                }
                var item = template.root.AddComponent<HatcheryEventFlashlight>();
                item.flashlight = copies[source.flashlight];
                item.lightsParent = copies[source.lightsParent];
                item.lightStart = copies[source.lightStart];
                item.lights = new Transform[source.lights.Length];
                for (int i = 0; i < item.lights.Length; i++) item.lights[i] = copies[source.lights[i]];
                item.clickSource = template.root.AddComponent<AudioSource>();
                if (source.clickSource != null)
                {
                    item.clickSource.clip = source.clickSource.clip;
                    item.clickSource.volume = source.clickSource.volume;
                    item.clickSource.pitch = source.clickSource.pitch;
                    item.clickSource.spatialBlend = source.clickSource.spatialBlend;
                    item.clickSource.outputAudioMixerGroup = source.clickSource.outputAudioMixerGroup;
                }
                item.clickSource.playOnAwake = false;
                item.flashlight.gameObject.SetActive(true);
                NativePortableAccess.CopyFlashlightBrightness(source, item.lights);
                PrepareLights(template.root);
                return template;
            }
            catch { template.Dispose(); throw; }
        }

        internal static PortableLightTemplate FromLantern(GRToolLantern source)
        {
            var template = new PortableLightTemplate("lantern", false);
            try
            {
                var clone = UnityEngine.Object.Instantiate(source.gameEntity.gameObject, template.root.transform, false);
                clone.SetActive(false);
                clone.transform.localPosition = Vector3.zero;
                clone.transform.localRotation = Quaternion.identity;
                clone.transform.localScale = Vector3.one;
                Strip(clone, true);
                var item = clone.GetComponentInChildren<GRToolLantern>(true);
                if (item == null || item.gameEntity == source.gameEntity || item.tool == source.tool || item.attributes == source.attributes || item.gameLight == source.gameLight)
                    throw new InvalidOperationException("Lantern references leave its prefab");
                item.gameEntity.manager = null;
                item.gameEntity.builtInEntities = new List<GameEntity>();
                item.gameEntity.gravityController = null;
                var body = item.gameEntity.GetComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = true;
                PrepareLights(clone);
                clone.SetActive(true);
                return template;
            }
            catch { template.Dispose(); throw; }
        }

        static Vector3 Divide(Vector3 value, Vector3 scale)
        {
            if (!LightValues.IsFinitePoint(scale) || scale.x == 0 || scale.y == 0 || scale.z == 0) throw new InvalidOperationException("Uninitialized item transform");
            return new Vector3(value.x / scale.x, value.y / scale.y, value.z / scale.z);
        }

        static void MapBranch(Transform source, Transform clone, Dictionary<Transform, Transform> copies)
        {
            copies.Add(source, clone);
            for (int i = 0; i < source.childCount; i++) MapBranch(source.GetChild(i), clone.GetChild(i), copies);
        }

        static bool Keep(Component component, bool lantern)
        {
            if (component is Transform || component is MeshFilter || component is MeshRenderer || component is SkinnedMeshRenderer || component is Light || component is GameLight || component is AudioSource) return true;
            return lantern && (component is GameEntity || component is GameGrabbable || component is GRTool || component is GRToolLantern || component is GRAttributes || component is GRMeterEnergy || component is Rigidbody || component is Collider);
        }

        static void Strip(GameObject clone, bool lantern)
        {
            foreach (var component in clone.GetComponentsInChildren<Component>(true))
                if (component != null && !Keep(component, lantern)) UnityEngine.Object.DestroyImmediate(component);
            foreach (var audio in clone.GetComponentsInChildren<AudioSource>(true)) audio.playOnAwake = false;
        }

        static void PrepareLights(GameObject clone)
        {
            foreach (var light in clone.GetComponentsInChildren<GameLight>(true)) light.enabled = false;
        }

        internal PortableLightInstance CreateInstance()
        {
            PortableLightHooks.Initialize();
            var instance = UnityEngine.Object.Instantiate(root);
            instance.name = flashlight ? "phosphorite_local_flashlight" : "phosphorite_local_lantern";
            instance.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(instance);
            try { return new PortableLightInstance(instance, flashlight); }
            catch { UnityEngine.Object.Destroy(instance); throw; }
        }

        public void Dispose()
        {
            if (root != null) UnityEngine.Object.Destroy(root);
        }
    }
}
