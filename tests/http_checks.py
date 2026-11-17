"""Real PostgreSQL/ASP.NET checks. Start an isolated test instance first.
WARFRAME_TEST_URL defaults to http://127.0.0.1:5026. Requires Python Playwright.
Creates only its own profiles, builds and guides; leaves public data untouched.
"""
import os,re,json,html,time
from playwright.sync_api import sync_playwright
BASE=os.getenv('WARFRAME_TEST_URL','http://127.0.0.1:5026')
checks=0
with sync_playwright() as p:
 def ok(value,message):
  global checks
  assert value,message;checks+=1
 def token(c,path='/auth/profile'):
  r=c.get(BASE+path);assert r.status==200,(path,r.status,r.text()[:100]);return html.unescape(re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"',r.text())[1])
 def post(c,path,data=None,t=None):return c.post(BASE+path,form={'__RequestVerificationToken':t or token(c),**(data or {})})
 c=p.request.new_context();other=p.request.new_context();anon=p.request.new_context()
 ok(c.post(BASE+'/auth/local',form={'displayName':'CSRF'}).status==400,'CSRF required')
 for ctx in [c,other]:ok(post(ctx,'/auth/local',{'displayName':'Same Name'}).status==200,'local profile sign in')
 form=c.get(BASE+'/Builds/Create').text()
 def options(name):
  select=re.search(r'<select[^>]*name="'+re.escape(name)+r'"[^>]*>(.*?)</select>',form,re.S)[1]
  return {html.unescape(re.sub('<[^>]*>','',n)).strip():int(v) for v,n in re.findall(r'<option[^>]*value="(\d+)"[^>]*>(.*?)</option>',select,re.S)}
 frames=options('Input.WarframeId');weapons={k.split(' (')[0]:v for k,v in options('Input.WeaponId').items()}
 frame=frames['Rhino'];weapon=weapons['Braton']
 def create(title,kind='warframe',target=None,reactor='true'):
  r=post(c,'/Builds/Create',{'Input.Title':title,'Input.TargetType':kind,'Input.WarframeId':str(target or frame) if kind=='warframe' else '', 'Input.WeaponId':str(target or weapon) if kind=='weapon' else '', 'Input.HasReactor':reactor,'Input.FormasCount':'0'})
  assert r.status==200 and '/Details/' in r.url,(r.url,r.text()[:1000]);return int(r.url.rsplit('/',1)[1])
 bid=create('HTTP frame '+str(time.time_ns()),reactor='false')
 def export(id,ctx=c):return ctx.get(BASE+f'/Builds/Export/{id}').json()
 ok(export(bid)['targetType']=='warframe','frame creation and export')
 source=c.get(BASE+f'/Builds/Details/{bid}').text()
 # Mod IDs and rank limits from real rendered controls; no hard-coded database IDs.
 def modid(name):
  for value,label in re.findall(r'<option[^>]*value="(\d+)"[^>]*>(.*?)</option>',source,re.S):
   if html.unescape(label).strip().startswith(name+' ·'):return int(value)
  raise AssertionError('missing mod '+name)
 def equip(name,slot,rank=0):return post(c,'/Builds/AddMod',{'buildId':str(bid),'modId':str(modid(name)),'slotIndex':str(slot),'currentRank':str(rank)})
 # Foreign profiles share a name but must never share authority.
 ok(post(other,f'/Builds/Delete/{bid}').status==403,'foreign delete denied')
 ok(post(other,'/Builds/SetSlotPolarity',{'buildId':str(bid),'slotIndex':'0','polarity':'Madurai'}).status==403,'foreign edit denied')
 ok(anon.get(BASE+f'/Builds/Details/{bid}').status==200,'public read')
 ok(post(c,'/Builds/AddMod',{'buildId':str(bid),'modId':str(modid('Vitality')),'slotIndex':'-1','currentRank':'0'}).status==400,'invalid slot rejected')
 equip('Vitality',0,-1);ok(not export(bid)['slots'],'negative rank not stored')
 equip('Vitality',9,0);ok(not export(bid)['slots'],'non-exilus mod rejected in exilus slot')
 equip('Corrosive Projection',8,5)
 equip('Vitality',0,10);equip('Redirection',1,10);equip('Flow',2,5)
 data=export(bid);ok(len([x for x in data['slots'] if x['modName']])==4,'aura permits valid 33/37 loadout')
 r=post(c,'/Builds/AddMod',{'buildId':str(bid),'modId':'0','slotIndex':'8','currentRank':'0'})
 ok('не хватает вместимости' in html.unescape(r.text()),'aura removal explains capacity failure')
 ok(any(s['slotIndex']==8 and s['modName']=='Corrosive Projection' for s in export(bid)['slots']),'aura removal cannot persist overcapacity')
 equip('Vitality',3,0);ok(len([x for x in export(bid)['slots'] if x['modName']=='Vitality'])==1,'duplicate family denied')
 ok(post(c,'/Builds/AddMod',{'buildId':str(bid),'modId':'0','slotIndex':'2','currentRank':'0'}).status==200,'normal removal')
 ok(post(c,'/Builds/SetSlotPolarity',{'buildId':str(bid),'slotIndex':'3','polarity':'Madurai'}).status==200,'empty polarized slot can persist')
 ok(any(s['slotIndex']==3 and s['modName'] is None for s in export(bid)['slots']),'empty slot survives export')
 copy=post(other,f'/Builds/Duplicate/{bid}');copyid=int(copy.url.rsplit('/',1)[1]);ok(copyid!=bid and export(copyid)['slots']==export(bid)['slots'],'duplicate preserves slots under new owner')
 ok(post(other,f'/Builds/Delete/{copyid}').status==200,'copy belongs to requester')
 data=export(bid)
 def imp(payload):
  return c.post(BASE+'/Builds/Import',multipart={'__RequestVerificationToken':token(c),'file':{'name':'build.json','mimeType':'application/json','buffer':json.dumps(payload).encode()}})
 r=imp(data);ok('/Details/' in r.url,'JSON roundtrip import');importid=int(r.url.rsplit('/',1)[1]);ok(export(importid)['slots']==data['slots'],'JSON roundtrip slots exact')
 bad=dict(data);bad['slots']=[{'slotIndex':0,'modName':'Vitality','rank':-10,'polarity':'None'}];r=imp(bad);ok('/Details/' not in r.url,'malformed import rejected')
 gun=create('HTTP gun '+str(time.time_ns()),'weapon');gunhtml=c.get(BASE+f'/Builds/Details/{gun}').text();ok('Урон за попадание' in html.unescape(gunhtml) and 'Слот 1' in html.unescape(gunhtml) and 'Аура' not in html.unescape(gunhtml),'weapon editor separate stats/slots')
 w=c.get(BASE+f'/Builds/Edit/{gun}');ok(w.status==200,'metadata editor')
 xss='<script>window.stolen=1</script>'
 post(c,'/Builds/AddComment',{'buildId':str(gun),'text':xss});ok(html.escape(xss) in c.get(BASE+f'/Builds/Details/{gun}').text(),'comment encoded')
 guide=post(c,'/Guides/Create',{'Title':'Guide '+str(time.time_ns()),'Description':'Useful guide','Content':xss,'Category':'Основы'})
 ok('/Guides/Details/' in guide.url,'guide created');gid=int(guide.url.rsplit('/',1)[1]);ok(xss not in guide.text(),'guide text encoded')
 ok(post(other,f'/Guides/Delete/{gid}').status==403,'same-name foreign guide deletion denied')
 ok(c.get(BASE+'/Builds/Details/99999999').status==404,'unknown build returns404')
 for target in [bid,gun,importid]:post(c,f'/Builds/Delete/{target}')
 post(c,f'/Guides/Delete/{gid}');ok(c.get(BASE+f'/Builds/Details/{bid}').status==404,'delete and comment cascades')
 post(c,'/auth/logout');ok('/auth/profile' in c.get(BASE+'/Builds/Create').url,'logout clears authority')
 print(f'PASS {checks} real HTTP checks: auth/CSRF/ownership, frame+weapon, capacity/ranks/slots, JSON copy/import, comments/guides,404/logout')
