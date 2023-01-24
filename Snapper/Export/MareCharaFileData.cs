using Dalamud.Game.ClientState.Objects.Enums;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MareSynchronos.Export;

public record MareCharaFileData
{
    public string Description { get; set; } = string.Empty;
    public string GlamourerData { get; set; } = string.Empty;
    public string CustomizePlusData { get; set; } = string.Empty;
    public string ManipulationData { get; set; } = string.Empty;
    public List<FileData> Files { get; set; } = new();
    public List<FileSwap> FileSwaps { get; set; } = new();

    public MareCharaFileData() { }

    public byte[] ToByteArray()
    {
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this));
    }

    public static MareCharaFileData FromByteArray(byte[] data)
    {
        return JsonConvert.DeserializeObject<MareCharaFileData>(Encoding.UTF8.GetString(data))!;
    }

    public record FileSwap(IEnumerable<string> GamePaths, string FileSwapPath);

    public record FileData(IEnumerable<string> GamePaths, long Length);
}
