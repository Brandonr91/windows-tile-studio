# General settings

Local preview `0.3.0-preview.6.1` adds **General settings** to the tray menu.
It opens **General** in the existing settings manager. You can also select that
tab from **Manage layouts & hotkeys**. Layouts, per-layout/zone hotkeys, sharing
and workspaces retain their own tools.

## Startup & tray

Status and the exact command are shown without enabling anything. **Enable for
this installation...** confirms before registering the owned current-user
Startup shortcut. **Disable / remove startup...** confirms before removing it.
An unrelated or unreadable shortcut is preserved. Windows policy can override
the registered entry. Current compiled/source-hosted mode and data path are kept.

These startup actions apply immediately, independently of the settings draft:
Cancel does not undo an explicitly confirmed startup action. They never restore
a workspace or launch saved apps. The manager launches quietly and respects the
saved bar visibility; no second start-quietly setting duplicates that preference.
See [startup mechanism and limits](WORKSPACES-AND-STARTUP.md).

Closing/minimizing the floating bar hides it to tray and remembers that choice;
only tray Exit quits. These semantics are explained rather than made optional.

## Layouts bar

Show/hide, reset position near the clock, and **Lock bar position** are drafts
until Save changes. Cancel/discard retains the current preferences. Lock blocks
dragging and position writes; normal clicks still open the flyout. An attempted
drag does not open it accidentally. Explicit position reset remains available
while locked. Older settings omit the lock and remain draggable by default.

## Flyout shortcut

Change the flyout-opening key here or in Hotkeys; both edit the same draft value.
Imports also refresh this display. A blank opening key is rejected. Existing
duplicate/OS-conflict validation and failed-save binding rollback still apply.
Save applies the change; Cancel keeps the current shortcut. Zone/profile keys
remain in Hotkeys. Bar/key-only edits do not reflow managed app geometry.

## Support

View version and tray/resize guidance; open the active data folder or local
status.log. Missing files report a useful error. These actions are local and
upload nothing. Logs/configuration/workspaces may expose paths/display identities
and optional titles: review before sharing. Share layouts exports the separate
portable catalog without app/window/display identity.

Phase 7 adds **Preview diagnostic report...** under Support: inspect allowlisted
aggregate XML, then optionally save locally. It excludes logs, titles, paths,
layout/monitor identities, keys and window identifiers and uploads nothing.
See [accessibility and support](ACCESSIBILITY-AND-SUPPORT.md) for fields, keyboard
editing, contrast/scaling support and remaining manual checks. User-selectable
theme/text-size controls remain unimplemented. Startup is never enabled by an update.

## Appearance (local beta.3)

Choose System, Light or Dark under General → Appearance. System follows Windows app mode/accent; high contrast takes priority. Preview is draft-only until Save changes. Cancel keeps the saved theme. See [appearance implementation and limits](APPEARANCE.md).
