using System.Globalization;
using System.Text;
using BananaOS;
using UnityEngine;

namespace phosphorite
{
    public sealed class WatchScreen
    {
        readonly StringBuilder text = new StringBuilder();
        readonly SelectionHandler selection;

        internal WatchScreen(string title, SelectionHandler selection)
        {
            this.selection = selection;
            text.AppendLine(title);
        }

        internal void Row(int index, string label)
        {
            text.AppendLine(selection.GetOriginalBananaOSSelectionText(index, label));
        }

        internal void Value(int index, string label, string value)
        {
            Row(index, label + ": < " + value + " >");
        }

        internal void Channel(int index, string label, float value, bool enabled = true)
        {
            Value(index, label, enabled ? LightValues.ToLevel(value).ToString(CultureInfo.InvariantCulture) : "--");
        }

        internal void Intensity(int index, float value, bool enabled = true)
        {
            Value(index, "Intensity", enabled ? value.ToString("F1", CultureInfo.InvariantCulture) : "--");
        }

        internal void Color(string label, Color color)
        {
            string hex = LightValues.ToHex(color);
            text.Append(label + ": <color=" + hex + ">####</color> " + hex);
        }

        internal void Status(string status)
        {
            if (!string.IsNullOrEmpty(status)) text.Append(" | " + status);
        }

        public override string ToString() { return text.ToString(); }
    }
}
