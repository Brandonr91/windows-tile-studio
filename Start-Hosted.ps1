[CmdletBinding()]
param(
    [string]$DataDirectory,
    [ValidateSet('Run','Show','Settings','Quit','Preview','Validate')]
    [string]$Action = 'Run',
    [switch]$NoRecover,
    [switch]$RunInProcess,
    [string]$StartupOwner
)
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($DataDirectory)) { $DataDirectory = Join-Path $PSScriptRoot 'data' }
$DataDirectory = [IO.Path]::GetFullPath($DataDirectory)
$windowsPowerShell = Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe'
if (-not (Test-Path -LiteralPath $windowsPowerShell)) {
    throw 'Hosted mode requires Windows PowerShell 5.1 and Windows Forms on Windows.'
}

# The background host is always Windows PowerShell 5.1 in an STA thread.
# No application EXE or OutputAssembly is produced by this launcher.
if (-not $RunInProcess -and $Action -ne 'Validate') {
    $scriptPath = Join-Path $PSScriptRoot 'Start-Hosted.ps1'
    $quotedData = '"' + $DataDirectory.TrimEnd('\') + '"'
    $arguments = @('-NoProfile','-STA','-ExecutionPolicy','Bypass','-File',('"'+$scriptPath+'"'),'-RunInProcess','-Action',$Action,'-DataDirectory',$quotedData)
    if ($NoRecover) { $arguments += '-NoRecover' }
    $child = Start-Process -FilePath $windowsPowerShell -ArgumentList $arguments -WindowStyle Hidden -PassThru
    [PSCustomObject]@{ Mode='PowerShellHosted'; ProcessId=$child.Id; DataDirectory=$DataDirectory }
    return
}
if ($PSVersionTable.PSVersion.Major -ne 5 -or $PSVersionTable.PSEdition -ne 'Desktop') {
    throw 'Use Windows PowerShell 5.1 with -STA for -RunInProcess or -Action Validate. Start-Hosted.cmd selects it automatically.'
}
if ([Threading.Thread]::CurrentThread.GetApartmentState() -ne [Threading.ApartmentState]::STA) {
    throw 'The Windows Forms host needs an STA thread. Start with powershell.exe -STA.'
}

$sourceFiles = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | Sort-Object Name | ForEach-Object { $_.FullName })
if ($sourceFiles.Count -lt 5) { throw 'The source folder is incomplete. Download or extract the whole project.' }
Add-Type -AssemblyName System.Windows.Forms,System.Drawing,System.Xml.Linq
if (-not ('SixSlotSnap' -as [type])) {
    Add-Type -LiteralPath $sourceFiles -ReferencedAssemblies System.dll,System.Core.dll,System.Xml.dll,System.Xml.Linq.dll,System.Drawing.dll,System.Windows.Forms.dll,Accessibility.dll
}

if ($Action -eq 'Validate') {
    New-Item -ItemType Directory -Force -Path $DataDirectory | Out-Null
    $settings = [AppSettings]::Defaults()
    $settings.Validate()
    $checkPath = Join-Path $DataDirectory 'host-check.xml'
    [SettingsStore]::Save($checkPath,$settings)
    $warning = $null
    $loaded = [SettingsStore]::Load($checkPath,[ref]$warning)
    if ($warning -or $loaded.Profiles.Count -ne 6) { throw 'In-memory settings serialization failed.' }
    [PSCustomObject]@{
        Mode='PowerShellHosted'
        PowerShellVersion=$PSVersionTable.PSVersion.ToString()
        AssemblyLocation=[SixSlotSnap].Assembly.Location
        StandaloneExeRequested=$false
        Profiles=$loaded.Profiles.Count
    } | ConvertTo-Json -Compress
    return
}

if ($Action -eq 'Run') {
    New-Item -ItemType Directory -Force -Path $DataDirectory | Out-Null
    if (-not (Test-Path -LiteralPath (Join-Path $DataDirectory 'settings.xml'))) {
        $previous = Join-Path $PSScriptRoot 'bin\settings.xml'
        if (Test-Path -LiteralPath $previous) { Copy-Item -LiteralPath $previous -Destination (Join-Path $DataDirectory 'settings.xml') }
        else {
            $legacy = Join-Path $PSScriptRoot 'bin\layout.txt'
            if (Test-Path -LiteralPath $legacy) { Copy-Item -LiteralPath $legacy -Destination (Join-Path $DataDirectory 'layout.txt') }
        }
    }
}

$applicationArguments = [Collections.Generic.List[string]]::new()
$applicationArguments.Add('--data-dir')
$applicationArguments.Add($DataDirectory)
$applicationArguments.Add('--source-root')
$applicationArguments.Add($PSScriptRoot)
switch ($Action) {
    'Show' { $applicationArguments.Add('--show') }
    'Settings' { $applicationArguments.Add('--settings') }
    'Quit' { $applicationArguments.Add('--quit') }
    'Preview' { $applicationArguments.Add('--preview') }
}
if ($NoRecover) { $applicationArguments.Add('--no-recover') }
[SixSlotSnap]::Main($applicationArguments.ToArray())
