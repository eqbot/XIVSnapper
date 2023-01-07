using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Snapper.Models;

public class FileReplacement
{
    private readonly Plugin plugin;

    public FileReplacement(Plugin plugin)
    {
        this.plugin = plugin;
    }

    public bool Computed => IsFileSwap || !HasFileReplacement || !string.IsNullOrEmpty(Hash);

    public List<string> GamePaths { get; set; } = new();

    public bool HasFileReplacement => GamePaths.Count >= 1 && GamePaths.Any(p => !string.Equals(p, ResolvedPath, System.StringComparison.Ordinal));

    public bool IsFileSwap => !Regex.IsMatch(ResolvedPath, @"^[a-zA-Z]:(/|\\)", RegexOptions.ECMAScript) && !string.Equals(GamePaths.First(), ResolvedPath, System.StringComparison.Ordinal);

    public string Hash { get; set; } = string.Empty;

    public string ResolvedPath { get; set; } = string.Empty;

    private void SetResolvedPath(string path)
    {
        ResolvedPath = path.ToLowerInvariant().Replace('\\', '/');
        if (!HasFileReplacement || IsFileSwap) return;
    }

    public bool Verify()
    {
        ResolvePath(GamePaths.First());

        var success = IsFileSwap;

        return success;
    }

    public override string ToString()
    {
        StringBuilder builder = new();
        builder.AppendLine($"Modded: {HasFileReplacement} - {string.Join(",", GamePaths)} => {ResolvedPath}");
        return builder.ToString();
    }

    internal void ReverseResolvePath(string path)
    {
        GamePaths = plugin.IpcManager.PenumbraReverseResolvePlayer(path).ToList();
        SetResolvedPath(path);
    }

    internal void ResolvePath(string path)
    {
        GamePaths = new List<string> { path };
        SetResolvedPath(plugin.IpcManager.PenumbraResolvePath(path));
    }

    internal void ResolvePathObject(string path, int objIdx)
    {
        GamePaths = new List<string> { path };
        SetResolvedPath(plugin.IpcManager.PenumbraResolvePathObject(path, objIdx));
    }

    internal void ReverseResolvePathObject(string path, int objIdx)
    {
        GamePaths = plugin.IpcManager.PenumbraReverseResolveObject(path, objIdx).ToList();
        SetResolvedPath(path);
    }
}
