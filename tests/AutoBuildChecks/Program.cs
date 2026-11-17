using WarframeBuilds.Models;
using WarframeBuilds.Services;
using System.Text.Json;

int count = 0;
void Check(bool value, string message) { if (!value) throw new Exception(message); count++; Console.WriteLine($"PASS {message}"); }
Mod M(int id, string name, string compatibility = "Warframe", int drain = 6, int rank = 5, bool exilus = false, string type = "") => new()
{ Id = id, Name = name, Compatibility = compatibility, BaseDrain = drain, MaxRank = rank, IsExilus = exilus, Type = type, Polarity = "Madurai", LevelStatsJson = JsonSerializer.Serialize(Enumerable.Range(0, rank + 1).Select(x => new { stats = new[] { $"+{x + 1}% Damage" } })) };
Build Frame(bool reactor = true) => new() { Id = 4, Title = "Original", WarframeId = 1, Warframe = new Warframe { Name = "Rhino", BaseArmor = 240 }, HasReactor = reactor, FormasCount = 7, BuildMods = [new BuildMod { SlotIndex = 2, Mod = M(999, "Original"), SlotPolarity = "Umbra", CurrentRank = 2 }] };
string Signature(AutoBuildPlan plan) => string.Join("|", plan.Build.BuildMods.Select(x => $"{x.SlotIndex}:{x.ModId}:{x.CurrentRank}:{x.SlotPolarity}"));
void Valid(AutoBuildPlan plan, int budget)
{
    var b = plan.Build; var s = BuildCalculator.Calculate(b);
    Check(s.UsedCapacity <= s.Capacity, "capacity never overflows");
    Check(b.BuildMods.All(x => x.Mod != null && x.CurrentRank >= 0 && x.CurrentRank <= x.Mod.MaxRank && ModCompatibility.CanEquip(b, x.Mod, x.SlotIndex)), "ranks and compatibility valid");
    Check(b.FormasCount <= budget && b.FormasCount == b.BuildMods.Count(x => x.SlotPolarity != "None"), "forma budget and actual count");
    Check(b.BuildMods.Select(x => ModCompatibility.Family(x.Mod!)).Distinct().Count() == b.BuildMods.Count, "no duplicate families");
    Check(b.BuildMods.Select(x => x.SlotIndex).Distinct().Count() == b.BuildMods.Count && b.BuildMods.All(x => plan.Reasons.ContainsKey(x.SlotIndex)), "unique slots and explanation per mod");
}
var catalog = new[] { M(1,"Vitality"),M(2,"Steel Fiber"),M(3,"Redirection"),M(4,"Intensify"),M(5,"Transient Fortitude"),M(6,"Blind Rage"),M(7,"Continuity"),M(8,"Stretch"),M(9,"Streamline"),M(10,"Flow"),M(11,"Power Drift",exilus:true), M(12,"Corrosive Projection",drain:-2,type:"Aura"),M(13,"Primed Continuity"), M(14,"Narrow Minded"), M(15,"Overextended"), M(16,"Fleeting Expertise") };
Check(AutoBuildPlanner.Goals(false).Count == 6 && AutoBuildPlanner.Goals(true).Count == 3, "goal sets");
var target = Frame(false);
var survival = AutoBuildPlanner.Plan(target, catalog, "survival");
var strength = AutoBuildPlanner.Plan(target, catalog, "strength");
Check(survival.Build.BuildMods.First(x => x.SlotIndex == 0).Mod!.Name == "Vitality", "survival prioritizes vitality");
Check(strength.Build.BuildMods.First(x => x.SlotIndex == 0).Mod!.Name == "Intensify", "strength prioritizes intensify");
Check(Signature(survival) != Signature(strength), "frame goals diverge");
Check(target.FormasCount == 7 && target.BuildMods.Count == 1 && target.BuildMods[0].SlotPolarity == "Umbra" && target.BuildMods[0].CurrentRank == 2, "input remains unchanged");
Check(!ReferenceEquals(target.BuildMods, survival.Build.BuildMods), "new build mod collection");
Check(!survival.Build.HasReactor && BuildCalculator.Calculate(survival.Build).Capacity == 37, "reactor choice and aura max rank");
Check(survival.Build.BuildMods.All(x => x.SlotPolarity == "None"), "zero forma neutral slots");
Check(survival.Notes.Any(x => x.Contains("ограничен вместимостью")), "partial ranks explained");
Check(Signature(survival) == Signature(AutoBuildPlanner.Plan(target, catalog.Reverse().ToArray(), "survival")), "deterministic across catalogue order");
Check(!survival.Build.BuildMods.Any(x => x.Mod!.Name.StartsWith("Primed")), "ordinary variants preferred");
Valid(survival, 0);
var formed = AutoBuildPlanner.Plan(Frame(), catalog, "strength", 3);
Check(formed.Build.BuildMods.First(x => x.SlotIndex == 8).SlotPolarity == "Madurai", "special polarity matched first");
Check(formed.Build.FormasCount == 3, "requested forma budget can be used");
Check(formed.Build.BuildMods.Any(x => x.SlotIndex == 9 && x.Mod!.IsExilus), "optional exilus has correct slot");
Valid(formed, 3);
foreach (var goal in AutoBuildPlanner.Goals(false).Keys) Valid(AutoBuildPlanner.Plan(Frame(), catalog, goal, 8), 8);
var empty = AutoBuildPlanner.Plan(Frame(), Array.Empty<Mod>(), "balanced");
Check(empty.Build.BuildMods.Count == 0 && empty.Notes.Any(x => x.Contains("пустая")), "empty catalogue explicit");
var sparse = AutoBuildPlanner.Plan(Frame(), [M(44,"Unknown Super Mod"),M(45,"Serration","Rifle")], "balanced");
Check(sparse.Build.BuildMods.Count == 0, "unknown and incompatible mods not invented");
var one = AutoBuildPlanner.Plan(Frame(), [M(46,"Continuity"),M(47,"Primed Continuity")], "duration");
Check(one.Build.BuildMods.Count == 1 && one.Build.BuildMods[0].Mod!.Name == "Continuity", "single base family selected");
var gunMods = new[] { M(100,"Serration","Rifle"),M(101,"Point Strike","Rifle"),M(102,"Vital Sense","Rifle"),M(103,"Malignant Force","Rifle"),M(104,"Rime Rounds","Rifle"),M(105,"Infected Clip","Rifle"),M(106,"Split Chamber","Rifle"),M(107,"Vigilante Supplies","Rifle",exilus:true),M(108,"Hornet Strike","Pistol"),M(109,"Pistol Gambit","Pistol"),M(110,"Point Blank","Shotgun"),M(111,"Blunderbuss","Shotgun"),M(112,"Pressure Point","Melee"),M(113,"True Steel","Melee"),M(114,"Iron Phoenix","Swords",drain:-2,type:"Stance"),M(115,"Clashing Forest","Staves",drain:-2,type:"Stance") };
Build Gun(string category,string type) => new() { WeaponId=1, Weapon=new Weapon {Name="Test",Category=category,WeaponType=type,BaseDamage=10}, HasReactor=true };
var rifle=Gun("Primary","Rifle");
var crit=AutoBuildPlanner.Plan(rifle,gunMods,"critical");var status=AutoBuildPlanner.Plan(rifle,gunMods,"status");
Check(crit.Build.BuildMods.First(x=>x.SlotIndex==0).Mod!.Name=="Point Strike" && status.Build.BuildMods.First(x=>x.SlotIndex==0).Mod!.Name=="Malignant Force", "weapon goals diverge");
Check(!crit.Build.BuildMods.Any(x=>x.SlotIndex==8), "ranged has no special slot");
Valid(crit,0);Valid(status,0);
foreach(var pair in new[]{("Secondary","Pistol","Hornet Strike"),("Primary","Shotgun","Point Blank"),("Melee","Sword","Pressure Point")})
{
 var p=AutoBuildPlanner.Plan(Gun(pair.Item1,pair.Item2),gunMods,"balanced",2);
 Check(p.Build.BuildMods.Any(x=>x.Mod!.Name==pair.Item3), $"category {pair.Item2} supported");Valid(p,2);
 if(pair.Item1=="Melee") Check(p.Build.BuildMods.Single(x=>x.SlotIndex==8).Mod!.Name=="Iron Phoenix", "compatible stance subtype selected");
}
Check(AutoBuildPlanner.Plan(Gun("Melee","Staff"),gunMods,"balanced").Build.BuildMods.Single(x=>x.SlotIndex==8).Mod!.Name=="Clashing Forest", "staff plural stance restriction");
foreach(int invalid in new[]{-1,9}) { bool threw=false;try{AutoBuildPlanner.Plan(Frame(),catalog,"balanced",invalid);}catch(ArgumentOutOfRangeException){threw=true;}Check(threw,"invalid forma budget rejected"); }
bool badGoal=false;try{AutoBuildPlanner.Plan(Frame(),catalog,"critical");}catch(ArgumentException){badGoal=true;}Check(badGoal,"wrong target goal rejected");


using (var store = new AutoBuildProposalStore())
{
 string first = store.Add("owner", new AutoBuildPlan(), "fingerprint");
 string last = first;
 for(int i=0;i<256;i++) last=store.Add("owner", new AutoBuildPlan(), "fingerprint");
 Check(store.Get(last)!=null, "new preview admitted at bounded-cache capacity");
 Check(store.Get(first)==null, "oldest preview evicted at bounded-cache capacity");
 Check(store.Get("invalid")==null, "invalid token does not resolve");
}

var bo = new Build { WeaponId=1, Weapon=new Weapon{Name="Bo",Category="Melee",WeaponType="Staff"} };
var flailing = M(777,"Flailing Branch","Staves",drain:-2,rank:5,type:"Stance");
Check(AutoBuildPlanner.Plan(bo,new[]{flailing},"balanced").Build.BuildMods.Any(x=>x.SlotIndex==8&&x.Mod==flailing),"starter Bo stance included");
Console.WriteLine($"All {count} auto-build checks passed.");
