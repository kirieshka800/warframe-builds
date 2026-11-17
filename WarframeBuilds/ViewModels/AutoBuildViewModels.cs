using System.ComponentModel.DataAnnotations;
using WarframeBuilds.Models;
namespace WarframeBuilds.ViewModels;
public sealed class AutoBuildInput
{
 public string TargetType {get;set;}="warframe";
 public int? WarframeId {get;set;}
 public int? WeaponId {get;set;}
 public string Goal {get;set;}="balanced";
 public bool HasReactor {get;set;}=true;
 [Range(0,8,ErrorMessage="Укажите от 0 до 8 форм.")]public int MaxFormas {get;set;}
}
public sealed class AutoBuildFormViewModel
{
 public AutoBuildInput Input {get;set;}=new();
 public List<Warframe> Warframes {get;set;}=new();
 public List<Weapon> Weapons {get;set;}=new();
}
public sealed class AutoBuildPreviewViewModel
{
 public string Token {get;set;}="";
 public AutoBuildPlan Plan {get;set;}=null!;
 public BuildStatistics Stats {get;set;}=null!;
 public DateTimeOffset ExpiresAt {get;set;}
}
