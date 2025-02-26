using Lumina.Excel.Sheets;

namespace QuestoGraph.Data
{
    internal class QuestData
    {
        public uint RowId => this.Quest.RowId;

        public string Name => this.Quest.Name.ExtractText();

        public Quest Quest { get; }

        public uint GilReward => this.Quest.GilReward;

        public bool HasEmoteReward => this.Quest.EmoteReward.RowId != 0;

        public Emote Emote => this.Quest.EmoteReward.Value;

        public bool HasActionReward => this.Quest.ActionReward.RowId != 0;

        public Lumina.Excel.Sheets.Action Action => this.Quest.ActionReward.Value;

        public bool HasGeneralActionRewards => this.GeneralActions.Count != 0;

        public IReadOnlyList<GeneralActionData> GeneralActions { get; } = [];

        public ItemRewardsData ItemRewards { get; }

        public bool HasInstanceUnlocks => this.InstanceUnlocks.Count != 0;

        public IReadOnlyList<InstanceData> InstanceUnlocks { get; } = [];

        public bool HasBeastTribeUnlock => this.Quest.BeastTribe.RowId != 0 && !this.Quest.IsRepeatable && this.Quest.BeastReputationRank.RowId == 0;

        public BeastTribe BeastTribe => this.Quest.BeastTribe.Value;

        public bool HasJobUnlock => this.JobUnlock.RowId != 0;

        public ClassJob JobUnlock { get; }

        public IReadOnlyList<uint> PreviousQuestsId { get; private set; } = [];

        public IReadOnlyList<uint> NextQuestIds { get; private set; } = [];

        // Unkown12 == true => Unreachable? 
        // It looks like a Quest Replaced / Discontinued flag. Every entry currently set by this, was not reachable
        // from what i could see
        public bool IsReachable => !this.Quest.Unknown12 || (!this.Quest.IssuerLocation.IsValid && this.Quest.IssuerLocation.RowId != 0);

        public QuestData(Quest quest)
        {
            this.Quest = quest;

            this.SetPreviousQuests();

            this.ItemRewards = new ItemRewardsData(quest);

            this.GeneralActions = quest.GeneralActionReward
                .Where(ga => ga.IsValid && ga.RowId != 0)
                .Select(ga => new GeneralActionData(ga.Value))
                .ToList();

            this.InstanceUnlocks = this.ParseInstanceUnlocks(quest);
            this.JobUnlock = this.ParseJobUnlock(quest);
        }

        private void SetPreviousQuests()
        {
            if (this.Quest.PreviousQuest.Count > 0)
            {
                var prevQuests = new List<uint>();
                prevQuests.AddRange(this.Quest.PreviousQuest.Where(pq => pq.IsValid && pq.RowId != 0).Select(pq => pq.RowId));
                this.PreviousQuestsId = prevQuests;
            }
        }

        private ClassJob ParseJobUnlock(Quest quest)
        {
            if (quest.ClassJobUnlock.RowId > 0)
            {
                return quest.ClassJobUnlock.Value;
            }

            const string ClassJobScriptInstruct = "CLASSJOB";
            if (quest.QuestParams.Any(param => param.ScriptInstruction.ExtractText().Contains(ClassJobScriptInstruct)))
            {
                var classParam = quest.QuestParams.FirstOrDefault(param => param.ScriptInstruction.ExtractText().StartsWith(ClassJobScriptInstruct));
                try
                {
                    return Plugin.DataManager.GetExcelSheet<ClassJob>()!.GetRow(classParam.ScriptArg);
                }
                catch
                {
                    Plugin.Log.Error($"Failed parsing {nameof(this.JobUnlock)} for quest ({this.RowId}) {this.Name}");
                }
            }
            else if (this.ItemRewards.HasOtherItemReward &&
                (this.ItemRewards.OtherItem!.RowId <= 16 && this.ItemRewards.OtherItem!.RowId >= 10)) // ../csv/QuestRewardOther.csv
            {
                // To get older classes, we check the other reward to find the soul stone.
                // We cant trust the naming, since ClientLanguage Settings could become a feature...
                // Also Other Items do different RowIds..... 
                var item = Plugin.DataManager.GetExcelSheet<Item>(Dalamud.Game.ClientLanguage.English)!.FirstOrDefault(i => i.Icon == this.ItemRewards.OtherItem.Icon);
                if (item.ClassJobUse.RowId != 0)
                {
                    return item.ClassJobUse.Value;
                }
            }

            return default;
        }

        private List<InstanceData> ParseInstanceUnlocks(Quest quest)
        {
            var instanceUnlocks = new List<InstanceData>();
            if (!quest.IsRepeatable) // Parse Instance Unlocks
            {
                const string InstanceScriptInstruct = "INSTANCEDUNGEON";

                instanceUnlocks.AddRange(quest.QuestParams
                    .Where(param => param.ScriptInstruction.ExtractText().Contains(InstanceScriptInstruct))
                    .Select(qp => new InstanceData(qp)));

                if (quest.InstanceContentUnlock.RowId != 0 && !instanceUnlocks.Any(iu => iu.ContentRowId == quest.InstanceContentUnlock.RowId))
                {
                    instanceUnlocks.Add(new InstanceData(quest.InstanceContentUnlock.RowId));
                }
            }

            return instanceUnlocks;
        }

        internal void AppendNextQuests(IEnumerable<uint> nextQuests)
        {
            var temp = this.NextQuestIds.ToList();
            temp.AddRange(nextQuests);
            this.NextQuestIds = temp;
        }
    }
}
