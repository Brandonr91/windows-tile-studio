# Roadmap and next work

Context established 2026-10-26 at `48b96b2`; updated 2026-10-27 for phase 6.
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
Phase 7 (accessibility, visual polish and support) is next.
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
  checks and disposable-bar mouse input verified; broader display-change handling
  remains planned.
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
| 3 | Keyboard/accessibility coverage | Test all four arrow directions, uneven row counts, layout cycling/wrap, constrained viewport scrolling, foreground-switch dismissal and cleanup. Current integration verifies Right/Enter and Tab/Shift+Tab, not every combination. Verify mouse fallback on conflicts remains usable. |
| 4 (initial implementation completed in phase 3) | App sizing feedback/constraints | Reported minimums constrain boundaries and rejected actual fits are not assigned. Validate remaining maximum/aspect/custom-frame and privilege cases without repeatedly forcing impossible bounds. |
| 5 (core completed in phase 5) | Visual proportion editor and portable layouts | Draft drag/undo/redo/Escape and accepted/canceled edits pass. Portable schema/privacy/conflict/failure tests pass. Keyboard divider movement and broader accessibility remain phase 7. |
| 6 (core implemented in phase 4) | Per-monitor state and display recovery | Independent sessions and optional XML records; pure topology and single-display native notifications tested. Physical mixed-DPI, work-area changes, docking/disconnect/reconnect and sleep/resume remain release validation. |
| 7 (core completed in phase 6) | Optional workspace restoration | Capture/preview/matching/manual choices, explicit fallback, native restore and separate launch tested with disposable apps. App document/session recreation and broader real-app matching remain limits. |
| 8 | Distribution/identity | Unify prototype naming carefully, add an application icon, reproducible versioned packaging and possibly signing. If startup is added, make it explicit opt-in with an equally usable removal path. |

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
