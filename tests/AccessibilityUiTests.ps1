param([string]$OutputDirectory)
$ErrorActionPreference='Stop'
if ($PSVersionTable.PSVersion.Major -ne 5 -or [Threading.Thread]::CurrentThread.GetApartmentState() -ne 'STA') {throw 'Use Windows PowerShell 5.1 -STA.'}
$root=Split-Path -Parent $PSScriptRoot
if (!$OutputDirectory) {$OutputDirectory=Join-Path $root 'bin\accessibility-hosted-ui-data'}
$sources=@(Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs' | ForEach-Object {$_.FullName})
$sources+=Join-Path $PSScriptRoot 'AccessibilityUiTests.cs'
Add-Type -AssemblyName System.Windows.Forms,System.Drawing,System.Xml.Linq
Add-Type -LiteralPath $sources -ReferencedAssemblies System.dll,System.Core.dll,System.Xml.dll,System.Xml.Linq.dll,System.Drawing.dll,System.Windows.Forms.dll,Accessibility.dll
if ([AccessibilityUiTests]::Main([string[]]@([IO.Path]::GetFullPath($OutputDirectory))) -ne 0) {throw 'Hosted accessibility/support checks failed.'}
