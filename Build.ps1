param([switch]$Test,[switch]$Integration,[switch]$HostedIntegration,[switch]$Compatibility,[switch]$StartupRecovery,[switch]$PhaseOneNative,[switch]$PlacementRecovery,[switch]$HostedPlacementRecovery,[switch]$LauncherUi,[switch]$EditorUi,[switch]$WorkspaceUi,[switch]$WorkspaceIntegration,[switch]$AccessibilityUi,[ValidateRange(0,200)][int]$BetaSoak=0,[string]$HostedSourceRoot,[string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'The Windows .NET Framework C# compiler was not found.'
}
$output = if ([string]::IsNullOrWhiteSpace($OutputDirectory)) { Join-Path $PSScriptRoot 'bin' } else { [IO.Path]::GetFullPath($OutputDirectory) }
$executable = Join-Path $output 'SixSlotSnap.exe'
$sources = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | ForEach-Object { $_.FullName })
$version=Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'release\version.json') | ConvertFrom-Json
if($version.version -notmatch '^\d+\.\d+\.\d+(?:-(?:preview|beta|rc)\.\d+)?$' -or $version.channel -notin @('preview','beta','rc','stable') -or $version.assemblyVersion -notmatch '^\d+\.\d+\.\d+\.\d+$' -or $version.fileVersion -notmatch '^\d+\.\d+\.\d+\.\d+$' -or $version.product -ne 'Windows Tile Studio'){throw 'Invalid release version metadata.'}
if(!([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'src\SixSlotSnap.cs')).Contains('ProductVersion="'+$version.version+'"'))){throw 'Application and release version metadata disagree.'}
$longestOutput=Join-Path $output 'AssemblyInfo.generated.cs'
if($longestOutput.Length -ge 260){throw 'The output exceeds the legacy Framework file-path limit. Choose a shorter installation/output folder.'}
$stageParent=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')
if($stageParent.Length -gt 120){throw 'The temporary folder is too deep for the Framework compiler. Set TEMP and TMP to a shorter writable folder, then retry.'}
$stage=Join-Path $stageParent ('WtsBuild-'+[Guid]::NewGuid().ToString('N').Substring(0,12))
New-Item -ItemType Directory -Path $stage | Out-Null
$oldTemp=$env:TEMP;$oldTmp=$env:TMP
try {
$env:TEMP=$stage;$env:TMP=$stage
$builtExecutable=Join-Path $stage 'SixSlotSnap.exe'
$stagedSources=@(foreach($file in Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs'){$target=Join-Path $stage $file.Name;Copy-Item -LiteralPath $file.FullName -Destination $target;$target})
$metadata=Join-Path $stage 'AssemblyInfo.generated.cs'
$attributes='using System.Reflection;[assembly:AssemblyTitle("Windows Tile Studio")][assembly:AssemblyProduct("Windows Tile Studio")][assembly:AssemblyDescription("Local window layouts and linked resizing")][assembly:AssemblyVersion("'+$version.assemblyVersion+'")][assembly:AssemblyFileVersion("'+$version.fileVersion+'")][assembly:AssemblyInformationalVersion("'+$version.version+'")][assembly:AssemblyCompany("Windows Tile Studio contributors")]'
[IO.File]::WriteAllText($metadata,$attributes,[Text.UTF8Encoding]::new($false))
$icon=Join-Path $stage 'WindowsTileStudio.ico'
& (Join-Path $PSScriptRoot 'tools\New-AppIcon.ps1') -OutputPath $icon
& $compiler /nologo /target:winexe /reference:Accessibility.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll /reference:System.Xml.dll /reference:System.Xml.Linq.dll "/win32icon:$icon" "/out:$builtExecutable" $stagedSources $metadata
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed. No new executable was published; inspect the compiler diagnostics above.' }
New-Item -ItemType Directory -Force -Path $output | Out-Null
$pending=Join-Path $output ('.wts-'+[Guid]::NewGuid().ToString('N').Substring(0,8)+'.exe')
try {
    [IO.File]::Copy($builtExecutable,$pending,$false)
    if([IO.File]::Exists($executable)){[IO.File]::Replace($pending,$executable,[NullString]::Value)}else{[IO.File]::Move($pending,$executable)}
} catch {throw ('Build publication failed. Close the app or choose a shorter writable output folder. The previous executable was retained. '+$_.Exception.Message)}
finally{if([IO.File]::Exists($pending)){[IO.File]::Delete($pending)}}
Copy-Item -LiteralPath $metadata,$icon -Destination $output -Force
} finally {
    $env:TEMP=$oldTemp;$env:TMP=$oldTmp
    $resolved=[IO.Path]::GetFullPath($stage)
    if(!$resolved.StartsWith($stageParent+'\',[StringComparison]::OrdinalIgnoreCase) -or (Split-Path -Leaf $resolved) -notmatch '^WtsBuild-[0-9a-f]{12}$'){throw 'Unexpected compiler staging cleanup path.'}
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
Write-Host "Built $executable"
$revision='unknown';$dirty='unknown'
if((Test-Path -LiteralPath (Join-Path $PSScriptRoot '.git')) -and (Get-Command git -ErrorAction SilentlyContinue)){$revision=(& git -C $PSScriptRoot rev-parse HEAD);if($LASTEXITCODE -ne 0){throw 'Could not read test source revision.'};$dirty=(@(& git -C $PSScriptRoot status --porcelain).Count -gt 0).ToString().ToLowerInvariant()}
elseif(Test-Path -LiteralPath (Join-Path $PSScriptRoot 'package-manifest.json')){$manifest=Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'package-manifest.json') | ConvertFrom-Json;$revision=[string]$manifest.sourceRevision;$dirty=([bool]$manifest.sourceDirty).ToString().ToLowerInvariant()}
if(!$revision){$revision='unknown'}
$revision=[Security.SecurityElement]::Escape($revision)
[IO.File]::WriteAllText((Join-Path $output 'test-source.xml'),('<TestSource revision="'+$revision+'" dirty="'+$dirty+'" />'),[Text.UTF8Encoding]::new($false))
if ($Test) {
    $testExecutable = Join-Path $output 'GridTests.exe'
    & $compiler /nologo /target:exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll /reference:System.Xml.Linq.dll "/reference:$executable" "/out:$testExecutable" (Join-Path $PSScriptRoot 'tests\GridTests.cs') (Join-Path $PSScriptRoot 'tests\TestRunRecord.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
    & $testExecutable
    if ($LASTEXITCODE -ne 0) { throw 'Grid tests failed.' }
}
if ($PhaseOneNative) {
    $phaseOneTest=Join-Path $output 'PhaseOneNativeTests.exe'
    & $compiler /nologo /target:exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll /reference:System.Xml.Linq.dll "/reference:$executable" "/out:$phaseOneTest" (Join-Path $PSScriptRoot 'tests\PhaseOneNativeTests.cs')
    if($LASTEXITCODE -ne 0){throw 'Phase-1 native fixture compilation failed.'}
    & $phaseOneTest (Join-Path $output 'phase1-native-data')
    if($LASTEXITCODE -ne 0){throw 'Phase-1 native lifecycle checks were not passed. Daily controller must be stopped only in an approved interval.'}
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
    & $compiler /nologo /target:exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll /reference:System.Xml.dll /reference:System.Xml.Linq.dll "/reference:$executable" "/out:$integrationExecutable" (Join-Path $PSScriptRoot 'tests\IntegrationTests.cs') (Join-Path $PSScriptRoot 'tests\TestRunRecord.cs')
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

