using Dalamud.Game;
using Lumina.Excel.Sheets;
using QuestoGraph.Data.Settings;
using QuestoGraph.Enums;

namespace QuestoGraph.Data
{
    internal class QuestData
    {
        public QuestDataLocalized QuestDataEn { get; }

        public QuestDataLocalized QuestDataDe { get; }

        public QuestDataLocalized QuestDataFr { get; }

        public QuestDataLocalized QuestDataJp { get; }

        public uint RowId => this.QuestDataEn.RowId;

        public string Name => this.GetLocalizedQuestData(this.languageSettings.QuestNames).Name;

        public Quest Quest => this.GetLocalizedQuestData(this.languageSettings.QuestNames).Quest;

        public QuestTypes QuestType => this.QuestDataEn.QuestType;

        public uint GilReward => this.QuestDataEn.GilReward;

        public bool HasEmoteReward => this.QuestDataEn.Quest.EmoteReward.RowId != 0;

        public Emote Emote => this.Quest.EmoteReward.Value;

        public bool HasActionReward => this.Quest.ActionReward.RowId != 0;

        public Lumina.Excel.Sheets.Action Action => this.Quest.ActionReward.Value;

        public bool HasGeneralActionRewards => this.GeneralActions.Count != 0;

        public IReadOnlyList<GeneralActionData> GeneralActions => this.GetLocalizedQuestData(this.languageSettings.Rewards).GeneralActions;

        public ItemRewardsData ItemRewards => this.GetLocalizedQuestData(this.languageSettings.Rewards).ItemRewards;

        public bool HasInstanceUnlocks => this.InstanceUnlocks.Count != 0;

        public IReadOnlyList<InstanceData> InstanceUnlocks => this.GetLocalizedQuestData(this.languageSettings.Instances).InstanceUnlocks;

        public bool HasBeastTribeUnlock => this.QuestDataEn.BeastTribe.RowId != 0 && !this.QuestDataEn.Quest.IsRepeatable && this.QuestDataEn.Quest.BeastReputationRank.RowId == 0;

        public BeastTribe BeastTribe => this.Quest.BeastTribe.Value;

        public bool HasJobUnlock => this.JobUnlock.RowId != 0;

        public ClassJob JobUnlock => this.GetLocalizedQuestData(this.languageSettings.Rewards).JobUnlock;

        public IReadOnlyList<uint> PreviousQuestsId => this.GetLocalizedQuestData(this.languageSettings.QuestNames).PreviousQuestsId;

        public IReadOnlyList<uint> NextQuestIds => this.GetLocalizedQuestData(this.languageSettings.QuestNames).NextQuestIds;

        public bool IsReachable => this.GetLocalizedQuestData(this.languageSettings.QuestNames).IsReachable;

        private readonly LanguageSettings languageSettings;

        public QuestData(LanguageSettings languageSettings, QuestDataLocalized questDataEn, QuestDataLocalized questDataDe, QuestDataLocalized questDataFr, QuestDataLocalized questDataJp)
        {
            this.languageSettings = languageSettings;
            this.QuestDataEn = questDataEn;
            this.QuestDataDe = questDataDe;
            this.QuestDataFr = questDataFr;
            this.QuestDataJp = questDataJp;
        }

        private QuestDataLocalized GetLocalizedQuestData(ClientLanguage language) => language switch
        {
            ClientLanguage.Japanese => this.QuestDataJp,
            ClientLanguage.German => this.QuestDataDe,
            ClientLanguage.French => this.QuestDataFr,
            _ => this.QuestDataEn,
        };

        internal void AppendNextQuests(IEnumerable<uint> nextQuests)
        {
            this.QuestDataEn.AppendNextQuests(nextQuests);
            this.QuestDataDe.AppendNextQuests(nextQuests);
            this.QuestDataFr.AppendNextQuests(nextQuests);
            this.QuestDataJp.AppendNextQuests(nextQuests);
        }
    }
}
