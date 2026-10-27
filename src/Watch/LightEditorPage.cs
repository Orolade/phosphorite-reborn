using BananaOS;
using BananaOS.Pages;

namespace phosphorite
{
    public class LightEditorPage : WatchPage
    {
        enum Row { Source, Enabled, Intensity, Red, Green, Blue, Move, Destroy }
        public override string Title { get { return "Light Editor"; } }
        public override bool DisplayOnMainMenu { get { return true; } }

        public override void OnPostModSetup() { selectionHandler.maxIndex = 7; }
        public override void OnPageOpen() { WatchPages.OnInput(); }

        public override void OnButtonPressed(WatchButtonType button)
        {
            if (WatchNavigation.Handle(button, selectionHandler, Adjust, Confirm)) return;
            LightEditorTool.CancelMove();
            ReturnToMainMenu();
        }


        public override string OnGetScreenContent()
        {
            bool exists = LightEditor.HasSelection;
            var color = LightEditor.LightColor;
            var screen = new WatchScreen(Title, selectionHandler);
            screen.Value((int) Row.Source, "Light", exists ? (LightEditor.SelectedIndex + 1) + " / " + LightEditor.LightCount : "--");
            screen.Row((int) Row.Enabled, "Light: " + (exists ? LightEditor.SelectedEnabled ? "ON" : "OFF" : "--"));
            screen.Intensity((int) Row.Intensity, LightEditor.Intensity, exists);
            screen.Channel((int) Row.Red, "R", color.r, exists);
            screen.Channel((int) Row.Green, "G", color.g, exists);
            screen.Channel((int) Row.Blue, "B", color.b, exists);
            screen.Row((int) Row.Move, LightEditorTool.IsMoving ? "Cancel move" : "Move light");
            screen.Row((int) Row.Destroy, "Destroy light");
            if (exists) screen.Color("Color", color);
            screen.Status(LightEditor.Status);
            return screen.ToString();
        }

        void Adjust(int index, int direction)
        {
            var row = (Row) index;
            switch (row)
            {
                case Row.Source: LightEditor.CycleLight(direction); break;
                case Row.Enabled: LightEditor.SetSelectedEnabled(direction > 0); break;
                case Row.Intensity: LightEditor.ChangeIntensity(direction); break;
                case Row.Red: case Row.Green: case Row.Blue: LightEditor.ChangeChannel(index - (int) Row.Red, direction); break;
            }
        }

        void Confirm(int index)
        {
            switch ((Row) index)
            {
                case Row.Enabled: LightEditor.SetSelectedEnabled(!LightEditor.SelectedEnabled); break;
                case Row.Move: LightEditorTool.ToggleMove(); break;
                case Row.Destroy: LightEditor.DeleteSelected(); break;
            }
        }

    }
}
