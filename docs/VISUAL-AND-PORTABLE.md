# Visual editing and portable layouts

Local phase-5 preview `0.3.0-preview.5`, 2026-10-27.

## Edit a layout visually

Open **Manage layouts & hotkeys** from the tray. On **Layouts**, select a
layout and choose **Edit visually...**. Drag blue vertical lines to change the
two neighboring column widths. Drag a horizontal line to change only the two
adjacent rows in that column. Other columns keep their heights.

The preview uses the same normalized column-stack geometry and row-major zone
numbers as snapping. It supports different row counts in each column. Every
segment keeps at least 4% of its dimension. App minimum sizes still determine
whether a real window fits; the preview is not an app-compatibility guarantee.

Each completed drag is one Undo step; Redo is available until another edit.
Escape during a drag cancels that gesture. History is capped at 50 gestures.
**Use proportions** copies the preview into the settings draft; **Cancel** or
closing the visual dialog discards that preview. **Undo grid change** in settings
can also undo an accepted visual edit. Numeric weights remain available.

Real windows and saved settings change only when you choose **Save changes**
in the main settings dialog. Closing a changed draft asks whether to discard.
Appearance, launcher visibility and close-to-tray behavior remain separate.

## Export and import

The **Share layouts** tab exports the current draft's whole layout catalog and
shortcuts as a `.tilelayouts.xml` file. No monitor device identities, HWNDs,
launcher coordinates, app identities, logs or executable commands are included.
Names and shortcut choices are included intentionally. Files contain schema-1
`TileStudioLayouts` data, not the local `settings.xml` schema.

Import choices:

- **Add as copies** preserves the current catalog. Copies get fresh IDs and
  unique names, so they do not overwrite per-monitor saved geometry.
- **Replace layout catalog** replaces the draft catalog after confirmation.
  Saving removes the old catalog and releases incompatible assignments across
  monitors; re-snap windows to link them to an imported layout. It does not
  relaunch apps or force windows across displays.
- **Import shortcuts too** is off initially. When off, existing global keys
  are preserved and imported layout keys are unassigned. When on, imported
  global shortcuts replace the draft global set; duplicates against retained
  layouts are rejected. Windows/reserved-key availability is checked on Save,
  using the existing rollback behavior.

The confirmation summarizes the count and shortcut/replacement policy before
staging. Review layouts/hotkeys, then Save. **Undo import** restores the catalog
and hotkeys from before the most recent import, with confirmation because later
layout/hotkey edits will also be undone. It keeps Appearance changes. Canceling
the main settings dialog preserves the live configuration.

Imports reject unsupported/missing versions, wrong/unknown elements or
attributes, malformed XML, DTD/external-entity declarations, XML depth over 32, files over 256 KB,
invalid geometry, missing/duplicate identities/names, unsupported/duplicate keys
and catalogs exceeding 12 layouts. Validation and conflict checks complete on
copies before the draft is replaced. Export uses a temporary file and atomic
replacement; a failed export preserves the destination.

## Verification and limits

- Pure checks cover isolated pair/column edits, 4% clipping, cancel/undo/redo,
  random drags, portable roundtrip/privacy, IDs/names, profile cap, shortcut
  conflicts, invalid XML/version/entities and blocked-write preservation.
- Real-message-loop UI tests cover pointer drag, pair-only heights, Undo/Redo,
  Escape, painting, nested settings acceptance/grid undo, draft isolation,
  import/failure/undo/discard and sharing-page rendering. Both compiled and
  source-hosted tests are available with `-EditorUi` and
  `tests/EditorUiTests.ps1` (Windows PowerShell 5.1 STA).
- A real mouse drag/Undo in a disposable preview was visually checked through
  computer use. It changed only the center pair, then restored the divider.
- Existing controller regression suites retain linked resizing, hidden launcher,
  font/DPI painting, recovery and monitor-state coverage. Synthetic display
  tests do not establish physical mixed-DPI/docking/sleep coverage.
- Keyboard divider dragging, arbitrary overlapping zones and saved app workspaces
  are not implemented. Use numeric weights for keyboard-based editing.

During this phase Windows/.NET window inventories were checked for error dialogs,
and local Application crash events for the app/test processes were inspected.
No unexpected dialogs or matching crash events were observed in the sampled
checks. This is not continuous surveillance of the notification area. Expected
fit/hotkey/recovery warnings are intentionally exercised by the regression suite.
