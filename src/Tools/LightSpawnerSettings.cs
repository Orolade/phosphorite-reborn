using BepInEx.Configuration;
using UnityEngine;

namespace phosphorite
{
    public static class LightSpawnerSettings
    {
        static float intensity = 1f;
        static Color color = Color.white;
        static ConfigEntry<float> intensityConfig;
        static ConfigEntry<string> colorConfig;

        public static float Intensity { get { return intensity; } }
        public static Color Color { get { return color; } }

        public static void SetIntensity(float value)
        {
            if (!LightValues.IsValidIntensity(value)) return;
            intensity = value;
            if (intensityConfig != null) intensityConfig.Value = value;
        }

        public static void SetColor(Color value)
        {
            color = new Color(LightValues.ToByte(value.r) / 255f, LightValues.ToByte(value.g) / 255f, LightValues.ToByte(value.b) / 255f, 1);
            if (colorConfig != null) colorConfig.Value = LightValues.ToHex(color);
        }

        public static void ChangeChannel(int channel, int direction)
        {
            SetColor(LightValues.StepColor(color, channel, direction));
        }

        internal static void Configure(ConfigFile config)
        {
            intensityConfig = config.Bind("LightGun", "Intensity", 1f, "");
            colorConfig = config.Bind("LightGun", "Color", "#FFFFFF", "");
            SetIntensity(intensityConfig.Value);
            Color parsed;
            string saved = colorConfig.Value;
            SetColor(ColorUtility.TryParseHtmlString(saved ?? "", out parsed) ? parsed : Color.white);
        }
    }
}
