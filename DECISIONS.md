# Decision record

Recorded 2026-10-26 from established user requirements and inspection of the
implementation at `48b96b2`. These records capture existing choices; proposals
are explicitly labeled. Consult actual source before relying on them.

| ID | Status | Decision and reason | Consequence / evidence |
| --- | --- | --- | --- |
| D01 | Established requirement | Preserve the original six-slot repository; use `Brandonr91/windows-tile-studio` for the configurable successor. | Never push successor changes to `six-slot-snap`. Prototype class/EXE/mutex names remain for compatibility. |
| D02 | Established requirement, implemented | Default to a 3-column/2-row grid on the user's 5K2K desktop; shared widths, independent horizontal boundaries. Every height adjustment must affect only the adjacent pair. | `LayoutModel.Adjust`, `BeginResizeSession`, `ApplyResizeFrames`; geometry and six-window tests. Width changes must preserve all existing heights. |
| D03 | Implemented scope | Use rectangular column stacks with independently weighted rows, not arbitrary overlapping zones. | Limits: 12 profiles, 4 columns, 4 rows per column, 16 zones, 4% segment floor. A freeform builder would require a new adjacency model. |
| D04 | Established requirement, implemented | Supply a custom persistent native-like selector instead of extending Windows' own Snap catalog. | Nonactivating Forms panel retains the original target. Selection, outside click, Escape, toggle or switching apps dismisses it. No drag-to-zone integration. |
| D05 | Implemented | Use the installed .NET Framework compiler, Windows Forms and Win32 APIs without external packages. | Small local source build; Windows-specific. Preserve physical-pixel coordinates and DWM frame compensation. |
| D06 | Implemented | Isolate resize axes using actual starting bounds and separate affected-slot sets, rather than reapply the whole grid on every tick. | Fixes flanking-height resets and simulated native group propagation. Widths may be shared; unrelated heights must remain untouched even when saved ratios are stale. |
| D07 | Implemented | Register navigation keys only while the nonactivating flyout is open; roll back the entire navigation set if any key conflicts. | Mouse selection remains available. No low-level keyboard hook. Escape has its own registration/polling fallback. Keep cleanup on every exit path. |
| D08 | Implemented | Offer PowerShell 5.1 STA in-memory hosting alongside optional compiled mode. | No standalone app EXE required in hosted mode; PowerShell remains running and startup compilation still occurs. Restart the host after source edits. |
| D09 | Implemented | Install versioned source separately from persistent user data. | Downloader writes `versions/`, `current.json`, launcher and `data/`; validates archives and confines staging cleanup. No silent compiled fallback for explicit Hosted mode. |
| D10 | Implemented limitation | Recover assignments from current visible geometry, keep only one active work area. | No saved app identities, no workspace restore, no per-monitor layout database. Those require future design/migration work. |
| D11 | Established requirement | No AI service at runtime; changes and startup behavior remain local and explicit. | No telemetry or automatic startup. Network use belongs to the opt-in source downloader. No scheduled development was created. |
| D12 | Process requirement | Verify the source and desktop behavior separately; preserve authoritative context across sessions. | Read AGENTS and context records after compaction; update docs with meaningful changes. Distinguish test evidence from planned or unverified behavior. |
| D13 | User requirement, implemented 2026-10-26 | Move the persistent Layouts bar from top center to near the taskbar clock and make it draggable. | Default above the primary taskbar's right end, outside the clock's controls. A normal click opens the selector; a drag moves only the bar and saves coordinates. Manual initial positioning and screen clamping prevent default-position overrides/offscreen restoration. This supersedes the original top-center launcher placement. |
| D14 | User requirement, implemented 2026-10-26 | Give the full manager its own tray identity and make legacy installations yield to it when opted in. | Reuse the existing tray entry with a blue six-tile icon and Windows Tile Studio name. A reversible per-folder redirect preserves original bytes, closes the exact legacy process and redirects old launch paths. No global blocker/service, no original repository edits, and no startup setting changes. |

## Resolved failures and tradeoffs

- Phase 3 (`0.3.0-preview.3`, 2026-10-27) treats advertised minimum sizes as
  constraints, verifies actual placement before assignment, and pins one height
  boundary per gesture. A background probe keeps size messaging off the UI loop.
  Its DPI context must be set/restored explicitly; tests reproduced drift when
  worker GetWindowRect coordinates were virtualized while DWM bounds were physical.
- Timeout/rejection must give feedback and stop repeated forcing. Rollback is
  best-effort and may require explicit re-snap. Closed windows release slots;
  app restarts do not authorize implicit adoption. Representative compatibility
  coverage and remaining limits are recorded in `docs/COMPATIBILITY.md`.

- Phase 1 of the user's phased polish request groups bar controls in Appearance
  and uses one visible Windows Tile Studio identity/version. Internal class,
  mutex, receiver and EXE names stay compatible. Appearance/name/hotkey-only
  saves do not reflow the grid. Settings reset is draft-only; tray bar reset
  applies immediately and is separate from resetting layout proportions.

- Initial display assumptions were corrected to physical 5120 × 2160 at 125%
  scaling, rather than treating the scaled desktop dimensions as panel resolution.
- Column-specific math alone did not isolate heights: whole-grid application and
  settling could reset flanking windows. The fix captures actual frames and scopes
  axes, including corrections for native group movement; do not regress to full-grid
  live reflow.
- A modeled height change selects at most one adjacent pair per sample. The
  controller accumulates affected slots during a drag. More complex app-driven
  edge changes across samples need additional compatibility tests, not an assertion
  that every possible native timing sequence has been verified.
- Startup previously failed when a configured permanent shortcut was unavailable.
  Phase 2 supersedes that behavior with a usable tray/Recovery settings route,
  original-file preservation and no silent persisted fallback. Actual binding
  snapshots are restored after failed writes, including an empty startup set.
- Phase 2 safeguards remain local to the settings draft: confirm destructive
  grid changes, allow undo before save, and ask before discarding on close.
  Legacy restoration must remain independent of the preferred manager folder.
- Hosted binaries are not code-signed releases. Archive hashes record downloaded
  bytes and version identity; they are not a substitute for signing.

## Phase 4 decision update

Phase 4 supersedes D10's single-work-area limitation: per-monitor sessions retain
selected layouts, geometry and transient assignment arrays. Windows device-interface
paths (GDI fallback) provide identity without guessing across port/driver changes.
Schema 1 adds optional monitor snapshots, retaining old-file loading. The current
active geometry also mirrors into the shared catalog for existing tools. Names,
grid shapes and hotkeys stay shared; proportions are per-monitor. Draft saves
merge latest runtime snapshots and preserve untouched concurrent geometry.

Display notifications are debounced, not polled. Work-area changes reflow only
eligible same-monitor assignments with verified placement; disconnect releases
HWND ownership and reconnect performs optional geometry recovery. No automatic
cross-screen return, app relaunch, service or startup changes. Single-display
native notification tests plus pure topology cases do not establish physical
mixed-DPI/docking/sleep coverage. See [display checkpoint](docs/DISPLAYS.md).

## Prior-phase audit decision update

Advance drag history only to the source rectangle used for model calculation,
never a newer rectangle read after synchronous neighbor placement. The latter
could consume unapplied movement and incorrectly release the source. The new
neighbor-triggered regression fails on d8852bf and passes with this correction.
Keep inconclusive supplemental harness runs separate from accepted verification;
see docs/PHASE-AUDIT.md for the matrix and physical/manual coverage gaps.

## Proposed, not yet implemented

App-specific minimum-size negotiation and per-monitor persistence are implemented
in phases 3–4 within documented limits. A graphical divider editor,
optional app/workspace restoration, optional login startup, and
release signing remain roadmap candidates. Do not treat them as user approval
to install services, enable startup, publish releases or change system settings.

When changing an established decision, append its replacement and rationale;
retain the old decision's history rather than silently removing it.
