# Workspaces and optional login startup

Phase 6 introduced these features; preview `0.3.0-preview.6.1` moves startup to
**General settings → Startup & tray**. Open **Workspaces** from the tray for
capture/restore. Workspace and confirmed startup actions are immediate, separate
from layout/bar/hotkey Save/Cancel drafts. See [General settings](GENERAL-SETTINGS.md).

## Capture and restore

1. Snap the windows you want to manage. Enter a workspace name and choose
   **Capture assigned windows**. Only supported, currently assigned windows
   on connected monitors are captured. Replacing a named capture asks first.
2. Select the saved workspace and choose **Preview / refresh**. Review app,
   zone, target display, status and matching-window choices.
3. Ambiguous apps need a manual window choice. Check only the rows you want,
   then **Restore selected windows...** and confirm the previewed move.

Saved identity is the full local executable path and native window class.
**Save exact window titles** is off initially; when selected, titles are stored
locally and help distinguish several browser/app windows. Without titles, two
equally plausible windows are never matched by their enumeration order. Even
with titles, duplicates require a choice. If a title changed, the matching-window
dropdown can select another window of that same executable/class explicitly.

The preview holds temporary HWND/PID/process-start data only in memory. Restore
rechecks current instances and display/layout availability; stale entries are
skipped, not silently rebound. No HWND/PID is saved as durable identity.

Restore uses saved proportions in the existing matching layout/grid. If the
catalog removed that layout or changed its grid, capture again. Compatible
unselected assignments remain connected; changing a display's managed geometry
releases incompatible assignments without moving those other windows. Restore
is best-effort per selected window and reports successes/skips; it is not an
atomic all-app transaction. App minimum/frame checks still apply. Capture another
workspace beforehand if you want a saved arrangement to return to.

Disconnected saved displays remain unavailable unless you explicitly choose
**Use current display for disconnected saved displays**. Here current means the
controller's active managed display, shown in the preview. Restoring several
saved displays onto one screen at once is rejected; select one saved display's
rows at a time. Normal resizing still never pulls windows across displays.

**Launch selected missing app...** is a separate confirmed action for a row
whose app has no matching current windows. It starts the displayed local `.exe`
without arguments. It does not recreate documents, URLs, profiles or sessions.
Wait for the app, preview again and choose its window. Restore, app startup,
resume and login never relaunch saved apps automatically.

Workspace actions are explicit. While this dialog is open, placement/flyout
hotkeys focus the dialog rather than moving background windows. Close it to
resume normal placement. Layout settings and workspace dialogs do not open
over one another. Closing the dialog keeps the manager/tray running.

## Storage and recovery

Captures live in the active data directory's `workspaces.xml`, independently of
`settings.xml`. Files contain app executable paths, monitor identities and saved
geometry; titles are included only when selected. They contain no executable
arguments, scripts or automatic launch instructions. Do not share this local
file as a portable layout; use phase 5's Share layouts instead.

The store supports 16 named workspaces, 16 saved displays each and 16 placements
per display. XML is bounded to 1 MB and depth 32, prohibits DTD/external entities,
and validates profiles, paths, slots and duplicate identities. Atomic replacement
keeps `workspaces.xml.bak`. A corrupt file is preserved and capture/restore
controls are disabled with recovery guidance; General startup controls are independent.
Deleting a capture asks first and changes only the saved library, not apps.

Limitations: executable paths/classes can change with app updates, requiring a
new capture. Shared hosts such as ApplicationFrameHost/conhost are skipped rather
than guessed; some Store/console apps are unsupported. Elevated/inaccessible or
fully hung apps, virtual desktops, app sessions and physical mixed-DPI/docking
need broader testing. Per-monitor identity still has phase-4 port/driver limits.

## Login startup

**General → Startup & tray** shows registration status and the exact launch command.
**Enable for this installation...** asks before creating/updating the current
user's `Windows Tile Studio.lnk` in their Startup folder. It preserves compiled
or PowerShell-hosted mode, source location and the active data directory. Missing
files prevent registration. An owned entry from another Tile Studio installation
can be replaced after confirmation; unrelated/unreadable entries are preserved.

**Disable / remove startup...** removes only that owned shortcut. It does not
stop the currently running controller. You can also remove that shortcut manually
from your account's Startup folder after quitting. No service, scheduled task,
administrator setting or registry Run entry is installed. Hidden launcher state
is retained. Login launch starts the manager only; workspace restore stays manual.

Windows can override startup entries in its startup-app settings. The UI reports
the shortcut registration, not a guarantee that Windows policy will execute it.
Other legacy/unrelated startup entries are not modified. A separate existing
entry may start a different installation first; review Windows startup settings
if necessary. The single-instance controller still prevents duplicate managers.

The per-user Startup folder is a supported Windows mechanism. It also avoids
the documented 260-character registry Run-command limit for long hosted source
and data paths. See Microsoft's [startup configuration guide](https://support.microsoft.com/en-us/windows/experience/startup-boot/configure-startup-applications-in-windows)
and [Run/RunOnce documentation](https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys).
Shortcut creation uses the installed Windows Shell automation component; errors
preserve existing entries. Source-hosted mode still needs Windows PowerShell 5.1.

## Verification checkpoint

- Pure checks cover matching, title opt-in, ambiguity/duplicates, missing apps,
  explicit monitor fallback, removed/changed layouts, stale process identity,
  XML privacy/backup/failure preservation and startup opt-in/ownership/quoting.
- Real-loop workspace UI tests cover capture/preview/selected restore through
  injected callbacks, explicit launch guards, startup confirmation/cancel/enable/
  disable with memory storage, rendering and native shortcut roundtrip/removal
  in an isolated temporary folder. No real login startup was enabled.
- Native disposable windows prove capture/save/load/preview/restore of actual
  visible frames, stale-process refusal and preservation of compatible unselected
  slots. Explicit launch starts only an inert test probe, with zero arguments.
- Compiled and hosted controller regression suites pass within prior coverage.
  New cold-start checks prove the first workspace and settings dialogs are visible
  after a hidden background start with the launcher off. This fixed a reproduced
  invisible-first-dialog issue found during live inspection.
- Visual review caught a clipped preview/footer; docked layout, displayed names
  and visible action controls now have regression assertions.
- Real login/logout, production shortcut enable/disable, missing real app relaunch,
  and physical multi-monitor/sleep cases were not performed. The actual account's
  startup entry and real workspace files were left unchanged during development.

Commands: `Build.ps1 -Test -WorkspaceUi -WorkspaceIntegration`, plus Windows
PowerShell 5.1 STA `tests/WorkspaceUiTests.ps1` for source-only UI coverage.
`WorkspaceNativeTests.exe --hidden-dialogs [path-to-Start-Hosted.ps1]` checks
compiled/hosted hidden-start visibility with the daily controller stopped.
