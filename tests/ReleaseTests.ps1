param([string]$WorkDirectory,[string]$PackageDirectory)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing,System.Windows.Forms
$root=Split-Path -Parent $PSScriptRoot
. (Join-Path $root 'InstallationSupport.ps1')
if(!$WorkDirectory){$WorkDirectory=Join-Path $env:TEMP 'WtsReleaseTests'}
$run=Join-Path ([IO.Path]::GetFullPath($WorkDirectory)) ([Guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Path $run -Force | Out-Null
$results=New-Object 'Collections.Generic.List[string]'
function Assert([bool]$Value,[string]$Message){if(!$Value){throw $Message}}
function Reject([scriptblock]$Action,[string]$Message){$failed=$false;try{& $Action | Out-Null}catch{$failed=$true};Assert $failed $Message}
function Install([string]$Zip,[string]$Destination,[string]$Mode='Hosted'){& (Join-Path $root 'Get-FromGitHub.ps1') -ArchivePath $Zip -InstallRoot $Destination -Mode $Mode -ExpectedSha256 (Get-FileHash -LiteralPath $Zip).Hash -NoLaunch}
function Manage([string]$Destination,[string]$Action,[switch]$RemoveData){& (Join-Path $root 'Manage-Installation.ps1') -InstallRoot $Destination -Action $Action -StartupDirectory $startup -RemoveData:$RemoveData -Confirm:$false}
function Candidate([string]$Name,[bool]$Broken,[bool]$UpdateManifest=$true){
    $folder=Join-Path $run $Name;New-Item -ItemType Directory -Path $folder | Out-Null
    Copy-Item -LiteralPath $sourceProject -Destination (Join-Path $folder 'WindowsTileStudio') -Recurse
    $project=Join-Path $folder 'WindowsTileStudio';$relative=if($Broken){'src/LayoutModel.cs'}else{'README.md'};$path=Join-Path $project $relative
    [IO.File]::AppendAllText($path,$(if($Broken){"`nINVALID_CSHARP_RELEASE_FIXTURE"}else{"`nRelease lifecycle fixture.`n"}))
    if($UpdateManifest){$manifestPath=Join-Path $project 'package-manifest.json';$manifest=Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json;$entry=$manifest.files | Where-Object {$_.path -eq $relative};$entry.bytes=(Get-Item -LiteralPath $path).Length;$entry.sha256=(Get-FileHash -LiteralPath $path).Hash.ToLowerInvariant();[IO.File]::WriteAllText($manifestPath,($manifest | ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))}
    $zip=Join-Path $run ($Name+'.zip');Compress-Archive -LiteralPath $project -DestinationPath $zip;return $zip
}
try{
    if($PackageDirectory){$packages=[IO.Path]::GetFullPath($PackageDirectory);$built=Get-Content -Raw -LiteralPath (Join-Path $packages 'release-info.json') | ConvertFrom-Json}else{$packages=Join-Path $run 'packages';$built=& (Join-Path $root 'Build-Release.ps1') -AllowDirty -OutputDirectory $packages}
    foreach($artifact in $built.artifacts){Assert-WtsRelative $artifact.file;$file=Assert-WtsChild $packages (Join-Path $packages $artifact.file);Assert ((Get-FileHash -LiteralPath $file).Hash.ToLowerInvariant() -eq $artifact.sha256) 'Archive checksum does not match release metadata.'}
    $source=Join-Path $packages ($built.artifacts | Where-Object {$_.kind -eq 'Source'}).file
    $portable=Join-Path $packages ($built.artifacts | Where-Object {$_.kind -eq 'PortableCompiled'}).file
    $second=& (Join-Path $root 'Build-Release.ps1') -AllowDirty -Kind Source -OutputDirectory (Join-Path $run 'repeat')
    Assert ((Get-FileHash -LiteralPath $source).Hash -eq (Get-FileHash -LiteralPath (Join-Path $run ('repeat\'+$second.artifacts[0].file))).Hash) 'Identical source inputs did not produce an identical ZIP.'
    $extract=Join-Path $run 'source';Expand-Archive -LiteralPath $source -DestinationPath $extract;$sourceProject=Join-Path $extract 'WindowsTileStudio'
    $portableExtract=Join-Path $run 'portable';Expand-Archive -LiteralPath $portable -DestinationPath $portableExtract;$portableProject=Join-Path $portableExtract 'WindowsTileStudio'
    [void](Test-WtsPackage $sourceProject -RequireManifest);[void](Test-WtsPackage $portableProject -RequireManifest)
    Assert (@(Get-ChildItem -LiteralPath $sourceProject -Filter '*.exe' -Recurse).Count -eq 0) 'Source package contains an executable.'
    $identity=[Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $portableProject 'bin\SixSlotSnap.exe'))
    $versionMetadata=Get-Content -Raw -LiteralPath (Join-Path $sourceProject 'release\version.json') | ConvertFrom-Json
    Assert ($identity.ProductName -eq 'Windows Tile Studio' -and $identity.ProductVersion -eq $built.version -and $identity.FileVersion -eq $versionMetadata.fileVersion) 'Compiled product identity is wrong.'
    $icon=[Drawing.Icon]::ExtractAssociatedIcon((Join-Path $portableProject 'bin\SixSlotSnap.exe'));Assert ($null -ne $icon) 'Compiled app icon is missing.';$icon.Dispose()
    $results.Add('Source ZIP is byte-repeatable for identical inputs; both manifests verify; source excludes EXEs and compiled identity/icon match preview.8.')
    $destination=Join-Path $run "Installed app's folder";$startup=Join-Path $run 'isolated-startup';New-Item -ItemType Directory -Path $startup | Out-Null
    $first=Install $source $destination
    $sentinel=Join-Path $first.DataDirectory 'settings.xml';[IO.File]::WriteAllText($sentinel,'synthetic user settings; never launched')
    [void](Manage $destination 'Validate')
    $repeat=Install $source $destination;Assert ($repeat.VersionDirectory -eq $first.VersionDirectory -and [IO.File]::ReadAllText($sentinel) -eq 'synthetic user settings; never launched') 'Repeat install changed source identity/data.'
    $updated=Install (Candidate 'update' $false) $destination
    Assert ($updated.VersionDirectory -ne $first.VersionDirectory) 'Update did not create a distinct immutable code version.'
    [void](Manage $destination 'Rollback');Assert ((Read-WtsPointer (Join-Path $destination 'current.json')).version -eq (Split-Path -Leaf $first.VersionDirectory)) 'Rollback did not select the prior version.'
    [void](Manage $destination 'Rollback');Assert ((Read-WtsPointer (Join-Path $destination 'current.json')).version -eq (Split-Path -Leaf $updated.VersionDirectory)) 'Rollback toggle did not preserve the former current version.'
    $previousBytes=[IO.File]::ReadAllText((Join-Path $destination 'previous.json'));$activeBytes=[IO.File]::ReadAllText((Join-Path $destination 'current.json'))
    $lock=[IO.File]::Open((Join-Path $destination 'current.json'),[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
    try{Reject {Manage $destination 'Rollback'} 'A locked active pointer was overwritten.'}finally{$lock.Dispose()}
    Assert ([IO.File]::ReadAllText((Join-Path $destination 'previous.json')) -eq $previousBytes -and [IO.File]::ReadAllText((Join-Path $destination 'current.json')) -eq $activeBytes) 'Failed rollback damaged active/previous pointers.'
    $pointer=[IO.File]::ReadAllText((Join-Path $destination 'current.json'))
    Reject {& (Join-Path $root 'Get-FromGitHub.ps1') -ArchivePath $source -InstallRoot $destination -NoLaunch -ExpectedSha256 ('0'*64)} 'Hash mismatch was accepted.'
    Reject {Install (Candidate 'invalid-code' $true) $destination} 'A manifest-valid noncompiling candidate was activated.'
    Reject {Install (Candidate 'tampered-package' $false $false) $destination} 'A manifest hash mismatch was accepted.'
    Assert ([IO.File]::ReadAllText((Join-Path $destination 'current.json')) -eq $pointer -and [IO.File]::ReadAllText($sentinel) -eq 'synthetic user settings; never launched') 'Failed updates changed pointer/data.'
    $cached=Join-Path $updated.VersionDirectory 'README.md';$cachedBytes=[IO.File]::ReadAllBytes($cached)
    try{[IO.File]::AppendAllText($cached,'tampered cache');Reject {Install (Join-Path $run 'update.zip') $destination} 'Modified cached source was silently reused.'}finally{[IO.File]::WriteAllBytes($cached,$cachedBytes)}
    $results.Add('Offline install/repeat/update/rollback preserves data; bad hash, altered package/cache and noncompiling update preserve the active pointer.')
    $compiledRoot=Join-Path $run 'compiled-install';$compiled=Install $portable $compiledRoot 'Compiled';[void](Manage $compiledRoot 'Validate')
    $exePath=Join-Path $compiled.VersionDirectory 'bin\SixSlotSnap.exe';$exeBytes=[IO.File]::ReadAllBytes($exePath)
    try{[IO.File]::AppendAllText($exePath,'altered executable');Reject {Manage $compiledRoot 'Validate'} 'A changed compiled artifact passed integrity validation.'}finally{[IO.File]::WriteAllBytes($exePath,$exeBytes)}
    $sourceCompiledRoot=Join-Path $run 'source-compiled-install';$sourceCompiled=Install $source $sourceCompiledRoot 'Compiled';[void](Manage $sourceCompiledRoot 'Validate');$recompiled=Install $source $sourceCompiledRoot 'Compiled';Assert ($recompiled.VersionDirectory -eq $sourceCompiled.VersionDirectory) 'Source-compiled cache identity changed on repeat.';[void](Manage $sourceCompiledRoot 'Uninstall' -RemoveData)
    # Load app types from a separate extraction; do not lock the installed EXE.
    Add-Type -AssemblyName System.Drawing,System.Windows.Forms
    [Reflection.Assembly]::LoadFrom((Join-Path $portableProject 'bin\SixSlotSnap.exe')) | Out-Null
    $stable=[StartupLaunchSpec]::InstalledLauncher($compiled.VersionDirectory,$compiled.DataDirectory)
    Assert ($stable -eq (Join-Path $compiledRoot 'Launch-Installed.ps1')) 'Installed startup did not find the stable launcher.'
    Assert ($null -eq [StartupLaunchSpec]::InstalledLauncher($compiled.VersionDirectory,$first.DataDirectory)) 'Startup guessed across another data directory.'
    $spec=New-Object StartupLaunchSpec;$spec.Executable=Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe';$spec.InstalledScript=$stable;$spec.DataDirectory=$compiled.DataDirectory;$spec.ValidateFiles()
    Assert ($spec.Command.Contains('Launch-Installed.ps1') -and !$spec.Command.Contains('\versions\')) 'Startup command remained pinned to immutable source.'
    $registration=[StartupRegistration]::new($spec,[UserStartupValue]::new($startup));$registration.Enable($false)
    & (Join-Path $root 'Manage-Installation.ps1') -InstallRoot $compiledRoot -Action Uninstall -StartupDirectory $startup -WhatIf | Out-Null
    Assert ((Test-Path -LiteralPath (Join-Path $compiledRoot 'current.json')) -and (Test-Path -LiteralPath (Join-Path $startup 'Windows Tile Studio.lnk'))) 'WhatIf changed installation/startup.'
    [void](Manage $compiledRoot 'Uninstall');Assert (!(Test-Path -LiteralPath (Join-Path $compiledRoot 'versions')) -and !(Test-Path -LiteralPath (Join-Path $startup 'Windows Tile Studio.lnk')) -and (Test-Path -LiteralPath $compiled.DataDirectory)) 'Owned uninstall failed to remove code/startup or preserve data.'
    [void](Install $portable $compiledRoot 'Compiled');[void](Manage $compiledRoot 'Uninstall' -RemoveData);Assert (!(Test-Path -LiteralPath (Join-Path $compiledRoot 'data'))) 'Explicit synthetic data removal failed.'
    $results.Add('Compiled package installs/verifies; stable startup is tested only in a temp folder; WhatIf is read-only; uninstall preserves data and reinstall works; explicit synthetic purge works.')
    $shell=New-Object -ComObject WScript.Shell;$shortcut=Join-Path $startup 'Windows Tile Studio.lnk'
    try{$link=$shell.CreateShortcut($shortcut);$link.TargetPath=Join-Path $env:WINDIR 'System32\notepad.exe';$link.Arguments='unrelated';$link.Save();[Runtime.InteropServices.Marshal]::FinalReleaseComObject($link)|Out-Null}finally{[Runtime.InteropServices.Marshal]::FinalReleaseComObject($shell)|Out-Null}
    $unrelatedHash=(Get-FileHash -LiteralPath $shortcut).Hash
    $notes=Join-Path $destination 'my-notes.txt';[IO.File]::WriteAllText($notes,'keep notes')
    $outside=Join-Path $run 'outside';New-Item -ItemType Directory -Path $outside | Out-Null;[IO.File]::WriteAllText((Join-Path $outside 'keep.txt'),'outside data')
    $junction=Join-Path $destination 'versions\linked'
    New-Item -ItemType Junction -Path $junction -Target $outside | Out-Null
    try{Reject {Manage $destination 'Uninstall'} 'Uninstall followed an unexpected linked path.';Assert ((Test-Path -LiteralPath (Join-Path $destination 'current.json')) -and [IO.File]::ReadAllText((Join-Path $outside 'keep.txt')) -eq 'outside data') 'Linked-path rejection changed data.'}finally{if((Get-Item -LiteralPath $junction).Attributes -band [IO.FileAttributes]::ReparsePoint){[IO.Directory]::Delete($junction)}}
    [void](Manage $destination 'Uninstall');Assert ([IO.File]::ReadAllText($sentinel) -eq 'synthetic user settings; never launched' -and [IO.File]::ReadAllText($notes) -eq 'keep notes' -and (Get-FileHash -LiteralPath $shortcut).Hash -eq $unrelatedHash) 'Uninstall changed retained data, notes or unrelated startup.'
    $results.Add('Linked paths are refused before deletion; default removal retains settings/unrelated files and unrelated startup shortcuts.')
    $report=@('PASS')+@($results);[IO.File]::WriteAllLines((Join-Path $run 'release-results.txt'),$report);$report;Write-Output ('Result directory: '+$run)
}catch{[IO.File]::WriteAllText((Join-Path $run 'release-error.txt'),$_.ToString());throw}
