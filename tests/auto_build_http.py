"""Automatic preview/confirmation integration checks; isolated test app only."""
import re,os,html,concurrent.futures,urllib.request,urllib.parse,subprocess
from playwright.sync_api import sync_playwright
BASE=os.getenv('WARFRAME_TEST_URL','http://127.0.0.1:5026');count=0
with sync_playwright() as p:
 def ok(value,message):
  global count
  assert value,message;count+=1
 def token(c,path='/auth/profile'):
  r=c.get(BASE+path);assert r.ok,(path,r.status);return html.unescape(re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"',r.text())[1])
 def post(c,path,data=None):return c.post(BASE+path,form={'__RequestVerificationToken':token(c),**(data or {})})
 c=p.request.new_context();other=p.request.new_context();anon=p.request.new_context()
 for ctx in [c,other]:post(ctx,'/auth/local',{'displayName':'Auto Test'})
 def total():
  s=html.unescape(c.get(BASE+'/Builds?mine=true').text());return int(re.search('Найдено:\\s*(\\d+)',s)[1])
 def fields():
  page=c.get(BASE+'/AutoBuild').text()
  def opts(name):
   s=re.search(r'<select[^>]*name="'+re.escape(name)+r'"[^>]*>(.*?)</select>',page,re.S)[1]
   return {html.unescape(re.sub('<[^>]*>','',n)).strip().split(' (')[0]:int(v) for v,n in re.findall(r'<option[^>]*value="(\d+)"[^>]*>(.*?)</option>',s,re.S)}
  return opts('Input.WarframeId'),opts('Input.WeaponId')
 frames,weapons=fields()
 def preview(kind='warframe',name='Rhino',goal='balanced',formas=0,reactor=True):
  data={'Input.TargetType':kind,'Input.WarframeId':str(frames.get(name,'')),'Input.WeaponId':str(weapons.get(name,'')),'Input.Goal':goal,'Input.MaxFormas':str(formas),'Input.HasReactor':str(reactor).lower()}
  r=post(c,'/AutoBuild/Preview',data);assert r.status==200,(r.status,r.text()[:200]);return r
 def proposal_token(r):return urllib.parse.parse_qs(urllib.parse.urlsplit(r.url).query)['token'][0]
 ok('/auth/profile' in anon.get(BASE+'/AutoBuild').url,'autopick requires login')
 ok(c.post(BASE+'/AutoBuild/Preview',form={}).status==400,'preview CSRF')
 ok(total()==0,'new profile empty')
 r=preview(goal='strength');t=proposal_token(r);ok(total()==0,'preview never creates a build')
 ok('Сила способностей' in html.unescape(r.text()),'selected goal shown')
 ok('Подтвердить и сохранить' in html.unescape(r.text()),'explicit confirmation offered')
 ok(other.get(BASE+'/AutoBuild/Proposal?token='+t).status==404,'foreign preview denied')
 ok(post(other,'/AutoBuild/Confirm',{'token':t}).status==404,'foreign confirmation denied')
 ok(c.post(BASE+'/AutoBuild/Confirm',form={'token':t}).status==400,'confirm CSRF')
 response=post(c,'/AutoBuild/Confirm',{'token':t,'Input.MaxFormas':'8','FormasCount':'99'});saved=int(response.url.rsplit('/',1)[1]);data=c.get(BASE+f'/Builds/Export/{saved}').json()
 ok(total()==1,'confirmation saves exactly one build');ok(data['formasCount']==0,'hidden form tampering ignored')
 ok(any(s['modName']=='Intensify' for s in data['slots']),'strength proposal saved with priority mod')
 again=post(c,'/AutoBuild/Confirm',{'token':t});ok(again.url.endswith('/'+str(saved)) and total()==1,'repeat confirm idempotent')
 ok(c.get(BASE+'/AutoBuild/Proposal?token='+t).url.endswith('/'+str(saved)),'saved preview redirects to result')
 r=preview(goal='survival',formas=2,reactor=False);t=proposal_token(r);post(c,'/AutoBuild/Cancel',{'token':t});ok(total()==1,'cancel never saves')
 post(c,'/AutoBuild/Confirm',{'token':t});ok(total()==1,'cancelled preview cannot be confirmed')
 post(c,'/AutoBuild/Confirm',{'token':'0'*32});ok(total()==1,'unknown/expired preview does not save')
 for kind,name,goal in [('weapon','Braton','critical'),('weapon','Lato','status'),('weapon','Skana','balanced'),('weapon','Bo','balanced')]:
  r=preview(kind,name,goal,formas=1);t=proposal_token(r);save=post(c,'/AutoBuild/Confirm',{'token':t});ok('/Builds/Details/' in save.url,name+' saved')
  d=c.get(BASE+'/Builds/Export/'+save.url.rsplit('/',1)[1]).json();ok(d['targetType']=='weapon' and 0<=d['formasCount']<=1,name+' type/budget')
  if name in ['Skana','Bo']:ok(any(s['slotIndex']==8 for s in d['slots']),name+' compatible stance')
 # Double clicks in parallel use independent HTTP clients with the authenticated cookie.
 r=preview(goal='balanced');t=proposal_token(r);csrf=token(c)
 cookie='; '.join(x['name']+'='+x['value'] for x in c.storage_state()['cookies'])
 def confirm_parallel(_):
  body=urllib.parse.urlencode({'token':t,'__RequestVerificationToken':csrf}).encode()
  req=urllib.request.Request(BASE+'/AutoBuild/Confirm',data=body,headers={'Cookie':cookie,'Content-Type':'application/x-www-form-urlencoded'})
  with urllib.request.urlopen(req) as response:return response.geturl()
 before=total()
 with concurrent.futures.ThreadPoolExecutor(2) as executor:urls=list(executor.map(confirm_parallel,range(2)))
 ok(urls[0]==urls[1] and total()==before+1,'parallel confirmation one result')
 # Catalogue drift must not silently save another configuration.
 database=os.getenv('WARFRAME_TEST_DATABASE_URL');psql=os.getenv('PSQL','psql')
 if database:
  r=preview();t=proposal_token(r)
  def sql(s):return subprocess.run([psql,database,'-v','ON_ERROR_STOP=1','-tAc',s],capture_output=True,text=True,check=True).stdout.strip()
  sql('UPDATE "Mods" SET "BaseDrain"="BaseDrain"+1 WHERE "Name"=\'Vitality\';')
  before=total()
  try:
   result=post(c,'/AutoBuild/Confirm',{'token':t});ok('Каталог изменился' in html.unescape(result.text()) and total()==before,'catalogue drift blocked')
  finally:sql('UPDATE "Mods" SET "BaseDrain"="BaseDrain"-1 WHERE "Name"=\'Vitality\';')
 # Invalid rules/targets/budgets return form errors without creating anything.
 for kind,name,goal,formas in [('weapon','Braton','strength',0),('warframe','Rhino','critical',0),('warframe','missing','balanced',0),('warframe','Rhino','balanced',-1),('warframe','Rhino','balanced',9)]:
  before=total();r=preview(kind,name,goal,formas);ok('/Proposal' not in r.url and total()==before,'invalid form rejected')
 print(f'PASS {count} auto-build HTTP checks: preview/owner/CSRF/confirm/cancel/replay, goals, guns/melee/stances, budget, concurrent save, catalogue drift, validation')
