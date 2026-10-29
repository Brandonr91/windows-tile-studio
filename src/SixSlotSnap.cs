using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;

public static class NativeMethods {
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X,Y; }
    [StructLayout(LayoutKind.Sequential)] public struct MINMAXINFO { public POINT Reserved,MaxSize,MaxPosition,MinTrackSize,MaxTrackSize; }
    [DllImport("user32.dll",EntryPoint="SendMessageTimeoutW",SetLastError=true)] public static extern IntPtr QueryMinMax(IntPtr window,uint message,IntPtr wparam,ref MINMAXINFO info,uint flags,uint timeout,out IntPtr result);
    [DllImport("user32.dll")] public static extern bool IsHungAppWindow(IntPtr window);
    public delegate bool EnumProc(IntPtr window,IntPtr parameter);
    public delegate void WinEventProc(IntPtr hook,uint evt,IntPtr window,int objectId,int childId,uint thread,uint time);
    [StructLayout(LayoutKind.Sequential)] public struct MonitorInfo { public int Size; public SixSlotSnap.RECT Monitor,Work; public uint Flags; }
    [DllImport("user32.dll")] public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] public static extern IntPtr MonitorFromWindow(IntPtr window,uint flags);
    [DllImport("user32.dll",CharSet=CharSet.Auto)] public static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr window,out SixSlotSnap.RECT rect);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr window,int attr,out SixSlotSnap.RECT rect,int size);
    [DllImport("user32.dll")] public static extern bool IsZoomed(IntPtr window);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr window,int command);
    [DllImport("user32.dll",SetLastError=true)] public static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
    [DllImport("user32.dll",SetLastError=true)] public static extern bool RegisterHotKey(IntPtr window,int id,uint modifiers,uint key);
    [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr window,int id);
    [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr window);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc callback,IntPtr parameter);
    [DllImport("user32.dll")] public static extern IntPtr SetWinEventHook(uint first,uint last,IntPtr module,WinEventProc callback,uint process,uint thread,uint flags);
    [DllImport("user32.dll")] public static extern bool UnhookWinEvent(IntPtr hook);
    [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr window);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr window,out uint process);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr window,StringBuilder name,int size);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern IntPtr FindWindow(string className,string caption);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr window,int message,IntPtr wparam,IntPtr lparam);
    [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr icon);
    public static bool VisibleRect(IntPtr window,out SixSlotSnap.RECT rect) { return DwmGetWindowAttribute(window,9,out rect,16)==0 || GetWindowRect(window,out rect); }
    public static MonitorInfo Monitor(IntPtr window) { var m=new MonitorInfo(); m.Size=Marshal.SizeOf(m); if(!GetMonitorInfo(MonitorFromWindow(window,2),ref m)) throw new InvalidOperationException("Could not read monitor work area."); return m; }
}

public class HotkeyRegistry : IDisposable {
    readonly IntPtr window;
    Dictionary<int,Shortcut> registered=new Dictionary<int,Shortcut>();
    public HotkeyRegistry(IntPtr handle) { window=handle; }
    public Dictionary<int,Shortcut> Snapshot() { return registered.ToDictionary(p=>p.Key,p=>p.Value.Clone()); }
    public string Restore(Dictionary<int,Shortcut> previous) {
        foreach(int id in registered.Keys) NativeMethods.UnregisterHotKey(window,id);
        registered=new Dictionary<int,Shortcut>(); string error=null;
        foreach(var pair in previous) {
            if(NativeMethods.RegisterHotKey(window,pair.Key,0x4000|(uint)pair.Value.Modifiers,(uint)pair.Value.Key)) registered.Add(pair.Key,pair.Value.Clone());
            else error="A previous shortcut could not be restored: "+pair.Value+". Open Hotkeys and choose an available combination.";
        }
        return error;
    }
    public static Dictionary<int,Shortcut> Bindings(AppSettings settings) {
        var result=new Dictionary<int,Shortcut>(); result.Add(100,settings.FlyoutShortcut);
        for(int i=0;i<16;i++) if(settings.SlotShortcuts[i].Enabled) result.Add(i+1,settings.SlotShortcuts[i]);
        for(int i=0;i<settings.Profiles.Count;i++) if(settings.Profiles[i].Shortcut.Enabled) result.Add(300+i,settings.Profiles[i].Shortcut);
        return result;
    }
    public bool Replace(AppSettings candidate,out string error) {
        error=null; candidate.Validate(); var next=Bindings(candidate); var previous=registered;
        foreach(int id in previous.Keys) NativeMethods.UnregisterHotKey(window,id);
        var installed=new Dictionary<int,Shortcut>();
        foreach(var pair in next) {
            if(!NativeMethods.RegisterHotKey(window,pair.Key,0x4000|(uint)pair.Value.Modifiers,(uint)pair.Value.Key)) {
                error="The shortcut "+pair.Value+" is already in use or reserved by Windows. Choose another combination.";
                foreach(int id in installed.Keys) NativeMethods.UnregisterHotKey(window,id);
                registered=new Dictionary<int,Shortcut>();
                foreach(var old in previous) if(NativeMethods.RegisterHotKey(window,old.Key,0x4000|(uint)old.Value.Modifiers,(uint)old.Value.Key)) registered.Add(old.Key,old.Value); else error+=" Previous shortcut could not be restored: "+old.Value+".";
                return false;
            }
            installed.Add(pair.Key,pair.Value.Clone());
        }
        registered=installed; return true;
    }
    public void Dispose() { foreach(int id in registered.Keys) NativeMethods.UnregisterHotKey(window,id); registered.Clear(); }
}

public class SixSlotSnap : ApplicationContext {
    public const string ProductVersion="0.3.0-preview.6.1";
    public static Icon CreateStudioIcon() {
        using(var bitmap=new Bitmap(32,32)) using(var graphics=Graphics.FromImage(bitmap)) {
            graphics.Clear(Color.Transparent);
            using(var background=new SolidBrush(Color.FromArgb(24,33,47))) graphics.FillRectangle(background,0,0,32,32);
            for(int row=0;row<2;row++) for(int column=0;column<3;column++) {
                using(var brush=new SolidBrush(column==1?Color.FromArgb(75,201,231):Color.FromArgb(74,139,236))) graphics.FillRectangle(brush,3+column*9,4+row*13,8,11);
            }
            IntPtr handle=bitmap.GetHicon();
            try { using(var temporary=Icon.FromHandle(handle)) return (Icon)temporary.Clone(); }
            finally { NativeMethods.DestroyIcon(handle); }
        }
    }
    readonly List<int> navigationHotkeys=new List<int>();
    static readonly Keys[] navigationKeys={Keys.Left,Keys.Right,Keys.Up,Keys.Down,Keys.Tab,Keys.Tab,Keys.Enter};
    void RegisterNavigation() {
        for(int i=0;i<navigationKeys.Length;i++) {
            int id=201+i;
            if(!NativeMethods.RegisterHotKey(receiver.Handle,id,0x4000u|(i==5?4u:0u),(uint)navigationKeys[i])) {
                ReleaseNavigation(); Log("Flyout keyboard navigation unavailable: a navigation key is reserved."); return;
            }
            navigationHotkeys.Add(id);
        }
        flyout.KeyboardAvailable(true);
    }
    void ReleaseNavigation() { foreach(int id in navigationHotkeys) NativeMethods.UnregisterHotKey(receiver.Handle,id); navigationHotkeys.Clear(); }
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L,T,R,B; }
    public const string ControllerCaption="SixSlotSnap.Controller.v2";
    public const int ShowCommand=0x8001,SettingsCommand=0x8002,QuitCommand=0x8003,SnapCommand=0x8004,ProfileCommand=0x8005,WorkspacesCommand=0x8006;
    readonly string folder,settingsPath;
    AppSettings settings;
    public LayoutModel grid;
    IntPtr[] assigned=new IntPtr[16];
    readonly MonitorWorkspace workspace;
    string settingsMonitorId;
    AppSettings settingsOpened;
    readonly IntPtr[] resizeWindows=new IntPtr[16];
    readonly RECT[] resizeBaseline=new RECT[16];
    readonly bool[] resizeBaselineValid=new bool[16];
    readonly HashSet<int> resizeWidths=new HashSet<int>(),resizeHeights=new HashSet<int>();
    int resizeHeightBoundary=-1;
    IntPtr resizeSource;
    class ResizeProbe { public readonly Size[] Minimums=new Size[16]; public readonly bool[] Valid=new bool[16]; public volatile bool Ready,Cancelled; public bool Applied; }
    ResizeProbe resizeProbe;
    bool resizeEnding,resizeAttempted;
    LayoutProfile resizeStartProfile;
    string lastPlacementIssue;
    DateTime lastFitNotice;
    readonly CommandWindow receiver;
    readonly HotkeyRegistry hotkeys;
    readonly NotifyIcon tray;
    readonly System.Windows.Forms.Timer resizeTimer=new System.Windows.Forms.Timer(),settleTimer=new System.Windows.Forms.Timer(),flyoutTimer=new System.Windows.Forms.Timer(),displayTimer=new System.Windows.Forms.Timer();
    int displayRefreshes;
    readonly NativeMethods.WinEventProc eventCallback;
    IntPtr eventHook,destroyHook,dragging,targetWindow,lastExternalWindow;
    RECT lastDrag;
    bool changed,mouseWasDown,escapeWasDown,escapeRegistered,disposed;
    int settlePasses;
    SnapFlyout flyout;
    LauncherTab launcher;
    ToolStripMenuItem launcherMenu;
    SnapSettingsForm settingsForm;
    WorkspaceDialog workspaceForm;
    readonly StartupLaunchSpec startupLaunch;
    readonly bool recover;
    bool preserveInvalidSettings;
    string recoveryNotice;
    class CommandWindow : NativeWindow {
        public Action<int,IntPtr,IntPtr> Received;
        public CommandWindow() { CreateHandle(new CreateParams{Caption=ControllerCaption}); }
        protected override void WndProc(ref Message m) { if((m.Msg==0x312 || m.Msg==0x7e || m.Msg==0x1a || m.Msg==0x218 || (m.Msg>=ShowCommand && m.Msg<=WorkspacesCommand)) && Received!=null) Received(m.Msg,m.WParam,m.LParam); base.WndProc(ref m); }
    }
    void Log(string text) { try { File.AppendAllText(Path.Combine(folder,"status.log"),DateTime.Now.ToString("s")+" "+text+Environment.NewLine); } catch(IOException) {} }
    static bool Near(RECT a,RECT b) { return Math.Abs(a.L-b.L)<16 && Math.Abs(a.R-b.R)<16 && Math.Abs(a.T-b.T)<16 && Math.Abs(a.B-b.B)<16; }
    static bool Down(int key) { return (NativeMethods.GetAsyncKeyState(key)&0x8000)!=0; }
    static bool MouseDown() { return Down(1)||Down(2)||Down(4); }
    static string ProcessName(IntPtr w) { uint pid; NativeMethods.GetWindowThreadProcessId(w,out pid); try { return Process.GetProcessById((int)pid).ProcessName; } catch { return "selected window"; } }
    bool ValidTarget(IntPtr w) {
        if(w==IntPtr.Zero || !NativeMethods.IsWindow(w) || !NativeMethods.IsWindowVisible(w)) return false;
        uint pid; NativeMethods.GetWindowThreadProcessId(w,out pid); var name=new StringBuilder(256); NativeMethods.GetClassName(w,name,name.Capacity);
        string cls=name.ToString(); return pid!=(uint)Process.GetCurrentProcess().Id && cls!="Progman" && cls!="WorkerW" && cls!="Shell_TrayWnd" && cls!="Shell_SecondaryTrayWnd";
    }
    void BindActive() { grid=workspace.Active.Model; assigned=workspace.Active.Windows; workspace.Capture(); }
    // Use the current inventory's native handle on the drag loop. Device-interface
    // enumeration belongs to inventory refresh/target selection, not every frame.
    bool OnActiveMonitor(IntPtr window) { return NativeMethods.MonitorFromWindow(window,2)==workspace.Active.Display.Handle; }
    void ActivateMonitor(DisplayDescriptor display) {
        MonitorSession known;
        if(!workspace.Sessions.TryGetValue(display.Id,out known) || !known.Connected || !MonitorWorkspace.SameRect(known.Display.Work,display.Work) || !MonitorWorkspace.SameRect(known.Display.Bounds,display.Bounds)) RefreshDisplays();
        if(workspace.Active==null || !string.Equals(display.Id,workspace.Active.Display.Id,StringComparison.OrdinalIgnoreCase)) {
            CancelResizeSession(); workspace.Activate(display); BindActive();
        }
        if(workspace.Active.NeedsRecovery) { RecoverAssignments(); workspace.Active.NeedsRecovery=false; }
    }
    void TargetMonitor(IntPtr window) { if(ValidTarget(window)) ActivateMonitor(DisplayInventory.ForWindow(window)); }
    bool ReleaseWindow(IntPtr window) {
        bool released=false;
        foreach(var session in workspace.Sessions.Values) for(int i=0;i<session.Windows.Length;i++) if(window!=IntPtr.Zero && session.Windows[i]==window) {session.Windows[i]=IntPtr.Zero;released=true;}
        return released;
    }
    void RefreshDisplays() {
        displayTimer.Stop();
        try {
            var displays=DisplayInventory.Connected(); CancelResizeSession(); HideFlyout();
            bool changedDisplays=workspace.UpdateDisplays(displays); var active=workspace.Active;
            foreach(var session in workspace.Sessions.Values.Where(s=>s.Connected).ToArray()) {
                workspace.Activate(session.Display); BindActive();
                if(session.NeedsReflow) {
                    for(int i=0;i<grid.Profile.SlotCount;i++) {
                        IntPtr window=assigned[i];
                        if(!ValidTarget(window) || !OnActiveMonitor(window)) {assigned[i]=IntPtr.Zero;continue;}
                        if(NativeMethods.IsIconic(window) || NativeMethods.IsZoomed(window) || !PlaceWindow(window,grid.RectFor(i),false)) assigned[i]=IntPtr.Zero;
                    }
                    session.NeedsReflow=false;
                }
                if(session.NeedsRecovery) { RecoverAssignments();session.NeedsRecovery=false; }
            }
            workspace.Activate(active.Display);BindActive();displayRefreshes++;
            if(changedDisplays) SaveSettings(); RefreshLauncher(); WriteState();
        } catch(Exception e) {Log("Display refresh: "+e.Message);}
    }
    void SaveSettings() { workspace.Capture(); if(preserveInvalidSettings) return; try { SettingsStore.Save(settingsPath,settings); } catch(Exception e) { Log("Settings save failed: "+e.Message); } }
    void WriteState() {
        try {
            var root=new XElement("Runtime",new XAttribute("pid",Process.GetCurrentProcess().Id),new XAttribute("version",ProductVersion),new XAttribute("recoveryRequired",recoveryNotice!=null),new XAttribute("controller",receiver.Handle.ToInt64()),new XAttribute("profile",settings.ActiveProfileId),new XAttribute("flyoutVisible",flyout!=null && flyout.Visible),new XAttribute("flyoutHandle",flyout==null?0:flyout.Handle.ToInt64()),new XAttribute("target",targetWindow.ToInt64()),new XAttribute("flyoutShortcut",settings.FlyoutShortcut));
            if(lastPlacementIssue!=null) root.Add(new XAttribute("placementIssue",lastPlacementIssue));
            root.Add(new XAttribute("resizing",dragging!=IntPtr.Zero));
            root.Add(new XAttribute("resizeSource",dragging.ToInt64()));
            root.Add(new XAttribute("launcherEnabled",settings.ShowLauncherTab),new XAttribute("launcherVisible",launcher!=null && !launcher.IsDisposed && launcher.Visible),new XAttribute("launcherHandle",launcher==null || launcher.IsDisposed || !launcher.IsHandleCreated?0:launcher.Handle.ToInt64()));
            root.Add(new XAttribute("activeMonitor",workspace.Active.Display.Id),new XAttribute("displayRefreshes",displayRefreshes));
            var monitors=new XElement("Monitors");
            foreach(var session in workspace.Sessions.Values) {
                var monitor=new XElement("Monitor",new XAttribute("id",session.Display.Id),new XAttribute("name",session.Display.Name),new XAttribute("connected",session.Connected),new XAttribute("profile",session.Model.Profile.Id),new XAttribute("left",session.Model.Work.L),new XAttribute("top",session.Model.Work.T),new XAttribute("right",session.Model.Work.R),new XAttribute("bottom",session.Model.Work.B));
                for(int i=0;i<session.Model.Profile.SlotCount;i++) if(session.Windows[i]!=IntPtr.Zero && NativeMethods.IsWindow(session.Windows[i])) monitor.Add(new XElement("Zone",new XAttribute("number",i+1),new XAttribute("window",session.Windows[i].ToInt64())));
                monitors.Add(monitor);
            }
            root.Add(monitors);
            for(int i=0;i<grid.Profile.SlotCount;i++) if(assigned[i]!=IntPtr.Zero && NativeMethods.IsWindow(assigned[i])) root.Add(new XElement("Zone",new XAttribute("number",i+1),new XAttribute("window",assigned[i].ToInt64())));
            string path=Path.Combine(folder,"runtime-state.xml"); root.Save(path+".tmp"); if(File.Exists(path)) File.Replace(path+".tmp",path,null); else File.Move(path+".tmp",path);
        } catch(IOException) {} catch(UnauthorizedAccessException) {}
    }
    void RecoverAssignments() {
        if(!recover) return; var rects=new RECT[16];
        for(int i=0;i<grid.Profile.SlotCount;i++) if(assigned[i]!=IntPtr.Zero) NativeMethods.VisibleRect(assigned[i],out rects[i]);
        NativeMethods.EnumWindows(delegate(IntPtr window,IntPtr unused) {
            RECT rect; if(!ValidTarget(window) || !OnActiveMonitor(window) || assigned.Contains(window) || NativeMethods.IsIconic(window) || NativeMethods.IsZoomed(window) || !NativeMethods.VisibleRect(window,out rect)) return true;
            if(workspace.Sessions.Values.Any(s=>s!=workspace.Active && s.Windows.Contains(window))) return true;
            for(int i=0;i<grid.Profile.SlotCount;i++) if(assigned[i]==IntPtr.Zero) {
                RECT wanted=grid.RectFor(i); int c,r; grid.Locate(i,out c,out r); int count=grid.Profile.Columns[c].Rows.Count;
                bool match=Near(rect,wanted);
                if(!match && count==2) match=Math.Abs(rect.L-wanted.L)<16 && Math.Abs(rect.R-wanted.R)<16 && (r==0?Math.Abs(rect.T-grid.Work.T)<16:Math.Abs(rect.B-grid.Work.B)<16) && rect.B-rect.T<(grid.Work.B-grid.Work.T)*0.95;
                if(match) { assigned[i]=window; rects[i]=rect; break; }
            }
            return true;
        },IntPtr.Zero);
        for(int c=0;c<grid.Profile.Columns.Count;c++) if(grid.Profile.Columns[c].Rows.Count==2) {
            int top=grid.IndexOf(c,0),bottom=grid.IndexOf(c,1);
            if(assigned[top]!=IntPtr.Zero && assigned[bottom]!=IntPtr.Zero && Math.Abs(rects[top].B-rects[bottom].T)<16) {
                double ratio=((rects[top].B+rects[bottom].T)/2.0-grid.Work.T)/(grid.Work.B-grid.Work.T);
                if(ratio>=0.04 && ratio<=0.96) grid.Profile.Columns[c].Rows=new List<double>{ratio,1-ratio};
            }
        }
        Log("Recovered "+assigned.Count(w=>w!=IntPtr.Zero)+" occupied zones in "+grid.Profile.Name+".");
    }
    bool ReadMinimum(IntPtr window,out Size minimum) {
        minimum=Size.Empty; RECT outer,visible; IntPtr result;
        if(NativeMethods.IsHungAppWindow(window) || !NativeMethods.GetWindowRect(window,out outer) || !NativeMethods.VisibleRect(window,out visible)) return false;
        var info=new NativeMethods.MINMAXINFO();
        if(NativeMethods.QueryMinMax(window,0x24,IntPtr.Zero,ref info,0x23,50,out result)==IntPtr.Zero) return false;
        minimum=new Size(Math.Max(0,info.MinTrackSize.X-((outer.R-outer.L)-(visible.R-visible.L))),Math.Max(0,info.MinTrackSize.Y-((outer.B-outer.T)-(visible.B-visible.T))));
        return true;
    }
    void FitIssue(string message,bool notify) {
        bool fresh=message!=lastPlacementIssue || (DateTime.UtcNow-lastFitNotice).TotalSeconds>2;
        lastPlacementIssue=message;
        if(fresh) { lastFitNotice=DateTime.UtcNow; Log(message); if(notify) tray.ShowBalloonTip(3500,"Window does not fit",message,ToolTipIcon.Warning); }
        WriteState();
    }
    static bool SameFrame(RECT a,RECT b) { return Math.Abs(a.L-b.L)<=2 && Math.Abs(a.R-b.R)<=2 && Math.Abs(a.T-b.T)<=2 && Math.Abs(a.B-b.B)<=2; }
    bool PlaceWindow(IntPtr window,RECT wanted,bool notify,bool queryMinimum=true) {
        RECT outer,visible; if(!NativeMethods.GetWindowRect(window,out outer) || !NativeMethods.VisibleRect(window,out visible)) return false;
        if(NativeMethods.IsHungAppWindow(window)) { FitIssue("The app is not responding. Try snapping it again after it responds.",notify); return false; }
        Size minimum=Size.Empty;
        if(queryMinimum && !ReadMinimum(window,out minimum)) { FitIssue("The app did not provide size information in time, or Windows blocked access. Try again; administrator apps may require matching privileges.",notify); return false; }
        if(queryMinimum && (wanted.R-wanted.L<minimum.Width || wanted.B-wanted.T<minimum.Height)) { FitIssue("This app needs at least "+minimum.Width+" x "+minimum.Height+" pixels; the selected zone is "+(wanted.R-wanted.L)+" x "+(wanted.B-wanted.T)+". Choose a larger layout or enlarge its column/row.",notify); return false; }
        int l=visible.L-outer.L,t=visible.T-outer.T,r=outer.R-visible.R,b=outer.B-visible.B;
        if(!NativeMethods.SetWindowPos(window,IntPtr.Zero,wanted.L-l,wanted.T-t,wanted.R-wanted.L+l+r,wanted.B-wanted.T+t+b,0x14)) {
            Log("Placement failed, Win32 "+Marshal.GetLastWin32Error()); if(notify) tray.ShowBalloonTip(3000,"Window could not be moved","The app may need matching administrator privileges.",ToolTipIcon.Warning); return false;
        }
        RECT actual;
        if(!NativeMethods.VisibleRect(window,out actual) || !SameFrame(actual,wanted)) {
            NativeMethods.SetWindowPos(window,IntPtr.Zero,outer.L,outer.T,outer.R-outer.L,outer.B-outer.T,0x14);
            FitIssue("The app refused the requested frame. Its previous bounds were requested again; choose a larger zone or a supported window style.",notify); return false;
        }
        return true;
    }
    void ApplyLayout() {
        for(int i=0;i<grid.Profile.SlotCount;i++) { IntPtr w=assigned[i]; if(!NativeMethods.IsWindow(w) || !OnActiveMonitor(w)) { assigned[i]=IntPtr.Zero; continue; } if(w!=dragging && !NativeMethods.IsIconic(w) && !NativeMethods.IsZoomed(w) && !PlaceWindow(w,grid.RectFor(i),false)) assigned[i]=IntPtr.Zero; }
    }
    void CancelResizeSession() {
        resizeTimer.Stop(); settleTimer.Stop(); dragging=IntPtr.Zero;
        changed=false; resizeHeightBoundary=-1; resizeSource=IntPtr.Zero; if(resizeProbe!=null) resizeProbe.Cancelled=true; resizeProbe=null; resizeEnding=false; resizeAttempted=false;
        resizeWidths.Clear(); resizeHeights.Clear(); Array.Clear(resizeBaselineValid,0,16);
    }
    void BeginResizeSession(IntPtr window) {
        CancelResizeSession(); dragging=window; resizeSource=window; changed=false;
        resizeStartProfile=grid.Profile.Clone(); Array.Clear(grid.MinimumWidths,0,16); Array.Clear(grid.MinimumHeights,0,16);
        for(int i=0;i<grid.Profile.SlotCount;i++) {
            if(assigned[i]!=IntPtr.Zero && (!NativeMethods.IsWindow(assigned[i]) || !OnActiveMonitor(assigned[i]))) assigned[i]=IntPtr.Zero;
            resizeWindows[i]=assigned[i];
            resizeBaselineValid[i]=NativeMethods.IsWindow(assigned[i]) && NativeMethods.VisibleRect(assigned[i],out resizeBaseline[i]);
        }
        int sourceSlot=Array.IndexOf(assigned,window);
        if(sourceSlot>=0) lastDrag=resizeBaseline[sourceSlot];
        var probe=new ResizeProbe(); resizeProbe=probe; var windows=(IntPtr[])resizeWindows.Clone(); var eligible=(bool[])resizeBaselineValid.Clone(); int count=grid.Profile.SlotCount;
        ThreadPool.QueueUserWorkItem(delegate {
            IntPtr previousContext=NativeMethods.SetThreadDpiAwarenessContext(new IntPtr(-4));
            try { for(int i=0;i<count && !probe.Cancelled;i++) if(eligible[i] && !NativeMethods.IsIconic(windows[i]) && !NativeMethods.IsZoomed(windows[i])) probe.Valid[i]=ReadMinimum(windows[i],out probe.Minimums[i]); }
            finally { if(previousContext!=IntPtr.Zero) NativeMethods.SetThreadDpiAwarenessContext(previousContext); probe.Ready=true; }
        });
        resizeTimer.Start(); WriteState();
    }
    void ApplyResizeFrames(bool includeSource) {
        for(int i=0;i<grid.Profile.SlotCount;i++) {
            IntPtr window=assigned[i]; RECT current;
            if(window!=IntPtr.Zero && NativeMethods.IsWindow(window) && !OnActiveMonitor(window)) {assigned[i]=IntPtr.Zero;continue;}
            if(!resizeBaselineValid[i] || window!=resizeWindows[i] || !NativeMethods.IsWindow(window)) continue;
            if((window==dragging && !includeSource) || NativeMethods.IsIconic(window) || NativeMethods.IsZoomed(window)) continue;
            if(!NativeMethods.VisibleRect(window,out current)) continue;
            RECT wanted=resizeBaseline[i],model=grid.RectFor(i);
            if(resizeWidths.Contains(i)) { wanted.L=model.L; wanted.R=model.R; }
            if(resizeHeights.Contains(i)) { wanted.T=model.T; wanted.B=model.B; }
            // Unaffected windows stay at their actual starting bounds. If
            // native Snap also resized them, undo only that unintended change.
            if(current.L!=wanted.L || current.R!=wanted.R || current.T!=wanted.T || current.B!=wanted.B) {
                if(!PlaceWindow(window,wanted,false,false)) { AbortResize(); return; }
            }
        }
    }
    void AbortResize() {
        IntPtr source=resizeSource;
        for(int i=0;i<grid.Profile.SlotCount;i++) if(resizeBaselineValid[i] && assigned[i]==resizeWindows[i] && NativeMethods.IsWindow(assigned[i]) && !NativeMethods.IsIconic(assigned[i]) && !NativeMethods.IsZoomed(assigned[i])) PlaceWindow(assigned[i],resizeBaseline[i],false,false);
        if(resizeStartProfile!=null) grid.Profile.Columns=resizeStartProfile.Columns.Select(c=>c.Clone()).ToList();
        int slot=Array.IndexOf(assigned,source); if(slot>=0) assigned[slot]=IntPtr.Zero;
        CancelResizeSession(); SaveSettings(); FitIssue("Linked resizing stopped because an app rejected its frame. Previous bounds were requested again. Snap the source window again to reconnect it.",true);
    }
    void UseProfile(string id) {
        if(id==grid.Profile.Id) return;
        CancelResizeSession(); workspace.SelectProfile(id); BindActive();
        RecoverAssignments(); workspace.Active.NeedsRecovery=false; SaveSettings();
    }
    public bool SnapTo(IntPtr window,string profileId,int slot) {
        if(!ValidTarget(window)) { tray.ShowBalloonTip(2500,"Choose a window first","Select the app you want to place, then open the layout flyout.",ToolTipIcon.Info); return false; }
        TargetMonitor(window);
        var template=settings.Profiles.FirstOrDefault(p=>p.Id==profileId); if(template==null || slot<0 || slot>=template.SlotCount) return false;
        var profile=workspace.Resolve(workspace.Active.Display.Id,profileId);
        CancelResizeSession();
        var work=workspace.Active.Display.Work; var desired=new LayoutModel(profile){Work=work}.RectFor(slot);
        bool wasMaximized=NativeMethods.IsZoomed(window),wasMinimized=NativeMethods.IsIconic(window);
        if(wasMaximized || wasMinimized) NativeMethods.ShowWindow(window,9);
        lastPlacementIssue=null;
        if(!PlaceWindow(window,desired,true)) { if(wasMaximized) NativeMethods.ShowWindow(window,3); else if(wasMinimized) NativeMethods.ShowWindow(window,6); return false; }
        UseProfile(profileId); ReleaseWindow(window);
        assigned[slot]=window; SaveSettings(); WriteState(); return true;
    }
    void UpdateDrag() {
        if(dragging==IntPtr.Zero) return;
        if(!NativeMethods.IsWindow(dragging)) { CancelResizeSession(); return; }
        if(!OnActiveMonitor(dragging)) {ReleaseWindow(dragging);CancelResizeSession();SaveSettings();WriteState();return;}
        int slot=Array.IndexOf(assigned,dragging); RECT current;
        if(slot<0 || !NativeMethods.VisibleRect(dragging,out current) || NativeMethods.IsZoomed(dragging) || NativeMethods.IsIconic(dragging)) return;
        if(resizeHeightBoundary<0 && Math.Abs((current.B-current.T)-(lastDrag.B-lastDrag.T))>1) {
            int column,row; grid.Locate(slot,out column,out row);
            bool top=row>0 && Math.Abs(current.T-lastDrag.T)>1,bottom=row<grid.Profile.Columns[column].Rows.Count-1 && Math.Abs(current.B-lastDrag.B)>1;
            if(top||bottom) resizeHeightBoundary=top && (!bottom || Math.Abs(current.T-lastDrag.T)>Math.Abs(current.B-lastDrag.B))?row:row+1;
        }
        if(resizeProbe==null || !resizeProbe.Ready) return;
        if(!resizeProbe.Applied) {
            for(int i=0;i<grid.Profile.SlotCount;i++) {
                if(resizeProbe.Valid[i]) { grid.MinimumWidths[i]=resizeProbe.Minimums[i].Width>0?resizeProbe.Minimums[i].Width+1:0; grid.MinimumHeights[i]=resizeProbe.Minimums[i].Height>0?resizeProbe.Minimums[i].Height+1:0; }
                else if(!NativeMethods.IsIconic(resizeWindows[i]) && !NativeMethods.IsZoomed(resizeWindows[i])) resizeBaselineValid[i]=false;
            }
            int source=Array.IndexOf(assigned,dragging);
            if(source<0 || !resizeBaselineValid[source]) { if(source>=0) assigned[source]=IntPtr.Zero; CancelResizeSession(); FitIssue("The source app did not respond to its size query. Snap it again after it responds.",true); return; }
            resizeProbe.Applied=true;
        }
        RECT proposed=current;
        if(Math.Abs((current.R-current.L)-(lastDrag.R-lastDrag.L))>1 || Math.Abs((current.B-current.T)-(lastDrag.B-lastDrag.T))>1) resizeAttempted=true;
        if(resizeHeightBoundary>=0) { int column,row; grid.Locate(slot,out column,out row); if(resizeHeightBoundary==row) proposed.B=lastDrag.B; else proposed.T=lastDrag.T; }
        if(grid.Adjust(slot,lastDrag,proposed)) {
            if(grid.WidthAffectedSlots.Concat(grid.HeightAffectedSlots).Any(i=>!resizeBaselineValid[i])) { AbortResize(); return; }
            if(resizeHeightBoundary<0 && grid.HeightAffectedSlots.Length==2) { int column,row; grid.Locate(grid.HeightAffectedSlots[0],out column,out row); resizeHeightBoundary=row+1; }
            changed=true; resizeWidths.UnionWith(grid.WidthAffectedSlots); resizeHeights.UnionWith(grid.HeightAffectedSlots);
        }
        if(grid.ConstraintBlocked) { AbortResize(); return; }
        if(changed) ApplyResizeFrames(false);
        // Advance only to the frame used for this adjustment. The app may move
        // again while we update neighbors; a fresh read here would consume that
        // movement without applying it, dropping the final edge change.
        if(dragging!=IntPtr.Zero) lastDrag=current;
    }
    void CompleteResize() {
        if(dragging==IntPtr.Zero || !resizeEnding || resizeProbe==null || !resizeProbe.Applied) return;
        IntPtr window=dragging; resizeTimer.Stop(); int slot=Array.IndexOf(assigned,window); RECT actual;
        if(slot>=0 && !changed && resizeAttempted && !NativeMethods.IsIconic(window) && !NativeMethods.IsZoomed(window)) {
            if(!PlaceWindow(window,resizeBaseline[slot],true,false)) { AbortResize(); return; }
            FitIssue("This divider reached its size limit. Choose a larger layout or adjust another boundary.",true);
        }
        if(slot>=0 && (NativeMethods.IsZoomed(window)||(!changed && !resizeAttempted && NativeMethods.VisibleRect(window,out actual) && !Near(actual,grid.RectFor(slot))))) {
            NativeMethods.VisibleRect(window,out actual);
            Log("Released moved/maximized resize source in zone "+(slot+1)+"; attempted="+resizeAttempted+"; baseline="+resizeBaseline[slot].L+","+resizeBaseline[slot].T+","+resizeBaseline[slot].R+","+resizeBaseline[slot].B+"; last="+lastDrag.L+","+lastDrag.T+","+lastDrag.R+","+lastDrag.B+"; actual="+actual.L+","+actual.T+","+actual.R+","+actual.B);
            assigned[slot]=IntPtr.Zero;
        }
        dragging=IntPtr.Zero; resizeEnding=false;
        if(changed) { ApplyResizeFrames(true); SaveSettings(); settlePasses=0; settleTimer.Start(); }
        WriteState();
    }
    void OnWindowEvent(IntPtr hook,uint evt,IntPtr window,int objectId,int childId,uint thread,uint time) {
        try {
            if(evt==0x8001 && objectId==0 && childId==0) {
                bool released=ReleaseWindow(window);
                if(released) { if(window==resizeSource) { SaveSettings(); CancelResizeSession(); } WriteState(); }
                return;
            }
            if(evt==3) { if(ValidTarget(window)) lastExternalWindow=window; return; }
            if(evt==10 && (dragging==IntPtr.Zero || !NativeMethods.IsWindow(dragging))) {
                // A grouped native resize must not transfer ownership of the
                // active height boundary to a flanking window.
                var owner=workspace.Sessions.Values.FirstOrDefault(s=>s.Connected && s.Windows.Contains(window));
                if(owner!=null) {
                    if(NativeMethods.MonitorFromWindow(window,2)!=owner.Display.Handle) {ReleaseWindow(window);WriteState();return;}
                    ActivateMonitor(owner.Display); BeginResizeSession(window);
                }
            }
            else if(evt==11 && window==dragging) {
                resizeEnding=true; UpdateDrag(); CompleteResize();
            }
        } catch(Exception e) { Log("Resize: "+e.Message); }
    }
    double DpiScale(IntPtr target) { uint dpi=NativeMethods.GetDpiForWindow(target); return dpi==0?1:dpi/96.0; }
    void RefreshLauncher() {
        if(launcherMenu!=null) {launcherMenu.Text=settings.ShowLauncherTab?"Hide Layouts bar to tray":"Show Layouts bar";launcherMenu.Checked=settings.ShowLauncherTab;}
        if(launcher!=null) { launcher.Dispose(); launcher=null; } if(!settings.ShowLauncherTab) return;
        IntPtr taskbar=NativeMethods.FindWindow("Shell_TrayWnd",null);
        IntPtr anchor=taskbar==IntPtr.Zero?NativeMethods.GetForegroundWindow():taskbar;
        var m=NativeMethods.Monitor(anchor); double scale=DpiScale(anchor); launcher=new LauncherTab(settings.FlyoutShortcut.ToString(),scale,settings.LauncherPositionLocked);
        launcher.OpenRequested=delegate { OpenFlyout(NativeMethods.GetForegroundWindow(),null); };
        launcher.HideRequested=delegate { SetLauncherVisible(false); };
        launcher.PositionChanged=delegate(Point location) { settings.LauncherPositionSet=true; settings.LauncherX=location.X; settings.LauncherY=location.Y; SaveSettings(); };
        int gap=Math.Max(4,(int)(8*scale));
        Point position=settings.LauncherPositionSet?new Point(settings.LauncherX,settings.LauncherY):new Point(m.Work.R-launcher.Width-gap,m.Work.B-launcher.Height-gap);
        var area=settings.LauncherPositionSet?DisplayInventory.ForPoint(position.X,position.Y).Work:m.Work;
        Rectangle work=Rectangle.FromLTRB(area.L,area.T,area.R,area.B);
        launcher.Location=LauncherTab.ClampPosition(position,launcher.Size,work); if(flyout==null) launcher.ShowUnfocused();
    }
    void ResetLauncherPosition() {
        settings.LauncherPositionSet=false;
        if(settingsForm!=null && !settingsForm.IsDisposed) settingsForm.Draft.LauncherPositionSet=false;
        SaveSettings(); RefreshLauncher(); WriteState();
    }
    void SetLauncherVisible(bool visible) {
        settings.ShowLauncherTab=visible;
        if(settingsForm!=null && !settingsForm.IsDisposed) settingsForm.SyncLauncherVisibility(visible);
        if(!visible && launcher!=null)launcher.Hide();
        if(visible)RefreshLauncher();
        if(launcherMenu!=null){launcherMenu.Text=visible?"Hide Layouts bar to tray":"Show Layouts bar";launcherMenu.Checked=visible;}
        SaveSettings();WriteState();
    }
    public void OpenFlyout(IntPtr target,string onlyProfile) {
        if(workspaceForm!=null&&!workspaceForm.IsDisposed){workspaceForm.Activate();return;}
        if(settingsForm!=null && !settingsForm.IsDisposed) { settingsForm.Activate(); return; }
        if(flyout!=null && flyout.Visible) { HideFlyout(); return; }
        if(!ValidTarget(target) && ValidTarget(lastExternalWindow)) target=lastExternalWindow;
        if(!ValidTarget(target)) { tray.ShowBalloonTip(2500,"Select an app window","Select a window, then press "+settings.FlyoutShortcut+".",ToolTipIcon.Info); return; }
        TargetMonitor(target); workspace.Capture();
        targetWindow=target; var monitor=NativeMethods.Monitor(target); var visibleSettings=settings.Clone();
        visibleSettings.Profiles=settings.Profiles.Select(p=>workspace.Resolve(workspace.Active.Display.Id,p.Id)).ToList();
        if(onlyProfile!=null) visibleSettings.Profiles=visibleSettings.Profiles.Where(p=>p.Id==onlyProfile).ToList();
        flyout=new SnapFlyout(visibleSettings,ProcessName(target),DpiScale(target),monitor.Work.R-monitor.Work.L,monitor.Work.B-monitor.Work.T);
        flyout.Location=new Point(monitor.Work.L+(monitor.Work.R-monitor.Work.L-flyout.Width)/2,monitor.Work.T+Math.Max(8,(int)(24*DpiScale(target))));
        flyout.ZoneSelected=delegate(string id,int slot) { IntPtr chosen=targetWindow; HideFlyout(); if(SnapTo(chosen,id,slot)) NativeMethods.SetForegroundWindow(chosen); };
        flyout.ManageRequested=delegate { HideFlyout(); OpenSettings(); }; flyout.DismissRequested=HideFlyout;
        if(launcher!=null) launcher.Hide(); mouseWasDown=MouseDown(); escapeWasDown=Down(27); escapeRegistered=NativeMethods.RegisterHotKey(receiver.Handle,200,0x4000,27);
        RegisterNavigation(); flyout.ShowUnfocused(); flyoutTimer.Start(); WriteState();
    }
    public void HideFlyout() {
        flyoutTimer.Stop(); ReleaseNavigation(); if(escapeRegistered) NativeMethods.UnregisterHotKey(receiver.Handle,200); escapeRegistered=false;
        if(flyout!=null) { var old=flyout; flyout=null; old.Hide(); old.Dispose(); }
        if(launcher!=null && !launcher.IsDisposed && !disposed && settings.ShowLauncherTab) launcher.ShowUnfocused(); WriteState();
    }
    void PollFlyout() {
        if(flyout==null) return; bool mouse=MouseDown(),escape=Down(27);
        if(NativeMethods.GetForegroundWindow()!=targetWindow) { HideFlyout(); return; }
        if((mouse && !mouseWasDown && !flyout.Bounds.Contains(Cursor.Position)) || (escape && !escapeWasDown)) { HideFlyout(); return; }
        mouseWasDown=mouse; escapeWasDown=escape;
    }
    public string ApplySettings(AppSettings draft) {
        if(settingsMonitorId!=null) {
            MonitorSession editorMonitor;
            if(!workspace.Sessions.TryGetValue(settingsMonitorId,out editorMonitor) || !editorMonitor.Connected) return "The display being edited was disconnected. Reconnect it before saving, or cancel and reopen settings for a connected display.";
            ActivateMonitor(editorMonitor.Display);
        }
        workspace.PrepareDraft(draft,settingsOpened);
        draft.Validate(); HideFlyout(); CancelResizeSession(); var previousBindings=hotkeys.Snapshot(); string error; if(!hotkeys.Replace(draft,out error)) return error;
        try { SettingsStore.Save(settingsPath,draft); } catch(Exception e) { string rollback=hotkeys.Restore(previousBindings); return "Could not save settings: "+e.Message+" Check the data folder is writable and not locked by another program."+(rollback==null?"":" "+rollback); }
        bool same=grid.Profile.Id==draft.ActiveProfileId;
        settings=draft; preserveInvalidSettings=false; recoveryNotice=null;
        bool geometryChanged=workspace.Rebind(settings); BindActive();
        if(same && geometryChanged) ApplyLayout();
        tray.ContextMenuStrip.Items[0].Text="Choose layout  "+settings.FlyoutShortcut;
        RefreshLauncher(); WriteState(); return null;
    }
    public void OpenSettings(bool general=false) {
        if(workspaceForm!=null&&!workspaceForm.IsDisposed){workspaceForm.Activate();return;}
        HideFlyout(); if(settingsForm!=null && !settingsForm.IsDisposed) { if(general)settingsForm.SelectGeneral();settingsForm.Activate(); return; }
        workspace.Capture(); settingsMonitorId=workspace.Active.Display.Id; settingsOpened=settings.Clone();
        settingsOpened.Profiles=settings.Profiles.Select(p=>workspace.Resolve(settingsMonitorId,p.Id)).ToList();
        try {
            using(var form=new SnapSettingsForm(settingsOpened,recoveryNotice,new StartupRegistration(startupLaunch,new UserStartupValue()),folder)) { settingsForm=form; form.DisplayContext(workspace.Active.Display.Name); form.SaveRequested=ApplySettings;if(general&&recoveryNotice==null)form.SelectGeneral(); ShowManagedDialog(form); }
        } finally {settingsForm=null;settingsMonitorId=null;settingsOpened=null;}
    }
    void HandleCommand(int message,IntPtr a,IntPtr b) {
        try {
            if(workspaceForm!=null&&!workspaceForm.IsDisposed&&(message==ShowCommand||message==SnapCommand||message==ProfileCommand||message==0x312)){workspaceForm.Activate();return;}
            if(message==0x7e || message==0x1a || message==0x218) {
                if(message==0x218 && a.ToInt32()==4) {displayTimer.Stop();CancelResizeSession();HideFlyout();SaveSettings();}
                else if(message!=0x218 || a.ToInt32()==0x12 || a.ToInt32()==7) {displayTimer.Stop();displayTimer.Start();}
            } else if(message==0x312) {
                int id=a.ToInt32(); if(id==100) OpenFlyout(NativeMethods.GetForegroundWindow(),null); else if(id==200) HideFlyout();
                else if(id>=201 && id<=207 && flyout!=null && navigationHotkeys.Contains(id)) flyout.Navigate(navigationKeys[id-201],id==206);
                else if(id>=1 && id<=16) { IntPtr target=flyout==null?NativeMethods.GetForegroundWindow():targetWindow; HideFlyout(); TargetMonitor(target); if(id<=grid.Profile.SlotCount) SnapTo(target,grid.Profile.Id,id-1); }
                else if(id>=300 && id<300+settings.Profiles.Count) OpenFlyout(NativeMethods.GetForegroundWindow(),settings.Profiles[id-300].Id);
            } else if(message==ShowCommand) OpenFlyout(b==IntPtr.Zero?NativeMethods.GetForegroundWindow():b,null);
            else if(message==SettingsCommand) OpenSettings(); else if(message==WorkspacesCommand)OpenWorkspaces(); else if(message==QuitCommand) ExitThread();
            else if(message==SnapCommand) { HideFlyout(); TargetMonitor(b); SnapTo(b,grid.Profile.Id,a.ToInt32()-1); }
            else if(message==ProfileCommand && a.ToInt32()>=0 && a.ToInt32()<settings.Profiles.Count) { HideFlyout(); TargetMonitor(b==IntPtr.Zero?NativeMethods.GetForegroundWindow():b); UseProfile(settings.Profiles[a.ToInt32()].Id); WriteState(); }
        } catch(Exception e) { Log("Command: "+e); tray.ShowBalloonTip(2500,"Windows Tile Studio",e.Message,ToolTipIcon.Warning); }
    }
    public SavedWorkspace CaptureWorkspace(string name,bool matchTitles) {
        var result=new SavedWorkspace{Name=name,MatchTitles=matchTitles};workspace.Capture();
        foreach(var session in workspace.Sessions.Values.Where(s=>s.Connected)) {
            var display=new SavedWorkspaceDisplay{MonitorId=session.Display.Id,DisplayName=session.Display.Name,Layout=session.Model.Profile.Clone()};display.Layout.Shortcut=new Shortcut();
            for(int slot=0;slot<session.Model.Profile.SlotCount;slot++) {
                var window=session.Windows[slot];if(!ValidTarget(window)||NativeMethods.MonitorFromWindow(window,2)!=session.Display.Handle)continue;var live=WorkspaceApps.Read(window);if(live==null)continue;
                display.Windows.Add(new SavedPlacement{ApplicationPath=live.ApplicationPath,WindowClass=live.WindowClass,WindowTitle=matchTitles?live.Title:null,Slot=slot});
            }
            if(display.Windows.Count>0)result.Displays.Add(display);
        }
        result.Validate();return result;
    }
    public WorkspaceRestorePlan PreviewWorkspace(SavedWorkspace saved,bool fallback) {
        var windows=new List<LiveWorkspaceWindow>();NativeMethods.EnumWindows(delegate(IntPtr window,IntPtr unused){if(ValidTarget(window)){var live=WorkspaceApps.Read(window);if(live!=null)windows.Add(live);}return true;},IntPtr.Zero);
        return WorkspacePlanner.Build(saved,windows,DisplayInventory.Connected(),settings.Profiles,fallback?workspace.Active.Display.Id:null);
    }
    public string RestoreWorkspace(WorkspaceRestorePlan plan) {
        var selected=plan.SelectedRows();var displays=DisplayInventory.Connected();int restored=0,skipped=0;var issues=new List<string>();var before=workspace.Active;
        CancelResizeSession();HideFlyout();
        foreach(var group in selected.GroupBy(r=>r.Target.Id,StringComparer.OrdinalIgnoreCase)) {
            bool prepared=false;
            foreach(var row in group) {
                var target=displays.FirstOrDefault(d=>string.Equals(d.Id,row.Target.Id,StringComparison.OrdinalIgnoreCase));var profile=settings.Profiles.FirstOrDefault(p=>p.Id==row.LayoutId);var live=WorkspaceApps.Read(row.Chosen.Handle);
                if(target==null||profile==null||!MonitorWorkspace.SameShape(profile,row.SavedDisplay.Layout)||!ValidTarget(row.Chosen.Handle)||!WorkspaceApps.SameInstance(row.Chosen,live)||!WorkspaceApps.Matches(row.Saved,live,false)||(plan.Workspace.MatchTitles&&live.Title!=row.Chosen.Title)) {skipped++;issues.Add("A display, layout or window changed; refresh the preview.");continue;}
                var geometry=row.SavedDisplay.Layout.Clone();geometry.Id=profile.Id;geometry.Name=profile.Name;geometry.Shortcut=profile.Shortcut.Clone();geometry.Validate();
                var wanted=new LayoutModel(geometry){Work=target.Work}.RectFor(row.Saved.Slot);bool maximized=NativeMethods.IsZoomed(live.Handle),minimized=NativeMethods.IsIconic(live.Handle);
                if(maximized||minimized)NativeMethods.ShowWindow(live.Handle,9);
                if(!PlaceWindow(live.Handle,wanted,false)){if(maximized)NativeMethods.ShowWindow(live.Handle,3);else if(minimized)NativeMethods.ShowWindow(live.Handle,6);skipped++;issues.Add("An app rejected its requested frame.");continue;}
                ActivateMonitor(target);
                if(!prepared) {
                    if(workspace.Active.Model.Profile.Id!=geometry.Id||!workspace.Active.Model.Profile.GeometryMatches(geometry)){workspace.Active.Model=new LayoutModel(geometry){Work=target.Work};workspace.Active.Windows=new IntPtr[16];}
                    workspace.Active.NeedsRecovery=false;BindActive();prepared=true;
                }
                ReleaseWindow(live.Handle);assigned[row.Saved.Slot]=live.Handle;restored++;
            }
        }
        if(before.Connected)ActivateMonitor(before.Display);SaveSettings();WriteState();
        string summary="Restored "+restored+" windows; skipped "+skipped+". No apps were launched.";if(issues.Count>0)summary+=" "+string.Join(" ",issues.Distinct());Log(summary);return summary;
    }
    public void OpenWorkspaces() {
        HideFlyout();if(settingsForm!=null&&!settingsForm.IsDisposed){settingsForm.Activate();return;}if(workspaceForm!=null&&!workspaceForm.IsDisposed){workspaceForm.Activate();return;}
        try {using(var form=new WorkspaceDialog(Path.Combine(folder,"workspaces.xml"),CaptureWorkspace,PreviewWorkspace,RestoreWorkspace,WorkspaceApps.Launch)){workspaceForm=form;ShowManagedDialog(form);}}
        finally {workspaceForm=null;}
    }
    static void ShowManagedDialog(Form form) {
        // A process launched with STARTUPINFO/SW_HIDE can hide its first Form.
        // Queue an explicit show after that initial native show has completed.
        form.Load+=delegate {form.BeginInvoke((Action)delegate {if(!form.IsDisposed){NativeMethods.ShowWindow(form.Handle,5);form.Activate();}});};
        form.ShowDialog();
    }
    public SixSlotSnap(string dataDirectory,bool recoverWindows,string sourceDirectory=null) {
        folder=dataDirectory; Directory.CreateDirectory(folder); settingsPath=Path.Combine(folder,"settings.xml"); recover=recoverWindows;
        string assembly=typeof(SixSlotSnap).Assembly.Location;
        startupLaunch=new StartupLaunchSpec{Executable=assembly,DataDirectory=folder};
        if(string.IsNullOrEmpty(assembly)){startupLaunch.Executable=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"System32","WindowsPowerShell","v1.0","powershell.exe");if(sourceDirectory==null)startupLaunch.UnavailableReason="Hosted source location is unknown. Start with Start-Hosted.cmd before enabling login startup.";else startupLaunch.SourceScript=Path.Combine(sourceDirectory,"Start-Hosted.ps1");}
        NativeMethods.SetThreadDpiAwarenessContext(new IntPtr(-4)); string warning; settings=SettingsStore.Load(settingsPath,out warning); preserveInvalidSettings=warning!=null;
        if(!File.Exists(settingsPath)) SettingsStore.MigrateLegacy(Path.Combine(folder,"layout.txt"),settings);
        settings.Validate(); workspace=new MonitorWorkspace(settings); workspace.UpdateDisplays(DisplayInventory.Connected()); workspace.Activate(DisplayInventory.ForWindow(NativeMethods.GetForegroundWindow())); BindActive();
        receiver=new CommandWindow(); hotkeys=new HotkeyRegistry(receiver.Handle); string conflict;
        recoveryNotice=warning;
        if(!hotkeys.Replace(settings,out conflict)) { preserveInvalidSettings=true; recoveryNotice=(warning==null?"":warning+"\r\n\r\n")+"Startup shortcuts are unavailable. "+conflict+" No replacement bindings were kept. Review Hotkeys and save an available combination."; }
        var menu=new ContextMenuStrip();
        menu.Items.Add("Choose layout  "+settings.FlyoutShortcut,null,delegate { OpenFlyout(NativeMethods.GetForegroundWindow(),null); });
        menu.Items.Add("Manage layouts & hotkeys",null,delegate { OpenSettings(); });
        menu.Items.Add("General settings",null,delegate {OpenSettings(true);});
        menu.Items.Add("Workspaces",null,delegate {OpenWorkspaces();});
        launcherMenu=(ToolStripMenuItem)menu.Items.Add("Show Layouts bar",null,delegate {SetLauncherVisible(!settings.ShowLauncherTab);});
        menu.Items.Add("Reset Layouts bar position",null,delegate { ResetLauncherPosition(); });
        menu.Items.Add("Reset active layout proportions",null,delegate { if(MessageBox.Show("Reset this layout to equal columns and rows? Its assigned windows will be resized. Choose No to keep the current proportions.","Reset layout proportions?",MessageBoxButtons.YesNo,MessageBoxIcon.Question,MessageBoxDefaultButton.Button2)!=DialogResult.Yes) return; CancelResizeSession(); foreach(var c in grid.Profile.Columns) { c.Width=1.0/grid.Profile.Columns.Count; for(int r=0;r<c.Rows.Count;r++) c.Rows[r]=1.0/c.Rows.Count; } ApplyLayout(); SaveSettings(); WriteState(); });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("About Windows Tile Studio",null,delegate { MessageBox.Show("Windows Tile Studio "+ProductVersion+"\n\nCustom layouts, keyboard placement, and linked resizing.\n\nRuns locally. No AI service or account is required to use the controller.","About Windows Tile Studio",MessageBoxButtons.OK,MessageBoxIcon.Information); });
        menu.Items.Add("Exit",null,delegate { ExitThread(); });
        tray=new NotifyIcon{Icon=CreateStudioIcon(),Text="Windows Tile Studio — layouts and hotkeys",ContextMenuStrip=menu,Visible=true}; tray.DoubleClick+=delegate { OpenSettings(true); }; receiver.Received=HandleCommand;
        resizeTimer.Interval=30; resizeTimer.Tick+=delegate { try { UpdateDrag(); CompleteResize(); } catch(Exception e) { Log("Resize tick: "+e.Message); } };
        settleTimer.Interval=100; settleTimer.Tick+=delegate { try { if(dragging==IntPtr.Zero) ApplyResizeFrames(true); } catch(Exception e) { Log("Settle: "+e.Message); } if(++settlePasses>=3) settleTimer.Stop(); };
        flyoutTimer.Interval=20; flyoutTimer.Tick+=delegate { PollFlyout(); };
        displayTimer.Interval=350; displayTimer.Tick+=delegate { RefreshDisplays(); };
        IntPtr foreground=NativeMethods.GetForegroundWindow(); if(ValidTarget(foreground)) lastExternalWindow=foreground;
        eventCallback=OnWindowEvent; eventHook=NativeMethods.SetWinEventHook(3,11,IntPtr.Zero,eventCallback,0,0,0);
        destroyHook=NativeMethods.SetWinEventHook(0x8001,0x8001,IntPtr.Zero,eventCallback,0,0,0);
        if(destroyHook==IntPtr.Zero) Log("Closed-window event hook unavailable; dead handles are filtered when runtime state is next written.");
        if(eventHook==IntPtr.Zero) { tray.ShowBalloonTip(4000,"Linked resizing unavailable","Windows resize notifications could not be registered.",ToolTipIcon.Warning); Log("ERROR: move/size hook registration failed."); }
        var initial=workspace.Active;
        foreach(var session in workspace.Sessions.Values.ToArray()) {workspace.Activate(session.Display);BindActive();RecoverAssignments();session.NeedsRecovery=false;}
        workspace.Activate(initial.Display);BindActive(); SaveSettings(); RefreshLauncher(); WriteState(); Log("Ready "+ProductVersion+"; "+settings.Profiles.Count+" layouts; flyout "+settings.FlyoutShortcut+".");
        if(warning!=null) { Log(warning); tray.ShowBalloonTip(5000,"Settings recovery",warning,ToolTipIcon.Warning); }
        if(recoveryNotice!=null) { Log("Recovery required: "+recoveryNotice); NativeMethods.PostMessage(receiver.Handle,SettingsCommand,IntPtr.Zero,IntPtr.Zero); }
    }
    protected override void ExitThreadCore() {
        if(disposed) return;
        if(settingsForm!=null && !settingsForm.IsDisposed) { var open=settingsForm; open.DialogResult=DialogResult.Cancel; open.Close(); if(!open.IsDisposed && open.DialogResult==DialogResult.None) return; }
        if(workspaceForm!=null&&!workspaceForm.IsDisposed)workspaceForm.Close();
        disposed=true;
        resizeTimer.Stop(); settleTimer.Stop(); displayTimer.Stop(); SaveSettings(); HideFlyout(); resizeTimer.Dispose(); settleTimer.Dispose(); flyoutTimer.Dispose(); displayTimer.Dispose();
        if(eventHook!=IntPtr.Zero) NativeMethods.UnhookWinEvent(eventHook); if(destroyHook!=IntPtr.Zero) NativeMethods.UnhookWinEvent(destroyHook); hotkeys.Dispose(); receiver.DestroyHandle(); if(launcher!=null) launcher.Dispose(); tray.Visible=false; var ownedIcon=tray.Icon; tray.Dispose(); if(ownedIcon!=null) ownedIcon.Dispose(); Log("Stopped."); base.ExitThreadCore();
    }
    [STAThread] public static void Main(string[] args) {
        NativeMethods.SetThreadDpiAwarenessContext(new IntPtr(-4)); Application.EnableVisualStyles();
        string folder=AppDomain.CurrentDomain.BaseDirectory; int data=Array.IndexOf(args,"--data-dir"); if(data>=0 && data+1<args.Length) folder=Path.GetFullPath(args[data+1]);
        string command=args.FirstOrDefault(a=>a=="--show"||a=="--settings"||a=="--workspaces"||a=="--quit"||a=="--snap"||a=="--layout");
        if(command!=null) {
            IntPtr existing=NativeMethods.FindWindow(null,ControllerCaption); if(existing==IntPtr.Zero) { MessageBox.Show("Start Windows Tile Studio before sending a command.","Windows Tile Studio"); return; }
            int message=command=="--show"?ShowCommand:command=="--settings"?SettingsCommand:command=="--workspaces"?WorkspacesCommand:command=="--quit"?QuitCommand:command=="--snap"?SnapCommand:ProfileCommand;
            int number=0,at=Array.IndexOf(args,command); if((message==SnapCommand||message==ProfileCommand) && (at+1>=args.Length||!int.TryParse(args[at+1],out number))) return;
            NativeMethods.PostMessage(existing,message,new IntPtr(number),NativeMethods.GetForegroundWindow()); return;
        }
        if(args.Contains("--preview")) {
            Directory.CreateDirectory(folder); using(var form=new SnapFlyout(AppSettings.Defaults(),"Example window",1.25,5120,2160)) using(var image=new Bitmap(form.Width,form.Height)) { form.StartPosition=FormStartPosition.Manual; form.Location=new Point(-30000,-30000); form.ShowUnfocused(); Application.DoEvents(); form.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size)); image.Save(Path.Combine(folder,"flyout-preview.png")); } return;
        }
        bool created; using(var mutex=new Mutex(true,"Local\\SixSlotSnapController",out created)) {
            if(!created) return;
            int source=Array.IndexOf(args,"--source-root");string sourceRoot=source>=0&&source+1<args.Length?Path.GetFullPath(args[source+1]):null;
            try { Application.Run(new SixSlotSnap(folder,!args.Contains("--no-recover"),sourceRoot)); }
            catch(Exception e) { Directory.CreateDirectory(folder); File.AppendAllText(Path.Combine(folder,"status.log"),e+Environment.NewLine); MessageBox.Show(e.Message,"Windows Tile Studio could not start"); Environment.ExitCode=1; }
        }
    }
}
