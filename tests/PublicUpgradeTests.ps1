param([Parameter(Mandatory=$true)][string]$PackageDirectory,[Parameter(Mandatory=$true)][string]$PreviousArchive,[Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{40}$')][string]$PreviousRevision,[string]$WorkDirectory)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
. (Join-Path $root 'InstallationSupport.ps1')
if(@(Get-CimInstance Win32_Process | Where-Object {$_.Name -eq 'SixSlotSnap.exe' -or ($_.Name -eq 'powershell.exe' -and $_.CommandLine -like '*Start-Hosted.ps1*')}).Count){throw 'Stop the daily controller before native public-upgrade checks.'}
if(!$WorkDirectory){$WorkDirectory=Join-Path $env:TEMP 'WtsPublicUpgrade'}
$run=Join-Path ([IO.Path]::GetFullPath($WorkDirectory)) ([Guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Path $run -Force | Out-Null
$install=Join-Path $run 'app';$hostExe=Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe'
$info=Get-Content -Raw -LiteralPath (Join-Path $PackageDirectory 'release-info.json') | ConvertFrom-Json
Add-Type 'using System;using System.Runtime.InteropServices;public static class WtsUpgradeNative{[DllImport("user32.dll")]public static extern bool PostMessage(IntPtr h,int m,IntPtr w,IntPtr l);[DllImport("user32.dll")]public static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);}'
function Assert([bool]$Value,[string]$Message){if(!$Value){throw $Message}}
function Hash([string]$Path){return (Get-FileHash -LiteralPath $Path).Hash}
function Smoke([bool]$Legacy){
    $runtime=Join-Path $install 'data\runtime-state.xml';$app=$null;$receiver=[IntPtr]::Zero
    if(Test-Path -LiteralPath $runtime){[void](Assert-WtsChild $install $runtime);Remove-Item -LiteralPath $runtime}
    $wrapper=Start-Process -FilePath $hostExe -ArgumentList ('-NoProfile -STA -ExecutionPolicy Bypass -File "'+(Join-Path $install 'Launch-Installed.ps1')+'" -NoRecover') -WindowStyle Hidden -PassThru
    try{
        $until=(Get-Date).AddSeconds(25);$state=$null
        do{try{if(Test-Path -LiteralPath $runtime){[xml]$state=Get-Content -Raw -LiteralPath $runtime}}catch [IO.IOException]{}catch [Xml.XmlException]{};if(!$state){Start-Sleep -Milliseconds 50}}while(!$state -and (Get-Date) -lt $until)
        Assert ($null -ne $state) 'Installed controller did not start.'
        $app=Get-Process -Id ([int]$state.Runtime.pid);$process=Get-CimInstance Win32_Process -Filter ('ProcessId='+$app.Id)
        Assert ($process.ExecutablePath.StartsWith($install+'\versions\',[StringComparison]::OrdinalIgnoreCase)) 'Controller came from another installation.'
        Assert (@($state.Runtime.SelectNodes('.//Zone')).Count -eq 0) 'Upgrade smoke adopted a real window.'
        if(!$Legacy){Assert ($state.Runtime.version -eq $info.version -and $state.Runtime.recoveryRequired -eq 'false' -and $state.Runtime.launcherVisible -eq 'false') 'Beta startup version/recovery/bar state is wrong.'}
        $receiver=[IntPtr]([long]$state.Runtime.controller);[uint32]$owner=0;[void][WtsUpgradeNative]::GetWindowThreadProcessId($receiver,[ref]$owner);Assert ($owner -eq $app.Id) 'Receiver ownership changed.'
    }finally{
        if($app -and !$app.HasExited){[void][WtsUpgradeNative]::PostMessage($receiver,0x8003,[IntPtr]::Zero,[IntPtr]::Zero);if(!$app.WaitForExit(5000)){$app.Kill();throw 'Test controller failed to quit normally.'}}
        if(!$wrapper.HasExited){if(!$wrapper.WaitForExit(5000)){$wrapper.Kill()}};$wrapper.Dispose();if($app){$app.Dispose()}
    }
}
try{
    # GitHub codeload embeds its immutable revision in the ZIP end comment.
    $zipBytes=[IO.File]::ReadAllBytes($PreviousArchive);$revision=$null
    for($i=$zipBytes.Length-22;$i -ge [Math]::Max(0,$zipBytes.Length-65557);$i--){if([BitConverter]::ToUInt32($zipBytes,$i) -eq 0x06054b50){$length=[BitConverter]::ToUInt16($zipBytes,$i+20);if($i+22+$length -eq $zipBytes.Length){$revision=[Text.Encoding]::ASCII.GetString($zipBytes,$i+22,$length);break}}}
    Assert ($revision -eq $PreviousRevision) 'Previous archive revision does not match its GitHub ZIP comment.'
    $old=& (Join-Path $root 'Get-FromGitHub.ps1') -ArchivePath $PreviousArchive -ExpectedSha256 (Hash $PreviousArchive) -InstallRoot $install -Mode Compiled -NoLaunch
    & $hostExe -NoProfile -STA -ExecutionPolicy Bypass -File (Join-Path $old.VersionDirectory 'Start-Hosted.ps1') -RunInProcess -Action Validate -DataDirectory $old.DataDirectory | Out-Null
    Assert ($LASTEXITCODE -eq 0) 'Public settings template failed.'
    $settings=Join-Path $old.DataDirectory 'settings.xml'
    [xml]$fixture=Get-Content -Raw -LiteralPath (Join-Path $old.DataDirectory 'host-check.xml');$fixture.AppSettings.ShowLauncherTab='false';$fixture.AppSettings.FlyoutShortcut.Key='F23';foreach($key in $fixture.AppSettings.SlotShortcuts.Shortcut){$key.Key='None'}
    $fixture.AppSettings.Profiles.LayoutProfile[0].Name='Upgrade fixture'
    $columns=$fixture.AppSettings.Profiles.LayoutProfile[0].Columns.ColumnDefinition;$columns[0].Width='0.5';$columns[1].Width='0.25';$columns[2].Width='0.25';$columns[0].Rows.SelectNodes('double')[0].InnerText='0.6';$columns[0].Rows.SelectNodes('double')[1].InnerText='0.4';$fixture.Save($settings)
    Smoke $true
    $oldBytes=[IO.File]::ReadAllBytes($settings);$oldPointer=[IO.File]::ReadAllText((Join-Path $install 'current.json'))
    $artifact=$info.artifacts | Where-Object {$_.kind -eq 'PortableCompiled'}
    $new=& (Join-Path $root 'Get-FromGitHub.ps1') -ArchivePath (Join-Path $PackageDirectory $artifact.file) -ExpectedSha256 $artifact.sha256 -InstallRoot $install -Mode Compiled -NoLaunch
    Smoke $false
    [xml]$upgraded=Get-Content -Raw -LiteralPath $settings
    Assert ($upgraded.AppSettings.Profiles.LayoutProfile[0].Name -eq 'Upgrade fixture' -and $upgraded.AppSettings.FlyoutShortcut.Key -eq 'F23' -and $upgraded.AppSettings.ShowLauncherTab -eq 'false') 'Upgrade lost existing preferences.'
    $columns=$upgraded.AppSettings.Profiles.LayoutProfile[0].Columns.ColumnDefinition;Assert ([Math]::Abs([double]$columns[0].Width-0.5) -lt 0.00001 -and [Math]::Abs([double]$columns[0].Rows.SelectNodes('double')[0].InnerText-0.6) -lt 0.00001) 'Upgrade lost custom geometry weights.'
    $upgraded.AppSettings.LauncherPositionLocked='true';$upgraded.Save($settings)
    $workspace=Join-Path $new.DataDirectory 'workspaces.xml';[IO.File]::WriteAllText($workspace,'<?xml version="1.0"?><TileStudioWorkspaces><Version>1</Version><Workspaces /></TileStudioWorkspaces>')
    $settingsHash=Hash $settings;$workspaceHash=Hash $workspace;$pointerHash=Hash (Join-Path $install 'current.json');$previousHash=Hash (Join-Path $install 'previous.json');$betaPointer=[IO.File]::ReadAllBytes((Join-Path $install 'current.json'))
    $blocked=$false;try{& (Join-Path $root 'Manage-Installation.ps1') -InstallRoot $install -Action Rollback -Confirm:$false}catch{$blocked=$_.Exception.Message.Contains('cannot safely read')}
    Assert $blocked 'Lossy rollback was not blocked.'
    Assert ((Hash $settings) -eq $settingsHash -and (Hash $workspace) -eq $workspaceHash -and (Hash (Join-Path $install 'current.json')) -eq $pointerHash -and (Hash (Join-Path $install 'previous.json')) -eq $previousHash) 'Blocked rollback changed pointers or saved data.'
    $blocked=$false;try{& (Join-Path $root 'Get-FromGitHub.ps1') -ArchivePath $PreviousArchive -ExpectedSha256 (Hash $PreviousArchive) -InstallRoot $install -Mode Compiled -NoLaunch | Out-Null}catch{$blocked=$_.Exception.Message.Contains('cannot safely read')}
    Assert $blocked 'Lossy downgrade installation was not blocked.'
    Assert ((Hash $settings) -eq $settingsHash -and (Hash (Join-Path $install 'current.json')) -eq $pointerHash -and (Hash (Join-Path $install 'previous.json')) -eq $previousHash) 'Blocked downgrade installation changed pointers or settings.'
    # Even a manually selected legacy pointer must fail before starting old code.
    [IO.File]::WriteAllText((Join-Path $install 'current.json'),$oldPointer)
    $guard=Join-Path $run 'launch-guard';New-Item -ItemType Directory -Path $guard | Out-Null
    try{Invoke-WtsValidation $hostExe ('-NoProfile -STA -ExecutionPolicy Bypass -File "'+(Join-Path $install 'Launch-Installed.ps1')+'" -NoRecover') $guard;throw 'Unsafe legacy launch passed its compatibility guard.'}catch{Assert ($_.Exception.Message.Contains('cannot safely read')) 'Unsafe legacy launch failed for an unrelated reason.'}finally{[IO.File]::WriteAllBytes((Join-Path $install 'current.json'),$betaPointer)}
    Assert ((Hash $settings) -eq $settingsHash -and (Hash $workspace) -eq $workspaceHash) 'Blocked legacy launch changed data.'
    # Explicitly restore only fixture data before the compatible rollback.
    [IO.File]::WriteAllBytes((Join-Path $run 'beta-settings-retained.xml'),[IO.File]::ReadAllBytes($settings));[IO.File]::WriteAllBytes($settings,$oldBytes)
    & (Join-Path $root 'Manage-Installation.ps1') -InstallRoot $install -Action Rollback -Confirm:$false
    Smoke $true
    Assert ((Hash $workspace) -eq $workspaceHash) 'Compatible legacy rollback modified separate workspace data.'
    [pscustomobject]@{Passed=$true;PreviousRevision=$PreviousRevision;PreviousArchiveSha256=(Hash $PreviousArchive).ToLowerInvariant();CandidateVersion=$info.version;CandidateArchiveSha256=$artifact.sha256;Checks=@('public compiled launch','beta upgrade retains preferences','lossy rollback blocked with byte-preserved data and pointers','lossy downgrade install blocked','actual installed legacy launch blocked','compatible rollback after explicit fixture settings restore','workspace bytes retained');Scope='Disposable install and windows-free no-recovery launch; no account startup changes'} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $run 'public-upgrade-result.json') -Encoding UTF8
    Write-Output ('PASS: public upgrade and guarded rollback. Evidence: '+(Join-Path $run 'public-upgrade-result.json'))
}catch{[IO.File]::WriteAllText((Join-Path $run 'public-upgrade-error.txt'),$_.ToString());throw}
