using QuestoGraph.Enums;

namespace QuestoGraph.Data.Settings
{
    public class SearchFilterSettings
    {
        public bool UseAdvancedFilter { get; set; } = false;

        public SelectableClientLanguage SearchLangauge { get; set; } = SelectableClientLanguage.Default;

        public bool UseSettingsPrefilter { get; set; } = true;

        public bool IncludeQuestNames { get; set; } = true;

        public bool IncludeEmotes { get; set; } = true;

        public bool IncludeInstances { get; set; } = true;

        public bool IncludeActions { get; set; } = true;

        public bool IncludeItems { get; set; } = true;
    }
}
