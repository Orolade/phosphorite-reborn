using BananaOS;
using BananaOS.Pages;

namespace phosphorite
{
    public class LightingPage : WatchPage
    {
        public override string Title { get { return "Lighting"; } }
        public override bool DisplayOnMainMenu { get { return true; } }

        public override void OnPostModSetup() { selectionHandler.maxIndex = 5; }
        public override void OnPageOpen() { WatchPages.OnInput(); }

        public override void OnButtonPressed(WatchButtonType button)
        {
            if (WatchNavigation.Handle(button, selectionHandler, Adjust, Confirm)) return;
            ReturnToMainMenu();
        }


        public override string OnGetScreenContent()
        {
            var color = LightingManager.ambient;
            var screen = new WatchScreen(Title, selectionHandler);
            screen.Channel(0, "Ambient R", color.r);
            screen.Channel(1, "Ambient G", color.g);
            screen.Channel(2, "Ambient B", color.b);
            screen.Row(3, "Save Lights to JSON");
            screen.Row(4, "Load Lights from JSON");
            screen.Row(5, "Clear All Lights");
            screen.Color("Ambient", color);
            screen.Status(LightingFiles.FileStatus);
            return screen.ToString();
        }

        void Adjust(int row, int direction)
        {
            if (row <= 2) SceneLighting.ChangeAmbient(row, direction);
        }

        void Confirm(int row)
        {
            if (row >= 3) LightingFiles.RunFileAction(row - 3);
        }
    }
}
