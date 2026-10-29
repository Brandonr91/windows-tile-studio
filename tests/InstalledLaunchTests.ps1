param([Parameter(Mandatory=$true)][string]$PackageDirectory,[string]$WorkDirectory)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
. (Join-Path $root 'InstallationSupport.ps1')
if(@(Get-CimInstance Win32_Process | Where-Object {$_.Name -eq 'SixSlotSnap.exe' -or ($_.Name -eq 'powershell.exe' -and $_.CommandLine -like '*Start-Hosted.ps1*')}).Count){throw 'Stop the daily controller before native installed-launch checks.'}
if(!$WorkDirectory){$WorkDirectory=Join-Path $env:TEMP 'WtsInstalledTests'}
$run=Join-Path ([IO.Path]::GetFullPath($WorkDirectory)) ([Guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Path $run -Force | Out-Null
$install=Join-Path $run "App's folder";$startup=Join-Path $run 'startup';New-Item -ItemType Directory -Path $startup | Out-Null
$info=Get-Content -Raw -LiteralPath (Join-Path $PackageDirectory 'release-info.json') | ConvertFrom-Json
$hostExe=Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe'
Add-Type 'using System;using System.Runtime.InteropServices;public static class WtsInstalledNative{[DllImport("user32.dll")]public static extern bool PostMessage(IntPtr h,int m,IntPtr w,IntPtr l);[DllImport("user32.dll")]public static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);}'
function Assert([bool]$Value,[string]$Message){if(!$Value){throw $Message}}
function Install([string]$Kind,[string]$Mode){$artifact=$info.artifacts | Where-Object {$_.kind -eq $Kind};& (Join-Path $root 'Get-FromGitHub.ps1') -ArchivePath (Join-Path $PackageDirectory $artifact.file) -ExpectedSha256 $artifact.sha256 -InstallRoot $install -Mode $Mode -NoLaunch}
function Smoke([string]$Mode){
    $runtime=Join-Path $install 'data\runtime-state.xml';$app=$null;$receiver=[IntPtr]::Zero
    if(Test-Path -LiteralPath $runtime){[void](Assert-WtsChild $install $runtime);Remove-Item -LiteralPath $runtime}
    $wrapper=Start-Process -FilePath $hostExe -ArgumentList ('-NoProfile -STA -ExecutionPolicy Bypass -File "'+(Join-Path $install 'Launch-Installed.ps1')+'" -NoRecover') -WindowStyle Hidden -PassThru
    try{
        $until=(Get-Date).AddSeconds(20);$state=$null
        do{try{if(Test-Path -LiteralPath $runtime){[xml]$state=Get-Content -Raw -LiteralPath $runtime}}catch [IO.IOException]{}catch [Xml.XmlException]{};if(!$state){Start-Sleep -Milliseconds 50}}while(!$state -and (Get-Date) -lt $until)
        Assert ($null -ne $state -and $state.Runtime.version -eq $info.version) 'Installed launcher did not start the expected version.'
        $app=Get-Process -Id ([int]$state.Runtime.pid);$process=Get-CimInstance Win32_Process -Filter ('ProcessId='+$app.Id)
        Assert ($process.CommandLine.Contains($install+'\versions\')) 'Installed runtime came from another source location.'
        Assert (($Mode -eq 'Hosted' -and $process.Name -eq 'powershell.exe') -or ($Mode -eq 'Compiled' -and $process.Name -eq 'SixSlotSnap.exe')) 'Installed launcher selected the wrong mode.'
        Assert ($state.Runtime.launcherVisible -eq 'false' -and @($state.Runtime.SelectNodes('.//Zone')).Count -eq 0) 'Installed smoke showed a bar or adopted real windows.'
        $receiver=[IntPtr]([long]$state.Runtime.controller);[uint32]$owner=0;[void][WtsInstalledNative]::GetWindowThreadProcessId($receiver,[ref]$owner);Assert ($owner -eq $app.Id) 'Controller receiver ownership changed.'
        $refused=$false;try{& (Join-Path $root 'Manage-Installation.ps1') -InstallRoot $install -Action Uninstall -StartupDirectory $startup -Confirm:$false | Out-Null}catch{$refused=$_.Exception.Message.Contains('Exit this installation')}
        Assert ($refused -and !$app.HasExited) 'Management did not refuse uninstall of its running installation.'
    }finally{
        if(!$app){$owned=@(Get-CimInstance Win32_Process | Where-Object {($_.Name -eq 'SixSlotSnap.exe' -and $_.ExecutablePath -and $_.ExecutablePath.StartsWith($install+'\versions\',[StringComparison]::OrdinalIgnoreCase)) -or ($_.Name -eq 'powershell.exe' -and $_.CommandLine -and $_.CommandLine.Contains($install+'\versions\'))});if($owned.Count -eq 1){$app=Get-Process -Id $owned[0].ProcessId;if(Test-Path -LiteralPath $runtime){[xml]$last=Get-Content -Raw -LiteralPath $runtime;if([int]$last.Runtime.pid -eq $app.Id){$receiver=[IntPtr]([long]$last.Runtime.controller)}}}}
        if($app -and !$app.HasExited){[void][WtsInstalledNative]::PostMessage($receiver,0x8003,[IntPtr]::Zero,[IntPtr]::Zero);if(!$app.WaitForExit(5000)){$app.Kill();throw 'Isolated installed controller did not exit normally.'}}
        if(!$wrapper.HasExited){if(!$wrapper.WaitForExit(5000)){$wrapper.Kill()}};$wrapper.Dispose();if($app){$app.Dispose()}
    }
}
try{
    $first=Install 'Source' 'Hosted'
    & $hostExe -NoProfile -STA -ExecutionPolicy Bypass -File (Join-Path $first.VersionDirectory 'Start-Hosted.ps1') -RunInProcess -Action Validate -DataDirectory $first.DataDirectory | Out-Null
    if($LASTEXITCODE -ne 0){throw 'Isolated settings template could not be created.'}
    [xml]$settings=Get-Content -Raw -LiteralPath (Join-Path $first.DataDirectory 'host-check.xml');$settings.AppSettings.ShowLauncherTab='false';$settings.AppSettings.FlyoutShortcut.Key='F23';foreach($key in $settings.AppSettings.SlotShortcuts.Shortcut){$key.Key='None'};$settings.Save((Join-Path $first.DataDirectory 'settings.xml'))
    Smoke 'Hosted';$compiled=Install 'PortableCompiled' 'Compiled';Smoke 'Compiled'
    & (Join-Path $root 'Manage-Installation.ps1') -InstallRoot $install -Action Rollback -Confirm:$false;Smoke 'Hosted'
    & (Join-Path $root 'Manage-Installation.ps1') -InstallRoot $install -Action Uninstall -StartupDirectory $startup -Confirm:$false | Out-Null
    Assert (Test-Path -LiteralPath (Join-Path $install 'data\settings.xml')) 'Native uninstall removed retained data.'
    Write-Output 'PASS: actual installed Hosted/Compiled launch, version/mode/path checks, no-recovery/hidden bar, running-install refusal, rollback launch and clean exit. Real startup and windows were untouched.'
    Write-Output ('Result directory: '+$run)
}catch{[IO.File]::WriteAllText((Join-Path $run 'native-install-error.txt'),$_.ToString());throw}
