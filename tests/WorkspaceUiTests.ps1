param([string]$OutputDirectory)
$ErrorActionPreference='Stop'
if ($PSVersionTable.PSVersion.Major -ne 5 -or [Threading.Thread]::CurrentThread.GetApartmentState() -ne 'STA') {throw 'Run this test with Windows PowerShell 5.1 -STA.'}
$root=Split-Path -Parent $PSScriptRoot
if (!$OutputDirectory) {$OutputDirectory=Join-Path $root 'bin\workspace-hosted-ui-data'}
$sources=@(Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs' | ForEach-Object {$_.FullName})
$sources+=Join-Path $PSScriptRoot 'WorkspaceUiTests.cs'
Add-Type -AssemblyName System.Windows.Forms,System.Drawing,System.Xml.Linq
Add-Type -LiteralPath $sources -ReferencedAssemblies System.dll,System.Core.dll,System.Xml.dll,System.Xml.Linq.dll,System.Drawing.dll,System.Windows.Forms.dll
if ([WorkspaceUiTests]::Main([string[]]@([IO.Path]::GetFullPath($OutputDirectory))) -ne 0) {throw 'Hosted workspace UI regression failed.'}
