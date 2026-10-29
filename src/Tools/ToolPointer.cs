using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace phosphorite
{
    public static class ToolPointer
    {
        static GameObject gunRoot, gunPointer;
        static LineRenderer gunLine;
        static Renderer gunPointerRenderer;
        static Material gunMaterial;
        static bool hadSurface;
        internal static bool Exists
        {
            get
            {
                return gunRoot != null;
            }
        }

        static void CreatePointer()
        {
            if (gunRoot != null && gunLine != null && gunPointer != null && gunMaterial != null) return;
            Dispose();
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) throw new InvalidOperationException("No unlit shader is available for the Light Spawner pointer.");
            gunMaterial = new Material(shader);
            gunMaterial.name = "phosphorite_light_gun_material";
            gunMaterial.hideFlags = HideFlags.HideAndDontSave;
            gunRoot = new GameObject("phosphorite_light_gun");
            gunRoot.SetActive(false);
            gunRoot.layer = 2;
            gunRoot.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(gunRoot);
            gunLine = gunRoot.AddComponent<LineRenderer>();
            gunLine.positionCount = 2;
            gunLine.useWorldSpace = true;
            gunLine.sharedMaterial = gunMaterial;
            gunLine.shadowCastingMode = ShadowCastingMode.Off;
            gunLine.receiveShadows = false;
            gunPointer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gunPointer.name = "phosphorite_light_gun_pointer";
            gunPointer.transform.SetParent(gunRoot.transform, false);
            gunPointer.layer = 2;
            gunPointer.hideFlags = HideFlags.HideAndDontSave;
            var collider = gunPointer.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                UnityEngine.Object.Destroy(collider);
            }
            gunPointerRenderer = gunPointer.GetComponent<Renderer>();
            gunPointerRenderer.sharedMaterial = gunMaterial;
            gunPointerRenderer.shadowCastingMode = ShadowCastingMode.Off;
            gunPointerRenderer.receiveShadows = false;
        }

        internal static void ShowPointer(Vector3 origin, Vector3 end, bool showMarker, Color color, float scale, bool surface)
        {
            CreatePointer();
            if (gunRoot.activeSelf && surface == hadSurface)
                end = Vector3.Lerp(gunPointer.transform.position, end, 1f - Mathf.Exp(-26.5f * Time.unscaledDeltaTime));
            hadSurface = surface;
            gunMaterial.color = color;
            gunLine.startColor = Color.white;
            gunLine.endColor = Color.white;
            gunLine.startWidth = 0.004f * scale;
            gunLine.endWidth = 0.004f * scale;
            gunLine.SetPosition(0, origin);
            gunLine.SetPosition(1, end);
            gunPointer.transform.position = end;
            gunPointer.transform.localScale = Vector3.one * (0.06f * scale);
            gunPointerRenderer.enabled = showMarker;
            gunRoot.SetActive(true);
        }

        internal static void HidePointer()
        {
            if (gunRoot != null) gunRoot.SetActive(false);
        }

        public static void Dispose()
        {
            if (gunRoot != null)
            {
                gunRoot.SetActive(false);
                UnityEngine.Object.Destroy(gunRoot);
            }
            if (gunMaterial != null) UnityEngine.Object.Destroy(gunMaterial);
            gunRoot = null;
            gunPointer = null;
            gunLine = null;
            gunPointerRenderer = null;
            gunMaterial = null;
        }

        internal static bool Contains(Transform target)
        {
            return gunRoot != null && target.IsChildOf(gunRoot.transform);
        }
    }
}
