namespace WarframeBuilds.Models;

public sealed class BuildStatistics
{
    public int Capacity { get; set; }
    public int UsedCapacity { get; set; }
    public double Health { get; set; }
    public double Shields { get; set; }
    public double Armor { get; set; }
    public double Energy { get; set; }
    public double AbilityStrength { get; set; } = 100;
    public double AbilityDuration { get; set; } = 100;
    public double AbilityRange { get; set; } = 100;
    public double AbilityEfficiency { get; set; } = 100;
    public double Damage { get; set; }
    public double CriticalChance { get; set; }
    public double CriticalMultiplier { get; set; }
    public double StatusChance { get; set; }
    public double FireRate { get; set; }
    public double Multishot { get; set; } = 1;
    public double MagazineSize { get; set; }
    public double ReloadTime { get; set; }
    public List<string> Warnings { get; set; } = new();
}
