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

        public QuestDataLocalized GetLocalizedQuestData(ClientLanguage language) => language switch
        {
            ClientLanguage.Japanese => this.QuestDataJp,
            ClientLanguage.German => this.QuestDataDe,
            ClientLanguage.French => this.QuestDataFr,
            _ => this.QuestDataEn,
        };

        internal void AppendNextQuests(IEnumerable<uint> nextQuests)
        {
            this.QuestDataEn?.AppendNextQuests(nextQuests);
            this.QuestDataDe?.AppendNextQuests(nextQuests);
            this.QuestDataFr?.AppendNextQuests(nextQuests);
            this.QuestDataJp?.AppendNextQuests(nextQuests);
        }

        //internal bool ContainsName(string name, StringComparison comparer, ClientLanguage language)
        //{
        //    var localized = this.GetLocalizedQuestData(language);
        //    return LevenshteinDistance.Calculate(localized.Name.ToLower(), name.ToLower()) < 5;
        //}

        //internal bool ContainsEmote(string name, StringComparison comparer, ClientLanguage language)
        //{
        //    var localized = this.GetLocalizedQuestData(language);
        //    return localized.HasEmoteReward && LevenshteinDistance.Calculate(localized.Emote.Name.ExtractText().ToLower(), name.ToLower()) < 5;
        //}

        //internal bool ContainsInstance(string name, StringComparison comparer, ClientLanguage language)
        //{
        //    var localized = this.GetLocalizedQuestData(language);
        //    return localized.InstanceUnlocks.Any(iu => iu.ContentFound && LevenshteinDistance.Calculate(iu.Name.ToLower(), name.ToLower()) < 5);
        //}

        //internal bool ContainsAction(string name, StringComparison comparer, ClientLanguage language)
        //{
        //    var localized = this.GetLocalizedQuestData(language);
        //    return LevenshteinDistance.Calculate(localized.Action.Name.ExtractText().ToLower(), name.ToLower()) < 5 ||
        //           localized.GeneralActions.Any(ga => LevenshteinDistance.Calculate(ga.Name.ToLower(), name.ToLower()) < 5);
        //}


        //internal bool ContainsItems(string name, StringComparison comparer, ClientLanguage language)
        //{
        //    var localized = this.GetLocalizedQuestData(language);
        //    return localized.ItemRewards.RewardItems.Any(r => LevenshteinDistance.Calculate(r.Name.ToLower(), name.ToLower()) < 5) ||
        //             localized.ItemRewards.OptionalItems.Any(r => LevenshteinDistance.Calculate(r.Name.ToLower(), name.ToLower()) < 5) ||
        //             localized.ItemRewards.CatalystItems.Any(r => LevenshteinDistance.Calculate(r.Name.ToLower(), name.ToLower()) < 5) ||
        //             (localized.ItemRewards.HasOtherItemReward && LevenshteinDistance.Calculate(localized.ItemRewards.OtherItem!.Name.ToLower(), name.ToLower()) < 5);
        //}

        internal bool ContainsName(string name, StringComparison comparer, ClientLanguage language)
        {
            var localized = this.GetLocalizedQuestData(language);
            return localized.Name.Contains(name, comparer);
        }

        internal bool ContainsEmote(string name, StringComparison comparer, ClientLanguage language)
        {
            var localized = this.GetLocalizedQuestData(language);
            return localized.HasEmoteReward && localized.Emote.Name.ExtractText().Contains(name, comparer);
        }

        internal bool ContainsInstance(string name, StringComparison comparer, ClientLanguage language)
        {
            var localized = this.GetLocalizedQuestData(language);
            return localized.InstanceUnlocks.Any(iu => iu.ContentFound && iu.Name.Contains(name, comparer));
        }

        internal bool ContainsAction(string name, StringComparison comparer, ClientLanguage language)
        {
            var localized = this.GetLocalizedQuestData(language);
            return localized.Action.Name.ExtractText().Contains(name, comparer) ||
                               localized.GeneralActions.Any(ga => ga.Name.Contains(name, comparer));
        }

        internal bool ContainsItems(string name, StringComparison comparer, ClientLanguage language)
        {
            var localized = this.GetLocalizedQuestData(language);
            return localized.ItemRewards.RewardItems.Any(r => r.Name.Contains(name, comparer)) ||
                     localized.ItemRewards.OptionalItems.Any(r => r.Name.Contains(name, comparer)) ||
                     localized.ItemRewards.CatalystItems.Any(r => r.Name.Contains(name, comparer)) ||
                     (localized.ItemRewards.HasOtherItemReward && localized.ItemRewards.OtherItem!.Name.Contains(name, comparer));
        }

        internal bool ContainsText(string text, StringComparison comparer, ClientLanguage language)
            => this.ContainsName(text, comparer, language) ||
                this.ContainsEmote(text, comparer, language) ||
                this.ContainsInstance(text, comparer, language) ||
                this.ContainsAction(text, comparer, language) ||
                this.ContainsItems(text, comparer, language);
    }
}
