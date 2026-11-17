using Microsoft.EntityFrameworkCore;
using WarframeBuilds.Models;
namespace WarframeBuilds.Data;
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
 public DbSet<Warframe> Warframes => Set<Warframe>();
 public DbSet<Guide> Guides => Set<Guide>();
 public DbSet<Mod> Mods => Set<Mod>();
 public DbSet<Weapon> Weapons => Set<Weapon>();
 public DbSet<GearItem> GearItems => Set<GearItem>();
 public DbSet<Build> Builds => Set<Build>();
 public DbSet<BuildMod> BuildMods => Set<BuildMod>();
 public DbSet<BuildComment> BuildComments => Set<BuildComment>();
 protected override void OnModelCreating(ModelBuilder m)
 {
  // Request length validation must not truncate existing user content during upgrade.
  foreach(var e in m.Model.GetEntityTypes()) foreach(var p in e.GetProperties().Where(p=>p.ClrType==typeof(string))) p.SetColumnType("text");
  m.Entity<BuildMod>().HasKey(x=>x.Id);
  m.Entity<BuildMod>().HasIndex(x=>new{x.BuildId,x.SlotIndex}).IsUnique();
  m.Entity<BuildMod>().HasOne(x=>x.Build).WithMany(x=>x.BuildMods).HasForeignKey(x=>x.BuildId).OnDelete(DeleteBehavior.Cascade);
  m.Entity<BuildMod>().HasOne(x=>x.Mod).WithMany().HasForeignKey(x=>x.ModId).OnDelete(DeleteBehavior.SetNull);
  m.Entity<BuildComment>().HasOne(x=>x.Build).WithMany().HasForeignKey(x=>x.BuildId).OnDelete(DeleteBehavior.Cascade);
  m.Entity<Build>().Property(x=>x.UpdatedAt).IsConcurrencyToken();
  m.Entity<Build>().HasIndex(x=>x.UserId);
  m.Entity<Build>().ToTable(t=>t.HasCheckConstraint("CK_Build_Target","\"WarframeId\" IS NULL OR \"WeaponId\" IS NULL"));
 }
}
