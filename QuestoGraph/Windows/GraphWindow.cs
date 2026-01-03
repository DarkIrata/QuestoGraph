using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Miscellaneous;
using QuestoGraph.Data;
using QuestoGraph.Data.Settings;
using QuestoGraph.Manager;

namespace QuestoGraph.Windows
{
    internal static class MsaglExtensions
    {
        public static Vector2 ToVector2(this Point p)
            => new((float)p.X, (float)p.Y);
    }

    internal class GraphWindow : Window, IDisposable
    {
        private readonly Config config;
        private readonly QuestsManager questsManager;

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

            //this.SelectedQuest = this.questsManager.QuestData.FirstOrDefault(qd => qd.Value.Name.Contains("By Agents Unknown")).Value;
            //this.SelectedQuest = this.questsManager.QuestData.FirstOrDefault(qd => qd.Value.Name.Contains("Dawntrail")).Value;
            this.SelectedQuest = this.questsManager.QuestData.FirstOrDefault(qd => qd.Value.Name.Contains("Endwalker")).Value;
            this.StartGraphRecalculation(this.SelectedQuest);
        }

        public void Dispose()
        {
        }

        private GeometryGraph? Graph { get; set; }

        private Node? CenterNode { get; set; }

        public override void Draw()
        {
            if (this.SelectedQuest == null)
            {
                return;
            }

            if (ImGui.BeginChild("quest-map", new Vector2(-1, -1)))
            {
                if (this.SelectedQuest.RowId == 0 && this.Graph == null)
                {
                    ImGui.TextUnformatted("Generating map...");
                }

                if (this.Graph != null)
                {
                    this.DrawGraph(this.Graph);
                }

                ImGui.EndChild();
            }
        }

        private static class Colors
        {
            internal static readonly uint Bg = ImGui.GetColorU32(new Vector4(0.13f, 0.13f, 0.13f, 1));
            internal static readonly uint Bg2 = ImGui.GetColorU32(new Vector4(0.3f, 0.3f, 0.3f, 1));
            internal static readonly uint Text = ImGui.GetColorU32(new Vector4(0.9f, 0.9f, 0.9f, 1));
            internal static readonly uint Line = ImGui.GetColorU32(new Vector4(0.7f, 0.7f, 0.7f, 1));
            internal static readonly uint Grid = ImGui.GetColorU32(new Vector4(0.1f, 0.1f, 0.1f, 1));

            internal static readonly Vector4 NormalQuest = new(0.54f, 0.45f, 0.36f, 1);
            internal static readonly Vector4 MsqQuest = new(0.29f, 0.35f, 0.44f, 1);
            internal static readonly Vector4 BlueQuest = new(0.024F, 0.016f, 0.72f, 1);
        }
        private Vector2 GetTopLeft(GeometryObject item)
        {
            // imgui measures from top left as 0,0
            return item.BoundingBox.RightTop.ToVector2() + this._offset;
        }

        private Vector2 GetBottomRight(GeometryObject item)
        {
            return item.BoundingBox.LeftBottom.ToVector2() + this._offset;
        }

        private bool _relayout;
        private Vector2 _offset = Vector2.Zero;
        private static readonly Vector2 TextOffset = new(5, 2);
        private const int GridSmall = 10;
        private const int GridLarge = 50;
        private bool _viewDrag;
        private Vector2 _lastDragPos;
        private QuestData SelectedQuest;
        private LayoutAlgorithmSettings LayoutSettings { get; } = new SugiyamaLayoutSettings();

        internal CancellationTokenSource StartGraphRecalculation(QuestData quest)
        {
            var cts = new CancellationTokenSource();
            Task.Run(() =>
            {
                Plugin.Log.Debug($"Starting Graph");
                var info = this.GetGraphInfo(quest, cts.Token);
                this.Graph = info.FinishedGraph;
                this.CenterNode = info.CenterNode;
                Plugin.Log.Debug($"Graph Finished");
            }, cts.Token);

            return cts;
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
                this._offset = this.CenterNode.Center.ToVector2() * -1 + (canvasBottomRight - canvasTopLeft) / 2;
                this.CenterNode = null;
            }

            drawList.PushClipRect(canvasTopLeft, canvasBottomRight, true);

            drawList.AddRectFilled(canvasTopLeft, canvasBottomRight, Colors.Bg);
            // ========= GRID =========
            for (var i = 0; i < size.X / GridSmall; i++)
            {
                drawList.AddLine(new Vector2(canvasTopLeft.X + i * GridSmall, canvasTopLeft.Y), new Vector2(canvasTopLeft.X + i * GridSmall, canvasBottomRight.Y), Colors.Grid, 1.0f);
            }

            for (var i = 0; i < size.Y / GridSmall; i++)
            {
                drawList.AddLine(new Vector2(canvasTopLeft.X, canvasTopLeft.Y + i * GridSmall), new Vector2(canvasBottomRight.X, canvasTopLeft.Y + i * GridSmall), Colors.Grid, 1.0f);
            }

            for (var i = 0; i < size.X / GridLarge; i++)
            {
                drawList.AddLine(new Vector2(canvasTopLeft.X + i * GridLarge, canvasTopLeft.Y), new Vector2(canvasTopLeft.X + i * GridLarge, canvasBottomRight.Y), Colors.Grid, 2.0f);
            }

            for (var i = 0; i < size.Y / GridLarge; i++)
            {
                drawList.AddLine(new Vector2(canvasTopLeft.X, canvasTopLeft.Y + i * GridLarge), new Vector2(canvasBottomRight.X, canvasTopLeft.Y + i * GridLarge), Colors.Grid, 2.0f);
            }

            drawList.AddRect(canvasTopLeft, canvasBottomRight, Colors.Bg2);

            Vector2 ConvertDrawPoint(Point p)
            {
                var ret = canvasBottomRight - (p.ToVector2() + this._offset);
                return ret;
            }

            foreach (var edge in graph.Edges)
            {
                var start = canvasBottomRight - this.GetTopLeft(edge);
                if (IsHidden(edge, start))
                {
                    continue;
                }

                var curve = edge.Curve;
                switch (curve)
                {
                    case Curve c:
                        {
                            foreach (var s in c.Segments)
                            {
                                switch (s)
                                {
                                    case LineSegment l:
                                        drawList.AddLine(
                                            ConvertDrawPoint(l.Start),
                                            ConvertDrawPoint(l.End),
                                            Colors.Line,
                                            3.0f
                                        );
                                        break;
                                    case CubicBezierSegment cs:
                                        drawList.AddBezierCubic(
                                            ConvertDrawPoint(cs.B(0)),
                                            ConvertDrawPoint(cs.B(1)),
                                            ConvertDrawPoint(cs.B(2)),
                                            ConvertDrawPoint(cs.B(3)),
                                            Colors.Line,
                                            3.0f
                                        );
                                        break;
                                }
                            }

                            break;
                        }
                    case LineSegment l:
                        drawList.AddLine(
                            ConvertDrawPoint(l.Start),
                            ConvertDrawPoint(l.End),
                            Colors.Line,
                            3.0f
                        );
                        break;
                }

                void DrawArrow(Vector2 start, Vector2 end)
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

                if (edge.ArrowheadAtTarget)
                {
                    DrawArrow(
                        ConvertDrawPoint(edge.Curve.End),
                        ConvertDrawPoint(edge.EdgeGeometry.TargetArrowhead.TipPosition)
                    );
                }

                if (edge.ArrowheadAtSource)
                {
                    DrawArrow(
                        ConvertDrawPoint(edge.Curve.Start),
                        ConvertDrawPoint(edge.EdgeGeometry.SourceArrowhead.TipPosition)
                    );
                }
            }

            bool IsHidden(GeometryObject node, Vector2 start)
            {
                var width = (float)node.BoundingBox.Width;
                var height = (float)node.BoundingBox.Height;
                return start.X + width < canvasTopLeft.X
                       || start.Y + height < canvasTopLeft.Y
                       || start.X > canvasBottomRight.X
                       || start.Y > canvasBottomRight.Y;
            }

            var drawn = new List<(Vector2, Vector2, uint)>();

            foreach (var node in graph.Nodes)
            {
                var start = canvasBottomRight - this.GetTopLeft(node);

                if (IsHidden(node, start))
                {
                    continue;
                }

                var graphNode = (GraphNode)node.UserData;
                var color = Colors.NormalQuest;
                var textColour = Colors.Text;

                // This could be in GraphNode so we dont need to call it everytime
                if (graphNode.QuestData != null)
                {
                    var quest = graphNode.QuestData.Quest;
                    color = quest.EventIconType.RowId switch
                    {
                        1 => Colors.NormalQuest, // normal
                        3 => Colors.MsqQuest, // msq
                        8 => Colors.BlueQuest, // blue
                        10 => Colors.BlueQuest, // also blue
                        _ => Colors.NormalQuest,
                    };


                    var completed = QuestManager.IsQuestComplete(quest.RowId);
                    if (completed)
                    {
                        color.W = .5f;
                        textColour = (uint)((0x80 << 24) | (textColour & 0xFFFFFF));
                    }
                }

                var end = canvasBottomRight - this.GetBottomRight(node);

                drawn.Add((start, end, graphNode.QuestId));

                if (graphNode.QuestId == this.SelectedQuest.RowId)
                {
                    drawList.AddRect(start - Vector2.One, end + Vector2.One, Colors.Line, 5, ImDrawFlags.RoundCornersAll);
                }

                drawList.AddRectFilled(start, end, ImGui.GetColorU32(color), 5, ImDrawFlags.RoundCornersAll);
                drawList.AddText(start + TextOffset, textColour, graphNode.Name);
            }

            // HOW ABOUT DRAGGING THE VIEW?
            if (ImGui.IsItemActive())
            {
                if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                {
                    var d = ImGui.GetMouseDragDelta();
                    if (this._viewDrag)
                    {
                        var delta = d - this._lastDragPos;
                        this._offset -= delta;
                    }

                    this._viewDrag = true;
                    this._lastDragPos = d;
                }
                else
                {
                    this._viewDrag = false;
                }
            }
            else
            {
                if (!this._viewDrag)
                {
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
                                Plugin.Log.Debug("LEFT");
                            }

                            if (right)
                            {
                                Plugin.Log.Debug("RIGHT");
                            }

                            break;
                        }
                    }
                }

                this._viewDrag = false;
            }

            drawList.PopClipRect();
            ImGui.EndGroup();
            // ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5);
        }

        private (GeometryGraph? FinishedGraph, Node? CenterNode) GetGraphInfo(QuestData questData, CancellationToken cancel)
        {
            if (questData == null)
            {
                return (null, null);
            }

            //Plugin.Log.Debug($"Graph for: {questData!.Name}");

            var msaglNodes = new Dictionary<uint, Node>();
            var links = new List<(uint Source, uint Target)>();
            var g = new GeometryGraph();

            void AddGraphNode(GraphNode node)
            {
                try
                {
                    var dims = ImGui.CalcTextSize(node.Name) + TextOffset * 2;
                    var graphNode = new Node(CurveFactory.CreateRectangle(dims.X, dims.Y, new Point()), node);
                    g.Nodes.Add(graphNode);
                    msaglNodes[questData.RowId] = graphNode;

                    //IEnumerable<Node<QuestData>> parents;
                    ////if (this.Plugin.Config.ShowRedundantArrows)
                    //{
                    //    //parents = node.Parents;
                    //}
                    //else
                    //{
                    //    // only add if no *other* parent also shares
                    //    parents = node.Parents
                    //        .Where(q => {
                    //            return !node.Parents
                    //                .Where(other => other != q)
                    //                .Any(other => other.Parents.Contains(q));
                    //        });
                    //}

                    foreach (var parentId in questData.PreviousQuestsId)
                    {
                        links.Add((parentId, questData.RowId));
                    }
                    //Plugin.Log.Debug($"{g.Nodes.Count} Nodes");
                }
                catch (Exception ex)
                {
                    Plugin.Log.Error(ex, "ERROR AddGraphNode");
                }
            }

            void AddNode(QuestData questData)
            {
                try
                {
                    //Plugin.Log.Debug($"AddNode({node.Name})");
                    if (msaglNodes.ContainsKey(questData.RowId))
                    {
                        return;
                    }

                    var node = new GraphNode(questData);
                    AddGraphNode(node);
                }
                catch (Exception ex)
                {
                    Plugin.Log.Error(ex, "ERROR AddNode");
                }
            }

            void AddNext(QuestData node)
            {
                if (node != null)
                {
                    foreach (var nodeId in node.NextQuestIds)
                    {
                        var nextNode = this.questsManager.QuestData[nodeId];
                        if (cancel.IsCancellationRequested)
                        {
                            return;
                        }

                        AddNode(nextNode);
                        if (nextNode.PreviousQuestsId.Count > 0)
                        {
                            AddNext(nextNode);
                        }
                    }
                }
            }

            void AddPrev(QuestData node)
            {
                if (node != null)
                {

                    if (node.RowId == 69602)
                    {
                        Plugin.Log.Info($"AAAAAAAAA - {node.Name}");
                    }

                    foreach (var nodeId in node.PreviousQuestsId)
                    {
                        var prevNode = this.questsManager.QuestData[nodeId];
                        if (cancel.IsCancellationRequested)
                        {
                            return;
                        }

                        if (prevNode.RowId == 69602)
                        {
                            Plugin.Log.Info($"BBBBBBB > {prevNode.PreviousQuestsId.Count} - {node.Name}");
                        }

                        AddNode(prevNode);
                        //if (prevNode.PreviousQuestsId.Count > 0 && !this.IsMSQ(prevNode))
                        if (prevNode.PreviousQuestsId.Count > 0)
                        {
                            AddPrev(prevNode);
                        }
                    }
                }
            }

            AddNode(questData);
            AddNext(questData);

            //var compressedMsq = this.CompressMSQ(questData);
            //if (compressedMsq != null)
            {
                //AddGraphNode(compressedMsq);
            }
            //else
            {
                //AddPrev(questData);
            }

            if (cancel.IsCancellationRequested)
            {
                return (null, null);
            }

            foreach (var (sourceId, targetId) in links)
            {
                try
                {
                    if (cancel.IsCancellationRequested)
                    {
                        return (null, null);
                    }

                    if (!msaglNodes.TryGetValue(sourceId, out var source) || !msaglNodes.TryGetValue(targetId, out var target))
                    {
                        continue;
                    }

                    var edge = new Edge(source, target);
                    //if (this.Plugin.Config.ShowArrowheads)
                    {
                        edge.EdgeGeometry = new EdgeGeometry
                        {
                            TargetArrowhead = new Arrowhead(),
                        };
                    }

                    g.Edges.Add(edge);
                }
                catch (Exception ex)
                {
                    Plugin.Log.Error(ex, "ERROR Links");
                }
            }

            LayoutHelpers.CalculateLayout(g, this.LayoutSettings, null);

            Node? centre = null;
            if (g.Nodes.Count > 0)
            {
                centre = g.Nodes[0];
            }

            return cancel.IsCancellationRequested
                ? (null, null)
                : (g, centre);
        }

        private GraphNode? CompressMSQ(QuestData questData)
        {
            var msqEndQuestName = questData.RowId switch
            {
                70058 => "A Realm Reborn (2.0)",
                66729 => "A Realm Awoken (2.1)",
                66899 => "Through the Maelstrom (2.2)",
                66996 => "Defenders of Eorzea (2.3)",
                65625 => "Dreams of Ice (2.4)",
                65965 => "Before the Fall - Part 1 (2.5)",
                65964 => "Before the Fall - Part 2 (2.55)",
                67205 => "Heavensward (3.0)",
                67699 => "As Goes Light, So Goes Darkness (3.1)",
                67777 => "The Gears of Change (3.2)",
                67783 => "Revenge of the Horde (3.3)",
                67886 => "Soul Surrender (3.4)",
                67891 => "The Far Edge of Fate - Part 1 (3.5)",
                67895 => "The Far Edge of Fate - Part 2 (3.56)",
                68089 => "Stormblood (4.0)",
                68508 => "The Legend Returns (4.1)",
                68565 => "Rise of a New Sun (4.2)",
                68612 => "Under the Moonlight (4.3)",
                68685 => "Prelude in Violet (4.4)",
                68719 => "A Requiem for Heroes - Part 1 (4.5)",
                68721 => "A Requiem for Heroes - Part 2 (4.56)",
                69190 => "Shadowbringers (5.0)",
                69218 => "Vows of Virtue, Deeds of Cruelty (5.1)",
                69306 => "Echoes of a Fallen Star (5.2)",
                69318 => "Reflections in Crystal (5.3)",
                69552 => "Futures Rewritten (5.4)",
                69599 => "Death Unto Dawn - Part 1 (5.5)",
                69602 => "Death Unto Dawn - Part 2 (5.55)",
                70000 => "Endwalker (6.0)",
                70062 => "Newfound Adventure (6.1)",
                70136 => "Buried Memory (6.2)",
                70214 => "Gods Revel, Lands Tremble (6.3)",
                70279 => "The Dark Throne (6.4)",
                70286 => "Growing Light (6.5)",
                70289 => "The Coming Dawn (6.55)",
                70495 => "Dawntrail (7.0)",
                70786 => "Crossroads (7.1)",
                70842 => "Seekers of Eternity (7.2)",
                _ => null,
            };

            if (msqEndQuestName != null)
            {
                return new GraphNode(msqEndQuestName);
            }

            return null;
        }
    }
}
