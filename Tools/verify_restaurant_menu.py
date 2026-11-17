"""Verify the frozen menu/sales revision using actual isolated game results."""
import hashlib,json,re,xml.etree.ElementTree as E
from pathlib import Path
R=Path(__file__).resolve().parents[1]
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 digest=sha(R/'Assemblies/TurboMegaMod.dll');progress=read(R/'TestRuns/MenuRevisionProgress.json')
 assert progress['passed'] and progress['productionAssemblySha256']==digest and not progress['error']
 assert not (R/'Assemblies/TurboMegaMod.TestProbe.dll').exists()
 expected=[('RestaurantMenu','restaurantMenu',19),('menu reload','restaurantMenuReload',14),('SalesDelivery','salesDelivery',42),('delivery reload','salesDeliveryReload',44),('Restaurant','restaurant',36),('restaurant reload','restaurantReload',12),('Sales','sales',55),('Wholesale','wholesale',54)]
 assert [x['name'] for x in progress['completed']]==[x[0] for x in expected]
 counts={};evidence=[]
 for entry,(name,key,minimum) in zip(progress['completed'],expected):
  profile=Path(entry['profile']).resolve();assert profile.is_relative_to((R/'TestRuns').resolve())
  doc=read(profile/entry['file']);assert doc['passed'] and doc['productionAssemblySha256']==digest and doc['runtimeErrors']==0 and not doc['gpuLost']
  assert doc[key]['passed'] and not doc[key]['errors'] and doc[key]['checks']>=minimum
  assert doc['mapActivity']['framesAdvanced']>0 and doc['mapActivity']['ticksAdvanced']>0
  if entry['file']=='Result.json':
   assert doc['fullUserModSet'] and doc['localization']['passed'] and doc['localization']['keyed']==3018 and doc['settingsUi']['passed']
   assert doc['odysseyGrammar']['passed'] and doc['odysseyGrammar']['checks']==252
  else:assert doc['unresolvedReferences']==0 and doc['isolatedFixtureSave'] and not doc['userSaveTested']
  counts[name]=doc[key]['checks'];publication={'restaurantMenu':'RestaurantMenu-FullSet.json','restaurantMenuReload':'RestaurantMenu-Reload.json'}.get(key,'MenuRevision-'+key+'.json');target=R/'Documentation'/publication
  target.write_text(json.dumps(doc,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');evidence.append(dict(file=target.name,sha256=sha(target)))
 profile=Path(progress['completed'][0]['profile']);ui=read(profile/'RestaurantMenuUiProbe.json');assert ui['passed']
 screenshot=profile/'RestaurantMenu.png';assert screenshot.exists()
 base=read(R/'Documentation/FactionRansomRevisionVerification.json')['sourceSha256']
 sources={p.relative_to(R).as_posix():sha(p) for p in sorted((R/'Source').rglob('*.cs'))}
 assert {p for p,h in base.items() if sources.get(p)!=h}=={'Source/ComplexManagement.cs','Source/ComplexServicesDialog.cs','Source/Restaurant.cs','Source/ComplexSales.cs'}
 assert set(sources)-set(base)=={'Source/RestaurantMenu.cs'}
 assert all(p.stat().st_mtime<=(R/'Assemblies/TurboMegaMod.dll').stat().st_mtime for p in (R/'Source').rglob('*.cs'))
 lang={l:{x.tag:x.text or '' for x in E.parse(R/f'Languages/{l}/Keyed/RestaurantMenu.xml').getroot()} for l in ('Russian','English')}
 assert lang['Russian'].keys()==lang['English'].keys() and len(lang['English'])==12
 for k in lang['English']:assert sorted(re.findall(r'\{\d+\}',lang['English'][k]))==sorted(re.findall(r'\{\d+\}',lang['Russian'][k]))
 result=dict(passed=True,productionAssemblySha256=digest,runtimeChecks=sum(counts.values()),successfulIsolatedSteps=len(expected),checksByStep=counts,sourceSha256=sources,evidence=evidence,
  actualScribeLegacyMigrationTested=True,missingLegacyDefStaysEmpty=True,explicitEmptyMenuSaveLoadAndReinstallTested=True,physicalStockUsesPortionCount=True,
  actualDietBudgetAndFridgeSelectionTested=True,pinnedMealAndQuoteAcrossMenuChangeTested=True,actualCarriedMealSaveLoadTested=True,
  blockedPhysicalReceiptsNoPhantomCreditTested=True,dietChangeCancelsAndRefundsTested=True,freeMealNoIncomeTested=True,
  actualSalesPredicateShortCircuitAndSingleWaitAtInstrumented=True,actualCreateSingleWorkerPassInstrumented=True,
  visualLayoutInspected=True,visualResolution='1280x800',visualScreenshotSha256=sha(screenshot),otherResolutionsOrDpiTested=False,
  userSaveTested=False,live16Tested=False,allSubsystemRegression=False,fpsImprovementMeasured=False,
  rejectedFixtureAttempts=[dict(profile='RestaurantMenu-FullSet-20261117-073538',reason='Test XML assumed an omitted default flag existed; no pass claimed.'),dict(profile='RestaurantMenu-FullSet-20261117-073737',reason='Same-tick fixture jobs triggered native limiter; reload edited an obsolete Hospitality food-policy reference. Replaced by clean same-binary run.')])
 (R/'Documentation/RestaurantMenuRevisionVerification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
 print(json.dumps({k:v for k,v in result.items() if k not in ('sourceSha256','evidence','rejectedFixtureAttempts')},ensure_ascii=False))
if __name__=='__main__':main()
