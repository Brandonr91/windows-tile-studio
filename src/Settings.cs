using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.Xml;

[Flags] public enum ShortcutModifiers { None=0,Alt=1,Control=2,Shift=4,Windows=8 }
public class Shortcut {
    public ShortcutModifiers Modifiers;
    public Keys Key=Keys.None;
    public Shortcut Clone() { return new Shortcut{Modifiers=Modifiers,Key=Key}; }
    public bool Enabled { get { return Key!=Keys.None; } }
    public override string ToString() {
        if(!Enabled) return "Not assigned";
        string key=Key>=Keys.D0 && Key<=Keys.D9?((int)Key-(int)Keys.D0).ToString():Key.ToString();
        string text="";
        if((Modifiers&ShortcutModifiers.Control)!=0) text+="Ctrl+";
        if((Modifiers&ShortcutModifiers.Alt)!=0) text+="Alt+";
        if((Modifiers&ShortcutModifiers.Shift)!=0) text+="Shift+";
        if((Modifiers&ShortcutModifiers.Windows)!=0) text+="Win+";
        return text+key;
    }
    public void Validate() {
        if(!Enabled) return;
        if((Modifiers&(ShortcutModifiers.Control|ShortcutModifiers.Alt|ShortcutModifiers.Windows))==0 || ((int)Modifiers&~15)!=0)
            throw new ArgumentException("Shortcuts need Ctrl, Alt, or Win plus a key.");
        int k=(int)Key;
        bool supported=(k>=0x30 && k<=0x39)||(k>=0x41 && k<=0x5a)||(k>=0x70 && k<=0x87)||Key==Keys.Space||Key==Keys.Tab||Key==Keys.Home||Key==Keys.End||Key==Keys.PageUp||Key==Keys.PageDown||Key==Keys.Left||Key==Keys.Right||Key==Keys.Up||Key==Keys.Down;
        if(!supported) throw new ArgumentException("Choose a letter, number, function key, Space, Tab, or navigation key.");
    }
}

public enum AppearanceMode { System, Light, Dark }
public class AppSettings {
    public AppearanceMode Appearance=AppearanceMode.System;
    public int Version=1;
    public string ActiveProfileId="six-grid";
    public bool ShowLauncherTab=true;
    public bool LauncherPositionSet;
    public bool LauncherPositionLocked;
    public int LauncherX,LauncherY;
    public Shortcut FlyoutShortcut=new Shortcut{Modifiers=ShortcutModifiers.Control|ShortcutModifiers.Alt|ShortcutModifiers.Windows,Key=Keys.Space};
    public List<Shortcut> SlotShortcuts=new List<Shortcut>();
    public List<LayoutProfile> Profiles=new List<LayoutProfile>();
    public List<MonitorConfiguration> Monitors=new List<MonitorConfiguration>();
    public static AppSettings Defaults() {
        var s=new AppSettings();
        s.Profiles.Add(LayoutProfile.Create("six-grid","Six windows",3,2));
        s.Profiles.Add(LayoutProfile.Create("two-columns","Two columns",2,1));
        s.Profiles.Add(LayoutProfile.Create("four-grid","Four windows",2,2));
        s.Profiles.Add(LayoutProfile.Create("three-columns","Three columns",3,1));
        s.Profiles.Add(LayoutProfile.Create("six-portrait","Two columns, three rows",2,3));
        var focus=LayoutProfile.Create("wide-center","Wide center",3,1);
        focus.Columns[0].Width=0.25; focus.Columns[1].Width=0.5; focus.Columns[2].Width=0.25; s.Profiles.Add(focus);
        for(int i=0;i<16;i++) s.SlotShortcuts.Add(i<9?new Shortcut{Modifiers=ShortcutModifiers.Control|ShortcutModifiers.Alt,Key=(Keys)((int)Keys.D1+i)}:new Shortcut());
        return s;
    }
    public AppSettings Clone() { return new AppSettings{Appearance=Appearance,Version=Version,ActiveProfileId=ActiveProfileId,ShowLauncherTab=ShowLauncherTab,LauncherPositionSet=LauncherPositionSet,LauncherPositionLocked=LauncherPositionLocked,LauncherX=LauncherX,LauncherY=LauncherY,FlyoutShortcut=FlyoutShortcut.Clone(),SlotShortcuts=SlotShortcuts.Select(s=>s.Clone()).ToList(),Profiles=Profiles.Select(p=>p.Clone()).ToList(),Monitors=Monitors.Select(m=>m.Clone()).ToList()}; }
    public void Validate() {
        if(Version!=1) throw new ArgumentException("This settings version is not supported.");
        if(!Enum.IsDefined(typeof(AppearanceMode),Appearance)) throw new ArgumentException("Choose System, Light or Dark appearance.");
        if(Profiles==null || Profiles.Count<1 || Profiles.Count>12 || Profiles.Any(p=>p==null)) throw new ArgumentException("Keep between 1 and 12 layouts.");
        foreach(var p in Profiles) { p.Validate(); if(p.Shortcut==null) p.Shortcut=new Shortcut(); }
        if(Profiles.Select(p=>p.Id).Distinct().Count()!=Profiles.Count || Profiles.Select(p=>p.Name.Trim().ToLowerInvariant()).Distinct().Count()!=Profiles.Count)
            throw new ArgumentException("Layout names and IDs must be unique.");
        if(!Profiles.Any(p=>p.Id==ActiveProfileId)) ActiveProfileId=Profiles[0].Id;
        if(Monitors==null) Monitors=new List<MonitorConfiguration>();
        if(Monitors.Count>32 || Monitors.Any(m=>m==null || string.IsNullOrWhiteSpace(m.Id) || m.Id.Length>1024) || Monitors.Select(m=>m.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=Monitors.Count) throw new ArgumentException("Monitor records need unique IDs and at most 32 saved displays.");
        foreach(var monitor in Monitors) {
            if(monitor.Profiles==null || monitor.Profiles.Count>12 || monitor.Profiles.Any(p=>p==null) || monitor.Profiles.Select(p=>p.Id).Distinct().Count()!=monitor.Profiles.Count) throw new ArgumentException("Monitor layout records are invalid.");
            monitor.Profiles.RemoveAll(p=>!Profiles.Any(template=>template.Id==p.Id));
            foreach(var profile in monitor.Profiles) {profile.Validate();if(profile.Shortcut==null)profile.Shortcut=new Shortcut();}
            if(!Profiles.Any(p=>p.Id==monitor.ActiveProfileId)) monitor.ActiveProfileId=ActiveProfileId;
        }
        if(FlyoutShortcut==null || !FlyoutShortcut.Enabled) throw new ArgumentException("Assign a shortcut to open the flyout.");
        if(SlotShortcuts==null || SlotShortcuts.Count!=16 || SlotShortcuts.Any(s=>s==null)) throw new ArgumentException("Expected 16 zone shortcut entries.");
        var keys=new HashSet<string>();
        foreach(var shortcut in new[]{FlyoutShortcut}.Concat(SlotShortcuts).Concat(Profiles.Select(p=>p.Shortcut))) {
            shortcut.Validate(); if(!shortcut.Enabled) continue;
            string token=((int)shortcut.Modifiers)+":"+(int)shortcut.Key;
            if(!keys.Add(token)) throw new ArgumentException("Duplicate shortcut: "+shortcut);
        }
    }
}

public static class SettingsStore {
    static readonly XmlSerializer Serializer=new XmlSerializer(typeof(AppSettings));
    static SettingsStore(){Serializer.UnknownElement+=delegate{throw new ArgumentException("Unsupported settings fields were preserved. Use the version that created this file or review a backup.");};Serializer.UnknownAttribute+=delegate{throw new ArgumentException("Unsupported settings attributes were preserved. Use the version that created this file or review a backup.");};}
    public static AppSettings Load(string path,out string warning) {
        warning=null;
        if(!File.Exists(path)) return AppSettings.Defaults();
        try { using(var stream=File.OpenRead(path)) {
            if(stream.Length>1048576)throw new ArgumentException("Settings exceed the 1 MB limit.");
            var options=new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=1048576};
            using(var reader=XmlReader.Create(stream,options))while(reader.Read())if(reader.Depth>32)throw new ArgumentException("Settings are nested too deeply.");stream.Position=0;
            using(var reader=XmlReader.Create(stream,options)){var s=(AppSettings)Serializer.Deserialize(reader);s.Validate();return s;}
        } }
        catch(Exception e) { warning="Settings could not be loaded. Defaults are running; the original file has been preserved. "+e.GetBaseException().Message; return AppSettings.Defaults(); }
    }
    public static void Save(string path,AppSettings settings) {
        settings.Validate();
        string temp=path+".tmp";
        try {
            using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)) Serializer.Serialize(stream,settings);
            if(File.Exists(path)) File.Replace(temp,path,path+".bak"); else File.Move(temp,path);
        } finally { if(File.Exists(temp)) File.Delete(temp); }
    }
    public static void MigrateLegacy(string path,AppSettings settings) {
        if(!File.Exists(path)) return;
        try {
            double[] v=File.ReadAllLines(path).Select(s=>double.Parse(s,CultureInfo.InvariantCulture)).ToArray();
            if(v.Length!=5 || !(v[0]>0.04 && v[1]-v[0]>0.04 && v[1]<0.96) || v.Skip(2).Any(d=>!(d>0.04 && d<0.96))) return;
            var p=settings.Profiles.First(s=>s.Id=="six-grid");
            p.Columns[0].Width=v[0]; p.Columns[1].Width=v[1]-v[0]; p.Columns[2].Width=1-v[1];
            for(int c=0;c<3;c++) p.Columns[c].Rows=new List<double>{v[c+2],1-v[c+2]};
        } catch(FormatException) {} catch(OverflowException) {}
    }
}
