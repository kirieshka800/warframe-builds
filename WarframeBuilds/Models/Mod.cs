namespace WarframeBuilds.Models;
public class Mod
{
 public int Id { get; set; }
 public string Name { get; set; } = "";
 public string UniqueName { get; set; } = "";
 public string Description { get; set; } = "";
 public string Compatibility { get; set; } = "";
 public string Type { get; set; } = "";
 public bool IsExilus { get; set; }
 public string LevelStatsJson { get; set; } = "[]";
 public int BaseDrain { get; set; }
 public string Polarity { get; set; } = "";
 public string Rarity { get; set; } = "";
 public int BaseRank { get; set; }
 public int MaxRank { get; set; }
 public string? ImageName { get; set; }
}
