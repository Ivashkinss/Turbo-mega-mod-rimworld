# Turbo mega mod

All project changes belong inside this mod directory. Do not modify original mods, game files, user settings, or saves as part of routine development.

The user wants completed, checked changes pushed after each revision to:
https://github.com/Ivashkinss/Turbo-mega-mod-rimworld.git

Do not commit logs, saves, user configuration, credentials, or temporary game-test data. Preserve existing work in the remote repository. Run checks relevant to changed behavior before committing and pushing.

The display name is Turbo mega mod. Preserve the technical packageId colonyproject.integrated to keep the existing enabled-mod configuration valid.

The user authorizes removing original mod folders after their components have been fully integrated and checked. Keep external dependencies and originals that are not yet integrated. Preserve a local ignored backup and the relative order of all remaining active mods when removing the retired originals from the active list. Do not treat this authorization as permission to remove unrelated mods.

Gemini delegation uses Delegation/QUEUE.json as the sole explicit task registry. After writing a complete, reviewable TASK.md, add its id/taskFile/outputDir/expectedOutputs as pending for the user's polling worker; mark tasks already handed over manually as assigned to avoid duplicate execution. Gemini owns only Tools/Build/GeminiWorkerState/<id>.json execution acknowledgements plus each task's permitted output. Codex owns the queue and reviews results independently. Do not assume Antigravity remains active or has executed a pending task until its status/artifacts are observed. Continue telling the user the exact TASK.md path.
