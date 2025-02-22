using Lumina.Excel.Sheets;
using QuestoGraph.Data;

namespace QuestoGraph.Manager
{
    internal class QuestsManager
    {
        public IReadOnlyDictionary<uint, QuestData> QuestData { get; private set; } = new Dictionary<uint, QuestData>();

        public QuestsManager()
        {
            this.Initialize();
            this.Debug();
        }

        private void Initialize()
        {
            Plugin.Log.Info($"Initializing..");
            var result = new Dictionary<uint, QuestData>();
            foreach (var quest in Plugin.DataManager.GetExcelSheet<Quest>(Dalamud.Game.ClientLanguage.English))
            {
                if (string.IsNullOrEmpty(quest.Name.ExtractText()))
                {
                    continue;
                }

                var questData = new QuestData(quest);
                result.Add(questData.RowId, questData);

                //var (_, nodes) = Node<Quest>.BuildTree(allQuests);
                //this.AllNodes = nodes;
            }
            this.QuestData = result;

            Plugin.Log.Info($"{this.QuestData.Count} Quests loaded");
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