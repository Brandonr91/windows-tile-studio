# Architecture

Context established 2026-10-26 from base application `48b96b2`; updated
2026-10-27 for local phase-4 preview `0.3.0-preview.4`. Earlier milestones and
decisions remain recorded in the handoff and decision log.
This document describes implemented behavior. Future work is in [ROADMAP](ROADMAP.md).
Existing [project notes](docs/PROJECT-NOTES.md) retain the origin and lessons learned.

## Scope and execution

Windows Tile Studio is a local C#/Windows Forms controller using Win32 window
APIs. It places and links real app windows; it does not modify Windows' built-in
Snap layout catalog, replace the taskbar, or create virtual monitors. The original
`Brandonr91/six-slot-snap` repository is a separate preserved predecessor.

`Build.ps1` compiles all six `src/*.cs` files using the installed .NET Framework
compiler, with framework assemblies and no external packages. Output remains
`bin/SixSlotSnap.exe`. `Start.cmd` calls `Start.ps1` for this compiled mode.
`Start.ps1` only builds if the EXE is missing: after source changes explicitly
rebuild rather than assume a restart recompiles it.

`Start-Hosted.cmd` selects Windows PowerShell 5.1 Desktop in STA mode.
`Start-Hosted.ps1` normally launches a hidden child host, uses `Add-Type` without
`OutputAssembly`, and invokes the same `SixSlotSnap.Main`. There is no standalone
app EXE in hosted mode; Windows PowerShell and runtime/compiler components still
execute. Loaded types require a fresh host to pick up edited source.

## Source map

| File | Implemented responsibility |
| --- | --- |
| `src/SixSlotSnap.cs` | Win32 interop, application context, tray, target validation, placement, recovery, resize sessions, timers, hotkeys, IPC, CLI entry point. |
| `src/LayoutModel.cs` | Profiles, normalized column/row weights, row-major slot mapping, rectangles, independent boundary changes and affected-slot sets. |
| `src/Settings.cs` | Shortcut validation, six presets, configuration schema version 1, XML load/save/backup and legacy import. |
| `src/Flyout.cs` | Nonactivating flyout, layout cards, zone highlight/navigation, draggable clock-area launcher bar. |
| `src/SettingsForm.cs` | Draft-based layout/weight editor, preview and shortcut dialog; validation before applying/saving. |
| `src/DisplayState.cs` | Native fresh monitor inventory, per-monitor saved geometry, pure session ownership/topology and draft merge transitions. |

## Geometry and resize ownership

The default profile has three columns with two rows each, numbered `1 2 3` above
`4 5 6`. Layouts support 1–4 columns, 1–4 rows per column, at most 16 zones and
12 profiles. Columns can have different row counts. Each column owns its own
row weights; all rows in a column share its width. Weights normalize to 1 and
each segment must be at least 4% of its dimension.

Coordinates use physical monitor work-area pixels with thread DPI awareness
context `-4`. Placement compensates outer bounds using DWM visible-frame bounds
(falling back to `GetWindowRect`). `SetWindowPos` preserves activation/Z order.

`SetWinEventHook(3,11)` observes foreground and move/size start/end. A start on
an assigned window captures actual bounds and HWNDs of the assigned set. The
30 ms timer samples the source. `LayoutModel.Adjust` reports width and height
affected slots separately. Width changes affect rows in the two adjacent
columns; height changes choose one internal boundary and its adjacent pair.
The controller pins the first observed height boundary for the whole gesture,
including while size probes are pending. If both edges first move together,
the larger displacement wins (bottom wins a tie). Runtime per-slot minimum
arrays constrain the model: each column takes the largest reported width minimum
among its rows; height limits apply only to the adjacent pair. Reported minimums
have a conservative 1-pixel rounding margin. Generic 400 px width / 200 px height
targets and the 4% floor remain fallback constraints. Impossible adjacent-pair
constraints do not mutate that boundary.

At resize start actual frames are captured before probing. A ThreadPool pass
queries WM_GETMINMAXINFO with a 50 ms SendMessageTimeout per eligible window,
sets its own physical-pixel DPI context and restores the prior context afterward.
The UI timer observes initial movement while waiting, then applies the latest
cumulative movement when results arrive. An early end event is completed after
probe readiness. Cancellation marks stale probes so they stop between queries;
they do not write UI state. Minimums are transient, not saved in layout profiles.

`ApplyResizeFrames` starts from captured actual bounds, replacing only scoped
axes with model bounds. Unaffected windows can be restored if native Snap
grouping moved them. Neighbor start/end events cannot take ownership while
the original drag is active. The source is skipped during the drag, aligned
at release, and three 100 ms settling passes apply the same scopes. Explicit
snap, profile/settings changes and proportion reset cancel stale sessions.
Whole-grid `ApplyLayout` is retained for explicit reflow/reset, not live dragging.

The prior-phase audit reproduced a race when the source moved again during
synchronous neighbor placement. `lastDrag` now advances only to the sampled
rectangle used for the adjustment; resampling afterward could silently consume
the unprocessed movement. An external disposable neighbor-triggered regression
fails on pre-fix `d8852bf` and passes in both launch modes after correction.
See [audit evidence](docs/PHASE-AUDIT.md).

Snap checks advertised minimums before moving and verifies the resulting visible
frame within two physical pixels before recording assignment. Rejected frames
request restoration of previous outer bounds. Failed snaps do not first switch
the active profile/work area. A constrained no-op drag requests the original
source frame instead of silently disconnecting it. Unexpected frame refusals
or impossible constraints stop the session, request baseline frames, restore
model proportions and release the source for re-snap. Unresponsive affected
neighbors are not repeatedly forced. Throttled feedback and the last issue are
recorded in runtime XML, together with resize/source status.

Assignments are HWNDs in a 16-entry in-memory array, one window per slot.
Moving an unchanged-size window away releases its slot at move/size end.
Recovery enumerates visible eligible windows with a less-than-16-pixel edge
tolerance. Two-row columns also recognize complementary top/bottom pairs and
recover their divider from actual bounds. No app identity or workspace session
is persisted. Phase 4 replaces the single assignment array with one session per
monitor; the controller binds its grid/assignment references to the active session.
Monitor-affinity checks prevent recovery/reflow/resize from pulling windows across
screens. Profile switches clear only that display's assignments. Destroy events
release the HWND across every session.

Schema-1 optional monitor records retain selected profiles and geometry snapshots;
the catalog and shortcuts remain shared. Windows device-interface paths identify
monitors, with GDI names as fallback. Port/driver changes can change identity.
Display/work-area/power messages use a 350 ms debounce and fresh native inventory.
Disconnect releases runtime ownership, retaining preferences; changed connected
work areas request verified frames only for eligible same-monitor assignments.
Reconnect uses geometry recovery, not app relaunch. Settings drafts preserve
concurrent monitor snapshots and target the display named when opened. See
[display checkpoint](docs/DISPLAYS.md) for migration, behavior and hardware limits.

A separate EVENT_OBJECT_DESTROY hook releases closed top-level windows and is
unhooked on exit. A recreated/restarted window must be explicitly snapped;
stored HWNDs are not treated as durable workspace identity.

## Flyout, keyboard and command flow

Phase-1 UI uses a third Appearance settings tab for launcher visibility and
draft position reset. Cancel discards those changes. The tray also offers an
immediate bar reset and About with `SixSlotSnap.ProductVersion` (currently
`0.3.0-preview.4`). Visible UI names are Windows Tile Studio; compatibility
identifiers remain unchanged. `LayoutProfile.GeometryMatches` compares segment
counts/weights with tolerance `1e-8`; `ApplySettings` only reflows the same active
profile when its geometry changed, preserving actual frames on appearance-only
saves. This tolerance avoids numeric-editor rounding triggering a reflow.

The persistent launcher defaults to the bottom-right of the primary taskbar's
monitor work area, with an 8-DPI-scaled-pixel gap, above the clock area rather
than over taskbar controls. `StartPosition.Manual` prevents Windows overriding
that location. Left-button capture distinguishes a click from a drag using the
system drag threshold. Dragging changes the bar's location, not app assignments;
release saves `LauncherPositionSet`, `LauncherX`, `LauncherY` in schema-1 settings.
Older settings omit these fields and receive the new default. Clone and XML
round-trip preserve them. Restored/dragged bounds are clamped to a screen's work
area using fresh native monitor geometry. Phase 4 also scales the owned launcher
font/dimensions at creation and WM_DPICHANGED, clamps suggested bounds, and
recreates/clamps it after display notifications. Physical mixed-DPI crossing
remains unverified on the single available display.

Launcher DPI fonts are cached by DPI and retained until child controls have been
disposed. SetScale explicitly updates child font references and reuses cached
fonts. Immediate disposal during replacement caused GDI+ Label.OnPaint failures;
real-loop initial/repeated paint and synthetic-DPI tests now cover this lifecycle
in compiled and source-hosted execution. No exception suppression masks failures.

The flyout uses a nonactivating topmost tool window. It retains the external
target HWND and returns selection to that target. Arrows choose a zone using
rectangle-center directional distance; Tab/Shift+Tab cycles layouts and resets
the zone to 1; Enter places it. Temporary key registrations are all-or-nothing,
with a visible mouse fallback on conflict. Escape is registered separately,
with polling fallback. A 20 ms timer dismisses on outside mouse press, Escape,
or a foreground window different from the target. Closing releases navigation
keys; no general keyboard hook is used.

Permanent hotkeys use IDs 1–16 (zones), 100 (flyout), 300+ (profiles).
Temporary IDs are 200 (Escape), 201–207 (navigation). Failed replacement of
permanent bindings attempts to restore prior registrations and reports failure.
Defaults: Ctrl+Alt+Win+Space opens; Ctrl+Alt+1 through 9 select numbered zones.

Single instance: mutex `Local\SixSlotSnapController`. Hidden native receiver:
`SixSlotSnap.Controller.v2`. Messages `0x8001`–`0x8005` select Show, Settings,
Quit, Snap, Profile. CLI supports `--show`, `--settings`, `--quit`, `--snap N`,
`--layout N`, `--data-dir PATH`, `--no-recover`, `--preview`. IPC profile indices
are zero-based; snap zone numbers are one-based. Command messages control the
existing session controller, regardless of the sender's data directory.

## Persistence and installation

Phase 2 (`0.3.0-preview.2`) keeps the controller alive if startup hotkey
registration fails. The failed candidate set is removed, autosave is suppressed,
and a posted settings command opens the Recovery tab. Invalid configuration
also opens Recovery while keeping the original bytes. `recoveryNotice` remains
until explicit successful Save; runtime XML reports version/recoveryRequired.
No fallback shortcut is silently persisted. The tray remains the recovery route
when no global bindings are available.

`ApplySettings` snapshots actual registered hotkeys before a candidate change;
failed file writes restore that snapshot. This matters when startup has no
registered keys even though the file requests conflicting shortcuts. The editor
tracks changed drafts, prompts on close, retains edits after save/validation
errors, confirms layout removal/grid replacement and stores per-profile grid
history for undo before save. The live tray proportion reset has its own
confirmation; launcher reset remains a separate nondestructive action.

Launch scripts default to `data/` beside source; direct EXE entry defaults to
its base directory unless `--data-dir` is supplied. Both launch modes import
older `bin/settings.xml` or `bin/layout.txt` on first use. `SettingsStore` writes
a temporary file then replaces the original with `.bak`. Invalid configuration
is preserved while defaults run; ordinary autosave is suppressed until an
explicit successful settings save. `runtime-state.xml` is diagnostics, not an
assignment restore database. `status.log` is local; neither belongs in Git.

`Get-FromGitHub.ps1` fetches the fixed successor repository's codeload ZIP or
accepts an offline archive. It checks archive paths/size, a single top folder,
reserved Windows names and symlinks before extraction. Code lives under
`versions/<commit-or-hash>`, data under a separate `data/`, and `current.json`
points the generated launcher at a version. SHA256 and any ZIP commit comment
are recorded; this is not a signed distribution or independent authenticity
guarantee. Default mode is Hosted; explicit Auto may fall back to Compiled for
older source. Only the fresh bounded staging directory is recursively cleaned.
The controller has no network service, AI dependency or telemetry; the installer
makes network requests when used without `-ArchivePath`.

## Verification boundaries

### Tray identity and optional legacy compatibility

The existing `NotifyIcon` now identifies itself as Windows Tile Studio, with a
32-pixel blue/cyan six-tile icon drawn using System.Drawing. Its HICON is cloned,
the temporary handle is destroyed, and the owned Icon is disposed on exit.
There is still one tray entry per controller, not a second companion process.

`Set-LegacySupersession.ps1` compiles `tools/LegacyRedirect.cs` outside `src/`
into a specifically selected legacy installation. It preserves the old EXE,
records original/guard SHA256 hashes and manager script/data paths in a sidecar
XML file, and closes only a running process at that exact old EXE path. If that
process cannot close normally it is stopped. Re-enable checks recorded hashes;
`-Disable` restores original bytes and retains the backup for recovery.

The redirect exits when the manager receiver already exists; otherwise it
starts the selected hosted source with the selected data directory. Missing
preferred source reports an error rather than silently running legacy code.
This blocks normal old shortcuts/direct launches in opted-in folders even while
the manager is stopped. It does not intercept unrelated copied binaries, install
a service, edit startup entries, or change the original repository. The shared
mutex still prevents simultaneous unmodified controller instances. This optional
compatibility shim is an EXE in the legacy folder; hosted manager mode remains
source-only. `tests/SupersessionTests.ps1` uses a disposable fake legacy process
and source launcher; run it with the daily controller stopped.

Phase 2 also writes standalone Restore-SixSlotSnap.cmd/.ps1 helpers in opted-in
legacy folders. They restore only when original and guard hashes match and
retain the backup, with no dependency on manager source. The redirect's missing
source message points to this helper. XML setup writes use explicit InnerText
string assignment for Windows PowerShell 5.1 compatibility.

`GridTests` checks geometry/configuration without desktop movement.
`IntegrationTests` launches a separate controller and six disposable windows,
injects move/size notifications, inspects actual bounds, clicks controls and
sends actual keyboard input. It requires the existing controller stopped.
`LauncherTests.ps1` checks hosted compilation/serialization, version updates,
path quoting, data preservation and unsafe archive rejection. See [HANDOFF](HANDOFF.md)
for recorded evidence; these tests do not establish universal app compatibility.
