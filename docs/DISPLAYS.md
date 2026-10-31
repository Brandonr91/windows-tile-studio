# Display support checkpoint

Local beta.9 review update: recovery now matches all four saved slot edges and
preserves saved proportions; it no longer learns row weights from complementary
window frames. Mismatches require an explicit re-snap. Existing same-monitor
work-area reflow remains. Deployed locally with approval on October 31; physical
docking/sleep acceptance remains unverified. See
[phase-1 behavior and evidence](PHASE-1-RECOVERY.md).

Phase 4, local `0.3.0-preview.4`, 2026-10-27. This is a preview, with the
hardware coverage below; it is not a multi-monitor production certification.

## Using per-monitor layouts

Select an app on the display you want to manage, then open the layout flyout
or use a zone shortcut. Each display remembers its selected layout and its
own column widths and independent row dividers. Switching displays keeps the
other display's live assignments. A height gesture still affects only the
adjacent pair in that column on that display.

Open Manage layouts from that display's flyout to edit its proportions. The
settings banner identifies the display being edited. Layout names, grid shapes,
the catalog and hotkeys are shared; proportions and selected layouts are per
monitor. A shared grid shape change releases incompatible live assignments;
re-snap windows to connect them to the changed grid. Settings opened from the
tray edit the last active managed display. If it disconnects with a draft open,
save reports the problem and keeps the draft available for reconnection or cancel.

Display, work-area and resume notifications trigger a debounced fresh inventory.
A work-area/resolution change requests adjusted bounds only for still-assigned
normal windows that remain on the same monitor. Rejected fits, minimized and
maximized assignments are released on that change; re-snap when appropriate.
Moving an assigned window onto another monitor releases its old connection.
There is no automatic cross-monitor return or application launch.

Disconnecting clears that monitor's transient assignments and retains saved
layout proportions. Reconnection can recover existing matching visible geometry
when recovery is enabled; it does not restore a saved workspace. The launcher
is clamped to a currently available work area and responds to DPI changes.
An open flyout closes on a display refresh so it cannot use stale geometry.

## Identity and migration

The monitor key is Windows' monitor device-interface path from
[EnumDisplayDevicesW](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumdisplaydevicesw)
with `EDD_GET_DEVICE_INTERFACE_NAME`; a GDI display name is the fallback.
This normally survives ordinary enumeration/reconnect, but port, driver or
device changes can produce a new identity. No EDID serial matching or speculative
merge is implemented. Old records are retained, at most 32, with no current
monitor-record removal UI. Machine-specific IDs stay in local settings/logs,
not in repository fixtures or exported examples.

Schema 1 gains an optional `Monitors` list. Existing files without it seed the
first display from the existing selected profile/proportions; normal settings
replacement retains `.bak`. Each saved monitor record contains a selected
profile ID and profile geometry snapshots. Layout metadata/hotkeys come from
the shared catalog; mismatched shapes ignore old geometry. HWND assignments
remain memory-only. The shared catalog mirrors the current display's active
geometry for backward-compatible tooling. An older controller will ignore the
new fields and may discard them on save; use the fallback settings snapshot
when rolling back. Corrupt-file preservation/recovery remains unchanged.

`DisplayState.cs` separates inventory from pure `MonitorWorkspace` transitions.
The controller owns placement, recovery, native notifications and timers.
Inventory uses
[EnumDisplayMonitors](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumdisplaymonitors)
and physical coordinates. Suspend cancels outstanding resize/flyout state and
saves; resume automatic/user notifications schedule a fresh inventory, following
[WM_POWERBROADCAST](https://learn.microsoft.com/en-us/windows/win32/power/wm-powerbroadcast).
No periodic display polling, OS display changes, services or startup registration
are added.

## Verification and remaining release gates

- Pure tests: two simulated physical-coordinate displays, negative origin,
  different sizes, independent profiles/ratios/assignments, pair-only heights,
  switch/restart, disconnect/reconnect, work-area reflow flags, shape/removal
  fallback, deep clone, XML migration/roundtrip and concurrent-draft preservation.
  Launcher dimensions at 125% and 200% are checked separately.
- Hosted/native suite: six disposable windows, live display inventory, synthetic
  display/work-area/resume messages with unchanged hardware, preserved bounds
  and assignments; existing resize/flyout/settings/recovery/compatibility matrix.
  The synthetic notifications do not change Windows display or power settings.
- Available hardware: one 5120 x 2160 display at 125%. No actual second display,
  mixed-DPI crossing, physical unplug/docking, scale/resolution/taskbar change,
  or sleep/wake cycle was performed. These remain explicit release gates.
- Simulated topology proves state ownership, not Windows' physical relocation
  timing. Geometry recovery can still ambiguously adopt overlapping windows.
  Identity changes require choosing/recreating the layout for the new identity.
- Phase-3 elevated/hung-app and arbitrary frame/size limitations still apply.

Before production, test two real monitors with different scales, one at a
negative origin: snap and resize on each, save/restart, move apps between them,
change the taskbar work area, disconnect/reconnect in both directions, dock,
and sleep/wake with a settings draft open. Verify no unrelated app moves, no
stale HWND adoption, correct launcher scaling/visibility and preserved ratios.
