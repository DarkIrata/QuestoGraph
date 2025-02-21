using Dalamud.Interface.Windowing;
using QuestoGraph.Data;
using QuestoGraph.Windows;

namespace QuestoGraph.Manager
{
    internal class UIManager : IDisposable
    {
        private readonly Config config;
        private readonly QuestsManager questsManager;

        private readonly WindowSystem WindowSystem = new(Plugin.Name);
        private readonly MainWindow mainWindow;

        public UIManager(Config config, QuestsManager questsManager)
        {
            this.config = config;
            this.questsManager = questsManager;

            this.mainWindow = new MainWindow(this.config, this.questsManager);
            this.WindowSystem.AddWindow(this.mainWindow);

            Plugin.Interface.UiBuilder.Draw += this.DrawUI;
            Plugin.Interface.UiBuilder.OpenMainUi += this.Toggle;
        }

        public void Dispose()
        {
            Plugin.Interface.UiBuilder.OpenMainUi -= this.Toggle;

            if (this.mainWindow.IsOpen)
            {
                this.mainWindow.Toggle();
            }
            this.mainWindow.Dispose();

            Plugin.Interface.UiBuilder.Draw -= this.DrawUI;
        }

        private void DrawUI() => this.WindowSystem.Draw();

        public void Toggle() => this.mainWindow.Toggle();
    }
}
