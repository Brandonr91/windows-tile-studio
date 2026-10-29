# Shared local packaging/installation helpers. No action occurs on dot-source.
function Assert-WtsChild([string]$Root,[string]$Path) {
    $rootPath=[IO.Path]::GetFullPath($Root).TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar
    $full=[IO.Path]::GetFullPath($Path)
    if(!$full.StartsWith($rootPath,[StringComparison]::OrdinalIgnoreCase)){throw 'Path escapes its owned folder.'}
    Assert-WtsNoLinks $full
    return $full
}
function Assert-WtsNoLinks([string]$Path) {
    $current=[IO.Path]::GetFullPath($Path)
    while($current){if(Test-Path -LiteralPath $current){$item=Get-Item -Force -LiteralPath $current;if(($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw 'Linked/reparse installation paths are not supported.'}};$parent=Split-Path -Parent $current;if($parent -eq $current){break};$current=$parent}
}
function Assert-WtsRelative([string]$Path) {
    if(!$Path -or $Path.Contains('\') -or $Path.StartsWith('/') -or $Path.Contains(':') -or $Path.IndexOfAny([IO.Path]::GetInvalidPathChars()) -ge 0){throw 'Invalid package path.'}
    foreach($part in $Path.Split('/')){if(!$part -or $part -eq '.' -or $part -eq '..' -or $part.EndsWith('.') -or $part.EndsWith(' ') -or $part.IndexOfAny([IO.Path]::GetInvalidFileNameChars()) -ge 0 -or $part.Split('.')[0] -match '^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])$'){throw 'Invalid Windows package path.'}}
}
function Get-WtsTreeFiles([string]$Root) {
    Assert-WtsNoLinks $Root;$queue=New-Object 'Collections.Generic.Queue[string]';$queue.Enqueue([IO.Path]::GetFullPath($Root))
    while($queue.Count){foreach($entry in Get-ChildItem -Force -LiteralPath $queue.Dequeue()){Assert-WtsNoLinks $entry.FullName;if($entry.PSIsContainer){$queue.Enqueue($entry.FullName)}else{$entry}}}
}
function Write-WtsAtomic([string]$Path,[string]$Text) {
    $temp=$Path+'.'+[Guid]::NewGuid().ToString('N')+'.tmp'
    try{[IO.File]::WriteAllText($temp,$Text,[Text.UTF8Encoding]::new($false));if([IO.File]::Exists($Path)){[IO.File]::Replace($temp,$Path,$Path+'.bak')}else{[IO.File]::Move($temp,$Path)}}finally{if([IO.File]::Exists($temp)){[IO.File]::Delete($temp)}}
}
function Read-WtsPointer([string]$Path) {
    $value=Get-Content -Raw -LiteralPath $Path | ConvertFrom-Json
    if($value.schema -ne 1 -or $value.repository -ne 'Brandonr91/windows-tile-studio' -or $value.version -notmatch '^(?:[0-9a-f]{40}|(?:local|archive)-[0-9a-f]{24})$' -or $value.mode -notin @('Hosted','Compiled')){throw 'Invalid installed version pointer.'}
    return $value
}
function Test-WtsPackage([string]$Root,[switch]$RequireManifest,[switch]$AllowBuildOutput) {
    $tree=@(Get-WtsTreeFiles $Root)
    $path=Join-Path $Root 'package-manifest.json'
    if(!(Test-Path -LiteralPath $path)){if($RequireManifest){throw 'The package manifest is missing.'};return $null}
    if((Get-Item -LiteralPath $path).Length -gt 1MB){throw 'Package manifest is too large.'}
    $manifest=Get-Content -Raw -LiteralPath $path | ConvertFrom-Json
    if($manifest.schema -ne 1 -or $manifest.repository -ne 'Brandonr91/windows-tile-studio' -or $manifest.kind -notin @('Source','PortableCompiled') -or @($manifest.files).Count -lt 1 -or @($manifest.files).Count -gt 1000){throw 'Invalid package manifest.'}
    $names=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach($entry in $manifest.files){Assert-WtsRelative $entry.path;if(!$names.Add($entry.path) -or $entry.path -eq 'package-manifest.json' -or $entry.sha256 -notmatch '^[0-9a-f]{64}$'){throw 'Invalid or duplicate manifest entry.'};$file=Assert-WtsChild $Root (Join-Path $Root $entry.path);if(!(Test-Path -LiteralPath $file -PathType Leaf) -or (Get-Item -LiteralPath $file).Length -ne $entry.bytes -or (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant() -ne $entry.sha256){throw ('Package integrity mismatch: '+$entry.path)}}
    foreach($file in $tree){$relative=$file.FullName.Substring([IO.Path]::GetFullPath($Root).TrimEnd('\').Length+1).Replace('\','/');if($relative -eq 'package-manifest.json' -or ($AllowBuildOutput -and $relative.StartsWith('bin/'))){continue};if(!$names.Contains($relative)){throw ('Unlisted package file: '+$relative)}}
    return $manifest
}
function Assert-WtsNoRunningInstall([string]$Root) {
    $prefix=[IO.Path]::GetFullPath($Root).TrimEnd('\')+'\versions\'
    $found=@(Get-CimInstance Win32_Process | Where-Object {($_.Name -eq 'SixSlotSnap.exe' -and $_.ExecutablePath -and $_.ExecutablePath.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)) -or ($_.Name -eq 'powershell.exe' -and $_.CommandLine -and $_.CommandLine.IndexOf($prefix,[StringComparison]::OrdinalIgnoreCase) -ge 0)})
    if($found.Count){throw 'Exit this installation from its tray menu before rollback or uninstall. Other installations were not stopped.'}
}
function Test-WtsInstalledPointer([string]$Root,$Pointer) {
    $version=Assert-WtsChild $Root (Join-Path $Root ('versions\'+$Pointer.version))
    [void](Test-WtsPackage $version -RequireManifest -AllowBuildOutput)
    if($Pointer.mode -eq 'Compiled'){$exe=Assert-WtsChild $version (Join-Path $version 'bin\SixSlotSnap.exe');if($Pointer.compiledSha256 -notmatch '^[0-9a-f]{64}$' -or !(Test-Path -LiteralPath $exe) -or (Get-FileHash -LiteralPath $exe).Hash.ToLowerInvariant() -ne $Pointer.compiledSha256){throw 'The installed compiled artifact is missing or changed. Reinstall a verified package.'}}
}
function Set-WtsCurrent([string]$Root,$Candidate) {
    $path=Join-Path $Root 'current.json';$prior=if(Test-Path -LiteralPath $path){Read-WtsPointer $path}else{$null}
    if($prior -and $prior.version -eq $Candidate.version -and $prior.mode -eq $Candidate.mode){if($prior.compiledSha256 -ne $Candidate.compiledSha256){Write-WtsAtomic $path ($Candidate | ConvertTo-Json -Depth 10)};return}
    $previous=Join-Path $Root 'previous.json';$previousText=if(Test-Path -LiteralPath $previous){[IO.File]::ReadAllText($previous)}else{$null}
    if($prior){Write-WtsAtomic $previous ($prior | ConvertTo-Json -Depth 10)}
    try{Write-WtsAtomic $path ($Candidate | ConvertTo-Json -Depth 10)}catch{if($prior){if($null -ne $previousText){Write-WtsAtomic $previous $previousText}else{[void](Assert-WtsChild $Root $previous);Remove-Item -LiteralPath $previous -Force}};throw}
}
function Invoke-WtsValidation([string]$Executable,[string]$Arguments,[string]$Folder) {
    $out=Join-Path $Folder 'candidate-stdout.txt';$err=Join-Path $Folder 'candidate-stderr.txt'
    $child=Start-Process -FilePath $Executable -ArgumentList $Arguments -WindowStyle Hidden -RedirectStandardOutput $out -RedirectStandardError $err -PassThru
    [void]$child.Handle
    try{if(!$child.WaitForExit(60000)){$child.Kill();throw 'Candidate validation exceeded 60 seconds; the active version was preserved.'};$child.WaitForExit();if($child.ExitCode -ne 0){$lines=@(Get-Content -LiteralPath $out -ErrorAction SilentlyContinue)+@(Get-Content -LiteralPath $err -ErrorAction SilentlyContinue);throw ('Candidate validation failed; previous active version and data were preserved. '+(($lines | Select-Object -Last 12) -join ' '))}}finally{$child.Dispose()}
}
function Assert-WtsConfiguration([string]$Root,[string]$VersionDirectory) {
    $settings=Assert-WtsChild $Root (Join-Path $Root 'data\settings.xml')
    if(!(Test-Path -LiteralPath $settings)){return}
    $capabilities=Join-Path $VersionDirectory 'release\capabilities.json'
    if(Test-Path -LiteralPath $capabilities){$policy=Get-Content -Raw -LiteralPath $capabilities | ConvertFrom-Json;if($policy.schema -eq 1 -and $policy.unknownSettings -eq 'preserve-and-recover'){return}}
    $probeScript=Join-Path $PSScriptRoot 'Test-ConfigurationCompatibility.ps1'
    if(!(Test-Path -LiteralPath $probeScript)){throw 'Configuration compatibility tools are missing. Update the complete installer bundle before rollback or launch.'}
    $temporary=Assert-WtsChild $Root (Join-Path $Root ('.compatibility-'+[Guid]::NewGuid().ToString('N')))
    New-Item -ItemType Directory -Path $temporary | Out-Null
    try{
        $snapshot=Join-Path $temporary 'settings.xml';Copy-Item -LiteralPath $settings -Destination $snapshot
        $hostExe=Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe'
        try{Invoke-WtsValidation $hostExe ('-NoProfile -STA -ExecutionPolicy Bypass -File "'+$probeScript+'" -SourceRoot "'+$VersionDirectory+'" -SettingsFile "'+$snapshot+'"') $temporary}catch{throw 'The selected version cannot safely read the saved settings. The active version and settings were preserved. Back up data and restore settings written by that version before retrying.'}
    }finally{[void](Assert-WtsChild $Root $temporary);if((Split-Path -Leaf $temporary) -notmatch '^\.compatibility-[0-9a-f]{32}$'){throw 'Unexpected compatibility cleanup path.'};Remove-Item -LiteralPath $temporary -Recurse -Force}
}
