# Project notes

These notes preserve the project's origin and early lessons. Current component
behavior is documented in [ARCHITECTURE](../ARCHITECTURE.md), priorities and
known gaps in [ROADMAP](../ROADMAP.md), and the latest verification/delivery
checkpoint in [HANDOFF](../HANDOFF.md). Updated 2026-10-26 for the first polish
milestone; historical lessons below remain applicable.

## Origin and behavior

Six Slot Snap was made for a 5K2K ultrawide Windows desktop where six windows
needed to remain useful after placement. The important distinction was
between remembering six rectangles and continuously managing their shared
boundaries.

The resulting model has two shared vertical column boundaries and three
independent horizontal row boundaries. The top and bottom windows in a column
always share its width. The three row dividers can be at different heights.

## Implementation map

The controller is split across `src/`:

| Component | Responsibility |
| --- | --- |
| `LayoutModel` / `LayoutProfile` | Convert saved column and row weights into zone rectangles and adjust shared boundaries. |
| `SnapTo` / `PlaceWindow` | Track the selected window and position its visible frame. |
| `SnapFlyout` / `LayoutCard` / `LauncherTab` | Show persistent layout previews and select a zone without taking foreground focus. |
| `SnapSettingsForm` / `ShortcutDialog` | Edit layouts, proportions, and shortcut assignments. |
| `CommandWindow` / `HotkeyRegistry` | Receive global shortcuts and replace registrations with conflict rollback. |
| `OnWindowEvent` / `UpdateDrag` | Observe native move/size notifications and update neighbors. |
| `RecoverAssignments` | Reconnect occupied slots from their visible geometry. |
| `SettingsStore` | Persist profiles and hotkeys to XML, keep a backup, and import legacy six-slot ratios. |

The controller uses a per-monitor DPI-aware thread context before reading
monitor and window geometry. It compares outer window bounds with DWM visible
frame bounds to account for invisible borders. This mattered on the original
display: DPI-scaled coordinates described a 4096 × 1728 desktop while the
physical display was 5120 × 2160 at 125% scaling.

Native move/size start and end events identify the window being manipulated.
A timer samples its visible rectangle during the drag. Other assigned windows
follow only the affected axes of the updated grid; other axes retain their
actual starting bounds. The controller avoids repeatedly positioning the
window currently under the mouse. After release, it aligns the affected edges
and performs a short settling pass using the same scopes. Height adjustments
are limited to an adjacent pair; flanking height changes caused by native
grouping are corrected instead of propagated.

## Lessons from the first version

- A successful six-slot placement test did not establish linked resizing.
- All six assignments must remain connected, including windows in neighboring
  columns. One displaced window can make an otherwise correct layout look broken.
- A saved divider ratio can be stale after a native resize or a restart. Matching
  existing top/bottom pairs provides better recovery evidence in that case.
- Moving a window away and resizing an internal edge are different actions.
  Moving away should release the slot instead of reshaping the entire grid.
- Each column needs a separate row ratio. Applying one global row height would
  violate the intended interaction even when the dividers happen to line up.

## Verification and limits

The committed geometry test runner performs deterministic layout and boundary
checks at two monitor origins, including randomized extreme resize inputs.
It does not touch desktop windows. Development also included controlled six-window
integration checks with native move/size notifications and observed window bounds.

An opt-in cross-process suite is included as `tests/IntegrationTests.cs` and
can be run with `Build.ps1 -Integration` while the controller is stopped.
It uses isolated settings and disposable windows, with no adoption of existing
apps. Actual desktop checks also exercised hotkey customization, native keyboard
input, Escape, outside clicks, and GUI layout creation. These checks do not cover
every frame style, privilege level, app minimum size, or real mouse-drag timing.
The project should be treated
as an early utility with known constraints, not a replacement for every Windows
window-management feature.

## Possible next work

- Broader cross-process, interactive regression coverage.
- Better minimum-size negotiation before resizing neighbors.
- Per-monitor state and display-change handling.
- A more visual layout editor and broader keyboard/accessibility coverage.
  Arrow navigation, Tab/Shift+Tab and Enter selection are now implemented;
  temporary registrations are released on dismissal or rolled back on conflict.
- Explicit assignment persistence that survives app restarts.
- Optional startup registration with an equally simple removal path.
- Code-signing and a reproducible release pipeline if binary downloads are added.

The controller has no telemetry or network client. The optional source downloader
does fetch GitHub archives; see [launch modes](LAUNCHING.md). Launch scripts now
store settings and logs under `data/` by default, separately from versioned source
in installed mode; a directly launched EXE still defaults to its base directory.
User settings and logs are excluded from version control. Review logs before
sharing them in an issue.
