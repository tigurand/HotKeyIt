using Dalamud.Game.ClientState.Keys;

namespace HotKeyIt.Models;

public static class HotkeyKeys
{
    // A curated list of "common" keys.
    // (Mouse buttons and IME/special keys are excluded.)
    public static readonly VirtualKey[] Common =
    {
        // Letters
        VirtualKey.A, VirtualKey.B, VirtualKey.C, VirtualKey.D, VirtualKey.E, VirtualKey.F, VirtualKey.G, VirtualKey.H, VirtualKey.I,
        VirtualKey.J, VirtualKey.K, VirtualKey.L, VirtualKey.M, VirtualKey.N, VirtualKey.O, VirtualKey.P, VirtualKey.Q, VirtualKey.R,
        VirtualKey.S, VirtualKey.T, VirtualKey.U, VirtualKey.V, VirtualKey.W, VirtualKey.X, VirtualKey.Y, VirtualKey.Z,

        // Digits
        VirtualKey.KEY_0, VirtualKey.KEY_1, VirtualKey.KEY_2, VirtualKey.KEY_3, VirtualKey.KEY_4,
        VirtualKey.KEY_5, VirtualKey.KEY_6, VirtualKey.KEY_7, VirtualKey.KEY_8, VirtualKey.KEY_9,

        // Numpad digits
        VirtualKey.NUMPAD0, VirtualKey.NUMPAD1, VirtualKey.NUMPAD2, VirtualKey.NUMPAD3, VirtualKey.NUMPAD4,
        VirtualKey.NUMPAD5, VirtualKey.NUMPAD6, VirtualKey.NUMPAD7, VirtualKey.NUMPAD8, VirtualKey.NUMPAD9,

        // Numpad operations
        VirtualKey.ADD, VirtualKey.SUBTRACT, VirtualKey.MULTIPLY, VirtualKey.DIVIDE, VirtualKey.DECIMAL,

        // Function keys
        VirtualKey.F1, VirtualKey.F2, VirtualKey.F3, VirtualKey.F4, VirtualKey.F5, VirtualKey.F6,
        VirtualKey.F7, VirtualKey.F8, VirtualKey.F9, VirtualKey.F10, VirtualKey.F11, VirtualKey.F12,

        // Navigation
        VirtualKey.UP, VirtualKey.DOWN, VirtualKey.LEFT, VirtualKey.RIGHT,
        VirtualKey.HOME, VirtualKey.END, VirtualKey.PRIOR, VirtualKey.NEXT, // PageUp / PageDown
        VirtualKey.INSERT, VirtualKey.DELETE,

        // Editing / control
        VirtualKey.ESCAPE, VirtualKey.TAB, VirtualKey.RETURN, VirtualKey.SPACE, VirtualKey.BACK,

        // Punctuation
        VirtualKey.OEM_1, VirtualKey.OEM_2, VirtualKey.OEM_3, VirtualKey.OEM_4, VirtualKey.OEM_5, VirtualKey.OEM_6, VirtualKey.OEM_7,
        VirtualKey.OEM_PLUS, VirtualKey.OEM_COMMA, VirtualKey.OEM_MINUS, VirtualKey.OEM_PERIOD,
    };
}

