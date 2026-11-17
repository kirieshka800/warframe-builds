using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarframeBuilds.Data;
using WarframeBuilds.Models;
using WarframeBuilds.Services;
using WarframeBuilds.ViewModels;
namespace WarframeBuilds.Controllers;
[Authorize]
public sealed class AutoBuildController(ApplicationDbContext db,AutoBuildProposalStore store):Controller
{
 private string Owner=>User.FindFirstValue(ClaimTypes.NameIdentifier)!;
 private async Task<AutoBuildFormViewModel> Form(AutoBuildInput input)=>new(){Input=input,Warframes=await db.Warframes.AsNoTracking().OrderBy(x=>x.Name).ToListAsync(),Weapons=await db.Weapons.AsNoTracking().OrderBy(x=>x.Name).ToListAsync()};
 [HttpGet]public async Task<IActionResult> Index(int? warframeId,int? weaponId)=>View(await Form(new(){WarframeId=warframeId,WeaponId=weaponId,TargetType=weaponId.HasValue?"weapon":"warframe"}));
 private async Task<Build?> Target(AutoBuildInput input)
 {
  if(input.TargetType=="warframe" && input.WarframeId is >0)
  {var item=await db.Warframes.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==input.WarframeId);if(item!=null)return new(){WarframeId=item.Id,Warframe=item,HasReactor=input.HasReactor};}
  if(input.TargetType=="weapon" && input.WeaponId is >0)
  {var item=await db.Weapons.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==input.WeaponId);if(item!=null)return new(){WeaponId=item.Id,Weapon=item,HasReactor=input.HasReactor};}
  return null;
 }
 private static string Fingerprint(Build b)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{Frame=b.Warframe,Weapon=b.Weapon,Mods=b.BuildMods.OrderBy(x=>x.SlotIndex).Select(x=>new{x.SlotIndex,x.Mod})}))));
 [HttpPost]public async Task<IActionResult> Preview([Bind(Prefix="Input")]AutoBuildInput input)
 {
  var target=await Target(input);
  if(target==null)ModelState.AddModelError("","Выберите снаряжение из каталога.");
  if(!AutoBuildPlanner.Goals(input.TargetType=="weapon").ContainsKey(input.Goal??""))ModelState.AddModelError("Input.Goal","Выберите подходящую цель сборки.");
  if(!ModelState.IsValid)return View("Index",await Form(input));
  var plan=AutoBuildPlanner.Plan(target!,await db.Mods.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),input.Goal!,input.MaxFormas);
  var title=$"{target!.TargetName} — {plan.GoalLabel}";plan.Build.Title=title.Length>120?title[..120]:title;
  plan.Build.Description=$"Стартовая сборка по правилам. Цель: {plan.GoalLabel}.\n"+string.Join("\n",plan.Notes);
  var token=store.Add(Owner,plan,Fingerprint(plan.Build));return RedirectToAction(nameof(Proposal),new{token});
 }
 private IActionResult Expired(){TempData["Error"]="Предложение устарело или отменено. Подберите билд заново: предпросмотр действует 15 минут и не сохраняется после перезапуска сервера.";return RedirectToAction(nameof(Index));}
 [HttpGet]public IActionResult Proposal(string? token)
 {
  var proposal=store.Get(token);if(proposal==null)return Expired();if(proposal.OwnerId!=Owner)return NotFound();
  if(proposal.Cancelled)return Expired();if(proposal.SavedBuildId is int id)return RedirectToAction("Details","Builds",new{id});
  return View(new AutoBuildPreviewViewModel{Token=token!,Plan=proposal.Plan,Stats=BuildCalculator.Calculate(proposal.Plan.Build),ExpiresAt=proposal.ExpiresAt});
 }
 [HttpPost]public async Task<IActionResult> Confirm(string? token)
 {
  var p=store.Get(token);if(p==null)return Expired();if(p.OwnerId!=Owner)return NotFound();
  await p.Gate.WaitAsync(HttpContext.RequestAborted);
  try
  {
   if(p.ExpiresAt<=DateTimeOffset.UtcNow||p.Cancelled)return Expired();
   if(p.SavedBuildId is int saved)return RedirectToAction("Details","Builds",new{id=saved});
   var proposed=p.Plan.Build;if(!proposed.BuildMods.Any(x=>x.ModId.HasValue))return Expired();
   await using var tx=await db.Database.BeginTransactionAsync(HttpContext.RequestAborted);
   // Coordinate with catalogue sync: the saved configuration must match the preview.
   await db.Database.ExecuteSqlRawAsync("LOCK TABLE \"Warframes\", \"Mods\", \"Weapons\" IN SHARE MODE",HttpContext.RequestAborted);
   var current=await Target(new(){TargetType=proposed.IsWeapon?"weapon":"warframe",WarframeId=proposed.WarframeId,WeaponId=proposed.WeaponId,HasReactor=proposed.HasReactor});
   var ids=proposed.BuildMods.Where(x=>x.ModId.HasValue).Select(x=>x.ModId!.Value).ToArray();
   var mods=await db.Mods.AsNoTracking().Where(x=>ids.Contains(x.Id)).ToDictionaryAsync(x=>x.Id);
   if(current==null)return Changed();
   foreach(var slot in proposed.BuildMods)
   {
    if(!slot.ModId.HasValue||!mods.TryGetValue(slot.ModId.Value,out var mod))return Changed();
    if(slot.CurrentRank<0||slot.CurrentRank>mod.MaxRank||!ModCompatibility.CanEquip(current,mod,slot.SlotIndex))return Changed();
    current.BuildMods.Add(new(){SlotIndex=slot.SlotIndex,Mod=mod,ModId=mod.Id,CurrentRank=slot.CurrentRank,SlotPolarity=slot.SlotPolarity});
   }
   var stats=BuildCalculator.Calculate(current);
   if(Fingerprint(current)!=p.Fingerprint||stats.UsedCapacity>stats.Capacity)return Changed();
   var build=new Build{Title=proposed.Title,Description=proposed.Description,WarframeId=proposed.WarframeId,WeaponId=proposed.WeaponId,HasReactor=proposed.HasReactor,FormasCount=proposed.FormasCount,UserId=Owner,AuthorName=User.Identity?.Name??"Tenno",BuildMods=current.BuildMods.Select(x=>new BuildMod{SlotIndex=x.SlotIndex,ModId=x.ModId,CurrentRank=x.CurrentRank,SlotPolarity=x.SlotPolarity}).ToList()};
   db.Builds.Add(build);await db.SaveChangesAsync(HttpContext.RequestAborted);await tx.CommitAsync(HttpContext.RequestAborted);p.SavedBuildId=build.Id;
   TempData["Success"]="Предложенная сборка сохранена. Теперь её можно редактировать вручную.";return RedirectToAction("Details","Builds",new{id=build.Id});
  }
  finally{p.Gate.Release();}
 }
 private IActionResult Changed(){TempData["Error"]="Каталог изменился после подбора. Ничего не сохранено — подготовьте предложение заново.";return RedirectToAction(nameof(Index));}
 [HttpPost]public async Task<IActionResult> Cancel(string? token)
 {
  var p=store.Get(token);if(p==null)return Expired();if(p.OwnerId!=Owner)return NotFound();await p.Gate.WaitAsync();
  try{if(p.SavedBuildId is int id)return RedirectToAction("Details","Builds",new{id});p.Cancelled=true;TempData["Success"]="Предложение отменено. Билд не создавался.";return RedirectToAction(nameof(Index));}finally{p.Gate.Release();}
 }
}
