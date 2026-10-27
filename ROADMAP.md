# Roadmap and next work

Updated 2026-10-26 after inspecting the application at `48b96b2`.
The existing [development roadmap](docs/ROADMAP.md) remains the authoritative
milestone history. This root entry adds current priorities and acceptance criteria
without replacing that history. [HANDOFF](HANDOFF.md) records the last stopping point.

## Completed and verified within recorded test scope

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
| 1 | Compatibility and resize verification | Add disposable windows with explicit minimum sizes and a 3-row column. Reproduce both sides of every height boundary, width-only changes, corners, minimize/restore and duplicate native events. Establish a failing test before changing behavior. Every height operation must preserve nonadjacent heights and width-only operations must preserve all heights. |
| 2 | Startup hotkey-conflict recovery | Inspect constructor registration failure and settings loading. Add an isolated conflicting-binding test; design an explicit recoverable settings route that preserves the user's file. Do not claim recovery until startup can open that route without losing old settings or leaking keys. |
| 3 | Keyboard/accessibility coverage | Test all four arrow directions, uneven row counts, layout cycling/wrap, constrained viewport scrolling, foreground-switch dismissal and cleanup. Current integration verifies Right/Enter and Tab/Shift+Tab, not every combination. Verify mouse fallback on conflicts remains usable. |
| 4 | App minimum-size feedback/negotiation | After priority 1 reproduces constraints, decide how to obtain/handle app minimums and rejected placements. Avoid repeatedly forcing impossible bounds; report fit limitations without disturbing unrelated windows. Generic 400/200 clamps are not app negotiation. |
| 5 | Visual proportion editor | Start with a draft preview with draggable boundaries and cancel/save semantics. Reuse the same normalized model and pair-only height rules; keep current numeric weight editor usable. |
| 6 | Per-monitor state and display recovery | Design monitor identity, data schema migration and assignment ownership first. Test negative origins, mixed DPI, work-area changes, disconnect/reconnect and sleep/resume before claiming support. |
| 7 | Optional workspace restoration | Define explicit app identity/relaunch policy and stale-window handling; do not serialize HWNDs as durable identities. Make adoption/relaunch opt-in. |
| 8 | Distribution/identity | Unify prototype naming carefully, add an application icon, reproducible versioned packaging and possibly signing. If startup is added, make it explicit opt-in with an equally usable removal path. |

No deadline, background automation or finished polished release is implied.
Follow the user's current priority before this suggested ordering.

## Known defects, limitations and open risks

| Status | Finding | Basis / next step |
| --- | --- | --- |
| Resolved, regression covered | Heights in flanking columns could follow/reset during another pair's resize. | Scoped axes and captured actual bounds; stale-ratio and native-style group regression tests now pass. Retain these tests. |
| Confirmed code behavior; recovery gap | An unavailable permanent shortcut during startup terminates initialization with a dialog. | Constructor throws on `HotkeyRegistry.Replace` failure. Runtime editing has rollback; startup has no recovery editor. Priority 2. |
| Implemented limitation | App minimum sizes can prevent an exact fit; elevated apps may reject placement. | Generic model clamps and Win32 failure logging only; no negotiated minimums or verified post-placement fit. |
| Implemented limitation | Only one active monitor/work area; no display-change or sleep/resume recovery. | `SetWork` clears assignment array; no per-monitor state. |
| Implemented limitation | Geometry-based recovery may ambiguously adopt overlapping windows; no app restart restoration. | `RecoverAssignments` enumeration and current HWND array only. |
| Inspection risk, not reproduced defect | A long drag could accumulate multiple adjacent height pairs if an app changes opposite edges across samples in a 3+ row column. | `Adjust` limits each sample to one pair; controller unions scopes. Add a targeted test before deciding whether boundary ownership must be pinned per gesture. |
| Verification gap | Native grouping, unusual frames, mixed DPI and competing tilers may behave differently from synthetic test timing. | Run controlled compatibility cases; do not equate the current suite with universal compatibility. |
| Operational limitation | Hosted source edits and compiled source edits do not update a running process automatically. | Restart the host; explicitly rebuild compiled mode. Do not test stale code. |

The earlier documentation-only task did not reproduce a new application defect.
The launcher update subsequently corrected Windows' default initial-position
override by explicitly using manual positioning.
