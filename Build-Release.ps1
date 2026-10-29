[CmdletBinding()]
param([ValidateSet('All','Source','PortableCompiled')][string]$Kind='All',[string]$OutputDirectory=(Join-Path $PSScriptRoot 'bin\releases'),[switch]$AllowDirty)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'InstallationSupport.ps1')
Add-Type -AssemblyName System.IO.Compression,System.IO.Compression.FileSystem
$version=Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'release\version.json') | ConvertFrom-Json
if($version.version -notmatch '^\d+\.\d+\.\d+(?:-(?:preview|beta|rc)\.\d+)?$' -or $version.product -ne 'Windows Tile Studio' -or !([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'src\SixSlotSnap.cs')).Contains('ProductVersion="'+$version.version+'"'))){throw 'Source and release version metadata disagree.'}
$revision=$null;$dirty=$false
if(Test-Path -LiteralPath (Join-Path $PSScriptRoot '.git')){$revision=(& git -C $PSScriptRoot rev-parse HEAD);$dirty=@(& git -C $PSScriptRoot status --porcelain).Count -gt 0;if($dirty -and !$AllowDirty){throw 'Commit/review changes before release packaging, or explicitly use -AllowDirty for a local preview.'}}
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory);Assert-WtsNoLinks $OutputDirectory;New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$files=@{}
foreach($pattern in Get-Content -LiteralPath (Join-Path $PSScriptRoot 'release\package-files.txt')){
    if(!$pattern -or $pattern.StartsWith('#')){continue}
    $items=if($pattern.EndsWith('/**')){Get-ChildItem -Force -Recurse -File -LiteralPath (Join-Path $PSScriptRoot $pattern.Substring(0,$pattern.Length-3))}elseif($pattern.Contains('*')){Get-ChildItem -Force -File -LiteralPath (Join-Path $PSScriptRoot (Split-Path -Parent $pattern)) -Filter (Split-Path -Leaf $pattern)}else{Get-Item -Force -LiteralPath (Join-Path $PSScriptRoot $pattern)}
    foreach($file in $items){$full=Assert-WtsChild $PSScriptRoot $file.FullName;$relative=$full.Substring($PSScriptRoot.Length+1).Replace('\','/');Assert-WtsRelative $relative;if($relative -match '(^|/)(data|work|bin|\.git)/|\.(exe|dll|log|bak|tmp|lnk)$|(^|/)(settings|workspaces|runtime-state)\.xml'){throw ('Private/generated file in package input: '+$relative)};$files[$relative]=$full}
}
function Write-ReleaseArchive([string]$ArchiveKind,[hashtable]$Payload) {
    [string[]]$names=@($Payload.Keys);[Array]::Sort($names,[StringComparer]::Ordinal)
    $entries=@(foreach($name in $names){[ordered]@{path=$name;bytes=(Get-Item -LiteralPath $Payload[$name]).Length;sha256=(Get-FileHash -LiteralPath $Payload[$name] -Algorithm SHA256).Hash.ToLowerInvariant()}})
    $manifest=[ordered]@{schema=1;repository='Brandonr91/windows-tile-studio';product=$version.product;version=$version.version;channel=$version.channel;kind=$ArchiveKind;sourceRevision=$revision;sourceDirty=$dirty;signing='unsigned';files=$entries}
    $json=($manifest | ConvertTo-Json -Depth 12).Replace("`r`n","`n")+"`n"
    $filename='WindowsTileStudio-'+$version.version+'-'+$ArchiveKind+'.zip';$path=Join-Path $OutputDirectory $filename
    $stream=[IO.File]::Open($path,[IO.FileMode]::Create);$archive=New-Object IO.Compression.ZipArchive($stream,[IO.Compression.ZipArchiveMode]::Create,$false)
    try{
        [string[]]$all=@($names)+@('package-manifest.json');[Array]::Sort($all,[StringComparer]::Ordinal)
        foreach($name in $all){$entry=$archive.CreateEntry('WindowsTileStudio/'+$name,[IO.Compression.CompressionLevel]::NoCompression);$entry.LastWriteTime=[DateTimeOffset]::new(2000,1,1,0,0,0,[TimeSpan]::Zero);$entry.ExternalAttributes=0;$target=$entry.Open();try{if($name -eq 'package-manifest.json'){$bytes=[Text.Encoding]::UTF8.GetBytes($json);$target.Write($bytes,0,$bytes.Length)}else{$inputStream=[IO.File]::OpenRead($Payload[$name]);try{$inputStream.CopyTo($target)}finally{$inputStream.Dispose()}}}finally{$target.Dispose()}}
    }finally{$archive.Dispose();$stream.Dispose()}
    return [ordered]@{file=$filename;kind=$ArchiveKind;sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant();fileCount=$all.Count;signing='unsigned'}
}
$artifacts=@();$compiled=$null
try{
    if($Kind -in @('All','Source')){$artifacts+=Write-ReleaseArchive 'Source' $files}
    if($Kind -in @('All','PortableCompiled')){$compiled=Assert-WtsChild $OutputDirectory (Join-Path $OutputDirectory ('.build-'+[Guid]::NewGuid().ToString('N')));& (Join-Path $PSScriptRoot 'Build.ps1') -OutputDirectory $compiled | Out-Null;$portable=$files.Clone();$portable['bin/SixSlotSnap.exe']=Join-Path $compiled 'SixSlotSnap.exe';$artifacts+=Write-ReleaseArchive 'PortableCompiled' $portable}
    $checksums=(@($artifacts | Sort-Object file | ForEach-Object {$_.sha256+'  '+$_.file}) -join "`n")+"`n"
    [IO.File]::WriteAllText((Join-Path $OutputDirectory 'SHA256SUMS.txt'),$checksums,[Text.UTF8Encoding]::new($false))
    $info=[ordered]@{schema=1;product=$version.product;version=$version.version;sourceRevision=$revision;sourceDirty=$dirty;signing='unsigned';artifacts=$artifacts}
    [IO.File]::WriteAllText((Join-Path $OutputDirectory 'release-info.json'),($info | ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))
    [pscustomobject]$info
}finally{if($compiled -and (Test-Path -LiteralPath $compiled)){[void](Assert-WtsChild $OutputDirectory $compiled);if((Split-Path -Leaf $compiled) -notmatch '^\.build-[0-9a-f]{32}$'){throw 'Unexpected build cleanup path.'};Remove-Item -LiteralPath $compiled -Recurse -Force}}
