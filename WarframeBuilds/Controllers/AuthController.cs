using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
namespace WarframeBuilds.Controllers;
public class AuthController(IConfiguration config) : Controller
{
 [HttpGet("auth/login")] public IActionResult Login(string? returnUrl=null)=>Challenge(new AuthenticationProperties{RedirectUri=Url.IsLocalUrl(returnUrl)?returnUrl:"/Builds?mine=true"},"Steam");
 [HttpGet("auth/profile")] public IActionResult Profile(){if(!config.GetValue("Authentication:AllowLocalProfiles",true))return NotFound();return View();}
 [HttpPost("auth/local")]public async Task<IActionResult> Local(string? displayName)
 {
  if(!config.GetValue("Authentication:AllowLocalProfiles",true))return NotFound();
  displayName=displayName?.Trim();if(string.IsNullOrEmpty(displayName)||displayName.Length>50){TempData["Error"]="Введите имя длиной от 1 до 50 символов.";return RedirectToAction(nameof(Profile));}
  // Each browser profile has an independent random identifier; display names are not identities.
  var claims=new[]{new Claim(ClaimTypes.NameIdentifier,"local:"+Guid.NewGuid().ToString("N")),new Claim(ClaimTypes.Name,displayName),new Claim("profile_type","local")};
  await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme)),new AuthenticationProperties{IsPersistent=true});
  TempData["Success"]="Локальный профиль создан. Для постоянного доступа с разных устройств используйте Steam; экспортируйте важные билды в JSON.";return RedirectToAction("Index","Builds",new{mine=true});
 }
 [HttpPost("auth/logout")]public async Task<IActionResult> Logout(){await HttpContext.SignOutAsync();return RedirectToAction("Index","Home");}
}
