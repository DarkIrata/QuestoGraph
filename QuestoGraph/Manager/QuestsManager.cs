using System.Diagnostics;
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
            foreach (var quest in Plugin.DataManager.GetExcelSheet<Quest>(this.config.General.Language))
            {
                if (string.IsNullOrEmpty(quest.Name.ExtractText()) ||
                    result.ContainsKey(quest.RowId))
                {
                    continue;
                }

                var questData = new QuestData(quest);
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
            Plugin.Log.Info($"{this.QuestData.Count} Quests loaded - {sw.Elapsed}");
        }

        // We run even at the start through it, so given settings would apply on load
        public IEnumerable<QuestData> GetFilteredList(string filter)
        {
            if (this.CurrentState != State.Initialized ||
            (this.filteredQuestData != null && string.Equals(filter, this.lastFilter, StringComparison.InvariantCultureIgnoreCase)))
            {
                return this.filteredQuestData ?? Array.Empty<QuestData>();
            }

            bool DeepContains(QuestData data)
            {
                // This is disgusting, and i should feel disgusted!
                const StringComparison comparer = StringComparison.InvariantCultureIgnoreCase;
                var hasFilter = !string.IsNullOrWhiteSpace(filter);
                filter = !hasFilter ? string.Empty : filter!;
                var nameContains = data.Name.Contains(filter, comparer);

                var result = false;
                if (!result && this.config.Display.ShowMSQQuests)
                {
                    result = nameContains &&
                           data.QuestType == Enums.QuestTypes.MSQ;
                }

                if (!result && this.config.Display.ShowNormalQuests)
                {
                    result = nameContains &&
                           data.QuestType == Enums.QuestTypes.Normal;
                }

                if (!result && this.config.Display.ShowBlueQuests)
                {
                    result = nameContains &&
                           data.QuestType == Enums.QuestTypes.Blue;
                }

                if (!result && this.config.Display.ShowEmoteQuests && data.HasEmoteReward)
                {
                    result = (hasFilter && nameContains) || !hasFilter;
                    if (this.config.Search.IncludeEmotes && hasFilter)
                    {
                        result = data.Emote.Name.ExtractText().Contains(filter!, comparer) || nameContains;
                    }
                }

                if (!result && this.config.Display.ShowInstanceUnlocks && data.InstanceUnlocks.Any(iu => iu.ContentFound))
                {
                    result = (hasFilter && nameContains) || !hasFilter;
                    if (this.config.Search.IncludeInstances && hasFilter)
                    {
                        result = data.InstanceUnlocks.Any(iu => iu.ContentFound && iu.Name.Contains(filter!, comparer)) || nameContains;
                    }
                }

                if (!result && this.config.Display.ShowJobAndActionQuests && (data.HasJobUnlock || data.HasActionReward || data.HasGeneralActionRewards))
                {
                    result = (hasFilter && nameContains) || !hasFilter;
                    if (this.config.Search.IncludeActions && hasFilter)
                    {
                        result = data.Action.Name.ExtractText().Contains(filter!, comparer) ||
                                 data.GeneralActions.Any(ga => ga.Name.Contains(filter!, comparer)) ||
                                 nameContains;
                    }
                }

                if (!result && this.config.Display.ShowWithRewards && data.ItemRewards.HasAnyItemRewards)
                {
                    result = (hasFilter && nameContains) || !hasFilter;
                    if (this.config.Search.IncludeItems && hasFilter)
                    {
                        result = data.ItemRewards.RewardItems.Any(r => r.Name.Contains(filter!, comparer)) ||
                                 data.ItemRewards.OptionalItems.Any(r => r.Name.Contains(filter!, comparer)) ||
                                 data.ItemRewards.CatalystItems.Any(r => r.Name.Contains(filter!, comparer)) ||
                                 (data.ItemRewards.HasOtherItemReward && data.ItemRewards.OtherItem!.Name.Contains(filter!, comparer)) ||
                                 nameContains;
                    }
                }

                return result;
            }

            Plugin.Log.Debug($"Refreshing Filtered List with filter '{filter ?? string.Empty}'");
            this.lastFilter = filter ?? string.Empty;
            this.filteredQuestData = this.QuestData.Values.Where(qd => qd.IsReachable && DeepContains(qd)).ToList();
            return this.filteredQuestData;
        }
    }
}