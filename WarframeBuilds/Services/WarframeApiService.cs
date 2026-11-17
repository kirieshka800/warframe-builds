using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WarframeBuilds.Data;
using WarframeBuilds.Models;

namespace WarframeBuilds.Services;

public class WarframeApiService(HttpClient httpClient, ApplicationDbContext context)
{
    private const int MaximumBytes = 48 * 1024 * 1024;
    private static readonly HashSet<string> GearFiles = new(StringComparer.Ordinal)
    { "Sentinels.json", "Archwing.json", "Arch-Gun.json", "Arch-Melee.json", "Pets.json", "SentinelWeapons.json", "Arcanes.json", "OperatorAmps.json", "Necramechs.json" };

    // Bound both the entire request duration and response size, including chunked responses.
    private async Task<List<T>> FetchAsync<T>(string file, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(90));
        using var response = await httpClient.GetAsync("https://raw.githubusercontent.com/WFCD/warframe-items/master/data/json/" + file,
            HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > MaximumBytes) throw new InvalidDataException("Catalogue response is too large.");
        await using var input = await response.Content.ReadAsStreamAsync(timeout.Token);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int count;
        while ((count = await input.ReadAsync(chunk, timeout.Token)) > 0)
        {
            if (buffer.Length + count > MaximumBytes) throw new InvalidDataException("Catalogue response is too large.");
            await buffer.WriteAsync(chunk.AsMemory(0, count), timeout.Token);
        }
        using var document = JsonDocument.Parse(buffer.ToArray());
        if (document.RootElement.ValueKind != JsonValueKind.Array) throw new InvalidDataException("Catalogue must contain an array.");
        var items = new List<T>();
        foreach (var element in document.RootElement.EnumerateArray())
        {
            try { var item = element.Deserialize<T>(); if (item != null) items.Add(item); }
            catch (JsonException) { /* One malformed record must not invalidate the remaining catalogue. */ }
        }
        return items;
    }
    private static Dictionary<string, T> Index<T>(IEnumerable<T> rows, Func<T, string> key) =>
        rows.Where(x => !string.IsNullOrWhiteSpace(key(x))).GroupBy(key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
    private static string Polarity(string? value) => string.IsNullOrWhiteSpace(value) ? "None" :
        char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();

    public async Task SyncModsAsync(CancellationToken cancellationToken = default)
    {
        var items = await FetchAsync<ModItemDto>("Mods.json", cancellationToken);
        var rows = await context.Mods.ToListAsync(cancellationToken);
        var names = Index(rows, x => x.Name); var unique = Index(rows, x => x.UniqueName);
        foreach (var item in items.Where(x => !x.UniqueName.Contains("/Beginner/") && !x.UniqueName.Contains("/Intermediate/") && !x.UniqueName.Contains("/Expert/") && !x.UniqueName.Contains("/PvP/"))
                     .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Select(g => g.OrderByDescending(x => x.FusionLimit).First()))
        {
            if (string.IsNullOrWhiteSpace(item.Name) || item.FusionLimit is < 0 or > 20 || item.BaseDrain is < -100 or > 100) continue;
            if (!unique.TryGetValue(item.UniqueName, out var row) && !names.TryGetValue(item.Name, out row))
            { row = new Mod(); context.Mods.Add(row); }
            row.Name = item.Name; row.UniqueName = item.UniqueName; row.Description = item.Description ?? "";
            row.Compatibility = item.Compatibility.Equals("AURA", StringComparison.OrdinalIgnoreCase) ? "Warframe" : item.Compatibility;
            row.Type = item.Compatibility.Equals("AURA", StringComparison.OrdinalIgnoreCase) ? "Aura" : item.Type;
            row.IsExilus = item.IsExilus;
            row.BaseDrain = item.BaseDrain; row.MaxRank = item.FusionLimit; row.Polarity = Polarity(item.Polarity);
            row.Rarity = item.Rarity ?? "Common"; row.ImageName = item.ImageName;
            row.LevelStatsJson = item.LevelStats.ValueKind == JsonValueKind.Array ? item.LevelStats.GetRawText() : "[]";
            names[row.Name] = row; if (row.UniqueName.Length > 0) unique[row.UniqueName] = row;
        }
        await context.SaveChangesAsync(cancellationToken);
    }
    public async Task SyncWeaponsAsync(CancellationToken cancellationToken = default)
    {
        // Fetch before modifying tracked entities so a failed category leaves this sync untouched.
        var all = new List<WeaponItemDto>();
        foreach (var category in new[] { "Primary", "Secondary", "Melee" })
        {
            var items = await FetchAsync<WeaponItemDto>(category + ".json", cancellationToken);
            foreach (var item in items) item.Category = category;
            all.AddRange(items);
        }
        var rows = await context.Weapons.ToListAsync(cancellationToken);
        var names = Index(rows, x => x.Name); var unique = Index(rows, x => x.UniqueName);
        foreach (var item in all)
        {
            if (string.IsNullOrWhiteSpace(item.Name) || item.BaseDamage <= 0 || !double.IsFinite(item.BaseDamage)) continue;
            if (!unique.TryGetValue(item.UniqueName, out var row) && !names.TryGetValue(item.Name, out row))
            { row = new Weapon(); context.Weapons.Add(row); }
            row.Name = item.Name; row.UniqueName = item.UniqueName; row.Category = item.Category;
            // WFCD currently labels every melee weapon simply "Melee". Preserve known subtype
            // metadata and explicitly map starter families; do not guess from arbitrary asset paths.
            row.WeaponType = !string.IsNullOrWhiteSpace(item.WeaponClass) ? item.WeaponClass : item.Category == "Melee" && item.WeaponType == "Melee"
                ? item.Name switch
                {
                    "Skana" or "Skana Prime" or "Prisma Skana" or "MK1-Skana" => "Sword",
                    "Bo" or "Bo Prime" or "MK1-Bo" => "Staff",
                    "Dual Zoren" => "Dual Swords",
                    _ => string.IsNullOrWhiteSpace(row.WeaponType) ? "Melee" : row.WeaponType
                }
                : item.WeaponType;
            row.Description = item.Description ?? "";
            row.Polarity = Polarity(item.Polarity); row.Rarity = item.Rarity; row.ImageName = item.ImageName;
            row.BaseDamage = item.BaseDamage; row.CriticalChance = item.CriticalChance;
            row.CriticalMultiplier = item.CriticalMultiplier; row.StatusChance = item.StatusChance;
            row.FireRate = item.FireRate; row.MagazineSize = item.MagazineSize; row.ReloadTime = item.ReloadTime;
            names[row.Name] = row; if (row.UniqueName.Length > 0) unique[row.UniqueName] = row;
        }
        await context.SaveChangesAsync(cancellationToken);
    }
    public async Task SyncWarframesAsync(CancellationToken cancellationToken = default)
    {
        var items = await FetchAsync<WarframeItemDto>("Warframes.json", cancellationToken);
        var names = Index(await context.Warframes.ToListAsync(cancellationToken), x => x.Name);
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Name) || item.Type != "Warframe" || item.Health is null or <= 0) continue;
            if (!names.TryGetValue(item.Name, out var row)) { row = new Warframe(); context.Warframes.Add(row); }
            row.Name = item.Name; row.Description = item.Description ?? "";
            row.BaseHealth = item.Health.Value; row.BaseShield = item.Shield ?? 0;
            row.BaseArmor = item.Armor ?? 0; row.BaseEnergy = item.Power ?? 0;
            row.ImageName = item.ImageName;
            row.ImageUrl = item.ImageName == null ? null : "https://cdn.warframestat.us/img/" + Uri.EscapeDataString(item.ImageName);
            names[row.Name] = row;
        }
        await context.SaveChangesAsync(cancellationToken);
    }
    public async Task SyncGearCategoryAsync(string categoryName, string jsonFileName, CancellationToken cancellationToken = default)
    {
        if (!GearFiles.Contains(jsonFileName) || string.IsNullOrWhiteSpace(categoryName) || categoryName.Length > 60)
            throw new ArgumentException("Unknown gear catalogue category or file.");
        var items = await FetchAsync<WeaponItemDto>(jsonFileName, cancellationToken);
        var names = Index(await context.GearItems.Where(x => x.Category == categoryName).ToListAsync(cancellationToken), x => x.Name);
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Name)) continue;
            if (!names.TryGetValue(item.Name, out var row)) { row = new GearItem(); context.GearItems.Add(row); }
            row.Name = item.Name; row.Category = categoryName; row.Description = item.Description ?? "";
            row.Polarity = Polarity(item.Polarity); row.Rarity = item.Rarity;
            names[row.Name] = row;
        }
        await context.SaveChangesAsync(cancellationToken);
    }
}
