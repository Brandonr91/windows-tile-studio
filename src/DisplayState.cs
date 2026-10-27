using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

public class MonitorConfiguration {
    public string Id;
    public string ActiveProfileId;
    public List<LayoutProfile> Profiles=new List<LayoutProfile>();
    public MonitorConfiguration Clone() { return new MonitorConfiguration{Id=Id,ActiveProfileId=ActiveProfileId,Profiles=Profiles.Select(p=>p.Clone()).ToList()}; }
}

public class DisplayDescriptor {
    public string Id,Name;
    public SixSlotSnap.RECT Bounds,Work;
    public bool Primary;
    public IntPtr Handle;
}

public static class DisplayInventory {
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] struct MonitorInfoEx {
        public int Size; public SixSlotSnap.RECT Monitor,Work; public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)] public string Device;
    }
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] struct DisplayDevice {
        public int Size;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)] public string Name;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)] public string Description;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)] public string Id;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)] public string Key;
    }
    delegate bool MonitorProc(IntPtr monitor,IntPtr dc,IntPtr rectangle,IntPtr data);
    [DllImport("user32.dll",CharSet=CharSet.Unicode,EntryPoint="GetMonitorInfoW")] static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfoEx info);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern bool EnumDisplayDevices(string device,uint index,ref DisplayDevice info,uint flags);
    [DllImport("user32.dll")] static extern bool EnumDisplayMonitors(IntPtr dc,IntPtr clip,MonitorProc callback,IntPtr data);
    [DllImport("user32.dll")] static extern IntPtr MonitorFromPoint(NativeMethods.POINT point,uint flags);
    static DisplayDescriptor Describe(IntPtr handle) {
        var info=new MonitorInfoEx(); info.Size=Marshal.SizeOf(info);
        if(!GetMonitorInfo(handle,ref info)) throw new InvalidOperationException("The selected monitor is no longer available.");
        var target=new DisplayDevice();target.Size=Marshal.SizeOf(target);
        string id="gdi:"+info.Device;
        if(EnumDisplayDevices(info.Device,0,ref target,1) && !string.IsNullOrWhiteSpace(target.Id)) id="monitor:"+target.Id;
        return new DisplayDescriptor{Id=id,Name=info.Device,Handle=handle,Bounds=info.Monitor,Work=info.Work,Primary=(info.Flags&1)!=0};
    }
    public static DisplayDescriptor ForWindow(IntPtr window) { return Describe(NativeMethods.MonitorFromWindow(window,2)); }
    public static DisplayDescriptor ForPoint(int x,int y) { return Describe(MonitorFromPoint(new NativeMethods.POINT{X=x,Y=y},2)); }
    public static List<DisplayDescriptor> Connected() {
        var result=new List<DisplayDescriptor>();
        MonitorProc callback=delegate(IntPtr monitor,IntPtr dc,IntPtr rectangle,IntPtr data) { result.Add(Describe(monitor));return true; };
        if(!EnumDisplayMonitors(IntPtr.Zero,IntPtr.Zero,callback,IntPtr.Zero) || result.Count==0) throw new InvalidOperationException("Windows did not report an available display.");
        return result;
    }
}

public class MonitorSession {
    public DisplayDescriptor Display;
    public LayoutModel Model;
    public IntPtr[] Windows=new IntPtr[16];
    public bool Connected,NeedsReflow,NeedsRecovery;
}

// Pure state transitions; native placement belongs to the controller.
public class MonitorWorkspace {
    public AppSettings Settings { get; private set; }
    public readonly Dictionary<string,MonitorSession> Sessions=new Dictionary<string,MonitorSession>(StringComparer.OrdinalIgnoreCase);
    public MonitorSession Active { get; private set; }
    public MonitorWorkspace(AppSettings settings) { Settings=settings; }
    public static bool SameRect(SixSlotSnap.RECT a,SixSlotSnap.RECT b) { return a.L==b.L && a.T==b.T && a.R==b.R && a.B==b.B; }
    public static bool SameShape(LayoutProfile a,LayoutProfile b) { return a.Columns.Count==b.Columns.Count && a.Columns.Select(c=>c.Rows.Count).SequenceEqual(b.Columns.Select(c=>c.Rows.Count)); }
    MonitorConfiguration Configuration(string id) {
        var config=Settings.Monitors.FirstOrDefault(m=>string.Equals(m.Id,id,StringComparison.OrdinalIgnoreCase));
        if(config==null) {config=new MonitorConfiguration{Id=id,ActiveProfileId=Settings.ActiveProfileId};Settings.Monitors.Add(config);}
        return config;
    }
    public LayoutProfile Resolve(string monitorId,string profileId) {
        var template=Settings.Profiles.First(p=>p.Id==profileId);var result=template.Clone();
        var snapshot=Configuration(monitorId).Profiles.FirstOrDefault(p=>p.Id==profileId);
        if(snapshot!=null && SameShape(template,snapshot)) result.Columns=snapshot.Columns.Select(c=>c.Clone()).ToList();
        return result;
    }
    void Remember(MonitorSession session) {
        var config=Configuration(session.Display.Id);config.ActiveProfileId=session.Model.Profile.Id;
        config.Profiles.RemoveAll(p=>p.Id==session.Model.Profile.Id);config.Profiles.Add(session.Model.Profile.Clone());
    }
    public void Capture() {
        if(Active==null) return;Remember(Active);Settings.ActiveProfileId=Active.Model.Profile.Id;
        var template=Settings.Profiles.FirstOrDefault(p=>p.Id==Active.Model.Profile.Id);
        if(template!=null) template.Columns=Active.Model.Profile.Columns.Select(c=>c.Clone()).ToList();
    }
    public MonitorSession Activate(DisplayDescriptor display) {
        if(Active!=null && string.Equals(Active.Display.Id,display.Id,StringComparison.OrdinalIgnoreCase)) return Active;
        Capture();MonitorSession session;
        if(!Sessions.TryGetValue(display.Id,out session)) {
            var config=Configuration(display.Id);string id=Settings.Profiles.Any(p=>p.Id==config.ActiveProfileId)?config.ActiveProfileId:Settings.Profiles[0].Id;
            session=new MonitorSession{Display=display,Connected=true,NeedsRecovery=true,Model=new LayoutModel(Resolve(display.Id,id)){Work=display.Work}};Sessions.Add(display.Id,session);
        }
        Active=session;Capture();return session;
    }
    public void SelectProfile(string id) {
        if(Active.Model.Profile.Id==id) return;Capture();
        Active.Model=new LayoutModel(Resolve(Active.Display.Id,id)){Work=Active.Display.Work};Active.Windows=new IntPtr[16];Active.NeedsRecovery=true;Capture();
    }
    public void PrepareDraft(AppSettings draft,AppSettings opened) {
        Capture();draft.Monitors=Settings.Monitors.Select(m=>m.Clone()).ToList();
        if(opened!=null) foreach(var profile in draft.Profiles) {
            var original=opened.Profiles.FirstOrDefault(p=>p.Id==profile.Id);
            if(original!=null && original.GeometryMatches(profile)) profile.Columns=Resolve(Active.Display.Id,profile.Id).Columns.Select(c=>c.Clone()).ToList();
        }
        var config=draft.Monitors.First(m=>string.Equals(m.Id,Active.Display.Id,StringComparison.OrdinalIgnoreCase));
        config.ActiveProfileId=draft.ActiveProfileId;
        config.Profiles=draft.Profiles.Select(p=>p.Clone()).ToList();
    }
    public bool UpdateDisplays(IList<DisplayDescriptor> displays) {
        if(displays.Count==0) return false;Capture();bool changed=false;
        var incoming=new HashSet<string>(displays.Select(d=>d.Id),StringComparer.OrdinalIgnoreCase);
        foreach(var session in Sessions.Values) if(session.Connected && !incoming.Contains(session.Display.Id)) {Remember(session);session.Connected=false;session.Windows=new IntPtr[16];session.NeedsReflow=false;changed=true;}
        foreach(var display in displays) {
            MonitorSession session;
            if(!Sessions.TryGetValue(display.Id,out session)) {
                var config=Configuration(display.Id);string id=Settings.Profiles.Any(p=>p.Id==config.ActiveProfileId)?config.ActiveProfileId:Settings.Profiles[0].Id;
                session=new MonitorSession{Display=display,Connected=true,NeedsRecovery=true,Model=new LayoutModel(Resolve(display.Id,id)){Work=display.Work}};Sessions.Add(display.Id,session);changed=true;
            } else {
                bool wasConnected=session.Connected;
                if(!wasConnected || !SameRect(session.Display.Work,display.Work) || !SameRect(session.Display.Bounds,display.Bounds)) {
                    session.NeedsReflow=wasConnected;session.NeedsRecovery=!wasConnected;session.Model.Work=display.Work;changed=true;
                }
                session.Display=display;session.Connected=true;
            }
        }
        if(Active==null || !Active.Connected) Active=Sessions[(displays.FirstOrDefault(d=>d.Primary)??displays[0]).Id];
        Capture();return changed;
    }
    public bool Rebind(AppSettings next) {
        Settings=next;bool activeChanged=false;
        foreach(var session in Sessions.Values) {
            string id=session==Active?next.ActiveProfileId:Configuration(session.Display.Id).ActiveProfileId;
            if(!next.Profiles.Any(p=>p.Id==id)) id=next.Profiles[0].Id;
            var profile=session==Active?next.Profiles.First(p=>p.Id==id).Clone():Resolve(session.Display.Id,id);
            bool shapeChanged=session.Model.Profile.Id!=id || !SameShape(session.Model.Profile,profile);
            if(session==Active) activeChanged=shapeChanged || !session.Model.Profile.GeometryMatches(profile);
            if(shapeChanged) session.Windows=new IntPtr[16];
            session.Model=new LayoutModel(profile){Work=session.Display.Work};
        }
        Capture();return activeChanged;
    }
}
