using Dalamud.Game;

namespace QuestoGraph.Data.Settings
{
    public class LanguageSettings
    {
        public ClientLanguage QuestNames { get; set; } = Plugin.DataManager.Language;

        public ClientLanguage Rewards { get; set; } = Plugin.DataManager.Language;

        public ClientLanguage Instances { get; set; } = Plugin.DataManager.Language;
    }
}
