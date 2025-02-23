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
                            foreach (var questData in this.questsManager.QuestData.Values) // FILTER HERE
                            {
                                var isSelected = this.selectedQuestData == questData;
                                if (questData.Name.Contains(this.filter, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    if (ImGui.Selectable(questData.Name, isSelected, ImGuiSelectableFlags.AllowDoubleClick))
                                    {
                                        this.selectedQuestData = questData;
                                    }
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

            ImGui.SameLine();
            this.DrawSelectedQuestDetails();
        }

        private void DrawSelectedQuestDetails()
        {
            using (var container = ImRaii.Child("QuestsInfoContainer", Vector2.Zero, true))
            {
                if (container.Success)
                {
                    using (var fp = new ImGuiUtils.FreeCursorPos())  // Cause Pingu - NOOT NOOT
                    {
                        var pinguIcon = ImGuiUtils.GetIcon(234564);
                        if (pinguIcon != null)
                        {
                            const ushort size = 64;
                            var regAvail = ImGui.GetContentRegionAvail();
                            fp.SetPos(regAvail.X - size, regAvail.Y - size);
                            ImGui.Image(pinguIcon.ImGuiHandle, new Vector2(size, size), Vector2.Zero, Vector2.One, new Vector4(1f, 1f, 1f, 0.25f));
                        }
                    }

                    if (this.selectedQuestData == null)
                    {
                        const string text = "♪ No quest selected ♫";
                        ImGui.SetCursorPos((ImGui.GetContentRegionAvail() - ImGui.CalcTextSize(text)) * 0.5f);
                        ImGui.TextUnformatted(text);

                        return;
                    }

                    ImGuiUtils.AddQuestImage(this.selectedQuestData);

                    ImGuiHelpers.ScaledDummy(1f, 1.5f);

                    this.DrawQuestMeta(this.selectedQuestData);
                    ImGui.Separator();

                    this.DrawRewards(this.selectedQuestData);
                    ImGui.Separator();

                    this.ShowPreviousQuests(this.selectedQuestData);
                }
            }
        }

        private void ShowPreviousQuests(QuestData questData)
        {
            if (questData.Quest.PreviousQuest.Count > 0)
            {
                ImGui.TextUnformatted("Previous Quests:");
                using (var child = ImRaii.Child("##PrevQuests", Vector2.Zero, false, ImGuiWindowFlags.HorizontalScrollbar))
                {
                    if (child.Success)
                    {
                        foreach (var prevQuest in questData.Quest.PreviousQuest)
                        {
                            if (prevQuest.RowId > 0)
                            {
                                var prevQuestData = this.questsManager.QuestData[prevQuest.RowId];
                                if (ImGui.Selectable(prevQuestData.Name))
                                {
                                    this.selectedQuestData = prevQuestData;
                                    this.filter = string.Empty;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DrawRewards(QuestData questData)
        {
            var dummySize = new Vector2(2f, 1f);
            const short iconSize = 24;


            ImGuiUtils.AddIcon(65002, iconSize, iconSize, "Gil");
            ImGui.TextUnformatted(questData.GilReward.ToString());

            ImGui.SameLine();
            ImGuiUtils.AddIcon(65001, iconSize, iconSize, "Exp");
            ImGui.TextUnformatted(GameUtils.CalculateExp(questData.Quest).ToString());

            if (questData.HasInstanceUnlocks)
            {
                ImGui.SameLine();
                var unlocks = questData.InstanceUnlocks.Where(i => i.ContentFound);
                var count = unlocks.Count();
                for (int i = 0; i < count; i++)
                {
                    var unlock = questData.InstanceUnlocks[i];
                    ImGuiUtils.AddIcon(unlock.ContentFinder.ContentType.Value.Icon, unlock.Name, 2.5f, i != (count - 1));
                }
            }

            if (questData.HasEmoteReward)
            {
                ImGui.SameLine();
                ImGuiUtils.AddIcon(questData.Emote.Icon, iconSize, iconSize, questData.Emote.Name.ExtractText(), false);
            }

            if (questData.HasActionReward)
            {
                ImGui.SameLine();
                ImGuiUtils.AddIcon(questData.Action.Icon, iconSize, iconSize, questData.Action.Name.ExtractText(), false);
            }

            if (questData.HasGeneralActionRewards)
            {
                ImGui.SameLine();
                var rewardsPos = ImGui.GetCursorPos();
                using (var freePos = new ImGuiUtils.FreeCursorPos(ImGuiUtils.FreeCursorPos.CursorReset.Y))
                {
                    const short iconModifier = 5;
                    var targetIconSize = iconSize + iconModifier;
                    var actions = questData.GeneralActions.Where(ga => ga.Icon > 0).ToArray();
                    var lastX = rewardsPos.X - (iconModifier / 2) + dummySize.X;
                    for (int i = 0; i < actions.Length; i++)
                    {
                        var newX = lastX + (i == 0 ? 0 : (i * targetIconSize) + (iconModifier / 2) + (2 * dummySize.X));
                        lastX = newX;

                        freePos.SetX(newX);
                        freePos.SetY(rewardsPos.Y - (iconModifier / 2));

                        var action = actions[i];
                        ImGuiUtils.AddIcon(
                            (uint)action.Icon,
                            targetIconSize,
                            targetIconSize,
                            $"Unlocks {action.Name}",
                            false);
                    }
                }
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 7f);
                ImGui.NewLine();
            }

            if (questData.HasBeastTribeUnlock)
            {
                ImGui.SameLine();
                var rewardsPos = ImGui.GetCursorPos();
                using (var freePos = new ImGuiUtils.FreeCursorPos(ImGuiUtils.FreeCursorPos.CursorReset.Y))
                {
                    const short iconModifier = 5;
                    var targetIconSize = iconSize + iconModifier;

                    freePos.SetX(rewardsPos.X - (iconModifier / 2) + dummySize.X);
                    freePos.SetY(rewardsPos.Y - (iconModifier / 2));
                    ImGuiUtils.AddIcon(
                        questData.BeastTribe.Icon,
                        targetIconSize,
                        targetIconSize,
                        $"Unlocks {questData.BeastTribe.Name.ExtractText()}",
                        false);
                }
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 7f);
                ImGui.NewLine();
            }

            if (questData.HasJobUnlock)
            {
                ImGui.SameLine();
                var rewardsPos = ImGui.GetCursorPos();
                using (var freePos = new ImGuiUtils.FreeCursorPos(ImGuiUtils.FreeCursorPos.CursorReset.Y))
                {
                    const short jobIconModifier = 11;
                    var jobIconSize = iconSize + jobIconModifier;

                    freePos.SetX(rewardsPos.X - (jobIconModifier / 2) + dummySize.X);
                    freePos.SetY(rewardsPos.Y - (jobIconModifier / 2));
                    ImGuiUtils.AddIcon(
                        GameUtils.GetJobIconId(questData.JobUnlock),
                        jobIconSize,
                        jobIconSize,
                        $"Unlocks {questData.JobUnlock.NameEnglish.ExtractText()}",
                        false);
                }
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 7f);
                ImGui.NewLine();
            }

            if (questData.ItemRewards.HasAnyItemRewards)
            {
                ImGui.Separator();
                void AddItem(ItemData? item)
                {
                    if (item == null)
                    {
                        return;
                    }

                    var tooltip = $"{item!.Amount}x {item.Name}";
                    if (item.IsHQ)
                    {
                        tooltip += " (HQ)";
                    }

                    const ushort size = 36;
                    ImGuiUtils.AddIcon(item.Icon, size, size, tooltip);
                }

                void AddItems(IReadOnlyList<ItemData> items)
                {
                    foreach (var item in items)
                    {
                        AddItem(item);
                    }
                }

                AddItems(questData.ItemRewards.CatalystItems);
                AddItems(questData.ItemRewards.RewardItems);
                AddItems(questData.ItemRewards.OptionalItems);
                AddItem(questData.ItemRewards.OtherItem);
                ImGui.NewLine();
            }
        }

        private void DrawQuestMeta(QuestData questData)
        {
            ImGuiUtils.AddIcon((uint)questData.Quest.JournalGenre.Value.Icon, 48, 48, setSameLine: true);

            var journalText = questData.Quest.JournalGenre.IsValid ? questData.Quest.JournalGenre.Value.Name.ExtractText() : string.Empty;
            var levelReqAndName = $"(Lvl: {questData.Quest.ClassJobLevel[0]}) {questData!.Name}";
            MapLinkPayload? issuerPayload = null;
            var location = "";

            // Map Info
            if (questData.Quest.IssuerLocation.IsValid && questData.Quest.IssuerLocation.RowId != 0)
            {
                issuerPayload = GameUtils.GetMapPayload(questData.Quest.IssuerLocation.Value);
                location = $"{issuerPayload.PlaceName} {issuerPayload.CoordinateString}";
            }
            var metaInfo = $"{journalText}\n{levelReqAndName}\n{location}";

            ImGui.SameLine();
            ImGui.Text(metaInfo);

            if (issuerPayload != null)
            {
                using (ImRaii.PushFont(UiBuilder.IconFont))
                using (var freePos = new ImGuiUtils.FreeCursorPos(ImGuiUtils.FreeCursorPos.CursorReset.Y))
                {
                    const float buttonSize = 24f;
                    freePos.SetX(ImGui.GetContentRegionAvail().X - buttonSize);
                    freePos.SetY(freePos.LastPos.Y - ImGui.CalcTextSize(metaInfo).Y);
                    if (ImGui.Button($"{FontAwesomeIcon.MapMarkerAlt.ToIconString()}##QuestsMarker_" + questData.RowId, new Vector2(buttonSize, buttonSize)))
                    {
                        GameUtils.ShowMapPos(issuerPayload);
                    }
                }
            }
        }
    }
}
