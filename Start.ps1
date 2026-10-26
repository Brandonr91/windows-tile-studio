param([string]$DataDirectory)
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($DataDirectory)) { $DataDirectory = Join-Path $PSScriptRoot 'data' }
$DataDirectory = [IO.Path]::GetFullPath($DataDirectory)
$executable = Join-Path $PSScriptRoot 'bin\SixSlotSnap.exe'
if (-not (Test-Path -LiteralPath $executable)) { & (Join-Path $PSScriptRoot 'Build.ps1') }
New-Item -ItemType Directory -Force -Path $DataDirectory | Out-Null
# Preserve preferences from the original portable bin/ settings location.
if (-not (Test-Path -LiteralPath (Join-Path $DataDirectory 'settings.xml'))) {
    $previous = Join-Path $PSScriptRoot 'bin\settings.xml'
    if (Test-Path -LiteralPath $previous) { Copy-Item -LiteralPath $previous -Destination (Join-Path $DataDirectory 'settings.xml') }
    else {
        $legacy = Join-Path $PSScriptRoot 'bin\layout.txt'
        if (Test-Path -LiteralPath $legacy) { Copy-Item -LiteralPath $legacy -Destination (Join-Path $DataDirectory 'layout.txt') }
    }
}
Start-Process -FilePath $executable -ArgumentList @('--data-dir',('"'+$DataDirectory.TrimEnd('\')+'"')) -WindowStyle Hidden

