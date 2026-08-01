using System.Numerics;
using Microsoft.Msagl.Core.Layout;

namespace QuestoGraph.Data.Graph
{
    internal struct DrawnNode
    {

        internal Vector2 Start { get; set; }

        internal Vector2 End { get; set; }

        internal Node Node { get; set; } = null!;

        public DrawnNode(Vector2 start, Vector2 end, Node node)
        {
            this.Start = start;
            this.End = end;
            this.Node = node;
        }

        public readonly bool TryGetNodeData<T>(out T? data)
        {
            data = default;
            if (this.Node?.UserData is T obj)
            {
                data = obj;
                return true;
            }

            return false;
        }
    }
}
