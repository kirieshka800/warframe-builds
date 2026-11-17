using System.ComponentModel.DataAnnotations;
using WarframeBuilds.Models;
namespace WarframeBuilds.ViewModels;
public class CreateBuildInput
{
    [Required(ErrorMessage = "Укажите название"), StringLength(120)] public string Title { get; set; } = "";
    public string TargetType { get; set; } = "warframe";
    public int? WarframeId { get; set; }
    public int? WeaponId { get; set; }
    [StringLength(12000)] public string? Description { get; set; }
    [Range(0, 100)] public int FormasCount { get; set; }
    public bool HasReactor { get; set; } = true;
    [StringLength(120)] public string? HelminthAbility { get; set; }
}
public class CreateBuildViewModel
{
    public CreateBuildInput Input { get; set; } = new();
    public List<Warframe> Warframes { get; set; } = new();
    public List<Weapon> Weapons { get; set; } = new();
}
public class BuildEditorViewModel
{
    public Build Build { get; set; } = null!;
    public BuildStatistics Stats { get; set; } = null!;
    public List<Mod> Mods { get; set; } = new();
    public bool IsOwner { get; set; }
    public List<BuildComment> Comments { get; set; } = new();
}
public class BuildCatalogViewModel
{
    public List<Build> Builds { get; set; } = new();
    public List<Warframe> Warframes { get; set; } = new();
    public List<Weapon> Weapons { get; set; } = new();
    public string? Search { get; set; }
    public string? Kind { get; set; }
    public bool Mine { get; set; }
    public int Total { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}
public class ArsenalViewModel
{
    public List<Warframe> Warframes { get; set; } = new();
    public List<Weapon> Weapons { get; set; } = new();
    public List<Mod> Mods { get; set; } = new();
    public string? Search { get; set; }
    public string Kind { get; set; } = "warframes";
    public int Total { get; set; }
    public int Page { get; set; } = 1;
}
