using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ECommons;
using HotKeyIt.Models;
using OtterGui.Raii;
using System;
using System.Numerics;

namespace HotKeyIt.Windows;

public sealed class HotKeyItWindow : Window, IDisposable
{
    private readonly Plugin hkiPlugin;

    private string nameBuffer = string.Empty;
    private string macroBuffer = string.Empty;
    private HotkeyBinding hotkeyBuffer = new();
    private bool enabledBuffer;
    private bool keyPassthroughBuffer;

    private bool dirty;

    public HotKeyItWindow(Plugin plugin)
        : base("HotKeyIt")
    {
        hkiPlugin = plugin;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(650, 400),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };

        this.TitleBarButtons.Add(new TitleBarButton { ShowTooltip = () => ImGui.SetTooltip("Support on Ko-fi"), Icon = FontAwesomeIcon.Heart, IconOffset = new Vector2(1, 1), Click = _ => GenericHelpers.ShellStart("https://ko-fi.com/lucillebagul") });
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        var cfg = hkiPlugin.Configuration;
        cfg.EnsureDefaults();

        using var table = ImRaii.Table("##layout", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.BordersInnerV);
        if (!table)
            return;

        ImGui.TableSetupColumn("Profiles", ImGuiTableColumnFlags.WidthFixed, 220);
        ImGui.TableSetupColumn("Editor", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextRow();

        DrawProfilesColumn(cfg);
        DrawEditorColumn(cfg);
    }

    private static float CalcIconTextButtonWidth(FontAwesomeIcon icon, string text)
    {
        var style = ImGui.GetStyle();
        var iconString = icon.ToIconString();
        Vector2 iconSize;
        using (ImRaii.PushFont(UiBuilder.IconFont))
            iconSize = ImGui.CalcTextSize(iconString);
        var textSize = ImGui.CalcTextSize(text);
        return style.FramePadding.X * 2 + iconSize.X + style.ItemInnerSpacing.X + textSize.X;
    }

    private static bool IconTextButton(string id, FontAwesomeIcon icon, string text, Vector2? size = null)
    {
        var style = ImGui.GetStyle();

        var iconString = icon.ToIconString();
        Vector2 iconSize;
        using (ImRaii.PushFont(UiBuilder.IconFont))
            iconSize = ImGui.CalcTextSize(iconString);
        var textSize = ImGui.CalcTextSize(text);

        var height = Math.Max(iconSize.Y, textSize.Y) + style.FramePadding.Y * 2;
        var width = style.FramePadding.X * 2 + iconSize.X + style.ItemInnerSpacing.X + textSize.X;

        var buttonSize = size ?? new Vector2(width, height);
        if (buttonSize.X <= 0)
            buttonSize.X = width;
        if (buttonSize.Y <= 0)
            buttonSize.Y = height;

        var pos = ImGui.GetCursorScreenPos();

        var pressed = ImGui.InvisibleButton(id, buttonSize);
        var hovered = ImGui.IsItemHovered();
        var held = ImGui.IsItemActive();

        var col = ImGui.GetColorU32(held ? ImGuiCol.ButtonActive : hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button);
        var colBorder = ImGui.GetColorU32(ImGuiCol.Border);
        var colText = ImGui.GetColorU32(ImGuiCol.Text);

        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(pos, pos + buttonSize, col, style.FrameRounding);
        drawList.AddRect(pos, pos + buttonSize, colBorder, style.FrameRounding);

        var textPos = pos + style.FramePadding;
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            drawList.AddText(UiBuilder.IconFont, ImGui.GetFontSize(), textPos, colText, iconString);
        }

        textPos.X += iconSize.X + style.ItemInnerSpacing.X;
        drawList.AddText(textPos, colText, text);

        return pressed;
    }

    private void DrawProfilesColumn(Configuration cfg)
    {
        ImGui.TableSetColumnIndex(0);

        var footerHeight = ImGui.GetFrameHeightWithSpacing();
        using (var child = ImRaii.Child("##profiles", new Vector2(0, -footerHeight), true))
        {
            if (child)
            {
                for (var i = 0; i < cfg.Profiles.Count; i++)
                {
                    var p = cfg.Profiles[i];
                    var selected = p.Id == cfg.SelectedProfileId;
                    if (ImGui.Selectable($"{p.Name}##{p.Id}", selected))
                    {
                        cfg.SelectedProfileId = p.Id;
                        cfg.Save();
                        SyncBuffersFromSelected(cfg);
                    }
                }
            }
        }

        using var footer = ImRaii.Group();
        var canRemove = cfg.Profiles.Count > 0;
        var deleteArmed = ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift;

        if (ImGui.Button("+", new Vector2(30, 0)))
        {
            var p = new Profile { Name = $"Profile {cfg.Profiles.Count + 1}" };
            cfg.Profiles.Add(p);
            cfg.SelectedProfileId = p.Id;
            cfg.Save();
            SyncBuffersFromSelected(cfg);
        }

        ImGui.SameLine();
        using (ImRaii.Disabled(!canRemove || !deleteArmed))
        {
            if (ImGui.Button("-", new Vector2(30, 0)) && canRemove && deleteArmed)
            {
                var idx = cfg.Profiles.FindIndex(p => p.Id == cfg.SelectedProfileId);
                if (idx < 0 && cfg.Profiles.Count > 0)
                    idx = 0;

                if (idx >= 0 && idx < cfg.Profiles.Count)
                    cfg.Profiles.RemoveAt(idx);

                cfg.SelectedProfileId = cfg.Profiles.Count > 0 ? cfg.Profiles[Math.Clamp(idx - 1, 0, cfg.Profiles.Count - 1)].Id : Guid.Empty;
                cfg.Save();
                SyncBuffersFromSelected(cfg);
            }
        }

        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(deleteArmed ? "Delete selected profile" : "Hold Ctrl+Shift to delete profile");
    }

    private void DrawEditorColumn(Configuration cfg)
    {
        ImGui.TableSetColumnIndex(1);

        var profile = cfg.GetSelectedProfile();
        if (profile == null)
        {
            ImGui.TextUnformatted("No saved profile yet.");
            ImGui.TextUnformatted("Use the + button on the left to create a profile.");
            return;
        }

        if (nameBuffer.Length == 0 && macroBuffer.Length == 0)
            SyncBuffersFromSelected(cfg);

        var footerHeight = ImGui.GetFrameHeightWithSpacing();
        using (var body = ImRaii.Child("##editorBody", new Vector2(0, -footerHeight), false))
        {
            if (!body)
                return;

            ImGui.TextUnformatted("Profile Settings");
            ImGui.Separator();

            using (var form = ImRaii.Table("##profileForm", 2, ImGuiTableFlags.SizingFixedFit))
            {
                if (form)
                {
                    ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 110);
                    ImGui.TableSetupColumn("Field", ImGuiTableColumnFlags.WidthStretch);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextUnformatted("Enable");
                    ImGui.TableNextColumn();
                    var enabled = enabledBuffer;
                    if (ImGui.Checkbox("##enabled", ref enabled))
                    {
                        enabledBuffer = enabled;
                        dirty = true;
                    }

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextUnformatted("Profile Name");
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    ImGui.InputText("##profileName", ref nameBuffer, 128);
                    if (ImGui.IsItemEdited())
                        dirty = true;
                }
            }

            ImGui.Spacing();
            ImGui.TextUnformatted("Hotkey");

            var ctrl = hotkeyBuffer.Ctrl;
            if (ImGui.Checkbox("Ctrl", ref ctrl))
            {
                hotkeyBuffer.Ctrl = ctrl;
                dirty = true;
            }
            ImGui.SameLine();
            var alt = hotkeyBuffer.Alt;
            if (ImGui.Checkbox("Alt", ref alt))
            {
                hotkeyBuffer.Alt = alt;
                dirty = true;
            }
            ImGui.SameLine();
            var shift = hotkeyBuffer.Shift;
            if (ImGui.Checkbox("Shift", ref shift))
            {
                hotkeyBuffer.Shift = shift;
                dirty = true;
            }

            ImGui.SetNextItemWidth(240);
            var keys = HotkeyKeys.Common;
            OtterGui.Widgets.Widget.KeySelector("Key", "Select the non-modifier key for the hotkey.", hotkeyBuffer.Key,
                k =>
                {
                    hotkeyBuffer.Key = k;
                    dirty = true;
                },
                keys);

            ImGui.SameLine();
            ImGui.TextUnformatted($"Current: {hotkeyBuffer}");

            var keyPassthrough = keyPassthroughBuffer;
            if (ImGui.Checkbox("Pass Input to Game", ref keyPassthrough))
            {
                keyPassthroughBuffer = keyPassthrough;
                dirty = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Disables the hotkey from blocking the game input.");

            ImGui.Spacing();
            ImGui.TextUnformatted("Macro");
            ImGui.SetNextItemWidth(-1);

            var macroHeight = Math.Max(ImGui.GetTextLineHeight() * 8, ImGui.GetContentRegionAvail().Y);
            ImGui.InputTextMultiline("##macro", ref macroBuffer, 20000, new Vector2(-1, macroHeight));
            if (ImGui.IsItemEdited())
                dirty = true;
        }

        if (IconTextButton("##testRun", FontAwesomeIcon.Play, "Test Run"))
            hkiPlugin.MacroExecutor.Start(macroBuffer);

        ImGui.SameLine();
        var saveWidth = CalcIconTextButtonWidth(FontAwesomeIcon.Save, "Save");
        var saveX = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - saveWidth;
        if (saveX > ImGui.GetCursorPosX())
            ImGui.SetCursorPosX(saveX);

        using (ImRaii.Disabled(!dirty))
        {
            if (IconTextButton("##save", FontAwesomeIcon.Save, "Save", new Vector2(saveWidth, 0)) && dirty)
            {
                profile.Enabled = enabledBuffer;
                profile.Name = nameBuffer.Trim().Length == 0 ? "(unnamed)" : nameBuffer.Trim();
                profile.Macro = macroBuffer;
                profile.KeyPassthrough = keyPassthroughBuffer;
                profile.Hotkey = new HotkeyBinding
                {
                    Key = hotkeyBuffer.Key,
                    Ctrl = hotkeyBuffer.Ctrl,
                    Alt = hotkeyBuffer.Alt,
                    Shift = hotkeyBuffer.Shift,
                };

                cfg.Save();
                dirty = false;
            }
        }
    }

    private void SyncBuffersFromSelected(Configuration cfg)
    {
        var profile = cfg.GetSelectedProfile();
        if (profile == null)
        {
            nameBuffer = string.Empty;
            macroBuffer = string.Empty;
            hotkeyBuffer = new HotkeyBinding();
            keyPassthroughBuffer = false;
            dirty = false;
            return;
        }

        nameBuffer = profile.Name;
        macroBuffer = profile.Macro;
        enabledBuffer = profile.Enabled;
        keyPassthroughBuffer = profile.KeyPassthrough;
        hotkeyBuffer = new HotkeyBinding
        {
            Key = profile.Hotkey.Key,
            Ctrl = profile.Hotkey.Ctrl,
            Alt = profile.Hotkey.Alt,
            Shift = profile.Hotkey.Shift,
        };

        dirty = false;
    }
}

