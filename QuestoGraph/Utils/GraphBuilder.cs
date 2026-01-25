using System.Diagnostics;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Miscellaneous;
using QuestoGraph.Data;
using QuestoGraph.Data.Graph;
using QuestoGraph.Data.Settings;
using QuestoGraph.Manager;

namespace QuestoGraph.Utils
{
    internal class GraphBuilder
    {
        private readonly LayoutAlgorithmSettings LayoutSettings = new SugiyamaLayoutSettings();

        private bool newStopwatch = false;

        private enum Side
        {
            Backward,
            Start,
            Forward
        }

        internal GraphData? Build(
            QuestsManager questsManager,
            QuestData questData,
            Config config,
            Stopwatch? sw,
            CancellationToken cancel)
        {
            if (questData is null)
            {
                return null;
            }

            if (sw is null)
            {
                this.newStopwatch = true;
                sw = new Stopwatch();
                sw.Start();
            }

            this.Log($"Building Graph for '{questData.Name} - '{sw.Elapsed}''", false);

            var nodes = this.BuildNodes(config, questsManager, questData, sw, cancel);
            var graph = this.CreateGraphWithEdges(config, nodes, sw, cancel);
            if (cancel.IsCancellationRequested)
            {
                return null;
            }

            this.Log($"Start Calculating Layout - '{sw.Elapsed}'");
            var msAglCancelToken = new CancelToken();
            using (var registration = cancel.Register(() => msAglCancelToken.Canceled = true))
            {
                LayoutHelpers.CalculateLayout(graph, LayoutSettings, msAglCancelToken);
            }
            this.Log($"Calculated Layout - '{sw.Elapsed}'");


            this.Log($"Finished Graph - '{sw.Elapsed}'", false);
            var centerNode = nodes.FirstOrDefault().Value.Node;
            if (this.newStopwatch)
            {
                sw!.Stop();
                this.newStopwatch = false;
            }

            return cancel.IsCancellationRequested
                ? null
                : new(graph, centerNode);
        }

        private Dictionary<uint, (Node Node, Side Side)> BuildNodes(
            Config config,
            QuestsManager questsManager,
            QuestData startQuestData,
            Stopwatch sw,
            CancellationToken cancel)
        {
            var nodes = new Dictionary<uint, (Node Node, Side Side)>();
            var stack = new Stack<(uint id, Side side)>();

            this.Log($"Determined Nodes - '{sw.Elapsed}'");
            stack.Push((startQuestData.RowId, Side.Start));
            while (stack.Count > 0)
            {
                if (cancel.IsCancellationRequested)
                {
                    this.Log($"Build was cancled (Nodes) - '{sw!.Elapsed}'", false);
                    return nodes;
                }

                var (id, side) = stack.Pop();
                if (nodes.ContainsKey(id) ||
                    !questsManager.QuestData.TryGetValue(id, out var currentQuestData))
                {
                    continue;
                }

                nodes[id] = (GraphUtils.GetNode(new NodeData(currentQuestData)), side);
                if (side != Side.Forward) // Start || Backward
                {
                    foreach (var prevId in currentQuestData.PreviousQuestsId)
                    {
                        if (config.Graph.CompressMSQ)
                        {
                            var result = GraphUtils.GetCompressMSQ(currentQuestData);
                            if (!string.IsNullOrEmpty(result.CompressedName))
                            {
                                this.Log($"Compressed Quest '{result.CompressedName}' (ID: {result.QuestId}) || Parent '{currentQuestData.Name}'- '{sw?.Elapsed}'");
                                var compressedMSQNode = GraphUtils.GetNode(new NodeData(prevId, result.CompressedName));
                                nodes[id] = (compressedMSQNode, Side.Backward);
                                continue;
                            }
                        }

                        stack.Push((prevId, Side.Backward));
                    }
                }

                if (side != Side.Backward) // Start || Forward
                {
                    foreach (var nextId in currentQuestData.NextQuestIds)
                    {
                        stack.Push((nextId, Side.Forward));
                    }
                }
            }
            this.Log($"Determined {nodes.Count} Nodes - '{sw!.Elapsed}'");

            return nodes;
        }

        private GeometryGraph CreateGraphWithEdges(Config config, Dictionary<uint, (Node Node, Side Side)> nodes, Stopwatch sw, CancellationToken cancel)
        {
            var graph = new GeometryGraph();
            foreach (var nodeInfo in nodes.Values)
            {
                graph.Nodes.Add(nodeInfo.Node);
            }
            this.Log($"Added {nodes.Count} Nodes - '{sw.Elapsed}'");

            this.Log($"Adding Edges - '{sw.Elapsed}'");
            foreach (var kvp in nodes)
            {
                if (nodes.TryGetValue(kvp.Key, out var toNodeInfo) &&
                    toNodeInfo.Node.UserData is NodeData nodeData)
                {
                    foreach (var prev in nodeData.PreviousIds)
                    {
                        if (cancel.IsCancellationRequested)
                        {
                            this.Log($"Build was cancled (Edges) - '{sw.Elapsed}'", false);
                            return graph;
                        }

                        if (nodes.TryGetValue(prev, out var fromNodeInfo))
                        {
                            var edge = new Edge(fromNodeInfo.Node, toNodeInfo.Node);
                            if (config.Graph.ShowArrowheads)
                            {
                                edge.EdgeGeometry = new EdgeGeometry
                                {
                                    TargetArrowhead = new Arrowhead(),
                                };
                            }

                            graph.Edges.Add(edge);
                        }
                    }
                }
            }

            this.Log($"Added Edges - '{sw.Elapsed}'");
            return graph;
        }

        private void Log(string message, bool isDebug = true)
        {
            var text = $"[{nameof(GraphBuilder)}] {message}";
            if (isDebug)
            {
                Plugin.Log.Debug(text);
            }
            else
            {
                Plugin.Log.Info(text);
            }
        }
    }
}
