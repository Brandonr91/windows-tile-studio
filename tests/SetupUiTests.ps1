param([Parameter(Mandatory=$true)][string]$ArchivePath,[string]$WorkDirectory=(Join-Path $env:TEMP 'WtsSetupUi'))
$ErrorActionPreference='Stop'
if($PSVersionTable.PSEdition -ne 'Desktop' -or [Threading.Thread]::CurrentThread.GetApartmentState() -ne 'STA'){throw 'Windows PowerShell 5.1 STA required.'}
Add-Type -AssemblyName System.Windows.Forms,System.Drawing
$repo=Split-Path -Parent $PSScriptRoot
$run=Join-Path ([IO.Path]::GetFullPath($WorkDirectory)) ([Guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Path $run -Force | Out-Null
$testResult=@{Failure=$null;Exercised=$false}
$testTimer=New-Object Windows.Forms.Timer;$testTimer.Interval=150
$testTimer.Add_Tick({
    $setupWindow=@([Windows.Forms.Application]::OpenForms | Where-Object {$_.Text -eq 'Windows Tile Studio - setup'}) | Select-Object -First 1
    if(!$setupWindow){return};$testTimer.Stop()
    try {
        $pathInput=@($setupWindow.Controls | Where-Object {$_.AccessibleName -eq 'Installation folder'})[0];$pathInput.Text=Join-Path $run 'installed'
        foreach($check in @($setupWindow.Controls | Where-Object {$_ -is [Windows.Forms.CheckBox]})){$check.Checked=$false}
        @($setupWindow.Controls | Where-Object {$_.Text -eq 'Install / update'})[0].PerformClick()
        $status=@($setupWindow.Controls | Where-Object {$_.AccessibleName -eq 'Setup outcome'})[0].Text
        if(!$status.StartsWith('Installed.')){throw $status}
        if(!(Test-Path -LiteralPath (Join-Path $pathInput.Text 'current.json'))){throw 'GUI did not install a candidate.'}
        if(!(Test-Path -LiteralPath (Join-Path $pathInput.Text 'Uninstall.cmd'))){throw 'Uninstall entry missing.'}
        $image=New-Object Drawing.Bitmap($setupWindow.Width,$setupWindow.Height);try{$setupWindow.DrawToBitmap($image,[Drawing.Rectangle]::new(0,0,$image.Width,$image.Height));$image.Save((Join-Path $run 'setup.png'))}finally{$image.Dispose()}
        $testResult.Exercised=$true
    } catch {$testResult.Failure=$_}
    finally{$setupWindow.Close()}
})
try{$testTimer.Start();& (Join-Path $repo 'Setup.ps1') -ArchivePath ([IO.Path]::GetFullPath($ArchivePath)) -Feedback {param($message,$title)}}finally{$testTimer.Stop();$testTimer.Dispose()}
if($testResult.Failure){throw $testResult.Failure};if(!$testResult.Exercised){throw 'Setup GUI fixture did not execute.'}
. (Join-Path $repo 'InstallationSupport.ps1')
Test-WtsInstalledPointer (Join-Path $run 'installed') (Read-WtsPointer (Join-Path $run 'installed\current.json'))
Write-Output ('PASS: setup GUI installs/verifies compiled mode from a local archive with launch/Start menu disabled. Result: '+$run)
