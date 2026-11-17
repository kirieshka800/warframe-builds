using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarframeBuilds.Data;
using WarframeBuilds.Models;
namespace WarframeBuilds.Controllers;
public class GuidesController(ApplicationDbContext db) : Controller
{
 private string OwnerId=>User.FindFirstValue(ClaimTypes.NameIdentifier)??"";
 private bool Owns(Guide g)=>OwnerId.Length>0&&g.UserId==OwnerId;
 public async Task<IActionResult> Index(string? category,string? search)
 {
  var q=db.Guides.AsNoTracking();if(!string.IsNullOrWhiteSpace(category))q=q.Where(x=>x.Category==category);
  if(!string.IsNullOrWhiteSpace(search)){var s=search.Trim().ToLower();q=q.Where(x=>x.Title.ToLower().Contains(s)||x.Description.ToLower().Contains(s));}
  ViewBag.Category=category;ViewBag.Search=search;return View(await q.OrderByDescending(x=>x.IsFeatured).ThenByDescending(x=>x.Id).ToListAsync());
 }
 public async Task<IActionResult> Details(int id){var g=await db.Guides.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==id);if(g==null)return NotFound();ViewBag.IsOwner=Owns(g);return View(g);}
 [HttpGet,Authorize]public IActionResult Create()=>View(new Guide());
 private void Validate(Guide g){if(string.IsNullOrWhiteSpace(g.Content))ModelState.AddModelError(nameof(g.Content),"Напишите текст гайда.");}
 [HttpPost,Authorize]public async Task<IActionResult> Create([Bind("Title,Description,Content,Category")]Guide guide)
 {
  Validate(guide);if(!ModelState.IsValid)return View(guide);
  guide.UserId=OwnerId;guide.Author=User.Identity?.Name??"Tenno";guide.ReadTime=Math.Max(1,guide.Content.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries).Length/180)+" мин";db.Guides.Add(guide);await db.SaveChangesAsync();return RedirectToAction(nameof(Details),new{id=guide.Id});
 }
 [HttpGet,Authorize]public async Task<IActionResult> Edit(int id){var g=await db.Guides.FindAsync(id);if(g==null)return NotFound();if(!Owns(g))return Forbid();return View(g);}
 [HttpPost,Authorize]public async Task<IActionResult> Edit(int id,[Bind("Title,Description,Content,Category")]Guide model)
 {
  var g=await db.Guides.FindAsync(id);if(g==null)return NotFound();if(!Owns(g))return Forbid();Validate(model);model.Id=id;if(!ModelState.IsValid)return View(model);
  g.Title=model.Title.Trim();g.Description=model.Description.Trim();g.Content=model.Content.Trim();g.Category=model.Category.Trim();g.ReadTime=Math.Max(1,g.Content.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries).Length/180)+" мин";await db.SaveChangesAsync();return RedirectToAction(nameof(Details),new{id});
 }
 [HttpPost,Authorize]public async Task<IActionResult> Delete(int id){var g=await db.Guides.FindAsync(id);if(g==null)return NotFound();if(!Owns(g))return Forbid();db.Guides.Remove(g);await db.SaveChangesAsync();return RedirectToAction(nameof(Index));}
}
