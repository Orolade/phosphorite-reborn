using UnityEngine;

namespace phosphorite
{
    public static class SceneLighting
    {
        public static void SetAmbient(Color color)
        {
            color = new Color(LightValues.ToByte(color.r) / 255f, LightValues.ToByte(color.g) / 255f, LightValues.ToByte(color.b) / 255f, 1);
            if (LightValues.SameAmbient(LightingManager.ambient, color)) return;
            LightingManager.SetAmbient(color);
            if (!ReferenceEquals(Plugin.Host, null))
            {
                if (ModSettings.AutoSave) LightingManager.SaveLights();
            }
        }

        public static void ChangeAmbient(int channel, int direction)
        {
            SetAmbient(LightValues.StepColor(LightingManager.ambient, channel, direction));
        }

    }
}
