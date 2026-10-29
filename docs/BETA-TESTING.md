# Beta validation

The eight local polishing phases have core implementations. `0.3.0-beta.1`
begins release validation; it is unsigned and has not been published. Passing
local tests does not close the manual gates in [RELEASE-CHECKLIST](RELEASE-CHECKLIST.md).
Beta.2 adds named settings page/action headers and current-installation keyboard
acceptance. Consult each package's version/provenance; older kits retain history.

## Safe test setup

The user selected the existing installation for this release; fresh-account/VM
testing is not required. Keep the
complete package, release-info.json and SHA256SUMS.txt together. Verify the archive
hash against a trusted copy. Preserve a separate backup of data/ before updates.
Exit other Tile Studio controllers and competing tilers before baseline checks.
Use disposable application windows. Enabling login startup is a separate choice.

Install the Source package with Hosted mode, then test PortableCompiled in
Compiled mode using [release/recovery instructions](RELEASE-AND-RECOVERY.md).
Record package version/hash, Windows build, scaling and monitor configuration.

## Test matrix

| Area | Local automated coverage | Remaining beta work |
| --- | --- | --- |
| Install/update/remove | Isolated-directory lifecycle, manifest/cache/binary tamper, failed activation, retained-data reinstall | Existing-installation acceptance and Windows policy behavior; fresh account waived |
| Published-version upgrade | Exact public source revision `cfdb2df728466b1256383cf9b8417b21a486a1ca`, synthetic preferences, real controller launch, guarded downgrade | Representative personal configurations/backups and real app sessions |
| Six-window resizing | Pair-only heights, linked widths, repeated disposable-window cycles in both modes | Sustained physical mouse dragging and varied application frames |
| Tray/dialogs/recovery | Hidden bar, dialog churn, future-field Recovery preserving original bytes | Actual screen reader, OS contrast/text-size changes |
| Resource sample | Bounded GDI/private-memory growth and 15-second idle CPU observation | Several days of daily use; not a long-duration leak guarantee |
| Restricted apps | Blocked external UI before snap, responsive settings, resume/re-snap, max-size/custom-aspect rejection in both modes | Freeze after successful probe, elevated/inaccessible apps and broader real apps |
| Displays/startup | Simulated topology and temporary owned shortcut checks | Physical mixed DPI, docking/sleep, actual logout/login |
| Distribution | Local packages/checksums and CI definition | Remote CI execution, signing/publisher and approved publication |

For each resize, confirm height changes affect exactly the two adjacent windows
in that column. Confirm width changes retain every window's previous height.
Close the Layouts bar; it must stay hidden through flyout use and restart.
Exercise General Save/Cancel and startup controls independently. Capture a
workspace without titles, preview choices, and restore explicitly. Check ambiguous
matches require a choice. Never expect automatic app/document recreation.

## Regression commands

For the current compiled checkout installation in Windows PowerShell 5.1 STA:
The helper below is in the updated checkout; older beta.1 archives predate it.

```powershell
.\tests\CurrentInstallationTests.ps1 -InstallationRoot $PWD -ExerciseStartup -ObservationSeconds 180
```

The switch authorizes a normal quit and execution of the existing owned Startup
shortcut; it never creates or rewrites the shortcut. Without the switch it observes
the live app without restarting. A failed shortcut launch gets a direct-restart
fallback. This does not log out or test Windows login policy. Settings/backup
copies and the shortcut snapshot stay in bin/current-installation-* and are private;
never add them to a beta kit. Workspace validation is reported only when a file
exists. Heartbeats and process-counter samples are passive, bounded observations.

Windows PowerShell 5.1, STA, from the complete checkout:

```powershell
.\Build.ps1 -Test
.\tests\LauncherTests.ps1
.\Build-Release.ps1 -OutputDirectory bin\beta-releases
.\tests\ReleaseTests.ps1 -PackageDirectory bin\beta-releases
.\tools\New-BetaKit.ps1 -PackageDirectory bin\beta-releases -OutputDirectory bin\beta-kit
```

With the daily controller stopped, isolated desktop checks:

```powershell
.\Build.ps1 -HostedIntegration -BetaSoak 30 -OutputDirectory bin\beta-validation
.\tests\InstalledLaunchTests.ps1 -PackageDirectory bin\beta-releases
.\tests\PublicUpgradeTests.ps1 -PackageDirectory bin\beta-releases -PreviousArchive 'C:\Downloads\reviewed-public-source.zip' -PreviousRevision 'cfdb2df728466b1256383cf9b8417b21a486a1ca'
```

Review the previous source before running the upgrade test. It verifies the ZIP's
embedded GitHub revision and records its SHA-256. Tests use fresh data directories,
no-recovery startup and owned disposable windows; they do not enable real startup.
The soak accepts 10–200 cycles and checks resource-growth budgets after warmup;
results are saved in each isolated data directory. It simulates resize events and
does not replace a long physical-input or hardware test.

The kit tool requires a new destination, verifies both archive hashes and copies
only packages, provenance/checksums and the testing/recovery documents. It does
not collect user data, send invitations or publish anything.

## Downgrade and reporting

Beta settings reject unknown fields/attributes into Recovery rather than silently
saving a reduced file. Installed selection of older code first probes a snapshot
against that version's settings types. Unsupported settings block install,
rollback and installed launch before old code can discard them. New code declares
its preserve-and-recover capability. This is not a schema migration or sandbox.
Keep the newer backup; restore a known compatible settings file explicitly before
retrying a legacy rollback. Workspace data remains separate. A direct launch of
an old EXE outside the verified launcher has no new protection.

Use [the feedback template](BETA-FEEDBACK-TEMPLATE.md). Preview the app's aggregate
diagnostic report before sharing it. Do not attach raw logs/settings/workspaces
without reviewing paths, titles and other private data. Nothing is uploaded by
the app or these tests. Stop a test on an unhandled Windows/.NET error and record
its exact text and the operation that preceded it; do not dismiss it as a pass.
