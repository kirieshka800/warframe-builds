"""Headless end-to-end checks against an isolated running ASP.NET/PostgreSQL app."""
import os,time,json,shutil
from pathlib import Path
from playwright.sync_api import sync_playwright,expect
BASE=os.getenv('WARFRAME_TEST_URL','http://127.0.0.1:5026')
ARTIFACTS=Path(os.getenv('WARFRAME_ARTIFACTS',str(Path(__file__).parent/'artifacts')));ARTIFACTS.mkdir(parents=True,exist_ok=True)
with sync_playwright() as p:
 browser=p.chromium.launch(executable_path=os.getenv('CHROMIUM_PATH') or shutil.which('chromium'),headless=True,args=['--no-sandbox'])
 c=browser.new_context(viewport={'width':1440,'height':1000},permissions=['clipboard-read','clipboard-write'],reduced_motion='reduce')
 page=c.new_page();errors=[];page.on('pageerror',lambda e:errors.append(str(e)))
 page.goto(BASE+'/auth/profile');page.locator('[name=displayName]').fill('Browser Tenno');page.get_by_role('button',name='Создать профиль',exact=True).click();page.wait_for_url('**/Builds?mine=*')
 page.goto(BASE+'/Builds/Create');page.locator('[name="Input.Title"]').fill('Browser frame '+str(time.time_ns()));page.locator('[name="Input.WarframeId"]').select_option(label='Rhino');page.get_by_role('button',name='Создать и перейти к модам →').click();page.wait_for_url('**/Builds/Details/*');frameurl=page.url
 expect(page.locator('[data-testid=mod-slot-8]')).to_contain_text('Аура')
 slot=page.locator('[data-testid=mod-slot-0]');slot.locator('summary').click();slot.locator('[data-mod-search]').fill('Vitality');select=slot.locator('[data-mod-select]');option=select.locator('option').filter(has_text='Vitality ·').first;select.select_option(value=option.get_attribute('value'));slot.locator('[data-rank]').select_option('10');slot.get_by_role('button',name='Применить',exact=True).click();page.wait_for_load_state();expect(page.locator('[data-testid=mod-slot-0]')).to_contain_text('Ранг 10 / 10')
 slot=page.locator('[data-testid=mod-slot-0]');slot.locator('summary').click();slot.locator('[name=polarity]').select_option('Vazarin');slot.get_by_role('button',name='Сохранить полярность').click();page.wait_for_load_state();expect(page.locator('[data-testid=mod-slot-0]')).to_contain_text('Vazarin');page.reload();expect(page.locator('[data-testid=mod-slot-0]')).to_contain_text('Ранг 10 / 10')
 page.locator('[data-copy-url]').click();expect(page.locator('[data-copy-url]')).to_contain_text('Ссылка скопирована');assert page.evaluate('navigator.clipboard.readText()')==frameurl
 page.locator('#comment-text').fill('<script>alert("test")</script>');page.locator('[data-testid=comment-form] button').click();page.wait_for_load_state();expect(page.locator('[data-testid=build-comments]')).to_contain_text('<script>alert("test")</script>')
 page.goto(BASE+'/Builds/Create');page.locator('[name="Input.Title"]').fill('Browser gun '+str(time.time_ns()));page.locator('[name="Input.TargetType"]').select_option('weapon');expect(page.locator('[name="Input.WeaponId"]')).to_be_visible();expect(page.locator('[name="Input.WarframeId"]')).not_to_be_visible();page.locator('[name="Input.WeaponId"]').select_option(label='Braton (Primary)');page.get_by_role('button',name='Создать и перейти к модам →').click();page.wait_for_url('**/Builds/Details/*');gunurl=page.url
 expect(page.locator('[data-testid=mod-slot-8]')).to_have_count(0);expect(page.locator('[data-testid=build-statistics]')).to_contain_text('Критический шанс')
 slot=page.locator('[data-testid=mod-slot-0]');slot.locator('summary').click();slot.locator('[data-mod-search]').fill('Serration');select=slot.locator('[data-mod-select]');option=select.locator('option').filter(has_text='Serration ·').first;select.select_option(value=option.get_attribute('value'));slot.locator('[data-rank]').select_option('10');slot.get_by_role('button',name='Применить',exact=True).click();page.wait_for_load_state();expect(page.locator('[data-testid=mod-slot-0]')).to_contain_text('Serration')
 page.get_by_role('link',name='Настройки',exact=True).click();page.locator('[name="Input.Title"]').fill('Updated browser weapon');page.get_by_role('button',name='Сохранить изменения').click();page.wait_for_url('**/Builds/Details/*');expect(page.locator('h1')).to_have_text('Updated browser weapon')
 for width in [1440,390]:
  page.set_viewport_size({'width':width,'height':1000 if width==1440 else 844})
  for path in ['/', '/Builds','/Database','/Database?kind=weapons','/Database?kind=mods','/Guides','/Guides/Create','/Builds/Create',frameurl,gunurl]:
   response=page.goto(path if path.startswith('http') else BASE+path);assert response.status==200,(path,response.status)
   expect(page.locator('h1').first).to_be_visible();assert page.evaluate('document.documentElement.scrollWidth <= innerWidth'),(path,width,'overflow')
 page.set_viewport_size({'width':1440,'height':1000});page.goto(BASE+'/');page.screenshot(path=str(ARTIFACTS/'home-final.png'),full_page=True)
 page.set_viewport_size({'width':390,'height':844});page.goto(gunurl);page.screenshot(path=str(ARTIFACTS/'editor-mobile.png'),full_page=True)
 assert not errors,errors
 browser.close();print('PASS browser: frame+weapon creation, mod search/rank/polarity persistence, clipboard, safe comment, metadata edit, 10 screens desktop/mobile; no JS errors')
