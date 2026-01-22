using System.Numerics;
using Dalamud.Bindings.ImGui;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using QuestoGraph.Data;
using QuestoGraph.Data.Graph;

namespace QuestoGraph.Utils
{
    internal static class GraphUtils
    {
        internal static readonly Vector2 TextOffset = new(5, 2);

        internal static Vector2 ToVector2(this Point p)
            => new((float)p.X, (float)p.Y);

        // imgui measures from top left as 0,0
        internal static Vector2 GetTopLeft(this GeometryObject item, Vector2 offset)
            => item.BoundingBox.RightTop.ToVector2() + offset;

        internal static Vector2 GetBottomRight(this GeometryObject item, Vector2 offset)
            => item.BoundingBox.LeftBottom.ToVector2() + offset;

        internal static Node GetNode(NodeData nodeData)
        {
            var dims = ImGui.CalcTextSize(nodeData.Text) + TextOffset * 2;
            return new Node(CurveFactory.CreateRectangle(dims.X, dims.Y, new Point()), nodeData);
        }

        internal static (uint QuestId, string? CompressedName) GetCompressMSQ(QuestData? questData)
        {
            if (questData == null)
            {
                return (0, null);
            }

            var name = questData.RowId switch
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
                70909 => "The Promise of Tomorrow (7.3)",
                70970 => "Into the Mist (7.4)",
                _ => null,
            };

            return (questData.RowId, name);
        }
    }
}
