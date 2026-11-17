namespace WarframeBuilds.Models;
public class Weapon
{
 public int Id { get; set; }
 public string Name { get; set; } = "";
 public string UniqueName { get; set; } = "";
 public string Category { get; set; } = "";
 public string WeaponType { get; set; } = "";
 public string Description { get; set; } = "";
 public string Polarity { get; set; } = "";
 public string Rarity { get; set; } = "";
 public double BaseDamage { get; set; }
 public double CriticalChance { get; set; }
 public double CriticalMultiplier { get; set; }
 public double StatusChance { get; set; }
 public double FireRate { get; set; }
 public int MagazineSize { get; set; }
 public double ReloadTime { get; set; }
 public string? ImageName { get; set; }
}
