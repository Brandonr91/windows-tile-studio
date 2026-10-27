# Launch modes and GitHub installation

This polish milestone adds two entry points: a source downloader for GitHub,
and a Windows PowerShell host that does not generate a standalone app EXE.

The hosted launcher must be present in the revision you download. An older
published basic revision cannot provide it. Use the current polish revision
for hosted mode; older basic revisions can still be installed explicitly
with `-Mode Compiled`.

## Run without a standalone application EXE

From the full source folder, double-click **Start-Hosted.cmd**. From the
dedicated hosted package, double-click **Start.cmd**. Both select Windows
PowerShell 5.1, create an STA background host, load the C# source into memory,
and run the same controller used by the compiled application.

Windows' existing `powershell.exe` is the host process. No app-specific
`SixSlotSnap.exe` is generated or required in this mode. This is not a claim
that no executable process or temporary runtime/compiler files exist anywhere
on Windows. The normal controller still runs locally without an AI service.
Source compilation happens at startup, so hosted mode can take longer to
start than an already-built EXE. PowerShell remains running while the
controller is active.

The flyout, layout editor, hotkeys, and linked resizing remain the same.
Use **Exit** in the tray menu to stop the host. The existing **Start.cmd** in
the full source checkout retains the optional compiled mode.

Both modes accept a data directory and now use `data/` beside the source by
default. They preserve an existing data file and can import the original
`bin/settings.xml` or `bin/layout.txt` on first use. Only one controller runs
in your Windows session; exit the old copy before switching modes.

For foreground diagnostics in Windows PowerShell 5.1:

```powershell
.\Start-Hosted.ps1 -RunInProcess -DataDirectory .\data
```

Start a fresh PowerShell process after changing C# source; types already loaded
into a running PowerShell session cannot be replaced in place. The default
background launcher starts a fresh process automatically.

## Download and run from GitHub

`Get-FromGitHub.ps1` downloads source from the Windows Tile Studio repository,
extracts a versioned copy, writes local launchers, and starts the selected mode.
It needs no Git installation, GitHub token, installer service, or downloaded EXE.

After a revision containing these launchers has been published, download and
inspect its `Get-FromGitHub.ps1`, then run:

```powershell
.\Get-FromGitHub.ps1 -Mode Hosted
```

The default installation folder is `%LOCALAPPDATA%\WindowsTileStudio`. To use
another dedicated folder or prepare the installation without starting it:

```powershell
.\Get-FromGitHub.ps1 -Mode Hosted -InstallRoot 'D:\Tools\WindowsTileStudio' -NoLaunch
```

For a published tag, branch, or exact commit, add `-Ref` with that revision.
The installer downloads one source archive, reads its commit from the Git ZIP
comment where available, and records the archive's SHA-256. An exact commit
argument must match the archive comment if one is present. If no commit can
be identified, the archive hash identifies the installed version. These records
are provenance information, not a code-signing certificate.

The installed folder contains:

```text
WindowsTileStudio/
  Start.cmd                 restart the selected mode
  Launch-Installed.ps1       read the active version and launch it
  current.json              selected code version and mode
  versions/<revision>/      downloaded source
  data/                     persistent settings and runtime logs
```

Run the downloader again to install a newer revision. The data folder is kept
outside version folders and is not replaced. Exit the running controller and
use the installed `Start.cmd` when you are ready to run the new version.
The installer does not register automatic startup or silently stop a running
controller. Keep a copy of `data/` if you want to preserve preferences when
removing the entire installation folder.

`-Mode Hosted` is the default and fails clearly if the downloaded revision
does not contain the hosted script. `-Mode Compiled` explicitly builds the
optional application EXE. `-Mode Auto` prefers Hosted when available and can
select Compiled for the older basic revision. The returned installation result
states which mode was selected.

For an already-downloaded source ZIP, `-ArchivePath` installs it without a
network request. The ZIP must contain the project under one top-level folder.

## Verification

Run the launcher tests from Windows PowerShell 5.1:

```powershell
.\tests\LauncherTests.ps1
.\tests\LauncherTests.ps1 -Network
```

Offline tests check in-memory compilation, serialization, paths with spaces
and apostrophes, repeated installation, preservation of data, unsafe archive
rejection, missing hosted files, and unrelated destination folders. The
optional network test downloads the currently published source without
launching it. These tests do not modify normal user settings.

With the current controller stopped, the hosted desktop integration test is:

```powershell
.\Build.ps1 -HostedIntegration
```

To prove the controller can run from a source folder containing no EXE, use
`-HostedSourceRoot` with a clean source-only folder. The test runner itself
is a separate compiled QA tool; the application under test runs inside
PowerShell. The suite uses disposable windows and isolated data.

Official references: Microsoft documents the default in-memory behavior of
[Add-Type](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/add-type?view=powershell-5.1);
Git documents the ZIP commit comment in
[git archive](https://git-scm.com/docs/git-archive).
