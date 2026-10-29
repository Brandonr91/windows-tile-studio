# Polishing phases

Established 2026-10-26 from the user's production-readiness and polishing lists.
All previously optional features are included in planned scope. Planned scope
is not implemented functionality or permission to enable startup automatically.
Complete and verify one reviewable phase at a time. This plan does not schedule
background work or promise dates. Preserve independent height pairs throughout.

2026-10-27 [prior-phase audit](PHASE-AUDIT.md): one lost-sample resize race
corrected with a before/after reproduction; both compiled and hosted full suites,
legacy restoration and source-launcher checks pass. Hardware/manual UI coverage
gaps remain explicit; this does not implement phase 5.

| Phase | Scope | Completion gate | Status |
| --- | --- | --- | --- |
| 1. Everyday controls and identity | Settings organization, launcher reset/visibility, consistent visible branding, tray version/About. | Appearance save/cancel works, no unnecessary window reflow, existing resize/flyout tests pass. | Implemented and verified locally as `0.3.0-preview.1`; not published. |
| 2. Editing safeguards and recovery | Unsaved changes, reversible grid edits/reset confirmation, actionable errors, startup hotkey conflicts, corrupt settings and missing-manager recovery. | Isolated failure reproductions preserve original files/working shortcuts and provide usable recovery. | Implemented and verified locally as `0.3.0-preview.2`; not published. |
| 3. Window compatibility and responsiveness | App minimum sizes/fit feedback, unusual frames, elevated apps, corners, fast dragging, minimize/restore, app restarts, resize performance/resource cleanup. | Minimum-size and 3+ row cases pass; unrelated heights never move; real-app/timing limitations are explicit. | Core implemented and representative matrix verified locally as `0.3.0-preview.3`; elevated/hung-app limits remain explicit. |
| 4. Displays and per-monitor profiles | Independent monitor layouts, mixed DPI/negative origins, docking/disconnects, work-area/taskbar changes, sleep/resume and launcher recovery. | Define monitor identity/schema migration and verify reconnects without disturbing unrelated windows. | Core implemented locally as `0.3.0-preview.4`; simulated topology and single-display notifications verified. Physical multi-monitor/docking/sleep release gates remain. |
| 5. Visual builder and portable layouts | Draggable dividers, live preview, undo/save/cancel, layout/hotkey export/import with validation/conflict handling. | Numeric editor remains usable; invalid imports cannot overwrite settings; adjacency rules hold. | Implemented locally as `0.3.0-preview.5`; pure, real-loop compiled/source-hosted editor and controller regressions pass within documented scope. |
| 6. Workspaces and optional startup | Saved assignments, opt-in restore/relaunch, durable app identity; opt-in launch at login with status/disable/removal. | No stale HWND identity, unrequested app launch or automatic startup registration. | Implemented locally as `0.3.0-preview.6`; pure/UI/native restore, temporary startup shortcut and cold-start checks pass. No real startup was enabled; actual login and broader hardware/app cases remain release gates. |
| 7. Accessibility, visuals and support | Keyboard/screen readers/high contrast/scaling, focus/hover, typography/spacing/themes, target feedback, local privacy-conscious diagnostics, measured startup/idle performance. | Keyboard-only tasks and small displays work; reports exclude window contents/private titles by default; measurements support performance claims. | Local preview.7 core implemented within documented tests: divider keyboard editing/value/focus, scrolled DPI-aware dialogs, system contrast and aggregate report export. Manual screen readers, OS text-size/contrast and physical hardware gates remain; separate theme/text-size controls are not implemented. |
| 8. Production delivery | Consistent executable/package identity; install/update/rollback/uninstall and legacy recovery; Windows CI, reproducible builds, versioned signing/checksums/release notes, clean-account/external beta testing. | Exact packages pass install/use/update/remove/rollback; documentation matches release; original repository is preserved. | Local preview.8 tooling implemented: manifests/checksums, source repeatability, product/icon, validated install, rollback/uninstall and stable startup. CI is configured, not yet run remotely; signing, actual clean-account/hardware/login/accessibility/external-beta gates remain. |

## Phase 1 result

- Layouts, Hotkeys and Appearance tabs. The launcher checkbox no longer
  overlaps the preview or says it lives at the screen top.
- Appearance show/hide and reset-to-clock-area are draft changes until Save;
  Cancel discards them. Tray reset is a separate immediate launcher action.
- Appearance/name/hotkey-only saves do not reflow the whole window grid.
  Geometry comparison tolerates tiny numeric-editor round-trip differences.
- Visible titles/messages use Windows Tile Studio; internal class, mutex,
  receiver and EXE names retain compatibility. About and settings identify
  this local preview as `0.3.0-preview.1`.
- Geometry/configuration PASS: 186128 checks. Hosted six-window suite PASS,
  including draft isolation and Appearance save. Appearance screen rendered
  and visually inspected. Broader scaling/accessibility remains phase 7.

## Phase 2 result

- Closing a changed draft asks whether to discard or keep editing, including
  when quitting the controller. Save errors keep the draft open with recovery
  guidance. Grid replacement/removal and live proportion reset require
  confirmation; Undo grid change restores earlier draft grids before saving.
- Startup shortcut conflicts keep the controller/tray alive and open a Recovery
  settings page with no partial candidate registrations or file replacement.
  Corrupt files also open Recovery with defaults for review; autosave remains
  suppressed until explicit successful Save. Existing files are backed up on
  replacement. Cancel preserves the original file.
- Failed file saves restore a snapshot of actual prior bindings, including the
  empty startup-recovery set, rather than assuming saved shortcuts were active.
- Legacy opt-in creates standalone Restore-SixSlotSnap.cmd/.ps1 helpers beside
  the old launch path. Hash-checked restoration works without manager source.
  Enable/disable/enable safely reuses a byte-identical original backup.
- PASS: 186129 geometry/configuration checks, including blocked-write byte
  preservation; hosted six-window suite plus editor safeguards and isolated
  compiled startup-conflict/corrupt-file cases; supersession recovery tests in
  Windows PowerShell 5.1. Recovery UI rendered and visually inspected. Tests
  use injected confirmation answers for deterministic editor branches; the live
  tray reset confirmation was implemented but not manually clicked in this phase.

## Phase 2 validation sequence (completed)

1. Inspect draft/save/cancel and constructor hotkey registration failure.
2. Add isolated reproductions for closing an edited draft, replacing a grid,
   resetting proportions and a reserved configured startup shortcut.
3. Implement discard/keep-editing and destructive-edit recovery deliberately;
   do not silently fall back or overwrite invalid settings on startup.
4. Verify save/registration rollback, run relevant geometry/hosted integration
   checks with disposable windows, then restore the daily controller.
5. Update context records and package the tested phase. Keep preview work
   distinct from a production release.

## Phase 3 result

Reported minimum constraints, post-placement fit verification, background DPI-aware
probes, pinned height gestures, limit feedback and closed-window cleanup are
implemented. Geometry/configuration: 186177 checks PASS. Full hosted regression
and focused compatibility suites PASS, including 3/4-row layouts, a delayed
query, borderless/tool frames and isolated Chrome. See the detailed
[compatibility checkpoint](COMPATIBILITY.md) for measurements and limits.
Elevated apps and completely hung targets remain unverified; this is not a
universal application-compatibility or production-release claim.

## Phase 3 validation sequence (completed for recorded cases)

Start with disposable windows that impose minimum sizes and a 3-row column.
Exercise both sides of each divider, width-only changes, corners, rapid native
notifications, minimize/restore and app restarts. Reproduce failures before
changing negotiation or resize ownership; preserve nonadjacent heights and
measure responsiveness. Multi-monitor support belongs to phase 4.

## Phase 4 result

Independent monitor sessions, optional schema-1 monitor records, per-monitor
profile/proportion selection, same-monitor ownership checks, display/work-area
and power notification handling, launcher DPI/clamping and display-scoped settings
draft merges are implemented. Disconnect retains saved geometry and releases
HWNDs; reconnect uses optional geometry recovery, never automatic app launch or
cross-screen return. Inventory enumeration is outside the drag-frame loop.

Pure tests cover negative origins, differently sized physical-coordinate displays,
switch/restart/disconnect/reconnect, XML migration, shape/removal fallback and
concurrent draft preservation. The hosted suite checks native display/work-area
and synthetic resume messages without changing Windows settings. Existing
resizing/recovery/compatibility and source-launcher suites pass within their scope.
Only one physical display is connected; mixed-DPI movement, physical docking,
taskbar changes and actual sleep/wake remain release gates. See
[display checkpoint](DISPLAYS.md).

## Phase 5 result

Visual editing is a nested draft, with adjacent-pair dividers, a 4% floor,
per-gesture undo/redo/Escape and numeric fallback. Share layouts exports a bounded
versioned XML DTO and stages validated copies/replacements with optional keys
and a confirmed undo checkpoint. IDs are regenerated, private machine/runtime
fields excluded, and OS shortcut conflicts checked on main Save. Pure checks:
198525 PASS. Real-loop editor tests pass compiled and source-hosted, including
painting, pointer gestures, cancel/undo and invalid import preservation. The
center-divider real mouse/Undo check passed. Full compiled and hosted controller
regressions, launcher paint/close-to-tray and source installer checks pass. No
unexpected Windows/.NET dialogs or relevant crash events were observed in sampled
checks. See [visual/portable guide](VISUAL-AND-PORTABLE.md) for exact limits.

## Phase 6 result

Local workspaces capture assigned app identities and geometry with optional
titles; previews require manual ambiguity choices and explicit disconnected-screen
fallback. Restore revalidates live instances, preserves compatible unselected
slots, verifies app frames and never launches apps. Relaunch is a separate
confirmed EXE-only/no-arguments action. Startup has explicit status/enable/remove
controls using an owned per-user shortcut; tests touch only isolated storage.
Saved workspaces are separate from local settings and portable layout exports.
Pure checks: 198558 PASS. Real-loop UI, native disposable restore, inert launch
probe, source hosting, shortcut roundtrip and cold hidden-start dialogs pass.
Full compiled/hosted regressions pass within their recorded scope; targeted
tests cover the later layout/visibility and compatible-slot fixes. See
[guide](WORKSPACES-AND-STARTUP.md). Startup is still off in the daily installation.

## General settings interlude after phase 6

Preview.6.1 adds tray General settings with Startup & tray, Layouts bar, Flyout
shortcut and Support. Startup moved out of Workspaces and remains separately
confirmed/immediate. Bar visibility/reset/lock and the shared flyout key stay
draft-only. Support opens local folder/log; diagnostic export/themes remain
phase 7. See [General settings](GENERAL-SETTINGS.md). This user-requested
interlude does not mark phase 7 complete; phases 1–6 remain the last completed set.

## Phase 7 result

Keyboard divider selection/nudges/undo/redo reuse the draft history and preserve
adjacent pairs. Focus/selection/status and accessible value feedback are explicit.
Resizable DPI-aware dialogs fit work areas; settings/workspace inputs scroll.
Custom panels use system colors for high contrast. Workspace actions reflect
available selections. General Support previews/saves a local aggregate report
without reading logs/workspaces or exposing identities/content. Focused checks
cover real canvas Right/Tab plus deterministic keys, constrained 800×600 forms,
injected contrast, flyout uneven directions/wrap/scroll and report privacy/save.
See [phase-7 guide](ACCESSIBILITY-AND-SUPPORT.md) and HANDOFF for verification,
measurements and the remaining manual assistive-technology/OS/hardware gates.

The original phase-7 validation brief is retained below as context:

Start with a keyboard/screen-reader/high-contrast and small-display audit of
settings, visual editor, workspace selection and tray controls. Make focus,
disabled/error/loading states and text/target feedback consistent, retaining
numeric editing and hidden-to-tray behavior. Improve layout/scaling from actual
reproductions, not broad theme replacement. Define a local diagnostic export
that omits titles, app/session content, raw monitor IDs and user paths by default;
make inclusion explicit. Measure packaged startup/idle/resize costs before claims.
Retain cold hidden-dialog, font lifetime, pair-only height, draft/import and
workspace/startup opt-in regressions. Hardware/login/elevated-app beta gates remain.

## Exact next work: phase 8

This phase's local tooling is now implemented. See
[release/recovery](RELEASE-AND-RECOVERY.md), [gate checklist](RELEASE-CHECKLIST.md)
and the latest HANDOFF for verification and artifacts. The brief below is retained
as scope/history. Next work is beta/release validation, not an invented phase 9.

Audit install/update/rollback/removal, artifact identity and existing CI. Establish
a reproducible source/binary manifest and checksums, then validate clean-account
flows, legacy restoration and versioned release notes. Preserve separate original
repository/history. Prepare the actual tested package before publication; do not
enable startup, sign/upload or push a release without the user's instruction.
Carry manual accessibility/hardware/login gates into the beta checklist.

## Before pushing a production release

- Review exact changes and relevant errors, naming, layout/accessibility and
  target feedback. Document unresolved limitations.
- Test the packaged source/binaries: geometry, desktop, hosting, installer and
  legacy restoration. Do not test a stale EXE instead of changed source.
- Measure startup, idle CPU/memory and resize responsiveness; verify native
  icon/hotkey/event-hook cleanup and resolve regressions.
- Update screenshots, quick start, troubleshooting, upgrade/uninstall/recovery,
  known limits, changelog/version and persistent project context.
- Exclude settings/logs/window identifiers/backups/temp files; inspect archives
  and the correct successor remote. Do not overwrite the original repository.
- Verify clean-account installation, update, rollback and removal; collect beta
  feedback from other configurations.
- Publish a tagged versioned package, checksums and signing where available.
  Preview source pushes may occur earlier; they are not passed production releases.
  Reconcile the independent public/local histories without force-pushing.
