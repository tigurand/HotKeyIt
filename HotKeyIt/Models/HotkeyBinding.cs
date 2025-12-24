using System;
using Dalamud.Game.ClientState.Keys;

namespace HotKeyIt.Models;

[Serializable]
public sealed class HotkeyBinding
{
    public VirtualKey Key { get; set; } = VirtualKey.NO_KEY;
    public bool Ctrl { get; set; }
    public bool Alt { get; set; }
    public bool Shift { get; set; }

    public bool IsValid
        => Key != VirtualKey.NO_KEY;

    public override string ToString()
    {
        if (!IsValid)
            return "(none)";

        var parts = new System.Collections.Generic.List<string>(4);
        if (Ctrl)
            parts.Add("Ctrl");
        if (Alt)
            parts.Add("Alt");
        if (Shift)
            parts.Add("Shift");
        parts.Add(Key.GetFancyName());
        return string.Join("+", parts);
    }
}

