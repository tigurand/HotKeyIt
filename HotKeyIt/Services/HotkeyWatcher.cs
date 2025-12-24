using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using HotKeyIt.Models;

namespace HotKeyIt.Services;

public sealed class HotkeyWatcher : IDisposable
{
    private readonly IFramework framework;
    private readonly IKeyState keyState;
    private readonly Configuration configuration;
    private readonly MacroExecutor executor;
    private readonly Dictionary<Guid, bool> keyWasDown = new();

    public HotkeyWatcher(IFramework framework, IKeyState keyState, Configuration configuration, MacroExecutor executor)
    {
        this.framework = framework;
        this.keyState = keyState;
        this.configuration = configuration;
        this.executor = executor;

        this.framework.Update += OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (executor.IsRunning)
            return;

        configuration.EnsureDefaults();

        if (configuration.Profiles.Count == 0)
            return;

        var ctrlDown = IsCtrlDown();
        var altDown = IsAltDown();
        var shiftDown = IsShiftDown();

        foreach (var profile in configuration.Profiles)
        {
            if (!profile.Enabled)
                continue;

            var hk = profile.Hotkey;
            if (!hk.IsValid)
                continue;

            var requiredOk = (!hk.Ctrl || ctrlDown) && (!hk.Alt || altDown) && (!hk.Shift || shiftDown);
            var isDown = keyState[hk.Key];

            keyWasDown.TryGetValue(profile.Id, out var wasDown);
            keyWasDown[profile.Id] = isDown;

            if (!requiredOk)
                continue;

            if (isDown && !wasDown)
                executor.Start(profile.Macro);
        }
    }

    private bool IsCtrlDown()
        => keyState[VirtualKey.CONTROL] || keyState[VirtualKey.LCONTROL] || keyState[VirtualKey.RCONTROL];

    private bool IsAltDown()
        => keyState[VirtualKey.MENU] || keyState[VirtualKey.LMENU] || keyState[VirtualKey.RMENU];

    private bool IsShiftDown()
        => keyState[VirtualKey.SHIFT] || keyState[VirtualKey.LSHIFT] || keyState[VirtualKey.RSHIFT];

    public void Dispose()
        => framework.Update -= OnFrameworkUpdate;
}

