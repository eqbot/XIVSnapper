using System;
using System.IO;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Snapper.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;
    private FileDialogManager FileDialogManager;

    public ConfigWindow(Plugin plugin) : base(
        "Snapper Settings",
        ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.Size = new Vector2(500, 75);
        this.SizeCondition = ImGuiCond.Always;

        this.Configuration = plugin.Configuration;
        this.FileDialogManager = plugin.FileDialogManager;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var workingDirectory = Configuration.WorkingDirectory;
        ImGui.InputText("Working Folder", ref workingDirectory, 255, ImGuiInputTextFlags.ReadOnly);
        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        string folderIcon = FontAwesomeIcon.Folder.ToIconString();
        if (ImGui.Button(folderIcon))
        {
            FileDialogManager.OpenFolderDialog("Snapper working directory", (status, path) =>
            {
                if (!status)
                {
                    return;
                }

                if (Directory.Exists(path))
                {
                    this.Configuration.WorkingDirectory = path;
                    this.Configuration.Save();
                }
            });
        }
        ImGui.PopFont();
    }
}
