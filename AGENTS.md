# Working on Windows Tile Studio

The application currently retains the `SixSlotSnap` executable and internal
class names. This repository is independent of the original six-slot project.

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
- Preserve physical-pixel DPI handling and invisible-frame compensation.
- Keep the visual selector nonactivating and retain clean hotkey cleanup.
- The flyout stays open until selection, outside click, Escape, or its toggle
  shortcut. Preserve the original target; never snap the flyout itself.
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
