using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using QuestoGraph.Data.Settings;
using QuestoGraph.Manager;

namespace QuestoGraph
{
    internal class Plugin : IDalamudPlugin
    {
        const string commandName = "/quests";
        public const string Name = "Quest'o'Graph";

        [PluginService]
        internal static IDalamudPluginInterface Interface { get; private set; } = null!;

        [PluginService]
        internal static IClientState ClientState { get; private set; } = null!;

        [PluginService]
        internal static ICommandManager CommandManager { get; private set; } = null!;

        [PluginService]
        internal static IDataManager DataManager { get; private set; } = null!;

        [PluginService]
        internal static ITextureProvider TextureProvider { get; private set; } = null!;

        [PluginService]
        internal static IGameGui GameGui { get; private set; } = null!;

        [PluginService]
        internal static IPluginLog Log { get; private set; } = null!;

        private Config Config { get; }

        private readonly QuestsManager questsManager;
        private readonly UIManager uiManager;

        public Plugin()
        {
            this.Config = Interface.GetPluginConfig() as Config ?? new Config();

            this.questsManager = new QuestsManager();
            this.uiManager = new UIManager(this.Config, this.questsManager);

            this.RegisterCommands();
        }

        private void RegisterCommands()
        {
            CommandManager.AddHandler(commandName, new CommandInfo((_, _) => this.uiManager.Toggle())
            {
                HelpMessage = "Show Quests",
            });
        }

        public void Dispose()
        {
            CommandManager.RemoveHandler(commandName);
        }
    }
}
