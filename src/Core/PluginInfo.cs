using System.Reflection;

[assembly: AssemblyVersion(phosphorite.PluginInfo.AssemblyVersion)]
[assembly: AssemblyFileVersion(phosphorite.PluginInfo.AssemblyVersion)]
[assembly: AssemblyInformationalVersion(phosphorite.PluginInfo.Version)]

namespace phosphorite
{
    static class PluginInfo
    {
        public const string GUID = "com.brokenstone.gorillatag.phosphorite";
        public const string Name = "phosphorite";
        public const string Version = "1.0.0";
        public const string AssemblyVersion = "1.0.0";
    }
}
