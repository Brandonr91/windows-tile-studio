param([Parameter(Mandatory=$true)][string]$SourceRoot,[Parameter(Mandatory=$true)][string]$SettingsFile)
$ErrorActionPreference='Stop'
if($PSVersionTable.PSVersion.Major -ne 5 -or [Threading.Thread]::CurrentThread.GetApartmentState() -ne 'STA'){throw 'Use Windows PowerShell 5.1 -STA.'}
Add-Type -AssemblyName System.Windows.Forms,System.Drawing,System.Xml.Linq
# Compile the selected version and probe together; never invoke its application.
$probe=Join-Path (Split-Path -Parent $SettingsFile) 'ConfigurationProbe.cs'
$code=@'
using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
public static class WtsConfigurationProbe {
    public static void Check(string path) {
        if(new FileInfo(path).Length>1048576)throw new InvalidOperationException("Settings exceed the compatibility-check limit.");
        var options=new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=1048576};
        using(var reader=XmlReader.Create(path,options))while(reader.Read())if(reader.Depth>32)throw new InvalidOperationException("Settings are nested too deeply.");
        var serializer=new XmlSerializer(typeof(AppSettings));
        serializer.UnknownElement+=delegate{throw new InvalidOperationException("The target version cannot preserve every saved setting.");};
        serializer.UnknownAttribute+=delegate{throw new InvalidOperationException("The target version cannot preserve every saved settings attribute.");};
        using(var reader=XmlReader.Create(path,options)){var settings=(AppSettings)serializer.Deserialize(reader);settings.Validate();}
    }
}
'@
[IO.File]::WriteAllText($probe,$code,[Text.UTF8Encoding]::new($false))
$sources=@(Get-ChildItem -LiteralPath (Join-Path $SourceRoot 'src') -Filter '*.cs' | ForEach-Object {$_.FullName})+@($probe)
Add-Type -LiteralPath $sources -ReferencedAssemblies System.dll,System.Core.dll,System.Xml.dll,System.Xml.Linq.dll,System.Drawing.dll,System.Windows.Forms.dll,Accessibility.dll
[WtsConfigurationProbe]::Check([IO.Path]::GetFullPath($SettingsFile))
Write-Output 'Configuration is compatible; saved settings were not changed.'
