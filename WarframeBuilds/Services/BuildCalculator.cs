using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using WarframeBuilds.Models;

namespace WarframeBuilds.Services;

public static class BuildCalculator
{
    public static int ModCost(Mod mod, int rank, string polarity, bool specialSlot = false)
    {
        int drain = Math.Max(0, (specialSlot ? Math.Abs(mod.BaseDrain) : mod.BaseDrain) + Math.Clamp(rank, 0, mod.MaxRank));
        bool none = string.IsNullOrWhiteSpace(polarity) || polarity.Equals("None", StringComparison.OrdinalIgnoreCase);
        bool match = !none && (polarity.Equals(mod.Polarity, StringComparison.OrdinalIgnoreCase) || polarity.Equals("Universal", StringComparison.OrdinalIgnoreCase));
        return specialSlot ? (none ? drain : match ? drain * 2 : (int)Math.Floor(drain * .8))
            : none ? drain : (int)Math.Ceiling(drain * (match ? .5 : 1.25));
    }

    public static BuildStatistics Calculate(Build build)
    {
        var result = new BuildStatistics { Capacity = build.HasReactor ? 60 : 30 };
        var bonuses = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var families = new HashSet<string>();
        foreach (var slot in build.BuildMods.OrderBy(x => x.SlotIndex))
        {
            var mod = slot.Mod;
            if (mod == null) continue;
            if (!ModCompatibility.CanEquip(build, mod, slot.SlotIndex))
            {
                result.Warnings.Add($"{mod.Name}: несовместимый слот или снаряжение; эффект не учтён.");
                continue;
            }
            if (!families.Add(ModCompatibility.Family(mod)))
            {
                result.Warnings.Add($"{mod.Name}: повтор семейства модов; эффект не учтён.");
                continue;
            }
            bool special = slot.SlotIndex == 8;
            int cost = ModCost(mod, slot.CurrentRank, slot.SlotPolarity, special);
            if (special) result.Capacity += cost; else result.UsedCapacity += cost;
            var stats = RankStats(mod, slot.CurrentRank);
            if (stats.Count == 0) result.Warnings.Add($"{mod.Name}: отсутствуют данные эффекта для выбранного ранга.");
            foreach (var rawStat in stats)
            {
                bool bowBonus = Regex.IsMatch(rawStat, @"\(x2 for bows\)", RegexOptions.IgnoreCase) && (build.Weapon?.WeaponType.Contains("Bow", StringComparison.OrdinalIgnoreCase) ?? false);
                var stat = Regex.Replace(rawStat, @"\(x2 for (?:bows|heavy attacks)\)", "", RegexOptions.IgnoreCase).Trim();
                if (Regex.IsMatch(stat, @"\b(enemies|enemy|allies|ally|companions|companion)\b", RegexOptions.IgnoreCase))
                { result.Warnings.Add($"{mod.Name}: эффект для другой цели не включён в личные характеристики."); continue; }
                // Conditional, set, proc and triggered effects must not become permanent base statistics.
                if (Regex.IsMatch(stat, @"\b(on|while|after|when|per|for|chance to|stack|kill|hit|aiming|sliding|wall|heavy attack|set bonus)\b", RegexOptions.IgnoreCase))
                { result.Warnings.Add($"{mod.Name}: условный эффект не включён в базовые характеристики."); continue; }
                bool any = false;
                foreach (Match match in Regex.Matches(stat, @"(?<n>[+−-]?\d+(?:[.,]\d+)?)\s*%\s*(?<label>[^+−\d\r\n;\-]+)", RegexOptions.IgnoreCase))
                {
                    var key = StatKey(match.Groups["label"].Value);
                    if (key == null) continue;
                    double amount = double.Parse(match.Groups["n"].Value.Replace('−', '-').Replace(',', '.'), CultureInfo.InvariantCulture) / 100;
                    if (bowBonus && key == "firerate") amount *= 2;
                    bonuses[key] = bonuses.GetValueOrDefault(key) + amount;
                    any = true;
                }
                if (!any) result.Warnings.Add($"{mod.Name}: эффект «{stat}» не рассчитывается.");
            }
        }
        double Scale(string key) => Math.Max(0, 1 + bonuses.GetValueOrDefault(key));
        if (build.Warframe is { } frame)
        {
            result.Warnings.Add("Параметры рассчитаны от базовых значений каталога; рост характеристик по рангу и пассивные способности не моделируются.");
            result.Health = frame.BaseHealth * Scale("health"); result.Shields = frame.BaseShield * Scale("shields");
            result.Armor = frame.BaseArmor * Scale("armor"); result.Energy = frame.BaseEnergy * Scale("energy");
            result.AbilityStrength = 100 * Scale("strength"); result.AbilityDuration = 100 * Scale("duration");
            result.AbilityRange = 100 * Scale("range"); result.AbilityEfficiency = Math.Clamp(100 * Scale("efficiency"), 0, 175);
        }
        if (build.Weapon is { } weapon)
        {
            result.Damage = weapon.BaseDamage * Scale("damage") * (1 + bonuses.GetValueOrDefault("element"));
            result.CriticalChance = weapon.CriticalChance * Scale("criticalchance"); result.CriticalMultiplier = weapon.CriticalMultiplier * Scale("criticaldamage");
            result.StatusChance = weapon.StatusChance * Scale("status"); result.FireRate = weapon.FireRate * Scale("firerate");
            result.Multishot = Scale("multishot"); result.MagazineSize = Math.Floor(weapon.MagazineSize * Scale("magazine"));
            result.ReloadTime = weapon.ReloadTime / Math.Max(.01, Scale("reload"));
            if (weapon.BaseDamage <= 0) result.Warnings.Add("Для оружия отсутствуют базовые данные урона.");
            result.Warnings.Add("Урон указан за попадание до сопротивлений. Условные эффекты, наборы, реакции статуса и отдельные режимы огня не учитываются.");
        }
        if (result.UsedCapacity > result.Capacity) result.Warnings.Add("Превышена вместимость модов.");
        result.Warnings = result.Warnings.Distinct().ToList();
        return result;
    }
    private static List<string> RankStats(Mod mod, int rank)
    {
        try
        {
            using var json = JsonDocument.Parse(mod.LevelStatsJson);
            if (json.RootElement.ValueKind != JsonValueKind.Array || json.RootElement.GetArrayLength() == 0) return [];
            var entry = json.RootElement[Math.Clamp(rank, 0, Math.Min(mod.MaxRank, json.RootElement.GetArrayLength() - 1))];
            if (!entry.TryGetProperty("stats", out var stats) || stats.ValueKind != JsonValueKind.Array) return [];
            return stats.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => Regex.Replace(x.GetString() ?? "", "<[^>]+>", "")).ToList();
        }
        catch (JsonException) { return []; }
    }
    private static string? StatKey(string label)
    {
        label = label.Trim().TrimEnd('.', ',').ToLowerInvariant();
        return label switch
        {
            "health" => "health", "shield capacity" or "shields" => "shields", "armor" => "armor", "energy max" or "maximum energy" or "energy" => "energy",
            "ability strength" => "strength", "ability duration" => "duration", "ability range" => "range", "ability efficiency" => "efficiency",
            "damage" or "melee damage" => "damage", "critical chance" => "criticalchance", "critical damage" or "critical multiplier" => "criticaldamage",
            "status chance" => "status", "fire rate" or "attack speed" => "firerate", "multishot" => "multishot", "magazine capacity" => "magazine", "reload speed" => "reload",
            "heat" or "cold" or "electricity" or "toxin" or "heat damage" or "cold damage" or "electricity damage" or "toxin damage" => "element", _ => null
        };
    }
}
