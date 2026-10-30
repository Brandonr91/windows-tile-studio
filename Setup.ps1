[CmdletBinding()]
param([string]$ArchivePath,[scriptblock]$Feedback)
$ErrorActionPreference='Stop'
if($PSVersionTable.PSEdition -ne 'Desktop' -or [Threading.Thread]::CurrentThread.GetApartmentState() -ne 'STA'){throw 'Use Setup.cmd (Windows PowerShell 5.1 STA).'}
. (Join-Path $PSScriptRoot 'InstallationSupport.ps1')
Add-Type -AssemblyName System.Windows.Forms,System.Drawing,System.IO.Compression.FileSystem
function Show-SetupMessage([string]$Message,[string]$Title,[string]$Icon){if($Feedback){& $Feedback $Message $Title}else{[void][Windows.Forms.MessageBox]::Show($form,$Message,$Title,'OK',$Icon)}}
$form=New-Object Windows.Forms.Form
$form.Text='Windows Tile Studio - setup';$form.ClientSize=New-Object Drawing.Size(650,480);$form.StartPosition='CenterScreen';$form.Font=New-Object Drawing.Font('Segoe UI',10);$form.FormBorderStyle='FixedDialog';$form.MaximizeBox=$false
function Label([string]$Text,[int]$Y,[int]$Height=30){$label=New-Object Windows.Forms.Label;$label.Text=$Text;$label.SetBounds(24,$Y,600,$Height);$form.Controls.Add($label)}
Label 'Install or update Windows Tile Studio' 20 36
Label 'Local installation for your account. No administrator access or startup changes.' 62 46
Label 'Installation folder' 116
$folder=New-Object Windows.Forms.TextBox;$folder.Text=Join-Path $env:LOCALAPPDATA 'WindowsTileStudio';$folder.SetBounds(24,148,510,30);$folder.AccessibleName='Installation folder';$form.Controls.Add($folder)
$browse=New-Object Windows.Forms.Button;$browse.Text='Browse';$browse.SetBounds(542,146,84,32);$form.Controls.Add($browse)
$browse.Add_Click({$picker=New-Object Windows.Forms.FolderBrowserDialog;try{if($picker.ShowDialog($form) -eq 'OK'){$folder.Text=$picker.SelectedPath}}finally{$picker.Dispose()}})
Label 'Delivery mode' 198
$mode=New-Object Windows.Forms.ComboBox;$mode.DropDownStyle='DropDownList';$mode.Items.AddRange(@('Compiled - faster startup, smaller helper footprint','Hosted - source only, no standalone app EXE'));$mode.SelectedIndex=0;$mode.SetBounds(24,230,600,32);$mode.AccessibleName='Delivery mode';$form.Controls.Add($mode)
$menu=New-Object Windows.Forms.CheckBox;$menu.Text='Add a Start menu shortcut (optional)';$menu.Checked=$true;$menu.SetBounds(24,278,600,28);$form.Controls.Add($menu)
$launch=New-Object Windows.Forms.CheckBox;$launch.Text='Launch after installation';$launch.Checked=$true;$launch.SetBounds(24,312,600,28);$form.Controls.Add($launch)
$status=New-Object Windows.Forms.Label;$status.Text='Close a running Tile Studio before updating. Settings are retained.';$status.SetBounds(24,354,600,50);$status.AccessibleName='Setup outcome';$form.Controls.Add($status)
$install=New-Object Windows.Forms.Button;$install.Text='Install / update';$install.SetBounds(424,420,200,36);$form.Controls.Add($install)
$close=New-Object Windows.Forms.Button;$close.Text='Close';$close.SetBounds(304,420,104,36);$close.DialogResult='Cancel';$form.Controls.Add($close);$form.CancelButton=$close
$install.Add_Click({
    $temporary=$null
    try {
        $selectedArchive=$ArchivePath
        if(!$selectedArchive -and !(Test-Path -LiteralPath (Join-Path $PSScriptRoot 'package-manifest.json'))){
            $picker=New-Object Windows.Forms.OpenFileDialog;$picker.Filter='Tile Studio release ZIP (*.zip)|*.zip';$picker.Title='Select the downloaded Tile Studio release'
            try{if($picker.ShowDialog($form) -ne 'OK'){return};$selectedArchive=$picker.FileName}finally{$picker.Dispose()}
        }
        foreach($control in @($install,$folder,$browse,$mode,$menu,$launch,$close)){$control.Enabled=$false};$form.ControlBox=$false;$form.UseWaitCursor=$true;$status.Text='Verifying package and preparing the selected mode...';[Windows.Forms.Application]::DoEvents()
        $root=[IO.Path]::GetFullPath($folder.Text).TrimEnd('\','/');Assert-WtsNoRunningInstall $root
        if(!$selectedArchive){$temporary=Join-Path ([IO.Path]::GetTempPath()) ('WtsSetup-'+[Guid]::NewGuid().ToString('N')+'.zip');New-WtsSetupArchive $PSScriptRoot $temporary;$selectedArchive=$temporary}
        $selectedMode=if($mode.SelectedIndex -eq 0){'Compiled'}else{'Hosted'}
        $result=& (Join-Path $PSScriptRoot 'Get-FromGitHub.ps1') -ArchivePath $selectedArchive -ExpectedSha256 (Get-FileHash -LiteralPath $selectedArchive).Hash -InstallRoot $root -Mode $selectedMode -NoLaunch
        $status.Text='Installed. Settings retained. Start.cmd, Rollback.cmd and Uninstall.cmd are in the selected folder.'
        if($menu.Checked){Set-WtsMenuLink $root ([Environment]::GetFolderPath('Programs'))}
        if($launch.Checked){& (Join-Path $root 'Launch-Installed.ps1') | Out-Null}
        Show-SetupMessage $status.Text 'Installation complete' 'Information'
    } catch {$status.Text=$_.Exception.Message;Show-SetupMessage $_.Exception.Message 'Setup could not complete' 'Warning'}
    finally{if($temporary -and [IO.File]::Exists($temporary)){[IO.File]::Delete($temporary)};foreach($control in @($install,$folder,$browse,$mode,$menu,$launch,$close)){$control.Enabled=$true};$form.ControlBox=$true;$form.UseWaitCursor=$false}
})
try{[void]$form.ShowDialog()}finally{$form.Dispose()}
