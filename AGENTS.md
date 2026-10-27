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
- Keep the visual selector nonactivating and retain clean hotkey cleanup.
- The flyout stays open until selection, outside click, Escape, or its toggle
  shortcut; switching foreground apps also dismisses it. Preserve the original
  target; never snap the flyout itself.
- Keyboard navigation uses temporary all-or-nothing registrations while open:
  arrows, Tab/Shift+Tab, Enter. Release them on every close/exit path and keep
  mouse selection working if registration conflicts.
- Failed hotkey changes must restore the previous registered shortcuts.
- A move-out releases a slot; a resize keeps neighboring edges joined.
- Keep the controller local: no network service or AI runtime dependency.

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

## Repository hygiene

Do not commit `bin/`, runtime logs, layout preferences, account details, or
machine-specific helper scripts. Document user-visible changes and limits.
Do not publish, upload a release, or enable automatic startup unless requested.
