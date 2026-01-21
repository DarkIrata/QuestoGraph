using System.Diagnostics;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using QuestoGraph.Data;
using QuestoGraph.Data.Settings;
using QuestoGraph.Manager;
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

        private GeometryGraph? Graph { get; set; }

        private Node? CenterNode { get; set; }

        private Vector2 dragOffset = Vector2.Zero;
        private float zoomLevel = 1f;
        private bool viewDrag;
        private Vector2 lastDragPos;
        private QuestData? SelectedQuest;
        private QuestData? lastSelectedQuest;
        private CancellationTokenSource? calcCancellationTokenSource = null;

        public GraphWindow(Config config, QuestsManager questsManager)
            : base($"{Plugin.Name} - Graph##GraphView", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            this.config = config;
            this.questsManager = questsManager;

            this.SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(600, 600),
                MaximumSize = new Vector2(1200, float.MaxValue),
            };
        }

        public override void OnOpen()
        {
            base.OnOpen();

            if (this.SelectedQuest == null)
            {
                Plugin.Log.Warning("No Quest was selected..");
            }
        }

        public void Dispose()
        {
        }

        public CancellationTokenSource? Show(QuestData questData)
        {
            this.SelectedQuest = questData;
            Plugin.Log.Info($"Showing Graph for '{questData.Name}'");

            var calc = this.StartGraphRecalculation(this.SelectedQuest);
            if (!this.IsOpen)
            {
                this.Toggle();
            }

            return calc;
        }

        internal CancellationTokenSource? RedrawGraph()
            => this.StartGraphRecalculation(this.SelectedQuest, true);

        public override void Draw()
        {
            if (this.SelectedQuest == null)
            {
                const string text = "♪ No quest selected ♫";
                ImGui.SetCursorPos((ImGui.GetContentRegionAvail() - ImGui.CalcTextSize(text)) * 0.5f);
                ImGui.TextUnformatted(text);

                return;
            }
            else if (this.SelectedQuest.RowId == 0 || this.Graph == null)
            {
                var diameter = 86;
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

            if (this.lastSelectedQuest == quest && !force)
            {
                return this.calcCancellationTokenSource;
            }

            this.calcCancellationTokenSource = new CancellationTokenSource();
            if (quest == null)
            {
                return this.calcCancellationTokenSource;
            }

            this.Graph = null;
            Task.Run(() =>
            {
                var sw = new Stopwatch();
                Plugin.Log.Info("Starting Graph Calculation");
                sw.Start();

                var info = GraphUtils.GetGraphInfo(quest, this.questsManager, this.config, calcCancellationTokenSource.Token, sw);

                this.zoomLevel = 1f;
                this.dragOffset = Vector2.Zero;

                this.Graph = info.FinishedGraph;
                this.CenterNode = info.CenterNode;

                this.lastSelectedQuest = this.SelectedQuest;
                sw.Stop();
                Plugin.Log.Info($"Graph Finished ({sw.Elapsed})");
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
            var canvasTopLeft = ImGui.GetItemRectMin();
            var canvasBottomRight = ImGui.GetItemRectMax();

            if (this.CenterNode != null)
            {
                this.dragOffset = (this.CenterNode.Center.ToVector2() * -1) + ((canvasBottomRight - canvasTopLeft) / 2);
                this.CenterNode = null;
            }

            drawList.PushClipRect(canvasTopLeft, canvasBottomRight, true);

            // Background
            drawList.AddRectFilled(canvasTopLeft, canvasBottomRight, Colors.Background);

            // Background Grid
            this.DrawGrid(drawList, size, canvasTopLeft, canvasBottomRight);

            // Graph Border
            drawList.AddRect(canvasTopLeft, canvasBottomRight, Colors.Border);

            // Content
            this.DrawNodeConnections(drawList, graph.Edges, canvasTopLeft, canvasBottomRight);
            var drawn = this.DrawNodes(drawList, graph.Nodes, canvasTopLeft, canvasBottomRight);

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
                    if (ImGui.IsWindowFocused() && io.MouseWheel != 0)
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
                    var right = ImGui.IsMouseReleased(ImGuiMouseButton.Right);
                    if (left || right)
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
                                Plugin.Log.Debug("LEFT");
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

                this.viewDrag = false;
            }

            drawList.PopClipRect();
            ImGui.EndGroup();
        }

        private void DrawNodeConnections(ImDrawListPtr drawList, EdgeCollection edges, Vector2 canvasTopLeft, Vector2 canvasBottomRight)
        {
            void DrawNodeLine(LineSegment line)
            {
                drawList.AddLine(
                    this.ConvertDrawPoint(canvasBottomRight, line.Start),
                    this.ConvertDrawPoint(canvasBottomRight, line.End),
                    Colors.Line,
                    3.0f * this.zoomLevel
                );
            }

            foreach (var edge in edges)
            {
                var start = canvasBottomRight - (edge.GetTopLeft(this.dragOffset) * this.zoomLevel);
                if (this.IsHidden(canvasTopLeft, canvasBottomRight, edge, start))
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
                                this.ConvertDrawPoint(canvasBottomRight, cs.B(0)),
                                this.ConvertDrawPoint(canvasBottomRight, cs.B(1)),
                                this.ConvertDrawPoint(canvasBottomRight, cs.B(2)),
                                this.ConvertDrawPoint(canvasBottomRight, cs.B(3)),
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
                        this.ConvertDrawPoint(canvasBottomRight, edge.Curve.End),
                        this.ConvertDrawPoint(canvasBottomRight, edge.EdgeGeometry.TargetArrowhead.TipPosition)
                    );
                }

                if (edge.ArrowheadAtSource)
                {
                    this.DrawArrow(
                        drawList,
                        this.ConvertDrawPoint(canvasBottomRight, edge.Curve.Start),
                        this.ConvertDrawPoint(canvasBottomRight, edge.EdgeGeometry.SourceArrowhead.TipPosition)
                    );
                }
            }
        }

        private List<(Vector2, Vector2, uint)> DrawNodes(ImDrawListPtr drawList, IList<Node> nodes, Vector2 canvasTopLeft, Vector2 canvasBottomRight)
        {
            var drawn = new List<(Vector2, Vector2, uint)>();
            foreach (var node in nodes)
            {
                var graphNode = node.UserData as NodeData;
                var topLeft = node.GetTopLeft(this.dragOffset);
                var start = canvasBottomRight - (node.GetTopLeft(this.dragOffset) * this.zoomLevel);

                if (this.IsHidden(canvasTopLeft, canvasBottomRight, node, start) ||
                    graphNode is null)
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

                var end = canvasBottomRight - (node.GetBottomRight(this.dragOffset) * this.zoomLevel);

                drawn.Add((start, end, graphNode!.Id));
                if (this.SelectedQuest is not null && graphNode.Id == this.SelectedQuest.RowId)
                {
                    drawList.AddRect(start - Vector2.One, end + Vector2.One, Colors.Border, 5, ImDrawFlags.RoundCornersAll, 3.5f);
                }

                drawList.AddRectFilled(start, end, ImGui.GetColorU32(backgroundColor), 5, ImDrawFlags.RoundCornersAll);
                var font = ImGui.GetFont();
                drawList.AddText(font, font.FontSize * this.zoomLevel, start + GraphUtils.TextOffset, textColour, graphNode.Text);
            }

            return drawn;
        }

        private void DrawGrid(ImDrawListPtr drawList, Vector2 size, Vector2 canvasTopLeft, Vector2 canvasBottomRight)
        {
            size /= (float)Math.Pow(this.zoomLevel, GridZoomMultiplier);

            // Vertical
            for (var i = 0; i < size.X / GridSmall; i++)
            {
                this.DrawGridLine(drawList, canvasTopLeft, canvasBottomRight, i * GridSmall, true, Colors.Grid, GridSmallThickness);
            }

            // Horizontal
            for (var i = 0; i < size.Y / GridSmall; i++)
            {
                this.DrawGridLine(drawList, canvasTopLeft, canvasBottomRight, i * GridSmall, false, Colors.Grid, GridSmallThickness);
            }

            // Vertical
            for (var i = 0; i < size.X / GridLarge; i++)
            {
                this.DrawGridLine(drawList, canvasTopLeft, canvasBottomRight, i * GridLarge, true, Colors.Grid, GridLargeThickness);
            }

            // Horizontal
            for (var i = 0; i < size.Y / GridLarge; i++)
            {
                this.DrawGridLine(drawList, canvasTopLeft, canvasBottomRight, i * GridLarge, true, Colors.Grid, GridLargeThickness);
            }
        }

        private void DrawGridLine(ImDrawListPtr drawList, Vector2 canvasTopLeft, Vector2 canvasBottomRight, int linePadding, bool isVertical, uint color, float thickness)
        {
            if (isVertical)
            {
                drawList.AddLine(new Vector2(canvasTopLeft.X + (linePadding * this.zoomLevel), canvasTopLeft.Y),
                    new Vector2(canvasTopLeft.X + (linePadding * this.zoomLevel), canvasBottomRight.Y), color, thickness);
            }
            else
            {
                drawList.AddLine(new Vector2(canvasTopLeft.X, canvasTopLeft.Y + (linePadding * this.zoomLevel)),
                    new Vector2(canvasBottomRight.X, canvasTopLeft.Y + (linePadding * this.zoomLevel)), color, thickness);
            }
        }

        private Vector2 ConvertDrawPoint(Vector2 canvasBottomRight, Point p)
            => canvasBottomRight - ((p.ToVector2() + this.dragOffset) * this.zoomLevel);

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
