param([switch]$Test,[switch]$Integration,[switch]$HostedIntegration,[switch]$Compatibility,[switch]$StartupRecovery,[switch]$PlacementRecovery,[switch]$HostedPlacementRecovery,[switch]$LauncherUi,[switch]$EditorUi,[switch]$WorkspaceUi,[switch]$WorkspaceIntegration,[switch]$AccessibilityUi,[ValidateRange(0,200)][int]$BetaSoak=0,[string]$HostedSourceRoot,[string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'The Windows .NET Framework C# compiler was not found.'
}
$output = if ([string]::IsNullOrWhiteSpace($OutputDirectory)) { Join-Path $PSScriptRoot 'bin' } else { [IO.Path]::GetFullPath($OutputDirectory) }
New-Item -ItemType Directory -Force -Path $output | Out-Null
$executable = Join-Path $output 'SixSlotSnap.exe'
$sources = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | ForEach-Object { $_.FullName })
$version=Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'release\version.json') | ConvertFrom-Json
if($version.version -notmatch '^\d+\.\d+\.\d+(?:-(?:preview|beta|rc)\.\d+)?$' -or $version.channel -notin @('preview','beta','rc','stable') -or $version.assemblyVersion -notmatch '^\d+\.\d+\.\d+\.\d+$' -or $version.fileVersion -notmatch '^\d+\.\d+\.\d+\.\d+$' -or $version.product -ne 'Windows Tile Studio'){throw 'Invalid release version metadata.'}
if(!([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'src\SixSlotSnap.cs')).Contains('ProductVersion="'+$version.version+'"'))){throw 'Application and release version metadata disagree.'}
$metadata=Join-Path $output 'AssemblyInfo.generated.cs'
$attributes='using System.Reflection;[assembly:AssemblyTitle("Windows Tile Studio")][assembly:AssemblyProduct("Windows Tile Studio")][assembly:AssemblyDescription("Local window layouts and linked resizing")][assembly:AssemblyVersion("'+$version.assemblyVersion+'")][assembly:AssemblyFileVersion("'+$version.fileVersion+'")][assembly:AssemblyInformationalVersion("'+$version.version+'")][assembly:AssemblyCompany("Windows Tile Studio contributors")]'
[IO.File]::WriteAllText($metadata,$attributes,[Text.UTF8Encoding]::new($false))
$icon=Join-Path $output 'WindowsTileStudio.ico'
& (Join-Path $PSScriptRoot 'tools\New-AppIcon.ps1') -OutputPath $icon
& $compiler /nologo /target:winexe /reference:Accessibility.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll /reference:System.Xml.dll /reference:System.Xml.Linq.dll "/win32icon:$icon" "/out:$executable" $sources $metadata
if ($LASTEXITCODE -ne 0) { throw 'Build failed. If the executable is running, exit it from the tray menu first.' }
Write-Host "Built $executable"
if ($Test) {
    $testExecutable = Join-Path $output 'GridTests.exe'
    & $compiler /nologo /target:exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll "/reference:$executable" "/out:$testExecutable" (Join-Path $PSScriptRoot 'tests\GridTests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
    & $testExecutable
    if ($LASTEXITCODE -ne 0) { throw 'Grid tests failed.' }
}
if ($LauncherUi) {
    $launcherTest = Join-Path $output 'LauncherUiTests.exe'
    & $compiler /nologo /target:exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll "/reference:$executable" "/out:$launcherTest" (Join-Path $PSScriptRoot 'tests\LauncherUiTests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Launcher UI test compilation failed.' }
    & $launcherTest
    if ($LASTEXITCODE -ne 0) { throw 'Launcher UI paint/DPI tests failed.' }
}
if ($EditorUi) {
    $editorTest = Join-Path $output 'EditorUiTests.exe'
    & $compiler /nologo /target:exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll "/reference:$executable" "/out:$editorTest" (Join-Path $PSScriptRoot 'tests\EditorUiTests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Editor UI test compilation failed.' }
    & $editorTest (Join-Path $output 'editor-ui-data')
    if ($LASTEXITCODE -ne 0) { throw 'Editor UI tests failed.' }
}
if ($WorkspaceUi) {
    $workspaceTest = Join-Path $output 'WorkspaceUiTests.exe'
    & $compiler /nologo /target:exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll "/reference:$executable" "/out:$workspaceTest" (Join-Path $PSScriptRoot 'tests\WorkspaceUiTests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Workspace UI test compilation failed.' }
    & $workspaceTest (Join-Path $output 'workspace-ui-data')
    if ($LASTEXITCODE -ne 0) { throw 'Workspace UI tests failed.' }
}
if ($AccessibilityUi) {
    $accessibilityTest = Join-Path $output 'AccessibilityUiTests.exe'
    & $compiler /nologo /target:exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll /reference:System.Xml.Linq.dll "/reference:$executable" "/out:$accessibilityTest" (Join-Path $PSScriptRoot 'tests\AccessibilityUiTests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Accessibility UI test compilation failed.' }
    & $accessibilityTest (Join-Path $output 'accessibility-ui-data')
    if ($LASTEXITCODE -ne 0) { throw 'Accessibility UI tests failed.' }
}
if ($WorkspaceIntegration) {
    $nativeTest = Join-Path $output 'WorkspaceNativeTests.exe'
    & $compiler /nologo /target:exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll "/reference:$executable" "/out:$nativeTest" (Join-Path $PSScriptRoot 'tests\WorkspaceNativeTests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Workspace native test compilation failed.' }
    & $compiler /nologo /target:winexe ('/out:'+(Join-Path $output 'WorkspaceLaunchProbe.exe')) (Join-Path $PSScriptRoot 'tests\WorkspaceLaunchProbe.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Workspace launch probe compilation failed.' }
    & $nativeTest
    if ($LASTEXITCODE -ne 0) { throw 'Workspace native tests failed.' }
    & $nativeTest --hidden-dialogs
    if ($LASTEXITCODE -ne 0) { throw 'Hidden-startup dialog tests failed.' }
}
if ($PlacementRecovery -or $HostedPlacementRecovery) {
    $raceTest=Join-Path $output 'PlacementRecoveryTests.exe'
    & $compiler /nologo /target:exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll /reference:System.Xml.Linq.dll "/reference:$executable" "/out:$raceTest" (Join-Path $PSScriptRoot 'tests\PlacementRecoveryTests.cs')
    if($LASTEXITCODE -ne 0){throw 'Placement race test compilation failed.'}
    if($PlacementRecovery){
        foreach($phase in @('query','changing','changed','restore')){& $raceTest (Join-Path $output ('placement-'+$phase)) --phase $phase;if($LASTEXITCODE -ne 0){throw ('Placement recovery failed: '+$phase)}}
        & $raceTest (Join-Path $output 'placement-linked-resize') --resize-race
        if($LASTEXITCODE -ne 0){throw 'Linked placement recovery failed.'}
        & $raceTest (Join-Path $output 'placement-parent-exit') --phase changing --crash-parent
        if($LASTEXITCODE -ne 0){throw 'Placement parent-exit cleanup failed.'}
    }
    if($HostedPlacementRecovery){
        $raceSource=if($HostedSourceRoot){[IO.Path]::GetFullPath($HostedSourceRoot)}else{$PSScriptRoot}
        foreach($phase in @('query','changing','changed','restore')){& $raceTest (Join-Path $output ('hosted-placement-'+$phase)) --phase $phase --hosted (Join-Path $raceSource 'Start-Hosted.ps1');if($LASTEXITCODE -ne 0){throw ('Hosted placement recovery failed: '+$phase)}}
        & $raceTest (Join-Path $output 'hosted-placement-linked-resize') --resize-race --hosted (Join-Path $raceSource 'Start-Hosted.ps1')
        if($LASTEXITCODE -ne 0){throw 'Hosted linked placement recovery failed.'}
        & $raceTest (Join-Path $output 'hosted-placement-parent-exit') --phase changing --crash-parent --hosted (Join-Path $raceSource 'Start-Hosted.ps1')
        if($LASTEXITCODE -ne 0){throw 'Hosted placement parent-exit cleanup failed.'}
    }
}
if ($Integration -or $HostedIntegration -or $Compatibility -or $StartupRecovery -or $BetaSoak) {
    $integrationExecutable = Join-Path $output 'IntegrationTests.exe'
    & $compiler /nologo /target:exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll /reference:System.Xml.dll /reference:System.Xml.Linq.dll "/reference:$executable" "/out:$integrationExecutable" (Join-Path $PSScriptRoot 'tests\IntegrationTests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Integration test compilation failed.' }
    if ($BetaSoak) {
        if($BetaSoak -lt 10){throw 'Beta soak requires 10–200 cycles.'}
        & $integrationExecutable (Join-Path $output 'beta-soak-compiled') --beta-soak $BetaSoak
        if($LASTEXITCODE -ne 0){throw 'Compiled beta soak failed.'}
        $soakSource=if($HostedSourceRoot){[IO.Path]::GetFullPath($HostedSourceRoot)}else{$PSScriptRoot}
        & $integrationExecutable (Join-Path $output 'beta-soak-hosted') --beta-soak $BetaSoak --hosted (Join-Path $soakSource 'Start-Hosted.ps1')
        if($LASTEXITCODE -ne 0){throw 'Source-hosted beta soak failed.'}
    }
    if ($Integration) {
        & $integrationExecutable
        if ($LASTEXITCODE -ne 0) { throw 'Integration tests failed; inspect bin/integration-data.' }
    }
    if ($StartupRecovery) {
        & $integrationExecutable (Join-Path $output 'startup-recovery-data') --startup-recovery-only
        if ($LASTEXITCODE -ne 0) { throw 'Startup recovery native checks failed.' }
    }
    if ($Compatibility) {
        & $integrationExecutable (Join-Path $output 'compatibility-data') --compatibility-only
        if ($LASTEXITCODE -ne 0) { throw 'Compatibility tests failed; inspect bin/compatibility-data.' }
    }
    if ($HostedIntegration) {
        if ([string]::IsNullOrWhiteSpace($HostedSourceRoot)) { $HostedSourceRoot = $PSScriptRoot }
        $hostScript = Join-Path ([IO.Path]::GetFullPath($HostedSourceRoot)) 'Start-Hosted.ps1'
        if (-not (Test-Path -LiteralPath $hostScript)) { throw 'Hosted source folder does not contain Start-Hosted.ps1.' }
        & $integrationExecutable (Join-Path $output 'integration-hosted-data') --hosted $hostScript
        if ($LASTEXITCODE -ne 0) { throw 'Hosted integration tests failed; inspect bin/integration-hosted-data.' }
    }
}

