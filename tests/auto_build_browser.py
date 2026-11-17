"""Browser flow for automatic build previews and deliberate confirmation."""
import os,shutil,re
from pathlib import Path
from playwright.sync_api import sync_playwright,expect
BASE=os.getenv('WARFRAME_TEST_URL','http://127.0.0.1:5026')
ARTIFACTS=Path(os.getenv('WARFRAME_ARTIFACTS',str(Path(__file__).parent/'artifacts')));ARTIFACTS.mkdir(exist_ok=True,parents=True)
with sync_playwright() as p:
 browser=p.chromium.launch(executable_path=os.getenv('CHROMIUM_PATH') or shutil.which('chromium'),headless=True,args=['--no-sandbox'])
 c=browser.new_context(viewport={'width':1440,'height':1000},reduced_motion='reduce');page=c.new_page();errors=[];page.on('pageerror',lambda e:errors.append(str(e)))
 page.goto(BASE+'/auth/profile');page.locator('[name=displayName]').fill('Auto browser');page.get_by_role('button',name='Создать профиль',exact=True).click();page.wait_for_url('**/Builds?mine=*')
 page.get_by_role('link',name='Подобрать билд',exact=True).click();expect(page.locator('h1')).to_have_text('От цели — к сборке.')
 page.locator('[name="Input.WarframeId"]').select_option(label='Rhino');page.locator('[data-auto-goal]').select_option('survival');page.locator('[name="Input.MaxFormas"]').fill('2');page.locator('[name="Input.HasReactor"][type=checkbox]').uncheck();page.locator('[data-auto-generate]').click();page.wait_for_url('**/AutoBuild/Proposal?token=*')
 expect(page.locator('h1')).to_contain_text('Выживаемость');expect(page.locator('[data-testid=auto-preview-slots]')).to_contain_text('Vitality');expect(page.locator('[data-auto-confirm]')).to_be_enabled()
 # Reload keeps the same proposal and preserves no-write-until-confirm flow.
 preview=page.url;page.reload();expect(page.locator('h1')).to_contain_text('Выживаемость');assert page.url==preview
 page.set_viewport_size({'width':390,'height':844});assert page.evaluate('document.documentElement.scrollWidth <= innerWidth'),'preview page overflow'
 page.screenshot(path=str(ARTIFACTS/'auto-preview-mobile.png'),full_page=True)
 page.locator('[data-auto-cancel]').click();page.wait_for_url('**/AutoBuild');expect(page.get_by_text('Предложение отменено. Билд не создавался.',exact=True)).to_be_visible()
 page.goto(BASE+'/AutoBuild');page.locator('[data-target-type]').select_option('weapon');expect(page.locator('[name="Input.WeaponId"]')).to_be_visible();expect(page.locator('[name="Input.WarframeId"]')).not_to_be_visible()
 assert page.locator('[data-auto-goal] option[value=survival]').evaluate('option => option.disabled')
 page.locator('[name="Input.WeaponId"]').select_option(label='Braton (Primary)');page.locator('[data-auto-goal]').select_option('critical');page.locator('[name="Input.MaxFormas"]').fill('0')
 assert page.evaluate('document.documentElement.scrollWidth <= innerWidth'),'form page overflow'
 page.locator('[data-auto-generate]').click();page.wait_for_url('**/AutoBuild/Proposal?token=*');expect(page.locator('h1')).to_contain_text('Критический урон');expect(page.locator('[data-testid=auto-preview-slots]')).to_contain_text('Point Strike')
 page.set_viewport_size({'width':1440,'height':1000});page.screenshot(path=str(ARTIFACTS/'auto-preview-desktop.png'),full_page=True)
 page.locator('[data-auto-confirm]').click();page.wait_for_url('**/Builds/Details/*');expect(page.locator('h1')).to_contain_text('Критический урон');expect(page.locator('[data-testid=mod-slot-0]')).to_contain_text('Point Strike')
 expect(page.locator('[data-testid=build-statistics]')).to_contain_text('Вместимость');page.reload();expect(page.locator('[data-testid=mod-slot-0]')).to_contain_text('Point Strike')
 assert not errors,errors
 browser.close();print('PASS auto browser: frame survival/weapon critical, budgets, preview/reload/cancel, explicit confirm/result persistence, type-specific goals, desktop/mobile; no JS errors')
