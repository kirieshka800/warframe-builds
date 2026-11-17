using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarframeBuilds.Data;
using WarframeBuilds.Models;
using WarframeBuilds.Services;
using WarframeBuilds.ViewModels;
namespace WarframeBuilds.Controllers;
public partial class BuildsController(ApplicationDbContext db) : Controller
{
 private string OwnerId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
 private bool Owns(Build b) => OwnerId.Length>0 && b.UserId==OwnerId;
 private IQueryable<Build> FullBuilds => db.Builds.Include(b=>b.Warframe).Include(b=>b.Weapon).Include(b=>b.BuildMods).ThenInclude(s=>s.Mod);
 private async Task<CreateBuildViewModel> Form(CreateBuildInput input) => new(){Input=input,Warframes=await db.Warframes.OrderBy(x=>x.Name).ToListAsync(),Weapons=await db.Weapons.OrderBy(x=>x.Name).ToListAsync()};
 [HttpGet,Authorize] public async Task<IActionResult> Create(int? warframeId,int? weaponId) => View(await Form(new(){Title="",WarframeId=warframeId,WeaponId=weaponId,TargetType=weaponId.HasValue?"weapon":"warframe"}));
 private async Task ValidateTarget(CreateBuildInput input)
 {
  if(input.TargetType=="warframe")
  { if(!input.WarframeId.HasValue || !await db.Warframes.AnyAsync(x=>x.Id==input.WarframeId))ModelState.AddModelError("Input.WarframeId","Выберите варфрейма из каталога"); }
  else if(input.TargetType=="weapon")
  { if(!input.WeaponId.HasValue || !await db.Weapons.AnyAsync(x=>x.Id==input.WeaponId))ModelState.AddModelError("Input.WeaponId","Выберите оружие из каталога"); }
  else ModelState.AddModelError("Input.TargetType","Неизвестный тип снаряжения");
 }
 [HttpPost,Authorize] public async Task<IActionResult> Create([Bind(Prefix="Input")] CreateBuildInput input)
 {
  await ValidateTarget(input);
  if(!ModelState.IsValid)return View(await Form(input));
  var b=new Build{Title=input.Title.Trim(),Description=input.Description?.Trim(),WarframeId=input.TargetType=="warframe"?input.WarframeId:null,WeaponId=input.TargetType=="weapon"?input.WeaponId:null,HasReactor=input.HasReactor,FormasCount=input.FormasCount,HelminthAbility=input.TargetType=="warframe"?input.HelminthAbility?.Trim():null,UserId=OwnerId,AuthorName=User.Identity?.Name??"Tenno"};
  db.Builds.Add(b);await db.SaveChangesAsync();TempData["Success"]="Билд создан. Установите моды в слоты — изменения сохраняются автоматически.";return RedirectToAction(nameof(Details),new{id=b.Id});
 }
 [HttpGet] public async Task<IActionResult> Index(string? search,string? kind,bool mine=false,int page=1)
 {
  if(mine && !User.Identity!.IsAuthenticated)return Challenge();
  search=search?.Trim();if(search?.Length>200)search=search[..200];
  var q=FullBuilds.AsNoTracking();
  if(mine)q=q.Where(b=>b.UserId==OwnerId);
  if(kind=="warframe")q=q.Where(b=>b.WarframeId!=null);
  if(kind=="weapon")q=q.Where(b=>b.WeaponId!=null);
  if(!string.IsNullOrEmpty(search)){var text=search.ToLower();q=q.Where(b=>b.Title.ToLower().Contains(text)||b.AuthorName.ToLower().Contains(text)||(b.Warframe!=null&&b.Warframe.Name.ToLower().Contains(text))||(b.Weapon!=null&&b.Weapon.Name.ToLower().Contains(text)));}
  var total=await q.CountAsync();page=Math.Clamp(page,1,Math.Max(1,(total+11)/12));
  return View(new BuildCatalogViewModel{Builds=await q.OrderByDescending(x=>x.UpdatedAt).ThenByDescending(x=>x.Id).Skip((page-1)*12).Take(12).ToListAsync(),Search=search,Kind=kind,Mine=mine,Page=page,Total=total});
 }
 [HttpGet] public async Task<IActionResult> Details(int id)
 {
  var b=await FullBuilds.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==id);if(b==null)return NotFound();
  return View(new BuildEditorViewModel{Build=b,Stats=BuildCalculator.Calculate(b),IsOwner=Owns(b),Mods=await db.Mods.AsNoTracking().OrderBy(x=>x.Name).ToListAsync(),Comments=await db.BuildComments.AsNoTracking().Where(x=>x.BuildId==id).OrderByDescending(x=>x.CreatedAt).Take(100).ToListAsync()});
 }
 [HttpGet] public IActionResult ViewBuild(int id)=>RedirectToActionPermanent(nameof(Details),new{id});
 [HttpGet,Authorize] public async Task<IActionResult> Edit(int id)
 {
  var b=await db.Builds.FindAsync(id);if(b==null)return NotFound();if(!Owns(b))return Forbid();ViewBag.BuildId=id;
  return View(await Form(new(){Title=b.Title,Description=b.Description,TargetType=b.IsWeapon?"weapon":"warframe",WarframeId=b.WarframeId,WeaponId=b.WeaponId,HasReactor=b.HasReactor,FormasCount=b.FormasCount,HelminthAbility=b.HelminthAbility}));
 }
 [HttpPost,Authorize] public async Task<IActionResult> Edit(int id,[Bind(Prefix="Input")]CreateBuildInput input)
 {
  var b=await FullBuilds.FirstOrDefaultAsync(x=>x.Id==id);if(b==null)return NotFound();if(!Owns(b))return Forbid();
  await ValidateTarget(input);
  if(input.TargetType!=(b.IsWeapon?"weapon":"warframe") || (b.IsWeapon?input.WeaponId!=b.WeaponId:input.WarframeId!=b.WarframeId))ModelState.AddModelError("","Снаряжение существующего билда менять нельзя. Создайте отдельную сборку.");
  var oldReactor=b.HasReactor;b.HasReactor=input.HasReactor;var stats=BuildCalculator.Calculate(b);b.HasReactor=oldReactor;
  if(stats.UsedCapacity>stats.Capacity)ModelState.AddModelError("Input.HasReactor","Сначала уменьшите стоимость установленных модов.");
  if(!ModelState.IsValid){ViewBag.BuildId=id;return View(await Form(input));}
  b.Title=input.Title.Trim();b.Description=input.Description?.Trim();b.FormasCount=input.FormasCount;b.HasReactor=input.HasReactor;b.HelminthAbility=b.IsWeapon?null:input.HelminthAbility?.Trim();b.UpdatedAt=DateTime.UtcNow;
  if(await Save())TempData["Success"]="Параметры билда сохранены.";return RedirectToAction(nameof(Details),new{id});
 }
 private async Task<bool> Save()
 {
  try{await db.SaveChangesAsync();return true;}
  catch(DbUpdateConcurrencyException){TempData["Error"]="Билд изменился в другой вкладке. Страница обновлена; повторите действие.";return false;}
  catch(DbUpdateException e) when(e.InnerException is Npgsql.PostgresException p && p.SqlState=="23505"){TempData["Error"]="Этот слот уже изменился в другой вкладке. Проверьте конфигурацию и повторите.";return false;}
 }
 private IActionResult EditorError(int id,string message){TempData["Error"]=message;return RedirectToAction(nameof(Details),new{id});}
 [HttpPost,Authorize] public async Task<IActionResult> AddMod(int buildId,int modId,int slotIndex,int currentRank)
 {
  var b=await FullBuilds.FirstOrDefaultAsync(x=>x.Id==buildId);if(b==null)return NotFound();if(!Owns(b))return Forbid();
  if(!ModCompatibility.ValidSlots(b).Contains(slotIndex))return BadRequest("Недопустимый слот");
  var slot=b.BuildMods.SingleOrDefault(x=>x.SlotIndex==slotIndex);
  if(modId==0)
  {
   var before=BuildCalculator.Calculate(b);
   if(slot!=null){slot.Mod=null;slot.ModId=null;slot.CurrentRank=0;}
   var after=BuildCalculator.Calculate(b);
   if(after.UsedCapacity>after.Capacity && after.UsedCapacity-after.Capacity>=Math.Max(0,before.UsedCapacity-before.Capacity))return EditorError(buildId,"Без этой ауры/стойки не хватает вместимости. Сначала уменьшите стоимость остальных модов.");
   b.UpdatedAt=DateTime.UtcNow;await Save();return RedirectToAction(nameof(Details),new{id=buildId});
  }
  var mod=await db.Mods.FindAsync(modId);if(mod==null)return NotFound();
  if(currentRank<0 || currentRank>mod.MaxRank)return EditorError(buildId,"Ранг мода выходит за допустимые границы.");
  if(!ModCompatibility.CanEquip(b,mod,slotIndex))return EditorError(buildId,"Этот мод несовместим со снаряжением или типом слота.");
  if(b.BuildMods.Any(x=>x.SlotIndex!=slotIndex&&x.Mod!=null&&ModCompatibility.Family(x.Mod)==ModCompatibility.Family(mod)))return EditorError(buildId,"Этот мод или его альтернативная версия уже установлены.");
  var previous=BuildCalculator.Calculate(b);var oldMod=slot?.Mod;var oldId=slot?.ModId;var oldRank=slot?.CurrentRank??0;
  if(slot==null){slot=new(){BuildId=b.Id,SlotIndex=slotIndex,SlotPolarity="None"};b.BuildMods.Add(slot);}
  slot.Mod=mod;slot.ModId=mod.Id;slot.CurrentRank=currentRank;
  var next=BuildCalculator.Calculate(b);
  if(next.UsedCapacity>next.Capacity && next.UsedCapacity-next.Capacity>=Math.Max(0,previous.UsedCapacity-previous.Capacity))return EditorError(buildId,$"Не хватает вместимости: {next.UsedCapacity} / {next.Capacity}. Уменьшите ранг, настройте полярность или установите реактор/катализатор.");
  b.UpdatedAt=DateTime.UtcNow;await Save();return RedirectToAction(nameof(Details),new{id=buildId});
 }
 [HttpPost,Authorize] public async Task<IActionResult> SetSlotPolarity(int buildId,int slotIndex,string polarity)
 {
  var b=await FullBuilds.FirstOrDefaultAsync(x=>x.Id==buildId);if(b==null)return NotFound();if(!Owns(b))return Forbid();
  if(!ModCompatibility.ValidSlots(b).Contains(slotIndex)||!ModCompatibility.Polarities.Contains(polarity))return BadRequest("Недопустимая полярность или слот");
  if(polarity=="Universal" && slotIndex!=8)return EditorError(buildId,"Универсальная полярность доступна только для ауры/стойки.");
  var previous=BuildCalculator.Calculate(b);var slot=b.BuildMods.SingleOrDefault(x=>x.SlotIndex==slotIndex);
  if(slot==null){slot=new(){BuildId=buildId,SlotIndex=slotIndex};b.BuildMods.Add(slot);}slot.SlotPolarity=polarity;
  var next=BuildCalculator.Calculate(b);if(next.UsedCapacity>next.Capacity&&next.UsedCapacity-next.Capacity>=Math.Max(0,previous.UsedCapacity-previous.Capacity))return EditorError(buildId,"Изменение полярности превышает вместимость. Сначала освободите место.");
  b.UpdatedAt=DateTime.UtcNow;await Save();return RedirectToAction(nameof(Details),new{id=buildId});
 }
 [HttpPost,Authorize]public async Task<IActionResult> Delete(int id)
 {
  var b=await db.Builds.FindAsync(id);if(b==null)return NotFound();if(!Owns(b))return Forbid();db.Builds.Remove(b);await Save();TempData["Success"]="Билд удалён.";return RedirectToAction(nameof(Index),new{mine=true});
 }
 [HttpPost,Authorize]public async Task<IActionResult> Duplicate(int id)
 {
  var source=await FullBuilds.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==id);if(source==null)return NotFound();
  var copy=new Build{Title=(source.Title.Length>110?source.Title[..110]:source.Title)+" — копия",Description=source.Description,WarframeId=source.WarframeId,WeaponId=source.WeaponId,HasReactor=source.HasReactor,FormasCount=source.FormasCount,HelminthAbility=source.HelminthAbility,UserId=OwnerId,AuthorName=User.Identity?.Name??"Tenno",BuildMods=source.BuildMods.Select(x=>new BuildMod{SlotIndex=x.SlotIndex,ModId=x.ModId,CurrentRank=x.CurrentRank,SlotPolarity=x.SlotPolarity}).ToList()};
  db.Builds.Add(copy);await db.SaveChangesAsync();TempData["Success"]="Копия сохранена в ваших билдах.";return RedirectToAction(nameof(Details),new{id=copy.Id});
 }
 [HttpPost,Authorize]public async Task<IActionResult> AddComment(int buildId,string? text)
 {
  if(!await db.Builds.AnyAsync(x=>x.Id==buildId))return NotFound();
  text=text?.Trim();if(string.IsNullOrEmpty(text)||text.Length>2000)return EditorError(buildId,"Комментарий должен содержать от 1 до 2000 символов.");
  db.BuildComments.Add(new(){BuildId=buildId,AuthorName=User.Identity?.Name??"Tenno",Text=text,CreatedAt=DateTime.UtcNow});await db.SaveChangesAsync();return RedirectToAction(nameof(Details),new{id=buildId});
 }
}
