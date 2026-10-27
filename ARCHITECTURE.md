# Architecture

Inspected 2026-10-26; base application `48b96b2`, plus the clock-area draggable
launcher change following context checkpoint `1aac7f3`.
This document describes implemented behavior. Future work is in [ROADMAP](ROADMAP.md).
Existing [project notes](docs/PROJECT-NOTES.md) retain the origin and lessons learned.

## Scope and execution

Windows Tile Studio is a local C#/Windows Forms controller using Win32 window
APIs. It places and links real app windows; it does not modify Windows' built-in
Snap layout catalog, replace the taskbar, or create virtual monitors. The original
`Brandonr91/six-slot-snap` repository is a separate preserved predecessor.

`Build.ps1` compiles all five `src/*.cs` files using the installed .NET Framework
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
If both height edges move in a sample, the larger displacement wins (bottom
wins a tie). Current clamps use generic 400 px width / 200 px height targets,
bounded by segment capacity and the 4% floor; they are not app minimum sizes.

`ApplyResizeFrames` starts from captured actual bounds, replacing only scoped
axes with model bounds. Unaffected windows can be restored if native Snap
grouping moved them. Neighbor start/end events cannot take ownership while
the original drag is active. The source is skipped during the drag, aligned
at release, and three 100 ms settling passes apply the same scopes. Explicit
snap, profile/settings changes and proportion reset cancel stale sessions.
Whole-grid `ApplyLayout` is retained for explicit reflow/reset, not live dragging.

Assignments are HWNDs in a 16-entry in-memory array, one window per slot.
Moving an unchanged-size window away releases its slot at move/size end.
Recovery enumerates visible eligible windows with a less-than-16-pixel edge
tolerance. Two-row columns also recognize complementary top/bottom pairs and
recover their divider from actual bounds. No app identity or workspace session
is persisted. A change of monitor work area clears assignments; no per-monitor
state or display-change subscription is implemented.

## Flyout, keyboard and command flow

The persistent launcher defaults to the bottom-right of the primary taskbar's
monitor work area, with an 8-DPI-scaled-pixel gap, above the clock area rather
than over taskbar controls. `StartPosition.Manual` prevents Windows overriding
that location. Left-button capture distinguishes a click from a drag using the
system drag threshold. Dragging changes the bar's location, not app assignments;
release saves `LauncherPositionSet`, `LauncherX`, `LauncherY` in schema-1 settings.
Older settings omit these fields and receive the new default. Clone and XML
round-trip preserve them. Restored/dragged bounds are clamped to a screen's work
area; this does not add full display-change or mixed-DPI migration support.

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

`GridTests` checks geometry/configuration without desktop movement.
`IntegrationTests` launches a separate controller and six disposable windows,
injects move/size notifications, inspects actual bounds, clicks controls and
sends actual keyboard input. It requires the existing controller stopped.
`LauncherTests.ps1` checks hosted compilation/serialization, version updates,
path quoting, data preservation and unsafe archive rejection. See [HANDOFF](HANDOFF.md)
for recorded evidence; these tests do not establish universal app compatibility.
