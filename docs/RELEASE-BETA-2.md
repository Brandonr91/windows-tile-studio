# Windows Tile Studio 0.3.0-beta.2 — release draft

Prepared 2026-10-29. This is a reviewed local beta candidate, not a published or
production-certified release. Packages are unsigned. No tag, GitHub Release or
push was performed while preparing this document.

## What this beta provides

Windows Tile Studio grew from a six-window layout on a 5K2K display: linked
column widths, with each height divider controlling only its two adjacent windows.
It adds a persistent snap-like selector, editable layouts/hotkeys and a visual
proportion editor while keeping that independent-height behavior.

- General settings for optional login startup, tray/bar behavior and local support.
- Closing the Layouts bar hides it to tray; saved visibility stays off until restored.
- Per-monitor layout preferences, draft editing/undo and validated portable layouts.
- Explicit workspace capture/preview/restore and separately confirmed app launch.
- Local aggregate diagnostic preview/export, keyboard editing and accessible labels.
- Source-hosted and compiled delivery, verified install/update, guarded rollback
  and data-preserving uninstall. Incompatible legacy settings selections are blocked.
- Beta.2 names the settings page selector and Hotkeys Change/Clear action headers.

This app controls window geometry through Windows APIs; it does not add layouts
to Windows' built-in Snap catalog. Runtime is local, with no AI service, account,
API key or telemetry dependency. The compatible internal executable name remains
SixSlotSnap.exe; its product/tray identity is Windows Tile Studio. The predecessor
six-slot-snap repository remains separate.

## Start and update

Extract a complete archive to a short writable folder. Source users can launch
Start-Hosted.cmd: Windows PowerShell 5.1 STA compiles the C# sources in memory
without a standalone application EXE. PortableCompiled users can launch Start.cmd.
Exit another running controller before launching a different copy. Login startup
is a separate explicit option under General; installation does not enable it.

For versioned installs, retain all installer companions and use Get-FromGitHub.ps1
with the reviewed local archive, its ExpectedSha256, chosen Mode and -NoLaunch.
Then use the installed Start.cmd when ready. See [release/recovery](RELEASE-AND-RECOVERY.md)
and [launching](LAUNCHING.md). Back up data/ before changing versions. Older
standalone EXEs/launchers do not acquire the new downgrade guard automatically;
never assume they can preserve a newer settings file.

Codex recreation/extension guidance and project origins are in
[the README](../README.md), [project notes](PROJECT-NOTES.md) and the five root
context documents. Start by reading the actual source/context and retain adjacent-
pair height, hidden-bar, explicit startup and local-only regression checks.

## Frozen reviewed artifacts

Both archives were built from clean source revision
`89613a20b63448e7b6d4a44dc27e81c63a117579`. Source has 79 archive entries;
PortableCompiled has 80, adding bin/SixSlotSnap.exe. File/product versions are
0.3.0.10 / 0.3.0-beta.2. Both include license, source, docs and recovery tools.

| Archive | SHA-256 |
| --- | --- |
| WindowsTileStudio-0.3.0-beta.2-Source.zip | d15cf8507054adfa55e3f9ad959302dba3a1a107ff1943ea9547dc61d8e7f0aa |
| WindowsTileStudio-0.3.0-beta.2-PortableCompiled.zip | f46f9e703fd17ff02128ea445676bc4f58451526f8a81add2daf576a200bb905 |

Local artifacts are bin/beta2-releases/; the existing local kit is bin/beta2-kit/.
release-info.json, SHA256SUMS.txt and per-file manifests describe exact bytes.
These files were not replaced by later history/review documentation commits.
Source ZIP repeatability applies to identical source/provenance; legacy compiled
PE metadata is not deterministic. Checksums are integrity evidence, not publisher
authentication. Do not silently rebuild and overwrite these reviewed filenames.

## Verified scope

- Exact beta.2 ReleaseTests: repeatable source, version/icon/manifest/checksum,
  install/repeat/update/rollback, failed activation, tamper/path/link refusal,
  retained data/reinstall, synthetic purge and scoped owned/unrelated startup.
- Exact beta.2 InstalledLaunchTests: stable installed Hosted and Compiled launch,
  mode/version/path/receiver identity, hidden bar, no Recovery or real-window
  adoption, running-install removal refusal, rollback launch and normal exit.
- Exact beta.2 PublicUpgradeTests: actual public cfdb2df source to beta.2 with
  synthetic names/geometry/keys, blocked lossy install/rollback/legacy launch,
  explicit compatible fixture-backup rollback and separate workspace-byte retention.
- Beta.2 geometry/configuration: 198564 checks. Compiled/hosted accessibility,
  compiled General/workspace, live current-installation keyboard/provider-tree checks.
- Prior scoped resize, restricted-app and resource observations are recorded in
  [compatibility](COMPATIBILITY.md), [beta testing](BETA-TESTING.md) and HANDOFF.

Package inventories exclude private settings, workspace files, logs, shortcuts,
runtime snapshots and test output. A targeted current-source/reachable-path review
found no matching known token/private-key patterns, personal account path/email
or private runtime payload names; this is not a guarantee against every secret.
All destructive lifecycle checks used disposable folders on this existing Windows
installation. Fresh-account/VM testing was waived by the user. Daily settings and
Startup bytes remained unchanged; the live app retained six assignments, hidden
bar and no Recovery. Windows Application crash-event sampling found no matches;
sampling is not continuous monitoring.

## Known limits

This candidate does not establish actual screen-reader speech, Windows appearance
switching, physical mixed-DPI/docking/sleep, real logout/login, elevated/inaccessible
apps, competing tilers, multi-day stability or broad external beta coverage.
No personal workspace file exists in the daily installation, so that personal
capture/backup scenario remains untested. Existing workspace fixture tests pass.

A target freezing after a successful size probe can still stall synchronous
placement/restoration; already-hung rejection tests do not remove that race.
App-specific frame restrictions can prevent a fit. Separate user-selected themes/
text sizing, arbitrary overlapping zones and document/session recreation are not
implemented. GitHub CI is configured locally but has not run remotely. Signing
and a production release remain separate decisions.

## GitHub review preparation

Fetched public main is `cfdb2df728466b1256383cf9b8417b21a486a1ca`. Its tracked
contents match local baseline `48b96b20d596dda0ecf00602e49487f29b474c47` when
ignoring line endings. The common ancestor is `51ffa887b822fc080d7297826a869f34937a4c36`.
The old independently recorded histories produce automatic merge conflicts even
though that baseline is equivalent.

Local merge `d056d170377271da41c6391bc0c4defe889bb1be` retains both histories:
parents are the tested 89613a2 source and public cfdb2df. Its entire tree is
unchanged from 89613a2. This was an intentional history-only merge after verifying
equivalence, not a blanket resolution of unknown upstream edits. Public main is
now an ancestor of the local review branch; force push is unnecessary. Later
review docs add no application change.

Before an authorized push, fetch again and recheck ancestry and the public diff.
If upstream changed, review those changes before proceeding. Push a review branch
and let the configured read-only Windows CI run; do not claim a pass in advance.
Review/merge through the normal GitHub flow, preserving the original repository.
If publishing these frozen assets, the version tag must identify their exact
source revision 89613a2, not a later documentation-only commit. A prerelease should
carry these checksums, scope/limits and unsigned status. No publication step in
this section has been executed.
