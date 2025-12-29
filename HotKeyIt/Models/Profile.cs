using System;

namespace HotKeyIt.Models;

[Serializable]
public sealed class Profile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public bool Enabled { get; set; } = true;
    public string Name { get; set; } = "New Profile";
    public HotkeyBinding Hotkey { get; set; } = new();
    public string Macro { get; set; } = string.Empty;
    public bool KeyPassthrough { get; set; } = false;
}

