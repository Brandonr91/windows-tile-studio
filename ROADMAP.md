# Roadmap and next work

## Current: beta/release validation, 2026-10-28

Local `0.3.0-beta.1` fixes reproduced lossy legacy settings downgrade through
strict Recovery and installed selection guards. Geometry/settings (198564),
compiled UI/native, full hosted integration, hosted General/accessibility,
installer lifecycle, actual public-source upgrade/guarded rollback and 30-cycle
compiled/hosted resize/dialog/resource samples pass within isolated test scope.
See HANDOFF for evidence/artifact provenance and docs/BETA-TESTING.md for the
campaign. A local kit tool collects only verified packages and test/recovery docs.

Next: fresh account/VM install/update/rollback/remove; representative personal
configurations and backups; actual login; physical mixed-DPI/docking/sleep;
screen reader and OS contrast/text-size; elevated/hung/unusual apps and competing
tilers; several days of daily use and external feedback. Signing/publisher,
remote CI and approved publication follow. No production push occurred and the
original repository stays untouched. The sections below retain milestone history.

Context established 2026-10-26 at `48b96b2`; updated 2026-10-28 for phase 7,
through phase-8 delivery preparation following `508783a`.
The existing [development roadmap](docs/ROADMAP.md) remains the authoritative
milestone history. This root entry adds current priorities and acceptance criteria
without replacing that history. [HANDOFF](HANDOFF.md) records the last stopping point.

The user has now requested phased polishing. The current sequence and all
previously optional features are captured in [polishing phases](docs/POLISH-PHASES.md).
Phases 1–6 are implemented locally within their recorded test scope. Phase 3's
[compatibility checkpoint](docs/COMPATIBILITY.md) documents remaining elevated,
hung-app and timing limits. Phase 4's [display checkpoint](docs/DISPLAYS.md) records
single-display native and simulated topology coverage, with physical multi-monitor
release gates outstanding. Phase 5's [visual/portable guide](docs/VISUAL-AND-PORTABLE.md)
records draft-only editing and validated import/export. Phase 6 adds explicit
[workspace restore and optional login startup](docs/WORKSPACES-AND-STARTUP.md).
Startup remains disabled unless chosen; restore/relaunch are never automatic.
Phase 7 adds keyboard divider editing, accessible selection/value feedback,
resizable/scrolled DPI-aware dialogs, system high-contrast colors and a local
allowlisted diagnostic preview/export. See [phase-7 scope and verification
limits](docs/ACCESSIBILITY-AND-SUPPORT.md). Phase 8 (production delivery) is next;
manual assistive-technology, OS contrast/text-size and hardware gates remain.
Phase 8 now adds local delivery tooling: versioned manifests/checksums, compiled
identity/icon, validated candidate activation, rollback/uninstall/data retention,
stable installed startup and a Windows CI definition. See
[release/recovery](docs/RELEASE-AND-RECOVERY.md). Local automation does not close
the [manual production gates](docs/RELEASE-CHECKLIST.md) or imply publication.
The user-requested General settings interlude adds startup/tray, bar lock and
visibility/reset, shared flyout shortcut and local support access in preview.6.1.
See [General settings](docs/GENERAL-SETTINGS.md). It does not advance the phase
counter by itself; the subsequent phase-7 implementation is recorded above.
The older priority table below is
retained as technical detail, not a competing phase order.

## Completed and verified within recorded test scope

- Phase 6: local capture and previewed restore with durable app path/class,
  optional titles, manual ambiguity/fallback and stale-process checks; separate
  confirmed argument-free launch; owned per-user startup shortcut controls with
  no automatic registration. Native disposable restore, temporary shortcut,
  UI/serialization and cold hidden-dialog visibility tests pass. Real login,
  production startup writes and physical display cases remain release gates.

- Phase 5: draggable visual proportions with per-gesture undo/redo/Escape,
  numeric fallback, settings draft isolation and portable catalog/hotkey transfer.
  XML bounds/schema/geometry/conflicts validate before staging; imports can add
  copies or replace explicitly, with a confirmed undo checkpoint. New pure and
  real-loop editor checks pass compiled and source-hosted. No matching Windows
  crash events or unexpected error dialogs were observed in sampled checks.

- User-requested close-to-tray: launcher ×/WM_CLOSE/minimize hides it and saves
  visibility off; tray Show Layouts bar restores it, Exit quits. Hidden bar stays
  hidden after flyout dismissal/restart. Real-loop UI and hosted controller
  close/persistence regressions pass; deployed with the bar hidden.

- Basic preview: six starter profiles, configurable layouts/weights/hotkeys,
  persistent nonactivating flyout and launcher, linked widths and independent
  row dividers, XML backup/recovery and legacy import.
- First polish: pair-only height resizing, preservation of actual flanking heights
  and width-only heights, native-style group correction, keyboard highlight and
  arrows/Tab/Shift+Tab/Enter, temporary hotkey cleanup and conflict rollback.
- Source delivery: PowerShell-hosted mode, optional compiled mode, versioned
  GitHub downloader, persistent data and offline archive support.
- Launcher placement update: clock-area default, drag-to-move with click/drag
  distinction, saved coordinates and work-area clamping. Geometry/settings
  checks and disposable-bar mouse input verified; phase 4 adds display-change
  handling, with physical multi-monitor validation still outstanding.
- Tray identity: Windows Tile Studio tooltip and generated six-tile icon.
  Optional per-folder legacy supersession closes the selected old controller,
  redirects its normal launch path, and restores original bytes when disabled.
  A fake-process/source fixture verifies enable, re-enable, redirect and restore.
- Geometry/configuration, disposable-window desktop and launcher test suites.
  A fresh public-source download and hosted validation passed in the previous
  development turn. See HANDOFF for revision/evidence limits.

## Outstanding work, proposed order

| Priority | Work | Exact first step and completion criteria |
| --- | --- | --- |
| 1 (core completed in phase 3) | Compatibility and resize verification | Minimum-size, 3/4-row, corners, minimize/restore, closure/recreation and isolated Chrome checks pass. Broader elevated/hung-app and native-timing coverage remains release validation; retain pair-only height and width-only-height regressions. |
| 2 (completed in phase 2) | Startup hotkey-conflict recovery | Recovery settings opens without exiting or replacing the saved file. Conflict/corrupt-file startup cases and editor recovery behavior pass isolated tests. |
| 3 (phase-7 core implemented) | Keyboard/accessibility coverage | Canvas keyboard editing/value/focus and constrained dialogs, plus flyout directions on uneven grids/wrapping/scrolling have focused checks. Native integration covers target/key cleanup/conflict fallback. Manual screen-reader announcements and actual OS contrast/text-size changes remain release gates. |
| 4 (initial implementation completed in phase 3) | App sizing feedback/constraints | Reported minimums constrain boundaries and rejected actual fits are not assigned. Validate remaining maximum/aspect/custom-frame and privilege cases without repeatedly forcing impossible bounds. |
| 5 (core completed in phases 5 and 7) | Visual proportion editor and portable layouts | Draft mouse/keyboard nudges, undo/redo/Escape and accepted/canceled edits covered. Portable schema/privacy/conflict/failure tests pass. Broader assistive-technology/hardware validation remains. |
| 6 (core implemented in phase 4) | Per-monitor state and display recovery | Independent sessions and optional XML records; pure topology and single-display native notifications tested. Physical mixed-DPI, work-area changes, docking/disconnect/reconnect and sleep/resume remain release validation. |
| 7 (core completed in phase 6) | Optional workspace restoration | Capture/preview/matching/manual choices, explicit fallback, native restore and separate launch tested with disposable apps. App document/session recreation and broader real-app matching remain limits. |
| 8 | Distribution/identity | Unify prototype naming carefully, add an application icon, reproducible versioned packaging and possibly signing. Preserve phase-6 explicit startup opt-in and owned removal. The generated tray icon already exists; a signed production distribution does not. |

No deadline, background automation or finished polished release is implied.
Follow the user's current priority before this suggested ordering.

## Known defects, limitations and open risks

| Status | Finding | Basis / next step |
| --- | --- | --- |
| Resolved, regression covered | Heights in flanking columns could follow/reset during another pair's resize. | Scoped axes and captured actual bounds; stale-ratio and native-style group regression tests now pass. Retain these tests. |
| Resolved, paint regression covered | Launcher could display a red cross and .NET "Parameter is not valid". | Premature DPI-font disposal reproduced in Label.OnPaint. Cache fonts until child disposal; 70-transition real-loop paint test passes compiled and source-hosted. Physical mixed-DPI crossing remains unverified. |
| Resolved, regression covered | An unavailable permanent shortcut previously terminated startup. | Phase 2 leaves tray/settings available, opens Recovery, preserves the file and installs no partial candidate set. Actual prior binding snapshots support failed-save rollback. |
| Implemented with remaining limits | App sizing restrictions and access can prevent an exact fit. | Phase 3 adds reported minimum constraints, verified placement and feedback; elevated apps, fully hung targets and arbitrary maximum/aspect constraints still need broader validation. |
| Implemented with hardware verification gap | Per-monitor state and display/resume recovery. | Phase 4 adds independent sessions, debounced fresh inventory and same-monitor verified reflow. Only one physical display available; real mixed-DPI/docking/sleep tests remain. |
| Implemented with explicit limits | Automatic recovery is geometry-only and can be ambiguous; saved workspaces are separate. | Phase 6 adds opt-in app/class/title matching and manual preview, without durable HWNDs or automatic relaunch. Shared hosts and app session/document identity remain unsupported. |
| Resolved, cold-start regression covered | First settings/workspace dialog could remain hidden after background startup with the bar off. | Explicit queued show after Load; compiled and hosted cold-start checks verify native visibility. |
| Resolved within regression scope | A long drag could accumulate multiple height pairs across opposite-edge samples. | Phase 3 pins the first observed height boundary for the whole gesture; three-row opposite-edge and all four-row divider-side cases pass. |
| Resolved, before/after regression covered | A final resize movement could be consumed during neighbor placement and release or misalign the source. | Prior-phase audit fixes bookkeeping to use the processed sample. Regression fails against pre-fix d8852bf and passes compiled/hosted; see docs/PHASE-AUDIT.md. |
| Verification gap | Native grouping, unusual frames, mixed DPI and competing tilers may behave differently from synthetic test timing. | Run controlled compatibility cases; do not equate the current suite with universal compatibility. |
| Operational limitation | Hosted source edits and compiled source edits do not update a running process automatically. | Restart the host; explicitly rebuild compiled mode. Do not test stale code. |

The earlier documentation-only task did not reproduce a new application defect.
The launcher update subsequently corrected Windows' default initial-position
override by explicitly using manual positioning.

## Exact next steps when development resumes

1. Read the five root context documents and inspect status/log/remotes/source.
   Verify a controller process before using runtime XML; the 2026-10-28 review
   found no matching compiled/source-hosted controller, despite retained XML.
2. Follow the user's latest request. The eight-phase implementation sequence now
   has local core implementations. Next is beta/release validation: fresh-account
   install/update/rollback/remove, actual login, hardware/assistive-technology and
   external feedback. Run the configured CI only after an authorized push; make
   a signing/publisher decision and review actual artifacts before publication.
3. Keep phase-7 keyboard/palette/viewport/report privacy checks and prior geometry,
   hosting/recovery/opt-in tests. Separate manual beta/hardware/accessibility
   gates from automated evidence; do not enable startup or capture real workspaces.
4. Run relevant checks from AGENTS/Build.ps1 against changed source; native
   integration requires an isolated test interval without a daily controller.
   Explicitly rebuild compiled mode or restart source hosting.
5. Update context/evidence and preserve data during any authorized deployment.
   Hardware, real login, elevated/hung-app validation, clean-account upgrade/
   removal, packaging/signing and publication remain release gates.

Diagnostic export is now implemented with an explicit aggregate allowlist and
preview/local save, separate from raw local log access. User-selectable themes/
text sizes remain unimplemented; standard controls and system high contrast are
the current appearance support. See HANDOFF for accepted reports and measurements.
