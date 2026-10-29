using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

[XmlRoot("TileStudioLayouts")]
public class PortableLayoutSet {
    public int Version;
    public string ActiveProfileId;
    public List<LayoutProfile> Profiles;
    public Shortcut FlyoutShortcut;
    public List<Shortcut> SlotShortcuts;
    public void Validate() {
        if(Version!=1)throw new ArgumentException("This portable layout version is not supported.");
        if(Profiles==null || Profiles.Any(p=>p==null || p.Id==null || p.Id.Length>128))throw new ArgumentException("Portable layouts are missing or invalid.");
        var check=AppSettings.Defaults();check.Profiles=Profiles;check.ActiveProfileId=ActiveProfileId;check.FlyoutShortcut=FlyoutShortcut;check.SlotShortcuts=SlotShortcuts;check.Validate();
        if(!Profiles.Any(p=>p.Id==ActiveProfileId))throw new ArgumentException("The selected portable layout is missing.");
    }
}

public static class PortableLayouts {
    public const int MaximumBytes=262144;
    public static PortableLayoutSet FromSettings(AppSettings settings) {
        var copy=settings.Clone();copy.Validate();
        return new PortableLayoutSet{Version=1,ActiveProfileId=copy.ActiveProfileId,Profiles=copy.Profiles.Select(p=>p.Clone()).ToList(),FlyoutShortcut=copy.FlyoutShortcut.Clone(),SlotShortcuts=copy.SlotShortcuts.Select(s=>s.Clone()).ToList()};
    }
    public static PortableLayoutSet Load(string path) {
        using(var stream=File.OpenRead(path)) {
            if(stream.Length>MaximumBytes)throw new ArgumentException("The portable file exceeds the 256 KB limit.");
            var options=new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=MaximumBytes,CloseInput=false};
            using(var preflight=XmlReader.Create(stream,options))while(preflight.Read())if(preflight.Depth>32)throw new ArgumentException("The portable file is nested too deeply.");
            stream.Position=0;
            var serializer=new XmlSerializer(typeof(PortableLayoutSet));
            serializer.UnknownElement+=delegate{throw new ArgumentException("The file contains an unrecognized element.");};
            serializer.UnknownAttribute+=delegate{throw new ArgumentException("The file contains an unrecognized attribute.");};
            using(var reader=XmlReader.Create(stream,options)) {var set=(PortableLayoutSet)serializer.Deserialize(reader);if(set==null)throw new ArgumentException("Portable layouts are missing.");set.Validate();return set;}
        }
    }
    public static void Export(string path,AppSettings settings) {
        var set=FromSettings(settings);set.Validate();string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try {
            using(var stream=File.Create(temp))new XmlSerializer(typeof(PortableLayoutSet)).Serialize(stream,set);
            if(new FileInfo(temp).Length>MaximumBytes)throw new ArgumentException("The portable file exceeds the 256 KB limit.");
            if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
        } finally {if(File.Exists(temp))File.Delete(temp);}
    }
    public static AppSettings Candidate(AppSettings current,PortableLayoutSet set,bool replace,bool importHotkeys) {
        if(set==null || set.Profiles==null || set.Profiles.Any(p=>p==null || p.Shortcut==null || p.Columns==null || p.Columns.Any(c=>c==null || c.Rows==null)))throw new ArgumentException("Portable layouts are missing or invalid.");
        // Work on a deep copy of the supplied bundle as validation normalizes it.
        var clone=new PortableLayoutSet{Version=set.Version,ActiveProfileId=set.ActiveProfileId,Profiles=set.Profiles==null?null:set.Profiles.Select(p=>p==null?null:p.Clone()).ToList(),FlyoutShortcut=set.FlyoutShortcut==null?null:set.FlyoutShortcut.Clone(),SlotShortcuts=set.SlotShortcuts==null?null:set.SlotShortcuts.Select(s=>s==null?null:s.Clone()).ToList()};clone.Validate();
        var result=current.Clone();if(replace)result.Profiles.Clear();
        if(result.Profiles.Count+clone.Profiles.Count>12)throw new ArgumentException("This would exceed 12 layouts. Remove layouts first, or choose Replace catalog.");
        var names=new HashSet<string>(result.Profiles.Select(p=>p.Name.Trim()),StringComparer.OrdinalIgnoreCase);
        foreach(var source in clone.Profiles) {
            var profile=source.Clone();profile.Id=Guid.NewGuid().ToString("N");string name=profile.Name.Trim();int n=2;
            while(names.Contains(name)){string suffix=" ("+(n++)+")";name=profile.Name.Trim();name=name.Substring(0,Math.Min(name.Length,48-suffix.Length))+suffix;}
            profile.Name=name;names.Add(name);if(!importHotkeys)profile.Shortcut=new Shortcut();
            result.Profiles.Add(profile);if(replace && source.Id==clone.ActiveProfileId)result.ActiveProfileId=profile.Id;
        }
        if(importHotkeys){result.FlyoutShortcut=clone.FlyoutShortcut.Clone();result.SlotShortcuts=clone.SlotShortcuts.Select(s=>s.Clone()).ToList();}
        result.Validate();return result;
    }
}
