param([Parameter(Mandatory=$true)][string]$PackageDirectory,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
. (Join-Path $root 'InstallationSupport.ps1')
$packages=[IO.Path]::GetFullPath($PackageDirectory);$output=[IO.Path]::GetFullPath($OutputDirectory)
Assert-WtsNoLinks $packages;Assert-WtsNoLinks $output
if(Test-Path -LiteralPath $output){throw 'Choose a new beta-kit folder; existing files are never overwritten.'}
$info=Get-Content -Raw -LiteralPath (Join-Path $packages 'release-info.json') | ConvertFrom-Json
if($info.schema -ne 1 -or $info.product -ne 'Windows Tile Studio' -or $info.version -notmatch '^\d+\.\d+\.\d+-(beta|rc)\.\d+$' -or @($info.artifacts).Count -ne 2){throw 'Expected source and portable beta/RC release artifacts.'}
if(@($info.artifacts | Where-Object {$_.kind -eq 'Source'}).Count -ne 1 -or @($info.artifacts | Where-Object {$_.kind -eq 'PortableCompiled'}).Count -ne 1 -or @($info.artifacts.file | Select-Object -Unique).Count -ne 2){throw 'Beta archives must have distinct source/portable entries.'}
foreach($artifact in $info.artifacts){Assert-WtsRelative $artifact.file;if($artifact.file.Contains('/') -or $artifact.sha256 -notmatch '^[0-9a-f]{64}$'){throw 'Unexpected beta archive metadata.'};$file=Assert-WtsChild $packages (Join-Path $packages $artifact.file);if((Get-FileHash -LiteralPath $file).Hash.ToLowerInvariant() -ne $artifact.sha256){throw 'Beta archive checksum mismatch.'}}
New-Item -ItemType Directory -Path $output | Out-Null
foreach($artifact in $info.artifacts){Copy-Item -LiteralPath (Join-Path $packages $artifact.file) -Destination (Join-Path $output $artifact.file)}
Copy-Item -LiteralPath (Join-Path $packages 'release-info.json') -Destination $output
# Derive checksums from verified metadata rather than copying an unchecked list.
[IO.File]::WriteAllLines((Join-Path $output 'SHA256SUMS.txt'),[string[]]@($info.artifacts | ForEach-Object {$_.sha256+'  '+$_.file}),[Text.UTF8Encoding]::new($false))
foreach($name in @('BETA-TESTING.md','BETA-FEEDBACK-TEMPLATE.md','RELEASE-CHECKLIST.md','RELEASE-AND-RECOVERY.md')){Copy-Item -LiteralPath (Join-Path $root ('docs\'+$name)) -Destination $output}
[IO.File]::WriteAllText((Join-Path $output 'START-HERE.txt'),('Windows Tile Studio '+$info.version+" - unsigned local beta.`r`nRead BETA-TESTING.md before installing. Extract a complete archive; keep its scripts together.`r`nVerify the archive SHA-256 against a trusted copy of SHA256SUMS.txt.`r`nRecord tested cases and use BETA-FEEDBACK-TEMPLATE.md. Manual release gates remain open.`r`nThis kit contains no runtime logs, user settings, workspaces or automatic uploads.`r`n"),[Text.UTF8Encoding]::new($false))
$inventory=@(Get-ChildItem -LiteralPath $output -File | Sort-Object Name | ForEach-Object {[ordered]@{file=$_.Name;bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName).Hash.ToLowerInvariant()}})
[IO.File]::WriteAllText((Join-Path $output 'kit-manifest.json'),([ordered]@{schema=1;version=$info.version;sourceRevision=$info.sourceRevision;sourceDirty=$info.sourceDirty;files=$inventory} | ConvertTo-Json -Depth 6),[Text.UTF8Encoding]::new($false))
Write-Output ('Verified local beta kit: '+$output)
