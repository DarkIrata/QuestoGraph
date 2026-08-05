using System.Diagnostics;
using Dalamud.Game;
using Lumina.Excel.Sheets;
using QuestoGraph.Data;
using QuestoGraph.Data.Settings;

namespace QuestoGraph.Manager
{
    internal class QuestsManager
    {
        internal enum State
        {
            Unloaded,
            Initializing,
            Initialized,
            Failed,
        }

        private readonly Config config;

        public IReadOnlyDictionary<uint, QuestData> QuestData { get; private set; } = new Dictionary<uint, QuestData>();

        public State CurrentState { get; private set; } = State.Unloaded;

        private string lastFilter = string.Empty;
        private IEnumerable<QuestData>? filteredQuestData;

        public QuestsManager(Config config)
        {
            this.config = config;

            this.ReInitialize();
        }

        internal void ReInitialize()
        {
            this.InitializeAsync();
        }

        internal void RefreshList()
        {
            this.filteredQuestData = null;
        }

        private Task InitializeAsync() => Task.Run(() =>
        {
            this.Initialize();
            this.GetFilteredList(string.Empty);
            this.filteredQuestData = null;
        });

        private void Initialize()
        {
            if (this.CurrentState == State.Initializing)
            {
                Plugin.Log.Warning($"Already Initializing Quests..");
                return;
            }

            var sw = new Stopwatch();
            sw.Start();

            Plugin.Log.Info($"Initializing Quests..");
            this.CurrentState = State.Initializing;
            var result = new Dictionary<uint, QuestData>();
            foreach (var quest in Plugin.DataManager.GetExcelSheet<Quest>(Dalamud.Game.ClientLanguage.English))
            {
                if (string.IsNullOrEmpty(quest.Name.ExtractText()) ||
                    result.ContainsKey(quest.RowId))
                {
                    continue;
                }

                var questDataEn = new QuestDataLocalized(quest, Dalamud.Game.ClientLanguage.English);
                var questDataDe = new QuestDataLocalized(quest.RowId, Dalamud.Game.ClientLanguage.German);
                var questDataFr = new QuestDataLocalized(quest.RowId, Dalamud.Game.ClientLanguage.French);
                var questDataJp = new QuestDataLocalized(quest.RowId, Dalamud.Game.ClientLanguage.Japanese);

                var questData = new QuestData(this.config.General.Language, questDataEn, questDataDe, questDataFr, questDataJp);
                result.Add(questData.RowId, questData);
            }

            Plugin.Log.Info($"Building NextQuest tree..");
            foreach (var quest in result)
            {
                var nextQuests = result.Values.Where(q => q.PreviousQuestsId.Any(pq => pq == quest.Key)).Select(q => q.RowId);
                quest.Value.AppendNextQuests(nextQuests);
            }

            this.QuestData = result;

            sw.Stop();
            this.CurrentState = State.Initialized;
            Plugin.Log.Info($"{this.QuestData.Count} (Total: {this.QuestData.Count * 4})  Quests loaded - {sw.Elapsed}");
        }

        // We run even at the start through it, so given settings would apply on load
        public IEnumerable<QuestData> GetFilteredList(string filter)
        {
            if (this.CurrentState != State.Initialized ||
                (this.filteredQuestData != null && string.Equals(filter, this.lastFilter, StringComparison.InvariantCultureIgnoreCase)))
            {
                return this.filteredQuestData ?? Array.Empty<QuestData>();
            }

            bool IsPrefiltered(QuestData data) // Filtered through basic Configuration
            {
                if (this.config.SearchFilter.UseSettingsPrefilter)
                {
                    return false;
                }

                // False = dont filter this quest from results
                // True = filter out
                // returning true / false for readability

                if ((this.config.Display.ShowMSQQuests && data.QuestType == Enums.QuestTypes.MSQ) ||
                    (this.config.Display.ShowBlueQuests && data.QuestType == Enums.QuestTypes.Blue) ||
                    (this.config.Display.ShowNormalQuests && data.QuestType == Enums.QuestTypes.Normal) ||
                    (this.config.Display.ShowEmoteQuests && data.HasEmoteReward) ||
                    (this.config.Display.ShowInstanceUnlocks && data.HasInstanceUnlocks) ||
                    (this.config.Display.ShowWithRewards && data.ItemRewards.HasAnyItemRewards) ||
                    (this.config.Display.ShowJobAndActionQuests && (data.HasJobUnlock || data.HasActionReward || data.HasGeneralActionRewards)))
                {
                    return false;
                }

                return true;
            }

            Plugin.Log.Debug($"Refreshing Filtered List with filter '{filter ?? string.Empty}'");
            this.lastFilter = filter ?? string.Empty;
            this.filteredQuestData = this.QuestData.Values.Where(qd => qd.IsReachable && !IsPrefiltered(qd) && this.DeepContains(qd, filter)).ToList();
            return this.filteredQuestData;
        }



        private bool DeepContains(QuestData data, string? filter)
        {
            // This is disgusting, and i should feel disgusted!
            const StringComparison comparer = StringComparison.InvariantCultureIgnoreCase;
            var hasFilter = !string.IsNullOrWhiteSpace(filter);

            if (!hasFilter)
            {
                return true;
            }

            bool Contains(string text, ClientLanguage questName, ClientLanguage emote, ClientLanguage instances, ClientLanguage actions, ClientLanguage items)
            {
                return (this.config.SearchFilter.IncludeQuestNames && data.ContainsName(text, comparer, questName)) ||
                        (this.config.SearchFilter.IncludeEmotes && data.ContainsEmote(text, comparer, emote)) ||
                        (this.config.SearchFilter.IncludeInstances && data.ContainsInstance(text, comparer, instances)) ||
                        (this.config.SearchFilter.IncludeActions && data.ContainsAction(text, comparer, actions)) ||
                        (this.config.SearchFilter.IncludeItems && data.ContainsItems(text, comparer, items));
            }

            ClientLanguage? targetLang;
            if (this.HasFilterLangPrefix(filter!, out targetLang))
            {
                return Contains(filter![3..], targetLang!.Value, targetLang!.Value, targetLang!.Value, targetLang!.Value, targetLang!.Value);
            }
            else if (this.HasSearchLanguageSet(filter!, out targetLang))
            {
                return Contains(filter!, targetLang!.Value, targetLang!.Value, targetLang!.Value, targetLang!.Value, targetLang!.Value);
            }
            else
            {
                return Contains(filter!,
                this.config.General.Language.QuestNames,
                this.config.General.Language.Rewards,
                this.config.General.Language.Instances,
                this.config.General.Language.Rewards,
                this.config.General.Language.Rewards);
            }
        }

        private bool HasFilterLangPrefix(string filter, out ClientLanguage? language)
        {
            language = null;

            if (filter!.StartsWith("en:"))
            {
                language = ClientLanguage.English;
            }
            else if (filter!.StartsWith("de:"))
            {
                language = ClientLanguage.German;
            }
            else if (filter!.StartsWith("jp:"))
            {
                language = ClientLanguage.Japanese;
            }
            else if (filter!.StartsWith("fr:"))
            {
                language = ClientLanguage.French;
            }

            return language is not null;
        }

        private bool HasSearchLanguageSet(string filter, out ClientLanguage? language)
        {
            language = null;

            if (this.config.SearchFilter.SearchLangauge != Enums.SelectableClientLanguage.Default)
            {
                switch (this.config.SearchFilter.SearchLangauge)
                {
                    case Enums.SelectableClientLanguage.English:
                        language = ClientLanguage.English;
                        break;
                    case Enums.SelectableClientLanguage.German:
                        language = ClientLanguage.German;
                        break;
                    case Enums.SelectableClientLanguage.French:
                        language = ClientLanguage.French;
                        break;
                    case Enums.SelectableClientLanguage.Japanese:
                        language = ClientLanguage.Japanese;
                        break;
                }
            }

            return language is not null;
        }
    }
}