using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using ImGuiNET;
using Snapper.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static Lumina.Data.Parsing.Layer.LayerCommon;

namespace Snapper.Windows
{
    public partial class MainWindow
    {
        private const uint RedHeaderColor = 0xFF1818C0;
        private const uint GreenHeaderColor = 0xFF18C018;

        private void DrawPlayerHeader()
        {
            var color =player == null ? RedHeaderColor : GreenHeaderColor;
            var buttonColor = ImGui.GetColorU32(ImGuiCol.FrameBg);
            ImGui.Button($"{currentLabel}##playerHeader", -Vector2.UnitX * 0.0001f);
        }

        private void DrawPlayerPanel()
        {
            ImGui.Text("Save snapshot of player ");
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            try
            {
                string saveIcon = FontAwesomeIcon.Save.ToIconString();
                if (ImGui.Button(saveIcon))
                {
                    //save snapshot
                    if (player != null)
                        Plugin.SnapshotManager.SaveSnapshot(player);
                }
            }
            finally
            {
                ImGui.PopFont();
            }

            ImGui.Text("Load snapshot onto ");
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            try
            {
                string loadIcon = FontAwesomeIcon.Clipboard.ToIconString();
                if (ImGui.Button(loadIcon))
                {
                    Plugin.FileDialogManager.OpenFolderDialog("Snapshot selection", (status, path) =>
                    {
                        if (!status)
                        {
                            return;
                        }

                        if (Directory.Exists(path))
                        {
                            if (player != null && objIdxSelected.HasValue)
                                Plugin.SnapshotManager.LoadSnapshot(player, objIdxSelected.Value, path);
                        }
                    }, Plugin.Configuration.WorkingDirectory);
                }
            }
            finally
            {
                ImGui.PopFont();
            }
        }

        private void DrawMonsterPanel()
        {

        }

        private void DrawActorPanel()
        {
            using var raii = ImGuiRaii.NewGroup();
            DrawPlayerHeader();
            if (!ImGui.BeginChild("##playerData", -Vector2.One, true))
            {
                ImGui.EndChild();
                return;
            }

            if (player == null || player.ModelType() == 0)
                DrawPlayerPanel();
            else
                DrawMonsterPanel();

            ImGui.EndChild();
        }
    }
}
