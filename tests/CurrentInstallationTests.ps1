param(
    [Parameter(Mandatory=$true)][string]$InstallationRoot,
    [ValidateRange(30,600)][int]$ObservationSeconds=180,
    [switch]$ExerciseStartup
)
$ErrorActionPreference='Stop'
if($PSVersionTable.PSVersion.Major -ne 5 -or [Threading.Thread]::CurrentThread.GetApartmentState() -ne 'STA'){throw 'Use Windows PowerShell 5.1 -STA.'}
$root=[IO.Path]::GetFullPath($InstallationRoot)
. (Join-Path $root 'InstallationSupport.ps1')
Assert-WtsNoLinks $root
$exe=Assert-WtsChild $root (Join-Path $root 'bin\SixSlotSnap.exe')
$data=Assert-WtsChild $root (Join-Path $root 'data')
$run=Assert-WtsChild $root (Join-Path $root ('bin\current-installation-'+[Guid]::NewGuid().ToString('N')))
New-Item -ItemType Directory -Path $run | Out-Null
Add-Type -AssemblyName System.Windows.Forms,System.Drawing
[void][Reflection.Assembly]::LoadFrom($exe)
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class WtsCurrentNative {
 [DllImport("user32.dll")]public static extern bool PostMessage(IntPtr h,int m,IntPtr w,IntPtr l);
 [DllImport("user32.dll")]public static extern uint GetWindowThreadProcessId(IntPtr h,out uint p);
 [DllImport("user32.dll")]public static extern uint GetGuiResources(IntPtr p,uint flags);
 [DllImport("user32.dll",SetLastError=true)]public static extern IntPtr SendMessageTimeout(IntPtr h,uint m,IntPtr w,IntPtr l,uint flags,uint timeout,out IntPtr result);
}
'@
function Assert([bool]$Value,[string]$Message){if(!$Value){throw $Message}}
function Hash([string]$Path){return (Get-FileHash -LiteralPath $Path).Hash}
function Read-State {
    $path=Join-Path $data 'runtime-state.xml'
    $options=New-Object Xml.XmlReaderSettings;$options.DtdProcessing=[Xml.DtdProcessing]::Prohibit;$options.XmlResolver=$null;$options.MaxCharactersInDocument=1048576
    $reader=[Xml.XmlReader]::Create($path,$options)
    try{$xml=New-Object Xml.XmlDocument;$xml.XmlResolver=$null;$xml.Load($reader);return $xml}finally{$reader.Dispose()}
}
function Get-Controller {
    $state=Read-State
    $process=Get-CimInstance Win32_Process -Filter ('ProcessId='+[int]$state.Runtime.pid)
    Assert ($null -ne $process -and $process.ExecutablePath -eq $exe -and $process.CommandLine.Contains('"'+$data+'"')) 'Runtime snapshot does not identify this live compiled installation.'
    [uint32]$owner=0;[void][WtsCurrentNative]::GetWindowThreadProcessId([IntPtr]([long]$state.Runtime.controller),[ref]$owner)
    Assert ($owner -eq $process.ProcessId) 'Controller receiver ownership differs from the live process.'
    return $state
}
function Wait-Restart([int]$OldId){
    $deadline=(Get-Date).AddSeconds(20)
    do{try{$state=Get-Controller;if([int]$state.Runtime.pid -ne $OldId){return $state}}catch{};Start-Sleep -Milliseconds 100}while((Get-Date) -lt $deadline)
    throw 'The startup shortcut did not start this installation within 20 seconds.'
}
$started=Get-Date;$restarted=$false;$stopped=$false;$samples=@()
try{
    $initial=Get-Controller;$initialId=[int]$initial.Runtime.pid
    $settings=Join-Path $data 'settings.xml';$settingsHash=Hash $settings
    $validated=@()
    foreach($name in @('settings.xml','settings.xml.bak','workspaces.xml','workspaces.xml.bak')){
        $file=Assert-WtsChild $data (Join-Path $data $name)
        if(!(Test-Path -LiteralPath $file)){continue}
        Assert ((Get-Item -LiteralPath $file).Length -le 1048576) 'Configuration snapshot exceeds its size limit.'
        $copy=Join-Path $run $name;Copy-Item -LiteralPath $file -Destination $copy
        if($name.StartsWith('settings')){$warning=$null;$loaded=[SettingsStore]::Load($copy,[ref]$warning);Assert (!$warning) 'A saved settings snapshot failed validation.';$count=$loaded.Profiles.Count}
        else{$loaded=[WorkspaceStore]::Load($copy);$count=$loaded.Workspaces.Count}
        $validated+=[pscustomobject]@{File=$name;Valid=$true;Items=$count;OriginalBytePreserved=((Hash $file) -eq (Hash $copy))}
    }
    $shortcut=Join-Path ([Environment]::GetFolderPath('Startup')) 'Windows Tile Studio.lnk'
    $shortcutHash=if(Test-Path -LiteralPath $shortcut){Hash $shortcut}else{$null}
    if($ExerciseStartup){
        Assert ($null -ne $shortcutHash) 'This installation has no existing startup shortcut; none was created.'
        $shell=$null;$link=$null
        try{
            $shell=New-Object -ComObject WScript.Shell;$link=$shell.CreateShortcut($shortcut)
            Assert ($link.TargetPath -eq $exe -and $link.Arguments.Contains('"'+$data+'"') -and $link.Arguments.EndsWith('--startup-owner WindowsTileStudio')) 'Startup points elsewhere; the existing entry was preserved.'
            Copy-Item -LiteralPath $shortcut -Destination (Join-Path $run 'Startup-shortcut.lnk')
        }finally{if($link){[void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($link)};if($shell){[void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($shell)}}
        $old=Get-Process -Id $initialId
        Assert ([WtsCurrentNative]::PostMessage([IntPtr]([long]$initial.Runtime.controller),0x8003,[IntPtr]::Zero,[IntPtr]::Zero)) 'The controller could not receive its normal quit command.'
        Assert ($old.WaitForExit(5000)) 'The daily controller did not quit normally; it was not killed.'
        $stopped=$true
        $clock=[Diagnostics.Stopwatch]::StartNew()
        Start-Process -FilePath $shortcut -WindowStyle Hidden
        $state=Wait-Restart $initialId;$startupMs=$clock.ElapsedMilliseconds;$stopped=$false;$restarted=$true
        Assert ($state.Runtime.version -eq $initial.Runtime.version -and $state.Runtime.recoveryRequired -eq 'false' -and $state.Runtime.launcherVisible -eq $initial.Runtime.launcherVisible) 'Startup changed version, entered Recovery or changed bar visibility.'
        Assert ((Hash $settings) -eq $settingsHash -and (Hash $shortcut) -eq $shortcutHash) 'Shortcut execution changed saved settings or the Startup entry.'
        Write-Output ('Existing Startup shortcut launch PASS ('+$startupMs+' ms); this was not logout/login.')
    }else{$state=$initial;$startupMs=$null}
    $controllerId=[int]$state.Runtime.pid;$process=Get-Process -Id $controllerId;$startTime=$process.StartTime
    $watch=[Diagnostics.Stopwatch]::StartNew()
    do{
        $state=Get-Controller;Assert ([int]$state.Runtime.pid -eq $controllerId) 'The observed controller changed.'
        $process.Refresh();Assert (!$process.HasExited -and $process.StartTime -eq $startTime) 'The observed process exited or was replaced.'
        [IntPtr]$reply=[IntPtr]::Zero
        $responding=[WtsCurrentNative]::SendMessageTimeout([IntPtr]([long]$state.Runtime.controller),0,[IntPtr]::Zero,[IntPtr]::Zero,3,500,[ref]$reply) -ne [IntPtr]::Zero
        Assert $responding 'The controller did not answer its bounded heartbeat.'
        $samples+=[pscustomobject]@{ElapsedMs=$watch.ElapsedMilliseconds;CpuMs=$process.TotalProcessorTime.TotalMilliseconds;PrivateBytes=$process.PrivateMemorySize64;WorkingSetBytes=$process.WorkingSet64;GdiObjects=[WtsCurrentNative]::GetGuiResources($process.Handle,0);Handles=$process.HandleCount;Assignments=@($state.Runtime.SelectNodes('./Zone')).Count;Recovery=($state.Runtime.recoveryRequired -eq 'true');Responsive=$responding}
        if($watch.Elapsed.TotalSeconds -ge $ObservationSeconds){break}
        Start-Sleep -Seconds 5
    }while($true)
    $events=@(Get-WinEvent -FilterHashtable @{LogName='Application';Id=1000,1001,1026;StartTime=$started} -ErrorAction SilentlyContinue | Where-Object {$_.Message -match 'SixSlotSnap|TileStudio'})
    $first=$samples[0];$last=$samples[-1]
    $result=[ordered]@{Version=$state.Runtime.version;Scope='Current compiled installation; no logout/login, app snapping, startup registration or configuration edits';StartupShortcutExecuted=$restarted;StartupMilliseconds=$startupMs;ValidatedSnapshots=$validated;WorkspacePresent=(Test-Path -LiteralPath (Join-Path $data 'workspaces.xml'));SettingsBytePreserved=((Hash $settings) -eq $settingsHash);StartupBytePreserved=(!$shortcutHash -or (Hash $shortcut) -eq $shortcutHash);ObservationSeconds=($last.ElapsedMs/1000.0);CpuMilliseconds=($last.CpuMs-$first.CpuMs);PrivateGrowthBytes=($last.PrivateBytes-$first.PrivateBytes);GdiGrowth=($last.GdiObjects-$first.GdiObjects);HeartbeatFailures=@($samples | Where-Object {!$_.Responsive}).Count;RecoverySamples=@($samples | Where-Object {$_.Recovery}).Count;SampledCrashEvents=$events.Count;Samples=$samples}
    [IO.File]::WriteAllText((Join-Path $run 'current-installation-result.json'),($result | ConvertTo-Json -Depth 7),[Text.UTF8Encoding]::new($false))
    Write-Output ('Current-installation observation complete: '+(Join-Path $run 'current-installation-result.json'))
}finally{
    if($stopped){
        # A failed shortcut launch must not leave the daily app intentionally stopped.
        try{$null=Get-Controller}catch{Start-Process -FilePath $exe -ArgumentList ('--data-dir "'+$data+'"') -WindowStyle Hidden;Write-Warning 'Startup test failed; the daily app was restarted directly.'}
    }
}
