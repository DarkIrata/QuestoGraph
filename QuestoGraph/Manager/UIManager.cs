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
        private readonly DebugWindow debugWindow;

        public UIManager(Config config, QuestsManager questsManager)
        {
            this.config = config;
            this.questsManager = questsManager;

            this.mainWindow = new MainWindow(this.config, this.questsManager, this.WindowSystem);
            this.settingsWindow = new SettingsWindow(this.config, this.questsManager);
            this.debugWindow = new DebugWindow(this.config, this.questsManager);
            this.WindowSystem.AddWindow(this.mainWindow);
            this.WindowSystem.AddWindow(this.settingsWindow);
            this.WindowSystem.AddWindow(this.debugWindow);

            Plugin.Interface.UiBuilder.Draw += this.DrawUI;
            Plugin.Interface.UiBuilder.OpenMainUi += this.ToggleMain;
            Plugin.Interface.UiBuilder.OpenConfigUi += this.ToggleSettings;

#if DEBUG
            //this.ToggleMain();
            //this.ToggleSettings();
            //this.ToggleDebug();
#endif
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
            this.debugWindow.Dispose();

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

        public void ToggleDebug() => this.debugWindow.Toggle();
    }
}
