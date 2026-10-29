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
| D10 | Historical limitation; superseded by phases 4 and 6 below | Originally recover assignments from current visible geometry and keep only one active work area. | Phase 4 adds per-monitor state; phase 6 adds separate explicit workspace capture/restore. Normal recovery remains geometry-only and does not relaunch apps. |
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

## Launcher paint decision update

Launcher paint follow-up: retain/cache fonts by DPI until after child disposal,
instead of disposing on replacement. Label/native rendering could still retain
the replaced object, causing the reproduced DrawString "Parameter is not valid"
exception. Actual launcher paint testing is required in both launch modes;
geometry/settings checks alone did not reveal this bug. Preserve the user's
currently active launch mode and data directory when restarting after a fix.

## Close-to-tray decision update

User request supersedes always-visible launcher behavior: closing/minimizing the
bar means hide to the existing tray, not controller exit. Reuse persisted
ShowLauncherTab rather than a second hidden-state flag. Tray Show Layouts bar
and Appearance explicitly restore it; flyout dismissal and refresh must not.
Direct disposal and tray Exit retain their shutdown semantics.

## Phase 5 decision update

Phase 5 implements graphical proportions and portable catalog/shortcut transfer.
Edits are nested drafts, not live OS/window changes. Reuse normalized column
stacks, pair-only row boundaries and a 4% floor; native app fit checks remain
separate. Transfer files have their own versioned DTO, bounded/DTD-prohibited XML,
and no monitor/HWND/appearance fields. Generate fresh IDs for both import modes
to avoid accidental reuse of per-monitor geometry. Hotkeys are opt-in on import;
duplicate conflicts reject the whole candidate before mutation, while actual OS
availability is checked on Save. Replacing a catalog is explicit and requires
re-snapping; no hidden app relaunch/auto-assignment. Undo import is a confirmed
checkpoint restore because it can also discard later catalog/hotkey draft edits.

## Phase 6 decision update

Phase 6 uses executable path/class and optional opt-in exact titles, not saved
HWNDs/PIDs or enumeration order. Ambiguous windows require a choice; disconnected
monitors need explicit fallback, and replaced grids require recapture. Restore
revalidates process instances and never relaunches apps. A separate confirmed
launch action accepts only a full local EXE path, with no arguments; documents,
URLs and sessions are not recreated. Captures are local immediate actions in a
separate backed-up file, excluded from portable layout transfer.

Login startup uses an owned current-user Startup shortcut, preserving compiled
or hosted source/data paths. The short Run-key approach was rejected because
long hosted commands can exceed Windows' documented limit. Startup is never
enabled by installation/tests. Unrelated shortcuts are preserved; removal is
available independently of corrupt workspace data. Windows policy can override
registration. First managed dialogs explicitly show after Load because hidden
process startup was reproduced hiding the first workspace form; cold-start tests
cover settings and workspace UI in both modes.

## Current implementation versus proposals

Phase 8 (`0.3.0-preview.8`) retains executable/mutex compatibility identifiers
while adding product metadata/icon and versioned Source/PortableCompiled packages.
Use explicit input lists and allowlisted manifests/checksums; do not label a
legacy csc build deterministic or a checksum a signature. A clean Git state is
the packaging default; local dirty previews must opt in and record provenance.

Validate candidates before activating pointers, keep prior pointers and recover
them if commit fails. Cached source/compiled artifacts must verify; user data
stays separate. Confirmed rollback/uninstall refuses running owned installs and
linked paths, preserves data/unrelated files by default, and removes only startup
targeting that installation. Retain an ownership marker for data-preserving
reinstall. New installed startup opt-ins target a stable verified launcher so
updates/rollback follow the pointer; migrate existing entries only explicitly.
The installer now ships with companion support/management scripts, not alone.
CI is a read-only check/artifact workflow, not a publication job. Signing, real
account/login, hardware and external beta verification remain production gates.

Phase 7 (`0.3.0-preview.7`) adds keyboard editing through the existing draft
gesture/history model rather than a separate geometry engine. Page Up/Down
selects dividers so Tab retains its normal focus-exit meaning. System high
contrast reuses Windows colors; no OS setting is modified. Resizable dialogs
retain readable control sizes through scrolling and fit to work areas.
Diagnostics use explicit allowlisted fields, not redaction of arbitrary logs or
serialized configuration. Reports are previewed and saved locally, with no
sensitive-field inclusion switch. Source mode and numeric process counters are
safe aggregates; .NET's reported OS version is labeled as reported, not a true
Windows-build claim. Manual screen-reader/contrast/text-scaling and hardware
verification remain release gates; no certification is implied.

2026-10-28 General settings interlude supersedes the Appearance tab and phase-6
placement of startup under Workspaces. Reuse the existing startup backend in
General, keeping immediate OS registration separate from draft Save/Cancel and
making that distinction visible. Keep close-to-tray behavior fixed, rather than
add an exit-on-close choice that conflicts with the user's requirement. Quiet
startup follows saved bar visibility instead of an independent competing flag.
Bar position lock is a backward-compatible optional schema-1 preference; ordinary
clicks and explicit reset remain usable. General and Hotkeys share one flyout
shortcut draft. Local support opens existing files only; sanitized diagnostic
export/theme/accessibility remain phase 7. See docs/GENERAL-SETTINGS.md.

App-specific minimum-size negotiation, per-monitor persistence, a graphical
divider editor, portable transfer, opt-in workspace restore/launch and optional
startup registration are implemented in phases 3–6 within documented limits.
Broader accessibility, release packaging/signing and external beta coverage remain
roadmap work. Do not treat them as user approval
to install services, enable startup, publish releases or change system settings.

2026-10-28 review at `7465c8d`: these mechanisms were confirmed by source
inspection, not a new desktop test run. A retained runtime snapshot is historical
until its process is verified. Context maintenance alone does not authorize
starting the controller, restoring windows or enabling login startup. Phase-7
accessibility changes and sanitized diagnostic export were proposals at that
earlier inspection; the phase-7 decision above supersedes that status.

When changing an established decision, append its replacement and rationale;
retain the old decision's history rather than silently removing it.

## Beta compatibility decision, 2026-10-28

Verified defect: the actual public cfdb2df settings serializer ignores and drops
new launcher position/lock and monitor fields when saving a newer file. Keep
settings version 1 for existing compatible upgrades, but fail into Recovery on
unknown elements/attributes rather than accepting a lossy partial load. Bound
XML size/depth and prohibit DTDs. Existing Recovery suppresses automatic saves.

Installed code selection probes legacy settings types against a snapshot before
activation, rollback or launch. Current code declares preserve-and-recover so
corrupt/future files can still reach its Recovery UI without a redundant startup
compile. The guard fails closed on a missing probe or incompatible legacy file;
restoring a compatible backup is explicit and does not delete the newer backup.
The probe runs trusted selected source with a parent deadline, not in a sandbox.
Per-file integrity protects consistency, not authenticity against a local writer.
Direct old executables and launchers from older bundles remain outside the guard.

Use a local unsigned beta kit and scoped upgrade/soak evidence before broader
testing. No phase 9 or production certification is invented; external/fresh-account,
hardware/login/accessibility and publisher/signing gates remain explicit.

## Restricted-app verification decision, 2026-10-28

Keep new hang/max/aspect coverage in disposable test fixtures rather than change
application behavior without a reproduced defect. Prove an actual blocked UI
with a bounded sent message and OS hung recognition after startup. An ordinary
managed wait was insufficient in the first fixture; a native event wait with a
rescue deadline supplies the intentional block. Never add this wait to product UI.
Run compatibility with the requested controller mode rather than always compiling
its controller during hosted integration. Normalize fixture paths so Chrome gets
an absolute isolated profile. Existing beta.1 packages and app version remain
unchanged; the follow-up test/docs commit is newer validation evidence, not a new
binary release. Freeze-after-probe and physical/privilege tests remain open.

## Existing-installation scope, 2026-10-29

The user explicitly chose the current installation and waived fresh-account/VM
validation. This replaces older roadmap/checklist requirements for a clean account;
it does not assert testing on a second environment. Validate real configurations
and backups on copies, execute the existing owned startup shortcut explicitly,
and observe the current controller without rearranging windows or changing startup.
Do not force a logout, uninstall the daily installation or introduce test corruption.
Keep private snapshots under ignored bin/, outside distribution. Actual logout/login
and multi-day/hardware/accessibility coverage remain distinct observations.

## Live accessibility naming, 2026-10-29

Actual beta.1 inspection exposed an unnamed main tab group and blank Hotkeys
action headers. Fix these with standard AccessibleName/header text in beta.2,
retaining the WinForms provider and existing shortcut behavior. Named header
objects now give the action cells context; avoid a custom accessibility provider
for this small problem. Verify both accessible objects and the installed live
tree, plus keyboard open/Cancel/focus-return routes. Do not equate those checks
with screen-reader announcements or change Windows appearance to claim coverage.

## Equivalent-baseline history reconciliation, 2026-10-29

Read-only fetch proved public cfdb2df is equivalent modulo EOL to local 48b96b2,
which is an ancestor of the tested local implementation. Both descend from 51ffa88
but record the early polish independently, producing textual merge conflicts.
After the equivalence check, use a local history-only ours merge with parents
89613a2/cfdb2df and assert the full tree is unchanged. d056d17 preserves both
histories and makes the public tip an ancestor without a force push or public
mutation. This rationale does not authorize discarding unknown future upstream
changes; every subsequent public tip needs fresh review.

Keep reviewed beta.2 archives frozen at 89613a2. Later review documentation does
not change their provenance; a publication tag for those assets must name that
exact source. No tag/push/release or CI pass is inferred from local preparation.

## Authorized review branch and CI, 2026-10-29

The user authorized continuing with review-branch push, a draft PR and CI. Keep
main and release assets unchanged during this scope. Use normal Git transport
after human Git Credential Manager authorization; connector metadata alone did
not imply working write permission (actual branch/PR writes returned 403).
The authenticated API fallback for draft creation uses the authorized Git helper
credential only in process memory, scoped to this repository; never log/store it.
Preserve full history rather than inventing a substitute snapshot commit.

Initial push/PR CI passed at 430a11a; final-head evidence must be refreshed after
documentation updates. CI upload is an ephemeral unsigned workflow artifact, not
a tagged GitHub Release or a replacement of the reviewed 89613a2 assets. Local
desktop verification and unverified manual limits remain distinct from CI coverage.

## Beta.2 publication, 2026-10-29

The user explicitly authorized review/merge, tagging and unsigned prerelease
publication. Preserve history with PR #1's normal merge; tag exact frozen source
89613a2 rather than the newer merge/docs head. Upload the two reviewed ZIPs and
checksum/provenance sidecars as a draft, verify server digests, publish as a
prerelease and independently verify all public download hashes. These steps are
complete. Never move this tag or overwrite these release bytes to refresh docs.
Future changes require a new version and relevant validation. This beta's known
manual/placement risks remain disclosed; publication is not production certification.

## User priorities after core acceptance, 2026-10-29

The user considers the proposed #1 reset behavior already satisfactory. Treat
additional #2 proportion conveniences and #3 app assignment as optional for other
users, rather than priorities for this user's workflow. Current preference
persistence/workspace tools are implemented; do not conflate them with new presets
or assignment rules. Recovery (#4) is mandatory; simplification (#5) is wanted;
a very modern manager UI (#6) is explicitly requested. Prioritize recovery,
simplification, then modern UI, preserving validated six-slot behavior. This
records scope, not completion or a selected framework/runtime migration. See
docs/NEXT-MILESTONES.md for acceptance criteria and the freeze-after-probe repro.

## Windows 11 visual direction, 2026-10-29

The user clarified modern as matching Windows 11's clean UI and following system
colors or selecting Light/Dark. Require three appearance modes: System default
with live Windows appearance/accent following, plus explicit Light and Dark.
Keep high contrast, keyboard/DPI support, cohesive native-looking surfaces and
draft Save/Cancel without geometry changes. This specifies the visual/behavioral
target, not a selected framework or completed theme implementation. Current beta.2
does not provide the three-mode selector. Recovery remains the first priority.

## Implement appearance now, 2026-10-29

The user's explicit request to implement themes/UI now supersedes the earlier recovery-first execution order for this scoped change. Keep framework/source-hosting compatibility: use shared WinForms palette/navigation/button components with documented Windows 11 DWM hints. Add one enum preference, System by default; preview only the settings draft and inherit it in owned dialogs. Apply globally only after successful persistence. Preserve high contrast, native providers and geometry. Cache colors outside paint; remove static system-event subscriptions on disposal. This delivers themes/basic navigation refresh, not the planned six-slot overview or freeze-after-probe recovery fix. No public release/push is implied.
