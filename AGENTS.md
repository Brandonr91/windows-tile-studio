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
- The flyout stays open until selection, outside click, Escape, or its toggle
  shortcut; switching foreground apps also dismisses it. Preserve the original
  target; never snap the flyout itself.
- Keyboard navigation uses temporary all-or-nothing registrations while open:
  arrows, Tab/Shift+Tab, Enter. Release them on every close/exit path and keep
  mouse selection working if registration conflicts.
- Failed hotkey changes must restore the previous registered shortcuts.
- A move-out releases a slot; a resize keeps neighboring edges joined.
- Keep the controller local: no network service or AI runtime dependency.
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
It uses a separate Chrome profile and must never move existing browser windows.
Performance samples are local observations, not universal latency/CPU promises.

## Repository hygiene

Do not commit `bin/`, runtime logs, layout preferences, account details, or
machine-specific helper scripts. Document user-visible changes and limits.
Do not publish, upload a release, or enable automatic startup unless requested.
