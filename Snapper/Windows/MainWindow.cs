using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using ImGuiNET;
using ImGuiScene;

namespace Snapper.Windows;

public partial class MainWindow : Window, IDisposable
{
    private const float SelectorWidth = 200;

    private Plugin Plugin;

    public MainWindow(Plugin plugin) : base(
        "Snapper", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.Plugin = plugin;
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        if (ImGui.Button("Show Settings"))
        {
            this.Plugin.DrawConfigUI();
        }

        ImGui.SameLine();
        if(ImGui.Button("Revert penumbra temp."))
        {
            this.Plugin.SnapshotManager.RevertAllSnapshots();
        }

        ImGui.Spacing();
        /*
        ImGui.Text("Have a goat:");
        ImGui.Indent(55);
        ImGui.Image(this.GoatImage.ImGuiHandle, new Vector2(this.GoatImage.Width, this.GoatImage.Height));
        ImGui.Unindent(55);*/

        this.DrawPlayerSelector();
        if (!currentLabel.Any())
            return;

        ImGui.SameLine();
        this.DrawActorPanel();
    }

}
