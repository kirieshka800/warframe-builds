using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarframeBuilds.Models;
using WarframeBuilds.Services;
namespace WarframeBuilds.Controllers;
public sealed class BuildFile
{
 public int Version {get;set;}=1;
 public string Title {get;set;}="";
 public string? Description {get;set;}
 public string TargetType {get;set;}="warframe";
 public string TargetName {get;set;}="";
 public bool HasReactor {get;set;}=true;
 public int FormasCount {get;set;}
 public string? HelminthAbility {get;set;}
 public List<BuildSlotFile>? Slots {get;set;}=new();
}
public sealed class BuildSlotFile
{
 public int SlotIndex {get;set;}
 public string? ModName {get;set;}
 public int Rank {get;set;}
 public string Polarity {get;set;}="None";
}
public partial class BuildsController
{
 private static readonly JsonSerializerOptions FileOptions=new(JsonSerializerDefaults.Web){WriteIndented=true,MaxDepth=16};
 [HttpGet] public async Task<IActionResult> Export(int id)
 {
  var b=await FullBuilds.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==id);if(b==null)return NotFound();
  var file=new BuildFile{Title=b.Title,Description=b.Description,TargetType=b.IsWeapon?"weapon":"warframe",TargetName=b.TargetName,HasReactor=b.HasReactor,FormasCount=b.FormasCount,HelminthAbility=b.HelminthAbility,Slots=b.BuildMods.OrderBy(x=>x.SlotIndex).Select(x=>new BuildSlotFile{SlotIndex=x.SlotIndex,ModName=x.Mod?.Name,Rank=x.CurrentRank,Polarity=x.SlotPolarity}).ToList()};
  return File(JsonSerializer.SerializeToUtf8Bytes(file,FileOptions),"application/json",$"warframe-build-{id}.json");
 }
 [HttpPost,Authorize,RequestSizeLimit(300000)]public async Task<IActionResult> Import(IFormFile? file)
 {
  IActionResult Fail(string text){TempData["Error"]=text;return RedirectToAction(nameof(Index),new{mine=true});}
  if(file==null||file.Length==0||file.Length>256000)return Fail("Выберите JSON-файл билда размером до 250 КБ.");
  BuildFile? data;
  try{await using var stream=file.OpenReadStream();data=await JsonSerializer.DeserializeAsync<BuildFile>(stream,FileOptions);}
  catch(JsonException){return Fail("Файл не является корректным JSON-билдом.");}
  if(data==null||data.Version!=1||string.IsNullOrWhiteSpace(data.Title)||data.Title.Length>120||data.TargetName==null||data.TargetName.Length>150||data.Description?.Length>12000||data.HelminthAbility?.Length>120||data.FormasCount<0||data.FormasCount>100||data.Slots==null||data.Slots.Count>10||data.Slots.Any(s=>s==null||s.ModName?.Length>150))return Fail("Неверная структура или версия файла билда.");
  var b=new Build{Title=data.Title.Trim(),Description=data.Description,HasReactor=data.HasReactor,FormasCount=data.FormasCount,HelminthAbility=data.HelminthAbility,UserId=OwnerId,AuthorName=User.Identity?.Name??"Tenno"};
  if(data.TargetType=="warframe"){b.Warframe=await db.Warframes.FirstOrDefaultAsync(x=>x.Name==data.TargetName);b.WarframeId=b.Warframe?.Id;}
  else if(data.TargetType=="weapon"){b.Weapon=await db.Weapons.FirstOrDefaultAsync(x=>x.Name==data.TargetName);b.WeaponId=b.Weapon?.Id;b.HelminthAbility=null;}
  else return Fail("Неизвестный тип снаряжения.");
  if(b.Warframe==null && b.Weapon==null)return Fail("Снаряжение не найдено в каталоге. Обновите базу данных перед импортом.");
  var names=data.Slots.Where(x=>!string.IsNullOrEmpty(x.ModName)).Select(x=>x.ModName!).Distinct().ToArray();
  var mods=await db.Mods.Where(x=>names.Contains(x.Name)).ToListAsync();var used=new HashSet<int>();var families=new HashSet<string>();
  foreach(var s in data.Slots)
  {
   if(!used.Add(s.SlotIndex)||!ModCompatibility.ValidSlots(b).Contains(s.SlotIndex)||!ModCompatibility.Polarities.Contains(s.Polarity)||s.Polarity=="Universal"&&s.SlotIndex!=8)return Fail("Недопустимый слот или полярность в файле.");
   Mod? mod=null;
   if(!string.IsNullOrEmpty(s.ModName))
   {
    mod=mods.FirstOrDefault(x=>x.Name==s.ModName);
    if(mod==null)return Fail($"Мод «{s.ModName}"+(s.ModName.Length<150?"» не найден в каталоге.":"…» не найден в каталоге."));
    if(s.Rank<0||s.Rank>mod.MaxRank||!ModCompatibility.CanEquip(b,mod,s.SlotIndex)||!families.Add(ModCompatibility.Family(mod)))return Fail("Файл содержит несовместимые моды, повторения или неверные ранги.");
   }
   else if(s.Rank!=0)return Fail("Пустой слот не может иметь ранг.");
   b.BuildMods.Add(new(){SlotIndex=s.SlotIndex,Mod=mod,ModId=mod?.Id,CurrentRank=s.Rank,SlotPolarity=s.Polarity});
  }
  var stats=BuildCalculator.Calculate(b);if(stats.UsedCapacity>stats.Capacity)return Fail($"Билд превышает вместимость: {stats.UsedCapacity} / {stats.Capacity}.");
  db.Builds.Add(b);await db.SaveChangesAsync();TempData["Success"]="Билд импортирован. Копия принадлежит вашему профилю.";return RedirectToAction(nameof(Details),new{id=b.Id});
 }
}
