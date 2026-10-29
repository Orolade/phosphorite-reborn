using BananaOS;
using BananaOS.Pages;

namespace phosphorite
{
    public class LightSpawnerPage : WatchPage
    {
        enum Row { Intensity, Red, Green, Blue, Pick }
        public override string Title { get { return "Light Spawner"; } }
        public override bool DisplayOnMainMenu { get { return true; } }

        public override void OnPostModSetup() { selectionHandler.maxIndex = 4; }
        public override void OnPageOpen() { WatchPages.OnInput(); }

        public override void OnButtonPressed(WatchButtonType button)
        {
            if (WatchNavigation.Handle(button, selectionHandler, Adjust, Confirm)) return;
            LightSpawner.CancelPick();
            ReturnToMainMenu();
        }


        public override string OnGetScreenContent()
        {
            var color = LightSpawnerSettings.Color;
            var screen = new WatchScreen(Title, selectionHandler);
            screen.Intensity((int) Row.Intensity, LightSpawnerSettings.Intensity);
            screen.Channel((int) Row.Red, "R", color.r);
            screen.Channel((int) Row.Green, "G", color.g);
            screen.Channel((int) Row.Blue, "B", color.b);
            screen.Row((int) Row.Pick, LightSpawner.IsPicking ? "Cancel pick" : "Pick light");
            screen.Color("Color", color);
            return screen.ToString();
        }

        void Adjust(int row, int direction)
        {
            switch ((Row) row)
            {
                case Row.Intensity: LightSpawnerSettings.SetIntensity(LightValues.StepSignedIntensity(LightSpawnerSettings.Intensity, direction)); break;
                case Row.Red: case Row.Green: case Row.Blue: LightSpawnerSettings.ChangeChannel(row - (int) Row.Red, direction); break;
            }
        }

        void Confirm(int row)
        {
            if ((Row) row == Row.Pick) LightSpawner.TogglePick();
        }
    }
}
