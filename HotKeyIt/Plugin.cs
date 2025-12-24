using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using ECommons;
using System;
using System.Linq;
using HotKeyIt.Services;
using HotKeyIt.Windows;
using OtterGui.Classes;
using OtterGui.Log;
using OtterGui.Services;

namespace HotKeyIt;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/hotkeyit";
    private const string CommandAlias = "/hki";

    public Configuration Configuration { get; init; }

    internal ServiceManager OtterServices { get; }
    internal KeyboardManager KeyboardManager { get; }
    internal MacroExecutor MacroExecutor { get; }
    internal HotkeyWatcher HotkeyWatcher { get; }

    public readonly WindowSystem WindowSystem = new("HotKeyIt");
    private HotKeyItWindow MainWindow { get; init; }

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        Service.Initialize(pluginInterface);

        ECommonsMain.Init(Service.PluginInterface, this);

        Configuration = Service.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.EnsureDefaults();

        OtterServices = new ServiceManager(new Logger());
        OtterServices.AddExistingService(Service.Framework);
        OtterServices.AddExistingService(Service.KeyState);
        OtterServices.CreateProvider();

        KeyboardManager = new KeyboardManager(Service.Framework, Service.KeyState);
        MacroExecutor = new MacroExecutor(Service.Framework, Service.CommandManager, Service.Log);
        HotkeyWatcher = new HotkeyWatcher(Service.Framework, Service.KeyState, Configuration, MacroExecutor);

        MainWindow = new HotKeyItWindow(this);

        WindowSystem.AddWindow(MainWindow);

        Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open HotKeyIt window, or run a profile macro: /hotkeyit <profile name>"
        });

        Service.CommandManager.AddHandler(CommandAlias, new CommandInfo(OnCommand)
        {
            HelpMessage = "Alias for /hotkeyit"
        });

        Service.PluginInterface.UiBuilder.Draw += WindowSystem.Draw;

        Service.PluginInterface.UiBuilder.OpenConfigUi += ToggleMainUi;

        Service.PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
    }

    public void Dispose()
    {
        Service.PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        Service.PluginInterface.UiBuilder.OpenConfigUi -= ToggleMainUi;
        Service.PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        
        WindowSystem.RemoveAllWindows();

        MainWindow.Dispose();

        MacroExecutor.Dispose();
        HotkeyWatcher.Dispose();
        KeyboardManager.Dispose();

        OtterServices.Dispose();

        ECommonsMain.Dispose();

        Service.CommandManager.RemoveHandler(CommandName);
        Service.CommandManager.RemoveHandler(CommandAlias);
    }

    private void OnCommand(string command, string args)
    {
        args = args?.Trim() ?? string.Empty;
        if (args.Length == 0)
        {
            MainWindow.Toggle();
            return;
        }

        var profile = Configuration.Profiles.FirstOrDefault(p => string.Equals(p.Name, args, StringComparison.OrdinalIgnoreCase));
        if (profile == null)
        {
            Service.ChatGui.Print($"[HotKeyIt] Profile '{args}' not found.");
            return;
        }

        MacroExecutor.Start(profile.Macro);
    }

    public void ToggleMainUi() => MainWindow.Toggle();
}
