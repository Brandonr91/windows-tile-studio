param([Parameter(Mandatory=$true)][string]$SourceRoot,[string]$OutputPath,[int]$Samples=3)
$ErrorActionPreference='Stop'
if ($Samples -lt 1 -or $Samples -gt 5) {throw 'Use 1-5 samples.'}
$SourceRoot=[IO.Path]::GetFullPath($SourceRoot)
if (@(Get-CimInstance Win32_Process | Where-Object {$_.Name -eq 'SixSlotSnap.exe' -or ($_.Name -eq 'powershell.exe' -and $_.CommandLine -like '*Start-Hosted.ps1*')}).Count) {throw 'Stop the daily controller before isolated startup measurements.'}
$windowsHost=Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe'
& $windowsHost -NoProfile -ExecutionPolicy Bypass -File (Join-Path $SourceRoot 'Build.ps1')
if ($LASTEXITCODE -ne 0) {throw 'Packaged source build failed.'}
$executable=Join-Path $SourceRoot 'bin\SixSlotSnap.exe'
$script=Join-Path $SourceRoot 'Start-Hosted.ps1'
$run=Join-Path $SourceRoot ('bin\performance-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $run | Out-Null
$template=Join-Path $run 'template'
& $windowsHost -NoProfile -STA -ExecutionPolicy Bypass -File $script -RunInProcess -Action Validate -DataDirectory $template | Out-Null
if ($LASTEXITCODE -ne 0) {throw 'Packaged hosted validation failed.'}
[xml]$configuration=Get-Content -Raw -LiteralPath (Join-Path $template 'host-check.xml')
$configuration.AppSettings.ShowLauncherTab='false'
$configuration.AppSettings.FlyoutShortcut.Key='F23'
foreach($shortcut in $configuration.AppSettings.SlotShortcuts.Shortcut) {$shortcut.Key='None'}
$results=@()
foreach($mode in @('Compiled','SourceHosted')) {
    for($sample=1;$sample -le $Samples;$sample++) {
        $data=Join-Path $run ($mode+'-'+$sample);New-Item -ItemType Directory -Path $data | Out-Null
        $configuration.Save((Join-Path $data 'settings.xml'))
        $file=if($mode -eq 'Compiled'){$executable}else{$windowsHost}
        $arguments=if($mode -eq 'Compiled'){'--no-recover --data-dir "'+$data+'"'}else{'-NoProfile -STA -ExecutionPolicy Bypass -File "'+$script+'" -RunInProcess -NoRecover -DataDirectory "'+$data+'"'}
        $clock=[Diagnostics.Stopwatch]::StartNew()
        $process=Start-Process -FilePath $file -ArgumentList $arguments -WindowStyle Hidden -PassThru
        try {
            $runtime=Join-Path $data 'runtime-state.xml';$ready=$false
            while(!$ready -and $clock.Elapsed.TotalSeconds -lt 20) {
                if($process.HasExited){throw 'Isolated controller exited before readiness.'}
                try{if(Test-Path -LiteralPath $runtime){[xml]$state=Get-Content -Raw -LiteralPath $runtime;$ready=([int]$state.Runtime.pid -eq $process.Id)}}catch [IO.IOException]{}catch [Xml.XmlException]{}
                if(!$ready){Start-Sleep -Milliseconds 20}
            }
            if(!$ready){throw 'Isolated controller readiness timed out.'}
            $startupMs=$clock.ElapsedMilliseconds;$process.Refresh();$cpu=$process.TotalProcessorTime.TotalMilliseconds
            Start-Sleep -Milliseconds 1000
            $process.Refresh()
            $results+=[pscustomobject]@{Mode=$mode;Sample=$sample;StartupMilliseconds=$startupMs;IdleIntervalMilliseconds=1000;IdleCpuMilliseconds=($process.TotalProcessorTime.TotalMilliseconds-$cpu);WorkingSetBytes=$process.WorkingSet64;PrivateBytes=$process.PrivateMemorySize64}
        }finally {
            if(!$process.HasExited){Start-Process -FilePath $executable -ArgumentList '--quit' -WindowStyle Hidden -Wait;if(!$process.WaitForExit(5000)){$process.Kill();throw 'Isolated controller did not exit normally.'}}
            $process.Dispose()
        }
    }
}
$json=$results | ConvertTo-Json
if($OutputPath){[IO.File]::WriteAllText([IO.Path]::GetFullPath($OutputPath),$json)}
Write-Output $json
