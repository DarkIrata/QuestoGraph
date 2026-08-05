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
        private ClientLanguage oldQuestNameLanguage;

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
            this.oldQuestNameLanguage = this.config.General.Language.QuestNames;
        }

        public override void OnClose()
        {
            base.OnClose();

            Plugin.Log.Info("Saving configuration");
            Plugin.Interface.SavePluginConfig(this.config);

            //this.questsManager.ReInitialize();
            this.questsManager.RefreshList();

            if (this.config.Graph.ShowArrowheads != this.oldShowArrowheads ||
                this.config.Graph.CompressMSQ != this.oldCompressMSQ ||
                this.config.General.Language.QuestNames != this.oldQuestNameLanguage)
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
            using (var indent = new ImRaii.IndentDisposable())
            {
                indent.Indent(1);
                ImGui.TextUnformatted("Quests will be refreshed / reloaded");
                ImGui.TextUnformatted("when you close settings.");
            }

            ImGuiUtils.SeperatorWithText("Display");
            using (var indent = new ImRaii.IndentDisposable())
            {
                indent.Indent(1);
                this.config.General.ShowQuestId = ImGuiUtils.Checkbox("Show Quest Id", this.config.General.ShowQuestId);
            }

            ImGuiUtils.SeperatorWithText("Language");
            using (var indent = new ImRaii.IndentDisposable())
            {
                indent.Indent(1);
                this.config.General.Language.QuestNames = ImGuiUtils.Combobox("Quests", this.config.General.Language.QuestNames, Plugin.DataManager.Language, Enum.GetValues<ClientLanguage>());
                this.config.General.Language.Rewards = ImGuiUtils.Combobox("Rewards", this.config.General.Language.Rewards, Plugin.DataManager.Language, Enum.GetValues<ClientLanguage>());
                this.config.General.Language.Instances = ImGuiUtils.Combobox("Instances", this.config.General.Language.Instances, Plugin.DataManager.Language, Enum.GetValues<ClientLanguage>());
            }
        }

        private void DrawFilterOption()
        {
            ImGuiUtils.SeperatorWithText("Display");
            using (var indent = new ImRaii.IndentDisposable())
            {
                indent.Indent(1);
                this.config.Display.ShowMSQQuests = ImGuiUtils.Checkbox("MSQ Quests", this.config.Display.ShowMSQQuests);
                this.config.Display.ShowNormalQuests = ImGuiUtils.Checkbox("Normal Quests", this.config.Display.ShowNormalQuests);
                this.config.Display.ShowBlueQuests = ImGuiUtils.Checkbox("Blue Quests", this.config.Display.ShowBlueQuests);
                this.config.Display.ShowEmoteQuests = ImGuiUtils.Checkbox("Unlocks Emotes", this.config.Display.ShowEmoteQuests);
                this.config.Display.ShowWithRewards = ImGuiUtils.Checkbox("Has Item Rewards", this.config.Display.ShowWithRewards);
                this.config.Display.ShowInstanceUnlocks = ImGuiUtils.Checkbox("Unlocks Instances", this.config.Display.ShowInstanceUnlocks);
                this.config.Display.ShowJobAndActionQuests = ImGuiUtils.Checkbox("Unlocks Job / Actions", this.config.Display.ShowJobAndActionQuests);
            }
        }

        private void DrawColorOption()
        {
            ImGuiUtils.SeperatorWithText("Sidebar");

            using (var indent = new ImRaii.IndentDisposable())
            {
                indent.Indent(1);
                this.config.Colors.SidebarDefaultColor = this.ColorEdit("Default Quest", "DefaultQuest", this.config.Colors.SidebarDefaultColor, this.backupConfig.Colors.SidebarDefaultColor);
                //this.config.Colors.SidebarCompletedColor = this.ColorEdit("Completed Quest", "DoneQuest", this.config.Colors.SidebarCompletedColor, this.backupConfig.Colors.SidebarCompletedColor);

                this.config.Colors.SidebarMSQColor = this.ColorEdit("MSQ Quest", "MSQQuest", this.config.Colors.SidebarMSQColor, this.backupConfig.Colors.SidebarMSQColor);
                //this.config.Colors.SidebarMSQCompletedColor = this.ColorEdit("Completed MSQ Quest", "DoneMSQQuest", this.config.Colors.SidebarMSQCompletedColor, this.backupConfig.Colors.SidebarMSQCompletedColor);

                this.config.Colors.SidebarBlueColor = this.ColorEdit("Blue Quest", "BlueQuest", this.config.Colors.SidebarBlueColor, this.backupConfig.Colors.SidebarBlueColor);
                //this.config.Colors.SidebarBlueCompletedColor = this.ColorEdit("Completed Blue Quest", "DoneBlueQuest", this.config.Colors.SidebarBlueCompletedColor, this.backupConfig.Colors.SidebarBlueCompletedColor);
            }

            ImGuiUtils.SeperatorWithText("Graph");
            using (var indent = new ImRaii.IndentDisposable())
            {
                indent.Indent(1);
                this.config.Colors.GraphDefaultBackgroundColor = this.ColorEdit("Default", "GraphDefault", this.config.Colors.GraphDefaultBackgroundColor, this.backupConfig.Colors.GraphDefaultBackgroundColor);
                this.config.Colors.GraphMSQBackgroundColor = this.ColorEdit("MSQ Quest", "GraphMSQ", this.config.Colors.GraphMSQBackgroundColor, this.backupConfig.Colors.GraphMSQBackgroundColor);
                this.config.Colors.GraphBlueBackgroundColor = this.ColorEdit("Blue Quest", "GraphBlue", this.config.Colors.GraphBlueBackgroundColor, this.backupConfig.Colors.GraphBlueBackgroundColor);

                this.config.Colors.GraphInitialQuestBorder = this.ColorEdit("Initial Quest Border", "GraphInitialQBorder", this.config.Colors.GraphInitialQuestBorder, this.backupConfig.Colors.GraphInitialQuestBorder);
                this.config.Colors.GraphHighlightedQuestBorder = this.ColorEdit("Highlighted Quest Border", "GraphHighlightedQBorder", this.config.Colors.GraphHighlightedQuestBorder, this.backupConfig.Colors.GraphHighlightedQuestBorder);

                this.config.Colors.GraphLineColor = this.ColorEdit("Line", "GraphLineColor", this.config.Colors.GraphLineColor, this.backupConfig.Colors.GraphLineColor);
                this.config.Colors.GraphLineSelectedColor = this.ColorEdit("Highlighted Line", "GraphLineSelectedColor", this.config.Colors.GraphLineSelectedColor, this.backupConfig.Colors.GraphLineSelectedColor);
            }
        }

        private void DrawGraphOption()
        {
            ImGuiUtils.SeperatorWithText("Display");
            using (var indent = new ImRaii.IndentDisposable())
            {
                indent.Indent(1);
                this.config.Graph.CompressMSQ = ImGuiUtils.Checkbox("Compress MSQ Quests", this.config.Graph.CompressMSQ);
                this.config.Graph.ShowArrowheads = ImGuiUtils.Checkbox("Show Arrowheads", this.config.Graph.ShowArrowheads);
            }
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
