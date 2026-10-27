using BananaOS;
using BananaOS.Pages;

namespace phosphorite
{
    public class LightingPage : WatchPage
    {
        public override string Title { get { return "Lighting"; } }
        public override bool DisplayOnMainMenu { get { return true; } }

        public override void OnPostModSetup() { selectionHandler.maxIndex = 6; }
        public override void OnPageOpen() { WatchPages.OnInput(); }

        public override void OnButtonPressed(WatchButtonType button)
        {
            if (WatchNavigation.Handle(button, selectionHandler, Adjust, Confirm)) return;
            ReturnToMainMenu();
        }


        public override string OnGetScreenContent()
        {
            var color = SceneLighting.AmbientColor;
            var screen = new WatchScreen(Title, selectionHandler);
            screen.Row(0, "Baked lighting: " + (SceneLighting.BakedEnabled ? "ON" : "OFF"));
            screen.Channel(1, "Ambient R", color.r);
            screen.Channel(2, "Ambient G", color.g);
            screen.Channel(3, "Ambient B", color.b);
            screen.Row(4, "Save Lights to JSON");
            screen.Row(5, "Load Lights from JSON");
            screen.Row(6, "Clear All Lights");
            screen.Color("Ambient", color);
            screen.Status(LightingFiles.FileStatus);
            return screen.ToString();
        }

        void Adjust(int row, int direction)
        {
            if (row == 0) SceneLighting.SetBakedEnabled(direction > 0);
            else if (row <= 3) SceneLighting.ChangeAmbient(row - 1, direction);
        }

        void Confirm(int row)
        {
            if (row == 0) SceneLighting.SetBakedEnabled(!SceneLighting.BakedEnabled);
            else if (row >= 4) LightingFiles.RunFileAction(row - 4);
        }
    }
}
