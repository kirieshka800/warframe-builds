using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarframeBuilds.Data;
using WarframeBuilds.Services;
var builder=WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews(o=>o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
 .AddCookie(o=>{o.LoginPath=builder.Configuration.GetValue("Authentication:AllowLocalProfiles",true)?"/auth/profile":"/auth/login";o.AccessDeniedPath="/Home/AccessDenied";o.ExpireTimeSpan=TimeSpan.FromDays(90);o.SlidingExpiration=true;o.Cookie.HttpOnly=true;o.Cookie.SameSite=SameSiteMode.Lax;})
 .AddSteam(o=>{o.SignInScheme=CookieAuthenticationDefaults.AuthenticationScheme;});
builder.Services.AddDbContext<ApplicationDbContext>(o=>o.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")??throw new InvalidOperationException("Укажите ConnectionStrings__DefaultConnection.")));
builder.Services.AddHttpClient<WarframeApiService>(c=>c.Timeout=TimeSpan.FromMinutes(2));
builder.Services.AddSingleton<AutoBuildProposalStore>();
builder.Services.AddAntiforgery(o=>o.HeaderName="X-CSRF-TOKEN");
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["DataProtection:Path"]??Path.Combine(builder.Environment.ContentRootPath,"App_Data","Keys")));
var app=builder.Build();
if(!app.Environment.IsDevelopment()){app.UseExceptionHandler("/Home/Error");app.UseHsts();}
app.Use(async(context,next)=>{context.Response.Headers.XContentTypeOptions="nosniff";context.Response.Headers["Referrer-Policy"]="strict-origin-when-cross-origin";await next();});
app.UseStatusCodePagesWithReExecute("/Home/Status", "?code={0}");
app.UseStaticFiles();app.UseRouting();app.UseAuthentication();app.UseAuthorization();
app.MapControllerRoute("default","{controller=Home}/{action=Index}/{id?}");
using(var scope=app.Services.CreateScope())
{
 var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
 await db.Database.MigrateAsync();await CatalogSeeder.SeedAsync(db);
}
await app.RunAsync();
public partial class Program {}
