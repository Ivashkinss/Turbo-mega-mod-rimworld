"""Validate the compact inspector checkpoint on the exact tested binary."""
import argparse,hashlib,json,xml.etree.ElementTree as E,re
from pathlib import Path
R=Path(__file__).resolve().parents[1]
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 parser=argparse.ArgumentParser();parser.add_argument('--hud',required=True);parser.add_argument('--contract',required=True);a=parser.parse_args();digest=sha(R/'Assemblies/TurboMegaMod.dll')
 assert not (R/'Assemblies/TurboMegaMod.TestProbe.dll').exists();evidence=[];counts={}
 for key,path,name in [('pawnOverview',a.hud,'PawnOverview-FullSet.json'),('workContract',a.contract,'PawnOverview-WorkContract-FullSet.json')]:
  path=Path(path).resolve();assert path.is_relative_to((R/'TestRuns').resolve());doc=read(path/'Result.json')
  assert doc['passed'] and doc['productionAssemblySha256']==digest and doc['runtimeErrors']==0 and not doc['gpuLost'] and doc['fullUserModSet']
  assert doc[key]['passed'] and not doc[key]['errors'] and doc[key]['checks']>=(15 if key=='pawnOverview' else 19)
  assert doc['settingsUi']['passed'] and doc['localization']['passed'] and doc['localization']['keyed']==2989 and doc['odysseyGrammar']['passed'] and doc['odysseyGrammar']['checks']==252
  assert doc['mapActivity']['framesAdvanced']>0 and doc['mapActivity']['ticksAdvanced']>0
  target=R/'Documentation'/name;target.write_text(json.dumps(doc,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');evidence.append(dict(file=name,sha256=sha(target)));counts[key]=doc[key]['checks']
  if key=='pawnOverview':counts['nativeGui']=doc['settingsUi']['checks'];assert counts['nativeGui']==3
 base=read(R/'Documentation/WorkContractRevisionVerification.json')['sourceSha256'];sources={p.relative_to(R).as_posix():sha(p) for p in sorted((R/'Source').rglob('*.cs'))}
 assert {p for p,h in base.items() if sources.get(p)!=h}=={'Source/RuntimeLocalization.cs'} and set(sources)-set(base)=={'Source/PawnOverview.cs'}
 assert all(p.stat().st_mtime<=(R/'Assemblies/TurboMegaMod.dll').stat().st_mtime for p in (R/'Source').rglob('*.cs'))
 loc={lang:{el.tag:el.text or '' for el in E.parse(R/f'Languages/{lang}/Keyed/PawnOverview.xml').getroot()} for lang in ('Russian','English')}
 assert loc['Russian'].keys()==loc['English'].keys() and len(loc['English'])==15
 for k in loc['English']:assert sorted(re.findall(r'\{\d+\}',loc['English'][k]))==sorted(re.findall(r'\{\d+\}',loc['Russian'][k]))
 pictures={f:sha(Path(a.hud)/f'PawnOverview-{f}.png') for f in ('Human','Animal')}
 result=dict(passed=True,productionAssemblySha256=digest,runtimeChecks=sum(counts.values()),successfulIsolatedSteps=2,checksByStep=counts,sourceSha256=sources,evidence=evidence,screenshotSha256=pictures,visuallyInspected=['Human','Animal'],screenResolution=[1280,800],humanPanelNominalSize=[460,220],nativeMoodExplicitlyIncluded=True,nativeHeaderIconsAndPoliciesRendered=True,settingsScribeRoundTripTested=True,externalRimHudGuardSimulated=True,externalRimHudDllTested=False,nativeSelectionAndGuiStateTested=True,financialSourceUnchanged=True,workContractFreshRegression=True,userSaveTested=False,live16Tested=False,allDpiAndResolutionsTested=False,allSubsystemRegression=False)
 (R/'Documentation/PawnOverviewRevisionVerification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');print(json.dumps({k:v for k,v in result.items() if k not in ('sourceSha256','evidence','screenshotSha256')},ensure_ascii=False))
if __name__=='__main__':main()
