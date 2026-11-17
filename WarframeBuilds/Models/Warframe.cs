namespace WarframeBuilds.Models;

public class Warframe
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty; // Добавь это
    public int BaseHealth { get; set; }
    public int BaseShield { get; set; } // И это
    public int BaseArmor { get; set; }
    public int BaseEnergy { get; set; }
    public string? ImageUrl { get; set; } // И это
    public string? ImageName { get; set; }
}