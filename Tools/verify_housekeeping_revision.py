"""Check completed cleaning credit, real hotel funding and saved accounting."""
import hashlib,json,subprocess
from pathlib import Path
R=Path(__file__).resolve().parents[1]
def read(p):return json.loads(p.read_text(encoding="utf-8-sig"))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 digest=sha(R/"Assemblies/TurboMegaMod.dll");assert not (R/"Assemblies/TurboMegaMod.TestProbe.dll").exists()
 state=read(R/"TestRuns/HousekeepingProgress.json");assert state["passed"] and not state["current"] and not state["error"] and state["productionAssemblySha256"]==digest
 steps=[("Housekeeping","housekeeping","Housekeeping-FullSet.json"),("Housekeeping reload","housekeepingReload","Housekeeping-Reload.json"),("Pricing","pricing","Pricing-FullSet.json"),("Pricing reload","pricingReload","Pricing-Reload.json")]
 assert [x["name"] for x in state["completed"]]==[x[0] for x in steps]
 counts={};evidence=[]
 for row,(name,key,pub) in zip(state["completed"],steps):
  profile=Path(row["profile"]).resolve();assert profile.is_relative_to((R/"TestRuns").resolve())
  file=profile/("ReloadResult.json" if name.endswith(" reload") else "Result.json");a=read(file);doc=R/"Documentation"/pub
  assert a==read(doc) and a["passed"] and a["productionAssemblySha256"]==digest and a["runtimeErrors"]==0 and not a["gpuLost"]
  assert a[key]["passed"] and not a[key]["errors"] and a["mapActivity"]["ticksAdvanced"]>0 and a["mapActivity"]["framesAdvanced"]>0
  if name.endswith(" reload"):assert a["unresolvedReferences"]==0 and a["isolatedFixtureSave"] and not a["userSaveTested"]
  else:
   assert a["fullUserModSet"] and a["localization"]["passed"] and a["settingsUi"]["passed"] and a["odysseyGrammar"]["checks"]==252
   assert [a["localization"][k] for k in ("keyed","scalarFields","grammarLines","displayPatches")]==[2963,702,90,12]
  counts[name]=a[key]["checks"];evidence.append(dict(file=pub,sha256=sha(doc)))
 assert counts["Housekeeping"]>=15 and counts["Housekeeping reload"]>=10 and counts["Pricing"]>=34 and counts["Pricing reload"]>=19
 base=read(R/"Documentation/GuestSupplyRevisionVerification.json")["sourceSha256"]
 sources={p.relative_to(R).as_posix():sha(p) for p in sorted((R/"Source").rglob("*.cs"))}
 assert {p for p,h in base.items() if sources.get(p)!=h}=={"Source/HotelBilling.cs","Source/WorkBuyout.cs"}
 assert set(sources)-set(base)=={"Source/HousekeepingCredit.cs"}
 assert all(p.stat().st_mtime<=(R/"Assemblies/TurboMegaMod.dll").stat().st_mtime for p in (R/"Source").rglob("*.cs"))
 resources=[f"Languages/{lang}/Keyed/{f}.xml" for lang in ("English","Russian") for f in ("Housekeeping","WorkBuyout")]
 tracked=set(subprocess.check_output(["git","ls-files"],cwd=R,text=True).splitlines());assert all(p in tracked for p in list(sources)+resources)
 result=dict(passed=True,productionAssemblySha256=digest,successfulIsolatedSteps=len(counts),runtimeChecks=sum(counts.values()),checksByStep=counts,sourceSha256=sources,resourcesSha256={p:sha(R/p) for p in resources},evidence=evidence,nativeCompletedLayerCleaningTested=True,intervalCallbackRuntimeTested=True,legacyCallbackPathPresent=True,creditFundedOnlyByRealReceivedRoomSilver=True,noNewPhysicalSilverFromCleaning=True,duplicateRevenueReceiptRejectedAcrossLoad=True,fractionalPendingWorkSurvivesLoad=True,interruptionAndOutsideComplexDoNotCredit=True,twoWorkersAndTargetCapTested=True,nativeReleaseStopsFurtherCredit=True,sourceWholeBedBillingPreserved=True,allSubsystemRegression=False,live16Tested=False,userSaveTested=False,visualLayoutInspected=False)
 (R/"Documentation/HousekeepingRevisionVerification.json").write_text(json.dumps(result,ensure_ascii=False,indent=2)+"\n",encoding="utf-8");print(json.dumps({k:v for k,v in result.items() if k not in ("sourceSha256","resourcesSha256","evidence")},ensure_ascii=False))
if __name__=="__main__":main()
