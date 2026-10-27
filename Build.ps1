param([switch]$Test,[switch]$Integration,[switch]$HostedIntegration,[string]$HostedSourceRoot)
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'The Windows .NET Framework C# compiler was not found.'
}
$output = Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$executable = Join-Path $output 'SixSlotSnap.exe'
$sources = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | ForEach-Object { $_.FullName })
& $compiler /nologo /target:winexe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll /reference:System.Xml.dll /reference:System.Xml.Linq.dll "/out:$executable" $sources
if ($LASTEXITCODE -ne 0) { throw 'Build failed. If the executable is running, exit it from the tray menu first.' }
Write-Host "Built $executable"
if ($Test) {
    $testExecutable = Join-Path $output 'GridTests.exe'
    & $compiler /nologo /target:exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll "/reference:$executable" "/out:$testExecutable" (Join-Path $PSScriptRoot 'tests\GridTests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
    & $testExecutable
    if ($LASTEXITCODE -ne 0) { throw 'Grid tests failed.' }
}
if ($Integration -or $HostedIntegration) {
    $integrationExecutable = Join-Path $output 'IntegrationTests.exe'
    & $compiler /nologo /target:exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll /reference:System.Xml.dll /reference:System.Xml.Linq.dll "/reference:$executable" "/out:$integrationExecutable" (Join-Path $PSScriptRoot 'tests\IntegrationTests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Integration test compilation failed.' }
    if ($Integration) {
        & $integrationExecutable
        if ($LASTEXITCODE -ne 0) { throw 'Integration tests failed; inspect bin/integration-data.' }
    }
    if ($HostedIntegration) {
        if ([string]::IsNullOrWhiteSpace($HostedSourceRoot)) { $HostedSourceRoot = $PSScriptRoot }
        $hostScript = Join-Path ([IO.Path]::GetFullPath($HostedSourceRoot)) 'Start-Hosted.ps1'
        if (-not (Test-Path -LiteralPath $hostScript)) { throw 'Hosted source folder does not contain Start-Hosted.ps1.' }
        & $integrationExecutable (Join-Path $output 'integration-hosted-data') --hosted $hostScript
        if ($LASTEXITCODE -ne 0) { throw 'Hosted integration tests failed; inspect bin/integration-hosted-data.' }
    }
}

