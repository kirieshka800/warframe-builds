using System.Text.Json.Serialization;
namespace WarframeBuilds.Models;
public class ModItemDto
{
 [JsonPropertyName("name")] public string Name { get; set; } = "";
 [JsonPropertyName("uniqueName")] public string UniqueName { get; set; } = "";
 [JsonPropertyName("description")] public string? Description { get; set; } = null;
 [JsonPropertyName("baseDrain")] public int BaseDrain { get; set; } = 0;
 [JsonPropertyName("polarity")] public string? Polarity { get; set; } = null;
 [JsonPropertyName("rarity")] public string? Rarity { get; set; } = null;
 [JsonPropertyName("fusionLimit")] public int FusionLimit { get; set; } = 0;
 [JsonPropertyName("imageName")] public string? ImageName { get; set; } = null;
 [JsonPropertyName("compatName")] public string Compatibility { get; set; } = "";
 [JsonPropertyName("type")] public string Type { get; set; } = "";
 [JsonPropertyName("isExilus")] public bool IsExilus { get; set; } = false;
 [JsonPropertyName("levelStats")] public System.Text.Json.JsonElement LevelStats { get; set; } = default;
}
