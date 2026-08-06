using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using QuestoGraph.Data;
using QuestoGraph.Data.Settings;
using QuestoGraph.Manager;
using QuestoGraph.Services.Events;
using QuestoGraph.Windows.Frames;

namespace QuestoGraph.Windows
{
    internal class GraphWindow : Window, IDisposable
    {
        private readonly GraphFrame graphFrame;

        public GraphWindow(Config config, QuestsManager questsManager, EventAggregator eventAggregator)
            : base($"{Plugin.Name} - Graph##GraphView", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            this.graphFrame = new GraphFrame(config, questsManager, eventAggregator);

            this.SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(600, 600),
                MaximumSize = new Vector2(1200, float.MaxValue),
            };
        }

        public override void OnOpen()
        {
            base.OnOpen();
            this.graphFrame.OnOpen();
        }

        public override void OnClose()
        {
            base.OnClose();
            this.graphFrame.OnClose();
        }

        public void Dispose()
        {
        }

        public override void Draw()
            => this.graphFrame.Draw();

        internal void RedrawGraph()
            => this.graphFrame.RedrawGraph();

        internal void Show(QuestData questData)
            => this.graphFrame.Show(questData);
    }
}
