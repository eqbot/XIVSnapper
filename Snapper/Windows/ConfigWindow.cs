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

    //ImGuiWindowFlags.NoResize
    public ConfigWindow(Plugin plugin) : base(
        "Snapper Settings",
         ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoResize)
    {
        this.Size = new Vector2(500, 115);
        this.SizeCondition = ImGuiCond.Always;

        this.Configuration = plugin.Configuration;
        this.FileDialogManager = plugin.FileDialogManager;
    }

    private string _singleLineText = "This is a long line of text that should word-wrap inside the text box based on the width you specify.";

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

        
        ImGui.Text("Glamourer design fallback string (Temp until GlamourerAPI workaround)");
        string fallbackString = Configuration.FallBackGlamourerString;
        ImGui.InputText("##input-format", ref fallbackString, 2500);
        if (fallbackString != Configuration.FallBackGlamourerString)
        {
            Configuration.FallBackGlamourerString = fallbackString;
            Configuration.Save();
        }
    }
}
