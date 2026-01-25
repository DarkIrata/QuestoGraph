using System.Numerics;
using System.Reflection;
using Dalamud.Bindings.ImGui;
using Dalamud.Game;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using QuestoGraph.Data.Settings;
using QuestoGraph.Manager;
using QuestoGraph.Services.Events;
using QuestoGraph.Utils;

namespace QuestoGraph.Windows
{
    internal class SettingsWindow : Window
    {
        private enum Options
        {
            General,
            Filter,
            Colors,
            Graph,
            About,
        }

        private readonly Config config;
        private readonly Config backupConfig = new();
        private readonly Version assemblyVersion = Assembly.GetExecutingAssembly()?.GetName()?.Version ?? new Version(0, 0);
        private readonly QuestsManager questsManager;
        private readonly UIManager uiManager;
        private readonly EventAggregator eventAggregator;

        private Options selectedOption = Options.General;
        private bool oldShowArrowheads = true;
        private bool oldCompressMSQ = true;

        public SettingsWindow(Config config, QuestsManager questsManager, UIManager uiManager, Services.Events.EventAggregator eventAggregator)
            : base($"{Plugin.Name} - Settings##Settings", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoResize)
        {
            this.config = config;
            this.questsManager = questsManager;
            this.uiManager = uiManager;
            this.eventAggregator = eventAggregator;

            var windowSize = new Vector2(375, 310);
            this.SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = windowSize,
                MaximumSize = windowSize,
            };
        }

        public override void OnOpen()
        {
            base.OnOpen();

            this.oldShowArrowheads = this.config.Graph.ShowArrowheads;
            this.oldCompressMSQ = this.config.Graph.CompressMSQ;
        }

        public override void OnClose()
        {
            base.OnClose();

            Plugin.Log.Info("Saving configuration");
            Plugin.Interface.SavePluginConfig(this.config);

            this.questsManager.ReInitialize();

            if (this.config.Graph.ShowArrowheads != this.oldShowArrowheads ||
                this.config.Graph.CompressMSQ != this.oldCompressMSQ)
            {
                this.uiManager.RedrawGraph();
            }
        }

        public void Dispose()
        {
        }

        public override void Draw()
        {
            ImGuiUtils.DrawPingu();
            var avail = ImGui.GetContentRegionAvail();
            using (var sidear = ImRaii.Child("##sidebar", new Vector2(75, avail.Y), false))
            {
                this.DrawSidebar();
            }

            ImGui.SameLine();
            using (var line = ImRaii.Child("##line", new Vector2(1f, avail.Y), true))
            {
            }

            ImGui.SameLine();
            using (var option = ImRaii.Child("##option", Vector2.Zero, false))
            {
                switch (this.selectedOption)
                {
                    case Options.General:
                        this.DrawGeneralOption();
                        break;
                    case Options.Filter:
                        this.DrawFilterOption();
                        break;
                    case Options.Colors:
                        this.DrawColorOption();
                        break;
                    case Options.Graph:
                        this.DrawGraphOption();
                        break;
                    case Options.About:
                    default:
                        this.DrawAboutOption();
                        break;
                }
            }
        }

        private void DrawAboutOption()
        {
            ImGuiHelpers.CenteredText("•°*•._ Quest'o'Graph _.•*°•");
            ImGuiHelpers.CenteredText($"v{this.assemblyVersion}");
            ImGuiHelpers.CenteredText("A (WIP) mostly rewritten ");
            ImGuiHelpers.CenteredText("   successor to QuestMap");
            ImGuiHelpers.CenteredText("-.,_,.='``'-.,_,.-'``'=.,_,.-");
            ImGuiHelpers.ScaledDummy(1f, 5f);
            ImGuiHelpers.CenteredText("Bugs or Improvement? Submit it!");
            ImGuiHelpers.CenteredText("Support me and my projects at Ko-Fi");
            ImGuiHelpers.ScaledDummy(1f, 8f);
            ImGui.TextUnformatted("SPECIAL THANKS");
            ImGui.BulletText("anna - QuestMap Creator");
            ImGui.BulletText("Azure Gem - QuestMap Contributor");
            ImGui.BulletText("celocne - Plugin Icon");
            ImGui.BulletText("All the testers");
        }

        private void DrawGeneralOption()
        {
            ImGuiUtils.SeperatorWithText("Info");
            using (var indent = new ImRaii.Indent())
            {
                indent.Push(1);
                ImGui.TextUnformatted("Quests will be refreshed / reloaded");
                ImGui.TextUnformatted("when you close settings.");
            }

            ImGuiUtils.SeperatorWithText("Language");
            using (var indent = new ImRaii.Indent())
            {
                indent.Push(1);
                this.config.General.Language.QuestNames = this.Combobox("Quests", this.config.General.Language.QuestNames, Plugin.DataManager.Language, Enum.GetValues<ClientLanguage>());
                this.config.General.Language.Rewards = this.Combobox("Rewards", this.config.General.Language.Rewards, Plugin.DataManager.Language, Enum.GetValues<ClientLanguage>());
                this.config.General.Language.Instances = this.Combobox("Instances", this.config.General.Language.Instances, Plugin.DataManager.Language, Enum.GetValues<ClientLanguage>());
            }
        }

        private void DrawFilterOption()
        {
            ImGuiUtils.SeperatorWithText("Display");
            using (var indent = new ImRaii.Indent())
            {
                indent.Push(1);
                this.config.Display.ShowMSQQuests = this.Checkbox("MSQ Quests", this.config.Display.ShowMSQQuests);
                this.config.Display.ShowNormalQuests = this.Checkbox("Normal Quests", this.config.Display.ShowNormalQuests);
                this.config.Display.ShowBlueQuests = this.Checkbox("Blue Quests", this.config.Display.ShowBlueQuests);
                this.config.Display.ShowEmoteQuests = this.Checkbox("Unlocks Emotes", this.config.Display.ShowEmoteQuests);
                this.config.Display.ShowWithRewards = this.Checkbox("Has Item Rewards", this.config.Display.ShowWithRewards);
                this.config.Display.ShowInstanceUnlocks = this.Checkbox("Unlocks Instances", this.config.Display.ShowInstanceUnlocks);
                this.config.Display.ShowJobAndActionQuests = this.Checkbox("Unlocks Job / Actions", this.config.Display.ShowJobAndActionQuests);
            }

            ImGuiUtils.SeperatorWithText("Search");
            using (var indent = new ImRaii.Indent())
            {
                indent.Push(1);
                this.config.Search.IncludeItems = this.Checkbox("Include Items", this.config.Search.IncludeItems);
                this.config.Search.IncludeEmotes = this.Checkbox("Include Emotes", this.config.Search.IncludeEmotes);
                this.config.Search.IncludeInstances = this.Checkbox("Include Instances", this.config.Search.IncludeInstances);
                this.config.Search.IncludeActions = this.Checkbox("Include Actions", this.config.Search.IncludeActions);
            }
        }

        private void DrawColorOption()
        {
            ImGuiUtils.SeperatorWithText("Sidebar");

            using (var indent = new ImRaii.Indent())
            {
                indent.Push(1);
                this.config.Colors.SidebarDefaultColor = this.ColorEdit("Default Quest", "DefaultQuest", this.config.Colors.SidebarDefaultColor, this.backupConfig.Colors.SidebarDefaultColor);
                //this.config.Colors.SidebarCompletedColor = this.ColorEdit("Completed Quest", "DoneQuest", this.config.Colors.SidebarCompletedColor, this.backupConfig.Colors.SidebarCompletedColor);

                this.config.Colors.SidebarMSQColor = this.ColorEdit("MSQ Quest", "MSQQuest", this.config.Colors.SidebarMSQColor, this.backupConfig.Colors.SidebarMSQColor);
                //this.config.Colors.SidebarMSQCompletedColor = this.ColorEdit("Completed MSQ Quest", "DoneMSQQuest", this.config.Colors.SidebarMSQCompletedColor, this.backupConfig.Colors.SidebarMSQCompletedColor);

                this.config.Colors.SidebarBlueColor = this.ColorEdit("Blue Quest", "BlueQuest", this.config.Colors.SidebarBlueColor, this.backupConfig.Colors.SidebarBlueColor);
                //this.config.Colors.SidebarBlueCompletedColor = this.ColorEdit("Completed Blue Quest", "DoneBlueQuest", this.config.Colors.SidebarBlueCompletedColor, this.backupConfig.Colors.SidebarBlueCompletedColor);
            }

            ImGuiUtils.SeperatorWithText("Graph");
            using (var indent = new ImRaii.Indent())
            {
                indent.Push(1);
                this.config.Colors.GraphDefaultBackgroundColor = this.ColorEdit("Default", "GraphDefault", this.config.Colors.GraphDefaultBackgroundColor, this.backupConfig.Colors.GraphDefaultBackgroundColor);
                this.config.Colors.GraphMSQBackgroundColor = this.ColorEdit("MSQ Quest", "GraphMSQ", this.config.Colors.GraphMSQBackgroundColor, this.backupConfig.Colors.GraphMSQBackgroundColor);
                this.config.Colors.GraphBlueBackgroundColor = this.ColorEdit("Blue Quest", "GraphBlue", this.config.Colors.GraphBlueBackgroundColor, this.backupConfig.Colors.GraphBlueBackgroundColor);

                this.config.Colors.GraphInitialQuestBorder = this.ColorEdit("Initial Quest", "GraphInitialQBorder", this.config.Colors.GraphInitialQuestBorder, this.backupConfig.Colors.GraphInitialQuestBorder);
                this.config.Colors.GraphHighlightedQuestBorder = this.ColorEdit("Highlighted Quest", "GraphHighlightedQBorder", this.config.Colors.GraphHighlightedQuestBorder, this.backupConfig.Colors.GraphHighlightedQuestBorder);
            }
        }

        private void DrawGraphOption()
        {
            ImGuiUtils.SeperatorWithText("Display");
            using (var indent = new ImRaii.Indent())
            {
                indent.Push(1);
                this.config.Graph.CompressMSQ = this.Checkbox("Compress MSQ Quests", this.config.Graph.CompressMSQ);
                this.config.Graph.ShowArrowheads = this.Checkbox("Show Arrowheads", this.config.Graph.ShowArrowheads);
            }
        }

        private bool Checkbox(string label, bool state)
        {
            var temp = state;
            if (ImGui.Checkbox(label, ref temp))
            {
                return temp;
            }

            return state;
        }

        private T Combobox<T>(string label, T currentValue, T fallbackValue, params T[] items)
        {
            if (items.Length < 1)
            {
                return currentValue;
            }

            var selectedIndex = items.IndexOf(currentValue);
            if (selectedIndex < 0)
            {
                selectedIndex = items.IndexOf(fallbackValue);
                if (selectedIndex < 0)
                {
                    selectedIndex = 0;
                }
            }

            if (ImGui.BeginCombo(label, items[selectedIndex]?.ToString()))
            {
                for (int i = 0; i < items.Length; i++)
                {
                    var isSelected = (i == selectedIndex);
                    if (ImGui.Selectable(items[i]?.ToString(), isSelected))
                    {
                        selectedIndex = i;
                    }

                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }

                ImGui.EndCombo();
            }

            return items[selectedIndex];
        }

        private Vector4 ColorEdit(string text, string suffix, Vector4 color, Vector4 reset)
        {
            const string resetButtonText = "Reset";

            var temp = color;
            if (ImGui.ColorEdit4($"{text}##{suffix}", ref temp, ImGuiColorEditFlags.NoInputs))
            {
                return temp;
            }

            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - ImGui.CalcTextSize(resetButtonText).X - 10);
            if (ImGui.Button($"{resetButtonText}##{suffix}"))
            {
                return reset;
            }

            ImGuiHelpers.ScaledDummy(0f, 1f);
            return color;
        }

        private void DrawSidebar()
        {
            foreach (var option in Enum.GetValues<Options>())
            {
                if (ImGui.Selectable(" " + option.ToString(), this.selectedOption == option))
                {
                    this.selectedOption = option;
                }
            }
        }
    }
}
