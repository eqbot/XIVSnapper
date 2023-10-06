using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
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
using MareSynchronos.Export;

namespace Snapper
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Snapper";
        private const string CommandName = "/psnap";

        public DalamudPluginInterface PluginInterface { get; init; }
        private ICommandManager CommandManager { get; init; }
        public Configuration Configuration { get; init; }
        public IObjectTable Objects { get; init; }
        public WindowSystem WindowSystem = new("Snapper");
        public FileDialogManager FileDialogManager = new FileDialogManager();
        public DalamudUtil DalamudUtil { get; init; }
        public IpcManager IpcManager { get; init; }
        public SnapshotManager SnapshotManager { get; init; }
        public MareCharaFileManager MCDFManager { get; init; }

        private ConfigWindow ConfigWindow { get; init; }
        private MainWindow MainWindow { get; init; }

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] ICommandManager commandManager,
            [RequiredVersion("1.0")] IFramework framework,
            [RequiredVersion("1.0")] IObjectTable objectTable,
            [RequiredVersion("1.0")] IClientState clientState,
            [RequiredVersion("1.0")] ICondition condition,
            [RequiredVersion("1.0")] IChatGui chatGui)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
            this.Objects = objectTable;

            this.DalamudUtil = new DalamudUtil(clientState, objectTable, framework, condition, chatGui);
            this.IpcManager = new IpcManager(pluginInterface, this.DalamudUtil);

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            this.SnapshotManager = new SnapshotManager(this);
            this.MCDFManager = new MareCharaFileManager(this);

            ConfigWindow = new ConfigWindow(this);
            MainWindow= new MainWindow(this);
            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(MainWindow);

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
            MainWindow.IsOpen = true;
        }

        private void DrawUI()
        {
            this.WindowSystem.Draw();
            this.FileDialogManager.Draw();
        }

        public void DrawConfigUI()
        {
            ConfigWindow.IsOpen = true;
        }
    }
}
