# Development roadmap

For current priorities, acceptance criteria and known defects/limits, read the
root [ROADMAP](../ROADMAP.md). This file retains the existing milestone history;
keep both views synchronized when a meaningful milestone or priority changes.

Current phased execution: [Polishing phases](POLISH-PHASES.md). Phase 1 adds
Appearance controls, launcher reset and version/branding consistency. All
previously optional capabilities are in the planned phases, not claimed complete.

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
- Broaden keyboard navigation checks across mixed layouts and competing apps.

## First polish milestone — implemented

- Windows PowerShell-hosted mode, with no standalone application EXE generated.
- GitHub source downloader with versioned code folders and persistent data.
- An explicit hosted/compiled mode choice and a no-launch installation option.
- Offline install/update/path tests and a live GitHub source-download test.
- Full cross-process desktop integration coverage against the hosted controller.
- Highlighted keyboard selection: arrows for zones, Tab/Shift+Tab for layouts,
  Enter to place, with temporary registration cleanup and conflict rollback.
- Pair-only height changes, preserving actual flanking heights during width
  resizing and correcting unwanted native group changes.
- Persistent Layouts bar near the taskbar clock; drag it to move, click to open,
  with saved position and work-area clamping.
- Distinct Windows Tile Studio tray name/icon and reversible, opt-in legacy
  launch redirection for explicitly selected older controller folders.

See [launch modes](LAUNCHING.md). Hosted installations require a revision
containing the hosted launchers. The original `six-slot-snap` repository
is unchanged.

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
