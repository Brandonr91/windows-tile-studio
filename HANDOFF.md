# Handoff

## Current checkpoint: phase 2

2026-10-26 following `c56b6a7`. Phase 2 is implemented locally as
`0.3.0-preview.2`: discard/keep-editing close protection; grid replacement/removal
confirmation and pre-save grid undo; live proportions reset confirmation;
actionable save errors retaining the draft; startup Recovery page for hotkey
conflicts and corrupt settings without replacing the file. Failed save rollback
uses actual registered binding snapshots, including an empty recovery set.

Legacy supersession now supplies standalone Restore-SixSlotSnap.cmd/.ps1 helpers
and actionable missing-source guidance. Setup supports enable/disable/enable
with identical original backup and Windows PowerShell 5.1 XML assignment.
Both local opted-in legacy folders were refreshed with the updated guard/helpers.

Validation: 186129 geometry/configuration checks pass, including blocked-save
byte preservation. Hosted six-window/editor suite passes; startup-conflict and
corrupt-file subcases launch the compiled controller with isolated data and
verify recovery UI, unchanged file and clean exit. Editor tests cover keep/discard,
confirmed/rejected grid replacement, undo and failed-save retention. Confirmation
answers are injected for deterministic editor tests; tray reset dialog was not
manually clicked. Supersession/standalone restore tests pass under Windows
PowerShell 5.1. Recovery screen rendered and inspected.

During development the new tests exposed a modal-close exit check and a test
reader file-share race; both were fixed and the suite rerun successfully.
Final source deployed to daily hosted delivery; runtime reports preview.2,
recoveryRequired=false and six assignments. Source fallback and settings snapshot
are under `work/phase-two-fallback`. Changes remain local until publication.
Next: phase 3 compatibility and responsiveness, using disposable minimum-size
and 3-row cases before changing resize behavior; see the phase plan.

## Current phase checkpoint

2026-10-26, following `d3a430f`. User requested phased polishing, including
all optional features from the production scope. See [phase plan](docs/POLISH-PHASES.md).
Phase 1 is implemented locally as `0.3.0-preview.1`: Appearance tab for bar
visibility/draft reset, immediate tray bar reset, About/version, consistent
visible names and no whole-grid reflow for appearance/name/hotkey-only saves.
The old top-of-screen checkbox also overlapped the preview; it is now removed
from Layouts. Phase 2 is editing safeguards and startup/failure recovery.

Verification: geometry/configuration PASS (186128 checks); hosted six-window
integration PASS, with new Appearance save and draft-cancel isolation checks.
Appearance screenshot rendered and visually inspected. Updated source deployed
to the separate daily hosted folder; preserve its settings. Source delivery
and context changes remain local until published. Next steps are in the phase
plan; do not treat later optional features as implemented or enable startup.

## Latest checkpoint: tray identity and legacy preference

2026-10-26, after launcher-placement commit `b3f941f`. User asked whether the
old tray label indicated a second controller, requested legacy supersession,
then requested a Windows Tile Studio tray entry/icon. Inspection found only the
hosted full manager; the old tooltip was a naming artifact.

Implemented: renamed its existing tray entry and added an owned blue/cyan six-tile
icon. Added `Set-LegacySupersession.ps1`, `tools/LegacyRedirect.cs`, and isolated
`tests/SupersessionTests.ps1`. The option is enabled locally for both
`outputs/SixSlotSnap` and `outputs/six-slot-snap/bin`, pointing to the daily
`windows-tile-studio-hosted` source/data. Original EXEs remain as
`SixSlotSnap.legacy-original.exe` in each folder. Old normal launch paths now
start the full manager, including when it was stopped, and do not create a
second controller when it is running. Untouched copied binaries elsewhere are
outside this option's scope. The original public repository is unchanged.

Tests: geometry/configuration PASS (186126 checks); supersession fake-process
test PASS for close, backup preservation, re-enable, redirected start and byte-for-byte
restoration. Hosted desktop suite passed on retry after one focus-sensitive
flyout-open failure; no flyout behavior was changed to hide that timing failure.
The tray icon was rendered and inspected as six blue/cyan tiles. Local launches
through both old paths retained the same full-manager PID and six assignments.

Disable supersession before using the previous-controller recovery helper:
run the setup script with each intended `-LegacyDirectory` and `-Disable`, then
exit the full manager and start the restored original. Never run a name-wide
process kill. Current changes are local until separately published; do not infer
GitHub main matches this checkpoint. Next: gather user feedback on the tray entry;
continue unrelated roadmap work only when requested.

Last update: 2026-10-26 (America/Chicago), following context checkpoint
`1aac7f3` on `feature/polish-launchers`. Latest request: move the persistent
Layouts bar near the taskbar clock and make it movable.

## Latest completed change

The bar defaults just above the primary taskbar's right end. Left-drag moves it
without opening the flyout; normal click still opens it. New optional schema-1
settings fields preserve its coordinates; restored/dragged bounds are clamped
to the screen work area. Manual StartPosition fixes Windows overriding the
initial location. This is launcher placement, not full display-change recovery.

`Build.ps1 -Test` passed 186126 checks, including launcher position cloning/XML
round-trip and negative-origin clamping. `Build.ps1 -HostedIntegration` passed
the existing six-window suite. After the final manual-position correction,
geometry tests passed again. A disposable launcher using the production mouse
handlers received actual drag and click input: drag callbacks fired without
an open callback, then a normal click fired the open callback. Its test-only
taskbar visibility differed from the production nonactivating tool window.

The updated source is installed in the daily hosted delivery; all six slots
recovered. User data was preserved, with a local fallback under
`work/launcher-position-fallback`. This change and context docs are local only;
public main still predates the context checkpoint unless independently updated.
Next: gather user feedback on placement; only continue roadmap work when asked.

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

The latest runtime snapshot showed a PowerShell-hosted controller with all six
zones occupied in profile `six-grid`, flyout closed. The launcher change paused
and restarted the daily controller for isolated testing and deployment. Process
IDs and HWNDs are transient;
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

The earlier context-only task reviewed existing evidence. New launcher-change
verification is recorded above; no new broad compatibility claim is made.

## Exact next steps

1. Run `git status --short --branch`, `git log -5 --oneline`, `git remote -v`.
   Review uncommitted work before editing. If publication is requested, fetch
   remote history and compare content; reconcile without overwriting either
   the original repository or independent public commits.
2. Resume the user's latest request. With no new feature priority, use the
   proposed order in root ROADMAP: first add a disposable-window reproduction
   for minimum-size and 3-row resize ownership. The launcher request is complete;
   do not automatically begin unrelated functionality.
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
