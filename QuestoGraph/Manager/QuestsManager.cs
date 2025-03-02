using System.Diagnostics;
using Lumina.Excel.Sheets;
using QuestoGraph.Data;
using QuestoGraph.Data.Settings;

namespace QuestoGraph.Manager
{
    internal class QuestsManager
    {
        private readonly Config config;
        public IReadOnlyDictionary<uint, QuestData> QuestData { get; private set; } = new Dictionary<uint, QuestData>();

        private string lastFilter = string.Empty;
        private IEnumerable<QuestData>? filteredQuestData;

        public QuestsManager(Config config)
        {
            this.config = config;

            this.Initialize();
            this.GetFilteredList(string.Empty);
        }

        private void Initialize()
        {
            var sw = new Stopwatch();
            sw.Start();

            Plugin.Log.Info($"Initializing Quests..");
            var result = new Dictionary<uint, QuestData>();
            foreach (var quest in Plugin.DataManager.GetExcelSheet<Quest>(Dalamud.Game.ClientLanguage.English))
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
            Plugin.Log.Info($"{this.QuestData.Count} Quests loaded - {sw.Elapsed}");
        }

        // We run even at the start through it, so given settings would apply on load
        public IEnumerable<QuestData> GetFilteredList(string filter)
        {
            if (this.filteredQuestData != null && string.Equals(filter, this.lastFilter, StringComparison.InvariantCultureIgnoreCase))
            {
                return this.filteredQuestData;
            }

            bool DeepContains(QuestData data)
            {
                // This is disgusting, and i should feel disgusted!
                const StringComparison comparer = StringComparison.InvariantCultureIgnoreCase;
                var hasFilter = !string.IsNullOrWhiteSpace(filter);
                var nameContains = !hasFilter || data.Name.Contains(filter!, comparer);

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
                    result = true;
                    if (this.config.Search.IncludeEmotes && hasFilter)
                    {
                        result = data.Emote.Name.ExtractText().Contains(filter!, comparer);
                    }
                }

                if (!result && this.config.Display.ShowInstanceUnlocks && data.InstanceUnlocks.Any(iu => iu.ContentFound))
                {
                    result = true;
                    if (this.config.Search.IncludeInstances && hasFilter)
                    {
                        result = data.InstanceUnlocks.Any(iu => iu.ContentFound && iu.Name.Contains(filter!, comparer));
                    }
                }

                if (!result && this.config.Display.ShowJobAndActionQuests && (data.HasJobUnlock || data.HasActionReward || data.HasGeneralActionRewards))
                {
                    result = true;
                    if (this.config.Search.IncludeActions && hasFilter)
                    {
                        result = data.Action.Name.ExtractText().Contains(filter!, comparer) ||
                                 data.GeneralActions.Any(ga => ga.Name.Contains(filter!, comparer));
                    }
                }

                if (!result && this.config.Display.ShowWithRewards && data.ItemRewards.HasAnyItemRewards)
                {
                    result = true;
                    if (this.config.Search.IncludeItems && hasFilter)
                    {
                        result = data.ItemRewards.RewardItems.Any(r => r.Name.Contains(filter!, comparer)) ||
                                 data.ItemRewards.OptionalItems.Any(r => r.Name.Contains(filter!, comparer)) ||
                                 data.ItemRewards.CatalystItems.Any(r => r.Name.Contains(filter!, comparer)) ||
                                 (data.ItemRewards.HasOtherItemReward && data.ItemRewards.OtherItem!.Name.Contains(filter!, comparer));
                    }
                }

                return result;
            }

            Plugin.Log.Debug($"Refreshing Filtered List with filter '{filter ?? string.Empty}'");
            this.lastFilter = filter ?? string.Empty;
            this.filteredQuestData = this.QuestData.Values.Where(qd => qd.IsReachable && DeepContains(qd));
            return this.filteredQuestData;
        }
    }
}