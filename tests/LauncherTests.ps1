[CmdletBinding()]
param(
    [string]$WorkDirectory,
    [switch]$Network
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($WorkDirectory)) { $WorkDirectory = Join-Path $projectRoot 'bin\launcher-tests' }
$windowsPowerShell = Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe'
$WorkDirectory = [IO.Path]::GetFullPath($WorkDirectory)
New-Item -ItemType Directory -Force -Path $WorkDirectory | Out-Null
$run = Join-Path $WorkDirectory ([Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $run | Out-Null
$results = [Collections.Generic.List[string]]::new()
function Assert([bool]$Condition,[string]$Message) { if (-not $Condition) { throw $Message } }
function Must-Fail([scriptblock]$Action,[string]$Message) {
    $failed=$false
    try { & $Action | Out-Null } catch { $failed=$true }
    Assert $failed $Message
}
function No-Exe([string]$Root) { return @(Get-ChildItem -LiteralPath $Root -Recurse -Filter '*.exe' -File).Count -eq 0 }
try {
    $fixture = Join-Path $run 'source'
    New-Item -ItemType Directory -Path $fixture | Out-Null
    Copy-Item -LiteralPath (Join-Path $projectRoot 'src') -Destination $fixture -Recurse
    foreach($file in @('README.md','Build.ps1','Start.ps1','Start-Hosted.ps1','Start-Hosted.cmd')) { Copy-Item -LiteralPath (Join-Path $projectRoot $file) -Destination $fixture }
    $checkData = Join-Path $run 'host-validation'
    $json = & $windowsPowerShell -NoProfile -STA -ExecutionPolicy Bypass -File (Join-Path $fixture 'Start-Hosted.ps1') -RunInProcess -Action Validate -DataDirectory $checkData
    Assert ($LASTEXITCODE -eq 0) 'Hosted compilation or settings validation failed.'
    $check = $json | ConvertFrom-Json
    Assert ($check.Mode -eq 'PowerShellHosted' -and $check.AssemblyLocation -eq '' -and $check.Profiles -eq 6) 'Hosted mode was not compiled in memory.'
    Assert (No-Exe $fixture) 'Hosted validation created an executable.'
    $results.Add('Hosted source loads in Windows PowerShell 5.1, serializes settings, and creates no standalone EXE.')

    $archivePath = Join-Path $run 'candidate.zip'
    Compress-Archive -LiteralPath $fixture -DestinationPath $archivePath
    $install = Join-Path $run "Installed app's folder with spaces"
    $installer = Join-Path $projectRoot 'Get-FromGitHub.ps1'
    $installed = & $installer -ArchivePath $archivePath -InstallRoot $install -Mode Hosted -NoLaunch
    Assert ($installed.Mode -eq 'Hosted' -and $installed.SourceKind -eq 'LocalArchive' -and -not $installed.Launched) 'Offline hosted installation metadata is incorrect.'
    Assert (Test-Path -LiteralPath $installed.Launcher) 'The installed launcher is missing.'
    Assert (No-Exe $install) 'Installing in hosted mode created an EXE.'
    $results.Add('Versioned hosted installation works with spaces and apostrophes in the destination path.')

    $sentinel = Join-Path $installed.DataDirectory 'settings-sentinel.txt'
    [IO.File]::WriteAllText($sentinel,'Keep this user configuration.')
    $installedAgain = & $installer -ArchivePath $archivePath -InstallRoot $install -Mode Hosted -NoLaunch
    Assert ([IO.File]::ReadAllText($sentinel) -eq 'Keep this user configuration.') 'An update replaced user data.'
    Assert ($installed.VersionDirectory -eq $installedAgain.VersionDirectory) 'Identical source archives did not reuse their version folder.'
    $results.Add('Repeated installation preserves user data and reuses the same version.')

    # Exercise the hosted launcher from its installed, versioned location.
    $installedJson = & $windowsPowerShell -NoProfile -STA -ExecutionPolicy Bypass -File (Join-Path $installed.VersionDirectory 'Start-Hosted.ps1') -RunInProcess -Action Validate -DataDirectory (Join-Path $run 'installed-host-check')
    Assert ($LASTEXITCODE -eq 0) 'The installed hosted source did not validate.'
    $installedCheck = $installedJson | ConvertFrom-Json
    Assert ($installedCheck.AssemblyLocation -eq '' -and (No-Exe $install)) 'The installed host emitted an application EXE.'
    $results.Add('Installed hosted source also compiles and runs its settings check without an application EXE.')

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $malicious = Join-Path $run 'bad-path.zip'
    $badZip = [IO.Compression.ZipFile]::Open($malicious,[IO.Compression.ZipArchiveMode]::Create)
    try {
        $one = $badZip.CreateEntry('source/README.md'); $writer=[IO.StreamWriter]::new($one.Open()); try { $writer.Write('fixture') } finally { $writer.Dispose() }
        $two = $badZip.CreateEntry('source/../../escaped.txt'); $writer=[IO.StreamWriter]::new($two.Open()); try { $writer.Write('must not be extracted') } finally { $writer.Dispose() }
    } finally { $badZip.Dispose() }
    $pointerBefore = [IO.File]::ReadAllText((Join-Path $install 'current.json'))
    Must-Fail { & $installer -ArchivePath $malicious -InstallRoot $install -NoLaunch } 'A path-traversal archive was accepted.'
    Assert ([IO.File]::ReadAllText((Join-Path $install 'current.json')) -eq $pointerBefore) 'A failed install changed the active version.'
    Assert (-not (Test-Path -LiteralPath (Join-Path $install 'escaped.txt'))) 'The malicious archive was extracted.'
    $results.Add('Unsafe archive paths are rejected before extraction; the active version remains unchanged.')
    foreach($unsafeName in @('source/.. /escape.txt','source/CON.txt')) {
        $unsafeZip=Join-Path $run ([Guid]::NewGuid().ToString('N')+'.zip')
        $package=[IO.Compression.ZipFile]::Open($unsafeZip,[IO.Compression.ZipArchiveMode]::Create)
        try {
            foreach($entryName in @('source/README.md',$unsafeName)) {
                $entry=$package.CreateEntry($entryName); $writer=[IO.StreamWriter]::new($entry.Open()); try { $writer.Write('fixture') } finally { $writer.Dispose() }
            }
        } finally { $package.Dispose() }
        Must-Fail { & $installer -ArchivePath $unsafeZip -InstallRoot $install -NoLaunch } 'A Windows path alias or device filename was accepted.'
    }
    Assert ([IO.File]::ReadAllText((Join-Path $install 'current.json')) -eq $pointerBefore) 'An unsafe Windows filename changed the active version.'
    $results.Add('Windows trailing-space path aliases and reserved device filenames are also rejected.')

    $legacyArchive = Join-Path $run 'basic-only.zip'
    Copy-Item -LiteralPath $archivePath -Destination $legacyArchive
    $legacy = [IO.Compression.ZipFile]::Open($legacyArchive,[IO.Compression.ZipArchiveMode]::Update)
    try { @($legacy.Entries | Where-Object { $_.FullName.Replace('\','/') -eq 'source/Start-Hosted.ps1' }) | ForEach-Object { $_.Delete() } } finally { $legacy.Dispose() }
    Must-Fail { & $installer -ArchivePath $legacyArchive -InstallRoot $install -Mode Hosted -NoLaunch } 'Hosted mode silently accepted a revision without the hosted launcher.'
    Assert ([IO.File]::ReadAllText((Join-Path $install 'current.json')) -eq $pointerBefore) 'Missing hosted files changed the active version.'
    $basic = & $installer -ArchivePath $legacyArchive -InstallRoot (Join-Path $run 'basic-compatible') -Mode Auto -NoLaunch
    Assert ($basic.Mode -eq 'Compiled') 'Auto mode did not explicitly select the basic compiled launcher.'
    $results.Add('Missing hosted files fail clearly; explicit Auto mode can select the older compiled version.')

    $occupied = Join-Path $run 'unrelated-folder'
    New-Item -ItemType Directory -Path $occupied | Out-Null
    [IO.File]::WriteAllText((Join-Path $occupied 'unrelated.txt'),'keep')
    Must-Fail { & $installer -ArchivePath $archivePath -InstallRoot $occupied -NoLaunch } 'An unrelated nonempty destination was overwritten.'
    Assert ([IO.File]::ReadAllText((Join-Path $occupied 'unrelated.txt')) -eq 'keep') 'An unrelated file changed.'
    $results.Add('A nonempty unrelated directory is not used as an installation target.')

    if ($Network) {
        # The currently published basic revision has no hosted script yet.
        # This test verifies the real GitHub resolve/download path without
        # launching or building the downloaded application.
        $remote = & $installer -InstallRoot (Join-Path $run 'github-download') -Mode Compiled -NoLaunch
        Assert ($remote.SourceKind -eq 'GitHub' -and $remote.Commit -match '^[0-9a-f]{40}$') 'GitHub revision pinning failed.'
        Assert (Test-Path -LiteralPath (Join-Path $remote.VersionDirectory 'src\SixSlotSnap.cs')) 'GitHub source was not installed.'
        Assert (No-Exe $remote.InstallRoot) 'A no-launch GitHub download unexpectedly compiled an EXE.'
        $results.Add('Actual GitHub source download records the archive commit without a REST API lookup or launch.')
    }
    $report = @('PASS') + @($results)
    [IO.File]::WriteAllLines((Join-Path $run 'launcher-results.txt'),$report)
    $report | ForEach-Object { Write-Output $_ }
    Write-Output ('Result directory: '+$run)
} catch {
    [IO.File]::WriteAllText((Join-Path $run 'launcher-error.txt'),$_.ToString())
    throw
}
