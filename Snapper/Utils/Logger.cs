using System;
using System.Diagnostics;
using Dalamud.Logging;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Microsoft.Extensions.Logging;

namespace Snapper.Utils;

internal class Logger : ILogger
{
    private readonly string name;

    public static void Info(string info)
    {
        var caller = new StackTrace().GetFrame(1)?.GetMethod()?.ReflectedType?.Name ?? "Unknown";
        Plugin.Log.Information($"[{caller}] {info}", "");
    }

    public static void Debug(string debug, string stringToHighlight = "")
    {
        var caller = new StackTrace().GetFrame(1)?.GetMethod()?.ReflectedType?.Name ?? "Unknown";
        if (debug.Contains(stringToHighlight, StringComparison.Ordinal) && !stringToHighlight.IsNullOrEmpty())
        {
            Plugin.Log.Warning($"[{caller}] {debug}");
        }
        else
        {
            Plugin.Log.Debug($"[{caller}] {debug}");
        }
    }

    public static void Error(string msg, Exception ex)
    {
        var caller = new StackTrace().GetFrame(1)?.GetMethod()?.ReflectedType?.Name ?? "Unknown";
        Plugin.Log.Error($"[{caller}] {msg} {Environment.NewLine} Exception: {ex.Message} {Environment.NewLine} {ex.StackTrace}");
    }

    public static void Warn(string msg, Exception ex)
    {
        var caller = new StackTrace().GetFrame(1)?.GetMethod()?.ReflectedType?.Name ?? "Unknown";
        Plugin.Log.Warning($"[{caller}] {msg} {Environment.NewLine} Exception: {ex.Message} {Environment.NewLine} {ex.StackTrace}");
    }

    public static void Error(string msg)
    {
        var caller = new StackTrace().GetFrame(1)?.GetMethod()?.ReflectedType?.Name ?? "Unknown";
        Plugin.Log.Error($"[{caller}] {msg}");
    }

    public static void Warn(string warn)
    {
        var caller = new StackTrace().GetFrame(1)?.GetMethod()?.ReflectedType?.Name ?? "Unknown";
        Plugin.Log.Warning($"[{caller}] {warn}");
    }

    public static void Verbose(string verbose)
    {
        var caller = new StackTrace().GetFrame(1)?.GetMethod()?.ReflectedType?.Name ?? "Unknown";
#if DEBUG
        Plugin.Log.Debug($"[{caller}] {verbose}");
#else
        Plugin.Log.Verbose($"[{caller}] {verbose}");
#endif
    }
    public Logger(string name)
    {
        this.name = name;
    }
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        switch (logLevel)
        {
            case LogLevel.Debug:
                Plugin.Log.Debug($"[{name}] [{eventId}] {formatter(state, exception)}");
                break;
            case LogLevel.Error:
            case LogLevel.Critical:
                Plugin.Log.Error($"[{name}] [{eventId}] {formatter(state, exception)}");
                break;
            case LogLevel.Information:
                Plugin.Log.Information($"[{name}] [{eventId}] {formatter(state, exception)}");
                break;
            case LogLevel.Warning:
                Plugin.Log.Warning($"[{name}] [{eventId}] {formatter(state, exception)}");
                break;
            case LogLevel.Trace:
            default:
#if DEBUG
                Plugin.Log.Verbose($"[{name}] [{eventId}] {formatter(state, exception)}");
#else
                Plugin.Log.Verbose($"[{name}] {eventId} {state} {formatter(state, exception)}");
#endif
                break;
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable BeginScope<TState>(TState state) => default!;
}
