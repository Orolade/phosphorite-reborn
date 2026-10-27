using BananaOS;
using BananaOS.Pages;

namespace phosphorite
{
    public class LightSpawnerPage : WatchPage
    {
        public override string Title { get { return "Light Spawner"; } }
        public override bool DisplayOnMainMenu { get { return true; } }

        public override void OnPostModSetup() { selectionHandler.maxIndex = 3; }
        public override void OnPageOpen() { WatchPages.OnInput(); }

        public override void OnButtonPressed(WatchButtonType button)
        {
            if (WatchNavigation.Handle(button, selectionHandler, Adjust)) return;
            ReturnToMainMenu();
        }


        public override string OnGetScreenContent()
        {
            var color = LightSpawner.DisplayColor;
            var screen = new WatchScreen(Title, selectionHandler);
            screen.Intensity(0, LightSpawner.Intensity);
            screen.Channel(1, "R", color.r);
            screen.Channel(2, "G", color.g);
            screen.Channel(3, "B", color.b);
            screen.Color("Color", color);
            return screen.ToString();
        }

        void Adjust(int row, int direction)
        {
            if (row == 0) LightSpawner.SetIntensity(LightValues.StepIntensity(LightSpawner.Intensity, direction));
            else LightSpawner.ChangeChannel(row - 1, direction);
        }
    }
}
