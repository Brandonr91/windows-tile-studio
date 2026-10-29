[CmdletBinding()]
param(
    [string]$InstallRoot = (Join-Path $env:LOCALAPPDATA 'WindowsTileStudio'),
    [ValidateSet('Hosted','Compiled','Auto')][string]$Mode = 'Hosted',
    [string]$Ref = 'main',
    [switch]$NoLaunch,
    [string]$ArchivePath,
    [ValidatePattern('^[0-9a-fA-F]{64}$')][string]$ExpectedSha256
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'InstallationSupport.ps1')
$repository = 'Brandonr91/windows-tile-studio'
$utf8 = [Text.UTF8Encoding]::new($false)

function Assert-ChildPath([string]$Root,[string]$Path) {
    $rootPath = [IO.Path]::GetFullPath($Root).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
    $candidate = [IO.Path]::GetFullPath($Path)
    if (-not $candidate.StartsWith($rootPath,[StringComparison]::OrdinalIgnoreCase)) { throw 'A package path escapes the installation folder.' }
    return $candidate
}
function Write-AtomicText([string]$Path,[string]$Text) {
    $temporary = $Path + '.' + [Guid]::NewGuid().ToString('N') + '.tmp'
    try {
        [IO.File]::WriteAllText($temporary,$Text,$utf8)
        if ([IO.File]::Exists($Path)) { [IO.File]::Replace($temporary,$Path,$Path+'.bak') }
        else { [IO.File]::Move($temporary,$Path) }
    } finally { if ([IO.File]::Exists($temporary)) { [IO.File]::Delete($temporary) } }
}
function Get-ArchiveCommit([string]$Path) {
    # git archive records the commit in the ZIP end-of-central-directory
    # comment. This avoids an extra, rate-limited GitHub REST lookup.
    $bytes = [IO.File]::ReadAllBytes($Path)
    for ($i=$bytes.Length-22; $i -ge [Math]::Max(0,$bytes.Length-65557); $i--) {
        if ($bytes[$i] -eq 0x50 -and $bytes[$i+1] -eq 0x4b -and $bytes[$i+2] -eq 5 -and $bytes[$i+3] -eq 6) {
            $length = [BitConverter]::ToUInt16($bytes,$i+20)
            if ($i+22+$length -eq $bytes.Length) {
                $comment = [Text.Encoding]::ASCII.GetString($bytes,$i+22,$length)
                if ($comment -match '^[0-9a-fA-F]{40}$') { return $comment.ToLowerInvariant() }
                return $null
            }
        }
    }
    return $null
}

$InstallRoot = [IO.Path]::GetFullPath($InstallRoot).TrimEnd('\','/')
Assert-WtsNoLinks $InstallRoot
if ($InstallRoot -eq [IO.Path]::GetPathRoot($InstallRoot).TrimEnd('\','/')) { throw 'Choose a dedicated application folder, not a drive or share root.' }
$marker = Join-Path $InstallRoot '.windows-tile-studio.json'
if (Test-Path -LiteralPath $InstallRoot) {
    if (-not (Test-Path -LiteralPath $InstallRoot -PathType Container)) { throw 'The installation path is not a directory.' }
    if (Test-Path -LiteralPath $marker) {
        $identity = Get-Content -Raw -LiteralPath $marker | ConvertFrom-Json
        if ($identity.repository -ne $repository -or $identity.schema -ne 1) { throw 'This folder belongs to another installation.' }
    } elseif (@(Get-ChildItem -Force -LiteralPath $InstallRoot).Count -gt 0) {
        throw 'Choose an empty folder or an existing Windows Tile Studio installation.'
    }
}
New-Item -ItemType Directory -Force -Path $InstallRoot | Out-Null
if (-not (Test-Path -LiteralPath $marker)) {
    Write-AtomicText $marker (@{schema=1;repository=$repository} | ConvertTo-Json -Compress)
}
$staging = Assert-ChildPath $InstallRoot (Join-Path $InstallRoot ('.staging-'+[Guid]::NewGuid().ToString('N')))
New-Item -ItemType Directory -Path $staging | Out-Null
$commit = $null
$sourceKind = 'GitHub'
try {
    if ($ArchivePath) {
        $zip = [IO.Path]::GetFullPath($ArchivePath)
        if (-not (Test-Path -LiteralPath $zip -PathType Leaf)) { throw 'The supplied source archive does not exist.' }
        $sourceKind = 'LocalArchive'
    } else {
        # Fetch once. The archive's own commit comment identifies the exact
        # revision that was served, even when the requested branch changes.
        [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
        $headers = @{'User-Agent'='Windows-Tile-Studio-Installer'}
        $encodedRef = [Uri]::EscapeDataString($Ref)
        $zip = Join-Path $staging 'source.zip'
        Invoke-WebRequest -UseBasicParsing -Uri ('https://codeload.github.com/'+$repository+'/zip/'+$encodedRef) -Headers $headers -OutFile $zip -TimeoutSec 60
    }
    if ((Get-Item -LiteralPath $zip).Length -gt 25MB) { throw 'The source archive is unexpectedly large.' }
    $archiveHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $zip).Hash.ToLowerInvariant()
    if($ExpectedSha256 -and $archiveHash -ne $ExpectedSha256.ToLowerInvariant()){throw 'Archive SHA-256 does not match the expected value. No version was activated.'}
    if ($sourceKind -eq 'GitHub') {
        $commit = Get-ArchiveCommit $zip
        if ($Ref -match '^[0-9a-fA-F]{40}$') {
            if ($commit -and $commit -ne $Ref.ToLowerInvariant()) { throw 'The downloaded archive does not match the requested commit.' }
            $commit = $Ref.ToLowerInvariant()
        }
    }
    $version = if ($commit) { $commit } elseif ($sourceKind -eq 'LocalArchive') { 'local-'+$archiveHash.Substring(0,24) } else { 'archive-'+$archiveHash.Substring(0,24) }
    $extracted = Assert-ChildPath $staging (Join-Path $staging 'extracted')
    New-Item -ItemType Directory -Path $extracted | Out-Null
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($zip)
    try {
        if ($archive.Entries.Count -lt 2 -or $archive.Entries.Count -gt 1000) { throw 'Unexpected number of archive entries.' }
        $topFolder = $null
        [long]$totalBytes = 0
        foreach ($entry in $archive.Entries) {
            $name = $entry.FullName.Replace('\','/')
            $parts = @($name.Split('/') | Where-Object { $_ -ne '' })
            if ($name.StartsWith('/') -or $name.Contains(':') -or $parts.Count -eq 0 -or @($parts | Where-Object { $_ -eq '..' -or $_ -eq '.' -or $_.EndsWith('.') -or $_.EndsWith(' ') -or $_.Split('.')[0] -match '^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])$' }).Count -gt 0) { throw 'Unsafe path in source archive.' }
            if (-not $topFolder) { $topFolder = $parts[0] }
            if ($parts[0] -ne $topFolder) { throw 'Source files must be under one project folder.' }
            if ((($entry.ExternalAttributes -shr 16) -band 0xF000) -eq 0xA000) { throw 'Symbolic links are not accepted in source packages.' }
            $totalBytes += $entry.Length
            if ($entry.Length -gt 5MB -or $totalBytes -gt 50MB) { throw 'The expanded source archive is unexpectedly large.' }
            [void](Assert-ChildPath $extracted (Join-Path $extracted $name))
        }
    } finally { $archive.Dispose() }
    [IO.Compression.ZipFile]::ExtractToDirectory($zip,$extracted)
    $project = Assert-ChildPath $extracted (Join-Path $extracted $topFolder)
    $packageManifest=Test-WtsPackage $project
    foreach ($required in @('README.md','Build.ps1','Start.ps1','src\SixSlotSnap.cs')) {
        if (-not (Test-Path -LiteralPath (Join-Path $project $required) -PathType Leaf)) { throw "Source package is missing $required." }
    }
    $hasHosted = Test-Path -LiteralPath (Join-Path $project 'Start-Hosted.ps1') -PathType Leaf
    $selectedMode = $Mode
    if ($Mode -eq 'Auto') { $selectedMode = if ($hasHosted) { 'Hosted' } else { 'Compiled' } }
    if ($selectedMode -eq 'Hosted' -and -not $hasHosted) {
        throw 'This published revision does not contain hosted mode yet. Choose a revision that includes Start-Hosted.ps1, or explicitly use -Mode Compiled for the basic EXE-based version.'
    }
    $versions = Join-Path $InstallRoot 'versions'
    New-Item -ItemType Directory -Force -Path $versions | Out-Null
    $versionPath = Assert-ChildPath $InstallRoot (Join-Path $versions $version)
    Assert-WtsNoLinks $versionPath
    $windowsHost=Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe'
    # Validate before changing current.json; use staging data, never user data.
    if($selectedMode -eq 'Hosted'){
        Invoke-WtsValidation $windowsHost ('-NoProfile -STA -ExecutionPolicy Bypass -File "'+(Join-Path $project 'Start-Hosted.ps1')+'" -RunInProcess -Action Validate -DataDirectory "'+(Join-Path $staging 'validation')+'"') $staging
    }else{
        if($packageManifest -and $packageManifest.kind -eq 'PortableCompiled'){
            Invoke-WtsValidation $windowsHost ('-NoProfile -ExecutionPolicy Bypass -File "'+(Join-Path $project 'Build.ps1')+'" -OutputDirectory "'+(Join-Path $staging 'compiled-validation')+'"') $staging
            $identity=[Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $project 'bin\SixSlotSnap.exe'))
            if($identity.ProductName -ne 'Windows Tile Studio' -or $identity.ProductVersion -ne $packageManifest.version){throw 'Compiled product identity does not match the package.'}
        }else{Invoke-WtsValidation $windowsHost ('-NoProfile -ExecutionPolicy Bypass -File "'+(Join-Path $project 'Build.ps1')+'"') $staging}
    }
    if(!$packageManifest){
        $legacyFiles=@(foreach($file in Get-WtsTreeFiles $project){$relative=$file.FullName.Substring($project.Length+1).Replace('\','/');if(!$relative.StartsWith('bin/')){[ordered]@{path=$relative;bytes=$file.Length;sha256=(Get-FileHash -LiteralPath $file.FullName).Hash.ToLowerInvariant()}}})
        $legacyManifest=[ordered]@{schema=1;repository=$repository;product='Windows Tile Studio';version='legacy-source';kind='Source';signing='unsigned';origin='InstallerGenerated';archiveSha256=$archiveHash;files=$legacyFiles}
        [IO.File]::WriteAllText((Join-Path $project 'package-manifest.json'),($legacyManifest | ConvertTo-Json -Depth 12),$utf8)
    }
    if(Test-Path -LiteralPath $versionPath){
        [void](Test-WtsPackage $versionPath -AllowBuildOutput)
        $expected=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
        foreach($file in Get-ChildItem -LiteralPath $project -Recurse -Force -File){$relative=$file.FullName.Substring($project.Length+1);if($relative.StartsWith('bin\')){continue};$cached=Assert-WtsChild $versionPath (Join-Path $versionPath $relative);if(!(Test-Path -LiteralPath $cached -PathType Leaf) -or (Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath $cached).Hash){throw 'Cached source was modified; no files or pointer were overwritten.'}}
        foreach($file in Get-ChildItem -LiteralPath $project -Recurse -Force -File){[void]$expected.Add($file.FullName.Substring($project.Length+1))}
        foreach($file in Get-WtsTreeFiles $versionPath){$relative=$file.FullName.Substring($versionPath.Length+1);if(!$relative.StartsWith('bin\') -and !$expected.Contains($relative)){throw 'Cached source has unlisted files; it was preserved without activation.'}}
    }
    if (-not (Test-Path -LiteralPath $versionPath)) {
        # Both source and destination are verified before this directory move.
        [void](Assert-ChildPath $staging $project)
        [void](Assert-ChildPath $InstallRoot $versionPath)
        Move-Item -LiteralPath $project -Destination $versionPath
    }
    $compiledHash=$null
    if($selectedMode -eq 'Compiled'){
        $installedExe=Assert-WtsChild $versionPath (Join-Path $versionPath 'bin\SixSlotSnap.exe')
        if(!(Test-Path -LiteralPath $installedExe)){
            [void](Assert-WtsChild $versionPath (Join-Path $versionPath 'bin'));Copy-Item -LiteralPath (Join-Path $project 'bin') -Destination $versionPath -Recurse
        }elseif(!$packageManifest -or $packageManifest.kind -ne 'PortableCompiled'){
            # Fresh versions contain the just-validated build; cached builds need
            # a previously recorded artifact hash before they can be reused.
            if(Test-Path -LiteralPath $project){$known=@(foreach($name in @('current.json','previous.json')){$p=Join-Path $InstallRoot $name;if(Test-Path -LiteralPath $p){$v=Read-WtsPointer $p;if($v.version -eq $version -and $v.compiledSha256){$v.compiledSha256}}});if((Get-FileHash -LiteralPath $installedExe).Hash.ToLowerInvariant() -notin $known){throw 'Cached compiled artifact cannot be verified; it was preserved without activation.'}}
        }
        $compiledHash=(Get-FileHash -LiteralPath $installedExe).Hash.ToLowerInvariant()
    }
    $dataPath = Assert-ChildPath $InstallRoot (Join-Path $InstallRoot 'data')
    New-Item -ItemType Directory -Force -Path $dataPath | Out-Null

    # This launcher reads the active version at run time; user data is never
    # stored inside a version folder and is not overwritten on update.
    $launchScript = @'
[CmdletBinding()]
param([ValidateSet('Hosted','Compiled','Auto')][string]$Mode,[string]$StartupOwner,[switch]$NoRecover)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'InstallationSupport.ps1')
$current=Read-WtsPointer (Join-Path $PSScriptRoot 'current.json')
if ($current.version -notmatch '^(?:[0-9a-f]{40}|(?:local|archive)-[0-9a-f]{24})$') { throw 'Invalid installed version pointer.' }
$project=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot ('versions\'+$current.version)))
$data=Join-Path $PSScriptRoot 'data'
if (-not $Mode -or $Mode -eq 'Auto') { $Mode=$current.mode }
if($Mode -eq $current.mode){Test-WtsInstalledPointer $PSScriptRoot $current}else{throw 'Install the desired mode explicitly before using it; the active artifact must be verified.'}
if ($Mode -eq 'Hosted') {
    & (Join-Path $project 'Start-Hosted.ps1') -DataDirectory $data -NoRecover:$NoRecover
} else {
    $application=Join-Path $project 'bin\SixSlotSnap.exe'
    if (-not (Test-Path -LiteralPath $application)) { & (Join-Path $project 'Build.ps1') }
    $arguments=@('--data-dir',('"'+$data.TrimEnd('\')+'"'));if($NoRecover){$arguments+='--no-recover'}
    Start-Process -FilePath $application -ArgumentList $arguments -WindowStyle Hidden
}
'@
    Write-AtomicText (Join-Path $InstallRoot 'Launch-Installed.ps1') $launchScript
    Write-AtomicText (Join-Path $InstallRoot 'Start.cmd') "@echo off`r`n`"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe`" -NoProfile -STA -ExecutionPolicy Bypass -File `"%~dp0Launch-Installed.ps1`"`r`nif errorlevel 1 pause`r`n"
    foreach($helper in @('Manage-Installation.ps1','InstallationSupport.ps1')){Write-AtomicText (Join-Path $InstallRoot $helper) ([IO.File]::ReadAllText((Join-Path $PSScriptRoot $helper)))}
    foreach($operation in @('Rollback','Uninstall')){Write-AtomicText (Join-Path $InstallRoot ($operation+'.cmd')) ("@echo off`r`n`"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe`" -NoProfile -ExecutionPolicy Bypass -File `"%~dp0Manage-Installation.ps1`" -InstallRoot `"%~dp0.`" -Action "+$operation+" -Confirm`r`npause`r`n")}
    $current = [ordered]@{schema=1;repository=$repository;version=$version;commit=$commit;archiveSha256=$archiveHash;sourceKind=$sourceKind;mode=$selectedMode;compiledSha256=$compiledHash;installedAtUtc=[DateTime]::UtcNow.ToString('o')}
    $pointer=Join-Path $InstallRoot 'current.json'
    $prior=if(Test-Path -LiteralPath $pointer){Read-WtsPointer $pointer}else{$null}
    Set-WtsCurrent $InstallRoot $current
    $result = [PSCustomObject]@{Repository=$repository;Commit=$commit;SourceKind=$sourceKind;Mode=$selectedMode;InstallRoot=$InstallRoot;VersionDirectory=$versionPath;DataDirectory=$dataPath;Launcher=(Join-Path $InstallRoot 'Start.cmd');Launched=(-not $NoLaunch)}
    if (-not $NoLaunch) { & (Join-Path $InstallRoot 'Launch-Installed.ps1') | Out-Null }
    $result
} finally {
    if (Test-Path -LiteralPath $staging) {
        # Only this invocation's fresh, bounded staging folder is removed.
        [void](Assert-ChildPath $InstallRoot $staging)
        if ((Split-Path -Leaf $staging) -notmatch '^\.staging-[0-9a-f]{32}$') { throw 'Unexpected staging path; cleanup refused.' }
        Remove-Item -LiteralPath $staging -Recurse -Force
    }
}
