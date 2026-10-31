param([string]$WorkDirectory)
$ErrorActionPreference='Stop'
if($PSVersionTable.PSVersion.Major -ne 5 -or [Threading.Thread]::CurrentThread.GetApartmentState() -ne 'STA'){throw 'Run with Windows PowerShell 5.1 -STA.'}
$root=Split-Path -Parent $PSScriptRoot
if([string]::IsNullOrWhiteSpace($WorkDirectory)){$WorkDirectory=Join-Path $root 'bin\phase1-native-hosted'}
$sources=@(Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs' | ForEach-Object {$_.FullName})
$sources+=Join-Path $PSScriptRoot 'PhaseOneNativeTests.cs'
Add-Type -AssemblyName System.Windows.Forms,System.Drawing,System.Xml.Linq
Add-Type -LiteralPath $sources -ReferencedAssemblies System.dll,System.Core.dll,System.Xml.dll,System.Xml.Linq.dll,System.Drawing.dll,System.Windows.Forms.dll,Accessibility.dll
$result=[PhaseOneNativeTests]::Main([string[]]@([IO.Path]::GetFullPath($WorkDirectory),$root))
if($result -ne 0){throw 'Source-hosted phase-1 native checks were not passed. Check output for failure or an existing-controller refusal.'}
