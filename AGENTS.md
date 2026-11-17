# Turbo mega mod

All project changes belong inside this mod directory. Do not modify original mods, game files, user settings, or saves as part of routine development.

Remote repository: https://github.com/Ivashkinss/Turbo-mega-mod-rimworld.git
Current user decision (18.11.2026): commit locally after accepted revisions. Standing permission: Claude pushes main to origin automatically after each major update — a stage or feature accepted by a real in-game run, or a significant rules/architecture change — without asking again. Small doc or queue edits ride along with the next major push. Executors (Gemini, Codex) do not push unless their TASK.md says so. Before pushing, check that the branch is not behind origin (no force push) and that no logs, saves, TestRuns/ or Tools/Build/ files are included. Last push: 18.11.2026, main at 43624b6.

Do not commit logs, saves, user configuration, credentials, or temporary game-test data. Preserve existing work in the remote repository. Run checks relevant to changed behavior before committing.

The display name is Turbo mega mod. Preserve the technical packageId colonyproject.integrated to keep the existing enabled-mod configuration valid.

The user authorizes removing original mod folders after their components have been fully integrated and checked. Keep external dependencies and originals that are not yet integrated. Preserve a local ignored backup and the relative order of all remaining active mods when removing the retired originals from the active list. Do not treat this authorization as permission to remove unrelated mods.

## Multi-agent work (since 18.11.2026)

Three agents work on this project. Gemini in Antigravity has a very large limit and does all volume work: implementation, integration into root, builds, game tests, logs and docs; it polls the queue. Claude (root, has a terminal, small limit) owns the queue, writes short tasks and reviews selectively. Codex (small limit, started manually) does only rare acceptance of high-risk areas and arbitration. Read `Delegation/COORDINATION.md` before doing anything — it defines roles, write permissions and hard rules, and supersedes older notes saying Codex owns the queue.

`Delegation/QUEUE.json` is the sole task registry; only Claude edits it. Each task has `assignee` (gemini|codex) and `kind` (implement|run|accept). Executors write acknowledgements only to `Tools/Build/GeminiWorkerState/<id>.json` or `Tools/Build/CodexWorkerState/<id>.json` plus each task's permitted output. Worker instructions: `Delegation/GEMINI-WORKER.md`, `Delegation/CODEX-WORKER.md`.
