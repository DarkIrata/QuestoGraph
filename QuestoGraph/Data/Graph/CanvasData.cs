
using System.Numerics;

namespace QuestoGraph.Data.Graph
{
    internal struct CanvasData
    {
        public Vector2 Topleft { get; }

        public Vector2 BottomRight { get; }

        public Vector2 Center { get; }

        public Vector2 Pivot { get; internal set; }

        public CanvasData(Vector2 topleft, Vector2 bottomRight)
        {
            this.Topleft = topleft;
            this.BottomRight = bottomRight;
            this.Center = (topleft + bottomRight) * 0.5f;
        }
    }
}
