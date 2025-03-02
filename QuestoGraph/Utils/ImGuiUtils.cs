using System.Numerics;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using QuestoGraph.Data;
using QuestoGraph.Data.Settings;

namespace QuestoGraph.Utils
{
    internal static class ImGuiUtils
    {
        internal class FreeCursorPos : IDisposable
        {
            internal enum CursorReset
            {
                None,
                All,
                X,
                Y,
            }

            public Vector2 LastPos { get; }

            public CursorReset Reset { get; }

            public FreeCursorPos(CursorReset reset = CursorReset.All)
            {
                this.LastPos = ImGui.GetCursorPos();
                this.Reset = reset;
            }

            public void SetX(float x)
                => ImGui.SetCursorPosX(x);

            public void SetY(float y)
                => ImGui.SetCursorPosY(y);

            public void SetPos(float x, float y)
                => ImGui.SetCursorPos(new Vector2(x, y));

            public void Dispose()
            {
                switch (this.Reset)
                {
                    case CursorReset.All:
                        ImGui.SetCursorPos(this.LastPos);
                        break;
                    case CursorReset.X:
                        ImGui.SetCursorPosX(this.LastPos.X);
                        break;
                    case CursorReset.Y:
                        ImGui.SetCursorPosY(this.LastPos.Y);
                        break;
                }
            }
        }

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

        internal static void AddQuestImage(QuestData questData, uint maxAllowedWidth = 524)
            => AddQuestImage(questData.Quest.Icon, questData.RowId, maxAllowedWidth);

        internal static void AddSpecialQuestImage(QuestData questData, uint maxAllowedWidth = 524)
            => AddQuestImage(questData.Quest.IconSpecial, questData.RowId, maxAllowedWidth);

        internal static void AddQuestImage(uint icon, uint questRowId, uint maxAllowedWidth = 524)
        {
            var image = GetIcon(icon);
            var padding = 7f;
            var contentRectX = ImGui.GetContentRegionAvail().X - (2 * padding);
            var maxWidth = Math.Clamp(contentRectX, padding, maxAllowedWidth);
            if (image != null)
            {
                var imageSize = new Vector2(maxWidth + (padding * 2), (maxWidth * image.Height) / image.Width);
                ImGui.SetCursorPosX(0 + padding);
                ImGui.Image(image.ImGuiHandle, imageSize);
            }
            else
            {
                var imageSize = new Vector2(maxWidth, (maxWidth * 360) / 1128); // Default Quest Image size
                ImGuiHelpers.ScaledDummy(imageSize);
                ImGui.SameLine();
                const string text = "== no image ==";
                ImGui.SetCursorPos((imageSize - ImGui.CalcTextSize(text)) * 0.5f);
                ImGui.Text(text);
            }

            if (QuestManager.IsQuestComplete(questRowId))
            {
                var doneIcon = GetIcon(60081);
                if (doneIcon != null)
                {
                    const ushort doneIconSize = 32;
                    var tempCursorPos = ImGui.GetCursorPos();
                    ImGui.SetCursorPos(new Vector2(ImGui.GetContentRegionAvail().X - doneIconSize - 5f, tempCursorPos.Y - doneIconSize - 10f));
                    ImGui.Image(doneIcon.ImGuiHandle, new Vector2(doneIconSize, doneIconSize));
                    Tooltip("Quest completed");
                    ImGui.SetCursorPos(tempCursorPos);
                }
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

        internal static bool SelectableQuest(QuestData questData, string? nameSuffix, ref bool isSelected)
        {
            using (var color = new ImRaii.Color())
            {
                if (QuestManager.IsQuestComplete(questData.RowId))
                {
                    color.Push(ImGuiCol.Text, 0xee76c922u);
                }

                if (ImGui.Selectable($"{questData.Name}##{questData.RowId}{nameSuffix ?? string.Empty}", isSelected))
                {
                    return true;
                }

                return false;
            }
        }
    }
}
