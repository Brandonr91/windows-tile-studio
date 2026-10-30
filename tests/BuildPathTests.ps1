param([string]$WorkDirectory=(Join-Path $env:TEMP 'WtsPathChecks'))
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$run=Join-Path ([IO.Path]::GetFullPath($WorkDirectory)) ([Guid]::NewGuid().ToString('N').Substring(0,8))
New-Item -ItemType Directory -Path $run -Force | Out-Null
$deep=$run
while($deep.Length -lt 218){$length=[Math]::Min(37,218-$deep.Length-1);if($length -lt 1){break};$deep=Join-Path $deep ('d'*$length)}
New-Item -ItemType Directory -Path $deep -Force | Out-Null
foreach($name in @('src','release','tools','Build.ps1')){Copy-Item -LiteralPath (Join-Path $root $name) -Destination $deep -Recurse}
function Assert([bool]$Value,[string]$Message){if(!$Value){throw $Message}}
function Reject([scriptblock]$Action,[string]$Text){$message=$null;try{& $Action}catch{$message=$_.Exception.Message};Assert ($message -and $message.Contains($Text)) ('Expected rejection: '+$Text);$global:LASTEXITCODE=0}
$tempBefore=$env:TEMP;$tmpBefore=$env:TMP;$script=Join-Path $deep 'Build.ps1';$exe=Join-Path $deep 'bin\SixSlotSnap.exe'
try {
    & $script
    Assert (Test-Path -LiteralPath $exe) 'Deep-path app build failed.'
    & $script
    Assert ($env:TEMP -eq $tempBefore -and $env:TMP -eq $tmpBefore) 'Compiler TEMP/TMP leaked into the caller.'
    $hash=(Get-FileHash -LiteralPath $exe).Hash
    $source=Join-Path $deep 'src\LayoutModel.cs';$bytes=[IO.File]::ReadAllBytes($source)
    try{[IO.File]::AppendAllText($source,"`nINVALID_CSHARP_PATH_FIXTURE");Reject {& $script *> (Join-Path $run 'expected-compile-failure.log')} 'Compilation failed'}finally{[IO.File]::WriteAllBytes($source,$bytes)}
    Assert ((Get-FileHash -LiteralPath $exe).Hash -eq $hash) 'Failed compilation replaced the previous EXE.'
    $held=[IO.File]::Open($exe,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::None)
    try{Reject {& $script *> (Join-Path $run 'expected-publication-failure.log')} 'Build publication failed'}finally{$held.Dispose()}
    Assert ((Get-FileHash -LiteralPath $exe).Hash -eq $hash) 'Failed publication replaced the previous EXE.'
    Reject {& $script -OutputDirectory ($deep+'\'+('x'*60))} 'file-path limit'
    try{$env:TEMP=$deep;$env:TMP=$deep;Reject {& $script} 'temporary folder is too deep'}finally{$env:TEMP=$tempBefore;$env:TMP=$tmpBefore}
    Assert ($env:TEMP -eq $tempBefore -and $env:TMP -eq $tmpBefore) 'Failed build did not restore TEMP/TMP.'
    $result=@('PASS','218-character project root builds and replaces successfully; compile/publication failures preserve the previous EXE; unsafe output/temp lengths fail clearly; caller TEMP/TMP retained.')
    [IO.File]::WriteAllLines((Join-Path $run 'build-path-results.txt'),$result);$result;Write-Output ('Result directory: '+$run)
}catch{[IO.File]::WriteAllText((Join-Path $run 'build-path-error.txt'),$_.ToString());throw}
