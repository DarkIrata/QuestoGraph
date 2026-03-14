using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Layout;
using QuestoGraph.Data.Graph;
using QuestoGraph.Manager;
using QuestoGraph.Services.Events;
using QuestoGraph.Utils;

namespace QuestoGraph.Windows
{
    internal class DebugWindow : Window, IDisposable
    {
        private static class Colors
        {
            internal static readonly uint Background = ImGui.ColorConvertFloat4ToU32(new Vector4(0.13f, 0.13f, 0.13f, 1));
            internal static readonly uint Border = ImGui.ColorConvertFloat4ToU32(new Vector4(0.3f, 0.3f, 0.3f, 1));
            internal static readonly uint Text = ImGui.ColorConvertFloat4ToU32(new Vector4(0.9f, 0.9f, 0.9f, 1));
            internal static readonly uint Line = ImGui.ColorConvertFloat4ToU32(new Vector4(0.7f, 0.7f, 0.7f, 1));
            internal static readonly uint Grid = ImGui.ColorConvertFloat4ToU32(new Vector4(0.1f, 0.1f, 0.1f, 1));
        }

        private const int GridSmall = 10;
        private const int GridLarge = 50;
        private const float GridSmallThickness = 1f;
        private const float GridLargeThickness = 2f;
        private const float GridZoomMultiplier = 1f;
        private const float ZoomLevelModifier = 0.055f;
        private const float MinZoomLevel = 0.125f;
        private const float MaxZoomLevel = 2f;

        private readonly EventAggregator eventAggregator;
        private readonly QuestsManager questsManager;

        private Node? CenterNode { get; set; }
        private GraphData? GraphData { get; set; }

        private Vector2 centerOffset = Vector2.Zero;
        private Vector2 dragOffset = Vector2.Zero;
        private float zoomLevel = 1f;
        private bool viewDrag = false;
        private Vector2 lastDragPos;

        public DebugWindow(QuestsManager questsManager, EventAggregator eventAggregator)
            : base($"{Plugin.Name} - DebugTest##DebugTestView", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            this.questsManager = questsManager;
            this.eventAggregator = eventAggregator;

            this.SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(600, 600),
                MaximumSize = new Vector2(1200, float.MaxValue),
            };
        }

        public override void OnOpen()
        {
            Plugin.Log.Info("SHOWING DEBUG WINDOW");
            var questData = questsManager.QuestData.FirstOrDefault(q => q.Value.Name == "Starlit Smiles").Value;
            var graph = new GraphBuilder();
            this.GraphData = graph.Build(questsManager, questData, new Data.Settings.Config(), null, default);
            this.CenterNode = this.GraphData.CenterNode;

            base.OnOpen();
        }

        public override void OnClose()
        {
            base.OnClose();
        }

        public void Dispose()
        {
        }


        public override void Draw()
        {
            if (ImGui.BeginChild("quest-map", new Vector2(-1, -1)))
            {
                this.DrawTestArea();

                ImGui.EndChild();
            }
        }

        private void DrawTestArea()
        {
            // now the fun (tm) begins
            var space = ImGui.GetContentRegionAvail();
            var size = new Vector2(space.X, space.Y);
            var drawList = ImGui.GetWindowDrawList();

            ImGui.BeginGroup();

            ImGui.InvisibleButton("##NodeEmpty", size);
            var canvasData = new CanvasData(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), size);
            canvasData.Pivot = canvasData.Center;// Maybe make use of ImGui.GetMousePos();

            //if (this.CenterNode != null)
            {
                var nodeTopLeft = canvasData.Center - this.CenterNode.Center.ToVector2();

                this.centerOffset = nodeTopLeft;
                this.dragOffset = nodeTopLeft;
            }

            drawList.PushClipRect(canvasData.Topleft, canvasData.BottomRight, true);

            // Background
            drawList.AddRectFilled(canvasData.Topleft, canvasData.BottomRight, Colors.Background);

            // Background Grid
            this.DrawGrid(drawList, size, canvasData);

            var drawn = this.DrawNodes(drawList, this.GraphData.Graph.Nodes, canvasData);

            // Graph Border
            drawList.AddRect(canvasData.Topleft, canvasData.BottomRight, Colors.Border);

            if (ImGui.IsItemActive())
            {
                if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                {
                    var d = ImGui.GetMouseDragDelta();
                    if (this.zoomLevel < 1f)
                    {
                        d /= (float)Math.Pow(this.zoomLevel, 1);
                    }

                    if (this.viewDrag)
                    {
                        var delta = d - this.lastDragPos;
                        this.dragOffset -= delta;
                    }

                    this.viewDrag = true;
                    this.lastDragPos = d;
                }
                else
                {
                    this.viewDrag = false;
                }
            }
            else
            {
                if (!this.viewDrag)
                {
                    var io = ImGui.GetIO();
                    if (ImGui.IsWindowFocused() &&
                        ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows) &&
                        io.MouseWheel != 0)
                    {
                        if (io.MouseWheel < 0 && this.zoomLevel > MinZoomLevel)
                        {
                            this.zoomLevel -= ZoomLevelModifier;
                        }
                        else if (io.MouseWheel > 0 && this.zoomLevel < MaxZoomLevel)
                        {
                            this.zoomLevel += ZoomLevelModifier;
                        }
                    }

                    var left = ImGui.IsMouseReleased(ImGuiMouseButton.Left);
                    var middle = ImGui.IsMouseReleased(ImGuiMouseButton.Middle);
                    var right = ImGui.IsMouseReleased(ImGuiMouseButton.Right);
                    if (left || middle || right)
                    {
                        {
                            var mousePos = ImGui.GetMousePos();
                            foreach (var (start, end, id) in drawn)
                            {
                                var inBox = mousePos.X >= start.X && mousePos.X <= end.X && mousePos.Y >= start.Y && mousePos.Y <= end.Y;
                                if (!inBox)
                                {
                                    continue;
                                }

                                break;
                            }
                        }
                    }
                }

                this.viewDrag = false;
            }

            drawList.PopClipRect();
            ImGui.EndGroup();
        }

        private List<(Vector2 Start, Vector2 End, uint id)> DrawNodes(
            ImDrawListPtr drawList,
            IList<Node> nodes,
            CanvasData canvasData)
        {
            var drawn = new List<(Vector2, Vector2, uint)>();
            foreach (var node in nodes)
            {
                var topLeft = node.GetTopLeft(this.dragOffset);
                var bottomRight = node.GetBottomRight(this.dragOffset);

                var size = bottomRight - topLeft;

                var start = canvasData.Pivot + ((topLeft - canvasData.Pivot) * this.zoomLevel);
                var end = start + (size * this.zoomLevel);

                if (this.IsHidden(canvasData.Topleft, canvasData.BottomRight, node, start) ||
                    node.UserData is not NodeData graphNode)
                {
                    continue;
                }

                var backgroundColor = new Data.Settings.Config().Colors.GraphDefaultBackgroundColor;
                var textColour = Colors.Text;

                if (graphNode?.QuestData != null)
                {
                    if (QuestManager.IsQuestComplete(graphNode.QuestData.RowId))
                    {
                        backgroundColor.W = .5f;
                        textColour = (uint)((0x80 << 24) | (textColour & 0xFFFFFF));
                    }
                }

                //var bottomRight = node.GetBottomRight(this.dragOffset);
                //var end = canvasData.Pivot + ((bottomRight - canvasData.Pivot) * this.zoomLevel);

                //drawn.Add((start, end, graphNode!.Id));

                drawList.AddRectFilled(end, start, ImGui.GetColorU32(backgroundColor), 5, ImDrawFlags.RoundCornersAll);
                drawList.AddRect(end + Vector2.One, start - Vector2.One, Colors.Border, 5, ImDrawFlags.RoundCornersAll, 2.5f * this.zoomLevel);

                var font = ImGui.GetFont();
                drawList.AddText(font, font.FontSize * this.zoomLevel, end + GraphUtils.TextOffset, textColour, graphNode.Text);
            }

            return drawn;
        }

        private void DrawGrid(ImDrawListPtr drawList, Vector2 size, CanvasData canvasData)
        {
            size /= (float)Math.Pow(this.zoomLevel, GridZoomMultiplier);

            // Vertical
            for (var i = 0; i < size.X / GridSmall; i++)
            {
                this.DrawGridLine(drawList, canvasData, i * GridSmall, true, Colors.Grid, GridSmallThickness);
            }

            // Horizontal
            for (var i = 0; i < size.Y / GridSmall; i++)
            {
                this.DrawGridLine(drawList, canvasData, i * GridSmall, false, Colors.Grid, GridSmallThickness);
            }

            // Vertical
            for (var i = 0; i < size.X / GridLarge; i++)
            {
                this.DrawGridLine(drawList, canvasData, i * GridLarge, true, Colors.Grid, GridLargeThickness);
            }

            // Horizontal
            for (var i = 0; i < size.Y / GridLarge; i++)
            {
                this.DrawGridLine(drawList, canvasData, i * GridLarge, true, Colors.Grid, GridLargeThickness);
            }
        }

        private void DrawGridLine(ImDrawListPtr drawList, CanvasData canvasData, int linePadding, bool isVertical, uint color, float thickness)
        {
            if (isVertical)
            {
                drawList.AddLine(new Vector2(canvasData.Topleft.X + (linePadding * this.zoomLevel), canvasData.Topleft.Y),
                    new Vector2(canvasData.Topleft.X + (linePadding * this.zoomLevel), canvasData.BottomRight.Y), color, thickness);
            }
            else
            {
                drawList.AddLine(new Vector2(canvasData.Topleft.X, canvasData.Topleft.Y + (linePadding * this.zoomLevel)),
                    new Vector2(canvasData.BottomRight.X, canvasData.Topleft.Y + (linePadding * this.zoomLevel)), color, thickness);
            }
        }

        private Vector2 ConvertDrawPointPivoted(Vector2 bottomRight, Vector2 pivot, Point p)
            => pivot + ((pivot - (bottomRight + (p.ToVector2() + this.dragOffset))) * this.zoomLevel);

        private Vector2 ConvertDrawPointPivoted(CanvasData canvasData, Point p)
            => this.ConvertDrawPointPivoted(canvasData.BottomRight, canvasData.Pivot, p);

        private bool IsHidden(Vector2 canvasTopLeft, Vector2 canvasBottomRight, GeometryObject node, Vector2 start)
        {
            var width = (float)node.BoundingBox.Width;
            var height = (float)node.BoundingBox.Height;
            return start.X + width < canvasTopLeft.X
                   || start.Y + height < canvasTopLeft.Y
                   || start.X > canvasBottomRight.X
                   || start.Y > canvasBottomRight.Y;
        }
    }
}
