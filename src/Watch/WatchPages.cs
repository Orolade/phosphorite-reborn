using System;
using BananaOS;
using BananaOS.Pages;

namespace phosphorite
{
    public static class WatchPages
    {
        public static void OnInput()
        {
            LightSpawner.SuppressInput();
            LightEditorTool.SuppressInput();
        }

        static readonly Type[] pages = { typeof(LightSpawnerPage), typeof(LightingPage), typeof(LightEditorPage), typeof(PortableLightsPage) };

        internal static bool Owns(Type type) { return Array.IndexOf(pages, type) >= 0; }

        internal static void Register()
        {
            try
            {
                foreach (var type in pages) MonkeWatch.RegisterPage(type);
                WatchMenuOrder.Update();
                var watch = MonkeWatch.Instance;
                if (watch == null || watch.watchPages == null) return;
                bool added = false;
                foreach (var type in pages)
                {
                    bool found = false;
                    foreach (var page in watch.watchPages) if (page != null && page.GetType() == type)
                    {
                        found = true;
                        break;
                    }
                    if (found) continue;
                    var instance = (WatchPage) watch.gameObject.AddComponent(type);
                    watch.watchPages.Add(instance);
                    instance.OnPostModSetup();
                    added = true;
                }
                if (added)
                {
                    foreach (var page in watch.watchPages) if (page is MainMenu)
                    {
                        MainMenu.screenPageDict.Clear();
                        page.OnPostModSetup();
                        break;
                    }
                    watch.UpdateScreen();
                }
            }
            catch (Exception e)
            {
                Plugin.Log("BananaOS: " + e, true);
            }
        }
    }
}
