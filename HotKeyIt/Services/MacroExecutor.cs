using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Dalamud.Plugin.Services;
using ECommons.Automation;

namespace HotKeyIt.Services;

public sealed class MacroExecutor : IDisposable
{
    private readonly IFramework framework;
    private readonly ICommandManager commandManager;
    private readonly IPluginLog log;

    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly Queue<MacroStep> queue = new();

    private long nextExecuteAtMs;
    private bool isRunning;

    private static readonly Regex WaitTagRegex = new(@"<\s*wait\s*\.\s*(?<sec>\d+(?:\.\d+)?)\s*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public MacroExecutor(IFramework framework, IChatGui chatGui, IPluginLog log)
    {
        this.framework = framework;

        commandManager = Service.CommandManager;
        this.log = log;
        this.framework.Update += OnFrameworkUpdate;
    }

    public MacroExecutor(IFramework framework, ICommandManager commandManager, IPluginLog log)
    {
        this.framework = framework;
        this.commandManager = commandManager;
        this.log = log;
        this.framework.Update += OnFrameworkUpdate;
    }

    public bool IsRunning
        => isRunning;

    public void Start(string macroText)
    {
        queue.Clear();
        foreach (var step in Parse(macroText))
            queue.Enqueue(step);

        if (queue.Count == 0)
        {
            isRunning = false;
            return;
        }

        isRunning = true;
        nextExecuteAtMs = clock.ElapsedMilliseconds;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!isRunning)
            return;

        if (clock.ElapsedMilliseconds < nextExecuteAtMs)
            return;

        if (queue.Count == 0)
        {
            isRunning = false;
            return;
        }

        var step = queue.Dequeue();

        if (!string.IsNullOrWhiteSpace(step.LineToSend))
        {
            try
            {
                Chat.SendMessage(step.LineToSend);
            }
            catch (Exception ex)
            {
                log.Error(ex, "Failed to send macro line: {Line}", step.LineToSend);
            }
        }

        var delayMs = (long)Math.Round(step.DelaySeconds * 1000.0, MidpointRounding.AwayFromZero);
        nextExecuteAtMs = clock.ElapsedMilliseconds + Math.Max(0, delayMs);
    }

    private static IEnumerable<MacroStep> Parse(string macroText)
    {
        if (string.IsNullOrWhiteSpace(macroText))
            yield break;

        var lines = macroText.Replace("\r", string.Empty).Split('\n');
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (TryParseWaitLine(line, out var waitSeconds))
            {
                yield return new MacroStep(null, waitSeconds);
                continue;
            }

            var delay = 0.0;
            var cleaned = WaitTagRegex.Replace(line, m =>
            {
                if (double.TryParse(m.Groups["sec"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var sec))
                    delay += sec;
                return string.Empty;
            }).Trim();

            yield return new MacroStep(cleaned.Length == 0 ? null : cleaned, delay);
        }
    }

    private static bool TryParseWaitLine(string line, out double seconds)
    {
        seconds = 0;
        if (!line.StartsWith("/wait", StringComparison.OrdinalIgnoreCase))
            return false;

        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
            return false;

        return double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out seconds);
    }

    public void Dispose()
        => framework.Update -= OnFrameworkUpdate;

    private readonly record struct MacroStep(string? LineToSend, double DelaySeconds);
}

