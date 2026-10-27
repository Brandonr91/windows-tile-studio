# Polishing phases

Established 2026-10-26 from the user's production-readiness and polishing lists.
All previously optional features are included in planned scope. Planned scope
is not implemented functionality or permission to enable startup automatically.
Complete and verify one reviewable phase at a time. This plan does not schedule
background work or promise dates. Preserve independent height pairs throughout.

| Phase | Scope | Completion gate | Status |
| --- | --- | --- | --- |
| 1. Everyday controls and identity | Settings organization, launcher reset/visibility, consistent visible branding, tray version/About. | Appearance save/cancel works, no unnecessary window reflow, existing resize/flyout tests pass. | Implemented and verified locally as `0.3.0-preview.1`; not published. |
| 2. Editing safeguards and recovery | Unsaved changes, reversible grid edits/reset confirmation, actionable errors, startup hotkey conflicts, corrupt settings and missing-manager recovery. | Isolated failure reproductions preserve original files/working shortcuts and provide usable recovery. | Planned; next. |
| 3. Window compatibility and responsiveness | App minimum sizes/fit feedback, unusual frames, elevated apps, corners, fast dragging, minimize/restore, app restarts, resize performance/resource cleanup. | Minimum-size and 3+ row cases pass; unrelated heights never move; real-app/timing limitations are explicit. | Planned. |
| 4. Displays and per-monitor profiles | Independent monitor layouts, mixed DPI/negative origins, docking/disconnects, work-area/taskbar changes, sleep/resume and launcher recovery. | Define monitor identity/schema migration and verify reconnects without disturbing unrelated windows. | Planned. |
| 5. Visual builder and portable layouts | Draggable dividers, live preview, undo/save/cancel, layout/hotkey export/import with validation/conflict handling. | Numeric editor remains usable; invalid imports cannot overwrite settings; adjacency rules hold. | Planned. |
| 6. Workspaces and optional startup | Saved assignments, opt-in restore/relaunch, durable app identity; opt-in launch at login with status/disable/removal. | No stale HWND identity, unrequested app launch or automatic startup registration. | Planned. |
| 7. Accessibility, visuals and support | Keyboard/screen readers/high contrast/scaling, focus/hover, typography/spacing/themes, target feedback, local privacy-conscious diagnostics, measured startup/idle performance. | Keyboard-only tasks and small displays work; reports exclude window contents/private titles by default; measurements support performance claims. | Planned. |
| 8. Production delivery | Consistent executable/package identity; install/update/rollback/uninstall and legacy recovery; Windows CI, reproducible builds, versioned signing/checksums/release notes, clean-account/external beta testing. | Exact packages pass install/use/update/remove/rollback; documentation matches release; original repository is preserved. | Planned. |

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

## Exact next work: phase 2

1. Inspect draft/save/cancel and constructor hotkey registration failure.
2. Add isolated reproductions for closing an edited draft, replacing a grid,
   resetting proportions and a reserved configured startup shortcut.
3. Implement discard/keep-editing and destructive-edit recovery deliberately;
   do not silently fall back or overwrite invalid settings on startup.
4. Verify save/registration rollback, run relevant geometry/hosted integration
   checks with disposable windows, then restore the daily controller.
5. Update context records and package the tested phase. Keep preview work
   distinct from a production release.

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
