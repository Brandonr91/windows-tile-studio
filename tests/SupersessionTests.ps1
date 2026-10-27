[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$run=Join-Path $root ('bin\supersession-tests\'+[Guid]::NewGuid().ToString('N'))
$legacy=Join-Path $run 'legacy controller'
$manager=Join-Path $run 'full manager'
$data=Join-Path $run 'persistent data'
New-Item -ItemType Directory -Force -Path $legacy,(Join-Path $manager 'src'),$data | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$source=Join-Path $run 'FakeLegacy.cs'
[IO.File]::WriteAllText($source,'class FakeLegacy { static void Main() { System.Threading.Thread.Sleep(60000); } }')
& $compiler /nologo /target:winexe ('/out:'+(Join-Path $legacy 'SixSlotSnap.exe')) $source
if ($LASTEXITCODE -ne 0) { throw 'Fixture build failed.' }
[IO.File]::WriteAllText((Join-Path $manager 'src\SixSlotSnap.cs'),'// Source fixture; no controller is launched.')
[IO.File]::WriteAllText((Join-Path $manager 'Start-Hosted.ps1'),'param([string]$DataDirectory) [IO.File]::WriteAllText((Join-Path $DataDirectory "redirected.txt"),"full-manager")')
$original=(Get-FileHash -LiteralPath (Join-Path $legacy 'SixSlotSnap.exe')).Hash
$setup=Join-Path $root 'Set-LegacySupersession.ps1'
$oldProcess=Start-Process -FilePath (Join-Path $legacy 'SixSlotSnap.exe') -WindowStyle Hidden -PassThru
& $setup -LegacyDirectory $legacy -ManagerSourceDirectory $manager -ManagerDataDirectory $data | Out-Null
if (-not $oldProcess.WaitForExit(3000)) { throw 'Enabling supersession left the selected legacy controller running.' }
if ((Get-FileHash -LiteralPath (Join-Path $legacy 'SixSlotSnap.legacy-original.exe')).Hash -ne $original) { throw 'Original executable was not preserved.' }
& $setup -LegacyDirectory $legacy -ManagerSourceDirectory $manager -ManagerDataDirectory $data | Out-Null
$guard=Start-Process -FilePath (Join-Path $legacy 'SixSlotSnap.exe') -WindowStyle Hidden -PassThru
if (-not $guard.WaitForExit(5000)) { throw 'Redirect launcher did not exit.' }
$deadline=[DateTime]::UtcNow.AddSeconds(8)
while (-not (Test-Path -LiteralPath (Join-Path $data 'redirected.txt')) -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Milliseconds 100 }
if (-not (Test-Path -LiteralPath (Join-Path $data 'redirected.txt'))) { throw 'Redirect did not start the preferred source launcher. Run this test with the daily controller stopped.' }
& $setup -LegacyDirectory $legacy -Disable | Out-Null
if ((Get-FileHash -LiteralPath (Join-Path $legacy 'SixSlotSnap.exe')).Hash -ne $original) { throw 'Disable did not restore the original executable.' }
if (Test-Path -LiteralPath (Join-Path $legacy 'SixSlotSnap.supersession.xml')) { throw 'Disable left an active redirect record.' }
'PASS: enable closes the selected legacy process; re-enable preserves original bytes; old EXE starts preferred source launcher with separate data; disable restores original bytes.'
