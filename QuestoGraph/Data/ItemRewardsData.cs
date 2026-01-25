using Dalamud.Game;
using Lumina.Excel.Sheets;

namespace QuestoGraph.Data
{
    internal class ItemRewardsData
    {
        public bool HasAnyItemRewards => this.HasOptionalItemRewards || this.HasCatalystItemRewards || this.HasDefaultItemRewards || this.HasOtherItemReward;

        public bool HasOptionalItemRewards => this.OptionalItems.Count != 0;

        public bool HasCatalystItemRewards => this.CatalystItems.Count != 0;

        public bool HasDefaultItemRewards => this.RewardItems.Count != 0;

        public bool HasOtherItemReward => this.OtherItem != null;

        public IReadOnlyList<ItemData> OptionalItems { get; } = [];

        public IReadOnlyList<ItemData> CatalystItems { get; } = [];

        public IReadOnlyList<ItemData> RewardItems { get; } = [];

        public ItemData? OtherItem { get; }

        public ItemRewardsData(Quest quest, ClientLanguage clientLanguage = ClientLanguage.English)
        {
            this.OptionalItems = this.ParseOptionalItems(quest, clientLanguage);
            this.CatalystItems = this.ParseCatalystItems(quest, clientLanguage);
            this.RewardItems = this.ParseRewardItems(quest, clientLanguage);
            this.OtherItem = this.ParseOtherItem(quest);
        }

        private ItemData? ParseOtherItem(Quest quest)
        {
            if (quest.OtherReward.RowId == 0)
            {
                return null;
            }

            var item = quest.OtherReward.Value;
            if (item.RowId == 0)
            {
                return null;
            }

            return new ItemData(item.RowId, item.Icon, item.Name.ExtractText(), 1, false, null);
        }

        private List<ItemData> ParseRewardItems(Quest quest, ClientLanguage clientLanguage = ClientLanguage.English)
        {
            var result = new List<ItemData>();
            for (int i = 0; i < quest.Reward.Count; i++)
            {
                var itemRow = quest.Reward[i];
                var itemId = itemRow.RowId;
                if (itemId == 0)
                {
                    continue;
                }

                var item = Plugin.DataManager.GetExcelSheet<Item>(clientLanguage)!.GetRow(itemId);
                var amount = quest.ItemCountReward[i];
                var stain = quest.RewardStain[i];

                //Plugin.Log.Info($"Reward: {amount}x {itemId} {(stain.RowId != 0 ? stain.Value.Name : string.Empty)}");
                var itemData = new ItemData(itemId, item.Icon, item.Name.ExtractText(), amount, false, stain.ValueNullable);
                result.Add(itemData);
            }

            return result;
        }

        private List<ItemData> ParseCatalystItems(Quest quest, ClientLanguage clientLanguage = ClientLanguage.English)
        {
            var result = new List<ItemData>();
            for (int i = 0; i < quest.ItemCatalyst.Count; i++)
            {
                var rowItem = quest.ItemCatalyst[i];
                var itemId = rowItem.RowId;
                if (itemId == 0)
                {
                    continue;
                }

                var item = Plugin.DataManager.GetExcelSheet<Item>(clientLanguage)!.GetRow(itemId);
                var amount = quest.ItemCountCatalyst[i];

                //Plugin.Log.Info($"Catalyst: {amount}x {name}");
                var itemData = new ItemData(itemId, item.Icon, item.Name.ExtractText(), amount, false, null);
                result.Add(itemData);
            }

            return result;
        }

        private List<ItemData> ParseOptionalItems(Quest quest, ClientLanguage clientLanguage = ClientLanguage.English)
        {
            var result = new List<ItemData>();
            for (int i = 0; i < quest.OptionalItemReward.Count; i++)
            {
                var rowItem = quest.OptionalItemReward[i];
                var itemId = rowItem.RowId;
                if (itemId == 0)
                {
                    continue;
                }

                var item = Plugin.DataManager.GetExcelSheet<Item>(clientLanguage)!.GetRow(itemId);
                var isHQ = quest.OptionalItemIsHQReward[i];
                var amount = quest.OptionalItemCountReward[i];
                var stain = quest.OptionalItemStainReward[i];

                //Plugin.Log.Info($"Optional: {amount}x {name} ({(isHQ ? "HQ" : "NQ")}) {(stain.RowId != 0 ? stain.Value.Name : string.Empty)}");
                var itemData = new ItemData(itemId, item.Icon, item.Name.ExtractText(), amount, isHQ, stain.ValueNullable);
                result.Add(itemData);
            }

            return result;
        }
    }
}
