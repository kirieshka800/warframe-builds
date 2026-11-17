using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarframeBuilds.Data;
using WarframeBuilds.ViewModels;
namespace WarframeBuilds.Controllers;
public class DatabaseController(ApplicationDbContext db) : Controller
{
 public async Task<IActionResult> Index(string? search,string kind="warframes",int page=1)
 {
  search=search?.Trim();if(search?.Length>200)search=search[..200];var text=search?.ToLower()??"";
  var vm=new ArsenalViewModel{Search=search,Kind=kind};
  if(kind=="weapons")
  {var q=db.Weapons.AsNoTracking().Where(x=>x.Name.ToLower().Contains(text)||x.Category.ToLower().Contains(text));vm.Total=await q.CountAsync();page=Math.Clamp(page,1,Math.Max(1,(vm.Total+23)/24));vm.Weapons=await q.OrderBy(x=>x.Name).Skip((page-1)*24).Take(24).ToListAsync();}
  else if(kind=="mods")
  {var q=db.Mods.AsNoTracking().Where(x=>x.Name.ToLower().Contains(text)||x.Description.ToLower().Contains(text));vm.Total=await q.CountAsync();page=Math.Clamp(page,1,Math.Max(1,(vm.Total+23)/24));vm.Mods=await q.OrderBy(x=>x.Name).Skip((page-1)*24).Take(24).ToListAsync();}
  else
  {vm.Kind="warframes";var q=db.Warframes.AsNoTracking().Where(x=>x.Name.ToLower().Contains(text));vm.Total=await q.CountAsync();page=Math.Clamp(page,1,Math.Max(1,(vm.Total+23)/24));vm.Warframes=await q.OrderBy(x=>x.Name).Skip((page-1)*24).Take(24).ToListAsync();}
  vm.Page=page;return View(vm);
 }
}
