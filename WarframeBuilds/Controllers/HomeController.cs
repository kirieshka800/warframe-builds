using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarframeBuilds.Data;
using WarframeBuilds.Models;
using WarframeBuilds.Services;
namespace WarframeBuilds.Controllers;
public class HomeController(ApplicationDbContext db,WarframeApiService api,IConfiguration config,ILogger<HomeController> log) : Controller
{
 private static readonly SemaphoreSlim SyncLock=new(1,1);
 public async Task<IActionResult> Index()
 {
  ViewBag.FrameCount=await db.Warframes.CountAsync();ViewBag.WeaponCount=await db.Weapons.CountAsync();ViewBag.ModCount=await db.Mods.CountAsync();ViewBag.BuildCount=await db.Builds.CountAsync();
  ViewBag.RecentBuilds=await db.Builds.Include(x=>x.Warframe).Include(x=>x.Weapon).OrderByDescending(x=>x.UpdatedAt).Take(4).AsNoTracking().ToListAsync();
  return View(await db.Warframes.OrderBy(x=>x.Name).Take(8).AsNoTracking().ToListAsync());
 }
 [HttpPost,Authorize]public async Task<IActionResult> SyncData()
 {
  if(!config.GetValue("Catalog:AllowSync",true))return Forbid();
  if(!await SyncLock.WaitAsync(0)){TempData["Error"]="Каталог уже обновляется. Попробуйте позже.";return RedirectToAction("Index","Database");}
  try{await api.SyncWarframesAsync();await api.SyncModsAsync();await api.SyncWeaponsAsync();TempData["Success"]="Каталог обновлён из WFCD. Существующие билды сохранены.";}
  catch(Exception e) when(e is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException or InvalidDataException){log.LogWarning(e,"Catalogue sync failed");TempData["Error"]="Не удалось завершить обновление каталога. Уже загруженные данные сохранены; можно продолжать работу и повторить позже.";}
  finally{SyncLock.Release();}return RedirectToAction("Index","Database");
 }
 [ResponseCache(Duration=0,Location=ResponseCacheLocation.None,NoStore=true)]public IActionResult Error()=>View(new ErrorViewModel{RequestId=Activity.Current?.Id??HttpContext.TraceIdentifier});
 public IActionResult Status(int code){ViewBag.StatusCode=code;Response.StatusCode=code;return View("Status");}
 public IActionResult AccessDenied()=>Status(403);
 public IActionResult Privacy()=>View();
}
