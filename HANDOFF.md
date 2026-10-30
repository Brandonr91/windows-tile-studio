# Handoff

## Beta.7 deployed and published — 2026-10-30

User explicitly authorized updating the existing installation and then publishing beta.7 downloads. Both are complete. Public unsigned prerelease: https://github.com/Brandonr91/windows-tile-studio/releases/tag/v0.3.0-beta.7 . Frozen clean source 19ea78e3da85aa195eb41eb5ca03b7ff6deca236; annotated tag edd6493d59bbd0303cc8b712d9c2c937143bc967. docs/RELEASE-BETA-7.md owns hashes, provenance, validation and deployment. Preserve beta.2/beta.6/beta.7 immutable tags/assets. Earlier local-only/pending statements below are historical.

Exact source CI 41118442748 passed. Clean Source/PortableCompiled assets pass manifests/repeatability and isolated ReleaseTests; actual installed Hosted/Compiled/rollback and Setup GUI checks pass. Four server asset digests verified before publication, four anonymous public downloads hash-verified after publication. No signing credentials supplied; release is unsigned.

Daily existing bin/SixSlotSnap.exe now is the exact release EXE, SHA4c20b75f4c49885eaad1433345b5f3176bee6a06ff2beb55f45eca774be15259. Actual process/runtime verifies beta.7, same six handles/outer frames, byte-identical settings and original Startup link, hidden bar, placementReady=true, no Recovery, two owned helpers and notificationsQuiet=false. Current run logs have zero ERROR entries/zero popups; no matching Windows app-crash event in preceding 30-minute sample. Hosted delivery now mirrors seventeen sources AND Start-Hosted.ps1; data remains separate/intact. Private snapshots/evidence are bin/beta7-daily-backup, bin/beta7-deployment.json and bin/beta7-public-verification. Do not ship them.

Private deployment verifier initially used a runtime-file read without delete sharing and raced atomic replacement. Shared ReadWrite/Delete stream completed verification; no app crash or app source change. Temp exact-package fixtures: WtsR7Final/29fcf7fe623c48849f82b33698d0eed0, WtsR7Launch/8c68e809f14d4da7844eaea8a714f9c7, WtsSetupUi/8a56cb44d64a4184a717c14ab35e35ef.

Next: normal use and the user's next request; no pending publication/redeployment or new feature expansion required. Signing requires publisher credentials. Broader hardware/DPI/power/privilege/assistive-technology and long-duration limits remain. Consult latest context plus actual Git/process state after compaction; do not resume historical pending work.


## Build, test-evidence and log amendments complete locally — 2026-10-30

User authorized the three log-audit follow-ups and asked what comes next while validation was active. All three are implemented and checked in the beta.7 candidate. Read docs/BUILD-AND-LOG-RELIABILITY.md. Earlier quiet-only commit 48796a9 was pushed on request; these new amendments have not been pushed or released. Current daily/published beta.6 stays unchanged.

Build.ps1 copies app compiler inputs/intermediates to short owned staging, scopes/restores TEMP/TMP and atomically publishes only success. Previous csc CS1619 reproduced against a 218-character disposable project root; updated code succeeds there. Formal BuildPathTests pass initial/repeat build, broken compile, locked publication, too-deep output/temp rejection and prior-EXE/environment retention. CI now includes this regression (configured locally, not remotely run for this amendment). Classic source/output bounds remain; no arbitrary-long-path claim.

IntegrationTests now uses tests/TestRunRecord.cs: fresh per-run directories, source revision/dirty/mode/version/timestamps, atomic latest-run.xml, explicit running/pass/fail, older-completion protection and cleanup included before final status. Unit checks preserve old failures and prevent interrupted/new runs from inheriting an old PASS. Historical top-level errors remain evidence only. Chrome test profiles use short unique TEMP paths; stderr is retained per run. Final compiled/hosted Chrome stderr contained only its existing nonfatal new-tab diagnostic (145 bytes), no long-path warning, and no console spam.

LocalStatusLog in Notifications.cs adds UTC severity/outcome/version/controller-run fields. Runtime logRun correlates locally; multiline exceptions stay one escaped line; I/O/access failures do not throw from the logger. Recovery/fit notifications retain text and quiet/default semantics, fatal startup UI remains. No settings schema/export/telemetry expansion.

Verification: 198571 geometry/settings/regression checks pass. Compiled startup conflict/corrupt/future checks pass with structured log context/zero popups; both compiled and actual PowerShell-hosted focused compatibility suites pass, including independent resizing and min/max/aspect/timeouts. Final run records: bin/log-amendments/startup-recovery-data/runs/20261030T194600081Z-d1317f45; compatibility-data/runs/20261030T194343955Z-0ff3f1e1; hosted/runs/20261030T194414813Z-08f70ddf. Startup was repeated against the final cleanup-completion bookkeeping; legacy failed run remains recorded separately. Earlier timing failure read initial state before warning publication; fixture now waits for the warning counter/context. ReleaseTests passed against an isolated dirty preview with the final app/build changes under TEMP/WtsLogFix/cbace0fba5e34056a1f4878edad625d1; subsequent changes were test bookkeeping/Chrome diagnostics/docs. This is local validation, not published artifact provenance.

Actual daily controller restarted normally after test intervals and verified beta.6, six same handles/outer frames, original Startup shortcut bytes, placementReady=true/no Recovery. Private backup bin/log-amendments-daily-backup; no real app movement or startup rewrite. Next: review/commit these amendments locally; push or deploy only when requested. Do not repeat completed suites without a new change/failure; beta.2/beta.6 release assets are immutable.


## Local notification follow-up completed — 2026-10-29

User supplied five warnings from tests. Exact messages match isolated invalid-settings and compatibility fixture logs; daily beta.6 runtime reports no Recovery and daily log contains none of these warning strings. Notifications.cs/NotificationDelivery separates always-on local logging from optional toast delivery. Local beta.7/file 0.3.0.15 adds --quiet-notifications and hosted -QuietNotifications, plus aggregate runtime counters. Native Integration/PlacementRecovery/Workspace fixtures opt in. No persistent setting, Windows-wide suppression, fatal startup suppression or real startup rewrite. Daily/published beta.6 stays unchanged; no push/publication requested.

198564 geometry/settings checks plus new injected quiet/default notification routing checks pass. Focused corrupt/future/conflict startup fixtures pass: quiet=true, zero popup calls and warning log retained, Recovery/original bytes intact. Compiled and PowerShell-hosted focused compatibility checks pass with explicit quiet=true, zero popup calls and retained warning logs, including hung/slow/min/max/aspect/size-limit outcomes. See docs/NOTIFICATIONS.md. Daily stopped normally in an isolated test interval and restored in finally; private backup bin/quiet-notifications-daily-backup. Daily beta.6 is restarted and verified by actual process/runtime: six same handles/outer frames, original Startup bytes, no Recovery and placementReady=true. Test evidence is bin/quiet-notifications/{startup-recovery-data,compatibility-data,hosted}; logs remain private/local. Next: follow the user's next request. No further check/redeployment/release is required merely to complete this test-noise fix. Current candidate is local and unpublished; published beta.6 assets are unchanged.


## Beta.6 completed, deployed, pushed and published — 2026-10-29

All five selected items are complete within the documented scope. Public unsigned prerelease: https://github.com/Brandonr91/windows-tile-studio/releases/tag/v0.3.0-beta.6 . Frozen source b052f8377a73105ef8e9d75c9a1400ad02d88a07; annotated tag 8da13c50bdece76c590ffd0f9406af687269c681. See docs/RELEASE-BETA-6.md for exact artifact hashes/provenance, test evidence and deployment record. Do not overwrite either published beta.2 or beta.6.

Final clean packages pass ReleaseTests, actual installed Hosted/Compiled/rollback and Setup GUI install/verification. Public assets independently downloaded anonymously and hash-verified. Source commit CI 40984354491 passed before publication. These supersede older pending checks below. An initial fixture hard-coded General tab position; fixed to select by name. A Setup test script-scope flag was fixed to share a result object. Neither initial failed run is counted as a pass.

Daily uses the exact released compiled EXE at existing bin/SixSlotSnap.exe with existing data, verified six same handles/outer frames, original Startup bytes, hidden bar, no Recovery and placementReady=true. Last PID44000 is transient. Hosted delivery matches sixteen sources and Start-Hosted.ps1. Private snapshots/evidence remain bin/studio-next-*, bin/beta6-deployment.json and temporary WtsR6Final/WtsR6Launch/WtsSetupUi folders. Zero sampled app-specific crash events over the 40-minute local sample. No new startup/menu registration on the daily installation; GUI/installer tests use temporary folders and disable real menu/launch.

Next: use the manager normally and follow the user's next request. Signing is pending publisher certificate/service credentials, not completed. Broader hardware/privilege/assistive-technology/long-duration observations remain limited. No need to repeat passed suites without a new change/failure. Consult these documents and actual Git/process state after compaction; do not resume older pending publication tasks.


## Beta.6 manager expansion: local implementation, 2026-10-29

User authorized live dashboard, embedded visual-first editing, UI polish and distribution improvements, then a new prerelease. Beta.6/file 0.3.0.14 implements these in sixteen C# sources plus Setup.cmd/Setup.ps1. See docs/LIVE-MANAGER.md and docs/SETUP.md. Signing requires publisher credentials; the release remains unsigned. No first-run walkthrough was selected.

Live status is injected from the controller, visible-page refresh only, no title collection/persistence. Retry uses a displayed token, native property lease/process generation and display/layout/slot checks. No app relaunch or occupied-slot replacement. Embedded editing reuses the visual engine, synchronizes the draft and keeps one grid undo per pointer gesture; Advanced exposes numeric weights. Shared rounded controls extend to flyout/editor/workspaces.

Setup wraps the safe per-user installer, repacks verified manifest entries only, supports Hosted/Compiled and optional stable Start menu links. Conflict/WhatIf/owned uninstall/data retention remain. Real startup is unchanged. Daily beta.5 was stopped normally for isolated tests and restarted in finally; private snapshots stay under bin/studio-next-daily-backup. Beta.5 push is complete at 5af442b; older no-push statements below are historical.

Verification: 198564 geometry checks, compiled/PowerShell5.1 STA EditorUi/WorkspaceUi/AccessibilityUi/LauncherUi pass. Embedded pair-only drag/one-gesture Undo/Advanced and live snapshot/token/disconnection UI checks pass. Native workspace tests verify actual assignments, explicit retry, expired token/process generation/occupied-slot refusal. Compiled/hosted placement-recovery suites, full integration in both modes and focused startup Recovery pass. First integration used obsolete numeric tab positions; corrected fixtures select General by name. Offline LauncherTests/ReleaseTests pass, including setup payload, isolated Start menu add/repeat/remove/conflicts, install/update/rollback/uninstall and source EXE exclusion. Setup GUI fixture installs/verifies Compiled from a local archive with launch/Start menu disabled; source-hosted setup payload remains EXE-free.

Next: inspect final package provenance, deploy to existing daily bin/hosted delivery preserving window bounds/settings/Startup bytes, then commit/push, exact-head CI and new beta.6 publication with verified checksums. Preserve beta.2 assets. Broader hardware/privilege/assistive-technology/long-duration coverage remains limited. Avoid repeating completed tests without a change/failure.


## Completed locally: bounded placement recovery, 2026-10-29

User authorized recovery hardening after the beta.4 push. Local **beta.5** (file
0.3.0.13) is implemented, tested and deployed to the existing daily compiled path.
There are fifteen C# sources. Read docs/PLACEMENT-RECOVERY.md and actual source.
No GitHub push/tag/release was authorized for this new change; beta.2 assets remain
unchanged. The earlier beta.4 push remains the public main until a new push.

Implemented: one active plus one spare hidden, owned helper; job kill-on-close;
current-account pipes with real peer/PID/thread/product-version/window-lease checks;
250ms per-call placement/window-state bound; verified helper cancellation; uncertain
assignment release; explicit retry; no async queued SetWindowPos or user-app kill.
Source hosting starts two fresh in-memory PowerShell helpers and writes no app EXE.
Geometry roots serialize reentrant WinEvents/timers, preserve modal UI loops and
keyboard selection, and keep the source-frame bookkeeping/independent heights.

Evidence:
- Baseline beta.4 SHA61c27e5272130946efa64b6e7e01cd25659f2b6b8d168febec875a805112655f:
  successful size reply followed by freeze blocked queued settings; move arrived
  after resume. A sent-message ping alone did not prove UI responsiveness.
- Final compiled/hosted PlacementRecovery pass query, pre-frame, post-frame,
  restoration, linked resize and parent-death cases. No additional move after
  resume; no false assignment; five other frames unchanged; explicit retry and
  helper cleanup pass. A frame committed BEFORE timeout can remain changed.
- Full compiled Integration and full HostedIntegration PASS in
  bin/recovery-final/{integration-data,integration-hosted-data}/integration-result.txt.
  Both include six-slot pair-only heights, width/height isolation, concurrent-source
  motion, keyboard/flyout cleanup and compatibility in the requested mode.
- WorkspaceIntegration and first hidden-dialog checks pass; StartupRecovery passes
  all conflict/corrupt/future cases. Compiled geometry198564 plus AccessibilityUi,
  EditorUi, WorkspaceUi and LauncherUi pass; all four hosted UI suites pass.
- LauncherTests passes source-only hosting/serialization and isolated installer paths.
  Offline ReleaseTests and installed Hosted/Compiled/rollback smoke pass. Source/host
  files in the tested package were hash-compared to current application files.
  The first deep test staging path exceeded legacy compiler temporary-path limits;
  short temp test roots passed. Do not claim arbitrary long-path installation support.
- Native regressions found/fixed STA reentry and keyboard callback routing. The
  blocked-app test now publishes its ready marker atomically to remove a file-read
  race. Helpers are filtered from test-controller fallback discovery; transient C#
  compiler children are excluded from helper-count assertions. Earlier failed runs
  are diagnostic history, not final passing evidence.

Deployment verified against actual process/runtime: daily repo bin/SixSlotSnap.exe,
PID42608 (transient), beta.5, placementReady true, no Recovery, bar hidden, exactly
six same handles and unchanged outer bounds versus the pre-test snapshot. Original
Windows Tile Studio.lnk bytes unchanged. Exactly two owned helper children; no
stray test/hosted controllers. Existing hosted delivery has matching fifteen source
files AND updated Start-Hosted.ps1; its separate data is retained. Live aggregate
result is ignored bin/recovery-deployment.json; private snapshots/backups stay in
bin/recovery-final-daily-backup and bin/recovery-* and must not ship.

Observed helper footprint: ~37MiB combined private bytes for compiled helpers;
~167MiB for hosted helpers at fixture startup. The daily compiled main and helpers
used 0 sampled CPU milliseconds over a 2-second idle interval. This is bounded
local evidence, not a multi-day resource guarantee. Zero sampled app-specific
Application Error/.NET Runtime crash events over the preceding two hours.

Isolated package evidence: temp WtsR5/4c40f94200414a1382084cad4d02e9b8 and
WtsR5Native/9daac4dca0b5454b877c3e57de926975. Packages contain dirty-checkout
provenance from local validation; they are not public release assets or a replacement
for frozen beta.2. Source package app hashes match the final implementation; later
context/test-only updates do not change the deployed native implementation.

Next: normal daily use/feedback, then an explicitly requested review/push/new beta.
Keep the old baseline and frozen beta.2 assets. Parent-side OS/DWM calls, app-owned
already-running/deferred code, physical multi-monitor/DPI/power, privilege/competing
manager and long-duration use remain broader manual coverage. Do not promise atomic
rollback of code the target already executed. Re-verify live ownership before future
native tests; normal quit stops only the manager and its owned helper jobs.


## In progress: bounded native placement, 2026-10-29

User authorized the recovery-hardening step after the beta.4 push. Active work is
local beta.5/file 0.3.0.13, currently UNCOMMITTED and NOT DEPLOYED to the daily bin.
Daily remains the retained beta.4 EXE; native batches quit/restart it normally in
finally blocks. Do not push/release or replace published beta.2 assets implicitly.

Read docs/PLACEMENT-RECOVERY.md and actual source. New fifteenth source file
PlacementBroker.cs uses two job-contained helpers, account-restricted local pipes,
actual peer/PID/thread/version/window-lease checks, 250ms per-call native placement
and state-change bounds, verified helper termination, warm-spare promotion and
explicit retry. Hosted workers use Start-Hosted Action PlacementWorker in memory.
Baseline beta.4 freeze-after-size-reply was proved: queued settings blocked and a
move arrived after resume. Both fixed delivery modes passed query/pre-frame/
post-frame/restore, no late additional movement, explicit retry, linked resize,
helper normal cleanup and parent-death cleanup. A precommitted frame can remain
changed; no atomic rollback of arbitrary app code is promised.

Broader tests exposed reentrant WinEvent callbacks during native/STA waits,
corrupting pipe sequencing. Confirmed with a defensive broker assertion. Current
controller adds geometry-depth/posted replay, skips nested timer ticks, preserves
modal-dialog message loops and refuses synchronous public saves/restores while
busy. Owned backend guard remains defensive. Latest compiled PlacementRecovery
and Compatibility pass with no reentrant errors. Full compiled/hosted batches
are being repeated against the final serialization change. A new compatibility
assertion checks the log for reentrant placement errors.

Current batch session is a tool-session detail, not repository state. Output under
bin/recovery-final includes WorkspaceIntegration, hidden-dialog, hosted placement
races, full Integration/HostedIntegration and StartupRecovery. Check actual
result files and process ownership before proceeding. Earlier failed/incomplete
native runs must not be counted as final passes. Source-hosted child discovery
now filters live SixSlotSnap/PowerShell children, excluding the temporary C# compiler.
InstalledLaunchTests fallback discovery excludes placement helpers.

Next exact steps: finish full native batch; run final compiled/hosted UI and
launcher checks; package/isolated installed-launch checks if relevant; measure
helper memory/idle cost; deploy tested EXE plus all fifteen hosted sources AND
updated Start-Hosted.ps1 with real startup bytes/data preserved. Verify all six
handles/bounds, bar-hidden and no Recovery. Final context/changelog/roadmap must
record actual outcomes, helper overhead and remaining limits. Keep original
backup/runtime/config snapshots local under ignored bin/recovery-*.


## Latest implementation: reference-inspired manager, 2026-10-29

Local beta.4 (file 0.3.0.12) now implements the user's dashboard reference direction:
rounded navy/white surfaces, icon rail, large numbered draft preview, thumbnail
layout library, grouped numeric inspector and visual theme cards. General/Share
and Hotkeys use matching card surfaces. System/Light/Dark, Windows accent following,
high contrast, Save/Cancel and source-hosted delivery are retained. DESIGN.md owns
visual tokens/composition; no native placement/resize logic or schema was changed.

Fixed a reproduced recomposition regression: reparenting initialized native
controls could mark a fresh settings draft dirty, leading to a spurious discard
prompt and blocking a normal recovery-controller quit. Clear initialization-only
dirty state at constructor completion; real subsequent edits still prompt. New
pristine normal/Recovery-form close assertions fail before and pass after this fix.
Build -StartupRecovery adds a focused native guard using the existing conflict,
corrupt and future-field startup fixtures; it requires the daily controller stopped.

Verification this turn:
- 198564 geometry/settings checks; compiled AccessibilityUi, EditorUi, WorkspaceUi
  and LauncherUi pass. New coverage: visual theme-card selection/selector sync,
  accessible selected description, 16-zone preview at three sizes, pristine close.
- PowerShell 5.1 STA hosted AccessibilityUi/EditorUi pass against the final fix;
  WorkspaceUi/LauncherUi passed during this composition change. Initial hosted
  keyboard Tab check failed once, then passed on retry. Actual speech/OS theme
  changes/mixed-DPI hardware remain unverified.
- Final compiled native Compatibility passes hung/max/aspect/timeout/minimum,
  pair-only 3/4-row/corner resize, lifecycle and isolated Chrome cases. Focused
  native StartupRecovery passes all three original-file-preserving cases.
- Full Integration is NOT a clean pass for this turn: earlier attempts encountered
  a disposed fixture, then the reproduced dirty-prompt issue; after that fix the
  full run reached the flyout and recorded outside mouse/foreground input that
  dismissed it. Do not convert this into a claimed full-suite/hosted-native pass.
- Light/Dark rendered screens inspected; final live Windows 11 Layouts screen
  opened/foregrounded through Computer Use and visually inspected.
- Actual daily repo EXE process 29008/runtime beta.4: six same HWNDs and unchanged
  outer bounds, no Recovery, bar hidden, original Startup bytes unchanged. Zero
  sampled app-specific crash events in preceding 35 minutes. Re-verify PIDs next
  session; local results/private snapshots are under ignored bin/studio-*.
- Existing hosted delivery src/ mirrors all fourteen updated C# files; separate
  hosted data retained. No second controller remains. Backup of prior daily EXE
  and data is bin/studio-design-backup. No user windows were intentionally arranged
  by testing. Source/package manifest includes DESIGN.md for future packages.

Next steps: user visual review of the open Layouts manager and Appearance cards.
Keep numeric editing and the existing interactive visual editor reachable. Resume
freeze-after-probe recovery hardening from docs/NEXT-MILESTONES.md when requested;
this passive preview does not show live occupancy. Full native interaction checks
need a quiet input interval if repeated. No push/tag/release happened; published
beta.2 remains frozen. Local builds do not imply production certification.


## Latest implementation: appearance and manager refresh, 2026-10-29

The user explicitly requested implementation now, overriding the earlier planned
recovery-first order for this scoped change. Local **0.3.0-beta.3** (file 0.3.0.11)
implements General → Appearance → System / Light / Dark, System as the default,
Windows accent following, system-event refresh, high-contrast priority and
Save/Cancel-isolated preview. Manager navigation uses an accessible button rail
and General strip backed by native TabControl page selection. Shared surfaces,
grids, inputs, rounded buttons and supported DWM titlebar/corner hints replace
mixed legacy styling. See docs/APPEARANCE.md. No framework/runtime dependency was
added. OS-owned widgets remain partly native-rendered; full six-slot overview and
recovery hardening remain pending. Published beta.2/tag/assets are unchanged.

Verification this turn:
- Build -Test: 198564 checks; theme-only draft geometry rebinding avoids reflow;
  portable export excludes Appearance/import retains it.
- Compiled AccessibilityUi (new mode/persistence/legacy/invalid Recovery/draft
  Cancel/failed Save/navigation/paint checks), WorkspaceUi, LauncherUi and EditorUi
  pass. Final header cleanup was rechecked with compiled/hosted AccessibilityUi.
- Windows PowerShell 5.1 STA hosted AccessibilityUi, WorkspaceUi, LauncherUi and
  EditorUi pass. Light/Dark screenshots inspected; live Windows 11 System-dark
  manager/Appearance page and dark titlebar observed through Computer Use.
- Compiled full native Integration passes during the change, including six-slot
  pair-only resize, keyboard/flyout, recovery and compatibility fixtures. Later
  changes only remove a clipped decorative subtitle and preserve ampersands.
- Hosted full native Integration is INCOMPLETE: first attempt failed flyout reopen;
  second recorded outside mouse input and foreground changing to a daily app
  during persistence testing. No host crash logged. Focused hosted UI suites pass;
  do not claim the full hosted native suite passed this turn.
- Final daily compiled process 61588 verified against actual process/runtime:
  beta.3, six identical HWND assignments and unchanged outer bounds, no Recovery,
  bar hidden, original Startup shortcut bytes unchanged; zero sampled app-specific
  crash events over preceding 45 minutes. PIDs are transient; re-verify next session.
- Existing hosted source delivery src/ mirrors updated fourteen files. Its separate
  data is retained; no second controller remains. Daily repo data retained and
  Appearance defaults to System. Local backup in bin/appearance-deploy-backup;
  private snapshots/results under bin/appearance-* must not ship.

Exact next steps: let the user use/review the new General → Appearance controls.
If revising visuals, inspect the live UI and preserve keyboard/high-contrast/DPI
contracts. If resuming broader roadmap, implement the disposable freeze-after-probe
repro from docs/NEXT-MILESTONES.md before selecting bounded placement recovery.
OS theme changes/Narrator/mixed-DPI hardware remain unverified; no OS settings were
changed. Local-only work: no GitHub push/tag/release or frozen asset replacement.


## Latest clarification: Windows 11 appearance, 2026-10-29

The requested modern manager should match Windows 11's clean visual style and
offer System/Light/Dark selection. System is the planned default, following
Windows appearance and relevant system/accent colors while running; Light/Dark
are explicit overrides. Preserve high-contrast accessibility, keyboard/DPI support,
settings Save/Cancel and managed-window geometry. This refines milestone 3 in
docs/NEXT-MILESTONES.md; recovery still leads the development sequence.

These are requirements/plans only. The current beta.2 has system high-contrast
support and its existing palette, not these user-selectable appearance modes.
No new UI stack, runtime dependency, Mica requirement, text-size feature, app code,
test result or release is implied by the clarification. Keep local planning records
separate from unchanged published beta.2 assets.

## Latest checkpoint: next recovery/UI priorities, 2026-10-29

User priority update after extensive positive six-slot testing: #1 reset need is
already satisfied in their workflow; #2/#3 proportion/app-assignment conveniences
may help others but are not personally important. Recovery (#4) is a must,
simplifying the manager (#5) is good, and a very modern manager UI (#6) is wanted.
The concrete planned sequence/acceptance is docs/NEXT-MILESTONES.md. No new app
functionality, framework choice, UI redesign, test result or release is claimed.

First recovery step is a disposable external app that answers the size probe and
then freezes before synchronous placement. Verify the existing heartbeat failure
and unrelated bounds, then design/test bounded recovery including stale/late
placement after resume. Repeating the already-hung-target test is insufficient.
Preserve pair-only heights, the other windows, explicit restore/relaunch and saved
settings/startup. Modernization should use a reviewed visual prototype and retain
source-hosted delivery unless a different tradeoff is explicitly accepted.

Feedback and this plan are local documentation commits, not additional public
release assets. Beta.2 stays unchanged. Follow the user's current request before
starting a large implementation or publishing these records.

## Latest checkpoint: user-reported six-slot acceptance, 2026-10-29

The user reports extensive testing of the six-slot layout and its dynamic resizing,
which works quite well on the current installation. They do not use the other
layouts. Record this as positive real-use user testimony for that specific core
workflow, supplementing the existing automated/native checks. No new defect was
reported; no exact duration, app matrix or gesture breakdown was supplied.

Do not extend this report to other layouts, workspace/portable/general features,
other machines or all production gates. Existing automated coverage for those
features remains valid within its earlier scope; user coverage is narrower.
No app change, new beta version, repackaging or additional test run is warranted
merely to record this feedback. Beta.2 release/tag/assets remain unchanged.
These feedback records are local until an authorized documentation push.

Next: follow the user's priority and address concrete defects or requested polish.
The six-slot workflow now has explicit positive user acceptance; do not repeatedly
ask the user to retest it without a change or new concern. Other feature/manual
limits remain distinct rather than erased or inferred from this report.

## Latest checkpoint: beta.2 merged, tagged and published, 2026-10-29

The user explicitly requested PR review/merge, exact-source tagging and unsigned
prerelease publication. PR #1 was reviewed against green ce7da2e, marked ready and
merged as b666f3b0b19ff86aa4351cc7a3ba1ed68ae5de50, with the same reviewed tree.
GitHub main now contains the polished manager and both histories. The original
six-slot-snap repository remains separate and untouched.

Published prerelease:
https://github.com/Brandonr91/windows-tile-studio/releases/tag/v0.3.0-beta.2
Release ID 428441196; draft=false, prerelease=true. Annotated tag v0.3.0-beta.2
resolves to tested 89613a20b63448e7b6d4a44dc27e81c63a117579, not the later merge.
Both frozen ZIPs, SHA256SUMS.txt and release-info.json were uploaded while draft,
verified against server size/digest, then published. All four files were downloaded
anonymously from public URLs and hashes match. No frozen file was rebuilt/replaced
and no certificate/signing was used. Checksums remain in docs/RELEASE-BETA-2.md.

Main merge CI run 40962856677 and tag CI run 40962921659 passed. A final docs
publication-record commit follows the merge; verify current main's latest run
before claiming its head passed. Current exact status is recorded outside Git in
outputs/windows-tile-studio-beta2-publication-final.json when available.

Evidence outside Git: outputs/windows-tile-studio-beta2-merge.json,
beta2-draft-release.json, beta2-uploaded-assets.json, beta2-published-release.json,
beta2-public-downloads.json and beta2-postmerge-runs.json (same output prefix).
The scoped API helper stays outside Git, uses authorized credentials only in
memory and never logs/stores them. Release notes include usage, startup/data
behavior, unsigned status and unverified limits. Private snapshots were not uploaded.

Local checkout is now main, fast-forwarded without discarding local history.
Daily app remains beta.2; publication does not install/restart/reconfigure it.
Fresh-account validation stays waived. Actual speech/OS appearance/hardware/login/
privilege, freeze-after-probe and multi-day limits remain recorded, not fixed.

Next: verify final docs CI and public release metadata/assets, then gather beta
feedback. Do not repeat publication, move this tag or overwrite these files.
Future fixes need a new version/provenance and relevant checks. Older draft and
prepublication scopes below are superseded by the completed user-authorized release.

## Latest checkpoint: draft PR published and Windows CI verified, 2026-10-29

Git Credential Manager device authorization completed for Brandonr91. Normal Git
push published full reviewed history to review/beta-2. Draft PR #1 is open and
attached to this chat: https://github.com/Brandonr91/windows-tile-studio/pull/1.
Main remains cfdb2df; no merge, version tag, GitHub Release or signing was performed.
The original six-slot repository, daily app/data and frozen artifacts are unchanged.

Initial remote head 430a11a passed both Windows CI events: push run 40961084350
and pull_request run 40961226184. Checkout, 198564 geometry/configuration checks,
offline source-hosting/installer tests, package lifecycle tests, unsigned builds
and artifact upload all succeeded. This documentation follow-up creates a newer
review head; verify its current runs too. Exact final head/run evidence is kept
outside Git in outputs/windows-tile-studio-beta2-ci-final.json when available.
Do not infer current-head success solely from an older green run.

The connector's writes were denied 403 despite reported repository push rights.
Git transport works after human authorization. Draft PR creation used the same
authorized Git credential through a scoped local API helper, in memory without
printing/persisting credentials. Connected GitHub read tools can inspect jobs/logs.
No further push/draft-PR/CI approval is needed within the authorized review branch.
Merge/tag/release publication remain separate user actions. Never automate login
or consent, request a password/token in chat, or commit temporary device codes.

CI artifacts are ephemeral unsigned builds from each workflow checkout and may
have different source revisions/hashes from frozen clean-89613a2 beta.2 archives.
They must not silently replace those reviewed release assets. CI does not run
interactive desktop suites; local native/accessibility evidence still supplies
that scope. Physical/actual speech/login/privilege/freeze-after-probe/multi-day
limits remain; fresh-account testing stays explicitly waived.

Next: verify current PR head checks and update its validation description. Leave
the PR as draft, main unchanged and releases unpublished unless the user requests
the next publication step. Keep project context synchronized without repeatedly
rebuilding frozen assets. Earlier authentication/prepublication notes are history.

## Latest checkpoint: authorized review push awaiting Git sign-in, 2026-10-29

The user continued the explicit next step of pushing a review branch and checking
GitHub CI. Scope is review/beta-2 plus a draft PR to main, with no main update,
tag or release publication. Do not ask for this authorization again.

Remote main rechecked as cfdb2df; working tree clean and reviewed local head was
f68e97d. Direct noninteractive Git push failed because Git has no saved sign-in.
Connected GitHub metadata reported push/admin access, but actual create_branch
was denied 403 Resource not accessible by integration. No branch or PR was created;
do not infer actual write capability from repository permissions alone.

Git Credential Manager's normal browser login was started for Brandonr91; the
human must complete authentication. Never automate authentication/consent dialogs
or request a token/password in chat. A pending user-input request asks for “signed
in” after completion. Verify credential readiness and current remote refs before
retrying a normal Git push; do not force push or replace known public history.

Prepared PR body is work/beta2-pull-request-body.md outside this repository. Title:
Prepare Windows Tile Studio beta.2 with layout editing, workspaces and verified delivery.
After authenticated push, use the connected PR tool to create a draft, attach its
URL to this chat, and inspect the exact PR head's checks/logs. Fix real CI failures
within the authorized branch and keep main/release assets unchanged. CI has not run.
App/data/artifacts are unchanged by this authentication checkpoint.

## Latest checkpoint: beta.2 release review, 2026-10-29

Exact clean-89613a2 beta.2 archives now pass ReleaseTests, InstalledLaunchTests
and actual-public-source PublicUpgradeTests. Accepted reports outside Git are
outputs/windows-tile-studio-beta2-release-final.txt, beta2-installed-final.txt,
beta2-upgrade-final.txt and beta2-release-final.json. The JSON verifies live daily
beta.2 with six assignments, hidden bar/no Recovery, settings/Startup bytes
preserved and zero matching sampled Windows crash events. The daily app was
paused only for isolated native launch tests and restored in finally. No fresh
Windows account, daily uninstall, real-app adoption or startup preference change.

Read docs/RELEASE-BETA-2.md for the concrete release draft, exact artifact hashes,
verified scope, limits and publication preparation. Existing bin/beta2-releases
and beta2-kit remain frozen at source 89613a20b63448e7b6d4a44dc27e81c63a117579;
later history/docs commits do not replace their bytes. Beta.1 artifacts remain
separate. No new app/version/deployment change was needed for release review.

History finding supersedes older warnings that assumed no compatible merge:
public cfdb2df and local baseline 48b96b2 have equivalent tracked contents when
ignoring EOLs, with shared ancestor 51ffa88. Verified local merge d056d17 has
parents 89613a2 and cfdb2df and the identical tree to 89613a2. Both histories are
retained, public tip is an ancestor and force push is unnecessary. This is local
only; origin/main remains cfdb2df. Targeted tracked-content/reachable-path checks
found no matched credential/private-key/account-path/email/runtime-payload patterns;
do not generalize a targeted scan into comprehensive secret clearance.

Next: user review of the release draft and remaining limits. No push, PR, tag,
GitHub Release, signing, remote CI or publication occurred. A subsequent authorized
push must first fetch and recheck public ancestry/diff; preserve six-slot-snap.
Frozen-asset publication must tag exact source 89613a2 rather than newer review
docs. If changing source/artifacts, use new provenance and rerun relevant checks.
Actual speech/OS appearance/hardware/login/privilege/freeze-after-probe/multi-day
coverage remains unverified; fresh-account validation stays explicitly waived.

## Latest checkpoint: live keyboard/accessibility acceptance, 2026-10-29

Current daily app is local `0.3.0-beta.2` (file version 0.3.0.10). Actual beta.1
UI inspection found an unnamed main settings TabControl and blank Hotkeys action
headers; cells consequently exposed row number/value without an action name.
SettingsForm now names the selector Settings pages and headers Change shortcut/
Clear shortcut. Live beta.2 provider tree confirms named headers and action cells.
No shortcut, layout, schema or focus-routing behavior changed. AccessibilityUi
adds accessible-object assertions for the selector and button-column headers.

Current-installation acceptance used the computer-use skill for real keyboard
navigation: Ctrl+Tab across main pages; Tab into General categories; Right/End
between categories; Tab/Shift+Tab into/out of bar controls; Tab through Support to
Preview diagnostic report; Space opens preview, Escape closes and returns focus
to its launcher. Hotkeys grid Tab reaches Change, Space opens Choose shortcut,
Escape cancels; settings Escape cancels. No binding or preference was saved.
The provider reports grid focus at table level in some cases; a visible focus cue
confirmed the selected Change cell. Actual screen-reader speech is unverified.

Verification: 198564 pure checks PASS; compiled AccessibilityUi and WorkspaceUi
PASS; Windows PowerShell 5.1 STA hosted AccessibilityUi PASS. New labels confirmed
in the live installed provider tree after rebuild. Settings/Startup bytes remain
identical to the pre-check snapshot; six daily assignments, hidden bar and no
Recovery. Sampled Application 1000/1001/1026 app/test crash events: zero. No OS
theme, contrast, scale or startup preference changed, and no real app was re-snapped.

Evidence outside Git: outputs/windows-tile-studio-keyboard-before.json,
keyboard-build.txt, keyboard-ui-tests.txt, keyboard-hosted-tests.txt and
keyboard-final.json. Runtime/process state is verified in that report, but PIDs
remain transient. Fallback binary/settings/hosted sources are in
work/keyboard-acceptance-fallback. Hosted delivery has the new fourteen sources
and current Start-Hosted.ps1 with its own data preserved; only one controller runs.
Wait for verified runtime readiness before issuing CLI --settings after a restart;
an earlier command correctly showed the not-started notice and was dismissed.

New beta.2 packaging, when present, is separate from prior beta.1 artifacts;
inspect its release-info.json for exact revision and hashes. Never overwrite the
older beta.1 packages/kit or include private ignored snapshots. No push/release.
Fresh-account testing stays waived by the user. Remaining work: actual screen-
reader speech/OS appearance, actual login when naturally performed, available
physical hardware/sleep/docking, privilege/competing-tiler/freeze-after-probe,
longer daily use, remote CI, signing/publisher choice and approved publication.

Older checkpoints below retain historical evidence and superseded app versions.

## Latest checkpoint: existing-installation acceptance, 2026-10-29

User instruction: use this installation; fresh-account/VM testing is not needed.
That gate is waived and must not be reintroduced from older sections below.
Coverage on other environments remains unverified. Application source/binary stay
beta.1; only validation tooling/docs changed. Existing beta packages/kit retain
their original 262c13a provenance and historical documents; none were overwritten.

CurrentInstallationTests.ps1 now validates real settings and existing backup on
private copies, verifies live runtime/process/receiver ownership, optionally quits
normally and executes the existing owned Startup shortcut, then samples bounded
heartbeats and process counters. It never writes preferences, enables startup,
snaps windows, logs out, uninstalls or deliberately corrupts the daily app. A
failed shortcut launch gets a direct-restart fallback. The test requires Windows
PowerShell 5.1 STA and supports this current compiled checkout layout.

Executed here with -ExerciseStartup -ObservationSeconds 180: current settings and
settings.xml.bak load without warnings, each containing six profiles; both originals
are byte-preserved. No workspaces.xml exists, so personal workspace/backup validation
is not claimed. Existing Startup shortcut launches the same beta.1 installation in
397 ms, with byte-identical settings/shortcut and hidden bar/no Recovery. This is
normal shortcut execution, not Windows logout/login or StartupApproved policy proof.

Observation lasted 184.988 seconds: zero heartbeat failures, zero Recovery samples,
zero sampled Application 1000/1001/1026 app crash events, GDI growth 0, private memory
growth -118784 bytes and process CPU 15.625 ms. These are short passive observations,
not multi-day leak/performance claims or continuous error-dialog monitoring.

Evidence: outputs/windows-tile-studio-current-installation.txt and
windows-tile-studio-current-installation-result.json. Private raw settings/backup/
shortcut snapshots stay in ignored bin/current-installation-<guid>; never publish
them or include them in packages. New test helper is in the checkout, not the older
beta.1 ZIPs. Daily process is restarted and live; verify PID/path/state rather than
relying on this checkpoint's transient PID. No version change, push or release.

Next: continue acceptance on this installation. Remaining work includes actual
login when naturally performed, keyboard/screen-reader/OS appearance verification,
available physical hardware/sleep/docking, privilege/competing-tiler and freeze-after-
probe risks, longer daily observations, remote CI, signing/publisher decision and
approved publication. Do not force logout, change system settings or create accounts
merely to complete a checklist. All older fresh-account requirements below are
historical and superseded by the explicit user instruction above.

## Latest checkpoint: restricted-app validation, 2026-10-29

The compatibility follow-up changes tests/docs only. App source and deployed
binary remain beta.1; bin/beta-releases and bin/beta-kit still identify clean
application/test baseline commit 262c13a, not this later verification commit.
No artifacts were overwritten, release published or startup preference changed.

Accepted focused compiled and actual source-hosted suites pass: a separate app
blocked after startup is independently confirmed by sent-message timeout and OS
hung detection; snapping rejects it without movement/assignment, settings stays
responsive, and explicit snapping succeeds after resume. Rejection samples were
171/172 ms including command pump. Maximum-size and custom fixed-aspect fixtures
reject unsupported frames and regain original bounds. Existing three/four-row,
minimum/corner, lifecycle and isolated Chrome cases pass in both modes. Geometry
checks remain 198564. Application behavior did not require a fix.

Harness fixes: use a native bounded event wait in the child instead of assuming
a managed wait/startup callback proves a hang; normalize fixture data paths to
absolute paths for isolated Chrome. HostedIntegration's compatibility subcase now
uses its requested hosted controller rather than silently switching to compiled.
The 30-second rescue wait is test-only. Early failed fixtures are not accepted
evidence. Chrome emitted a nonfatal new-tab/profile stderr diagnostic in accepted
runs; frame/close assertions passed. See docs/COMPATIBILITY.md.

External evidence: outputs/windows-tile-studio-restricted-native.txt,
restricted-hosted.txt, restricted-before.json and restricted-final.json.
The final report verifies the live restarted daily controller, hidden bar,
no Recovery, preserved settings/Startup bytes and sampled Windows crash events.
Runtime XML alone is not live-process evidence. PIDs are transient.

Remaining next steps are release gates, not an invented numbered phase: fresh
Windows account/VM, representative personal settings/workspace backups, actual
login, physical mixed-DPI/docking/sleep, screen reader/OS text and contrast,
elevated/inaccessible apps, competing tilers and longer daily/external beta.
Freeze-after-successful-probe and synchronous placement/restore deadlines remain
known risks; an already-hung target test does not close those gaps. WindowsSandbox
was not discoverable on this host; no account/system feature was enabled.
Remote CI, publisher/signing and approved publication remain pending. Continue
the user's current priority, read all root context documents and actual Git/live
state, and preserve the original six-slot repository and existing beta artifacts.

The beta implementation checkpoint below remains relevant historical context.

## Current checkpoint: beta validation, 2026-10-28

`0.3.0-beta.1` follows phase 8 on feature/polish-launchers. All eight phases
have local core implementations; this is release validation, not phase 9 or a
published production release. Read docs/BETA-TESTING.md and RELEASE-CHECKLIST.md.

Reproduced against actual public `cfdb2df728466b1256383cf9b8417b21a486a1ca`:
its serializer silently drops newer launcher position/lock and monitor settings.
Beta SettingsStore now enters Recovery on unknown elements/attributes, keeps
original bytes and suppresses autosave through existing Recovery behavior.
XML loads cap 1 MiB/32 levels and prohibit DTD/resolver use. Version remains 1;
no settings migration or backup restoration is automatic.

InstallationSupport probes legacy settings types against a temporary snapshot
before install/rollback/installed launch. It rejects lossy selections without
changing pointers/settings. Test-ConfigurationCompatibility.ps1 is the fourth
installer companion. Current source declares preserve-and-recover capability,
allowing corrupt/future files to reach Recovery without an extra startup compile.
Direct old EXEs/older launchers bypass the new guard. Trusted selected source runs
in a bounded child process, not a sandbox. New-BetaKit.ps1 checks archive hashes
and collects packages/provenance and test/recovery docs into a new local folder;
no user data collection, invitations, uploads or publication.

Verified local scope:

- Build -Test: 198564. Compiled LauncherUi/EditorUi/WorkspaceUi/AccessibilityUi,
  WorkspaceIntegration/cold hidden dialogs and full HostedIntegration pass.
  This includes future-field Recovery preserving bytes, existing resize ownership,
  concurrent-source, hotkey, font/paint, minimum/frame and isolated Chrome checks.
- Source-hosted General/workspace and accessibility suites pass independently;
  LauncherTests and ReleaseTests pass, including data retention and failed update
  integrity/pointer protection. Destructive/shortcut operations use temp fixtures.
- PublicUpgradeTests installs and launches actual immutable public source, upgrades
  to beta with synthetic names/keys/geometry, refuses incompatible rollback,
  downgrade installation and actual installed legacy launch, then exercises a
  compatible rollback after explicitly restoring fixture settings. An empty valid
  separate workspace library is retained byte-for-byte; personal captured workspaces
  are not claimed. Public ZIP SHA256 is
  eb8d75d6efa9af522bcbdf864fa9995e7689eaf1969144d875a047942ee5890f.
- Build -BetaSoak 30: both compiled and hosted pass 30 height cycles plus width
  gestures/dialog churn, six assignments and hidden-bar checks. Compiled GDI
  48→47, private 33,251,328→35,942,400 bytes; hosted GDI 48→58, private
  93,892,608→101,863,424. Each sample took about 46 seconds after warmup and
  reported 0 ms process CPU during its 15-second idle interval. These are bounded
  local observations, not multi-day leak/performance or physical-input guarantees.
- InstalledLaunchTests exercises actual stable Hosted/Compiled launch and rollback
  with no adoption of real windows. Windows Application crash events are sampled
  separately, not continuously monitored. Inspect the final external error report.

Evidence outside Git: outputs/windows-tile-studio-beta-*.txt/json, including
native, hosted-workspace, hosted-accessibility, launchers, release, public-upgrade,
installed, windows-errors and deployment. Later *-final reports identify final
clean-commit packages. Packages are bin/beta-releases/, kit bin/beta-kit/;
release-info.json supplies exact source revision/dirty state and archive hashes.
Earlier preview.8 bin/releases artifacts remain separate and unchanged. Development
packages may record dirty source; use the final clean provenance, not an old PASS.

Daily delivery target is repo bin/SixSlotSnap.exe with repo data/, beta.1, bar
hidden and no Recovery; verify the process/command line plus final deployment
report rather than stale XML. The preexisting enabled Startup shortcut must stay
byte-identical and direct to its original repo binary/data; no startup migration
or enablement was performed. Hosted delivery receives all fourteen current source
files and Start-Hosted.ps1, retaining separate data and no second controller.
Fallback binary/settings/hosted sources are work/beta-validation-fallback. Prior
preview source is Git 2fe2297. Verify deployed state after compaction.

Exact next steps:
1. Read all five root context files; inspect status/log/remotes and live process.
   Confirm final beta package/kit provenance and deployment/error evidence.
2. Continue the user's priority. Remaining release gates require a fresh account/VM,
   representative personal configs/backups, actual login, physical displays/docking/
   sleep, screen reader/OS text/contrast, privilege/hung/unusual apps, competing tilers
   and longer daily/external use. Mark untested cases honestly; do not change account
   settings, sign, publish or send tester messages merely to fill a checklist.
3. Collect feedback using the local template; resolve reproduced defects with scoped
   regressions. Preserve adjacent-pair heights, hidden bar, local-only runtime and
   existing startup/data. Theme/text-size preferences remain unimplemented.
4. Remote CI is configured but unexecuted. Reconcile divergent public/local histories
   without force push only when publication is requested; preserve six-slot-snap.

The following phase-8 checkpoint is historical.

## Current checkpoint: phase 8 delivery preparation, 2026-10-28

Implemented and deployed locally as `0.3.0-preview.8`, following `508783a` on
feature/polish-launchers. The eight-phase sequence now has local core tooling;
this is an unsigned preview, not a production release/certification. Read
docs/RELEASE-AND-RECOVERY.md and docs/RELEASE-CHECKLIST.md before distribution.

New delivery files: Build-Release.ps1, InstallationSupport.ps1,
Manage-Installation.ps1, release/version.json, release/package-files.txt,
tools/New-AppIcon.ps1, CHANGELOG.md and read-only .github/workflows/windows.yml.
The fourteen application sources remain dynamically compiled in both modes.
Build.ps1 checks source/release versions, generates assembly metadata and embeds
the owned six-tile icon. Product/file identity is Windows Tile Studio; compatible
SixSlotSnap.exe/receiver/mutex names remain. File version is 0.3.0.8.

Build-Release emits Source and PortableCompiled ZIPs, per-file manifest,
SHA256SUMS.txt and release-info.json. Reviewed input patterns exclude user data,
logs/runtime/backups. Source ZIPs repeat for identical bytes/provenance via fixed
entry order/timestamps/no compression; legacy csc compiled PE metadata is not
deterministic. Dirty previews require an explicit flag and record it. Signing
was not performed; the generated EXE reports NotSigned. Official action v7 refs
were read and pinned to their full SHAs; no workflow was pushed/run on GitHub.

Installer changes: companion scripts are required, optional expected archive
hash, bounded path/link checks, manifest/cache/source/compiled integrity checks,
staged candidate compile/host validation with isolated data and 60-second parent
deadline, activation only on success, previous pointer and write-failure rollback.
Legacy raw source receives a local integrity manifest. Launch-Installed verifies
current code/artifact; mode changes require installation, not an unchecked
override. No install/update automatically enables startup or kills a running app.

Management supports read-only status/validate, confirmed rollback and uninstall.
Running owned installs and linked paths are refused. Default uninstall retains
data/unrelated root files and a marker for reinstall; RemoveData is separate and
destructive. Only an owned startup command targeting that installation is removed.
New startup opt-ins from owned versions/<id> + matching data use the stable
Launch-Installed.ps1 across updates/rollback. Existing direct/version-specific
entries are not silently migrated. Ordinary checkout/hosted startup stays direct.

Verification completed before final artifact packaging:

- Build -Test PASS: 198560. Compiled LauncherUi/EditorUi/WorkspaceUi/AccessibilityUi,
  native WorkspaceIntegration and full HostedIntegration PASS, retaining prior
  geometry/concurrent-source, hotkeys/recovery, frame/minimum/Chrome, display,
  fonts/close-to-tray and accessibility/report privacy cases. Hosted General and
  accessibility checks plus source installer/serialization suite PASS separately.
- ReleaseTests PASS under Windows PowerShell 5.1: repeatable source ZIP, manifests/
  metadata/icon/checksums, fresh/repeat/update and rollback toggle, locked pointer
  rollback preservation, bad hash/noncompiling update/package/cache/binary tamper,
  Source-compiled cache, WhatIf, data-retaining uninstall/reinstall, synthetic
  explicit purge, owned/unrelated startup and junction rejection. All destructive
  operations/shortcut writes were confined to fresh temp fixtures. These are
  isolated-directory tests, not a fresh Windows account.
- InstalledLaunchTests PASS: actual stable installed Hosted launch, Compiled
  update/launch, rollback to Hosted and launch, runtime version/mode/path/receiver
  ownership, hidden bar/no recovery, running-install removal refusal and normal
  exit. No real windows adopted; no real startup entry enabled/changed.
- SupersessionTests PASS: exact selected fake legacy close/redirect, original
  hash preservation, enable/disable/re-enable and standalone restoration without
  manager source. Actual predecessor repositories/legacy settings untouched.
- PowerShell source syntax and diff/link checks are part of final review. Final
  committed artifacts are rechecked with ReleaseTests -PackageDirectory and the
  installed-launch suite; inspect final report files below for that later result.

Development findings resolved: PowerShell 5.1 required explicit Compression
assembly loading; an icon/build fixture nested too deeply hit the legacy compiler
path limit, so release fixtures use short Temp roots and validation retains error
output. Validation caches the child handle before waiting to retain exit status.
Final cross-shell verification also exposed ZIP/JSON differences between .NET
Framework/PowerShell 5.1 and PowerShell 7. Build-Release now delegates other shells
to 5.1, making canonical serialization consistent across entry points.
The first installed smoke reused a stale runtime snapshot between starts; it now
clears only its disposable snapshot and cleans up only its owned test process.
This was a test harness race, not data migration or a manager crash. Invalid code
and locked-pointer cases are deliberate failures, not unresolved product defects.

Daily deployment currently verifies repo bin/SixSlotSnap.exe with repo data/,
preview.8, hidden/disabled bar and recoveryRequired=false. Existing settings and
the already-enabled account Startup shortcut are byte-preserved against phase-8
snapshots. That shortcut still targets the same repo binary/data; no migration
was requested or performed. data/workspaces.xml remains absent. Hosted delivery
has all fourteen matching source files and current Start-Hosted.ps1, separate data
preserved, no second controller. Fallback binary/settings/hosted sources/launcher:
work/phase-eight-fallback; prior code also Git 508783a. PIDs are transient.

Accepted reports outside Git share outputs/windows-tile-studio-phase-eight-
prefix: full-tests.txt, hosted-Launcher.txt, hosted-WorkspaceUi.txt,
hosted-AccessibilityUi.txt, release-accepted.txt, installed-accepted.txt and
legacy.txt. Final artifacts/checks are bin/releases/, release-artifacts-final.txt,
installed-artifacts-final.txt, deployment.json and windows-errors.json. Early
build/release/installed-launch reports may retain failed development attempts;
use accepted/final reports and exact artifact revisions, not stale PASS/error files.
Release fixture outputs use Temp/WtsReleaseTests/<guid> and
Temp/WtsInstalledTests/<guid>; no user data was copied into shipped archives.

Exact next steps: read the five context files and actual status/log/remotes and
runtime/process/data state. Continue the user's latest request. There is no
planned phase 9: next is the release-checklist/beta campaign (fresh account/VM,
actual public-version upgrades, login, physical displays/docking/sleep,
screen-reader/OS contrast/text sizes, elevated/hung apps, competing tilers and
external daily use), then signing/publisher and explicit artifact/push/release
approval. User-selectable themes/text sizes remain unimplemented. Remote CI,
real login and clean-account validation are not claimed. Do not force-push the
divergent public/local histories or touch the original six-slot repository.

## Previous checkpoint: phase 7, 2026-10-28

Implemented/deployed locally as `0.3.0-preview.7`, following `cdac931` on
feature/polish-launchers. Read docs/ACCESSIBILITY-AND-SUPPORT.md for exact scope.
Visual drafts support Page Up/Down divider selection, 1% arrows/0.1% Shift,
Ctrl+Z/Y, normal Tab exit, selected-divider status/focus and an accessible slider
value. Existing adjacent-pair gesture math is reused; numeric editing remains.
Dialogs are resizable/DPI-aware, clamp to work areas and scroll absolute content;
workspace actions disable when selection prerequisites are absent. Custom panels
use Windows high-contrast colors. General Support adds allowlisted aggregate
report preview/atomic local save, with no log/workspace reads or identities.

Fourteen src files now include UiSupport.cs and Diagnostics.cs. Source-hosted
compilation and hosted UI test scripts explicitly reference the Windows/.NET
Accessibility.dll assembly for the custom accessible object; no package download
or external runtime dependency was added. Build.ps1 adds -AccessibilityUi.
tests/PerformanceTests.ps1 measures an extracted source package with isolated
settings, hidden bar and no geometry recovery. Do not run it with a daily controller.

Verification:

- Pre-change probe against installed preview.6.1 reproduced no Right-key canvas
  geometry change and settings exceeding 800×600. This probe required corrected
  assembly loading after an initial test-helper error; accepted before.txt has
  the two observed results, not those failed helper attempts.
- Final pure suite PASS: 198560. LauncherUi, EditorUi, WorkspaceUi/General and
  AccessibilityUi PASS compiled and Windows PowerShell 5.1 STA. Tests include
  actual canvas Right/Tab input with verified foreground/focus, deterministic
  divider/undo/floor checks, accessible value, uneven flyout directions/wrap/
  scroll, constrained 800×600 settings/workspace/editor, injected system contrast,
  report sentinel privacy, preview, atomic save/failure cleanup and source isolation.
- WorkspaceIntegration/native capture/restore and cold compiled dialogs PASS.
  Compiled Integration PASS (integration-final.txt, compiled section). Final
  HostedIntegration PASS separately in hosted-integration.txt after reference fix.
  Source installer/host validation and cold hosted first-dialog checks PASS.
  Full suites retain prior resizing/concurrent sample, hotkey cleanup/recovery,
  minimum/frame/Chrome, display notification and close-to-tray coverage.
- The final focused tests follow the later diagnostic field-label and minimum-
  dialog-size adjustments; no resize math changed. Rendered normal/contrast
  editor, small settings/workspace and flyout inspected. Visual review found an
  occluded status label, fixed with explicit table rows before final checks.
- One native Tab/Shift+Tab test initially failed; unchanged input flow passed on
  retry. New actual-key test required a verified foreground helper; an uneven-
  grid fixture incorrectly assumed two columns and was corrected. The missing
  hosted Accessibility.dll reference was a real launch failure, now fixed/tested.
  Do not treat failed intermediate reports or stale integration-error.txt as final.
- Application events 1000/1001/1026 sampled since phase development: zero matching
  app/test crashes, query succeeded. This is not continuous notification-area
  monitoring. No real workspaces/apps were captured/restored/launched by tests.

Packaged-source performance (3 process starts per mode, local machine only):
compiled readiness 137/139/262 ms, median 139; working set 40.0–40.4 MB (~38 MiB),
private ~26 MB. Source-hosted readiness 634/642/680 ms, median 642; working set
~113.5 MB (~108 MiB), private ~89.2 MB. One-second idle CPU samples: compiled
0/0/0 ms; hosted 15.625/0/0 ms. Finite resolution, warm caches and tiny sample
count preclude universal latency/CPU promises. Existing compatibility sample:
20 updates ~1506 ms including deliberate pumps/settling, no growing GDI count.

Deployment: backed up/stopped exact repo compiled daily controller, ran isolated
tests, rebuilt bin/ and restarted with repo data/. Runtime/process verifies
preview.7, bar hidden/disabled and recoveryRequired=false. All previous settings
values match by profile ID; startup serialization rotated the active monitor
profile to the end, so bytes differ only in that list order. Do not overwrite
preferences merely to restore XML ordering. Hosted delivery now has all fourteen
matching sources AND updated Start-Hosted.ps1; separate data preserved, no second
controller. Fallback binary/settings/hosted source: work/phase-seven-fallback;
prior code/launcher also Git cdac931.

Important fresh state: an owned Windows Tile Studio Startup shortcut now EXISTS.
Its creation/last-write was 19:19:43 local, predating phase development at 19:23:51;
it points at the repo compiled EXE/data and was preserved. Tests did not enable
or modify this real entry. Earlier handoffs saying startup off are historical.
No real login/logout test was performed; data/workspaces.xml remains absent.

Accepted reports outside Git share outputs/windows-tile-studio-phase-seven-
prefix: final-ui.txt, accessibility-final.txt, hosted-AccessibilityUi.txt,
hosted-EditorUi.txt, hosted-WorkspaceUi.txt, hosted-LauncherUi.txt,
integration-final.txt (compiled section only), hosted-integration.txt,
hosted-visibility.txt, launchers.txt, performance.json/.txt, deployment.json and
windows-errors.json. full-tests.txt includes initial UI/native PASS followed by
the input failure; intermediate reports are retained transparently. Screenshots:
bin/phase-seven/accessibility-ui-data/*.png. A source-only phase-seven-source.zip
was extracted/built/launched for measurements; final docs/package are refreshed
after verification with the same app sources. No public push/release occurred.

Exact next steps: read all five context files, status/log/remotes, uncommitted
diff and actual process/data state. Follow the user's latest request. Phase 8
(production delivery) is next: audit installer/update/rollback/uninstall and CI,
define reproducible artifact identity/manifest/checksums, then clean-account and
external beta validation. Do not force-push divergent public history or touch the
original six-slot repository. Preserve scoped regressions and the user's startup
and hidden-bar choices. User-selectable themes/text sizes remain unimplemented;
manual screen-reader announcements, actual OS contrast/text scaling, physical
mixed-DPI/docking/sleep, real login and privileged/hung apps remain release gates.
Phase-7 core implementation is not certification or a production-release claim.

## Previous checkpoint: General settings interlude, 2026-10-28

User requested General options before returning to polishing phases. Implemented
and installed locally as `0.3.0-preview.6.1`, following `7465c8d`. Tray **General
settings** (also tray double-click) opens the General tab. Four categories:
Startup & tray, Layouts bar, Flyout shortcut and Support. Startup moved out of
Workspaces; its existing owned-shortcut backend is unchanged. Confirmed startup
enable/remove acts immediately, even if other draft edits are canceled. Bar
visibility/reset/new position lock and shared flyout key remain Save/Cancel drafts.
Support opens local data folder/log with missing-file feedback. No theme controls
or sanitized diagnostic export yet. See docs/GENERAL-SETTINGS.md.

New src/GeneralSettings.cs is a partial SnapSettingsForm plus StartupControls;
all twelve source files compile dynamically in both modes. Optional schema-1
LauncherPositionLocked defaults false for old files, clones/roundtrips and blocks
drag movement/position writes without blocking clicks or explicit reset. No
resize math changed. General and Hotkeys share the flyout shortcut draft, also
refreshed by imports. Recovery still takes precedence when opening settings.

Verification against this change:

- Pure suite PASS: 198560 checks, including default/clone/XML bar lock.
- WorkspaceUi (now also General) PASS compiled and Windows PowerShell 5.1 STA:
  no registration on open, canceled/confirmed enable/removal in memory, immediate
  startup surviving settings Cancel, lock/reset/save/discard draft isolation,
  hidden-bar preservation, shared shortcut modal/display and missing-log feedback.
  Native shortcut roundtrips/removal still use a temporary folder only.
- LauncherUi PASS in both modes: actual painting through 70 DPI transitions,
  close/minimize-to-tray, bounded GDI, locked drag/no position callback and click.
- EditorUi PASS; full compiled and source-hosted Integration PASS including
  prior pair-only resizing/concurrent-source sample, flyout/hotkey/recovery,
  display notification and compatibility regressions. WorkspaceIntegration and
  cold compiled/hosted first-dialog checks PASS with updated workspace title.
- Final pure/General/launcher/hosted-source installer/cold hosted checks followed
  the last tray double-click wiring and Support copy change. Full integrations
  preceded those final UI-only adjustments; no geometry changes followed them.
- Rendered four General pages inspected. Real account startup was not enabled,
  no real workspace captured/restored and no real missing app launched. Opening
  local folder/existing log was not manually exercised; missing-log branch was.
- Application events 1000/1001/1026 sampled since development started: zero
  matching app/test crash events, query succeeded. No continuous tray monitoring.
  Isolated Chrome emitted its known profile/newtab stderr; compatibility passed.

The first integration attempt failed because an old test clicked the reset
button on a now-hidden tab. Test now selects General/Layouts bar; full suites
passed. An initial test-only variable naming compile error was corrected too.
Neither failure is an unresolved application defect.

Deployment: found the repo compiled controller running again, backed up and
stopped only that exact process for isolated integration. Rebuilt repo bin/ and
restarted with repo data/. Runtime/process verified preview.6.1, hidden bar,
recoveryRequired=false. Existing settings compare equal after excluding the new
default-false lock field. Real owned Startup shortcut and data/workspaces.xml
remain absent. Hosted delivery has all twelve matching sources and preserved
separate data; no second controller. Fallback binary/settings/hosted source and
before.json: work/general-settings-fallback. Existing ZIPs predate this update;
they were not refreshed. No publication or remote-state verification occurred.

Reports outside Git share outputs/windows-tile-studio-general- prefix:
final-ui.txt, workspace-ui.txt, integration-final.txt, hosted-ui.txt,
hosted-launcher-ui.txt, hosted-visibility.txt, launchers.txt, deployment.json and
windows-errors.json. Initial ui-tests.txt/integration.txt retain failed attempts;
use accepted later reports above. Rendered pages are under
bin/general-settings/workspace-ui-data/general-*.png. Fresh clones lack these.

Exact next steps: inspect status/log/remotes and read all five context documents
plus actual source before continuing. Follow the latest user request. **Phases
1–6 are complete within recorded scope; phase 7 (accessibility, visuals and
support) is next, not completed by this interlude.** Begin its keyboard/focus/
screen-reader/high-contrast/small-display audit as described in ROADMAP and
docs/POLISH-PHASES.md when requested. Preserve new General draft/immediate-action
tests plus prior resize/font/hidden-dialog regressions. Rediscover processes/data
before native tests; never trust an old runtime PID or run competing controllers.
Physical mixed-DPI/docking/sleep, real login, privileged/hung apps and production
install/update/rollback/removal/signing remain explicit release gates.

## Previous checkpoint: context review, 2026-10-28

Documentation-only review against local commit `7465c8d` on
`feature/polish-launchers`. Worktree was clean before this review. Origin is
`https://github.com/Brandonr91/windows-tile-studio.git`; no fetch/push or
publication was performed. Public revision is not freshly verified. Preserve
the independent original six-slot repository; reconcile histories before publishing.

Inspected current source, Build.ps1, Start-Hosted.ps1, root context, linked
milestone/phase notes and saved phase-6 evidence. All eleven src/*.cs files are
present; ProductVersion is `0.3.0-preview.6`. Phases 1–6 are implemented within
the evidence below. Phase 7 is planned. No application functionality changed;
no new build, desktop suite or hardware test was run during this review.

Fresh read-only operational check: repo runtime XML retains preview.6,
six-grid and hidden-bar/recovery-clear values, but its recorded PID had no live
process. Process inventory found no SixSlotSnap.exe or PowerShell command line
containing Start-Hosted.ps1. Treat assignments as stale, not verified live windows.
No controller was started/stopped. The actual Windows Tile Studio Startup
shortcut and repo data/workspaces.xml were absent. Other installations and
Windows startup mechanisms were not exhaustively audited.

Saved evidence re-read (previous runs, not rerun today): pure-final.txt reports
198558 PASS; visibility-tests.txt reports UI/native restore and cold compiled
dialog visibility PASS; hosted-visibility.txt reports cold hosted dialogs PASS.
The Application event report was checked on 2026-10-27 and reports zero matching
crashes; this is neither a fresh event query nor continuous tray-area monitoring.
All share the outputs/windows-tile-studio-phase-six- prefix outside Git and may
be absent in another clone. See phase 6 below for exact verification boundaries.

Resume steps:

1. Read AGENTS, ARCHITECTURE, ROADMAP, DECISIONS and this current checkpoint;
   inspect git status/log/remotes plus relevant source and uncommitted work.
2. Continue the latest user request. Context work is complete; do not begin
   phase 7 merely because it is next. If requested, follow root ROADMAP's exact
   steps and docs/POLISH-PHASES.md accessibility/visual/support criteria.
3. Before native tests/deployment, rediscover processes and the intended data
   directory. Last deployed mode was repo bin/SixSlotSnap.exe with repo data/;
   hosted delivery is a separate copy/data directory. Preserve hidden-bar
   preference, saved data and fallbacks. Never run competing controllers.
4. For later code changes, use focused test switches in AGENTS and Build.ps1.
   Do not claim old reports test new source. Keep real login, physical mixed-DPI/
   docking/sleep, elevated/hung apps and clean-account release gates open.
5. Synchronize these records with meaningful changes. Build/deploy/publish are
   separate operations, not consequences of updating documentation.

Everything below is dated history. Earlier headings and next-step instructions
describe their checkpoint only; they do not override this section or the current
roadmap. Older hosted/live-assignment claims are not current state.

## Previous checkpoint: phase 6

2026-10-27, following `3f6eee1`. Local `0.3.0-preview.6` implements explicit
workspace capture/preview/restore, a separate missing-app launch action, and
owned per-user login startup shortcut controls. Startup is OFF in the daily
installation, and no real workspace was captured/restored by the agent. Read
[workspace/startup guide](docs/WORKSPACES-AND-STARTUP.md) before changing behavior.
No public push, service, scheduled task or registry Run entry was added.

New files: Workspaces.cs (DTO/store/live identity/planning), WorkspaceForm.cs
(immediate user controls), StartupRegistration.cs (mode/data command and owned
Startup shortcut backend). Eleven src/*.cs files are compiled dynamically.
Local workspaces.xml is bounded to 1 MB/depth 32, backed up atomically, and
separate from settings/portable layout transfer. Captures use app EXE/class and
optional titles, never saved HWND/PID. Preview requires choices for ambiguity
and explicit disconnected-display fallback. Restore revalidates process start/
identity/current display/layout, preserves compatible unselected slots and
reports per-window failures. Relaunch is a separately confirmed local EXE with
zero arguments; it does not recreate app documents/URLs/sessions. Shared hosts
and inaccessible apps are skipped. Workspace/startup actions are immediate,
not settings drafts; placement hotkeys focus the open workspace dialog.

Startup uses Windows Tile Studio.lnk in the current user's Startup folder. A
dedicated shortcut avoids long hosted Run-command limits. No entry is created
on installation/open/status inspection. Enable explicitly preserves current
compiled or hosted source/data paths, Disable removes only an owned entry, and
unrelated/unreadable entries are preserved. Real startup was not enabled/tested
through login/logout; native shortcut tests use a fresh temporary folder only.
Start-Hosted.ps1 now forwards --source-root; Main/IPC adds --workspaces/0x8006.

Verification:

- Pure geometry/configuration/matching/store/startup checks PASS: 198558.
- Real-loop WorkspaceUiTests PASS: capture/preview/injected selected restore,
  launch guard, confirmation cancel/enable/disable with memory storage, painting,
  named selection and visible/footer bounds. Native Windows shortcut roundtrip,
  hosted arguments and removal tested only in an isolated temp folder.
- WorkspaceNativeTests PASS: two external disposable windows captured, saved/
  loaded, previewed and restored to actual saved visible frames; stale process
  reference refused; compatible unselected assignments retained. Explicit launch
  runs only an inert probe in a path with spaces/apostrophe, with zero arguments.
- Full compiled and hosted regressions PASS during this phase, including all
  prior resize/editor/hidden launcher/font/recovery/compatibility cases and
  opening/closing the real workspace UI without writing workspace files.
- Subsequent targeted native/UI tests cover docked footer/column repair,
  compatible-slot preservation and shared-host exclusion. Native readiness file
  publication had a test-only File.Exists/write race; atomic publication fixed it.
- Live inspection reproduced a hidden first workspace dialog after background
  startup with the bar off (native handle existed, WS_VISIBLE false). Queued
  explicit ShowManagedDialog after Load fixes both workspace and settings. Cold
  first-dialog visibility tests PASS for compiled and source-hosted starts. The
  corrected installed workspace dialog was then visually inspected and closed.
- Windows PowerShell 5.1 STA WorkspaceUiTests.ps1 and LauncherTests.ps1 PASS,
  covering in-memory source compilation and updated launcher/install handling.
- Window inventories and relevant Application crash events were sampled: no
  unexpected Windows/.NET error windows or matching crash events observed.
  This is not continuous notification-area monitoring. Physical mixed-DPI,
  docking/sleep, virtual desktops, elevated/hung apps and real login remain gates.

Current controller: compiled repo bin/SixSlotSnap.exe with repo data/, preview.6,
six recovered assignments, hidden launcher, no recovery warning. Settings hash
matches pre-phase; the real Startup shortcut stayed absent/unchanged. Hosted
src AND Start-Hosted.ps1 updated, separate data retained, no second controller.
Verify live process/runtime before tests. Fallback source/settings/startup
snapshot: work/phase-six-fallback; prior code is also Git 3f6eee1.

Reports outside Git use outputs/windows-tile-studio-phase-six- prefix:
full-tests.txt, final-tests.txt, workspace-final.txt, native-final.txt,
visibility-tests.txt, hosted-visibility.txt, hosted-ui.txt, pure-final.txt,
launcher-tests.txt, deployment.json and windows-errors.json. Full suites preceded
the later targeted fixes; the targeted/cold checks validate final affected paths.
Rendered workspace/startup PNGs are under bin/phase-six/workspace-ui-data and
copied to outputs for review. Source packages refreshed after the local commit.
Next planned phase is 7 (accessibility/visuals/support); follow latest user intent.

## Previous checkpoint: phase 5

2026-10-27, following `4ca3c9b`. User requested phase 5 and attention to Windows
errors near the clock. Local `0.3.0-preview.5` is implemented and deployed.
No public push, startup registration, saved app workspace or automatic relaunch.
Read [visual/portable guide](docs/VISUAL-AND-PORTABLE.md) before changing this flow.

New `VisualEditor.cs`: cloned draft session with one adjacent boundary per drag,
4% clipping, capped 50-gesture undo/redo and Escape; painted canvas/modal from
Layouts → Edit visually. Accepted geometry enters settings' existing grid history;
main Save remains the live apply point. Numeric editor retained. New
`PortableLayouts.cs`: schema-1 DTO, 256 KB bounded/DTD-prohibited strict XML,
atomic export and copy-first validation. Share layouts stages copied or explicitly
replaced catalogs, regenerates IDs/names, optionally imports shortcuts, rejects
conflicts before mutation and offers a confirmed pre-import checkpoint restore.
Local monitor/appearance/runtime data are excluded; OS key availability is still
checked on main Save. Example: examples/starter-layouts.tilelayouts.xml.

Verification:

- `Build.ps1 -Test -EditorUi -OutputDirectory bin/phase-five`: PASS, 198525 pure
  checks plus real-message-loop canvas pointer, Undo/Redo/Escape, painting,
  accepted settings draft/grid undo and import/failure/undo/discard coverage.
- Full compiled and hosted controller regressions PASS on the phase-5 functional
  implementation. Final preview.5 `-Test -LauncherUi -EditorUi -HostedIntegration`
  PASS after the version/status-label changes, including close-to-tray/no revival,
  font painting, concurrent-source resize, Recovery and compatibility subcases.
- Windows PowerShell 5.1 STA `tests/EditorUiTests.ps1`: PASS against final source,
  in-memory source hosting without an app EXE. `tests/LauncherTests.ps1`: PASS
  including updated eight-source compilation and install/data/archive validation.
- Final XML-depth preflight (max 32) was rechecked with pure/editor tests in
  both modes after the controller regressions; final pure count is 198525.
  Accepted report: outputs/windows-tile-studio-phase-five-editor-final.txt.
- Rendered visual/share pages inspected. Real mouse drag/Undo checked in a
  disposable preview through computer use: only center pair moved and restored.
- Windows window inventories sampled during testing/startup showed no unexpected
  error dialogs. Application events 1000/1001/1026 for the app/test process names
  since phase development began: no matching crash events. These are sampled
  checks, not continuous notification-area monitoring. Expected fit/hotkey warnings
  remain deliberate compatibility/recovery test cases.

`Build.ps1` adds optional OutputDirectory and EditorUi. Eight src/*.cs files are
loaded dynamically in both modes; no external dependency added. Initial normal
build was blocked by the running EXE; development used bin/phase-five until the
daily controller was paused for desktop tests. No stale running EXE was tested.

Current runtime is compiled repo bin/SixSlotSnap.exe with repo data/. It reports
preview.5, six recovered assignments, launcher hidden, recoveryRequired=false.
Settings hash matches the pre-phase snapshot. Hosted delivery src updated too;
its separate data is preserved and no second controller started. Fallback source
and both settings snapshots: work/phase-five-fallback (prior source also Git
4ca3c9b). Verify the actual process/runtime before any later desktop tests.

Reports outside Git: outputs/windows-tile-studio-phase-five-editor-tests.txt,
phase-five-full-tests.txt, phase-five-final-tests.txt, phase-five-hosted-editor.txt,
phase-five-launcher-tests.txt and phase-five-windows-errors.json (all share the
windows-tile-studio- filename prefix). Rendered PNGs under outputs with
phase-five-visual-editor/share-layouts names. Source packages refreshed after
the local commit. Physical mixed-DPI/docking/sleep, privileged/hung apps, keyboard
divider dragging and broader accessibility remain explicit gaps. Next planned
phase is 6 (opt-in workspaces/startup); follow the user's latest request first.

## Latest request: close the launcher to tray

2026-10-27, following `1f6302d`. User rejects the always-visible bar and requests
close-to-tray behavior. The bar now has an explicit × button. User WM_CLOSE and
minimize hide it and notify the controller; ShowLauncherTab=false is saved.
Flyout dismissal respects this preference, so it cannot resurrect the bar.
Tray Show/Hide Layouts bar explicitly restores/hides it; tray Exit still quits.
An open Appearance draft/checkbox is synchronized with this immediate action.
Shutdown/recreation uses Dispose, which is not canceled by the close handler.
Runtime XML reports launcherEnabled/Visible/Handle for verification.

Tests: real-loop LauncherUiTests now checks ×, WM_CLOSE and minimize hiding
without disposal, restoration, DPI painting and final disposal. Hosted integration
starts with a visible launcher, sends actual WM_CLOSE, verifies persisted false
and a live controller, exercises snapping/hotkeys, and checks flyout dismissal
does not revive the bar. See outputs/windows-tile-studio-close-to-tray-final-tests.txt
and the source-hosted launcher UI report for final results. Geometry checks remain
186297. Preserve prior physical/manual coverage limitations.

Final checks PASS: Build.ps1 -Test -LauncherUi -HostedIntegration and Windows
PowerShell 5.1 STA tests/LauncherUiTests.ps1. A development test exposed that raw
WM_CLOSE can bypass the UserClosing-only managed filter; WndProc now intercepts
the close request directly. State diagnostics do not recreate/access disposed
launcher handles. The real-controller close/persistence and no-resurrection checks
pass. Settings snapshots are under work/close-to-tray-before; ShowLauncherTab=false
is applied in both local data directories. Live compiled runtime confirms hidden
launcher, running controller, six assignments, no recovery warning.

Deployment keeps the user's compiled repo/data mode, deliberately sets only
ShowLauncherTab=false so the unwanted bar is hidden immediately, preserves its
position and other settings, and recovers six assignments. Hosted source updated
too without launching a second controller. Tray icon/hotkey remain the access
routes. No public push/new phase. Check live process/runtime before further work.

## Latest repeat phase 1–4 check

2026-10-27, starting at `53d589f`. Application source unchanged. PASS:
186297 geometry/configuration checks; compiled and source-hosted real-loop
launcher paint tests (70 DPI transitions each); full compiled integration;
final full hosted integration with recovery and compatibility subcases; legacy
supersession/restoration; source installer/hosting tests; archive source/content
checks. Both archives matched all six app sources and excluded runtime data/binaries.

Keep the failed attempts visible: first compiled run failed selector persistence
without recording a dismissal reason. A diagnostic compiled rerun passed. The
next hosted attempt could not acquire foreground focus before selector testing.
Test-only changes now capture foreign foreground HWNDs, mouse/Escape state and
refresh count on persistence failures, and retry the disposable target's focus
setup for up to two seconds. Final hosted run passed. The initial selector
failure's cause remains unconfirmed; do not label it fixed or definitively blame
user input. No application behavior was changed or new phase implemented.

Current compiled controller restored using repo `data/`; six assignments recovered,
recoveryRequired=false. Keep current data and verify actual process/runtime before
desktop work. The saved launcher position changed during this recheck interval;
only LauncherPositionSet/X/Y differ from the initial snapshot. Retain this latest
position rather than overwriting it. Snapshot: `work/phase-recheck-20261027-145329`.
Reports: `outputs/windows-tile-studio-phase-recheck-*`; accepted desktop runs are
`phase-recheck-compiled.txt` and `phase-recheck-hosted-final.txt`. Initial failures
remain in `phase-recheck-core.txt` and `phase-recheck-hosted.txt`. No public push.
Physical multi-monitor/docking/sleep, elevated/hung apps and remaining manual UI
coverage remain open, as described in docs/PHASE-AUDIT.md. Preserve this distinction
when answering status questions or resuming development.

## Latest fix: launcher paint exception

2026-10-27, following `1a3c193`, user reported the .NET "Parameter is not valid"
dialog and a red-cross launcher. A disposable launcher with a real message loop
reproduced the exception in System.Drawing.Graphics.DrawString / Label.OnPaint
against the pre-fix binary. DPI font replacement disposed a font still retained
by label/native rendering. The previous audit did not test actual launcher paint;
its prior pass must not be treated as coverage of this lifetime bug.

LauncherTab now caches fonts by DPI, explicitly updates child font references,
reuses cached fonts, and disposes them only after base/child disposal. No global
exception suppression was added. `Build.ps1 -LauncherUi` runs the new real-loop
test. Windows PowerShell 5.1 -STA `tests/LauncherUiTests.ps1` compiles the same
source/test in memory, without an app EXE, and can export a rendered image.
Both passed 70 synthetic 100/125/150/200% transitions, correct label sizes,
initial/repeated painting, bitmap rendering, bounded GDI count and disposal.
`Build.ps1 -Test -LauncherUi -HostedIntegration` passed: 186297 pure checks plus
the full hosted resize/settings/recovery/compatibility suite. Screenshot of the
fixed label was rendered and visually inspected. Physical mixed-DPI hardware
and live mouse drag remain separate coverage limits.

IMPORTANT runtime change: when inspected, the old hosted controller had stopped
and the user had started the compiled repo app using repo `data/`. The fix was
rebuilt and this SAME compiled mode/data was restarted; all six windows recovered,
recoveryRequired=false. The hosted delivery source was also updated but no second
controller started. Check repo `data/runtime-state.xml` first, verify process/path,
and do not assume the old hosted runtime XML is live. Keep both data directories.
Settings snapshots: `work/launcher-font-fallback/{compiled,hosted}-settings.xml`.
Reports: `outputs/windows-tile-studio-launcher-paint-before.txt`,
`windows-tile-studio-launcher-font-regression.txt`,
`windows-tile-studio-launcher-font-hosted-ui.txt`; rendered label:
`outputs/windows-tile-studio-launcher-fixed.png`. Before-fix stack is accepted
reproduction; final tests are the accepted verification. Packages refreshed after
the local fix commit. No push/new phase. Continue the user's latest priority.

## Latest verification: prior-phase audit

2026-10-27, user requested a check of all prior phases. Starting from `d8852bf`,
the first compiled suite passed but hosted repetitions exposed an intermittent
lost final resize sample. `UpdateDrag` read a fresh rectangle after applying
neighbors and advanced bookkeeping over movement that had never been modeled.
It now advances only to the rectangle used for that sample. The new synchronous
neighbor-triggered source-movement regression fails against isolated pre-fix
`d8852bf` and passes after correction in both launch modes. Release diagnostics
record frame coordinates in the existing local log.

Final `Build.ps1 -Test -Integration -HostedIntegration`: PASS, 186297 pure checks
plus both native suites and their compatibility/recovery subcases. Added direct
all-direction navigation/clamp/layout-wrap checks pass. PowerShell 5.1 legacy
supersession and source-hosting/installer suites PASS. See the full
[phase audit](docs/PHASE-AUDIT.md) for each function's actual coverage and limits.
An experimental supplemental in-process harness was inconclusive/stopped and
removed; its launcher checks are not accepted verification. Live launcher drag,
About/reset confirmation, physical multi-monitor/docking/sleep, elevated/hung
apps and every physical event timing remain manual/beta coverage.

Daily hosted source updated with the correction; restarted with all six original
assignments recovered and no recovery warning. Preserve data. Pre-audit settings
and runtime snapshot: `work/phase-audit-20261027-134223`. Reports are under
`outputs/windows-tile-studio-all-phases-*.txt`, outside Git; full accepted run is
`windows-tile-studio-all-phases-corrected-regression.txt`, and the baseline
expected failure is `windows-tile-studio-all-phases-before-fix.txt`. Source packages
refreshed after the audit commit. No public push or new phase implemented.
Resume the user's latest request after reading actual Git/runtime state; phase 5
remains the next planned development phase, not part of this audit.

## Previous checkpoint: phase 4

2026-10-27, following `9785a92`. Phase 4 core is implemented locally as
`0.3.0-preview.4`. Read [display support/verification](docs/DISPLAYS.md) and
the phase plan alongside the actual repository before continuing. Next requested
phase is phase 5: draft visual builder and portable layout/hotkey import/export.
No public push or production release was performed in this phase.

Implemented: `DisplayState.cs` native inventory and pure `MonitorWorkspace`;
optional schema-1 monitor records; independent selected profiles, proportions
and HWND arrays; shared names/grids/hotkeys; same-monitor ownership checks;
debounced display/work-area/resume refresh; disconnect releases transient HWNDs,
retaining preferences; reconnect can recover matching visible geometry. Reflow
verifies eligible same-monitor frames and releases unsupported assignments.
No cross-screen return, app relaunch, startup registration or OS display change.
Launcher creation/DPI changes/topology refresh scale and clamp its visible bounds.
Settings banner identifies its monitor, retains drafts on disconnect, merges
current monitor snapshots and preserves untouched concurrent geometry.

Verification on final source:

- `Build.ps1 -Test`: PASS, 186297 geometry/configuration checks, including
  simulated two-monitor negative-origin/different-size geometry, independent
  profiles/assignments/heights, restart, disconnect/reconnect, work-area flags,
  clone/XML migration, concurrent draft merge, profile removal/shared shape.
- `Build.ps1 -HostedIntegration`: PASS, native synthetic display/work-area/resume
  notifications preserve six disposable frames/assignments on unchanged hardware;
  existing full resize/flyout/editor/hotkey/recovery and compiled compatibility
  subcases pass. Banner layout has a non-overlap regression check.
- Focused `Build.ps1 -Compatibility`: PASS, including minimums, pair-only 3/4-row
  sides, lifecycle, delayed query and isolated Chrome. Final full run: 20 corner
  updates 1480 ms including deliberate pumps/settling, 125 ms controller CPU;
  idle one-second sample 0 ms; no growing GDI count. These are short local samples.
- Windows PowerShell 5.1 `tests/LauncherTests.ps1`: PASS, source-only hosting,
  installed-source validation, repeat installs/data preservation and unsafe ZIP
  rejection. No network download test was needed for this unpublished source.

During development, a notification test needed synchronous delivery for Windows'
pointer-bearing WM_SETTINGCHANGE; PostMessage was rejected. A settings title
suffix broke an exact-title recovery lookup, so display context now lives in a
banner. A later compatibility run exposed resize timing failure while device
enumeration was on the drag loop. Affinity checks now use cached native monitor
handles there; inventory/target selection resolves persistent IDs outside that
loop. Focused and full suites passed after the correction.

Available hardware remains one physical 5120 x 2160 display at 125%. No physical
second-monitor/mixed-DPI movement, docking/unplug, taskbar/scale change or actual
sleep/wake cycle was tested. Pure topology and synthetic notifications do not
close those production gates. Identity can change across ports/drivers; fallback
GDI keys are weaker. No record-removal UI or saved app workspace exists. Older
controllers may discard new monitor fields on save; use the fallback settings
snapshot for rollback. Phase-3 elevated/hung-app limits still apply.

Final source is installed in `outputs/windows-tile-studio-hosted/src`; daily
runtime reports preview.4, recoveryRequired=false, one connected monitor and all
six existing assignments. Verify its current PID/command line from runtime XML
before desktop work; these values are transient. Preserve daily data. Previous
source/settings snapshot: `work/phase-four-fallback`. Reports outside Git:
`outputs/windows-tile-studio-phase-four-integration-results.txt`,
`outputs/windows-tile-studio-phase-four-compatibility-results.txt`,
`outputs/windows-tile-studio-phase-four-launcher-results.txt`.
Source packages refreshed after the local commit; original six-slot repository
and legacy redirect configuration are unchanged.

Next steps: inspect status/log/remotes and source; follow phase 5 in
`docs/POLISH-PHASES.md` when requested. Begin with a draft-only visual divider
editor, numeric fallback and undo/cancel/save tests, then validate portable
imports without exporting monitor IDs/HWNDs/launcher location. Use isolated
desktop windows for behavior changes, never another controller alongside daily.
Physical display tests remain a separate explicit release checklist.

## Previous checkpoint: phase 3

2026-10-27, following `11f217a`. Phase 3 core is implemented locally as
`0.3.0-preview.3`. The original user workflow, pair-only heights, source-only
host and legacy preference are preserved. Next is phase 4 display/per-monitor
state, not a production release. See [compatibility evidence](docs/COMPATIBILITY.md)
and the phase plan before continuing after compaction or a new session.

Implemented: reported minimum constraints in transient model arrays; verified
visible frames before assignment; failed-fit feedback/best-effort restore;
background minimum probes with explicit thread DPI context, timeout/cancellation;
height-boundary ownership pinned even while probes are pending; limit no-op
restoration; closed-window event cleanup; explicit reconnect after restore/restart.
Runtime XML now reports resizing/source and the last placement issue.

Reproduced failures included an oversized app falsely marked snapped and closed
windows left in runtime assignments. Development also exposed worker DPI drift
and tests relying on fixed native-event timing. Tests now acknowledge source
ownership/completion; synthetic grouped-neighbor duplicates remain raw events.
No temporary DEBUG logging remains in source.

Final checks: `Build.ps1 -Test` PASS (186177); `Build.ps1 -HostedIntegration`
PASS, including phases 1–2 and compiled compatibility subcases; focused
`Build.ps1 -Compatibility` PASS twice with slow response, app minimums, at-limit
drag, 3/4-row dividers, corners, borderless/tool frames, minimize/maximize,
close/recreate, and isolated Chrome. Latest full-run corner sample: 20 updates,
1487 ms including deliberate pumping/settling, 47 ms controller CPU; one-second
idle sample 16 ms CPU; no growing GDI-object count. Short samples have finite
accounting resolution and are not universal performance guarantees.

Elevated apps and completely hung targets were not manually validated.
SendMessageTimeout bounds sizing queries; SetWindowPos/DWM still have no overall
hard deadline. Other frame/aspect/max constraints and native mouse timing need
broader beta coverage. Do not expand the tested matrix into an all-app claim.

Updated source is installed in the daily hosted folder; preserve its `data/`.
The prior source/settings fallback is under `work/phase-three-fallback`.
Reports are `outputs/windows-tile-studio-phase-three-test-results.txt` and
`outputs/windows-tile-studio-phase-three-compatibility-results.txt` outside Git.
Changes remain local until separately published; original repository untouched.

## Previous checkpoint: phase 2

2026-10-26 following `c56b6a7`. Phase 2 is implemented locally as
`0.3.0-preview.2`: discard/keep-editing close protection; grid replacement/removal
confirmation and pre-save grid undo; live proportions reset confirmation;
actionable save errors retaining the draft; startup Recovery page for hotkey
conflicts and corrupt settings without replacing the file. Failed save rollback
uses actual registered binding snapshots, including an empty recovery set.

Legacy supersession now supplies standalone Restore-SixSlotSnap.cmd/.ps1 helpers
and actionable missing-source guidance. Setup supports enable/disable/enable
with identical original backup and Windows PowerShell 5.1 XML assignment.
Both local opted-in legacy folders were refreshed with the updated guard/helpers.

Validation: 186129 geometry/configuration checks pass, including blocked-save
byte preservation. Hosted six-window/editor suite passes; startup-conflict and
corrupt-file subcases launch the compiled controller with isolated data and
verify recovery UI, unchanged file and clean exit. Editor tests cover keep/discard,
confirmed/rejected grid replacement, undo and failed-save retention. Confirmation
answers are injected for deterministic editor tests; tray reset dialog was not
manually clicked. Supersession/standalone restore tests pass under Windows
PowerShell 5.1. Recovery screen rendered and inspected.

During development the new tests exposed a modal-close exit check and a test
reader file-share race; both were fixed and the suite rerun successfully.
Final source deployed to daily hosted delivery; runtime reports preview.2,
recoveryRequired=false and six assignments. Source fallback and settings snapshot
are under `work/phase-two-fallback`. Changes remain local until publication.
Next: phase 3 compatibility and responsiveness, using disposable minimum-size
and 3-row cases before changing resize behavior; see the phase plan.

## Previous checkpoint: phase 1

2026-10-26, following `d3a430f`. User requested phased polishing, including
all optional features from the production scope. See [phase plan](docs/POLISH-PHASES.md).
Phase 1 is implemented locally as `0.3.0-preview.1`: Appearance tab for bar
visibility/draft reset, immediate tray bar reset, About/version, consistent
visible names and no whole-grid reflow for appearance/name/hotkey-only saves.
The old top-of-screen checkbox also overlapped the preview; it is now removed
from Layouts. Phase 2 is editing safeguards and startup/failure recovery.

Verification: geometry/configuration PASS (186128 checks); hosted six-window
integration PASS, with new Appearance save and draft-cancel isolation checks.
Appearance screenshot rendered and visually inspected. Updated source deployed
to the separate daily hosted folder; preserve its settings. Source delivery
and context changes remain local until published. Next steps are in the phase
plan; do not treat later optional features as implemented or enable startup.

## Latest checkpoint: tray identity and legacy preference

2026-10-26, after launcher-placement commit `b3f941f`. User asked whether the
old tray label indicated a second controller, requested legacy supersession,
then requested a Windows Tile Studio tray entry/icon. Inspection found only the
hosted full manager; the old tooltip was a naming artifact.

Implemented: renamed its existing tray entry and added an owned blue/cyan six-tile
icon. Added `Set-LegacySupersession.ps1`, `tools/LegacyRedirect.cs`, and isolated
`tests/SupersessionTests.ps1`. The option is enabled locally for both
`outputs/SixSlotSnap` and `outputs/six-slot-snap/bin`, pointing to the daily
`windows-tile-studio-hosted` source/data. Original EXEs remain as
`SixSlotSnap.legacy-original.exe` in each folder. Old normal launch paths now
start the full manager, including when it was stopped, and do not create a
second controller when it is running. Untouched copied binaries elsewhere are
outside this option's scope. The original public repository is unchanged.

Tests: geometry/configuration PASS (186126 checks); supersession fake-process
test PASS for close, backup preservation, re-enable, redirected start and byte-for-byte
restoration. Hosted desktop suite passed on retry after one focus-sensitive
flyout-open failure; no flyout behavior was changed to hide that timing failure.
The tray icon was rendered and inspected as six blue/cyan tiles. Local launches
through both old paths retained the same full-manager PID and six assignments.

Disable supersession before using the previous-controller recovery helper:
run the setup script with each intended `-LegacyDirectory` and `-Disable`, then
exit the full manager and start the restored original. Never run a name-wide
process kill. Current changes are local until separately published; do not infer
GitHub main matches this checkpoint. Next: gather user feedback on the tray entry;
continue unrelated roadmap work only when requested.

Last update: 2026-10-26 (America/Chicago), following context checkpoint
`1aac7f3` on `feature/polish-launchers`. Latest request: move the persistent
Layouts bar near the taskbar clock and make it movable.

## Latest completed change

The bar defaults just above the primary taskbar's right end. Left-drag moves it
without opening the flyout; normal click still opens it. New optional schema-1
settings fields preserve its coordinates; restored/dragged bounds are clamped
to the screen work area. Manual StartPosition fixes Windows overriding the
initial location. This is launcher placement, not full display-change recovery.

`Build.ps1 -Test` passed 186126 checks, including launcher position cloning/XML
round-trip and negative-origin clamping. `Build.ps1 -HostedIntegration` passed
the existing six-window suite. After the final manual-position correction,
geometry tests passed again. A disposable launcher using the production mouse
handlers received actual drag and click input: drag callbacks fired without
an open callback, then a normal click fired the open callback. Its test-only
taskbar visibility differed from the production nonactivating tool window.

The updated source is installed in the daily hosted delivery; all six slots
recovered. User data was preserved, with a local fallback under
`work/launcher-position-fallback`. This change and context docs are local only;
public main still predates the context checkpoint unless independently updated.
Next: gather user feedback on placement; only continue roadmap work when asked.

## Read before continuing

Read [AGENTS](AGENTS.md), [ARCHITECTURE](ARCHITECTURE.md),
[DECISIONS](DECISIONS.md), [ROADMAP](ROADMAP.md) and its linked milestone history.
Then inspect actual Git state and relevant source/tests. These records are a
dated checkpoint, not a substitute for the checkout or current user instructions.

## Repository and delivery checkpoint

- Intended remote: `https://github.com/Brandonr91/windows-tile-studio.git`.
  The original `Brandonr91/six-slot-snap` must remain separate and untouched.
- Application changes in local history: `a714b49` hosted/source launchers,
  `18d2156` pair-only height fix, `99fc839` keyboard polish,
  `48b96b2` documentation clarification. Worktree was clean before this task.
- Read-only remote check during this task returned public main
  `cfdb2df728466b1256383cf9b8417b21a486a1ca`.
  Publication used GitHub's signed-in web editor because Git/connector write
  access was unavailable. Public and local commit histories therefore differ.
  The previous turn cloned public main and compared normalized text for every
  tracked local file with no mismatches. Do not force-push the local history.
- This context update is local until separately published; do not infer that
  new documentation is already on public main. Inspect status/log to determine
  whether this documentation checkpoint has since been committed.

## Local environment checkpoint (not portable configuration)

The development checkout is `outputs/windows-tile-studio` under this Codex task
workspace. `outputs/windows-tile-studio-hosted` is the separate daily source-only
delivery, with its own `data/`. Editing the checkout does not update that copy.
There are older `six-slot-snap` folders and verification clones: do not mistake
them for the development repository or push from them.

The latest runtime snapshot showed a PowerShell-hosted controller with all six
zones occupied in profile `six-grid`, flyout closed. The launcher change paused
and restarted the daily controller for isolated testing and deployment. Process
IDs and HWNDs are transient;
re-read the runtime XML, verify the live process/command line, and confirm its
source/data location before interacting. Preserve daily `data/settings.xml`.

## Verified evidence and limits

Previous development-turn results, re-read during this inspection:

| Check | Recorded outcome / scope |
| --- | --- |
| `Build.ps1 -Test` on local and fresh public source | PASS, 186123 geometry/configuration checks. No desktop movement. |
| Public-source `Build.ps1 -HostedIntegration` | PASS: six disposable windows, linked widths, each row boundary from both sides, stale flanking-height preservation, native-style group correction, target preservation, flyout click, actual Right/Enter and Tab/Shift+Tab, key release/conflict rollback, settings editor save. |
| Windows PowerShell 5.1 `tests/LauncherTests.ps1` | PASS: source loading/serialization, no standalone app EXE, quoting, repeat installs, user-data preservation, unsafe ZIP paths/aliases/reserved names, older-source fallback, unrelated-folder rejection. |
| Fresh GitHub Hosted install with `-NoLaunch` | Retrieved public `cfdb2df...`; installed source `-Action Validate` returned empty assembly location and six profiles. |
| Hosted delivery/package | Six daily assignments recovered; package contained source/launchers, no app EXE/DLL or user settings. |

Workspace evidence includes `outputs/windows-tile-studio-polish-test-results.txt`,
`outputs/windows-tile-studio-launcher-test-results.txt`, and public verification
clone `work/polish-public-verification`. These files are outside this repository
and may not exist in another session/clone. Re-run relevant tests when code
changes; do not rely only on a saved PASS file or stale integration-error file.
Latest keyboard coverage does not prove all arrows/layout shapes, all native
mouse timing, app minimums, privilege levels or multi-monitor behavior.

The earlier context-only task reviewed existing evidence. New launcher-change
verification is recorded above; no new broad compatibility claim is made.

## Exact next steps

1. Run `git status --short --branch`, `git log -5 --oneline`, `git remote -v`.
   Review uncommitted work before editing. If publication is requested, fetch
   remote history and compare content; reconcile without overwriting either
   the original repository or independent public commits.
2. Resume the user's latest request. With no new feature priority, use the
   proposed order in root ROADMAP: first add a disposable-window reproduction
   for minimum-size and 3-row resize ownership. The launcher request is complete;
   do not automatically begin unrelated functionality.
3. For code changes, run `./Build.ps1 -Test`. It does not move desktop windows.
   For resize/flyout/hotkey changes, arrange a controlled pause of the daily
   controller and run `./Build.ps1 -HostedIntegration` with isolated data;
   `-HostedSourceRoot <source-only-folder>` can verify a clean hosted fixture.
   Never launch an integration controller alongside the daily one.
4. For launcher changes, use Windows PowerShell 5.1:

   ```powershell
   & "$env:WINDIR\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -STA -ExecutionPolicy Bypass -File .\tests\LauncherTests.ps1
   ```

   Add `-Network` only when testing the currently public download is relevant.
   Validate source independently in a temporary data directory with
   `Start-Hosted.ps1 -Action Validate` under the same PowerShell 5.1/STA host.
5. Before deploying, preserve the user's data, compare intended source files,
   explicitly rebuild compiled mode or restart hosted mode, and verify the
   running process, hotkey and recovered slots. Update only the intended daily
   delivery; keep a fallback. Publication and startup are separate actions.
6. Update these context records with meaningful changes, verification and the
   next stopping point. Update README/launching notes or milestone history
   when their user-facing claims change. Keep proposed work labeled as planned.
