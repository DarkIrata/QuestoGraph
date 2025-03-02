using Dalamud.Interface.Windowing;
using QuestoGraph.Data.Settings;
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

        public UIManager(Config config, QuestsManager questsManager)
        {
            this.config = config;
            this.questsManager = questsManager;

            this.settingsWindow = new SettingsWindow(this.config);
            this.mainWindow = new MainWindow(this.config, this.questsManager, this.WindowSystem);
            this.WindowSystem.AddWindow(this.mainWindow);
            this.WindowSystem.AddWindow(this.settingsWindow);

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
            this.mainWindow.Toggle();
        }

        public void ToggleSettings() => this.settingsWindow.Toggle();
    }
}
