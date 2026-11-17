using Microsoft.Extensions.Caching.Memory;
using WarframeBuilds.Models;
namespace WarframeBuilds.Services;
// Previews are short-lived server state, never trusted hidden form fields.
public sealed class AutoBuildProposalStore : IDisposable
{
 private readonly MemoryCache cache=new(new MemoryCacheOptions{SizeLimit=256});
 private readonly object admission=new();
 private readonly Queue<string> order=new();
 public sealed class Proposal
 {
  public required string OwnerId {get;init;}
  public required AutoBuildPlan Plan {get;init;}
  public required string Fingerprint {get;init;}
  public required DateTimeOffset ExpiresAt {get;init;}
  public SemaphoreSlim Gate {get;}=new(1,1);
  public int? SavedBuildId {get;set;}
  public bool Cancelled {get;set;}
 }
 public string Add(string owner,AutoBuildPlan plan,string fingerprint)
 {
  var token=Guid.NewGuid().ToString("N");var expires=DateTimeOffset.UtcNow.AddMinutes(15);
  lock(admission)
  {
   while(order.TryPeek(out var expired)&&!cache.TryGetValue(expired,out _))order.Dequeue();
   // MemoryCache rejects a new entry at its size limit; make room before admission.
   if(cache.Count>=256 && order.TryDequeue(out var oldest))cache.Remove(oldest);
   cache.Set(token,new Proposal{OwnerId=owner,Plan=plan,Fingerprint=fingerprint,ExpiresAt=expires},new MemoryCacheEntryOptions{AbsoluteExpiration=expires,Size=1});
   order.Enqueue(token);
  }
  return token;
 }
 public Proposal? Get(string? token)=>token is {Length:32}&&cache.TryGetValue<Proposal>(token,out var p)&&p!.ExpiresAt>DateTimeOffset.UtcNow?p:null;
 public void Dispose()=>cache.Dispose();
}
