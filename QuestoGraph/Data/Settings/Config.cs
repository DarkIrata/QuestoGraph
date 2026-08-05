using Dalamud.Configuration;

namespace QuestoGraph.Data.Settings
{
    [Serializable]
    internal class Config : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        public SearchFilterSettings SearchFilter { get; } = new SearchFilterSettings();

        public GeneralSettings General { get; set; } = new GeneralSettings();

        public DisplaySettings Display { get; set; } = new DisplaySettings();

        public ColorSettings Colors { get; set; } = new ColorSettings();

        public GraphSettings Graph { get; set; } = new GraphSettings();
    }
}
