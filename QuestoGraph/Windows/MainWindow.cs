using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
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
        private QuestData? selectedQuestData = null;

        public MainWindow(Config config, QuestsManager questsManager)
            : base($"{Plugin.Name} - Overview##Main View", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            this.config = config;
            this.questsManager = questsManager;

            this.SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(375, 100),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
            };
        }

        public void Dispose()
        {
        }

        public override void Draw()
        {
            using (var questsGroup = ImRaii.Group())
            {
                using (var container = ImRaii.Child("QuestsContainer", new Vector2(225, ImGui.GetContentRegionAvail().Y), true))
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
                                {
                                    if (questData.Name.Contains(this.filter, StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        var isSelected = this.selectedQuestData == questData;
                                        if (ImGui.Selectable(questData.Name, isSelected, ImGuiSelectableFlags.AllowDoubleClick))
                                        {
                                            this.selectedQuestData = questData;
                                        }

                                        if (isSelected)
                                        {
                                            ImGui.SetItemDefaultFocus();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

            }

            ImGui.SameLine();
            this.DrawSelectedQuestDetails();
        }

        private void DrawSelectedQuestDetails()
        {
            using (var questInfoGroup = ImRaii.Group())
            {
                using (var container = ImRaii.Child("QuestsInfoContainer", Vector2.Zero, true))
                {
                    if (container.Success)
                    {
                        if (this.selectedQuestData == null)
                        {
                            const string text = "♪ No quest selected ♫";
                            ImGui.SetCursorPos((ImGui.GetContentRegionAvail() - ImGui.CalcTextSize(text)) * 0.5f);
                            ImGui.TextUnformatted(text);

                            return;
                        }

                        ImGuiUtils.AddQuestImage(this.selectedQuestData);

                        ImGuiHelpers.ScaledDummy(1f, 1.5f);

                        this.DrawQuestMeta();
                        ImGui.Separator();

                        ImGuiUtils.AddIcon(65002, 20, 20, "Gil");
                        ImGui.TextUnformatted(this.selectedQuestData.GilReward.ToString());
                        ImGui.SameLine();
                        ImGuiHelpers.ScaledDummy(new Vector2(2f, 1f));
                        ImGui.SameLine();
                        ImGuiUtils.AddIcon(65001, 20, 20, "Exp");
                        ImGui.TextUnformatted(GameUtils.CalculateExp(this.selectedQuestData.Quest).ToString());

                        ImGui.Separator();
                        //using (var questChild = ImRaii.Child($"##Quest-{questData.RowId}", new Vector2(275, 55), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                        //{
                        //    //ImGui.Selectable(questData.Name, false, ImGuiSelectableFlags.AllowDoubleClick);
                        //    //ImGuiUtils.AddIcon(questData.Quest.Icon, null, 2f);
                        //    using (var questDetailsChild = ImRaii.Child($"##Quest-{questData.RowId}-details", new Vector2(275, 55), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                        //    {
                        //        if (questDetailsChild.Success)
                        //        {
                        //            ImGui.TextUnformatted(questData.Name);
                        //            ImGui.Separator();

                        //            const float unusualIconSize = 4.2f;
                        //            if (questData.ItemRewards.HasAnyItemRewards)
                        //            {
                        //                ImGuiUtils.AddIcon(000002, "Item Rewards", unusualIconSize);
                        //            }

                        //            if (questData.HasInstanceUnlocks)
                        //            {
                        //                foreach (var instanceUnlock in questData.InstanceUnlocks.Where(i => i.ContentFound))
                        //                {
                        //                    ImGuiUtils.AddIcon(instanceUnlock.ContentFinder.ContentType.Value.Icon, instanceUnlock.Name);
                        //                }
                        //            }

                        //            if (questData.HasEmoteReward)
                        //            {
                        //                ImGuiUtils.AddIcon(questData.Emote.Icon, questData.Emote.Name.ExtractText(), unusualIconSize);
                        //            }

                        //            if (questData.HasActionReward)
                        //            {
                        //                ImGuiUtils.AddIcon(questData.Action.Icon, questData.Action.Name.ExtractText(), unusualIconSize);
                        //            }

                        //            if (questData.HasGeneralActionRewards)
                        //            {
                        //                foreach (var generalAction in questData.GeneralActions.Where(ga => ga.Icon > 0))
                        //                {
                        //                    ImGuiUtils.AddIcon((uint)generalAction.Icon, generalAction.Name, unusualIconSize);
                        //                }
                        //            }

                        //            if (questData.HasBeastTribeUnlock)
                        //            {
                        //                ImGuiUtils.AddIcon(questData.BeastTribe.Icon, questData.BeastTribe.Name.ExtractText());
                        //            }
                        //        }
                        //    }
                        //}
                    }
                }
            }
        }

        private void DrawQuestMeta()
        {
            ImGuiUtils.AddIcon((uint)this.selectedQuestData!.Quest.JournalGenre.Value.Icon, 48, 48, setSameLine: true);
            var journalText = this.selectedQuestData.Quest.JournalGenre.IsValid ? this.selectedQuestData.Quest.JournalGenre.Value.Name.ExtractText() : string.Empty;
            var levelReqAndName = $"(Lvl: {this.selectedQuestData.Quest.ClassJobLevel[0]}) {this.selectedQuestData!.Name}";
            MapLinkPayload? issuerPayload = null;
            var location = "";
            // Map Info
            if (this.selectedQuestData.Quest.IssuerLocation.IsValid && this.selectedQuestData.Quest.IssuerLocation.RowId != 0)
            {
                issuerPayload = GameUtils.GetMapPayload(this.selectedQuestData.Quest.IssuerLocation.Value);
                location = $"{issuerPayload.PlaceName} {issuerPayload.CoordinateString}";
            }
            var metaInfo = $"{journalText}\n{levelReqAndName}\n{location}";
            ImGui.SameLine();
            ImGui.Text(metaInfo);

            if (issuerPayload != null)
            {
                using (ImRaii.PushFont(UiBuilder.IconFont))
                {
                    const float buttonSize = 24f;
                    ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X - buttonSize);
                    var tempY = ImGui.GetCursorPosY();
                    ImGui.SetCursorPosY(tempY - ImGui.CalcTextSize(metaInfo).Y);

                    if (ImGui.Button($"{FontAwesomeIcon.MapMarkerAlt.ToIconString()}##QuestsMarker_" + this.selectedQuestData.RowId, new Vector2(buttonSize, buttonSize)))
                    {
                        GameUtils.ShowMapPos(issuerPayload);
                    }
                    ImGui.SetCursorPosY(tempY);
                }
            }
        }
    }
}
