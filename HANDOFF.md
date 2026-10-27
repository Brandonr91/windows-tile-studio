# Handoff

## Current checkpoint: phase 6

2026-10-27, following `3f6eee1`. Local `0.3.0-preview.6` implements explicit
workspace capture/preview/restore, a separate missing-app launch action, and
owned per-user login startup shortcut controls. Startup is OFF in the daily
installation, and no real workspace was captured/restored by the agent. Read
[workspace/startup guide](docs/WORKSPACES-AND-STARTUP.md) before changing behavior.
No public push, service, scheduled task or registry Run entry was added.

New files: Workspaces.cs (DTO/store/live identity/planning), WorkspaceForm.cs
(immediate user controls), StartupRegistration.cs (mode/data command and owned
Startup shortcut backend). Eleven src/*.cs files are compiled dynamically.
Local workspaces.xml is bounded to 1 MB/depth 32, backed up atomically, and
separate from settings/portable layout transfer. Captures use app EXE/class and
optional titles, never saved HWND/PID. Preview requires choices for ambiguity
and explicit disconnected-display fallback. Restore revalidates process start/
identity/current display/layout, preserves compatible unselected slots and
reports per-window failures. Relaunch is a separately confirmed local EXE with
zero arguments; it does not recreate app documents/URLs/sessions. Shared hosts
and inaccessible apps are skipped. Workspace/startup actions are immediate,
not settings drafts; placement hotkeys focus the open workspace dialog.

Startup uses Windows Tile Studio.lnk in the current user's Startup folder. A
dedicated shortcut avoids long hosted Run-command limits. No entry is created
on installation/open/status inspection. Enable explicitly preserves current
compiled or hosted source/data paths, Disable removes only an owned entry, and
unrelated/unreadable entries are preserved. Real startup was not enabled/tested
through login/logout; native shortcut tests use a fresh temporary folder only.
Start-Hosted.ps1 now forwards --source-root; Main/IPC adds --workspaces/0x8006.

Verification:

- Pure geometry/configuration/matching/store/startup checks PASS: 198558.
- Real-loop WorkspaceUiTests PASS: capture/preview/injected selected restore,
  launch guard, confirmation cancel/enable/disable with memory storage, painting,
  named selection and visible/footer bounds. Native Windows shortcut roundtrip,
  hosted arguments and removal tested only in an isolated temp folder.
- WorkspaceNativeTests PASS: two external disposable windows captured, saved/
  loaded, previewed and restored to actual saved visible frames; stale process
  reference refused; compatible unselected assignments retained. Explicit launch
  runs only an inert probe in a path with spaces/apostrophe, with zero arguments.
- Full compiled and hosted regressions PASS during this phase, including all
  prior resize/editor/hidden launcher/font/recovery/compatibility cases and
  opening/closing the real workspace UI without writing workspace files.
- Subsequent targeted native/UI tests cover docked footer/column repair,
  compatible-slot preservation and shared-host exclusion. Native readiness file
  publication had a test-only File.Exists/write race; atomic publication fixed it.
- Live inspection reproduced a hidden first workspace dialog after background
  startup with the bar off (native handle existed, WS_VISIBLE false). Queued
  explicit ShowManagedDialog after Load fixes both workspace and settings. Cold
  first-dialog visibility tests PASS for compiled and source-hosted starts. The
  corrected installed workspace dialog was then visually inspected and closed.
- Windows PowerShell 5.1 STA WorkspaceUiTests.ps1 and LauncherTests.ps1 PASS,
  covering in-memory source compilation and updated launcher/install handling.
- Window inventories and relevant Application crash events were sampled: no
  unexpected Windows/.NET error windows or matching crash events observed.
  This is not continuous notification-area monitoring. Physical mixed-DPI,
  docking/sleep, virtual desktops, elevated/hung apps and real login remain gates.

Current controller: compiled repo bin/SixSlotSnap.exe with repo data/, preview.6,
six recovered assignments, hidden launcher, no recovery warning. Settings hash
matches pre-phase; the real Startup shortcut stayed absent/unchanged. Hosted
src AND Start-Hosted.ps1 updated, separate data retained, no second controller.
Verify live process/runtime before tests. Fallback source/settings/startup
snapshot: work/phase-six-fallback; prior code is also Git 3f6eee1.

Reports outside Git use outputs/windows-tile-studio-phase-six- prefix:
full-tests.txt, final-tests.txt, workspace-final.txt, native-final.txt,
visibility-tests.txt, hosted-visibility.txt, hosted-ui.txt, pure-final.txt,
launcher-tests.txt, deployment.json and windows-errors.json. Full suites preceded
the later targeted fixes; the targeted/cold checks validate final affected paths.
Rendered workspace/startup PNGs are under bin/phase-six/workspace-ui-data and
copied to outputs for review. Source packages refreshed after the local commit.
Next planned phase is 7 (accessibility/visuals/support); follow latest user intent.

## Current checkpoint: phase 5

2026-10-27, following `4ca3c9b`. User requested phase 5 and attention to Windows
errors near the clock. Local `0.3.0-preview.5` is implemented and deployed.
No public push, startup registration, saved app workspace or automatic relaunch.
Read [visual/portable guide](docs/VISUAL-AND-PORTABLE.md) before changing this flow.

New `VisualEditor.cs`: cloned draft session with one adjacent boundary per drag,
4% clipping, capped 50-gesture undo/redo and Escape; painted canvas/modal from
Layouts → Edit visually. Accepted geometry enters settings' existing grid history;
main Save remains the live apply point. Numeric editor retained. New
`PortableLayouts.cs`: schema-1 DTO, 256 KB bounded/DTD-prohibited strict XML,
atomic export and copy-first validation. Share layouts stages copied or explicitly
replaced catalogs, regenerates IDs/names, optionally imports shortcuts, rejects
conflicts before mutation and offers a confirmed pre-import checkpoint restore.
Local monitor/appearance/runtime data are excluded; OS key availability is still
checked on main Save. Example: examples/starter-layouts.tilelayouts.xml.

Verification:

- `Build.ps1 -Test -EditorUi -OutputDirectory bin/phase-five`: PASS, 198525 pure
  checks plus real-message-loop canvas pointer, Undo/Redo/Escape, painting,
  accepted settings draft/grid undo and import/failure/undo/discard coverage.
- Full compiled and hosted controller regressions PASS on the phase-5 functional
  implementation. Final preview.5 `-Test -LauncherUi -EditorUi -HostedIntegration`
  PASS after the version/status-label changes, including close-to-tray/no revival,
  font painting, concurrent-source resize, Recovery and compatibility subcases.
- Windows PowerShell 5.1 STA `tests/EditorUiTests.ps1`: PASS against final source,
  in-memory source hosting without an app EXE. `tests/LauncherTests.ps1`: PASS
  including updated eight-source compilation and install/data/archive validation.
- Final XML-depth preflight (max 32) was rechecked with pure/editor tests in
  both modes after the controller regressions; final pure count is 198525.
  Accepted report: outputs/windows-tile-studio-phase-five-editor-final.txt.
- Rendered visual/share pages inspected. Real mouse drag/Undo checked in a
  disposable preview through computer use: only center pair moved and restored.
- Windows window inventories sampled during testing/startup showed no unexpected
  error dialogs. Application events 1000/1001/1026 for the app/test process names
  since phase development began: no matching crash events. These are sampled
  checks, not continuous notification-area monitoring. Expected fit/hotkey warnings
  remain deliberate compatibility/recovery test cases.

`Build.ps1` adds optional OutputDirectory and EditorUi. Eight src/*.cs files are
loaded dynamically in both modes; no external dependency added. Initial normal
build was blocked by the running EXE; development used bin/phase-five until the
daily controller was paused for desktop tests. No stale running EXE was tested.

Current runtime is compiled repo bin/SixSlotSnap.exe with repo data/. It reports
preview.5, six recovered assignments, launcher hidden, recoveryRequired=false.
Settings hash matches the pre-phase snapshot. Hosted delivery src updated too;
its separate data is preserved and no second controller started. Fallback source
and both settings snapshots: work/phase-five-fallback (prior source also Git
4ca3c9b). Verify the actual process/runtime before any later desktop tests.

Reports outside Git: outputs/windows-tile-studio-phase-five-editor-tests.txt,
phase-five-full-tests.txt, phase-five-final-tests.txt, phase-five-hosted-editor.txt,
phase-five-launcher-tests.txt and phase-five-windows-errors.json (all share the
windows-tile-studio- filename prefix). Rendered PNGs under outputs with
phase-five-visual-editor/share-layouts names. Source packages refreshed after
the local commit. Physical mixed-DPI/docking/sleep, privileged/hung apps, keyboard
divider dragging and broader accessibility remain explicit gaps. Next planned
phase is 6 (opt-in workspaces/startup); follow the user's latest request first.

## Latest request: close the launcher to tray

2026-10-27, following `1f6302d`. User rejects the always-visible bar and requests
close-to-tray behavior. The bar now has an explicit × button. User WM_CLOSE and
minimize hide it and notify the controller; ShowLauncherTab=false is saved.
Flyout dismissal respects this preference, so it cannot resurrect the bar.
Tray Show/Hide Layouts bar explicitly restores/hides it; tray Exit still quits.
An open Appearance draft/checkbox is synchronized with this immediate action.
Shutdown/recreation uses Dispose, which is not canceled by the close handler.
Runtime XML reports launcherEnabled/Visible/Handle for verification.

Tests: real-loop LauncherUiTests now checks ×, WM_CLOSE and minimize hiding
without disposal, restoration, DPI painting and final disposal. Hosted integration
starts with a visible launcher, sends actual WM_CLOSE, verifies persisted false
and a live controller, exercises snapping/hotkeys, and checks flyout dismissal
does not revive the bar. See outputs/windows-tile-studio-close-to-tray-final-tests.txt
and the source-hosted launcher UI report for final results. Geometry checks remain
186297. Preserve prior physical/manual coverage limitations.

Final checks PASS: Build.ps1 -Test -LauncherUi -HostedIntegration and Windows
PowerShell 5.1 STA tests/LauncherUiTests.ps1. A development test exposed that raw
WM_CLOSE can bypass the UserClosing-only managed filter; WndProc now intercepts
the close request directly. State diagnostics do not recreate/access disposed
launcher handles. The real-controller close/persistence and no-resurrection checks
pass. Settings snapshots are under work/close-to-tray-before; ShowLauncherTab=false
is applied in both local data directories. Live compiled runtime confirms hidden
launcher, running controller, six assignments, no recovery warning.

Deployment keeps the user's compiled repo/data mode, deliberately sets only
ShowLauncherTab=false so the unwanted bar is hidden immediately, preserves its
position and other settings, and recovers six assignments. Hosted source updated
too without launching a second controller. Tray icon/hotkey remain the access
routes. No public push/new phase. Check live process/runtime before further work.

## Latest repeat phase 1–4 check

2026-10-27, starting at `53d589f`. Application source unchanged. PASS:
186297 geometry/configuration checks; compiled and source-hosted real-loop
launcher paint tests (70 DPI transitions each); full compiled integration;
final full hosted integration with recovery and compatibility subcases; legacy
supersession/restoration; source installer/hosting tests; archive source/content
checks. Both archives matched all six app sources and excluded runtime data/binaries.

Keep the failed attempts visible: first compiled run failed selector persistence
without recording a dismissal reason. A diagnostic compiled rerun passed. The
next hosted attempt could not acquire foreground focus before selector testing.
Test-only changes now capture foreign foreground HWNDs, mouse/Escape state and
refresh count on persistence failures, and retry the disposable target's focus
setup for up to two seconds. Final hosted run passed. The initial selector
failure's cause remains unconfirmed; do not label it fixed or definitively blame
user input. No application behavior was changed or new phase implemented.

Current compiled controller restored using repo `data/`; six assignments recovered,
recoveryRequired=false. Keep current data and verify actual process/runtime before
desktop work. The saved launcher position changed during this recheck interval;
only LauncherPositionSet/X/Y differ from the initial snapshot. Retain this latest
position rather than overwriting it. Snapshot: `work/phase-recheck-20261027-145329`.
Reports: `outputs/windows-tile-studio-phase-recheck-*`; accepted desktop runs are
`phase-recheck-compiled.txt` and `phase-recheck-hosted-final.txt`. Initial failures
remain in `phase-recheck-core.txt` and `phase-recheck-hosted.txt`. No public push.
Physical multi-monitor/docking/sleep, elevated/hung apps and remaining manual UI
coverage remain open, as described in docs/PHASE-AUDIT.md. Preserve this distinction
when answering status questions or resuming development.

## Latest fix: launcher paint exception

2026-10-27, following `1a3c193`, user reported the .NET "Parameter is not valid"
dialog and a red-cross launcher. A disposable launcher with a real message loop
reproduced the exception in System.Drawing.Graphics.DrawString / Label.OnPaint
against the pre-fix binary. DPI font replacement disposed a font still retained
by label/native rendering. The previous audit did not test actual launcher paint;
its prior pass must not be treated as coverage of this lifetime bug.

LauncherTab now caches fonts by DPI, explicitly updates child font references,
reuses cached fonts, and disposes them only after base/child disposal. No global
exception suppression was added. `Build.ps1 -LauncherUi` runs the new real-loop
test. Windows PowerShell 5.1 -STA `tests/LauncherUiTests.ps1` compiles the same
source/test in memory, without an app EXE, and can export a rendered image.
Both passed 70 synthetic 100/125/150/200% transitions, correct label sizes,
initial/repeated painting, bitmap rendering, bounded GDI count and disposal.
`Build.ps1 -Test -LauncherUi -HostedIntegration` passed: 186297 pure checks plus
the full hosted resize/settings/recovery/compatibility suite. Screenshot of the
fixed label was rendered and visually inspected. Physical mixed-DPI hardware
and live mouse drag remain separate coverage limits.

IMPORTANT runtime change: when inspected, the old hosted controller had stopped
and the user had started the compiled repo app using repo `data/`. The fix was
rebuilt and this SAME compiled mode/data was restarted; all six windows recovered,
recoveryRequired=false. The hosted delivery source was also updated but no second
controller started. Check repo `data/runtime-state.xml` first, verify process/path,
and do not assume the old hosted runtime XML is live. Keep both data directories.
Settings snapshots: `work/launcher-font-fallback/{compiled,hosted}-settings.xml`.
Reports: `outputs/windows-tile-studio-launcher-paint-before.txt`,
`windows-tile-studio-launcher-font-regression.txt`,
`windows-tile-studio-launcher-font-hosted-ui.txt`; rendered label:
`outputs/windows-tile-studio-launcher-fixed.png`. Before-fix stack is accepted
reproduction; final tests are the accepted verification. Packages refreshed after
the local fix commit. No push/new phase. Continue the user's latest priority.

## Latest verification: prior-phase audit

2026-10-27, user requested a check of all prior phases. Starting from `d8852bf`,
the first compiled suite passed but hosted repetitions exposed an intermittent
lost final resize sample. `UpdateDrag` read a fresh rectangle after applying
neighbors and advanced bookkeeping over movement that had never been modeled.
It now advances only to the rectangle used for that sample. The new synchronous
neighbor-triggered source-movement regression fails against isolated pre-fix
`d8852bf` and passes after correction in both launch modes. Release diagnostics
record frame coordinates in the existing local log.

Final `Build.ps1 -Test -Integration -HostedIntegration`: PASS, 186297 pure checks
plus both native suites and their compatibility/recovery subcases. Added direct
all-direction navigation/clamp/layout-wrap checks pass. PowerShell 5.1 legacy
supersession and source-hosting/installer suites PASS. See the full
[phase audit](docs/PHASE-AUDIT.md) for each function's actual coverage and limits.
An experimental supplemental in-process harness was inconclusive/stopped and
removed; its launcher checks are not accepted verification. Live launcher drag,
About/reset confirmation, physical multi-monitor/docking/sleep, elevated/hung
apps and every physical event timing remain manual/beta coverage.

Daily hosted source updated with the correction; restarted with all six original
assignments recovered and no recovery warning. Preserve data. Pre-audit settings
and runtime snapshot: `work/phase-audit-20261027-134223`. Reports are under
`outputs/windows-tile-studio-all-phases-*.txt`, outside Git; full accepted run is
`windows-tile-studio-all-phases-corrected-regression.txt`, and the baseline
expected failure is `windows-tile-studio-all-phases-before-fix.txt`. Source packages
refreshed after the audit commit. No public push or new phase implemented.
Resume the user's latest request after reading actual Git/runtime state; phase 5
remains the next planned development phase, not part of this audit.

## Current checkpoint: phase 4

2026-10-27, following `9785a92`. Phase 4 core is implemented locally as
`0.3.0-preview.4`. Read [display support/verification](docs/DISPLAYS.md) and
the phase plan alongside the actual repository before continuing. Next requested
phase is phase 5: draft visual builder and portable layout/hotkey import/export.
No public push or production release was performed in this phase.

Implemented: `DisplayState.cs` native inventory and pure `MonitorWorkspace`;
optional schema-1 monitor records; independent selected profiles, proportions
and HWND arrays; shared names/grids/hotkeys; same-monitor ownership checks;
debounced display/work-area/resume refresh; disconnect releases transient HWNDs,
retaining preferences; reconnect can recover matching visible geometry. Reflow
verifies eligible same-monitor frames and releases unsupported assignments.
No cross-screen return, app relaunch, startup registration or OS display change.
Launcher creation/DPI changes/topology refresh scale and clamp its visible bounds.
Settings banner identifies its monitor, retains drafts on disconnect, merges
current monitor snapshots and preserves untouched concurrent geometry.

Verification on final source:

- `Build.ps1 -Test`: PASS, 186297 geometry/configuration checks, including
  simulated two-monitor negative-origin/different-size geometry, independent
  profiles/assignments/heights, restart, disconnect/reconnect, work-area flags,
  clone/XML migration, concurrent draft merge, profile removal/shared shape.
- `Build.ps1 -HostedIntegration`: PASS, native synthetic display/work-area/resume
  notifications preserve six disposable frames/assignments on unchanged hardware;
  existing full resize/flyout/editor/hotkey/recovery and compiled compatibility
  subcases pass. Banner layout has a non-overlap regression check.
- Focused `Build.ps1 -Compatibility`: PASS, including minimums, pair-only 3/4-row
  sides, lifecycle, delayed query and isolated Chrome. Final full run: 20 corner
  updates 1480 ms including deliberate pumps/settling, 125 ms controller CPU;
  idle one-second sample 0 ms; no growing GDI count. These are short local samples.
- Windows PowerShell 5.1 `tests/LauncherTests.ps1`: PASS, source-only hosting,
  installed-source validation, repeat installs/data preservation and unsafe ZIP
  rejection. No network download test was needed for this unpublished source.

During development, a notification test needed synchronous delivery for Windows'
pointer-bearing WM_SETTINGCHANGE; PostMessage was rejected. A settings title
suffix broke an exact-title recovery lookup, so display context now lives in a
banner. A later compatibility run exposed resize timing failure while device
enumeration was on the drag loop. Affinity checks now use cached native monitor
handles there; inventory/target selection resolves persistent IDs outside that
loop. Focused and full suites passed after the correction.

Available hardware remains one physical 5120 x 2160 display at 125%. No physical
second-monitor/mixed-DPI movement, docking/unplug, taskbar/scale change or actual
sleep/wake cycle was tested. Pure topology and synthetic notifications do not
close those production gates. Identity can change across ports/drivers; fallback
GDI keys are weaker. No record-removal UI or saved app workspace exists. Older
controllers may discard new monitor fields on save; use the fallback settings
snapshot for rollback. Phase-3 elevated/hung-app limits still apply.

Final source is installed in `outputs/windows-tile-studio-hosted/src`; daily
runtime reports preview.4, recoveryRequired=false, one connected monitor and all
six existing assignments. Verify its current PID/command line from runtime XML
before desktop work; these values are transient. Preserve daily data. Previous
source/settings snapshot: `work/phase-four-fallback`. Reports outside Git:
`outputs/windows-tile-studio-phase-four-integration-results.txt`,
`outputs/windows-tile-studio-phase-four-compatibility-results.txt`,
`outputs/windows-tile-studio-phase-four-launcher-results.txt`.
Source packages refreshed after the local commit; original six-slot repository
and legacy redirect configuration are unchanged.

Next steps: inspect status/log/remotes and source; follow phase 5 in
`docs/POLISH-PHASES.md` when requested. Begin with a draft-only visual divider
editor, numeric fallback and undo/cancel/save tests, then validate portable
imports without exporting monitor IDs/HWNDs/launcher location. Use isolated
desktop windows for behavior changes, never another controller alongside daily.
Physical display tests remain a separate explicit release checklist.

## Previous checkpoint: phase 3

2026-10-27, following `11f217a`. Phase 3 core is implemented locally as
`0.3.0-preview.3`. The original user workflow, pair-only heights, source-only
host and legacy preference are preserved. Next is phase 4 display/per-monitor
state, not a production release. See [compatibility evidence](docs/COMPATIBILITY.md)
and the phase plan before continuing after compaction or a new session.

Implemented: reported minimum constraints in transient model arrays; verified
visible frames before assignment; failed-fit feedback/best-effort restore;
background minimum probes with explicit thread DPI context, timeout/cancellation;
height-boundary ownership pinned even while probes are pending; limit no-op
restoration; closed-window event cleanup; explicit reconnect after restore/restart.
Runtime XML now reports resizing/source and the last placement issue.

Reproduced failures included an oversized app falsely marked snapped and closed
windows left in runtime assignments. Development also exposed worker DPI drift
and tests relying on fixed native-event timing. Tests now acknowledge source
ownership/completion; synthetic grouped-neighbor duplicates remain raw events.
No temporary DEBUG logging remains in source.

Final checks: `Build.ps1 -Test` PASS (186177); `Build.ps1 -HostedIntegration`
PASS, including phases 1–2 and compiled compatibility subcases; focused
`Build.ps1 -Compatibility` PASS twice with slow response, app minimums, at-limit
drag, 3/4-row dividers, corners, borderless/tool frames, minimize/maximize,
close/recreate, and isolated Chrome. Latest full-run corner sample: 20 updates,
1487 ms including deliberate pumping/settling, 47 ms controller CPU; one-second
idle sample 16 ms CPU; no growing GDI-object count. Short samples have finite
accounting resolution and are not universal performance guarantees.

Elevated apps and completely hung targets were not manually validated.
SendMessageTimeout bounds sizing queries; SetWindowPos/DWM still have no overall
hard deadline. Other frame/aspect/max constraints and native mouse timing need
broader beta coverage. Do not expand the tested matrix into an all-app claim.

Updated source is installed in the daily hosted folder; preserve its `data/`.
The prior source/settings fallback is under `work/phase-three-fallback`.
Reports are `outputs/windows-tile-studio-phase-three-test-results.txt` and
`outputs/windows-tile-studio-phase-three-compatibility-results.txt` outside Git.
Changes remain local until separately published; original repository untouched.

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
