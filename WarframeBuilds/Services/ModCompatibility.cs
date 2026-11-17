using System.Text.RegularExpressions;
using WarframeBuilds.Models;

namespace WarframeBuilds.Services;

public static class ModCompatibility
{
    public static readonly string[] Polarities = ["None", "Madurai", "Vazarin", "Naramon", "Zenurik", "Unairu", "Umbra", "Universal"];
    public static int[] ValidSlots(Build build) => build.WarframeId.HasValue || build.Warframe != null || IsMelee(build)
        ? Enumerable.Range(0, 10).ToArray() : [0, 1, 2, 3, 4, 5, 6, 7, 9];
    public static bool IsMelee(Build build) => string.Equals(build.Weapon?.Category, "Melee", StringComparison.OrdinalIgnoreCase);
    public static string Family(Mod mod) => Regex.Replace(mod.Name.Trim(), @"^(?:(?:Primed|Umbral|Archon|Galvanized|Amalgam|Sacrificial|Flawed)\s+)+", "", RegexOptions.IgnoreCase).ToLowerInvariant();
    public static bool CanEquip(Build build, Mod mod, int slot)
    {
        if (!ValidSlots(build).Contains(slot)) return false;
        var type = mod.Type.Trim();
        var compatibility = mod.Compatibility.Trim();
        bool aura = type.Contains("Aura", StringComparison.OrdinalIgnoreCase) || compatibility.Equals("AURA", StringComparison.OrdinalIgnoreCase);
        bool stance = type.Contains("Stance", StringComparison.OrdinalIgnoreCase);
        bool frame = build.WarframeId.HasValue || build.Warframe != null;
        if (slot == 8) return frame ? aura && MatchesFrame(build, compatibility) : IsMelee(build) && stance && MatchesWeapon(build, compatibility);
        if (aura || stance || (slot == 9 && !mod.IsExilus)) return false;
        if (frame) return MatchesFrame(build, compatibility) && !WeaponTypes.Contains(type, StringComparer.OrdinalIgnoreCase);
        return build.Weapon != null && MatchesWeapon(build, compatibility) && !type.Contains("Warframe", StringComparison.OrdinalIgnoreCase);
    }
    private static readonly string[] WeaponTypes = ["Rifle", "Shotgun", "Pistol", "Secondary", "Primary", "Melee", "Bow", "Sniper", "Launcher", "Archgun", "Archmelee", "Companion", "Sentinel"];
    private static bool MatchesFrame(Build build, string value)
    {
        if (value.Equals("Warframe", StringComparison.OrdinalIgnoreCase) || value.Equals("Aura", StringComparison.OrdinalIgnoreCase)) return true;
        var name = build.Warframe?.Name ?? "";
        return value.Length > 0 && (value.Equals(name, StringComparison.OrdinalIgnoreCase) || value.Equals(name.Replace(" Prime", ""), StringComparison.OrdinalIgnoreCase));
    }
    private static string Singular(string value)
    {
        var name = value.Trim().ToLowerInvariant();
        return name switch { "staves" => "staff", "knives" => "knife", _ => name.TrimEnd('s') };
    }
    private static bool MatchesWeapon(Build build, string value)
    {
        var weapon = build.Weapon;
        if (weapon == null || value.Length == 0) return false;
        if (value.Equals(weapon.Name, StringComparison.OrdinalIgnoreCase) || value.Equals(weapon.Name.Replace(" Prime", ""), StringComparison.OrdinalIgnoreCase)) return true;
        if (value.Equals(weapon.Category, StringComparison.OrdinalIgnoreCase) || value.Equals(weapon.WeaponType, StringComparison.OrdinalIgnoreCase)) return true;
        if (weapon.Category.Equals("Melee", StringComparison.OrdinalIgnoreCase) && Singular(value) == Singular(weapon.WeaponType)) return true;
        // WFCD uses Pistol for the secondary mod pool and Rifle for non-shotgun primary guns.
        if (value.Equals("Pistol", StringComparison.OrdinalIgnoreCase) && weapon.Category == "Secondary") return true;
        if (value.Equals("Rifle", StringComparison.OrdinalIgnoreCase) && weapon.Category == "Primary" && !weapon.WeaponType.Contains("Shotgun", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}
