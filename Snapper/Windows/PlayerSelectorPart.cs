using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Penumbra.GameData.Structs;

namespace Snapper.Windows
{
    public partial class MainWindow
    {
        public static int GPoseObjectId = ObjectIndex.GPosePlayer.Index;
        public static int CharacterScreenIndex = ObjectIndex.CharacterScreen.Index;
        public static int ExamineScreenIndex = ObjectIndex.ExamineScreen.Index;
        public static int FittingRoomIndex = ObjectIndex.FittingRoom.Index;
        public static int DyePreviewIndex = ObjectIndex.DyePreview.Index;

        private string playerFilter = string.Empty;
        private string playerFilterLower = string.Empty;
        private string currentLabel = string.Empty;
        private ICharacter? player;
        private bool modifiable = false;
        private int? objIdxSelected;
        private ICharacter? playerSelected;
        private readonly Dictionary<string, int> playerNames = new(100);
        private readonly Dictionary<string, ICharacter?> gPoseActors = new(CharacterScreenIndex - GPoseObjectId);

        private IPluginLog PluginLog { get; init; }
        private void DrawPlayerFilter()
        {
            using var raii = new ImGuiRaii()
                .PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
                .PushStyle(ImGuiStyleVar.FrameRounding, 0);
            ImGui.SetNextItemWidth(SelectorWidth * ImGui.GetIO().FontGlobalScale);
            if (ImGui.InputTextWithHint("##playerFilter", "Filter Players...", ref playerFilter, 32))
                playerFilterLower = playerFilter.ToLowerInvariant();
        }

        private void DrawGPoseSelectable(ICharacter player, int objIdx)
        {
            var playerName = player.Name.ToString();
            if (!playerName.Any())
                return;

            gPoseActors[playerName] = null;

            DrawSelectable(player, $"{playerName} (GPose)", true, objIdx);
        }

        private void DrawSelectable(ICharacter player, string label, bool modifiable, int objIdx)
        {
            if (!playerFilterLower.Any() || label.ToLowerInvariant().Contains(playerFilterLower))
                if (ImGui.Selectable(label, currentLabel == label))
                {
                    currentLabel = label;
                    this.player = player;
                    this.objIdxSelected = objIdx;
                    this.modifiable = modifiable;
                    return;
                }

            if (currentLabel != label)
                return;

            try
            {
                this.player = player;
                this.objIdxSelected = objIdx;
                this.modifiable = modifiable;
            }
            catch (Exception e)
            {
                PluginLog.Error($"Could not load character {player.Name}s information:\n{e}");
            }
        }

        private void DrawPlayerSelectable(ICharacter player, int idx)
        {
            string playerName;
            bool modifiable = false;

            if(idx == CharacterScreenIndex)
            {
                playerName = "Character Screen Actor";
            }
            else if(idx == ExamineScreenIndex)
            {
                playerName = "Examine Screen Actor";
            }
            else if(idx == FittingRoomIndex)
            {
                playerName = "Fitting Room Actor";
            }
            else if(idx == DyePreviewIndex)
            {
                playerName = "Dye Preview Actor";
            }
            else
            {
                playerName = player.Name.ToString();
            }


            if (!playerName.Any())
                return;

            if (playerNames.TryGetValue(playerName, out var num))
                playerNames[playerName] = ++num;
            else
                playerNames[playerName] = num = 1;

            if (gPoseActors.ContainsKey(playerName))
            {
                gPoseActors[playerName] = player;
                return;
            }

            var label = GetLabel(player, playerName, num);
            DrawSelectable(player, label, modifiable, idx);
        }

        private static string GetLabel(ICharacter player, string playerName, int num)
        {
            if (player.ObjectKind == ObjectKind.Player)
                return num == 1 ? playerName : $"{playerName} #{num}";

            if (player.ObjectKind == ObjectKind.EventNpc)
                return num == 1 ? $"{playerName} (NPC)" : $"{playerName} #{num} (NPC)";

            return num == 1 ? $"{playerName} (Monster)" : $"{playerName} #{num} (Monster)";
        }

        private void DrawPlayerSelector()
        {
            ImGui.BeginGroup();
            DrawPlayerFilter();
            if (!ImGui.BeginChild("##playerSelector",
                    new Vector2(SelectorWidth * ImGui.GetIO().FontGlobalScale, -ImGui.GetFrameHeight() - 1), true))
            {
                ImGui.EndChild();
                ImGui.EndGroup();
                return;
            }

            playerNames.Clear();
            gPoseActors.Clear();

            for (var i = GPoseObjectId; i < GPoseObjectId + 48; ++i)
            {
                var player = CharacterFactory.Convert(Plugin.Objects[i]);
  
                if (player == null)
                    continue;

                DrawGPoseSelectable(player, i);
            }

            for (var i = 0; i < GPoseObjectId; ++i)
            {
                var player = CharacterFactory.Convert(Plugin.Objects[i]);
                if (player != null)
                    DrawPlayerSelectable(player, i);
            }

            for (var i = CharacterScreenIndex; i < Plugin.Objects.Length; ++i)
            {
                var player = CharacterFactory.Convert(Plugin.Objects[i]);
                if (player != null)
                    DrawPlayerSelectable(player, i);
            }
            ImGui.EndChild();


            //DrawSelectionButtons();
            ImGui.EndGroup();
        }
    }
}
