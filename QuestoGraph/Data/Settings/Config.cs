using Dalamud.Configuration;

namespace QuestoGraph.Data.Settings
{
    [Serializable]
    internal class Config : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        public DisplaySettings Display { get; set; } = new DisplaySettings();

        public SearchSettings Search { get; set; } = new SearchSettings();

        public ColorSettings Colors { get; set; } = new ColorSettings();
    }
}
