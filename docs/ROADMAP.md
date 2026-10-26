# Development roadmap

## Basic preview version — implemented

- Persistent custom-layout flyout, opened by a configurable hotkey.
- Six starter layouts and a settings GUI for adding/removing/editing layouts.
- Up to four columns and four rows per column, with editable proportions.
- Configurable flyout, zone, and per-layout shortcuts.
- Linked column boundaries and independent row boundaries within each column.
- Saved settings, backup on replacement, and legacy ratio migration.
- Geometry/configuration regressions, cross-process integration tests, and
  desktop checks of the actual hotkey, Escape, outside click, and GUI save.

The basic build has been verified locally and is published in this separate
repository. Broader daily use and compatibility testing remain part of the
next stage. Nothing in this roadmap schedules background work or promises a
particular release date.

## Next: practical use and fixes

- Collect concrete failures from normal app use before broadening the feature set.
- Exercise mixed app frames, minimum sizes, minimize/restore, and app restarts.
- Check display changes, sleep/resume, and competing window-management utilities.
- Improve startup recovery when a configured shortcut becomes unavailable.
- Add keyboard navigation inside the flyout; the basic flyout currently uses
  mouse selection, Escape, and configurable direct zone shortcuts.

## Polished version

- More visual proportion editing and better settings presentation.
- Per-monitor state and display-change recovery.
- App minimum-size negotiation and clearer fit feedback.
- Optional saved app assignments and workspace restore.
- Explicit opt-in login startup with a matching removal path.
- An application icon, versioned release packaging, and signing if distributed
  binaries become part of the project.

Keep the first version portable and easy to stop. Evaluate each addition
against the original goal: predictable snapping and connected resizing with
independent row dividers.
