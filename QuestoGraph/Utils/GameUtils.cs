using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;

namespace QuestoGraph.Utils
{
    internal unsafe static class GameUtils
    {
        internal static MapLinkPayload GetMapPayload(Level issuerLevel, float modifier = 1000f) // Scaling again so its more precise?
        {
            var terriroty = issuerLevel.Territory.Value.RowId;
            var map = issuerLevel.Map.Value.RowId;
            var x = issuerLevel.X * modifier;
            var y = issuerLevel.Z * modifier;

            return new MapLinkPayload(terriroty, map, (int)x, (int)y);
        }

        internal static void ShowMapPos(Level level)
            => Plugin.GameGui.OpenMapWithMapLink(GetMapPayload(level));

        internal static void ShowMapPos(MapLinkPayload payload)
            => Plugin.GameGui.OpenMapWithMapLink(payload);

        internal static void ShowInQuestJournal(uint questId)
            => AgentQuestJournal.Instance()->OpenForQuest(questId, 1u); // 2 would be Leves?

        internal static int CalculateExp(Quest quest)
        {
            var paramGrow = Plugin.DataManager.GetExcelSheet<ParamGrow>()!.GetRow(quest.ClassJobLevel[0]);
            var xp = quest.ExpFactor * paramGrow.ScaledQuestXP * paramGrow.QuestExpModifier / 100;
            return xp < 0 ? 0 : xp;
        }

        // Couldnt find it in the files, so i "map" it myself
        internal static uint GetJobIconId(ClassJob job)
        {
            const uint baseId = 62226;
            return job.RowId - 1 + baseId;
        }
    }
}
