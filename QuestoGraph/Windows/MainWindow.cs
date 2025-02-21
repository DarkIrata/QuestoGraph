using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Common.Math;
using ImGuiNET;
using QuestoGraph.Data;
using QuestoGraph.Manager;
using QuestoGraph.Utils;

namespace QuestoGraph.Windows
{
    internal class MainWindow : Window, IDisposable
    {
        private readonly Config config;
        private readonly QuestsManager questsManager;

        private string filter = string.Empty;

        public MainWindow(Config config, QuestsManager questsManager)
            : base($"{Plugin.Name} - Overview##Main View", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            this.config = config;
            this.questsManager = questsManager;

            this.SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(375, 100),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };
        }

        public void Dispose()
        {
        }

        public override void Draw()
        {
            ImGui.TextUnformatted($"All Quests:");

            using (var container = ImRaii.Child("QuestsContainer", Vector2.Zero, true))
            {
                if (container.Success)
                {
                    ImGui.InputTextWithHint("##NameFilter", "Search containing..", ref this.filter, 255);
                    ImGui.Separator();

                    using (var child = ImRaii.Child("##Quests", Vector2.Zero, false, ImGuiWindowFlags.HorizontalScrollbar))
                    {
                        if (child.Success)
                        {
                            foreach (var questData in this.questsManager.QuestData.Values)
                            //foreach (var questData in this.questsManager.QuestData.Values.Where(qd => qd.HasInstanceUnlocks ||
                            //qd.ItemRewards.HasAnyItemRewards ||
                            //qd.HasEmoteReward ||
                            //qd.HasGeneralActionRewards ||
                            //qd.HasBeastTribeUnlock ||
                            //qd.HasActionReward))
                            {
                                if (questData.Name.Contains(this.filter, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    using (var questChild = ImRaii.Child($"##Quest-{questData.RowId}", new Vector2(275, 55), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                                    {
                                        //ImGuiUtils.AddIcon(questData.Quest.Icon, null, 2f);
                                        using (var questDetailsChild = ImRaii.Child($"##Quest-{questData.RowId}-details", new Vector2(275, 55), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                                        {
                                            if (questDetailsChild.Success)
                                            {
                                                ImGui.TextUnformatted(questData.Name);
                                                ImGui.Separator();

                                                const float unusualIconSize = 4.2f;
                                                if (questData.ItemRewards.HasAnyItemRewards)
                                                {
                                                    ImGuiUtils.AddIcon(000002, "Item Rewards", unusualIconSize);
                                                }

                                                if (questData.HasInstanceUnlocks)
                                                {
                                                    foreach (var instanceUnlock in questData.InstanceUnlocks.Where(i => i.ContentFound))
                                                    {
                                                        ImGuiUtils.AddIcon(instanceUnlock.ContentFinder.ContentType.Value.Icon, instanceUnlock.Name);
                                                    }
                                                }

                                                if (questData.HasEmoteReward)
                                                {
                                                    ImGuiUtils.AddIcon(questData.Emote.Icon, questData.Emote.Name.ToString(), unusualIconSize);
                                                }

                                                if (questData.HasActionReward)
                                                {
                                                    ImGuiUtils.AddIcon(questData.Action.Icon, questData.Action.Name.ToString(), unusualIconSize);
                                                }

                                                if (questData.HasGeneralActionRewards)
                                                {
                                                    foreach (var generalAction in questData.GeneralActions.Where(ga => ga.Icon > 0))
                                                    {
                                                        ImGuiUtils.AddIcon((uint)generalAction.Icon, generalAction.Name, unusualIconSize);
                                                    }
                                                }

                                                if (questData.HasBeastTribeUnlock)
                                                {
                                                    ImGuiUtils.AddIcon(questData.BeastTribe.Icon, questData.BeastTribe.Name.ToString());
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
