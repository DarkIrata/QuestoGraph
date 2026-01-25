using Dalamud.Game;
using Lumina.Excel.Sheets;
using static Lumina.Excel.Sheets.Quest;

namespace QuestoGraph.Data
{
    internal class InstanceData
    {
        public ContentFinderCondition ContentFinder { get; }

        public uint ContentRowId => this.ContentFinder.Content.RowId;

        public string Name => this.ContentFinder.Name.ExtractText();

        public bool ContentFound => this.ContentFinder.RowId != 0;

        public InstanceData(QuestParamsStruct questParams, ClientLanguage clientLanguage = ClientLanguage.English)
            : this(questParams.ScriptArg, clientLanguage)
        {
        }

        public InstanceData(uint contentRowId, ClientLanguage clientLanguage = ClientLanguage.English)
        {
            // Only InstanceContent (ContentLinkType) 
            var contentFinder = Plugin.DataManager.GetExcelSheet<ContentFinderCondition>(clientLanguage)!
                .FirstOrDefault(cfc => cfc.Content.RowId == contentRowId && cfc.ContentLinkType == 1);

            if (contentFinder.RowId == 0)
            //contentFinder.UnlockQuest.RowId != 0) // TODO: Need to find out, to what that was changed
            {
                return;
            }

            this.ContentFinder = contentFinder;
        }
    }
}
