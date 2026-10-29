# Phase 7: accessibility, visuals and support

Local preview `0.3.0-preview.7`. This records implemented behavior and its test
limits, not an accessibility certification or a production release.

## Keyboard editing and feedback

In **Layouts → Edit visually**, focus the canvas with Tab or a click.
Page Up/Down cycles column dividers, then each column's row dividers. Left/Right
adjust the selected column divider; Up/Down adjust a row divider. Steps are 1%
of the full dimension, or 0.1% with Shift. Other-axis arrows do nothing. Each
nudge is an undoable gesture with the same 4% floor and adjacent-pair rule as
mouse editing. Ctrl+Z/Y undo/redo; Escape cancels an active mouse drag; Tab leaves
the canvas. A one-zone layout explains that there are no internal dividers.

The selected divider is dashed and gains a thicker line/focus outline when the
canvas is focused. A status label reports its position. The canvas exposes a
slider role and current divider/value through its accessible object and sends
value-change notifications. Numeric editing remains available. Edits remain
draft-only until the main Save, including keyboard edits.

Flyout selection now has accessible descriptions as well as its visual border.
Settings controls receive names from their existing labels where needed and
tab order follows visual position. Workspace preview/delete/restore/launch
buttons disable when their required saved item/selected match is absent. Native
instance and duplicate validation remains in the restore path; disabled state
is feedback, not a substitute for revalidation.

## Work areas, DPI and contrast

Settings, visual/workspace dialogs and diagnostic preview are resizable, use
WinForms DPI scaling and clamp their initial bounds to the current work area.
Absolute settings pages and workspace input groups scroll when needed; actions
remain outside the scrollable body. Scrolling preserves full numeric/general
controls instead of shrinking text into an unreadable layout. Grid columns have
minimum widths and can scroll horizontally.

Custom flyout/cards/bar/canvas use a shared palette. Windows high contrast uses
system window/text/highlight colors and is refreshed on system-color changes.
Normal custom panels retain a dark palette with stronger text/border contrast;
standard dialogs use Windows controls. No Windows theme, scale or contrast
setting is changed by the app/tests. Separate user-selectable themes/text sizes
are not implemented. Physical mixed-DPI movement and Windows text-size changes
still need manual testing; simulated bounds/palette tests do not establish them.

## Local diagnostic export

Open **General → Support → Preview diagnostic report...**. Inspect the read-only
XML before choosing Save report. Save writes a local file atomically; it never
uploads. No report is created automatically.

The allowlist contains application version/launch mode; OS version as reported
by .NET, CLR version, process bitness and high-contrast state; assignment count,
recovery/bar flags; grid column/row counts; connected display work-area sizes;
and process working-set/private-memory/cumulative-CPU samples. The reported OS
version can be compatibility/manifest-limited and is not a verified Windows build.
A process sample is an observation, not a benchmark.

The builder does not read logs or workspace files. It excludes titles, app paths,
profile names/IDs, keys, monitor names/IDs/handles/coordinates, HWND/PID, usernames,
data/source paths, command lines and window/document content. There is no option
to include those fields. Existing Open local log/data-folder actions remain
separate; their files are not sanitized merely because this exporter exists.

## Verification and remaining gates

Before this change, a probe against the installed preview.6.1 confirmed Right did
not change canvas geometry and settings exceeded an 800×600 work area. New
AccessibilityUi checks cover actual canvas Right/Tab input plus deterministic
divider keys/undo/redo/floors/uneven grids, accessible role/value, injected system
contrast colors, constrained dialogs/scroll access, flyout directions/wrap/scroll,
report sentinel privacy, atomic save/failure preservation and preview isolation.
Existing editor, General/workspace, launcher paint/DPI, compiled/source-hosted
native integration and source installer suites remain required.

Run `Build.ps1 -AccessibilityUi -OutputDirectory bin/phase-seven` and the Windows
PowerShell 5.1 STA `tests/AccessibilityUiTests.ps1` counterpart. Integration and
performance checks require stopping the daily controller and isolated disposable
windows/data. `tests/PerformanceTests.ps1 -SourceRoot <extracted-package>` measures
compiled and source-hosted startup readiness, one-second idle CPU, and memory
without recovering or moving real windows. See HANDOFF for accepted reports and
local measurements. Samples are specific to this machine, not performance promises.

Manual Narrator/other screen-reader announcements, actual OS contrast switching,
text-size-only scaling, physical mixed-DPI/docking/sleep, elevated/hung apps and
clean-account login/install/update/remove remain beta/release gates. No screen
reader or Windows setting was enabled just to test this phase. Phase 8 is
production delivery; passing these scoped tests does not close those gates.
