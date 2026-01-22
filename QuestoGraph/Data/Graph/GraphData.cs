using Microsoft.Msagl.Core.Layout;

namespace QuestoGraph.Data.Graph
{
    internal class GraphData
    {
        public GeometryGraph Graph { get; set; }

        public Node CenterNode { get; set; }

        public GraphData(GeometryGraph graph, Node centerNode)
        {
            this.Graph = graph;
            this.CenterNode = centerNode;
        }
    }
}
