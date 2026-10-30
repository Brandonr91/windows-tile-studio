# Working on Windows Tile Studio

The application currently retains the `SixSlotSnap` executable and internal
class names. This repository is independent of the original six-slot project.

## Required session recovery and documentation maintenance

Before development in a new session or after context compaction:

1. Read this file, [HANDOFF.md](HANDOFF.md), [ARCHITECTURE.md](ARCHITECTURE.md),
   [DECISIONS.md](DECISIONS.md), and [ROADMAP.md](ROADMAP.md). Follow the roadmap
   link to the existing milestone history in `docs/ROADMAP.md`.
2. Inspect `git status --short --branch`, `git log -5 --oneline`, and
   `git remote -v`. Inspect relevant source, tests, and any uncommitted changes.
   Documentation is context, not proof that the checkout or running host matches it.
3. Establish which controller and data directory are active before desktop tests
   or deployment. Runtime XML is a snapshot; verify the process and command line.
4. Continue the user's current request. Do not implement a roadmap item merely
   because it appears in the handoff.

Keep these records synchronized in the same change as meaningful behavior,
architecture, requirements, verification, or delivery changes. Record what was
tested, against which source, and remaining limitations. Mark proposals and
inspection findings separately from reproduced defects. Update HANDOFF with
the stopping point and executable next steps; record consequential tradeoffs in
DECISIONS. Preserve existing authoritative documents and link rather than
silently replace them. Never claim publication from a local commit alone.

Document ownership: AGENTS records working rules; ARCHITECTURE describes
implemented mechanisms; DECISIONS preserves choices and their replacements;
root ROADMAP owns current priorities/defects and links milestone history;
HANDOFF starts with the latest dated checkpoint and exact resumption steps.
Older handoff sections are historical evidence, not current instructions or
proof of a running controller. Distinguish fresh checks from saved test results.
Documentation-only work must not change app behavior, user data, startup
registration or deployment merely to refresh context.

## Build and test

Run `./Build.ps1 -Test` on Windows. It compiles all `src/*.cs` files with the local
.NET Framework C# compiler and runs geometry tests without moving desktop windows.
Do not introduce external packages or replace this build path without a reason.

`Start-Hosted.cmd` runs the same C# source through Windows PowerShell 5.1
Add-Type, with no OutputAssembly or standalone application EXE. Keep hosted
mode STA and pass an explicit data directory to the application entry point.
Validate launcher changes with `tests/LauncherTests.ps1` in Windows PowerShell
5.1; `-Network` is an opt-in public GitHub source download without launch.

## Behavior to preserve

- Preserve the default six-slot, three-column/two-row layout. Additional saved
  profiles support 1–4 columns and 1–4 rows per column, up to 16 zones.
- Shared left/right boundaries connect neighboring columns in both rows.
- Each column has its own independent top/bottom divider. Never move all
  three horizontal dividers merely because they happen to be aligned.
- Scope height resizing to exactly the adjacent pair. Preserve actual starting
  heights elsewhere, including during width resizing and settling. Extra native
  group events must not transfer resize ownership to a flanking window.
- Advance resize bookkeeping only to the source rectangle used for that sample.
  A fresh source read after applying neighbors can silently consume movement
  that occurred during synchronous placement. Retain the neighbor-triggered
  concurrent-source regression in the integration suite.
- Preserve physical-pixel DPI handling and invisible-frame compensation.
- Set and restore physical-pixel DPI context on background geometry-probe
  threads too. Never combine virtualized GetWindowRect with physical DWM bounds.
- Treat reported app minimum sizes as advisory constraints and verify actual
  frames after placement. Reject failed fits without adding a false assignment.
  Keep size queries bounded and off the live resize UI loop; cancelled probe
  results must not affect a new session. Pin a height gesture to one boundary.
- Release closed windows with lifecycle events; restarting/recreating an app
  does not authorize automatic adoption or relaunch.
- Keep the visual selector nonactivating and retain clean hotkey cleanup.
- Default the persistent Layouts bar near the taskbar clock (above the primary
  taskbar's right end). Dragging the bar moves it without opening the selector;
  normal clicks open it. Preserve saved position and keep restored bounds visible.
- User close/minimize of the Layouts bar hides it to tray and persists visibility
  off; never resurrect it on flyout dismissal, display refresh or restart. Keep
  tray Show Layouts bar available; only explicit tray Exit/quit stops the manager.
- General settings owns startup/tray, bar controls, flyout-key access and support.
  Keep startup enable/remove immediate and separately confirmed, clearly distinct
  from draft Save/Cancel. Bar lock defaults off for older files; it prevents
  drag/position writes without blocking normal clicks or explicit position reset.
  Keep General and Hotkeys synchronized through the same shortcut draft value.
  See docs/GENERAL-SETTINGS.md; WorkspaceUi tests now cover General in both modes.
- Keep cached DPI fonts alive until launcher child controls are disposed. A
  replaced font may still be retained by label/native paint callbacks; disposing
  it immediately caused Label.OnPaint "Parameter is not valid". Run
  Build.ps1 -LauncherUi for paint/DPI changes and the PowerShell 5.1 STA hosted
  tests/LauncherUiTests.ps1 path too. Geometry checks alone do not cover painting.
- The flyout stays open until selection, outside click, Escape, or its toggle
  shortcut; switching foreground apps also dismisses it. Preserve the original
  target; never snap the flyout itself.
- Keyboard navigation uses temporary all-or-nothing registrations while open:
  arrows, Tab/Shift+Tab, Enter. Release them on every close/exit path and keep
  mouse selection working if registration conflicts.
- Failed hotkey changes must restore the previous registered shortcuts.
- A move-out releases a slot; a resize keeps neighboring edges joined.
- Keep independent monitor sessions and saved proportions. Never pull a moved
  window back across displays during reflow or resize. Disconnect clears HWND
  ownership, retaining preferences; reconnect is geometry recovery, not relaunch.
  Shared names/grids/hotkeys and per-monitor proportions must remain distinct.
  Merge current monitor snapshots when saving an older settings draft. Consult
  docs/DISPLAYS.md and do not claim physical mixed-DPI/hotplug coverage from tests.
- Keep the controller local: no network service or AI runtime dependency.
- Workspaces are opt-in and separate from layout drafts/portable export. Never
  persist HWND/PID identity or choose ambiguous windows by order. Revalidate live
  process instances, layouts and displays before restore; relaunch is a separate
  confirmed, argument-free local EXE action. Titles need explicit capture opt-in.
  Login startup remains off unless enabled through its explicit action. Preserve
  unrelated startup shortcuts and provide owned removal. Run WorkspaceUi and
  WorkspaceIntegration plus source-hosted/cold hidden-dialog checks for changes.
  See docs/WORKSPACES-AND-STARTUP.md. No real account startup writes in tests.
- Visual edits/imports remain draft-only until main settings Save. Preserve
  adjacent-pair height scope, per-gesture undo/redo/Escape and numeric fallback.
  Portable exports must exclude monitor IDs/HWNDs/appearance/runtime data. Validate
  bounded XML and all conflicts on copies before staging; preserve the old draft
  on failure. See docs/VISUAL-AND-PORTABLE.md and run -EditorUi plus its hosted test.
- Appearance-only and shortcut/name-only saves must not reflow managed app
  windows. Keep appearance edits draft-only until Save; launcher reset from
  the tray is immediate and must not reset layout proportions.
- Protect changed drafts on close; require confirmation for grid replacement,
  removal and live proportion reset. Keep grid undo local to the draft.
- Startup shortcut conflicts must leave tray/recovery settings usable without
  replacing the user's file or keeping partial candidate registrations. Failed
  saves restore actual prior bindings, including an empty recovery binding set.
- Keep standalone legacy restoration usable if manager source is missing;
  verify original/guard hashes before replacing any executable.
- Use the Windows Tile Studio tray name and owned six-tile icon. Dispose its
  native icon resources on exit. Legacy supersession is opt-in per folder:
  preserve the original EXE, stop only that exact selected executable, support
  hash-checked restoration, and never rewrite the original public repository.

## Verification

User scope update, 2026-10-29: use this existing installation; a fresh account/VM
is not required for this release. Do not reintroduce that waived gate or create
accounts/system features for it. Record broader-environment coverage as unverified.
CurrentInstallationTests.ps1 validates copies of real settings/backups and observes
the live compiled controller. -ExerciseStartup explicitly quits normally and
executes its existing owned shortcut, with direct-restart fallback. Never rewrite
startup, corrupt real files, uninstall the daily app or force logout for this test.
Its bin/current-installation-* snapshots contain private configuration/shortcut
bytes: keep them local and exclude them from packages/reports. Shortcut execution
does not prove actual Windows login; short observations do not prove multi-day use.

Phase 7: preserve visual-editor Page Up/Down selection, arrow/Shift nudges,
Ctrl+Z/Y, Tab escape, accessible value and pair-only draft geometry. Run
`-AccessibilityUi` and Windows PowerShell 5.1 STA tests/AccessibilityUiTests.ps1
for accessibility/palette/viewport/report changes. Do not change actual Windows
contrast/scaling or claim screen-reader verification from accessible properties.
Keep the main Settings pages selector and Hotkeys Change/Clear column headers
named. Verify their accessible objects, not just visible button text: blank
headers left the action cells named only by row number in the live provider tree.
Live keyboard acceptance must cancel drafts, preserve saved settings/startup and
distinguish accessible-tree checks from actual screen-reader speech.
DiagnosticReport must remain an allowlist of aggregate numeric/boolean/version
data; never copy logs, titles, paths, profile/monitor IDs, keys, HWNDs or PIDs into
it. Preview before local save; no upload/automatic report. PerformanceTests.ps1
requires an extracted source package and no daily controller. Document sample
limits. See docs/ACCESSIBILITY-AND-SUPPORT.md.

Add geometry regression coverage for meaningful layout changes. Geometry tests
do not prove native dragging works: report which desktop interactions were
actually checked. Use disposable windows for interactive tests. Do not rearrange
real apps, change system settings, enable startup, or launch a second controller
unless the user requests it. Prefer a small reviewable change.

`./Build.ps1 -Integration` is opt-in and requires the controller to be stopped.
It launches a separate controller with disposable windows and isolated settings.
It does not adopt existing apps. Use it for changes affecting targeting,
native resize events, hotkey cleanup, or the settings editor.

`./Build.ps1 -HostedIntegration` runs the same suite against the PowerShell
host. Use `-HostedSourceRoot` for a clean source-only controller fixture.
Keep installer cleanup bounded to its own fresh staging directory; never
delete or replace user data when switching source versions.

`./Build.ps1 -Compatibility` runs focused disposable 3/4-row, size-limit,
lifecycle and isolated Chrome-frame checks with the daily controller stopped.
It also tests a separate genuinely blocked app, maximum-size and fixed-aspect
fixtures. Prove the block with a timed sent message and OS hung detection; keep
the native event wait/rescue deadline in the disposable child only. Use absolute
fixture data paths. HostedIntegration now runs its compatibility subcase in hosted
mode; focused --compatibility-only accepts --hosted <Start-Hosted.ps1> too.
It uses a separate Chrome profile and must never move existing browser windows.
Performance samples are local observations, not universal latency/CPU promises.

## Repository hygiene

Beta validation: settings with unknown fields/attributes must enter Recovery and
preserve original bytes. Keep the bounded XML reader and autosave suppression.
Before selecting legacy installed code, probe a settings snapshot using its own
types; reject incompatible install/rollback/launch before pointer/data changes.
Do not auto-restore an older settings backup. The preserve-and-recover capability
in release/capabilities.json avoids running a redundant legacy probe for current
code. Test with PublicUpgradeTests against reviewed immutable public source;
direct old-EXE launches bypass this protection. Build -BetaSoak 30 exercises both
modes with disposable windows and requires the daily controller stopped. Its
bounded resource sample is not multi-day or physical-input/hardware coverage.
Use docs/BETA-TESTING.md and retain unverified manual gates.

Phase 8 release work: build via Build-Release.ps1 and release/package-files.txt;
keep release/version.json, source ProductVersion and compiled metadata aligned.
Use tests/ReleaseTests.ps1 for isolated install/update/rollback/uninstall and
tests/InstalledLaunchTests.ps1 only with the daily controller stopped. Never test
uninstall/purge on the real installation or account Startup folder. New installed
startup opt-ins follow the stable verified launcher; do not migrate an existing
startup shortcut silently. Preserve path/link bounds, candidate validation before
pointer activation, previous-pointer rollback on write failure, cache/compiled
hash checks and default data retention. Source ZIP repeatability is scoped to
identical bytes/provenance; legacy compiled binaries are not deterministic or
signed. Keep docs/RELEASE-CHECKLIST.md gates open until actually verified. The
workflow is configured locally; do not claim a GitHub CI pass before it runs.

Do not commit `bin/`, runtime logs, layout preferences, account details, or
machine-specific helper scripts. Document user-visible changes and limits.
Do not publish, upload a release, or enable automatic startup unless requested.

Beta.2 release-review checkpoint: docs/RELEASE-BETA-2.md owns frozen artifact
hashes/provenance. Do not silently overwrite reviewed ZIPs after docs/history
changes. Public cfdb2df matched local 48b96b2 modulo EOL; d056d17 reconciles both
histories with an unchanged application tree. Do not repeat an ours merge for
unknown future upstream changes. Fetch/recheck ancestry and diff before an
authorized push; force push is not needed for the reviewed public baseline.

Review branch/CI checkpoint: draft PR #1 is authorized on review/beta-2. Normal
Git credentials are configured; do not repeat sign-in unless authentication fails.
The connector can report push rights yet deny writes; use actual operation results.
Check CI against the exact current PR head after updates, not only an older run.
Remote jobs cover geometry/hosting/installer/package checks, not native desktop UI.
Workflow artifacts have checkout-specific provenance and are not replacements for
frozen release assets. The user subsequently authorized PR #1 merge and beta.2
publication on 2026-10-29; those actions are complete. Never repeat publication,
move v0.3.0-beta.2 or replace its frozen assets. Future fixes need a new version and
verified provenance; old beta validation does not imply production readiness.

Appearance follow-up: consult docs/APPEARANCE.md. Preserve System/Light/Dark, high-contrast priority, draft-only preview and Save/Cancel. Keep user-preference handlers marshalled to the form thread and unsubscribe on disposal; cache palette lookups outside paint. Retain accessible navigation providers and run compiled/hosted AccessibilityUi plus LauncherUi for changes. Never change the user's Windows theme to test this automatically.

Manager visuals: consult DESIGN.md before changing cards, navigation or previews. LayoutOverview is a passive draft preview; keep Edit visually and precise numeric editing reachable. Theme preview cards and the selector must edit the same draft and expose selected feedback. Run compiled and hosted AccessibilityUi/EditorUi/WorkspaceUi/LauncherUi for these surfaces; preserve small-viewport scrolling and native providers.

Build -StartupRecovery runs focused conflict/corrupt/future-settings native startup fixtures with isolated data and no daily controller. Freshly constructed normal/Recovery settings must close without an unsaved prompt; initialization notifications are not user edits. Actual edits must retain the existing discard protection.

Placement recovery: read docs/PLACEMENT-RECOVERY.md. Preserve owned job cancellation, one active plus one spare helper, current-user/peer/version/lease validation and explicit retry. Never replace cancellation with SWP_ASYNCWINDOWPOS, an abandoned in-process thread or killing a user app. Treat frames already committed before timeout separately from late helper movement. Run compiled/hosted PlacementRecovery, integration and workspace/startup checks after placement changes. Queued-command responsiveness is stronger evidence than a sent-message ping. Keep hosted worker mode source-only; confirm helper shutdown and preserve the real Startup shortcut.

STA wait regression: serialize complete geometry callbacks, post deferred replay and preserve the sampled source frame. Keep modal UI lifetimes and navigation callbacks outside a held geometry transaction; never mask reentry by merely dropping a broker request. Treat fresh/failed helper startup as unavailable and require explicit retry. Tests use atomic ready-file publication; helper ownership checks must exclude the framework compiler. Preserve helper-job cleanup in package launch and rollback paths.

Manager expansion (beta.6): consult docs/LIVE-MANAGER.md and docs/SETUP.md. Live snapshots stay in memory and separate from drafts/reports. Retry requires the displayed token, original window property/process generation and unchanged display/layout/empty-or-same slot; never auto-retry/relaunch. Refresh only while visible; preserve dropdown/focus. Embedded visual gestures remain pair-only, draft-only and one undo checkpoint per pointer gesture. Numeric fallback is Advanced. Setup uses manifest-only verified payloads; Start menu links target the stable launcher, preserve conflicts and obey ShouldProcess. Tests supply temporary MenuDirectory as well as StartupDirectory. Published beta.2 is immutable.

Beta.6 publication is complete. docs/RELEASE-BETA-6.md owns frozen source/tag/artifact evidence. Never overwrite those assets/tag or infer that a later documentation commit changes the released source. Current daily delivery is beta.6; verify actual ownership before native testing. Signing remains pending publisher credentials.

Automated native failure fixtures must opt into --quiet-notifications (compiled) or -QuietNotifications (hosted). Log every notification while bypassing only its popup delivery; assert zero notificationPopups and retained Recovery/placement outcomes. Never silence Windows globally, change real startup or suppress fatal startup failures. See docs/NOTIFICATIONS.md. Daily remains released beta.6 until an explicit deployment; local beta.7 is the notification/test candidate.
