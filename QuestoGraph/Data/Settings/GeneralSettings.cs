using Dalamud.Game;

namespace QuestoGraph.Data.Settings
{
    public class GeneralSettings
    {
        public ClientLanguage Language { get; set; } = Plugin.DataManager.Language;
    }
}
