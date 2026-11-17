using System.ComponentModel.DataAnnotations;
namespace WarframeBuilds.Models;
public class Build
{
    public int Id { get; set; }
    [Required, StringLength(120)] public string Title { get; set; } = "";
    [StringLength(12000)] public string? Description { get; set; }
    public string? UserId { get; set; }
    public string AuthorName { get; set; } = "Tenno";
    [Range(0, 100)] public int FormasCount { get; set; }
    [StringLength(120)] public string? HelminthAbility { get; set; }
    public bool HasReactor { get; set; } = true;
    public int? WarframeId { get; set; }
    public Warframe? Warframe { get; set; }
    public int? WeaponId { get; set; }
    public Weapon? Weapon { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<BuildMod> BuildMods { get; set; } = new();
    public string TargetName => Warframe?.Name ?? Weapon?.Name ?? "Неизвестное снаряжение";
    public bool IsWeapon => WeaponId.HasValue;
}
public class BuildMod
{
    public int Id { get; set; }
    public int BuildId { get; set; }
    public Build Build { get; set; } = null!;
    public int SlotIndex { get; set; }
    public int? ModId { get; set; }
    public Mod? Mod { get; set; }
    public int CurrentRank { get; set; }
    public string SlotPolarity { get; set; } = "None";
}
