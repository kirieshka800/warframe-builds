using System.Collections.ObjectModel;
using WarframeBuilds.Models;

namespace WarframeBuilds.Services;

/// <summary>A deterministic catalogue-based starting point, using explicitly curated mod priorities.</summary>
public static class AutoBuildPlanner
{
    private static readonly IReadOnlyDictionary<string, string> FrameGoals = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
    {
        ["balanced"] = "Баланс", ["survival"] = "Выживаемость", ["strength"] = "Сила способностей",
        ["duration"] = "Длительность", ["range"] = "Радиус", ["efficiency"] = "Энергоэффективность"
    });
    private static readonly IReadOnlyDictionary<string, string> WeaponGoals = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
    { ["balanced"] = "Баланс", ["critical"] = "Критический урон", ["status"] = "Статус" });
    public static IReadOnlyDictionary<string, string> Goals(bool isWeapon) => isWeapon ? WeaponGoals : FrameGoals;

    public static AutoBuildPlan Plan(Build target, IReadOnlyList<Mod> catalog, string goal, int maxFormas = 0)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(catalog);
        if (maxFormas is < 0 or > 8) throw new ArgumentOutOfRangeException(nameof(maxFormas));
        bool weapon = target.WeaponId.HasValue || target.Weapon != null;
        var goalKey = goal ?? "";
        if (!Goals(weapon).TryGetValue(goalKey, out var label)) throw new ArgumentException("Неизвестная цель сборки.", nameof(goal));
        var build = new Build
        {
            Id = target.Id, Title = target.Title, Description = target.Description, UserId = target.UserId,
            AuthorName = target.AuthorName, HelminthAbility = target.HelminthAbility, HasReactor = target.HasReactor,
            WarframeId = target.WarframeId, Warframe = target.Warframe, WeaponId = target.WeaponId, Weapon = target.Weapon,
            CreatedAt = target.CreatedAt, UpdatedAt = target.UpdatedAt, FormasCount = 0
        };
        var plan = new AutoBuildPlan { Build = build, GoalLabel = label };
        plan.Notes.Add("Это подбор по заранее заданным приоритетам каталога, без учёта инвентаря, миссии и условных эффектов; он не гарантирует оптимальную сборку.");
        plan.Notes.Add("Исходные полярности приняты нейтральными: каталог не содержит естественные полярности слотов. Реактор/катализатор сохранён из настроек.");
        var priorities = Priorities(weapon, goalKey);
        var byName = catalog.OrderBy(x => x.Id).ThenBy(x => x.UniqueName, StringComparer.Ordinal)
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        var candidates = priorities.Where(byName.ContainsKey).Select(x => byName[x]).ToList();
        var families = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool Fits() { var stats = BuildCalculator.Calculate(build); return stats.UsedCapacity <= stats.Capacity; }
        bool Add(Mod mod, int slot, int rank, string reason)
        {
            if (mod.MaxRank < 0 || families.Contains(ModCompatibility.Family(mod)) || !ModCompatibility.CanEquip(build, mod, slot)) return false;
            var entry = new BuildMod { Build = build, BuildId = build.Id, Mod = mod, ModId = mod.Id, SlotIndex = slot, CurrentRank = rank, SlotPolarity = "None" };
            build.BuildMods.Add(entry);
            if (!Fits()) { build.BuildMods.Remove(entry); return false; }
            families.Add(ModCompatibility.Family(mod));
            plan.Reasons[slot] = $"{label}: {reason}. Семейство: {ModCompatibility.Family(mod)}.";
            return true;
        }
        if (ModCompatibility.ValidSlots(build).Contains(8))
        {
            foreach (var mod in candidates)
                if (Add(mod, 8, mod.MaxRank, weapon ? "совместимая стойка увеличивает вместимость" : "совместимая аура увеличивает вместимость")) break;
        }
        bool CanPolarize(BuildMod entry) => entry.Mod != null && entry.Mod.Polarity != "None" &&
            ModCompatibility.Polarities.Contains(entry.Mod.Polarity, StringComparer.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(entry.Mod.Polarity);
        var special = build.BuildMods.FirstOrDefault(x => x.SlotIndex == 8);
        if (special != null && maxFormas > 0 && CanPolarize(special))
        { special.SlotPolarity = special.Mod!.Polarity; build.FormasCount++; }
        int nextSlot = 0;
        foreach (var mod in candidates)
        {
            if (nextSlot >= 8) break;
            if (mod.IsExilus) continue;
            if (Add(mod, nextSlot, 0, "приоритетный базовый эффект, сначала минимальный ранг")) nextSlot++;
        }
        foreach (var mod in candidates)
            if (mod.IsExilus && Add(mod, 9, 0, "дополнительный совместимый эксилус при доступной вместимости")) break;
        // Spend the remaining budget on the largest prospective ordinary drain savings.
        foreach (var entry in build.BuildMods.Where(x => x.SlotIndex != 8 && CanPolarize(x))
                     .OrderByDescending(x => BuildCalculator.ModCost(x.Mod!, x.Mod!.MaxRank, "None") - BuildCalculator.ModCost(x.Mod!, x.Mod!.MaxRank, x.Mod!.Polarity))
                     .ThenBy(x => x.SlotIndex))
        {
            if (build.FormasCount >= maxFormas) break;
            var mod = entry.Mod!;
            if (BuildCalculator.ModCost(mod, mod.MaxRank, "None") <= BuildCalculator.ModCost(mod, mod.MaxRank, mod.Polarity)) continue;
            entry.SlotPolarity = mod.Polarity;
            build.FormasCount++;
        }
        foreach (var entry in build.BuildMods.Where(x => x.SlotIndex != 8).OrderBy(x => candidates.IndexOf(x.Mod!)).ThenBy(x => x.SlotIndex))
        {
            while (entry.CurrentRank < entry.Mod!.MaxRank)
            {
                entry.CurrentRank++;
                if (!Fits()) { entry.CurrentRank--; break; }
            }
        }
        foreach (var entry in build.BuildMods)
        {
            plan.Reasons[entry.SlotIndex] += $" Ранг {entry.CurrentRank}/{entry.Mod!.MaxRank}; полярность {entry.SlotPolarity}.";
            if (entry.CurrentRank < entry.Mod.MaxRank) plan.Notes.Add($"{entry.Mod.Name}: ранг {entry.CurrentRank}/{entry.Mod.MaxRank} ограничен вместимостью.");
        }
        plan.Notes.Add($"Использовано форм: {build.FormasCount} из разрешённых {maxFormas}; формы меняют полярности и не увеличивают базовую вместимость.");
        if (build.BuildMods.Count == 0) plan.Notes.Add("В каталоге нет совместимых модов из списка приоритетов: предложена пустая сборка.");
        else if (nextSlot < 8) plan.Notes.Add($"Заполнено обычных слотов: {nextSlot}/8. Не хватает совместимых приоритетных модов или вместимости; неизвестные моды не подставлены.");
        if (special == null && ModCompatibility.ValidSlots(build).Contains(8)) plan.Notes.Add("Совместимая аура/стойка из списка приоритетов не найдена.");
        return plan;
    }

    private static List<string> Priorities(bool weapon, string goal)
    {
        string[] focus = weapon ? goal switch
        {
            "critical" => ["Point Strike", "Vital Sense", "Blunderbuss", "Ravage", "Pistol Gambit", "Target Cracker", "True Steel", "Organ Shatter"],
            "status" => ["Malignant Force", "Rime Rounds", "High Voltage", "Thermite Rounds", "Toxic Barrage", "Frigid Blast", "Shell Shock", "Scattering Inferno", "Pistol Pestilence", "Frostbite", "Jolt", "Scorch", "Virulent Scourge", "Vicious Frost", "Voltaic Strike", "Volcanic Edge", "Rifle Aptitude", "Shotgun Savvy", "Sure Shot", "Melee Prowess", "Infected Clip", "Contagious Spread", "Pathogen Rounds", "Fever Strike"],
            _ => ["Serration", "Point Blank", "Hornet Strike", "Pressure Point", "Split Chamber", "Hell's Chamber", "Barrel Diffusion", "Fury", "Point Strike", "Blunderbuss", "Pistol Gambit", "True Steel"]
        } : goal switch
        {
            "survival" => ["Vitality", "Steel Fiber", "Redirection", "Fast Deflection", "Vigor", "Flow"],
            "strength" => ["Intensify", "Transient Fortitude", "Blind Rage", "Augur Secrets", "Power Drift"],
            "duration" => ["Continuity", "Narrow Minded", "Constitution", "Augur Message"],
            "range" => ["Stretch", "Overextended", "Augur Reach", "Cunning Drift"],
            "efficiency" => ["Streamline", "Fleeting Expertise", "Flow"],
            _ => ["Vitality", "Intensify", "Continuity", "Stretch", "Streamline", "Flow", "Steel Fiber", "Redirection"]
        };
        string[] special = weapon ? ["Iron Phoenix", "Clashing Forest", "Flailing Branch", "Crossing Snakes", "Cleaving Whirlwind", "Tempo Royale", "Shimmering Blight", "Swirling Tiger", "Crimson Dervish", "Tranquil Cleave", "Blind Justice", "Decisive Judgement", "Burning Wasp", "Coiling Viper", "Stinging Thorn"] : ["Corrosive Projection", "Steel Charge", "Energy Siphon", "Rejuvenation", "Physique", "Sprint Boost"];
        string[] fallback = weapon
            ? ["Serration", "Point Blank", "Hornet Strike", "Pressure Point", "Split Chamber", "Hell's Chamber", "Barrel Diffusion", "Point Strike", "Vital Sense", "Blunderbuss", "Ravage", "Pistol Gambit", "Target Cracker", "True Steel", "Organ Shatter", "Infected Clip", "Cryo Rounds", "Stormbringer", "Hellfire", "Contagious Spread", "Chilling Grasp", "Charged Shell", "Incendiary Coat", "Pathogen Rounds", "Deep Freeze", "Convulsion", "Heated Charge", "Fever Strike", "North Wind", "Shocking Touch", "Molten Impact", "Speed Trigger", "Shotgun Spazz", "Gunslinger", "Fury", "Fast Hands", "Tactical Pump", "Quickdraw", "Reach", "Rifle Ammo Mutation", "Shotgun Ammo Mutation", "Pistol Ammo Mutation", "Vigilante Supplies", "Terminal Velocity", "Lethal Momentum"]
            : ["Vitality", "Intensify", "Continuity", "Stretch", "Streamline", "Flow", "Steel Fiber", "Redirection", "Fast Deflection", "Rush", "Power Drift", "Cunning Drift", "Speed Drift", "Endurance Drift", "Mobilize"];
        return special.Concat(focus).Concat(fallback).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}
