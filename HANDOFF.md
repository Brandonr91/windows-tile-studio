# Handoff

Last context inspection: 2026-10-26 (America/Chicago). Application baseline:
local `48b96b2` on `feature/polish-launchers`. This task establishes persistent
context only; it does not change application behavior or resume feature development.

## Read before continuing

Read [AGENTS](AGENTS.md), [ARCHITECTURE](ARCHITECTURE.md),
[DECISIONS](DECISIONS.md), [ROADMAP](ROADMAP.md) and its linked milestone history.
Then inspect actual Git state and relevant source/tests. These records are a
dated checkpoint, not a substitute for the checkout or current user instructions.

## Repository and delivery checkpoint

- Intended remote: `https://github.com/Brandonr91/windows-tile-studio.git`.
  The original `Brandonr91/six-slot-snap` must remain separate and untouched.
- Application changes in local history: `a714b49` hosted/source launchers,
  `18d2156` pair-only height fix, `99fc839` keyboard polish,
  `48b96b2` documentation clarification. Worktree was clean before this task.
- Read-only remote check during this task returned public main
  `cfdb2df728466b1256383cf9b8417b21a486a1ca`.
  Publication used GitHub's signed-in web editor because Git/connector write
  access was unavailable. Public and local commit histories therefore differ.
  The previous turn cloned public main and compared normalized text for every
  tracked local file with no mismatches. Do not force-push the local history.
- This context update is local until separately published; do not infer that
  new documentation is already on public main. Inspect status/log to determine
  whether this documentation checkpoint has since been committed.

## Local environment checkpoint (not portable configuration)

The development checkout is `outputs/windows-tile-studio` under this Codex task
workspace. `outputs/windows-tile-studio-hosted` is the separate daily source-only
delivery, with its own `data/`. Editing the checkout does not update that copy.
There are older `six-slot-snap` folders and verification clones: do not mistake
them for the development repository or push from them.

The last runtime snapshot showed a PowerShell-hosted controller with all six
zones occupied in profile `six-grid`, flyout closed. This inspection did not
stop, restart, move, or resize those apps. Process IDs and HWNDs are transient;
re-read the runtime XML, verify the live process/command line, and confirm its
source/data location before interacting. Preserve daily `data/settings.xml`.

## Verified evidence and limits

Previous development-turn results, re-read during this inspection:

| Check | Recorded outcome / scope |
| --- | --- |
| `Build.ps1 -Test` on local and fresh public source | PASS, 186123 geometry/configuration checks. No desktop movement. |
| Public-source `Build.ps1 -HostedIntegration` | PASS: six disposable windows, linked widths, each row boundary from both sides, stale flanking-height preservation, native-style group correction, target preservation, flyout click, actual Right/Enter and Tab/Shift+Tab, key release/conflict rollback, settings editor save. |
| Windows PowerShell 5.1 `tests/LauncherTests.ps1` | PASS: source loading/serialization, no standalone app EXE, quoting, repeat installs, user-data preservation, unsafe ZIP paths/aliases/reserved names, older-source fallback, unrelated-folder rejection. |
| Fresh GitHub Hosted install with `-NoLaunch` | Retrieved public `cfdb2df...`; installed source `-Action Validate` returned empty assembly location and six profiles. |
| Hosted delivery/package | Six daily assignments recovered; package contained source/launchers, no app EXE/DLL or user settings. |

Workspace evidence includes `outputs/windows-tile-studio-polish-test-results.txt`,
`outputs/windows-tile-studio-launcher-test-results.txt`, and public verification
clone `work/polish-public-verification`. These files are outside this repository
and may not exist in another session/clone. Re-run relevant tests when code
changes; do not rely only on a saved PASS file or stale integration-error file.
Latest keyboard coverage does not prove all arrows/layout shapes, all native
mouse timing, app minimums, privilege levels or multi-monitor behavior.

The context-only task reviewed code and existing evidence; it did not rerun
desktop tests or make new compatibility claims.

## Exact next steps

1. Run `git status --short --branch`, `git log -5 --oneline`, `git remote -v`.
   Review uncommitted work before editing. If publication is requested, fetch
   remote history and compare content; reconcile without overwriting either
   the original repository or independent public commits.
2. Resume the user's latest request. With no new feature priority, use the
   proposed order in root ROADMAP: first add a disposable-window reproduction
   for minimum-size and 3-row resize ownership. Do not change behavior in this
   context-documentation task.
3. For code changes, run `./Build.ps1 -Test`. It does not move desktop windows.
   For resize/flyout/hotkey changes, arrange a controlled pause of the daily
   controller and run `./Build.ps1 -HostedIntegration` with isolated data;
   `-HostedSourceRoot <source-only-folder>` can verify a clean hosted fixture.
   Never launch an integration controller alongside the daily one.
4. For launcher changes, use Windows PowerShell 5.1:

   ```powershell
   & "$env:WINDIR\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -STA -ExecutionPolicy Bypass -File .\tests\LauncherTests.ps1
   ```

   Add `-Network` only when testing the currently public download is relevant.
   Validate source independently in a temporary data directory with
   `Start-Hosted.ps1 -Action Validate` under the same PowerShell 5.1/STA host.
5. Before deploying, preserve the user's data, compare intended source files,
   explicitly rebuild compiled mode or restart hosted mode, and verify the
   running process, hotkey and recovered slots. Update only the intended daily
   delivery; keep a fallback. Publication and startup are separate actions.
6. Update these context records with meaningful changes, verification and the
   next stopping point. Update README/launching notes or milestone history
   when their user-facing claims change. Keep proposed work labeled as planned.
