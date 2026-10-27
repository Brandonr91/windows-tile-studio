# Phases 1–4 regression audit

## Follow-up: actual launcher painting

After this audit, the user reported a red-cross launcher and "Parameter is not
valid" dialog. The previously explicit launcher coverage gap hid a real defect:
the DPI handler disposed a replaced font while Label.OnPaint still retained it.
A disposable real-message-loop test reproduced the original Graphics.DrawString
stack. Caching/reusing fonts by DPI until after child disposal fixes the failure.
Children receive the current DPI font explicitly. No error suppression was added.

New `Build.ps1 -LauncherUi` and Windows PowerShell 5.1 -STA
`tests/LauncherUiTests.ps1` pass actual painting through 70 synthetic DPI changes,
correct font sizes, bitmap rendering, stable GDI count and disposal. The fixed
bitmap was visually inspected. `Build.ps1 -Test -LauncherUi -HostedIntegration`
also passes the full existing suite. This updates paint coverage only; physical
mixed-DPI crossing, live mouse drag and confirmation dialogs remain separate
manual checks. Original audit evidence below is historical, not proof painting
was verified then. See HANDOFF for the currently active compiled/data location.

## Original audit

2026-10-27. Audited local preview `0.3.0-preview.4`, starting at `d8852bf`.
One resize defect was reproduced and corrected. This audit does not add phase-5
features, publish source, enable startup or change Windows display settings.

## Reproduced defect and correction

The first compiled full suite passed. The following hosted run lost an assignment
during a shared-width adjustment. A diagnostic rerun reproduced a lost final
height change: the source baseline ended at 1445, actual frame ended at 1945,
and the bookkeeping frame had already advanced to 1945 without recording a
resize attempt. The source was then classified as moved out and released.

`UpdateDrag` sampled one source rectangle for calculation, then read it again
after updating neighbors to advance `lastDrag`. A source change between those
reads could be consumed without reaching the model. The correction advances
`lastDrag` only to the sampled rectangle actually used in that update. A later
movement remains available for the next sample. Pair-only height ownership and
existing constraint handling remain in place. Releases now log the baseline,
last frame and actual frame to the existing local status log.

The added regression moves the source a second time from a neighbor's
SizeChanged event during synchronous controller placement. It checks the final
source edge, shared model and all six actual frames. The updated test fails
against an isolated checkout of pre-fix `d8852bf` with:
`Movement during neighbor placement was consumed without being applied`.
It passes in both compiled and PowerShell-hosted modes with the correction.
This before/after reproduction distinguishes the fix from a lucky rerun.

## Function coverage

| Phase / function | Audit outcome and scope |
| --- | --- |
| Base geometry and profiles | PASS: default 3 x 2, all 1–4 column/row shapes, mixed row counts, negative origins, coverage/adjacency, profile validation, legacy import, XML roundtrip/backup and malformed-file preservation. 186297 pure checks. |
| Base snapping and linked resizing | PASS in compiled and hosted desktop suites: six disposable windows, shared widths from both rows/edges, each column's independent height boundary, stale flanking-height preservation, width-only height preservation and native-style extra group events. Newly reproduced lost-sample race corrected and regression covered. |
| Selector and shortcuts | PASS: persistent nonactivating flyout, preserved original target, actual zone click, toggle/Escape, actual Right/Enter and Tab/Shift+Tab, temporary key cleanup/conflict rollback. Added direct navigation checks for all four directions, boundary clamping and forward/reverse layout wrap; direct calls do not establish physical key coverage for every combination. |
| Phase 1 Appearance/settings | PASS: settings draft show/hide/reset save/cancel, UI name/grid editing, geometry equality checks excluding names/shortcuts, monitor banner non-overlap. Existing source separates launcher reset from layout reset. Native no-reflow for every appearance/hotkey combination was not independently exercised in this audit. |
| Tray and movable launcher | Source inspected: tray name/owned icon/About/reset/exit entries; drag threshold/click distinction, saved coordinates, DPI dimensions and clamp. Pure saved-position and negative-origin/scaling checks pass. Earlier actual mouse-drag verification remains historical; live drag/About/reset confirmation was not newly verified here. |
| Phase 2 edit safeguards | PASS: confirmed/rejected grid replacement, grid undo, failed-save draft retention, discard/keep-editing close branches; isolated startup-conflict and corrupt-file Recovery cases preserve original bytes and keep the controller alive. Tests inject confirmation answers; live tray reset confirmation remains manual. |
| Hotkey recovery | PASS: conflicts report failure and restore actual prior binding snapshots, including an empty recovery set, and keys release on exit. Blocked settings-write preservation covered by pure tests. |
| Legacy supersession | PASS in a disposable legacy installation: exact selected process closes, original hash preserved on enable/re-enable, redirect starts preferred source, disable restores bytes, standalone hash-checked restoration works after manager source removal. Real legacy installation preferences were not changed. |
| Source-only hosting and installer | PASS: local-source in-memory PowerShell compilation, installed-source validation, paths with spaces/apostrophes, repeated installs/data preservation, unsafe archive rejection, explicit older-source fallback and unrelated-folder protection. Public network download was not rerun for unpublished code. |
| Phase 3 app compatibility | PASS in both full modes' compatibility subcases: advertised minimums, oversize rejection without assignment/movement, bounded delayed sizing query, 3/4-row divider sides, pinned opposite-edge gesture, corners, constrained no-op, minimize/maximize, close/recreate, borderless/tool frame and isolated Chrome. Existing browser profiles/windows were untouched. |
| Phase 4 monitor ownership | PASS: pure two-display state simulation for independent profiles/ratios/assignments, negative origins/different sizes, disconnect/reconnect, restart without persistent HWNDs, work-area flags, removal/shared shape, XML migration and concurrent draft merge. |
| Phase 4 notifications | PASS in compiled and hosted suites: live inventory and synthetic display/work-area/resume messages retain six frames/assignments on unchanged hardware. No OS topology or power-state change. |

## Verification records and limits

Accepted final run: `Build.ps1 -Test -Integration -HostedIntegration` PASS after
the correction. Windows PowerShell 5.1 `tests/SupersessionTests.ps1` and
`tests/LauncherTests.ps1` PASS. Full-suite compatibility includes short CPU/GDI
observations, not universal performance promises. Final hosted corner sample:
20 updates, 1511 ms including deliberate pumps/settling, 125 ms controller CPU;
one-second idle sample 0 ms; no growing GDI count.

An experimental supplemental in-process launcher/controller harness did not
complete and was stopped. It was removed and is not counted as a passing test
or an isolated product defect. The fixture's first direct navigation checks were
moved into the established integration suite and passed there. Launcher live
mouse/confirmation checks remain explicit manual follow-up.

Available hardware is still one 5120 x 2160 display at 125%. Actual mixed-DPI
crossing, docking/unplug/reconnect, taskbar/scale changes, real sleep/wake,
elevated/fully hung apps and arbitrary maximum/aspect restrictions remain
unverified. Synthetic native events do not prove every physical dragging timing.
See [display limits](DISPLAYS.md) and [compatibility limits](COMPATIBILITY.md).

Reports outside Git in `outputs/`:

- `windows-tile-studio-all-phases-regression.txt`: initial compiled pass and hosted failure.
- `windows-tile-studio-all-phases-hosted-recheck.txt`: diagnostic reproduction.
- `windows-tile-studio-all-phases-corrected-regression.txt`: accepted final full run.
- `windows-tile-studio-all-phases-before-fix.txt`: expected regression failure on pre-fix source.
- `windows-tile-studio-all-phases-legacy.txt`, `windows-tile-studio-all-phases-launchers.txt`: accepted source/legacy checks.

The daily source was updated with the correction, then restarted with existing
data. All six pre-audit assignments recovered, recoveryRequired=false. A pre-audit
settings/runtime snapshot is in `work/phase-audit-20261027-134223`; phase-4 source
is recoverable from Git `d8852bf`. Do not treat stored PIDs/HWNDs as durable IDs.
