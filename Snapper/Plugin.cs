using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Reflection;
using Dalamud.Interface.Windowing;
using Snapper.Windows;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Game.ClientState.Objects;
using Snapper.Utils;
using Dalamud.Game.ClientState;
using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.Game.ClientState.Conditions;
using Snapper.Managers;

namespace Snapper
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Snapper";
        private const string CommandName = "/psnap";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        public Configuration Configuration { get; init; }
        public ObjectTable Objects { get; init; }
        public WindowSystem WindowSystem = new("Snapper");
        public FileDialogManager FileDialogManager = new FileDialogManager();
        public DalamudUtil DalamudUtil { get; init; }
        public IpcManager IpcManager { get; init; }
        public SnapshotManager SnapshotManager { get; init; }


        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
            [RequiredVersion("1.0")] Framework framework,
            [RequiredVersion("1.0")] ObjectTable objectTable,
            [RequiredVersion("1.0")] ClientState clientState,
            [RequiredVersion("1.0")] Condition condition,
            [RequiredVersion("1.0")] ChatGui chatGui)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
            this.Objects = objectTable;

            this.DalamudUtil = new DalamudUtil(clientState, objectTable, framework, condition, chatGui);
            this.IpcManager = new IpcManager(pluginInterface, this.DalamudUtil);

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            this.SnapshotManager = new SnapshotManager(this);


            WindowSystem.AddWindow(new ConfigWindow(this));
            WindowSystem.AddWindow(new MainWindow(this));

            this.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens main Snapper interface"
            });

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            this.PluginInterface.UiBuilder.DisableGposeUiHide = true;
        }

        public void Dispose()
        {
            this.WindowSystem.RemoveAllWindows();
            this.CommandManager.RemoveHandler(CommandName);
            this.SnapshotManager.RevertAllSnapshots();
        }

        private void OnCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            WindowSystem.GetWindow("Snapper").IsOpen = true;
        }

        private void DrawUI()
        {
            this.WindowSystem.Draw();
            this.FileDialogManager.Draw();
        }

        public void DrawConfigUI()
        {
            WindowSystem.GetWindow("Snapper Settings").IsOpen = true;
        }
    }
}
