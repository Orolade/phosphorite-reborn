using System;
using BananaOS;

namespace phosphorite
{
    public static class WatchNavigation
    {
        internal static bool Handle(WatchButtonType button, SelectionHandler selection, Action<int, int> adjust, Action<int> confirm = null)
        {
            WatchPages.OnInput();
            switch (button)
            {
                case WatchButtonType.Up: selection.MoveSelectionUp(); break;
                case WatchButtonType.Down: selection.MoveSelectionDown(); break;
                case WatchButtonType.Left: adjust(selection.currentIndex, -1); break;
                case WatchButtonType.Right: adjust(selection.currentIndex, 1); break;
                case WatchButtonType.Enter: if (confirm != null) confirm(selection.currentIndex); break;
                case WatchButtonType.Back: return false;
            }
            return true;
        }
    }
}
