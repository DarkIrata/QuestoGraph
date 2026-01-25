using System.Diagnostics;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using QuestoGraph.Data;
using QuestoGraph.Data.Graph;
using QuestoGraph.Data.Settings;
using QuestoGraph.Manager;
using QuestoGraph.Services.Events;
using QuestoGraph.Utils;

namespace QuestoGraph.Windows
{
    internal class GraphWindow : Window, IDisposable
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

        private readonly Config config;
        private readonly QuestsManager questsManager;
        private readonly GraphBuilder graphBuilder;
        private readonly EventAggregator eventAggregator;

        private GeometryGraph? Graph { get; set; }

        private Node? CenterNode { get; set; }

        private Vector2 centerOffset = Vector2.Zero;
        private Vector2 dragOffset = Vector2.Zero;
        private float zoomLevel = 1f;
        private bool viewDrag = false;
        private Vector2 lastDragPos;
        private QuestData? initialSelectedQuest = null;
        private QuestData? lastInitialSelectedQuest = null;
        private QuestData? highlightedQuest = null;
        private CancellationTokenSource? calcCancellationTokenSource = null;

        public GraphWindow(Config config, QuestsManager questsManager, EventAggregator eventAggregator)
            : base($"{Plugin.Name} - Graph##GraphView", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            this.config = config;
            this.questsManager = questsManager;
            this.graphBuilder = new GraphBuilder();
            this.eventAggregator = eventAggregator;

            this.SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(600, 600),
                MaximumSize = new Vector2(1200, float.MaxValue),
            };
        }

        public override void OnOpen()
        {
            base.OnOpen();

            if (this.initialSelectedQuest == null)
            {
                Plugin.Log.Warning("No Quest was selected..");
            }
        }

        public override void OnClose()
        {
            base.OnClose();
            this.calcCancellationTokenSource?.Cancel();
        }

        public void Dispose()
        {
        }

        public CancellationTokenSource? Show(QuestData questData)
        {
            this.initialSelectedQuest = questData;
            this.highlightedQuest = questData;
            Plugin.Log.Info($"Showing Graph for '{questData.Name}'");

            var calc = this.StartGraphRecalculation(this.initialSelectedQuest);
            if (!this.IsOpen)
            {
                this.Toggle();
            }

            return calc;
        }

        internal CancellationTokenSource? RedrawGraph()
            => this.StartGraphRecalculation(this.initialSelectedQuest, true);

        public override void Draw()
        {
            if (this.initialSelectedQuest == null)
            {
                const string text = "♪ No quest selected ♫";
                ImGui.SetCursorPos((ImGui.GetContentRegionAvail() - ImGui.CalcTextSize(text)) * 0.5f);
                ImGui.TextUnformatted(text);

                return;
            }
            else if (this.initialSelectedQuest.RowId == 0 || this.Graph == null)
            {
                const int diameter = 86;
                ImGui.SetCursorPos((ImGui.GetWindowSize() - new Vector2(diameter, diameter)) * 0.5f);
                ImGuiUtils.DrawWeirdSpinner(
                    radius: diameter / 2,
                    thickness: 5f,
                    color: ImGui.GetColorU32(ImGuiCol.ButtonHovered)
                );

                ImGui.SameLine();

                const string text = "Loading";
                ImGui.SetCursorPos((ImGui.GetWindowSize() - ImGui.CalcTextSize(text)) * 0.5f);
                ImGui.TextUnformatted(text);

                return;
            }

            if (this.Graph != null && ImGui.BeginChild("quest-map", new Vector2(-1, -1)))
            {
                this.DrawGraph(this.Graph);

                ImGui.EndChild();
            }
        }

        internal CancellationTokenSource? StartGraphRecalculation(QuestData? quest, bool force = false)
        {
            if (this.calcCancellationTokenSource is not null)
            {
                Plugin.Log.Info("Cancelling Graph Calculation");
                this.calcCancellationTokenSource?.Cancel();
            }

            if (this.lastInitialSelectedQuest == quest && !force)
            {
                return this.calcCancellationTokenSource;
            }

            if (quest == null)
            {
                return this.calcCancellationTokenSource;
            }

            this.Graph = null;
            this.calcCancellationTokenSource = new CancellationTokenSource();
            Task.Run(async () =>
            {
                var sw = new Stopwatch();
                Plugin.Log.Info("Start Graph Recalculation");
                sw.Start();

                var graphData = this.graphBuilder.Build(this.questsManager, quest, this.config, sw, calcCancellationTokenSource.Token);

                this.zoomLevel = 1f;
                this.dragOffset = Vector2.Zero;

                this.Graph = graphData?.Graph;
                this.CenterNode = graphData?.CenterNode;

                this.lastInitialSelectedQuest = this.initialSelectedQuest;
                sw.Stop();

                Plugin.Log.Info($"Recalculation finished ({sw.Elapsed})");
                this.calcCancellationTokenSource = null;
            }, this.calcCancellationTokenSource.Token);

            return this.calcCancellationTokenSource;
        }

        private void DrawGraph(GeometryGraph graph)
        {
            // now the fun (tm) begins
            var space = ImGui.GetContentRegionAvail();
            var size = new Vector2(space.X, space.Y);
            var drawList = ImGui.GetWindowDrawList();

            ImGui.BeginGroup();

            ImGui.InvisibleButton("##NodeEmpty", size);
            var canvasData = new CanvasData(ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
            canvasData.Pivot = canvasData.Center;// Maybe make use of ImGui.GetMousePos();

            if (this.CenterNode != null)
            {
                var center = -canvasData.Center * 0.5f;
                var centerNodeVector = -this.CenterNode.Center.ToVector2();
                var x = center.X + (centerNodeVector.X + (this.CenterNode.Width / 2));
                var y = center.Y + centerNodeVector.Y - this.CenterNode.Height;
                this.centerOffset = new Vector2((float)x, (float)y);
                this.dragOffset = new Vector2((float)x, (float)y);

                this.CenterNode = null;
            }

            drawList.PushClipRect(canvasData.Topleft, canvasData.BottomRight, true);

            // Background
            drawList.AddRectFilled(canvasData.Topleft, canvasData.BottomRight, Colors.Background);

            // Background Grid
            this.DrawGrid(drawList, size, canvasData);

            // Content
            this.DrawNodeConnections(drawList, graph.Edges, canvasData);
            var drawn = this.DrawNodes(drawList, graph.Nodes, canvasData);

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

                                if (left)
                                {
                                    //this.InfoWindows.Add(id);
                                    if (this.questsManager.QuestData.TryGetValue(id, out var quest))
                                    {
                                        this.highlightedQuest = quest;
                                        this.eventAggregator.Publish(new GraphQuestClicked(quest));
                                    }
                                }

                                if (right)
                                {
                                    //unsafe
                                    //{
                                    //    AgentQuestJournal.Instance()->OpenForQuest(id, 1);
                                    //}
                                    Plugin.Log.Debug("RIGHT");
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

        private void DrawNodeConnections(ImDrawListPtr drawList, EdgeCollection edges, CanvasData canvasData)
        {
            void DrawNodeLine(LineSegment line)
            {
                drawList.AddLine(
                    this.ConvertDrawPointPivoted(canvasData, line.Start),
                    this.ConvertDrawPointPivoted(canvasData, line.End),
                    Colors.Line,
                    3.0f * this.zoomLevel
                );
            }

            foreach (var edge in edges)
            {
                var topLeft = canvasData.BottomRight + edge.GetTopLeft(this.dragOffset);
                var start = canvasData.Pivot + ((canvasData.Pivot - topLeft) * this.zoomLevel);
                if (this.IsHidden(canvasData.Topleft, canvasData.BottomRight, edge, start))
                {
                    continue;
                }

                if (edge.Curve is Curve curve)
                {
                    foreach (var segment in curve.Segments)
                    {
                        if (segment is LineSegment line)
                        {
                            DrawNodeLine(line);
                        }
                        else if (segment is CubicBezierSegment cs)
                        {
                            drawList.AddBezierCubic(
                                this.ConvertDrawPointPivoted(canvasData, cs.B(0)),
                                this.ConvertDrawPointPivoted(canvasData, cs.B(1)),
                                this.ConvertDrawPointPivoted(canvasData, cs.B(2)),
                                this.ConvertDrawPointPivoted(canvasData, cs.B(3)),
                                Colors.Line,
                                3.0f * this.zoomLevel
                            );
                        }
                    }
                }
                else if (edge.Curve is LineSegment line)
                {
                    DrawNodeLine(line);
                }

                if (edge.ArrowheadAtTarget)
                {
                    this.DrawArrow(
                        drawList,
                        this.ConvertDrawPointPivoted(canvasData, edge.Curve.End),
                        this.ConvertDrawPointPivoted(canvasData, edge.EdgeGeometry.TargetArrowhead.TipPosition)
                    );
                }
            }
        }

        private List<(Vector2 Start, Vector2 End, uint id)> DrawNodes(
            ImDrawListPtr drawList,
            IList<Node> nodes,
            CanvasData canvasData)
        {
            var drawn = new List<(Vector2, Vector2, uint)>();
            foreach (var node in nodes)
            {
                var topLeft = canvasData.BottomRight + node.GetTopLeft(this.dragOffset);
                var start = canvasData.Pivot + ((canvasData.Pivot - topLeft) * this.zoomLevel);

                if (this.IsHidden(canvasData.Topleft, canvasData.BottomRight, node, start) ||
                    node.UserData is not NodeData graphNode)
                {
                    continue;
                }

                var backgroundColor = this.config.Colors.GraphDefaultBackgroundColor;
                var textColour = Colors.Text;

                if (graphNode?.QuestData != null)
                {
                    backgroundColor = graphNode.QuestData.QuestType switch
                    {
                        Enums.QuestTypes.Normal => this.config.Colors.GraphDefaultBackgroundColor,
                        Enums.QuestTypes.MSQ => this.config.Colors.GraphMSQBackgroundColor,
                        Enums.QuestTypes.Blue => this.config.Colors.GraphBlueBackgroundColor,
                        _ => this.config.Colors.GraphDefaultBackgroundColor,
                    };

                    if (QuestManager.IsQuestComplete(graphNode.QuestData.RowId))
                    {
                        backgroundColor.W = .5f;
                        textColour = (uint)((0x80 << 24) | (textColour & 0xFFFFFF));
                    }
                }

                var bottomRight = canvasData.BottomRight + node.GetBottomRight(this.dragOffset);
                var end = canvasData.Pivot + ((canvasData.Pivot - bottomRight) * this.zoomLevel);

                drawn.Add((start, end, graphNode!.Id));
                drawList.AddRectFilled(start, end, ImGui.GetColorU32(backgroundColor), 5, ImDrawFlags.RoundCornersAll);
                if (graphNode.Id == (this.initialSelectedQuest?.RowId ?? 0) ||
                    graphNode.Id == (this.highlightedQuest?.RowId ?? 0))
                {
                }

                uint? nodeBorder = this.GetNodeBorderColor(graphNode.Id);
                if (nodeBorder is not null)
                {
                    drawList.AddRect(start - Vector2.One, end + Vector2.One, nodeBorder.Value, 5, ImDrawFlags.RoundCornersAll, 3.5f * this.zoomLevel);
                }

                var font = ImGui.GetFont();
                drawList.AddText(font, font.FontSize * this.zoomLevel, start + GraphUtils.TextOffset, textColour, graphNode.Text);
            }

            return drawn;
        }

        private uint? GetNodeBorderColor(uint id)
        {
            if (id == this.highlightedQuest?.RowId)
            {
                return ImGui.GetColorU32(this.config.Colors.GraphHighlightedQuestBorder);
            }
            else if (id == this.initialSelectedQuest?.RowId)
            {
                return ImGui.GetColorU32(this.config.Colors.GraphInitialQuestBorder);
            }

            return null;
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

        private void DrawArrow(ImDrawListPtr drawList, Vector2 start, Vector2 end)
        {
            const float arrowAngle = 30f;
            var dir = end - start;
            var h = dir;
            dir /= dir.Length();

            var s = new Vector2(-dir.Y, dir.X);
            s *= (float)(h.Length() * Math.Tan(arrowAngle * 0.5f * (Math.PI / 180f)));

            drawList.AddTriangleFilled(
                start + s,
                end,
                start - s,
                Colors.Line
            );
        }

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
