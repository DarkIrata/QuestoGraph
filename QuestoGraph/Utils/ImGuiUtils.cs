using System.Numerics;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using ImGuiNET;

namespace QuestoGraph.Utils
{
    internal static class ImGuiUtils
    {
        internal static IDalamudTextureWrap? GetIcon(uint id)
            => Plugin.TextureProvider.GetFromGameIcon(new GameIconLookup(id)).GetWrapOrDefault();

        internal static void Tooltip(string tooltip)
        {
            if (!ImGui.IsItemHovered())
            {
                return;
            }

            ImGui.BeginTooltip();
            ImGui.TextUnformatted(tooltip);
            ImGui.EndTooltip();
        }

        internal static void AddIcon(uint iconId, string? tooltip = null, float iconDiv = 3.3f)
        {
            var icon = GetIcon(iconId);
            if (icon != null)
            {
                //ImGui.Image(icon.ImGuiHandle, new Vector2(icon.Width / iconDiv, icon.Height / iconDiv), Vector2.Zero, Vector2.One, new Vector4(1f, 1f, 1f, 0.25f));
                ImGui.Image(icon.ImGuiHandle, new Vector2(icon.Width / iconDiv, icon.Height / iconDiv), Vector2.Zero, Vector2.One, new Vector4(1f, 1f, 1f, 1f));
                if (!string.IsNullOrEmpty(tooltip))
                {
                    Tooltip(tooltip);
                }

                ImGui.SameLine();
            }
        }
    }
}
