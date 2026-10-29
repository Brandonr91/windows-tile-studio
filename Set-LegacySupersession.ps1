[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$LegacyDirectory,
    [string]$ManagerSourceDirectory,
    [string]$ManagerDataDirectory,
    [switch]$Disable
)
$ErrorActionPreference='Stop'
if ([string]::IsNullOrWhiteSpace($ManagerSourceDirectory)) { $ManagerSourceDirectory=$PSScriptRoot }
$legacyRoot=[IO.Path]::GetFullPath($LegacyDirectory)
$application=Join-Path $legacyRoot 'SixSlotSnap.exe'
$backup=Join-Path $legacyRoot 'SixSlotSnap.legacy-original.exe'
$config=Join-Path $legacyRoot 'SixSlotSnap.supersession.xml'
if (-not (Test-Path -LiteralPath $application -PathType Leaf)) { throw 'Select a legacy folder containing SixSlotSnap.exe.' }
$existing=$null
if (Test-Path -LiteralPath $config) {
    [xml]$existing=Get-Content -Raw -LiteralPath $config
    if ($existing.DocumentElement.Name -ne 'TileStudioSupersession') { throw 'Unrecognized supersession record.' }
    if ((Get-FileHash -LiteralPath $application -Algorithm SHA256).Hash -ne $existing.TileStudioSupersession.GuardHash) { throw 'The legacy executable changed after setup. Refusing to overwrite it.' }
    if (-not (Test-Path -LiteralPath $backup) -or (Get-FileHash -LiteralPath $backup -Algorithm SHA256).Hash -ne $existing.TileStudioSupersession.OriginalHash) { throw 'The original backup is missing or changed.' }
}
if ($Disable) {
    if (-not $existing) { throw 'Legacy supersession is not enabled in this folder.' }
    Copy-Item -LiteralPath $backup -Destination $application -Force
    Remove-Item -LiteralPath $config
    # Retain the original backup for review and recovery.
    Write-Output 'Legacy supersession disabled; original executable restored. Exit Tile Studio before starting it.'
    return
}
$managerRoot=[IO.Path]::GetFullPath($ManagerSourceDirectory)
$script=Join-Path $managerRoot 'Start-Hosted.ps1'
if (-not (Test-Path -LiteralPath $script) -or -not (Test-Path -LiteralPath (Join-Path $managerRoot 'src\SixSlotSnap.cs'))) { throw 'Select the full manager source folder containing Start-Hosted.ps1 and src/SixSlotSnap.cs.' }
if ($managerRoot.TrimEnd('\') -eq $legacyRoot.TrimEnd('\')) { throw 'The full manager and legacy controller must have separate folders.' }
if ([string]::IsNullOrWhiteSpace($ManagerDataDirectory)) { $ManagerDataDirectory=Join-Path $managerRoot 'data' }
$data=[IO.Path]::GetFullPath($ManagerDataDirectory)
if (-not $existing -and (Test-Path -LiteralPath $backup) -and (Get-FileHash -LiteralPath $backup).Hash -ne (Get-FileHash -LiteralPath $application).Hash) { throw 'An unregistered backup differs from the current executable; inspect it before enabling supersession.' }
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { $compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
$temporary=Join-Path $legacyRoot ('TileStudioRedirect-'+[Guid]::NewGuid().ToString('N')+'.exe')
try {
    & $compiler /nologo /target:winexe /reference:System.Windows.Forms.dll /reference:System.Xml.Linq.dll ('/out:'+$temporary) (Join-Path $PSScriptRoot 'tools\LegacyRedirect.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Could not build the compatibility launcher.' }
    $originalHash=if ($existing) { [string]$existing.TileStudioSupersession.OriginalHash } else { (Get-FileHash -LiteralPath $application -Algorithm SHA256).Hash }
    # Stop only the executable in the explicitly selected legacy folder.
    # No process-name-wide termination, startup entry edits, or service install.
    foreach ($process in @(Get-Process -Name SixSlotSnap -ErrorAction SilentlyContinue)) {
        if ($process.Path -and [string]::Equals($process.Path,$application,[StringComparison]::OrdinalIgnoreCase)) {
            [void]$process.CloseMainWindow()
            if (-not $process.WaitForExit(1500)) { Stop-Process -Id $process.Id; $process.WaitForExit() }
        }
    }
    if (-not $existing -and -not (Test-Path -LiteralPath $backup)) { Copy-Item -LiteralPath $application -Destination $backup }
    $record=[xml]'<TileStudioSupersession><ManagerScript/><DataDirectory/><OriginalHash/><GuardHash/></TileStudioSupersession>'
    $record.SelectSingleNode('/TileStudioSupersession/ManagerScript').InnerText=[string]$script
    $record.SelectSingleNode('/TileStudioSupersession/DataDirectory').InnerText=[string]$data
    $record.SelectSingleNode('/TileStudioSupersession/OriginalHash').InnerText=[string]$originalHash
    $record.SelectSingleNode('/TileStudioSupersession/GuardHash').InnerText=[string](Get-FileHash -LiteralPath $temporary -Algorithm SHA256).Hash
    $record.Save($config+'.tmp')
    Copy-Item -LiteralPath $temporary -Destination $application -Force
    Move-Item -LiteralPath ($config+'.tmp') -Destination $config -Force
    # Standalone recovery must remain available if the preferred manager moves.
    $restoreScript=@'
$ErrorActionPreference='Stop'
$recordPath=Join-Path $PSScriptRoot 'SixSlotSnap.supersession.xml'
if (-not (Test-Path -LiteralPath $recordPath)) { Write-Output 'Legacy redirection is already disabled.'; return }
[xml]$record=Get-Content -Raw -LiteralPath $recordPath
$application=Join-Path $PSScriptRoot 'SixSlotSnap.exe'
$backup=Join-Path $PSScriptRoot 'SixSlotSnap.legacy-original.exe'
if ($record.DocumentElement.Name -ne 'TileStudioSupersession' -or (Get-FileHash -LiteralPath $application).Hash -ne $record.TileStudioSupersession.GuardHash -or (Get-FileHash -LiteralPath $backup).Hash -ne $record.TileStudioSupersession.OriginalHash) { throw 'The launcher or backup changed. No files were replaced; inspect the original backup before restoring.' }
Copy-Item -LiteralPath $backup -Destination $application -Force
Remove-Item -LiteralPath $recordPath
Write-Output 'Original Six Slot Snap restored. Exit Windows Tile Studio before starting it. The original backup has been retained.'
'@
    [IO.File]::WriteAllText((Join-Path $legacyRoot 'Restore-SixSlotSnap.ps1'),$restoreScript)
    [IO.File]::WriteAllText((Join-Path $legacyRoot 'Restore-SixSlotSnap.cmd'),"@echo off`r`n`"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe`" -NoProfile -ExecutionPolicy Bypass -File `"%~dp0Restore-SixSlotSnap.ps1`"`r`npause`r`n")
    [PSCustomObject]@{Enabled=$true;LegacyExecutable=$application;OriginalBackup=$backup;ManagerScript=$script;DataDirectory=$data}
} finally { if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary } }
