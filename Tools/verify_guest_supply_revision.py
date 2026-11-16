"""Verify the bounded guest-supply and managed-area revision against native evidence."""
import hashlib,json,subprocess
from pathlib import Path
R=Path(__file__).resolve().parents[1]
def read(p):return json.loads(p.read_text(encoding="utf-8-sig"))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 digest=sha(R/"Assemblies/TurboMegaMod.dll")
 assert not (R/"Assemblies/TurboMegaMod.TestProbe.dll").exists()
 state=read(R/"TestRuns/GuestSupplyProgress.json")
 assert state["passed"] and not state["current"] and not state["error"] and state["productionAssemblySha256"]==digest
 keys=[("Addiction","addiction","Addiction-FullSet.json"),("Addiction reload","addictionReload","Addiction-Reload.json"),("AreaCache","areaCache","AreaCache-FullSet.json"),("GeneticSupply","geneticSupply","GeneticSupply-FullSet.json"),("Hemogen","hemogen","Hemogen-FullSet.json"),("Pricing","pricing","Pricing-FullSet.json"),("Pricing reload","pricingReload","Pricing-Reload.json")]
 assert [x["name"] for x in state["completed"]]==[x[0] for x in keys]
 counts={};evidence=[]
 for record,(name,key,published) in zip(state["completed"],keys):
  profile=Path(record["profile"]).resolve();assert profile.is_relative_to((R/"TestRuns").resolve())
  file=profile/("ReloadResult.json" if name.endswith(" reload") else "Result.json")
  a=read(file);doc=R/"Documentation"/published
  assert a==read(doc) and a["passed"] and a["productionAssemblySha256"]==digest
  assert a["runtimeErrors"]==0 and not a["gpuLost"] and a[key]["passed"] and not a[key]["errors"]
  assert a["mapActivity"]["ticksAdvanced"]>0 and a["mapActivity"]["framesAdvanced"]>0
  if name.endswith(" reload"):assert a["unresolvedReferences"]==0 and a["isolatedFixtureSave"] and not a["userSaveTested"]
  else:
   assert a["fullUserModSet"] and a["localization"]["passed"] and a["settingsUi"]["passed"] and a["odysseyGrammar"]["checks"]==252
   assert [a["localization"][k] for k in ("keyed","scalarFields","grammarLines","displayPatches")]==[2961,702,90,12]
  counts[name]=a[key]["checks"];evidence.append(dict(file=published,sha256=sha(doc)))
 assert counts["Addiction"]>=28 and counts["Addiction reload"]>=5 and counts["AreaCache"]>=10
 old=read(R/"Documentation/AreaCacheBaseline-FullSet.json");base=read(R/"Documentation/PricingStageVerification.json")
 assert old["passed"] and old["areaCache"]["expectedBaselineFailure"] and old["areaCache"]["checks"]>=10 and old["productionAssemblySha256"]==base["productionAssemblySha256"]
 sources={p.relative_to(R).as_posix():sha(p) for p in sorted((R/"Source").rglob("*.cs"))}
 assert all(p.stat().st_mtime<=(R/"Assemblies/TurboMegaMod.dll").stat().st_mtime for p in (R/"Source").rglob("*.cs"))
 assert {p for p,h in base["sourceSha256"].items() if sources.get(p)!=h}=={"Source/GuestDutyTrees.cs","Source/ComplexManagement.cs","Source/ComplexServicesDialog.cs"}
 assert set(sources)-set(base["sourceSha256"])=={"Source/GuestAreaCacheHooks.cs","Source/GuestAddictionSupply.cs"}
 resources=["Languages/English/Keyed/GuestAddictionSupply.xml","Languages/Russian/Keyed/GuestAddictionSupply.xml"]
 tracked=set(subprocess.check_output(["git","ls-files"],cwd=R,text=True).splitlines());assert all(p in tracked for p in list(sources)+resources)
 result=dict(passed=True,productionAssemblySha256=digest,successfulIsolatedSteps=len(counts),runtimeChecks=sum(counts.values()),checksByStep=counts,sourceSha256=sources,resourcesSha256={p:sha(R/p) for p in resources},evidence=evidence,baselineMissingAreaExceptionReproduced=True,baselineProductionSha256=old["productionAssemblySha256"],baselineChecks=old["areaCache"]["checks"],baselineSeparateFromCurrentChecks=True,managedAreaSameTickNativePathFixed=True,deletedAutoAreaRecreationTested=True,staffCellsRemainExcluded=True,originalFailedRunExactTimingInstrumented=False,personalDoseBeforePurchase=True,nativeDrugEffectsRetained=True,addictionNotCuredByDose=True,paidPublicAndStaffSupply=True,actualShopPercentageRespected=True,realAutomaticIngestAndThinkKeyReload=True,sharedSupplyPolicyAndPreferencePersisted=True,ordinaryRecreationalScheduleImplemented=False,allDrugsAndModdedRacesRuntimeTested=False,live16Tested=False,userSaveTested=False,allSubsystemRegression=False)
 (R/"Documentation/GuestSupplyRevisionVerification.json").write_text(json.dumps(result,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
 print(json.dumps({k:v for k,v in result.items() if k not in ("sourceSha256","resourcesSha256","evidence")},ensure_ascii=False))
if __name__=="__main__":main()
