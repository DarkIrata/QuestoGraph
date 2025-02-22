using System.Numerics;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using QuestoGraph.Data;

namespace QuestoGraph.Utils
{
    internal static class ImGuiUtils
    {
        internal static IDalamudTextureWrap? GetIcon(uint id)
            => id > 0 ? Plugin.TextureProvider.GetFromGameIcon(new GameIconLookup(id)).GetWrapOrDefault() : null;

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

        internal static void WriteTitledText(string title, string? text, int depth = 1)
        {
            ImGui.TextUnformatted(title);
            using (var indent = new ImRaii.Indent())
            {
                indent.Push(depth);
                ImGui.TextUnformatted(text ?? string.Empty);
            }
        }

        internal static void AddQuestImage(QuestData questData, uint maxAllowedWidth = 525, bool center = true)
            => AddQuestImage(questData.Quest.Icon, maxAllowedWidth, center);

        internal static void AddSpecialQuestImage(QuestData questData, uint maxAllowedWidth = 525, bool center = true)
            => AddQuestImage(questData.Quest.IconSpecial, maxAllowedWidth, center);

        internal static void AddQuestImage(uint icon, uint maxAllowedWidth = 525, bool center = true)
        {
            var image = GetIcon(icon);
            if (image != null)
            {
                var contentRect = ImGui.GetContentRegionAvail();
                var maxWidth = Math.Clamp(contentRect.X, 100, maxAllowedWidth);
                var sizeVector = new Vector2(maxWidth, (maxWidth * image.Height) / image.Width);
                if (center)
                {
                    ImGui.SetCursorPosX((ImGui.GetContentRegionAvail().X - sizeVector.X) * 0.5f);
                }
                ImGui.Image(image.ImGuiHandle, sizeVector);
            }
        }

        internal static void AddIcon(uint iconId, string? tooltip = null, float iconSizeDiv = 3.3f, bool setSameLine = true)
        {
            var icon = GetIcon(iconId);
            if (icon != null)
            {
                AddIcon(icon, icon.Width / iconSizeDiv, icon.Height / iconSizeDiv, tooltip, setSameLine);
            }
        }

        internal static void AddIcon(uint iconId, float width, float height, string? tooltip = null, bool setSameLine = true)
            => AddIcon(GetIcon(iconId), width, height, tooltip, setSameLine);

        internal static void AddIcon(IDalamudTextureWrap? icon, float width, float height, string? tooltip = null, bool setSameLine = true)
        {
            if (icon != null)
            {
                //ImGui.Image(icon.ImGuiHandle, new Vector2(width, height), Vector2.Zero, Vector2.One, new Vector4(1f, 1f, 1f, 0.25f));
                ImGui.Image(icon.ImGuiHandle, new Vector2(width, height), Vector2.Zero, Vector2.One, new Vector4(1f, 1f, 1f, 1f));
                if (!string.IsNullOrEmpty(tooltip))
                {
                    Tooltip(tooltip);
                }

                if (setSameLine)
                {
                    ImGui.SameLine();
                }
            }
        }
    }
}
