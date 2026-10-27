using BananaOS;
using BananaOS.Pages;

namespace phosphorite
{
    public class PortableLightsPage : WatchPage
    {
        enum Row { Flashlight, Lantern, Recall }
        public override string Title { get { return "Portable Lights"; } }
        public override bool DisplayOnMainMenu { get { return true; } }

        public override void OnPostModSetup() { selectionHandler.maxIndex = 2; }
        public override void OnPageOpen() { WatchPages.OnInput(); }

        public override void OnButtonPressed(WatchButtonType button)
        {
            if (WatchNavigation.Handle(button, selectionHandler, Adjust, Confirm)) return;
            ReturnToMainMenu();
        }


        public override string OnGetScreenContent()
        {
            var screen = new WatchScreen(Title, selectionHandler);
            screen.Value((int) Row.Flashlight, "Flashlight", PortableLights.FlashlightEnabled ? "ON" : "OFF");
            screen.Value((int) Row.Lantern, "Lantern", PortableLights.LanternEnabled ? "SPAWNED" : "NONE");
            screen.Value((int) Row.Recall, "Recall lantern", PortableLights.LanternRight ? "Right" : "Left");
            screen.Status(PortableLights.Status);
            return screen.ToString();
        }

        void Adjust(int index, int direction)
        {
            switch ((Row) index)
            {
                case Row.Flashlight: PortableLights.SetEnabled(true, direction > 0); break;
                case Row.Lantern: PortableLights.SetEnabled(false, direction > 0); break;
                case Row.Recall: PortableLights.SetLanternHand(direction > 0); break;
            }
        }

        void Confirm(int index)
        {
            switch ((Row) index)
            {
                case Row.Flashlight: PortableLights.SetEnabled(true, !PortableLights.FlashlightEnabled); break;
                case Row.Lantern: PortableLights.SetEnabled(false, !PortableLights.LanternEnabled); break;
                case Row.Recall: PortableLights.RecallLantern(); break;
            }
        }
    }
}
