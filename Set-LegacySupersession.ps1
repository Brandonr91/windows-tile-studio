[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$LegacyDirectory,
    [string]$ManagerSourceDirectory=$PSScriptRoot,
    [string]$ManagerDataDirectory,
    [switch]$Disable
)
$ErrorActionPreference='Stop'
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
if (-not $existing -and (Test-Path -LiteralPath $backup)) { throw 'An unregistered backup already exists; inspect it before enabling supersession.' }
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
    if (-not $existing) { Copy-Item -LiteralPath $application -Destination $backup }
    $record=[xml]'<TileStudioSupersession><ManagerScript/><DataDirectory/><OriginalHash/><GuardHash/></TileStudioSupersession>'
    $record.TileStudioSupersession.ManagerScript=$script
    $record.TileStudioSupersession.DataDirectory=$data
    $record.TileStudioSupersession.OriginalHash=$originalHash
    $record.TileStudioSupersession.GuardHash=(Get-FileHash -LiteralPath $temporary -Algorithm SHA256).Hash
    $record.Save($config+'.tmp')
    Copy-Item -LiteralPath $temporary -Destination $application -Force
    Move-Item -LiteralPath ($config+'.tmp') -Destination $config -Force
    [PSCustomObject]@{Enabled=$true;LegacyExecutable=$application;OriginalBackup=$backup;ManagerScript=$script;DataDirectory=$data}
} finally { if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary } }
