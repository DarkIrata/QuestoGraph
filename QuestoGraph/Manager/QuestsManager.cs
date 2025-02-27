using System.Diagnostics;
using Lumina.Excel.Sheets;
using QuestoGraph.Data;

namespace QuestoGraph.Manager
{
    internal class QuestsManager
    {
        public IReadOnlyDictionary<uint, QuestData> QuestData { get; private set; } = new Dictionary<uint, QuestData>();

        private string lastFilter = string.Empty;
        private IEnumerable<QuestData>? filteredQuestData;

        public QuestsManager()
        {
            this.Initialize();
            this.GetFilteredList(string.Empty);
            this.Debug();
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

                //var (_, nodes) = Node<Quest>.BuildTree(allQuests);
                //this.AllNodes = nodes;
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
                return (string.IsNullOrWhiteSpace(filter) ||
                    data.Name.Contains(filter, comparer) ||
                    (data.HasJobUnlock && data.JobUnlock.Name.ExtractText().Contains(filter, comparer)) ||
                    (data.HasActionReward && data.Action.Name.ExtractText().Contains(filter, comparer)) ||
                    (data.HasEmoteReward && data.Emote.Name.ExtractText().Contains(filter, comparer)) ||
                    (data.HasInstanceUnlocks && data.InstanceUnlocks.Any(iu => iu.ContentFound && iu.Name.Contains(filter, comparer))) ||
                    (data.HasGeneralActionRewards && data.GeneralActions.Any(ga => ga.Name.Contains(filter, comparer))) ||
                    (data.ItemRewards.RewardItems.Any(r => r.Name.Contains(filter, comparer)) ||
                    data.ItemRewards.OptionalItems.Any(r => r.Name.Contains(filter, comparer)) ||
                    data.ItemRewards.CatalystItems.Any(r => r.Name.Contains(filter, comparer)) ||
                    (data.ItemRewards.HasOtherItemReward && data.ItemRewards.OtherItem!.Name.Contains(filter, comparer))));
            }

            Plugin.Log.Debug($"Refreshing Filtered List with filter '{filter ?? string.Empty}'");
            this.lastFilter = filter ?? string.Empty;
            this.filteredQuestData = this.QuestData.Values.Where(qd => qd.IsReachable && DeepContains(qd));
            return this.filteredQuestData;
        }

        private void Debug()
        {
            Plugin.Log.Info($"v========[DEBUG]========v");
            //var questData = this.QuestData.Values.FirstOrDefault(q => q.Emote.RowId == 122);
            //var questData = this.QuestData.Values.FirstOrDefault(q => q.Name.Contains("Nix That"));
            //var questData = this.QuestData.Values.FirstOrDefault(q => q.Name.Contains("Promises to Keep") && q.RowId == 69416);
            //var questData = this.QuestData.Values.FirstOrDefault(q => q.Name.Contains("A Spectacle for the Ages"));
            //var questData = this.QuestData.Values.FirstOrDefault(q => q.Name.Contains("A Good Adventurer Is Hard to Find"));
            //var questData = this.QuestData.Values.FirstOrDefault(q => q.Quest.ItemCatalyst.Count != 0 && q.Quest.ItemCatalyst.Any(i => i.RowId != 0));
            //var questData = this.QuestData.Values.FirstOrDefault(q => q.Quest.ActionReward.RowId != 0);
            //var questData = this.QuestData.Values.LastOrDefault(q => q.HasGeneralActionRewards);
            //var questData = this.QuestData.Values.FirstOrDefault(qd => qd.Quest.InstanceContent.Any(i => i.RowId != 0));
            //var questData = this.QuestData.Values.FirstOrDefault(q => q.Name.Contains("The Things We Do for Cheese"));
            var questData = this.QuestData.Values.FirstOrDefault(q => q.Name.Contains("Shadowbringers"));
            //var questData = this.QuestData.Values.FirstOrDefault(q => q.Name.Contains("The Path of the Righteous"));
            //var questData = this.QuestData.Values.FirstOrDefault(qd => qd.HasBeastTribeUnlock);
            //var questData = this.QuestData.Values.FirstOrDefault(q => q.Quest.ClassJobUnlock.RowId != 0);
            //var questData = this.QuestData.Values.FirstOrDefault(q => q.Name.Contains("Enter the Viper"));
            //var questData = this.QuestData.Values.FirstOrDefault(q => q.Name.Contains("Paladin's Pledge"));
            //var questData = this.QuestData.Values.FirstOrDefault(q => q.Name.Contains("Taking the Red"));
            //var questData = this.QuestData.Values.FirstOrDefault(q => q.Name.Contains("Nidhogg's Rage"));

            Plugin.Log.Info($"Quest: ({questData.RowId}) {questData.Name}");
            Plugin.Log.Info($"JournalGenre: {questData.Quest.JournalGenre.Value.Name}");
            Plugin.Log.Info($"Gil: {questData.GilReward}");
            Plugin.Log.Info($"Action: {questData.HasActionReward} | GenAction: {questData.HasGeneralActionRewards} | Emote: {questData.HasEmoteReward}");
            Plugin.Log.Info($"Reward: {questData.ItemRewards.HasDefaultItemRewards} | Optional: {questData.ItemRewards.HasOptionalItemRewards} | Catalyst: {questData.ItemRewards.HasCatalystItemRewards} | Other: {questData.ItemRewards.HasOtherItemReward}");
            Plugin.Log.Info($"Instanes: {questData.HasInstanceUnlocks} | BeastTribe: {questData.HasBeastTribeUnlock} | Job: {questData.HasJobUnlock}");

            Plugin.Log.Info($"Type: {questData.Quest.Type}");
            ///
            //if (questData.HasJobUnlock)
            //{
            //    Plugin.Log.Information($"Job: {questData.JobUnlock.Name}");
            //}

            //Plugin.Log.Information($"QR Other: {questData.Quest.QuestRewardOtherDisplay.Value.Name}");
            //Plugin.Log.Information($"CRY Other: {questData.Quest.CurrencyReward.Value.Name}");

            ////foreach (var item in questData.ItemRewards.RewardItems)
            ////{
            ////    Plugin.Log.Info($"Item: {item.Amount}x {item.Name} ({(item.IsHQ ? "HQ" : "NQ")}) {(item.HasStain ? item.Stain!.Value.Name : string.Empty)}");
            ////}
            Plugin.Log.Info($"^========[DEBUG]========^");
        }
    }
}