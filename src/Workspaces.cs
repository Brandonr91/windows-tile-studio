using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

public class SavedPlacement {
    public string ApplicationPath,WindowClass,WindowTitle;
    public int Slot;
}
public class SavedWorkspaceDisplay {
    public string MonitorId,DisplayName;
    public LayoutProfile Layout;
    public List<SavedPlacement> Windows=new List<SavedPlacement>();
}
public class SavedWorkspace {
    public string Id=Guid.NewGuid().ToString("N"),Name;
    public bool MatchTitles;
    public List<SavedWorkspaceDisplay> Displays=new List<SavedWorkspaceDisplay>();
    public override string ToString(){return Name??"Untitled workspace";}
    public void Validate() {
        if(string.IsNullOrWhiteSpace(Id)||Id.Length>128||string.IsNullOrWhiteSpace(Name)||Name.Length>48)throw new ArgumentException("Workspaces need a name of 1–48 characters and a valid ID.");
        if(Displays==null||Displays.Count<1||Displays.Count>16||Displays.Any(d=>d==null||string.IsNullOrWhiteSpace(d.MonitorId)||d.MonitorId.Length>1024||d.DisplayName!=null&&d.DisplayName.Length>128)||Displays.Select(d=>d.MonitorId).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=Displays.Count)throw new ArgumentException("Workspace displays are invalid.");
        foreach(var display in Displays) {
            if(display.Layout==null||display.Layout.Id==null||display.Layout.Id.Length>128)throw new ArgumentException("A saved layout is missing.");display.Layout.Validate();display.Layout.Shortcut=new Shortcut();
            if(display.Windows==null||display.Windows.Count>16||display.Windows.Any(w=>w==null)||display.Windows.Select(w=>w.Slot).Distinct().Count()!=display.Windows.Count)throw new ArgumentException("Workspace slots are invalid.");
            foreach(var window in display.Windows) {
                if(!WorkspaceApps.ValidPath(window.ApplicationPath)||string.IsNullOrWhiteSpace(window.WindowClass)||window.WindowClass.Length>256||window.Slot<0||window.Slot>=display.Layout.SlotCount||window.WindowTitle!=null&&window.WindowTitle.Length>512)throw new ArgumentException("A saved app identity or slot is invalid.");
                if(!MatchTitles)window.WindowTitle=null;
            }
        }
        if(Displays.Sum(d=>d.Windows.Count)<1)throw new ArgumentException("Snap at least one supported app window before capturing a workspace.");
    }
}
[XmlRoot("TileStudioWorkspaces")]
public class WorkspaceLibrary {
    public int Version=1;
    public List<SavedWorkspace> Workspaces=new List<SavedWorkspace>();
    public void Validate() {
        if(Version!=1||Workspaces==null||Workspaces.Count>16||Workspaces.Any(w=>w==null))throw new ArgumentException("This workspace library is invalid or unsupported.");
        foreach(var w in Workspaces)w.Validate();
        if(Workspaces.Select(w=>w.Id).Distinct().Count()!=Workspaces.Count||Workspaces.Select(w=>w.Name.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=Workspaces.Count)throw new ArgumentException("Workspace names and IDs must be unique.");
    }
}
public static class WorkspaceStore {
    public const int MaximumBytes=1048576;
    public static WorkspaceLibrary Load(string path) {
        if(!File.Exists(path))return new WorkspaceLibrary();
        using(var stream=File.OpenRead(path)) {
            if(stream.Length>MaximumBytes)throw new ArgumentException("Workspace file exceeds 1 MB.");
            var settings=new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=MaximumBytes,CloseInput=false};
            using(var reader=XmlReader.Create(stream,settings))while(reader.Read())if(reader.Depth>32)throw new ArgumentException("Workspace XML is nested too deeply.");stream.Position=0;
            var serializer=new XmlSerializer(typeof(WorkspaceLibrary));serializer.UnknownElement+=delegate{throw new ArgumentException("Unknown workspace data.");};serializer.UnknownAttribute+=delegate{throw new ArgumentException("Unknown workspace attribute.");};
            using(var reader=XmlReader.Create(stream,settings)){var result=(WorkspaceLibrary)serializer.Deserialize(reader);if(result==null)throw new ArgumentException("Workspace library is empty.");result.Validate();return result;}
        }
    }
    public static void Save(string path,WorkspaceLibrary library) {
        library.Validate();string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try {
            using(var stream=File.Create(temp))new XmlSerializer(typeof(WorkspaceLibrary)).Serialize(stream,library);
            if(new FileInfo(temp).Length>MaximumBytes)throw new ArgumentException("Workspace file exceeds 1 MB.");
            if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);
        }finally{if(File.Exists(temp))File.Delete(temp);}
    }
}

// HWND/PID/start time exist only in a live preview, never in saved workspace XML.
public class LiveWorkspaceWindow {
    public IntPtr Handle;
    public int ProcessId;
    public long ProcessStarted;
    public string ApplicationPath,WindowClass,Title;
    public override string ToString(){return string.IsNullOrWhiteSpace(Title)?Path.GetFileName(ApplicationPath):Title;}
}
public static class WorkspaceApps {
    [DllImport("kernel32.dll")] static extern IntPtr OpenProcess(uint access,bool inherit,uint process);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern bool QueryFullProcessImageName(IntPtr process,uint flags,StringBuilder path,ref int size);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr window,StringBuilder text,int size);
    public static bool ValidPath(string path) {
        try{return !string.IsNullOrWhiteSpace(path)&&path.Length<=32767&&Path.IsPathRooted(path)&&!path.StartsWith("\\\\",StringComparison.Ordinal)&&path.IndexOfAny(new[]{'"','\r','\n','\0'})<0&&string.Equals(Path.GetExtension(path),".exe",StringComparison.OrdinalIgnoreCase)&&Path.GetFullPath(path)==path;}catch{return false;}
    }
    public static LiveWorkspaceWindow Read(IntPtr window) {
        if(!NativeMethods.IsWindow(window)||!NativeMethods.IsWindowVisible(window))return null;uint pid;NativeMethods.GetWindowThreadProcessId(window,out pid);
        IntPtr process=OpenProcess(0x1000,false,pid);if(process==IntPtr.Zero)return null;
        try {
            int size=32768;var path=new StringBuilder(size);if(!QueryFullProcessImageName(process,0,path,ref size)||!ValidPath(path.ToString()))return null;
            if(new[]{"ApplicationFrameHost.exe","RuntimeBroker.exe","dllhost.exe","conhost.exe","OpenConsole.exe"}.Contains(Path.GetFileName(path.ToString()),StringComparer.OrdinalIgnoreCase))return null;
            var cls=new StringBuilder(257);var title=new StringBuilder(513);NativeMethods.GetClassName(window,cls,cls.Capacity);GetWindowText(window,title,title.Capacity);
            long started;try{using(var p=Process.GetProcessById((int)pid))started=p.StartTime.ToUniversalTime().Ticks;}catch{return null;}
            return new LiveWorkspaceWindow{Handle=window,ProcessId=(int)pid,ProcessStarted=started,ApplicationPath=path.ToString(),WindowClass=cls.ToString(),Title=title.ToString()};
        }finally{CloseHandle(process);}
    }
    public static bool Matches(SavedPlacement saved,LiveWorkspaceWindow live,bool titles) {return live!=null&&string.Equals(saved.ApplicationPath,live.ApplicationPath,StringComparison.OrdinalIgnoreCase)&&string.Equals(saved.WindowClass,live.WindowClass,StringComparison.Ordinal)&&(!titles||string.Equals(saved.WindowTitle,live.Title,StringComparison.Ordinal));}
    public static bool SameInstance(LiveWorkspaceWindow a,LiveWorkspaceWindow b){return a!=null&&b!=null&&a.Handle==b.Handle&&a.ProcessId==b.ProcessId&&a.ProcessStarted==b.ProcessStarted&&string.Equals(a.ApplicationPath,b.ApplicationPath,StringComparison.OrdinalIgnoreCase)&&a.WindowClass==b.WindowClass;}
    public static void Launch(SavedPlacement saved) {
        if(!ValidPath(saved.ApplicationPath)||!File.Exists(saved.ApplicationPath))throw new InvalidOperationException("The saved executable is unavailable. Open the app yourself, then refresh the preview.");
        Process.Start(new ProcessStartInfo{FileName=saved.ApplicationPath,Arguments="",WorkingDirectory=Path.GetDirectoryName(saved.ApplicationPath),UseShellExecute=false});
    }
}
public class WorkspaceRestoreRow {
    public SavedPlacement Saved;
    public SavedWorkspaceDisplay SavedDisplay;
    public DisplayDescriptor Target;
    public string LayoutId,Issue;
    public List<LiveWorkspaceWindow> Candidates=new List<LiveWorkspaceWindow>();
    public LiveWorkspaceWindow Chosen;
    public bool Selected,ManualChoice;
    public string Status {get{return Issue??(Chosen!=null?"Ready":Candidates.Count==0?"App missing or unavailable":"Choose a matching window");}}
}
public class WorkspaceRestorePlan {
    public SavedWorkspace Workspace;
    public List<WorkspaceRestoreRow> Rows=new List<WorkspaceRestoreRow>();
    public List<WorkspaceRestoreRow> SelectedRows() {
        var rows=Rows.Where(r=>r.Selected).ToList();if(rows.Count==0)throw new ArgumentException("Select at least one ready window.");
        if(rows.Any(r=>r.Issue!=null||r.Chosen==null||r.Target==null||!r.Candidates.Contains(r.Chosen)))throw new ArgumentException("Some selected rows are unavailable or need a window choice.");
        if(rows.Select(r=>r.Chosen.Handle).Distinct().Count()!=rows.Count)throw new ArgumentException("Choose each window only once.");
        if(rows.GroupBy(r=>r.Target.Id,StringComparer.OrdinalIgnoreCase).Any(g=>g.Select(r=>r.SavedDisplay.MonitorId).Distinct(StringComparer.OrdinalIgnoreCase).Count()>1))throw new ArgumentException("Several saved displays target the same screen. Restore one saved display at a time.");
        if(rows.GroupBy(r=>r.Target.Id+":"+r.Saved.Slot).Any(g=>g.Count()>1))throw new ArgumentException("Several windows target the same slot.");return rows;
    }
}
public static class WorkspacePlanner {
    public static WorkspaceRestorePlan Build(SavedWorkspace saved,IList<LiveWorkspaceWindow> windows,IList<DisplayDescriptor> displays,IList<LayoutProfile> layouts,string fallback) {
        saved.Validate();var plan=new WorkspaceRestorePlan{Workspace=saved};
        foreach(var display in saved.Displays)foreach(var entry in display.Windows) {
            var target=displays.FirstOrDefault(d=>string.Equals(d.Id,display.MonitorId,StringComparison.OrdinalIgnoreCase));
            bool missing=target==null;if(missing&&fallback!=null)target=displays.FirstOrDefault(d=>string.Equals(d.Id,fallback,StringComparison.OrdinalIgnoreCase));
            var layout=layouts.FirstOrDefault(p=>p.Id==display.Layout.Id);
            var row=new WorkspaceRestoreRow{Saved=entry,SavedDisplay=display,Target=target,LayoutId=layout==null?null:layout.Id};
            if(target==null)row.Issue="Display disconnected; choose fallback or reconnect";
            else if(layout==null||!MonitorWorkspace.SameShape(display.Layout,layout))row.Issue="Saved layout removed or grid changed; capture again";
            row.Candidates=windows.Where(w=>WorkspaceApps.Matches(entry,w,false)).ToList();var exact=row.Candidates.Where(w=>WorkspaceApps.Matches(entry,w,saved.MatchTitles)).ToList();if(exact.Count==1)row.Chosen=exact[0];row.Selected=row.Issue==null&&row.Chosen!=null;plan.Rows.Add(row);
        }
        // Matching two saved entries to one app/window is ambiguous, never an index guess.
        foreach(var group in plan.Rows.Where(r=>r.Chosen!=null).GroupBy(r=>r.Chosen.Handle).Where(g=>g.Count()>1))foreach(var row in group){row.Chosen=null;row.Selected=false;}
        return plan;
    }
}
