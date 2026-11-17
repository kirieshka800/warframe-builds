namespace WarframeBuilds.Models;

public class GearItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Sentinel, Archwing, Necramech, Operator, Arcane, etc.
    public string Description { get; set; } = string.Empty;
    public string Polarity { get; set; } = string.Empty;
    public string Rarity { get; set; } = string.Empty;
}