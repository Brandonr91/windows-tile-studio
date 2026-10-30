[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param([Parameter(Mandatory=$true)][string]$InstallRoot,[ValidateSet('Status','Validate','Rollback','Uninstall','AddShortcut','RemoveShortcut')][string]$Action='Status',[switch]$RemoveData,[string]$MenuDirectory=([Environment]::GetFolderPath('Programs')),[string]$StartupDirectory=([Environment]::GetFolderPath('Startup')))
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'InstallationSupport.ps1')
$InstallRoot=[IO.Path]::GetFullPath($InstallRoot).TrimEnd('\','/')
if($InstallRoot -eq [IO.Path]::GetPathRoot($InstallRoot).TrimEnd('\','/')){throw 'A dedicated owned installation folder is required.'}
Assert-WtsNoLinks $InstallRoot
$marker=Get-Content -Raw -LiteralPath (Join-Path $InstallRoot '.windows-tile-studio.json') | ConvertFrom-Json
if($marker.schema -ne 1 -or $marker.repository -ne 'Brandonr91/windows-tile-studio'){throw 'This is not an owned Tile Studio installation.'}
$pointerPath=Join-Path $InstallRoot 'current.json'
$current=if(Test-Path -LiteralPath $pointerPath){Read-WtsPointer $pointerPath}else{$null}
$version=if($current){Assert-WtsChild $InstallRoot (Join-Path $InstallRoot ('versions\'+$current.version))}else{$null}
if($Action -eq 'Status'){[pscustomobject]@{Installed=($null -ne $current);InstallRoot=$InstallRoot;Version=$(if($current){$current.version});Mode=$(if($current){$current.mode});DataDirectory=(Join-Path $InstallRoot 'data');RollbackAvailable=(Test-Path -LiteralPath (Join-Path $InstallRoot 'previous.json'))};return}
if(!$current -and $Action -ne 'Uninstall'){throw 'No active version is installed; retained data can be reused on reinstall.'}
if($Action -eq 'Validate'){Test-WtsInstalledPointer $InstallRoot $current;[pscustomobject]@{Valid=$true;Version=$current.version};return}
if($Action -in @('AddShortcut','RemoveShortcut')){if($PSCmdlet.ShouldProcess($MenuDirectory,$Action)){Set-WtsMenuLink $InstallRoot $MenuDirectory -Remove:($Action -eq 'RemoveShortcut')};return}
Assert-WtsNoRunningInstall $InstallRoot
if($Action -eq 'Rollback'){
    $previous=Read-WtsPointer (Join-Path $InstallRoot 'previous.json')
    $previousPath=Assert-WtsChild $InstallRoot (Join-Path $InstallRoot ('versions\'+$previous.version))
    Test-WtsInstalledPointer $InstallRoot $previous
    Assert-WtsConfiguration $InstallRoot $previousPath
    if($PSCmdlet.ShouldProcess($InstallRoot,'Switch to the previous verified version; keep data and startup preference')){Set-WtsCurrent $InstallRoot $previous}
    return
}
$owned=@('versions','current.json','current.json.bak','previous.json','previous.json.bak','Launch-Installed.ps1','Launch-Installed.ps1.bak','Start.cmd','Start.cmd.bak','Manage-Installation.ps1','Manage-Installation.ps1.bak','InstallationSupport.ps1','InstallationSupport.ps1.bak','Test-ConfigurationCompatibility.ps1','Test-ConfigurationCompatibility.ps1.bak','Rollback.cmd','Rollback.cmd.bak','Uninstall.cmd','Uninstall.cmd.bak')
if($RemoveData){$owned+=@('data','.windows-tile-studio.json','.windows-tile-studio.json.bak')}
foreach($name in $owned){$target=Assert-WtsChild $InstallRoot (Join-Path $InstallRoot $name);if(Test-Path -LiteralPath $target -PathType Container){[void]@(Get-WtsTreeFiles $target)}}
if(!$PSCmdlet.ShouldProcess($InstallRoot,('Uninstall owned code/launchers; '+$(if($RemoveData){'DELETE this installation data'}else{'PRESERVE data and unrelated files'})))){return}
Set-WtsMenuLink $InstallRoot $MenuDirectory -Remove
# Remove only a shortcut whose owned command targets this installation.
$shortcut=Assert-WtsChild $StartupDirectory (Join-Path $StartupDirectory 'Windows Tile Studio.lnk')
if(Test-Path -LiteralPath $shortcut -PathType Leaf){$shell=$null;$link=$null;try{$shell=New-Object -ComObject WScript.Shell;$link=$shell.CreateShortcut($shortcut);$target=$link.TargetPath;$shortcutArguments=$link.Arguments;$prefix=$InstallRoot+'\';$ownedCommand=$shortcutArguments.EndsWith(' --startup-owner WindowsTileStudio') -or $shortcutArguments.EndsWith(' -StartupOwner WindowsTileStudio');$thisInstall=($target.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase) -and $shortcutArguments.Contains('"'+(Join-Path $InstallRoot 'data')+'"')) -or ($target -eq (Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe') -and ($shortcutArguments.Contains('"'+(Join-Path $InstallRoot 'Launch-Installed.ps1')+'"') -or ($shortcutArguments.Contains('"'+$prefix+'versions\') -and $shortcutArguments.Contains('"'+(Join-Path $InstallRoot 'data')+'"'))));if($ownedCommand -and $thisInstall){Remove-Item -LiteralPath $shortcut -Force}}finally{if($link){[Runtime.InteropServices.Marshal]::FinalReleaseComObject($link)|Out-Null};if($shell){[Runtime.InteropServices.Marshal]::FinalReleaseComObject($shell)|Out-Null}}}
foreach($name in $owned){$target=Assert-WtsChild $InstallRoot (Join-Path $InstallRoot $name);if(Test-Path -LiteralPath $target){Remove-Item -LiteralPath $target -Recurse -Force}}
[pscustomobject]@{Uninstalled=$true;DataPreserved=(!$RemoveData);InstallRoot=$InstallRoot}
