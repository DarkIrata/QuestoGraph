using Dalamud.Interface.Windowing;
using QuestoGraph.Data;
using QuestoGraph.Data.Settings;
using QuestoGraph.Services.Events;
using QuestoGraph.Windows;

namespace QuestoGraph.Manager
{
    internal class UIManager : IDisposable
    {
        private readonly Config config;
        private readonly QuestsManager questsManager;

        private readonly WindowSystem WindowSystem = new(Plugin.Name);
        private readonly MainWindow mainWindow;
        private readonly SettingsWindow settingsWindow;
        private readonly GraphWindow graphWindow;
        private readonly EventAggregator eventAggregator;

        public UIManager(Config config, QuestsManager questsManager, EventAggregator eventAggregator)
        {
            this.config = config;
            this.questsManager = questsManager;
            this.eventAggregator = eventAggregator;

            this.mainWindow = new MainWindow(this.config, this.questsManager, this, this.eventAggregator);
            this.settingsWindow = new SettingsWindow(this.config, this.questsManager, this, this.eventAggregator);
            this.graphWindow = new GraphWindow(this.config, this.questsManager, this.eventAggregator);
            this.WindowSystem.AddWindow(this.mainWindow);
            this.WindowSystem.AddWindow(this.settingsWindow);
            this.WindowSystem.AddWindow(this.graphWindow);

            Plugin.Interface.UiBuilder.Draw += this.DrawUI;
            Plugin.Interface.UiBuilder.OpenMainUi += this.ToggleMain;
            Plugin.Interface.UiBuilder.OpenConfigUi += this.ToggleSettings;
        }

        public void Dispose()
        {
            Plugin.Interface.UiBuilder.OpenMainUi -= this.ToggleMain;
            Plugin.Interface.UiBuilder.OpenConfigUi -= this.ToggleSettings;

            foreach (var window in this.WindowSystem.Windows)
            {
                if (window.IsOpen)
                {
                    window.Toggle();
                }
            }

            this.mainWindow.Dispose();
            this.settingsWindow.Dispose();
            this.graphWindow.Dispose();

            Plugin.Interface.UiBuilder.Draw -= this.DrawUI;
        }

        private void DrawUI() => this.WindowSystem.Draw();

        public void ToggleMain() => this.ToggleMain(null);

        public void ToggleMain(string? args)
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                args = null;
            }

            this.mainWindow.Prefilter(args);
            if (this.mainWindow.IsOpen)
            {
                if (string.IsNullOrEmpty(args))
                {
                    this.mainWindow.Toggle();
                }
            }
            else
            {
                this.mainWindow.Toggle();
            }
        }

        public void ToggleSettings() => this.settingsWindow.Toggle();

        public void ToggleGraph() => this.graphWindow.Toggle();

        public void ShowGraph(QuestData questData) => this.graphWindow.Show(questData);

        internal void RedrawGraph()
        {
            this.graphWindow.RedrawGraph();
        }
    }
}
