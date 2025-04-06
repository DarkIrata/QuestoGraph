using System.Numerics;
using System.Reflection;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using QuestoGraph.Data.Settings;
using QuestoGraph.Utils;

namespace QuestoGraph.Windows
{
    internal class SettingsWindow : Window
    {
        private enum Options
        {
            Filter,
            Colors,
            About,
        }

        private Options selectedOption = Options.Filter;

        private readonly Config config;
        private readonly Config backupConfig = new();
        private readonly Version assemblyVersion = Assembly.GetExecutingAssembly()?.GetName()?.Version ?? new Version(0, 0);

        public SettingsWindow(Config config)
            : base($"{Plugin.Name} - Settings##Settings", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoResize)
        {
            this.config = config;

            var windowSize = new Vector2(375, 310);
            this.SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = windowSize,
                MaximumSize = windowSize,
            };
        }

        public override void OnClose()
        {
            base.OnClose();

            Plugin.Log.Info("Saving configuration");
            Plugin.Interface.SavePluginConfig(this.config);
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
                    case Options.Filter:
                        this.DrawFilterOptions();
                        break;
                    case Options.Colors:
                        this.DrawColorOptions();
                        break;
                    case Options.About:
                    default:
                        this.DrawAbout();
                        break;
                }
            }
        }

        private void DrawAbout()
        {
            ImGuiHelpers.CenteredText("•°*•._ Quest'o'Graph _.•*°•");
            ImGuiHelpers.CenteredText($"v{this.assemblyVersion}");
            ImGuiHelpers.CenteredText("A rewritten successor to QuestMap");
            ImGuiHelpers.CenteredText("-.,_,.='``'-.,_,.-'``'=.,_,.-");
            ImGuiHelpers.ScaledDummy(1f, 5f);
            ImGuiHelpers.CenteredText("Bugs or Improvement? Submit it!");
            ImGuiHelpers.CenteredText("Support me and my projects at Ko-Fi");
            ImGuiHelpers.ScaledDummy(1f, 8f);
            ImGui.TextUnformatted("SPECIAL THANKS");
            ImGui.BulletText("anna - QuestMap Creator");
            ImGui.BulletText("#?%$# - Plugin Icon");
            ImGui.BulletText("All the testers");
        }

        private void DrawFilterOptions()
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

        private void DrawColorOptions()
        {
            ImGuiUtils.SeperatorWithText("Sidebar");

            this.config.Colors.SidebarDefaultColor = this.ColorEdit("Default Quest", "Default", this.config.Colors.SidebarDefaultColor, this.backupConfig.Colors.SidebarDefaultColor);
            this.config.Colors.SidebarCompletedColor = this.ColorEdit("Completed Quest", "DoneQuest", this.config.Colors.SidebarCompletedColor, this.backupConfig.Colors.SidebarCompletedColor);
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

        private Vector4 ColorEdit(string text, string resetButtonSuffix, Vector4 color, Vector4 reset)
        {
            const string resetButtonText = "Reset";

            var temp = color;
            if (ImGui.ColorEdit4(text, ref temp, ImGuiColorEditFlags.NoInputs))
            {
                return temp;
            }

            ImGui.SameLine();
            if (ImGui.Button($"{resetButtonText}##{resetButtonSuffix}"))
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
