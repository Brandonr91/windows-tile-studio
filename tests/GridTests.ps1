[CmdletBinding()]
param([string]$WorkDirectory)
$ErrorActionPreference='Stop'
if($PSVersionTable.PSVersion.Major -ne 5 -or [Threading.Thread]::CurrentThread.GetApartmentState() -ne 'STA'){throw 'Run with Windows PowerShell 5.1 -STA.'}
$root=Split-Path -Parent $PSScriptRoot
if([string]::IsNullOrWhiteSpace($WorkDirectory)){$WorkDirectory=Join-Path $root 'bin\geometry-hosted'}
$run=Join-Path ([IO.Path]::GetFullPath($WorkDirectory)) ([Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $run -Force | Out-Null
$sources=@(Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs' | ForEach-Object {$_.FullName})
$sources+=Join-Path $PSScriptRoot 'GridTests.cs'
$sources+=Join-Path $PSScriptRoot 'TestRunRecord.cs'
Add-Type -AssemblyName System.Windows.Forms,System.Drawing,System.Xml.Linq
$types=Add-Type -LiteralPath $sources -ReferencedAssemblies System.dll,System.Core.dll,System.Xml.dll,System.Xml.Linq.dll,System.Drawing.dll,System.Windows.Forms.dll,Accessibility.dll -PassThru
if([SixSlotSnap].Assembly.Location -ne ''){throw 'Geometry host unexpectedly loaded a disk assembly.'}
$entry=$types | Where-Object Name -eq 'GridTests' | Select-Object -First 1
$result=$entry.GetMethod('Run').Invoke($null,[object[]]@($run.ToString()))
if($result -ne 0){throw 'Source-hosted geometry/settings checks failed.'}
Write-Output ('Result directory: '+$run)
