# Preview packages, install and recovery

Phase 8 prepares local delivery tooling for `0.3.0-preview.8`. It does not publish
a release, sign binaries, claim a clean-account pass or close hardware/beta gates.

## Build and verify

Run Windows PowerShell 5.1 from a complete checkout:

```powershell
.\Build.ps1 -Test
.\tests\LauncherTests.ps1
.\tests\ReleaseTests.ps1
.\Build-Release.ps1
```

The builder requires a clean checkout; `-AllowDirty` explicitly permits a local
preview and records that fact. `release/version.json` must agree with the source
version. Source and optional compiled-portable ZIPs are named by product/version/
kind; both contain the source, docs, license and recovery tools. Source contains
no EXE/DLL; PortableCompiled additionally contains `bin/SixSlotSnap.exe`.
Internal executable/mutex identities stay compatible; EXE product/file metadata
and its embedded six-tile icon identify Windows Tile Studio.

`package-manifest.json` lists every payload file, byte length and SHA-256, plus
version, kind, source revision/dirty state and unsigned status. `SHA256SUMS.txt`
and `release-info.json` identify archives. Inputs are reviewed in
release/package-files.txt; user settings, logs, runtime XML, workspaces, backups
and machine helper folders are excluded. ZIP entry order/timestamps/compression
are canonical: the source ZIP repeats byte-for-byte for identical input bytes
and provenance using Windows PowerShell 5.1. Calls from PowerShell 7 delegate
packaging to 5.1 so JSON/ZIP runtime differences do not change the result.
The legacy compiler emits changing PE metadata/MVIDs; compiled
ZIPs are checksummed per build, not claimed byte-reproducible.

Packages are **unsigned previews**. Hashes detect mismatched bytes relative to
the supplied checksum/manifest; a checksum shipped by the same untrusted sender
does not authenticate code. Review/trust the source and obtain checksums from a
trusted channel. No certificate was used or installed. Do not bypass a Windows
warning blindly; signing and publisher trust remain a release decision.

## Install/update without replacing data

Extract the full source/portable bundle into a short writable path. Keep
Get-FromGitHub.ps1, InstallationSupport.ps1 and Manage-Installation.ps1 together;
the installer is no longer a standalone one-file download.

```powershell
.\Get-FromGitHub.ps1 -ArchivePath 'C:\Downloads\WindowsTileStudio-0.3.0-preview.8-Source.zip' -ExpectedSha256 '<64-character hash from SHA256SUMS.txt>' -Mode Hosted -NoLaunch
```

Replace the example hash with the real value. Default InstallRoot is the
current user's LocalAppData/WindowsTileStudio; choose an empty dedicated folder
or an existing owned installation. Hosted stays source-only. Compiled can build
a source package or use a verified PortableCompiled payload. For online download,
omit ArchivePath and select the intended published `-Ref`; public main is not
assumed to contain this local preview. No GitHub token/admin/service is required.

The installer bounds/examines archive paths, rejects links/Windows aliases,
checks an optional expected archive hash and file manifest, then validates the
candidate in staging with isolated validation data. Hosted compilation uses
PowerShell 5.1 STA; Compiled builds before activation. A portable EXE is retained
and its identity checked while source validation builds elsewhere. Legacy raw
source archives receive a local integrity manifest after validation. Validation
has a 60-second child-process deadline; it executes selected package scripts and
is not a security sandbox. Very long paths can exceed legacy compiler limits;
choose a shorter install/staging path if validation reports that error.

Only a successful candidate changes current.json; previous.json retains the prior
version/mode for rollback. Failed validation/hash/cache checks preserve the active
pointer/data. Pointer-write failures restore previous-pointer contents too.
Identical reinstall keeps the active pointer/rollback history. Modified cached
source or recorded compiled binaries are refused, rather than silently overwritten.
Installed launch validates the selected source/artifact again. Keep custom source
changes in a development checkout and package a new version instead of editing
immutable versions. These checks are integrity guards, not protection against a
malicious local user who can replace the launcher/manifests together.

The installed Start.cmd reads current.json through Launch-Installed.ps1. Updates
do not kill the existing controller: exit it from its tray and start again when
ready. `-NoLaunch` suppresses controller startup, not validation/building. To
change execution mode, run the installer with the desired Mode; an unverified
one-off launcher mode override is refused. Data remains in a separate data/.

## Rollback and uninstall

Exit this installation before management operations. Other installations and
unrelated processes are not stopped. From the installed folder:

```powershell
.\Manage-Installation.ps1 -InstallRoot $PWD -Action Status
.\Manage-Installation.ps1 -InstallRoot $PWD -Action Validate
.\Manage-Installation.ps1 -InstallRoot $PWD -Action Rollback
.\Manage-Installation.ps1 -InstallRoot $PWD -Action Uninstall -WhatIf
.\Manage-Installation.ps1 -InstallRoot $PWD -Action Uninstall
```

Rollback/Uninstall.cmd offer confirmed versions of those actions. Rollback checks
the previous code/artifact and swaps the pointer; invoking it again switches back.
It does not restore settings backups: preserve data/ before trying versions with
different schemas. No data migration is silently undone. A missing/corrupt prior
version leaves the active pointer intact.

Uninstall removes owned versions/launchers/pointers and only the owned Startup
shortcut targeting that installation. Data and unrelated root files stay by
default; a small ownership marker stays so reinstall can reuse retained data.
`-RemoveData` is an additional explicit destructive option, with confirmation.
Linked/reparse paths are refused before recursive deletion. Tests exercise all
deletion only in fresh owned temporary fixtures. Back up custom code/data first;
uninstall does not hash-backup every personal edit inside versions/.

## Login startup and older installations

No installation/update/rollback enables login startup. New opt-ins from versioned
installations target the stable Launch-Installed.ps1, which follows current.json
across updates/rollback while retaining the same data directory. Ordinary checkout
and standalone hosted launches keep their existing path-preserving behavior.
Older version-specific shortcuts are preserved automatically; explicitly re-enable
startup once in General if you want to migrate one to the stable target. Unrelated
startup entries stay untouched. Real logout/login remains a manual release gate.

The original six-slot repository is separate. Preserve its optional per-folder
guard/original backup and standalone Restore-SixSlotSnap helpers; restore those
legacy paths before removing a manager they reference. Uninstall does not search
for or rewrite legacy folders elsewhere on the machine.

## CI and remaining production gates

The Windows workflow uses read-only repository permissions, pinned official
checkout/upload-artifact actions, Windows PowerShell, geometry/configuration,
source/installer and package-lifecycle checks. It uploads preview artifacts only;
there is no GitHub Release/tag/push/signing job. Native desktop suites run locally
with disposable windows, not claimed from a noninteractive hosted runner. This
workflow is configured locally; a GitHub Actions run is unverified until pushed.
Official references: [checkout](https://github.com/actions/checkout),
[upload-artifact](https://github.com/actions/upload-artifact).

Use [the release checklist](RELEASE-CHECKLIST.md). Local isolated directories
simulate fresh installs but are not a fresh Windows account/VM. Screen-reader,
physical displays/docking/sleep, real startup, privilege/hung-app, competing tiler,
update from actual public releases and external beta feedback remain gates.
