param([string]$ImagePath)
$ErrorActionPreference='Stop'
if ($PSVersionTable.PSVersion.Major -ne 5 -or [Threading.Thread]::CurrentThread.GetApartmentState() -ne 'STA') {
    throw 'Run this test with Windows PowerShell 5.1 -STA.'
}
$root=Split-Path -Parent $PSScriptRoot
$sources=@(Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs' | ForEach-Object {$_.FullName})
$sources+=Join-Path $PSScriptRoot 'LauncherUiTests.cs'
Add-Type -AssemblyName System.Windows.Forms,System.Drawing,System.Xml.Linq
Add-Type -LiteralPath $sources -ReferencedAssemblies System.dll,System.Core.dll,System.Xml.dll,System.Xml.Linq.dll,System.Drawing.dll,System.Windows.Forms.dll
$arguments=[string[]]@()
if ($ImagePath) {$arguments=[string[]]@([IO.Path]::GetFullPath($ImagePath))}
$result=[LauncherUiTests]::Main($arguments)
if ($result -ne 0) {throw 'Hosted launcher UI paint/DPI regression failed.'}
