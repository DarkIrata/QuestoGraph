namespace QuestoGraph.Data.Settings
{
    public class GeneralSettings
    {
        public bool ShowQuestId { get; set; } = false;

        public LanguageSettings Language { get; set; } = new LanguageSettings();
    }
}
