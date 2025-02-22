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

        public ItemRewardsData(Quest quest)
        {
            this.OptionalItems = this.ParseOptionalItems(quest);
            this.CatalystItems = this.ParseCatalystItems(quest);
            this.RewardItems = this.ParseRewardItems(quest);
            this.OtherItem = this.ParseOtherItem(quest);
        }

        private ItemData? ParseOtherItem(Quest quest)
        {
            if (quest.OtherReward.RowId == 0)
            {
                return null;
            }

            var item = quest.OtherReward.Value;
            return new ItemData(item.RowId, item.Icon, item.Name.ExtractText(), 1, false, null);
        }

        private IReadOnlyList<ItemData> ParseRewardItems(Quest quest)
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

                var item = Plugin.DataManager.GetExcelSheet<Item>(Dalamud.Game.ClientLanguage.English)!.GetRow(itemId);
                var amount = quest.ItemCountReward[i];
                var stain = quest.RewardStain[i];

                //Plugin.Log.Info($"Reward: {amount}x {itemId} {(stain.RowId != 0 ? stain.Value.Name : string.Empty)}");
                var itemData = new ItemData(itemId, item.Icon, item.Name.ExtractText(), amount, false, stain.ValueNullable);
                result.Add(itemData);
            }

            return result;
        }

        private List<ItemData> ParseCatalystItems(Quest quest)
        {
            var result = new List<ItemData>();
            for (int i = 0; i < quest.ItemCatalyst.Count; i++)
            {
                var item = quest.ItemCatalyst[i];
                var itemId = item.RowId;
                if (itemId == 0)
                {
                    continue;
                }

                var amount = quest.ItemCountCatalyst[i];

                //Plugin.Log.Info($"Catalyst: {amount}x {name}");
                var itemData = new ItemData(itemId, item.Value.Icon, item.Value.Name.ExtractText(), amount, false, null);
                result.Add(itemData);
            }

            return result;
        }

        private List<ItemData> ParseOptionalItems(Quest quest)
        {
            var result = new List<ItemData>();
            for (int i = 0; i < quest.OptionalItemReward.Count; i++)
            {
                var item = quest.OptionalItemReward[i];
                var itemId = item.RowId;
                if (itemId == 0)
                {
                    continue;
                }

                var isHQ = quest.OptionalItemIsHQReward[i];
                var amount = quest.OptionalItemCountReward[i];
                var stain = quest.OptionalItemStainReward[i];

                //Plugin.Log.Info($"Optional: {amount}x {name} ({(isHQ ? "HQ" : "NQ")}) {(stain.RowId != 0 ? stain.Value.Name : string.Empty)}");
                var itemData = new ItemData(itemId, item.Value.Icon, item.Value.Name.ExtractText(), amount, isHQ, stain.ValueNullable);
                result.Add(itemData);
            }

            return result;
        }
    }
}
