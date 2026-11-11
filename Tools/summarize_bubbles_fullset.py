"""Validate the full-set rendering result against the precise known external errors."""
import json
import re
import sys
from pathlib import Path

from lxml import etree as ET

PROJECT = Path(__file__).resolve().parents[1]
KNOWN = [
    "Field injection: VEF: VEF.AestheticScaling.CachedPawnDataExtensions.GetCachePrePatched: initializer GetDefaultCache must be exactly one static method on VEF.AestheticScaling.CachedPawnDataExtensions returning VEF.AestheticScaling.CachedPawnData and taking the target or nothing (0 found).",
    "Bad string pass when reading rule [tradeAdj_fem] [tradeNoun_fem]",
    "Failed to resolve text. Trying again with English. Exception: System.ArgumentNullException: Value cannot be null. (Parameter 'key')",
]


def main():
    source = Path(sys.argv[1])
    result = json.loads(source.read_text(encoding="utf-8-sig"))
    errors = [re.sub(r"^\d+:\d+:\d+\s+ERROR ", "", line) for line in result["errors"]]
    assert errors == KNOWN, errors
    assert not result["exited"] and result["stoppedByTestTimer"] and not result["gpuLost"]
    assert result["mapActivity"]["framesAdvanced"] > 0 and result["mapActivity"]["ticksAdvanced"] > 0
    assert result["localization"]["passed"]
    active = ET.parse(str(source.parent / "Config/ModsConfig.xml")).xpath("/ModsConfigData/activeMods/li/text()")
    assert "colonyproject.integrated" in active
    assert not {"jaxe.bubbles", "sarg.smartspeed", "dhultgren.smarterconstruction"}.intersection(active)
    for folder in ("Interaction Bubbles (Речевые пузыри)", "Smart Speed (4 скорость)",
                   "Smarter Construction (Умное строительство)"):
        assert not (PROJECT.parent / folder).exists(), folder
    report = {"gameVersion": (PROJECT.parent.parent / "Version.txt").read_text(encoding="utf-8").strip(),
              "profile": "full user mod list after removing the three integrated originals",
              "allThreeOriginalFoldersAbsent": True, "activeMods": active,
              "observedProcessSeconds": result["observedSeconds"], "stoppedByTestTimer": True,
              "mapActivity": result["mapActivity"], "localization": result["localization"],
              "gpuLost": False, "knownExternalRuntimeErrors": errors, "unexpectedRuntimeErrors": 0,
              "visualUiVerified": False, "passedWithKnownExternalIssues": True}
    (PROJECT / "Documentation/Bubbles-FullSet.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False))


if __name__ == "__main__":
    main()
