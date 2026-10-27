using System;
using System.Collections.Generic;
using BananaOS;
using BananaOS.Pages;

namespace phosphorite
{
    public static class WatchMenuOrder
    {
        static MonkeWatch lastWatch;
        static int typeCount = -1, pageCount = -1;

        internal static bool MoveToEnd<T>(List<T> items, Predicate<T> own)
        {
            if (items == null) return false;
            int first = items.FindIndex(own);
            if (first < 0) return false;
            bool misplaced = false;
            for (int i = first + 1; i < items.Count; i++) if (!own(items[i])) { misplaced = true; break; }
            if (!misplaced) return false;
            var moved = items.FindAll(own);
            items.RemoveAll(own);
            items.AddRange(moved);
            return true;
        }

        internal static void Update()
        {
            if (MonkeWatch.pageTypes != null && typeCount != MonkeWatch.pageTypes.Count)
            {
                MoveToEnd(MonkeWatch.pageTypes, WatchPages.Owns);
                typeCount = MonkeWatch.pageTypes.Count;
            }
            var watch = MonkeWatch.Instance;
            if (watch == null || watch.watchPages == null) return;
            if (watch == lastWatch && pageCount == watch.watchPages.Count) return;
            lastWatch = watch;
            pageCount = watch.watchPages.Count;
            if (!MoveToEnd(watch.watchPages, page => page != null && WatchPages.Owns(page.GetType()))) return;
            foreach (var page in watch.watchPages) if (page is MainMenu)
            {
                MainMenu.screenPageDict.Clear();
                page.OnPostModSetup();
                watch.UpdateScreen();
                break;
            }
        }
    }
}
