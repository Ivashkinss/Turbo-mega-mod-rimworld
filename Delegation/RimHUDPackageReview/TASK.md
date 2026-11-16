# RimHUD: package review
Read only Tools/Build/RimHUDCandidate (official Jaxe-Dev/RimHUD clone) and, if present, Tools/Build/RimHUDRelease. Network permitted only official GitHub/Steam sources if needed.
Write only Tools/Build/GeminiRimHUDPackageReview/ and your worker state. Do not edit production, Git, game or settings.
Deliver REPORT.md (at most 70 lines): exact upstream commit/version/packageId, distribution/license statements with paths and literal brief excerpts (explicitly flag if no license), runtime folders/dependencies, assumptions tied to original packageId/Mod instance or root paths, Russian translation availability. Look for 1.7 compatibility assumptions; no speculative compatibility conclusion.
Deliver FILES.json: runtime file list, sizes and SHA256 of the candidate; exclude source/build intermediates. If clone lacks DLL, record that; do not manufacture it. This is a bounded mechanical review, not native testing.
