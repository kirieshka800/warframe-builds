using System.Text.Json.Serialization;
namespace WarframeBuilds.Models;
public class WeaponItemDto
{
 [JsonPropertyName("weaponClass")] public string? WeaponClass { get; set; }
 [JsonPropertyName("name")] public string Name { get; set; } = "";
 [JsonPropertyName("uniqueName")] public string UniqueName { get; set; } = "";
 [JsonPropertyName("category")] public string Category { get; set; } = "";
 [JsonPropertyName("type")] public string WeaponType { get; set; } = "";
 [JsonPropertyName("description")] public string? Description { get; set; } = null;
 [JsonPropertyName("polarity")] public string Polarity { get; set; } = "";
 [JsonPropertyName("rarity")] public string Rarity { get; set; } = "";
 [JsonPropertyName("imageName")] public string? ImageName { get; set; } = null;
 [JsonPropertyName("totalDamage")] public double BaseDamage { get; set; } = 0;
 [JsonPropertyName("criticalChance")] public double CriticalChance { get; set; } = 0;
 [JsonPropertyName("criticalMultiplier")] public double CriticalMultiplier { get; set; } = 0;
 [JsonPropertyName("procChance")] public double StatusChance { get; set; } = 0;
 [JsonPropertyName("fireRate")] public double FireRate { get; set; } = 0;
 [JsonPropertyName("magazineSize")] public int MagazineSize { get; set; } = 0;
 [JsonPropertyName("reloadTime")] public double ReloadTime { get; set; } = 0;
}
