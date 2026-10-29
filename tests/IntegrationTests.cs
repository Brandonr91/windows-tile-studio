using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;

// Opt-in interactive test. Uses disposable windows in this process and a
// separate controller process. Never adopts or rearranges existing apps.
class IntegrationTests {
    class SlowMinimumForm : Form {
        public bool DelayMinimum;
        protected override void WndProc(ref Message message) { if(DelayMinimum && message.Msg==0x24) Thread.Sleep(200); base.WndProc(ref message); }
    }
    [DllImport("user32.dll")] static extern void NotifyWinEvent(uint evt,IntPtr window,int objectId,int childId);
    [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr parent,NativeMethods.EnumProc callback,IntPtr parameter);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr window,StringBuilder text,int max);
    [DllImport("user32.dll")] static extern IntPtr GetParent(IntPtr window);
    [DllImport("user32.dll",SetLastError=true)] static extern IntPtr SendMessageTimeout(IntPtr window,uint message,IntPtr wparam,IntPtr lparam,uint flags,uint timeout,out IntPtr result);
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] static extern bool AttachThreadInput(uint first,uint second,bool attach);
    [DllImport("user32.dll")] static extern uint GetGuiResources(IntPtr process,uint flags);
    static readonly List<string> passed=new List<string>();
    static string data;
    static IntPtr receiver;
    static Process controller;
    static Form[] forms;
    static string scenario="base";
    static void Pump(int milliseconds) { var end=DateTime.UtcNow.AddMilliseconds(milliseconds); while(DateTime.UtcNow<end) { Application.DoEvents(); Thread.Sleep(10); } }
    static void Check(bool good,string text) { if(!good) throw new Exception(text); }
    static void Wait(Func<bool> ready,string error,int seconds=8) { var end=DateTime.UtcNow.AddSeconds(seconds); while(DateTime.UtcNow<end) { Application.DoEvents(); try { if(ready()) return; } catch(IOException) {} catch(System.Xml.XmlException) {} Thread.Sleep(20); } throw new Exception(error); }
    static XElement State() { using(var stream=new FileStream(Path.Combine(data,"runtime-state.xml"),FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)) return XElement.Load(stream); }
    static bool Open() { return (bool)State().Attribute("flyoutVisible"); }
    static void Command(int message,int value,IntPtr target) { Check(NativeMethods.PostMessage(receiver,message,new IntPtr(value),target),"Could not send controller command"); Pump(160); }
    static void BeginResize(IntPtr window) { NotifyWinEvent(10,window,0,0); Wait(()=>((bool?)State().Attribute("resizing")??false) && (long?)State().Attribute("resizeSource")==window.ToInt64(),"Controller did not acknowledge resize ownership"); }
    static SixSlotSnap.RECT Bounds(IntPtr w) { SixSlotSnap.RECT rect; Check(NativeMethods.VisibleRect(w,out rect),"Cannot read test window bounds"); return rect; }
    static bool Equal(SixSlotSnap.RECT a,SixSlotSnap.RECT b) { return Math.Abs(a.L-b.L)<=1 && Math.Abs(a.R-b.R)<=1 && Math.Abs(a.T-b.T)<=1 && Math.Abs(a.B-b.B)<=1; }
    static LayoutModel Model() { string warning; var s=SettingsStore.Load(Path.Combine(data,"settings.xml"),out warning); Check(warning==null,"Test settings invalid: "+warning); return new LayoutModel(s.Profiles.First(p=>p.Id==s.ActiveProfileId)){Work=NativeMethods.Monitor(forms[0].Handle).Work}; }
    static void CheckAll() { Wait(()=>!((bool?)State().Attribute("resizing")??false),"Resize session did not finish"); var g=Model(); for(int i=0;i<forms.Length;i++) { var actual=Bounds(forms[i].Handle); var wanted=g.RectFor(i); Check(Equal(actual,wanted),scenario+": linked bounds wrong for zone "+(i+1)+"; actual "+actual.L+","+actual.T+","+actual.R+","+actual.B+" wanted "+wanted.L+","+wanted.T+","+wanted.R+","+wanted.B); } }
    static void Move(int index,SixSlotSnap.RECT wanted) {
        SixSlotSnap.RECT outer; NativeMethods.GetWindowRect(forms[index].Handle,out outer); var v=Bounds(forms[index].Handle);
        int l=v.L-outer.L,t=v.T-outer.T,r=outer.R-v.R,b=outer.B-v.B;
        Check(NativeMethods.SetWindowPos(forms[index].Handle,IntPtr.Zero,wanted.L-l,wanted.T-t,wanted.R-wanted.L+l+r,wanted.B-wanted.T+t+b,0x14),"Cannot resize test window");
    }
    static void Focus(IntPtr target) {
        var until=DateTime.UtcNow.AddSeconds(2);
        do {
            uint process; uint other=NativeMethods.GetWindowThreadProcessId(NativeMethods.GetForegroundWindow(),out process),own=GetCurrentThreadId();
            bool attached=other!=own && other!=0 && AttachThreadInput(own,other,true);
            try {NativeMethods.ShowWindow(target,5);NativeMethods.SetForegroundWindow(target);}
            finally {if(attached)AttachThreadInput(own,other,false);}
            Pump(100);
            if(NativeMethods.GetForegroundWindow()==target)return;
        } while(DateTime.UtcNow<until);
    }
    static void Click(IntPtr window) { IntPtr result; Check(SendMessageTimeout(window,0xF5,IntPtr.Zero,IntPtr.Zero,2,2000,out result)!=IntPtr.Zero,"Test button click failed"); Pump(160); }
    static string Text(IntPtr w) { var text=new StringBuilder(256); GetWindowText(w,text,text.Capacity); return text.ToString(); }
    static IntPtr ZoneButton(IntPtr flyout,string layout,string zone) {
        IntPtr card=IntPtr.Zero,button=IntPtr.Zero;
        EnumChildWindows(flyout,delegate(IntPtr w,IntPtr ignored) { if(Text(w)==layout) card=GetParent(w); return true; },IntPtr.Zero);
        Check(card!=IntPtr.Zero,"Layout card missing: "+layout);
        EnumChildWindows(card,delegate(IntPtr w,IntPtr ignored) { if(Text(w)==zone) button=w; return true; },IntPtr.Zero);
        Check(button!=IntPtr.Zero,"Zone button missing"); return button;
    }
    static Control Find(Control root,Func<Control,bool> match) {
        if(match(root)) return root;
        foreach(Control child in root.Controls) { var result=Find(child,match); if(result!=null) return result; }
        return null;
    }
    static void TestEditor() {
        using(var navigation=new SnapFlyout(AppSettings.Defaults(),"Disposable target",1.25,5120,2160)) {
            string selected=null;int zone=-1;navigation.ZoneSelected=delegate(string id,int slot){selected=id;zone=slot;};
            foreach(var key in new[]{Keys.Right,Keys.Down,Keys.Left,Keys.Up,Keys.Left,Keys.Up}) navigation.Navigate(key,false);
            navigation.Navigate(Keys.Enter,false);Check(selected=="six-grid" && zone==0,"Four-way flyout navigation/edge clamp failed");
            for(int i=0;i<6;i++) navigation.Navigate(Keys.Tab,false);
            navigation.Navigate(Keys.Enter,false);Check(selected=="six-grid" && zone==0,"Forward layout wrap failed");
            navigation.Navigate(Keys.Tab,true);navigation.Navigate(Keys.Enter,false);Check(selected=="wide-center" && zone==0,"Reverse layout wrap failed");
        }
        passed.Add("Direct flyout navigation checks all four directions, edge clamping, and forward/reverse layout wrapping.");
        var original=AppSettings.Defaults(); bool saved=false;
        original.LauncherPositionSet=true; original.LauncherX=456; original.LauncherY=789;
        using(var cancelled=new SnapSettingsForm(original)) {
            cancelled.StartPosition=FormStartPosition.Manual; cancelled.Location=new Point(-30000,-30000); cancelled.ShowInTaskbar=false; cancelled.Show();
            ((TabControl)Find(cancelled,c=>c is TabControl)).SelectedIndex=2; Pump(30);
            ((TabControl)Find(cancelled,c=>c is TabControl&&c.AccessibleName=="General settings categories")).SelectedIndex=1;Pump(30);
            ((Button)Find(cancelled,c=>c is Button && c.AccessibleName=="Reset Layouts bar position")).PerformClick();
            Check(!cancelled.Draft.LauncherPositionSet && original.LauncherPositionSet && original.LauncherX==456,"Resetting the draft changed live settings before save");
        }
        using(var editor=new SnapSettingsForm(original)) {
            editor.DisplayContext("Simulated display");
            editor.ConfirmAction=delegate { return true; };
            editor.StartPosition=FormStartPosition.Manual; editor.Location=new Point(-30000,-30000); editor.ShowInTaskbar=false; editor.Show(); Pump(50);
            Check(((TabControl)Find(editor,c=>c is TabControl)).Top>=36,"Monitor context overlaps the settings tabs");
            ((Button)Find(editor,c=>c is Button && c.Text=="Add layout")).PerformClick();
            ((TextBox)Find(editor,c=>c is TextBox && c.AccessibleName=="Layout name")).Text="Editor acceptance test";
            ((NumericUpDown)Find(editor,c=>c is NumericUpDown && c.AccessibleName=="Number of columns")).Value=2;
            ((NumericUpDown)Find(editor,c=>c is NumericUpDown && c.AccessibleName=="Default rows per column")).Value=3;
            ((Button)Find(editor,c=>c is Button && c.Text=="Set grid size")).PerformClick();
            var custom=editor.Draft.Profiles.Last();
            Check(custom.Name=="Editor acceptance test" && custom.Columns.Count==2 && custom.Columns.All(c=>c.Rows.Count==3),"Grid-size edit lost the name or dimensions");
            ((Button)Find(editor,c=>c is Button && c.AccessibleName=="Undo last grid change")).PerformClick();
            Check(custom.Columns.Count==3 && custom.Columns.All(c=>c.Rows.Count==2),"Undo did not restore the previous grid");
            editor.ConfirmAction=delegate { return false; };
            ((NumericUpDown)Find(editor,c=>c is NumericUpDown && c.AccessibleName=="Number of columns")).Value=2;
            ((NumericUpDown)Find(editor,c=>c is NumericUpDown && c.AccessibleName=="Default rows per column")).Value=3;
            ((Button)Find(editor,c=>c is Button && c.Text=="Set grid size")).PerformClick();
            Check(custom.Columns.Count==3,"Declining grid replacement changed the grid");
            editor.ConfirmAction=delegate { return true; };
            ((Button)Find(editor,c=>c is Button && c.Text=="Set grid size")).PerformClick();
            var tabs=(TabControl)Find(editor,c=>c is TabControl); tabs.SelectedIndex=2; Pump(30);
            var general=(TabControl)Find(editor,c=>c is TabControl&&c.AccessibleName=="General settings categories");general.SelectedTab=general.TabPages.Cast<TabPage>().Single(p=>p.Text=="Layouts bar");Pump(30);
            ((Button)Find(editor,c=>c is Button && c.AccessibleName=="Reset Layouts bar position")).PerformClick();
            ((CheckBox)Find(editor,c=>c is CheckBox && c.AccessibleName=="Show floating Layouts bar")).Checked=false;
            Check(!editor.Draft.LauncherPositionSet && original.LauncherPositionSet,"Appearance reset was not isolated to the draft");
            string saveError=null; editor.ReportError=delegate(string title,string message) { saveError=message; };
            editor.SaveRequested=delegate { return "Test: data folder is locked."; };
            ((Button)Find(editor,c=>c is Button && c.Text=="Save changes")).PerformClick();
            Check(editor.Visible && saveError!=null && original.ShowLauncherTab,"Failed save closed the draft or changed original settings");
            editor.SaveRequested=delegate(AppSettings draft) { SettingsStore.Save(Path.Combine(data,"editor-test-settings.xml"),draft); saved=true; return null; };
            ((Button)Find(editor,c=>c is Button && c.Text=="Save changes")).PerformClick();
            Check(saved && original.Profiles.Count==6,"Editor save failed or changed the input settings before save");
            string appearanceWarning; var appearance=SettingsStore.Load(Path.Combine(data,"editor-test-settings.xml"),out appearanceWarning);
            Check(appearanceWarning==null && !appearance.LauncherPositionSet && !appearance.ShowLauncherTab,"Appearance settings were not saved");
        }
        using(var closing=new SnapSettingsForm(original)) {
            closing.StartPosition=FormStartPosition.Manual; closing.Location=new Point(-30000,-30000); closing.ShowInTaskbar=false; closing.Show();
            ((TextBox)Find(closing,c=>c is TextBox && c.AccessibleName=="Layout name")).Text="Unsaved name";
            closing.ConfirmAction=delegate { return false; }; closing.Close();
            Check(closing.Visible && !closing.IsDisposed,"Keep editing did not cancel closing");
            closing.ConfirmAction=delegate { return true; }; closing.Close();
            Check(!closing.Visible && original.Profiles[0].Name!="Unsaved name","Discard did not close without changing original settings");
        }
        passed.Add("Settings UI saves custom layouts and Appearance visibility/reset; draft cancellation preserves original launcher settings.");
        passed.Add("Grid replacement confirmation/undo, failed-save draft retention, and discard/keep-editing close safeguards pass.");
    }
    static void TestStartupRecovery(string directory,bool reserveShortcut,bool futureFields=false) {
        string priorData=data; data=directory; Directory.CreateDirectory(data);
        string settingsPath=Path.Combine(data,"settings.xml"); var initial=AppSettings.Defaults(); initial.ShowLauncherTab=false;
        if(reserveShortcut) SettingsStore.Save(settingsPath,initial); else File.WriteAllText(settingsPath,"unreadable settings fixture");
        if(futureFields){SettingsStore.Save(settingsPath,initial);File.WriteAllText(settingsPath,File.ReadAllText(settingsPath).Replace("</AppSettings>","<FuturePreference>retained fixture</FuturePreference></AppSettings>"));}
        string original=File.ReadAllText(settingsPath);
        using(var blocker=new Form()) {
            if(reserveShortcut) Check(NativeMethods.RegisterHotKey(blocker.Handle,980,0x4000|(uint)initial.FlyoutShortcut.Modifiers,(uint)initial.FlyoutShortcut.Key),"Could not reserve startup shortcut");
            try {
                controller=Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"SixSlotSnap.exe"),"--no-recover --data-dir \""+data+"\""){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden});
                Wait(()=>File.Exists(Path.Combine(data,"runtime-state.xml")) && (int)State().Attribute("pid")==controller.Id,"Recovery controller did not start");
                receiver=new IntPtr((long)State().Attribute("controller"));
                Check((bool)State().Attribute("recoveryRequired"),"Startup failure did not enter recovery mode");
                Wait(()=>NativeMethods.FindWindow(null,"Windows Tile Studio — settings")!=IntPtr.Zero,"Startup recovery did not open settings");
                Check(!controller.HasExited && File.ReadAllText(settingsPath)==original,"Recovery exited or replaced the original settings");
                Command(SixSlotSnap.QuitCommand,0,IntPtr.Zero); Wait(()=>controller.HasExited,"Recovery controller did not close");
            } finally { if(reserveShortcut) NativeMethods.UnregisterHotKey(blocker.Handle,980); }
        }
        data=priorData;
        passed.Add(futureFields?"Future settings fields open Recovery and suppress autosave, preserving original bytes.":reserveShortcut?"Reserved startup shortcut opens recoverable settings without replacing the saved file.":"Corrupt startup settings opens recovery while preserving the original bytes.");
    }
    static void TestCompatibility(string directory) {
        string previousData=data; data=directory; Directory.CreateDirectory(data);
        foreach(var form in forms) form.Dispose();
        var settings=AppSettings.Defaults(); settings.ShowLauncherTab=false;
        settings.Profiles.Add(LayoutProfile.Create("compat-three","Compatibility three rows",2,3)); settings.ActiveProfileId="compat-three";
        settings.Profiles.Add(LayoutProfile.Create("compat-four","Compatibility four rows",2,4));
        SettingsStore.Save(Path.Combine(data,"settings.xml"),settings);
        forms=new Form[6]; for(int i=0;i<6;i++) { forms[i]=new Form{Text="Disposable compatibility window "+i,StartPosition=FormStartPosition.Manual,Location=new Point(200+30*i,200+30*i),Size=new Size(500,350)}; forms[i].Show(); }
        controller=Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"SixSlotSnap.exe"),"--no-recover --data-dir \""+data+"\""){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden});
        Wait(()=>File.Exists(Path.Combine(data,"runtime-state.xml")) && (int)State().Attribute("pid")==controller.Id,"Compatibility controller did not start"); receiver=new IntPtr((long)State().Attribute("controller"));
        using(var slow=new SlowMinimumForm {Text="Disposable slow sizing query",StartPosition=FormStartPosition.Manual,Location=new Point(250,250),Size=new Size(500,350)}) {
            slow.Show();Pump(60);var before=Bounds(slow.Handle);slow.DelayMinimum=true;Command(SixSlotSnap.SnapCommand,1,slow.Handle);slow.DelayMinimum=false;
            Check(!State().Elements("Zone").Any(z=>(long)z.Attribute("window")==slow.Handle.ToInt64()) && Equal(Bounds(slow.Handle),before),"A timed-out size query moved or assigned the window");
            Check(((string)State().Attribute("placementIssue")??"").Contains("size information"),"Timed-out size query had no actionable feedback");
        }
        passed.Add("A delayed sizing response times out without moving or assigning the app, with actionable feedback.");
        var model=Model(); var tile=model.RectFor(0); forms[0].MinimumSize=new Size(tile.R-tile.L+300,300); Pump(100);
        var original=Bounds(forms[0].Handle); Command(SixSlotSnap.SnapCommand,1,forms[0].Handle);
        Check(!State().Elements("Zone").Any(z=>(long)z.Attribute("window")==forms[0].Handle.ToInt64()),"An oversized app was falsely recorded as snapped");
        Check(Equal(Bounds(forms[0].Handle),original),"Failed snap changed the original window bounds");
        passed.Add("An oversized app is rejected without a false assignment or changed bounds.");
        forms[0].MinimumSize=new Size(200,150); Pump(100);
        scenario="three-row initial placement";
        for(int i=0;i<6;i++) Command(SixSlotSnap.SnapCommand,i+1,forms[i].Handle); CheckAll();
        for(int column=0;column<2;column++) for(int boundary=1;boundary<3;boundary++) for(int side=0;side<2;side++) {
            scenario="three-row column "+column+" boundary "+boundary+" side "+side;
            int slot=Model().IndexOf(column,boundary-1+side); var baseline=forms.Select(f=>Bounds(f.Handle)).ToArray();
            BeginResize(forms[slot].Handle); Pump(70); var proposed=Bounds(forms[slot].Handle); if(side==0) proposed.B+=25; else proposed.T-=20; Move(slot,proposed); Pump(100); NotifyWinEvent(11,forms[slot].Handle,0,0); Pump(370);
            CheckAll(); int first=Model().IndexOf(column,boundary-1),second=Model().IndexOf(column,boundary);
            for(int i=0;i<6;i++) if(i!=first && i!=second) Check(Equal(Bounds(forms[i].Handle),baseline[i]),"Three-row divider moved a nonadjacent window");
        }
        scenario="three-row minimum height";
        forms[4].MinimumSize=new Size(200,600); Pump(50); var beforeMinimum=forms.Select(f=>Bounds(f.Handle)).ToArray();
        BeginResize(forms[2].Handle); Pump(120); var squeeze=Bounds(forms[2].Handle); squeeze.B+=500; Move(2,squeeze); Pump(120); NotifyWinEvent(11,forms[2].Handle,0,0); Pump(370);
        CheckAll(); Check(Bounds(forms[4].Handle).B-Bounds(forms[4].Handle).T>=570,"Neighbor height minimum was not respected");
        for(int i=0;i<6;i++) if(i!=2 && i!=4) Check(Equal(Bounds(forms[i].Handle),beforeMinimum[i]),"Minimum-height negotiation disturbed an unrelated window");
        scenario="three-row minimum width"; forms[1].MinimumSize=new Size(1800,150); Pump(50); var beforeWidth=forms.Select(f=>Bounds(f.Handle)).ToArray();
        BeginResize(forms[0].Handle); Pump(120); var wide=Bounds(forms[0].Handle); wide.R+=1800; Move(0,wide); Pump(120); NotifyWinEvent(11,forms[0].Handle,0,0); Pump(370);
        CheckAll(); Check(Bounds(forms[1].Handle).R-Bounds(forms[1].Handle).L>=1770,"Neighbor column minimum was not respected");
        for(int i=0;i<6;i++) {var actual=Bounds(forms[i].Handle);Check(actual.T==beforeWidth[i].T && actual.B==beforeWidth[i].B,"Width minimum changed an independent height");}
        var atLimit=forms.Select(f=>Bounds(f.Handle)).ToArray(); BeginResize(forms[0].Handle);Pump(150);var pastLimit=Bounds(forms[0].Handle);pastLimit.R+=100;Move(0,pastLimit);Pump(120);NotifyWinEvent(11,forms[0].Handle,0,0);Pump(450);CheckAll();
        Check(State().Elements("Zone").Any(z=>(long)z.Attribute("window")==forms[0].Handle.ToInt64()),"A constrained no-op drag disconnected its source");
        for(int i=0;i<6;i++){var actual=Bounds(forms[i].Handle);Check(Equal(actual,atLimit[i]),"A drag beyond an existing minimum displaced zone "+i+" from "+atLimit[i].L+","+atLimit[i].R+" to "+actual.L+","+actual.R);}
        passed.Add("Reported neighbor minimum heights and column widths constrain dividers while preserving uninvolved heights.");
        scenario="pinned middle-row gesture"; var pinned=forms.Select(f=>Bounds(f.Handle)).ToArray();
        BeginResize(forms[2].Handle); Pump(70); var firstEdge=Bounds(forms[2].Handle); firstEdge.T+=30; Move(2,firstEdge); Pump(130);
        var opposite=Bounds(forms[2].Handle); opposite.B+=60; Move(2,opposite); Pump(130); NotifyWinEvent(11,forms[2].Handle,0,0); Pump(370);
        Check(Equal(Bounds(forms[4].Handle),pinned[4]),"A single height gesture switched boundaries and resized a third window");
        CheckAll(); passed.Add("Three-row divider sides stay pair-only; a gesture cannot switch to the opposite height boundary.");
        scenario="corner and rapid updates"; var cornerBaseline=forms.Select(f=>Bounds(f.Handle)).ToArray();
        controller.Refresh(); var cpuStart=controller.TotalProcessorTime; uint gdiBefore=GetGuiResources(controller.Handle,0); var clock=Stopwatch.StartNew();
        BeginResize(forms[2].Handle); Pump(120);
        for(int n=0;n<20;n++) {var corner=Bounds(forms[2].Handle);corner.T+=2;corner.R-=2;Move(2,corner);Pump(35);}
        NotifyWinEvent(11,forms[2].Handle,0,0); Pump(370); clock.Stop(); CheckAll();
        for(int i=0;i<6;i++) if(i!=0 && i!=2) {var actual=Bounds(forms[i].Handle);Check(actual.T==cornerBaseline[i].T && actual.B==cornerBaseline[i].B,"Corner updates changed an unrelated height");}
        controller.Refresh(); double cpu=(controller.TotalProcessorTime-cpuStart).TotalMilliseconds;
        Check(GetGuiResources(controller.Handle,0)<=gdiBefore+4,"Repeated resize updates leaked GDI objects");
        passed.Add("Measured 20 corner updates: "+clock.ElapsedMilliseconds+" ms including deliberate pumps/settling; controller CPU "+cpu.ToString("0")+" ms; no growing GDI-object count.");
        var idleCpu=controller.TotalProcessorTime; Pump(1000);controller.Refresh();passed.Add("Measured idle controller CPU over 1 second: "+(controller.TotalProcessorTime-idleCpu).TotalMilliseconds.ToString("0")+" ms.");
        NativeMethods.ShowWindow(forms[1].Handle,6); NativeMethods.ShowWindow(forms[5].Handle,3); Pump(100);
        var maxBefore=Bounds(forms[5].Handle);
        BeginResize(forms[0].Handle);Pump(120);var active=Bounds(forms[0].Handle);active.R-=20;Move(0,active);Pump(120);NotifyWinEvent(11,forms[0].Handle,0,0);Pump(370);
        Check(NativeMethods.IsIconic(forms[1].Handle) && NativeMethods.IsZoomed(forms[5].Handle) && Equal(Bounds(forms[5].Handle),maxBefore),"Resizing forced a minimized/maximized neighbor");
        NativeMethods.ShowWindow(forms[1].Handle,9);NativeMethods.ShowWindow(forms[5].Handle,9);Pump(100);Command(SixSlotSnap.SnapCommand,2,forms[1].Handle);Command(SixSlotSnap.SnapCommand,6,forms[5].Handle);CheckAll();
        passed.Add("Minimized/maximized neighbors remain untouched; restored windows reconnect on an explicit snap.");
        var expanded=new Form[8];Array.Copy(forms,expanded,6); forms=expanded;
        for(int i=6;i<8;i++){forms[i]=new Form{Text="Disposable fourth-row window "+i,FormBorderStyle=i==6?FormBorderStyle.None:FormBorderStyle.FixedToolWindow,StartPosition=FormStartPosition.Manual,Size=new Size(500,350)};forms[i].Show();}
        forms[4].MinimumSize=new Size(200,150);Command(SixSlotSnap.ProfileCommand,settings.Profiles.Count-1,IntPtr.Zero);
        for(int i=0;i<8;i++)Command(SixSlotSnap.SnapCommand,i+1,forms[i].Handle);scenario="four-row initial placement";CheckAll();
        for(int column=0;column<2;column++)for(int boundary=1;boundary<4;boundary++)for(int side=0;side<2;side++) {
            scenario="four-row divider "+column+"/"+boundary+"/"+side;var baseline=forms.Select(f=>Bounds(f.Handle)).ToArray();var g=Model();int slot=g.IndexOf(column,boundary-1+side);
            BeginResize(forms[slot].Handle);Pump(140);var proposed=Bounds(forms[slot].Handle);if(side==0)proposed.B+=15;else proposed.T-=10;Move(slot,proposed);Pump(100);NotifyWinEvent(11,forms[slot].Handle,0,0);Pump(370);CheckAll();
            int first=g.IndexOf(column,boundary-1),second=g.IndexOf(column,boundary);for(int i=0;i<8;i++)if(i!=first && i!=second)Check(Equal(Bounds(forms[i].Handle),baseline[i]),"Four-row divider changed a nonadjacent window");
        }
        passed.Add("All four-row internal dividers work from both sides, including borderless/tool frames, without changing nonadjacent frames.");
        long closed=forms[4].Handle.ToInt64();forms[4].Dispose();Pump(180);
        Wait(()=>!State().Elements("Zone").Any(z=>(long)z.Attribute("window")==closed),"Closed window remained assigned in runtime state");
        forms[4]=new Form{Text="Disposable restarted window",StartPosition=FormStartPosition.Manual,Size=new Size(500,350)};forms[4].Show();Pump(100);
        Check(!State().Elements("Zone").Any(z=>(long)z.Attribute("window")==forms[4].Handle.ToInt64()),"Restarted window was implicitly adopted");
        Command(SixSlotSnap.SnapCommand,5,forms[4].Handle);CheckAll();passed.Add("Closed windows release their slots; restarted windows require an explicit snap.");
        TestIsolatedChrome();
        Command(SixSlotSnap.QuitCommand,0,IntPtr.Zero); Wait(()=>controller.HasExited,"Compatibility controller did not exit"); data=previousData;
    }
    static void TestIsolatedChrome() {
        string executable=null;
        foreach(var candidate in Process.GetProcessesByName("chrome")) {
            try { if(executable==null) executable=candidate.MainModule.FileName; } catch(System.ComponentModel.Win32Exception) {} catch(InvalidOperationException) {} finally { candidate.Dispose(); }
        }
        if(executable==null) { passed.Add("Real Chrome frame check skipped: Chrome was not available; disposable frame coverage still passed."); return; }
        string profile=Path.Combine(data,"isolated-chrome-profile");
        using(var browser=Process.Start(new ProcessStartInfo(executable,"--user-data-dir=\""+profile+"\" --app=about:blank --no-first-run --no-default-browser-check --disable-background-networking --disable-background-mode"){UseShellExecute=false})) {
            IntPtr window=IntPtr.Zero;
            try {
                Wait(()=>{if(browser.HasExited)return false;NativeMethods.EnumWindows(delegate(IntPtr w,IntPtr ignored){uint pid;NativeMethods.GetWindowThreadProcessId(w,out pid);if(pid==browser.Id && NativeMethods.IsWindowVisible(w))window=w;return true;},IntPtr.Zero);return window!=IntPtr.Zero;},"Isolated Chrome did not expose a window",15);
                Command(SixSlotSnap.SnapCommand,8,window);Wait(()=>State().Elements("Zone").Any(z=>(long)z.Attribute("window")==window.ToInt64()),"Chrome rejected the four-row tile");
                Check(Equal(Bounds(window),Model().RectFor(7)),"Chrome visible frame did not fit its assigned zone");
                NativeMethods.PostMessage(window,0x10,IntPtr.Zero,IntPtr.Zero);Wait(()=>!NativeMethods.IsWindow(window),"Isolated Chrome did not close");
                Command(SixSlotSnap.SnapCommand,8,forms[7].Handle); CheckAll();passed.Add("An isolated Chrome app window fits the four-row layout with visible-frame compensation; existing browser profiles/windows were untouched.");
            } finally { if(!browser.HasExited) { browser.CloseMainWindow(); if(!browser.WaitForExit(2000)) browser.Kill(); } }
        }
    }
    static void BetaSoak(int cycles,string mode) {
        Action<int,string> dialog=delegate(int command,string title){Command(command,0,IntPtr.Zero);IntPtr window=IntPtr.Zero;Wait(()=>{window=NativeMethods.FindWindow(null,title);return window!=IntPtr.Zero&&NativeMethods.IsWindowVisible(window);},"Soak dialog did not become visible");NativeMethods.PostMessage(window,0x10,IntPtr.Zero,IntPtr.Zero);Wait(()=>!NativeMethods.IsWindow(window),"Soak dialog did not close");};
        for(int i=0;i<3;i++){dialog(SixSlotSnap.SettingsCommand,"Windows Tile Studio — settings");dialog(SixSlotSnap.WorkspacesCommand,"Windows Tile Studio — workspaces");}
        controller.Refresh();uint gdiBefore=GetGuiResources(controller.Handle,0);long privateBefore=controller.PrivateMemorySize64;var clock=Stopwatch.StartNew();
        for(int cycle=0;cycle<cycles;cycle++) {
            int column=cycle%3,slot=column+(cycle%2)*3;var baseline=forms.Select(f=>Bounds(f.Handle)).ToArray();scenario="beta soak "+cycle;
            BeginResize(forms[slot].Handle);Pump(100);var next=Bounds(forms[slot].Handle);int delta=(cycle%2==0?12:-12);if(slot<3)next.B+=delta;else next.T+=delta;Move(slot,next);Pump(130);NotifyWinEvent(11,forms[slot].Handle,0,0);Pump(430);CheckAll();
            for(int i=0;i<6;i++){var actual=Bounds(forms[i].Handle);Check(actual.L==baseline[i].L&&actual.R==baseline[i].R,"Soak height gesture changed a width");if(i!=column&&i!=column+3)Check(Equal(actual,baseline[i]),"Soak height gesture changed a flanking window");}
            if(cycle%4==0){baseline=forms.Select(f=>Bounds(f.Handle)).ToArray();slot=cycle%2;BeginResize(forms[slot].Handle);Pump(100);next=Bounds(forms[slot].Handle);next.R+=(cycle%8==0?12:-12);Move(slot,next);Pump(130);NotifyWinEvent(11,forms[slot].Handle,0,0);Pump(430);CheckAll();for(int i=0;i<6;i++){var actual=Bounds(forms[i].Handle);Check(actual.T==baseline[i].T&&actual.B==baseline[i].B,"Soak width gesture changed heights");}}
            if(cycle%3==0){dialog(SixSlotSnap.SettingsCommand,"Windows Tile Studio — settings");dialog(SixSlotSnap.WorkspacesCommand,"Windows Tile Studio — workspaces");}
            Check(!(bool)State().Attribute("launcherVisible")&&State().Elements("Zone").Count()==6,"Soak lost assignments or resurrected the bar");
            if((cycle+1)%10==0)Console.WriteLine("Soak "+mode+": "+(cycle+1)+"/"+cycles+" resize cycles.");
        }
        controller.Refresh();double cpu=controller.TotalProcessorTime.TotalMilliseconds;Pump(15000);controller.Refresh();double idleCpu=controller.TotalProcessorTime.TotalMilliseconds-cpu;uint gdiAfter=GetGuiResources(controller.Handle,0);long privateAfter=controller.PrivateMemorySize64;
        Check(gdiAfter<=gdiBefore+128,"Soak exceeded the bounded GDI growth budget");Check(privateAfter<=privateBefore+128L*1024*1024,"Soak exceeded the bounded private-memory growth budget");
        new XElement("BetaSoak",new XAttribute("mode",mode),new XAttribute("cycles",cycles),new XAttribute("elapsedMs",clock.ElapsedMilliseconds),new XAttribute("idleIntervalMs",15000),new XAttribute("idleCpuMs",idleCpu),new XAttribute("gdiBefore",gdiBefore),new XAttribute("gdiAfter",gdiAfter),new XAttribute("privateBefore",privateBefore),new XAttribute("privateAfter",privateAfter),new XAttribute("passed",true)).Save(Path.Combine(data,"beta-soak-summary.xml"));
        passed.Add("Bounded "+mode+" soak: "+cycles+" height/width cycles, dialog churn, pair-only heights, six assignments, hidden bar, resource budgets and 15-second idle sample pass.");
    }
    static void Run(string directory,string hostedScript,int soakCycles=0) {
        data=directory; Directory.CreateDirectory(data);
        Check(NativeMethods.FindWindow(null,SixSlotSnap.ControllerCaption)==IntPtr.Zero,"Exit the running controller before integration tests.");
        var settings=AppSettings.Defaults(); settings.ShowLauncherTab=true; SettingsStore.Save(Path.Combine(data,"settings.xml"),settings);
        forms=new Form[6]; for(int i=0;i<6;i++) { forms[i]=new Form{Text="Six Slot Snap disposable test "+(i+1),StartPosition=FormStartPosition.Manual,Location=new Point(200+20*i,250+20*i),Size=new Size(450,350)}; forms[i].Show(); NativeMethods.ShowWindow(forms[i].Handle,4); }
        if(hostedScript==null) {
            controller=Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"SixSlotSnap.exe"),"--no-recover --data-dir \""+data+"\""){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden});
        } else {
            string powershell=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"System32","WindowsPowerShell","v1.0","powershell.exe");
            controller=Process.Start(new ProcessStartInfo(powershell,"-NoProfile -STA -ExecutionPolicy Bypass -File \""+hostedScript+"\" -RunInProcess -NoRecover -DataDirectory \""+data+"\""){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardError=true,RedirectStandardOutput=true});
            controller.ErrorDataReceived+=delegate(object sender,DataReceivedEventArgs e) { if(e.Data!=null) try { File.AppendAllText(Path.Combine(data,"host-stderr.txt"),e.Data+Environment.NewLine); } catch(IOException) {} };
            controller.OutputDataReceived+=delegate(object sender,DataReceivedEventArgs e) { if(e.Data!=null) try { File.AppendAllText(Path.Combine(data,"host-stdout.txt"),e.Data+Environment.NewLine); } catch(IOException) {} };
            controller.BeginErrorReadLine(); controller.BeginOutputReadLine();
            passed.Add("Controller hosted inside Windows PowerShell with no standalone application EXE required.");
        }
        Wait(()=>File.Exists(Path.Combine(data,"runtime-state.xml")) && (int)State().Attribute("pid")==controller.Id,"Controller did not start; inspect its log and host-stderr.txt.",hostedScript==null?8:20);
        receiver=new IntPtr((long)State().Attribute("controller")); Check(receiver!=IntPtr.Zero,"Controller handle missing");
        var launcherHandle=new IntPtr((long)State().Attribute("launcherHandle"));Check(launcherHandle!=IntPtr.Zero && (bool)State().Attribute("launcherVisible"),"Launcher was not initially visible");
        NativeMethods.PostMessage(launcherHandle,0x10,IntPtr.Zero,IntPtr.Zero);
        Wait(()=>!(bool)State().Attribute("launcherVisible") && !(bool)State().Attribute("launcherEnabled"),"Closing launcher did not hide it to tray");
        Check(!controller.HasExited,"Closing launcher stopped the controller");
        string launcherWarning;Check(!SettingsStore.Load(Path.Combine(data,"settings.xml"),out launcherWarning).ShowLauncherTab,"Hidden launcher preference was not saved");
        passed.Add("Closing the real controller launcher hides it to tray and saves visibility while leaving the controller and hotkeys running.");
        for(int i=0;i<6;i++) Command(SixSlotSnap.SnapCommand,i+1,forms[i].Handle);
        CheckAll(); Check(State().Elements("Zone").Count()==6,"Not all six zones were assigned"); passed.Add("Six external-process test windows placed and tracked.");
        if(soakCycles>0){BetaSoak(soakCycles,hostedScript==null?"Compiled":"SourceHosted");Command(SixSlotSnap.QuitCommand,0,IntPtr.Zero);Wait(()=>controller.HasExited,"Soak controller did not exit");File.WriteAllLines(Path.Combine(data,"integration-result.txt"),new[]{"PASS"}.Concat(passed));return;}
        var displayBaseline=forms.Select(f=>Bounds(f.Handle)).ToArray();int refreshes=(int)State().Attribute("displayRefreshes");
        foreach(int notification in new[]{0x7e,0x1a,0x218}) {
            IntPtr notificationResult;
            Check(SendMessageTimeout(receiver,(uint)notification,new IntPtr(notification==0x218?0x12:0),IntPtr.Zero,2,2000,out notificationResult)!=IntPtr.Zero,"Could not deliver display notification");
            Wait(()=>(int)State().Attribute("displayRefreshes")>refreshes,"Display notification did not refresh inventory");refreshes=(int)State().Attribute("displayRefreshes");
            Check(State().Elements("Zone").Count()==6 && State().Element("Monitors").Elements("Monitor").Any(m=>(bool)m.Attribute("connected")),"Display refresh lost assignments or monitor state");
            for(int i=0;i<6;i++) Check(Equal(displayBaseline[i],Bounds(forms[i].Handle)),"Unchanged display notification moved a test window");
        }
        CheckAll();passed.Add("Display/work-area and synthetic resume notifications refresh live inventory without moving six assigned windows on unchanged hardware; no OS settings or power state changed.");
        for(int row=0;row<2;row++) for(int edge=0;edge<2;edge++) {
            scenario="shared width row "+row+" edge "+edge;
            int index=1+row*3; var beforeModel=Model(); var beforeRows=beforeModel.Profile.Columns.Select(c=>c.Rows.ToArray()).ToArray();
            BeginResize(forms[index].Handle); Pump(100); var r=Bounds(forms[index].Handle); if(edge==0) r.L+=50; else r.R-=45; Move(index,r); Pump(180);
            NotifyWinEvent(11,forms[index].Handle,0,0); Pump(450); CheckAll(); var after=Model();
            for(int c=0;c<3;c++) Check(after.Profile.Columns[c].Rows.SequenceEqual(beforeRows[c]),"Width resize changed an independent row divider");
        }
        scenario="source moves again during neighbor placement";
        bool armed=true,secondMove=false;int finalLeft=0;
        EventHandler moveDuringApply=delegate {
            if(!armed) return;armed=false;var next=Bounds(forms[1].Handle);next.L+=25;finalLeft=next.L;Move(1,next);secondMove=true;
        };
        forms[0].SizeChanged+=moveDuringApply;
        try {
            BeginResize(forms[1].Handle);Pump(100);var firstMove=Bounds(forms[1].Handle);firstMove.L+=35;Move(1,firstMove);
            Wait(()=>secondMove,"Neighbor did not trigger the concurrent source move");Pump(220);
            NotifyWinEvent(11,forms[1].Handle,0,0);Pump(450);CheckAll();
            Check(Bounds(forms[1].Handle).L==finalLeft,"Movement during neighbor placement was consumed without being applied");
        } finally {forms[0].SizeChanged-=moveDuringApply;}
        passed.Add("A source move during synchronous neighbor placement is processed on the next sample instead of lost.");
        for(int c=0;c<3;c++) for(int side=0;side<2;side++) {
            int index=c+side*3; var previous=Model();
            BeginResize(forms[index].Handle); Pump(100); var r=Bounds(forms[index].Handle); if(side==0) r.B+=75; else r.T-=35; Move(index,r); Pump(180);
            NotifyWinEvent(11,forms[index].Handle,0,0); Pump(450); CheckAll(); var after=Model();
            for(int other=0;other<3;other++) if(other!=c) Check(after.Profile.Columns[other].Rows.SequenceEqual(previous.Profile.Columns[other].Rows),"Unrelated row divider moved");
        }
        passed.Add("All six linked: both edges of middle column from both rows; all independent row dividers from both sides.");
        // Give the left pair an actual height that differs from its saved
        // model, as can happen after native window management. A resize in
        // the middle column must preserve those actual bounds, not reapply
        // a stale whole-grid height to the flanking pair.
        var leftTop=Bounds(forms[0].Handle); var leftBottom=Bounds(forms[3].Handle);
        leftTop.B-=105; leftBottom.T=leftTop.B; Move(0,leftTop); Move(3,leftBottom); Pump(100);
        var heightBaseline=forms.Select(f=>Bounds(f.Handle)).ToArray();
        BeginResize(forms[1].Handle); Pump(100); var middleHeight=Bounds(forms[1].Handle); middleHeight.B+=60; Move(1,middleHeight); Pump(180);
        NotifyWinEvent(11,forms[1].Handle,0,0); Pump(450);
        foreach(int i in new[]{0,2,3,5}) { var actual=Bounds(forms[i].Handle); Check(actual.T==heightBaseline[i].T && actual.B==heightBaseline[i].B,"Height drag reset a flanking pair to stale saved heights"); }
        heightBaseline=forms.Select(f=>Bounds(f.Handle)).ToArray();
        BeginResize(forms[1].Handle); Pump(100); var middleWidth=Bounds(forms[1].Handle); middleWidth.L+=35; Move(1,middleWidth); Pump(180);
        NotifyWinEvent(11,forms[1].Handle,0,0); Pump(450);
        for(int i=0;i<6;i++) { var actual=Bounds(forms[i].Handle); Check(actual.T==heightBaseline[i].T && actual.B==heightBaseline[i].B,"Width drag changed a window's independent height"); }
        Command(SixSlotSnap.SnapCommand,1,forms[0].Handle); Command(SixSlotSnap.SnapCommand,4,forms[3].Handle); CheckAll();
        passed.Add("Height resizing preserves actual flanking heights even when saved ratios are stale; width resizing preserves all existing heights.");

        heightBaseline=forms.Select(f=>Bounds(f.Handle)).ToArray(); var beforeGrouped=Model();
        BeginResize(forms[1].Handle); Pump(100);
        var sourceHeight=Bounds(forms[1].Handle); sourceHeight.B+=55; Move(1,sourceHeight); Pump(100);
        // Simulate native Snap grouping resizing both flanks and emitting
        // a second start/end. It must not hijack the active middle pair.
        NotifyWinEvent(10, forms[0].Handle,0,0);
        foreach(int flank in new[]{0,2}) { var upper=Bounds(forms[flank].Handle); var lower=Bounds(forms[flank+3].Handle); upper.B+=80; lower.T=upper.B; Move(flank,upper); Move(flank+3,lower); }
        Pump(180); NotifyWinEvent(11,forms[0].Handle,0,0); Pump(80);
        sourceHeight=Bounds(forms[1].Handle); sourceHeight.B+=20; Move(1,sourceHeight); Pump(180);
        NotifyWinEvent(11,forms[1].Handle,0,0); Pump(450); CheckAll();
        foreach(int i in new[]{0,2,3,5}) { var actual=Bounds(forms[i].Handle); Check(actual.T==heightBaseline[i].T && actual.B==heightBaseline[i].B,"Grouped native height movement reached a flanking pair"); }
        var afterGrouped=Model(); foreach(int c in new[]{0,2}) Check(afterGrouped.Profile.Columns[c].Rows.SequenceEqual(beforeGrouped.Profile.Columns[c].Rows),"Grouped native events changed a flanking column's row ratios");
        passed.Add("Native-style grouped flanking resizes are corrected; extra neighbor start/end events cannot take ownership of the active height pair.");
        Focus(forms[0].Handle); Check(NativeMethods.GetForegroundWindow()==forms[0].Handle,"Test window could not acquire foreground");
        var focusChanges=new List<long>();bool externalMouse=false,escapeDown=false;
        NativeMethods.WinEventProc focusProbe=delegate(IntPtr hook,uint evt,IntPtr window,int obj,int child,uint thread,uint time){if(window!=forms[0].Handle)focusChanges.Add(window.ToInt64());};
        IntPtr focusHook=NativeMethods.SetWinEventHook(3,3,IntPtr.Zero,focusProbe,0,0,0);
        try {
            Command(SixSlotSnap.ShowCommand,0,forms[0].Handle); Wait(Open,"Flyout did not open");
            var until=DateTime.UtcNow.AddMilliseconds(2200);
            while(DateTime.UtcNow<until) {Pump(20);externalMouse|=(NativeMethods.GetAsyncKeyState(1)&0x8000)!=0 || (NativeMethods.GetAsyncKeyState(2)&0x8000)!=0;escapeDown|=(NativeMethods.GetAsyncKeyState(27)&0x8000)!=0;}
            Check(Open(),"Flyout persistence check failed; other foreground HWNDs="+string.Join(",",focusChanges)+"; current foreground="+NativeMethods.GetForegroundWindow().ToInt64()+"; target="+forms[0].Handle.ToInt64()+"; mouseDownObserved="+externalMouse+"; EscapeObserved="+escapeDown+"; displayRefreshes="+(string)State().Attribute("displayRefreshes"));
            Check(NativeMethods.GetForegroundWindow()==forms[0].Handle,"Flyout took foreground focus");
        } finally {if(focusHook!=IntPtr.Zero)NativeMethods.UnhookWinEvent(focusHook);GC.KeepAlive(focusProbe);}
        Check((long)State().Attribute("target")==forms[0].Handle.ToInt64(),"Original window target was lost"); passed.Add("Flyout stays open and preserves the target window and foreground focus.");
        Command(0x312,200,IntPtr.Zero); Check(!Open(),"Escape shortcut route did not dismiss flyout");
        Command(SixSlotSnap.ShowCommand,0,receiver); Wait(Open,"Flyout could not recover the last external target");
        Check((long)State().Attribute("target")==forms[0].Handle.ToInt64(),"Tray-like opening lost the last external target");
        Command(0x312,200,IntPtr.Zero);
        Command(SixSlotSnap.ShowCommand,0,forms[0].Handle); Wait(Open,"Flyout did not reopen");
        IntPtr flyout=new IntPtr((long)State().Attribute("flyoutHandle")); var unchanged=Bounds(forms[1].Handle); Click(ZoneButton(flyout,"Two columns","2"));
        Wait(()=>!Open() && (string)State().Attribute("profile")=="two-columns","Layout zone selection failed");
        Check(Equal(Bounds(forms[0].Handle),Model().RectFor(1)),"Selected zone did not place original target"); Check(Equal(unchanged,Bounds(forms[1].Handle)),"Changing layouts moved an unrelated window"); passed.Add("Actual flyout button selects a different layout and moves only the original target.");
        Command(SixSlotSnap.ShowCommand,0,forms[0].Handle); Wait(Open,"Flyout did not reopen"); Command(SixSlotSnap.ShowCommand,0,forms[0].Handle); Check(!Open(),"Flyout shortcut toggle failed");
        Check(!(bool)State().Attribute("launcherVisible"),"Closing a flyout resurrected the hidden launcher");
        passed.Add("Escape route and repeated flyout command dismiss cleanly.");
        Focus(forms[0].Handle); Command(SixSlotSnap.ShowCommand,0,forms[0].Handle); Wait(Open,"Keyboard flyout did not open");
        SendKeys.SendWait("{RIGHT}"); Pump(150); SendKeys.SendWait("{ENTER}"); Pump(250);
        Check(!Open(),"Enter did not select the highlighted zone");
        Check(Equal(Bounds(forms[0].Handle),Model().RectFor(1)),"Arrow/Enter did not place the original target in zone 2");
        Focus(forms[0].Handle); Command(SixSlotSnap.ShowCommand,0,forms[0].Handle);
        SendKeys.SendWait("{TAB}"); Pump(100); SendKeys.SendWait("+{TAB}"); Pump(100); SendKeys.SendWait("{ENTER}"); Pump(250);
        Check(!Open() && Equal(Bounds(forms[0].Handle),Model().RectFor(0)),"Tab/Shift+Tab did not return to the original layout");
        using(var keyProbe=new Form()) {
            for(int i=0;i<7;i++) {
                var keys=new[]{Keys.Left,Keys.Right,Keys.Up,Keys.Down,Keys.Tab,Keys.Tab,Keys.Enter};
                Check(NativeMethods.RegisterHotKey(keyProbe.Handle,950+i,0x4000u|(i==5?4u:0u),(uint)keys[i]),"Flyout navigation key leaked after close");
                NativeMethods.UnregisterHotKey(keyProbe.Handle,950+i);
            }
            Check(NativeMethods.RegisterHotKey(keyProbe.Handle,970,0x4000,(uint)Keys.Up),"Could not reserve conflict key");
            Command(SixSlotSnap.ShowCommand,0,forms[0].Handle); Wait(Open,"Navigation conflict prevented mouse flyout");
            Check(NativeMethods.RegisterHotKey(keyProbe.Handle,971,0x4000,(uint)Keys.Left),"Partial navigation registration was not rolled back");
            NativeMethods.UnregisterHotKey(keyProbe.Handle,971); Command(0x312,200,IntPtr.Zero); NativeMethods.UnregisterHotKey(keyProbe.Handle,970);
        }
        passed.Add("Actual arrow/Enter and Tab/Shift+Tab input selects the original target; navigation keys release on close and roll back on conflicts.");
        string workspacePath=Path.Combine(data,"workspaces.xml");string workspaceBefore=File.Exists(workspacePath)?File.ReadAllText(workspacePath):null;
        Command(SixSlotSnap.WorkspacesCommand,0,IntPtr.Zero);IntPtr workspaceDialog=IntPtr.Zero;
        Wait(()=>{workspaceDialog=NativeMethods.FindWindow(null,"Windows Tile Studio — workspaces");return workspaceDialog!=IntPtr.Zero;},"Workspace dialog did not open from the running controller");
        NativeMethods.PostMessage(workspaceDialog,0x10,IntPtr.Zero,IntPtr.Zero);Wait(()=>!NativeMethods.IsWindow(workspaceDialog),"Workspace dialog did not close");
        Check(!controller.HasExited&&(File.Exists(workspacePath)?File.ReadAllText(workspacePath):null)==workspaceBefore,"Opening/closing workspace controls changed saved workspaces or stopped the controller");
        passed.Add("The running controller opens/closes workspace controls without capturing/restoring apps or changing workspace files.");
        Command(SixSlotSnap.QuitCommand,0,IntPtr.Zero); Wait(()=>controller.HasExited,"Controller did not exit cleanly");
        var duplicate=new Form(); IntPtr handle=duplicate.Handle;
        var shortcut=AppSettings.Defaults().FlyoutShortcut; Check(NativeMethods.RegisterHotKey(handle,900,0x4000|(uint)shortcut.Modifiers,(uint)shortcut.Key),"Flyout hotkey was not released on exit"); NativeMethods.UnregisterHotKey(handle,900); duplicate.Dispose();
        passed.Add("Controller exit releases its hotkeys.");
        using(var holder=new Form()) using(var blocker=new Form()) using(var registry=new HotkeyRegistry(holder.Handle)) {
            var baseline=AppSettings.Defaults(); foreach(var key in baseline.SlotShortcuts) key.Key=Keys.None;
            baseline.FlyoutShortcut.Key=Keys.F23; string error;
            Check(registry.Replace(baseline,out error),"Could not install isolated hotkey heightBaseline: "+error);
            Check(NativeMethods.RegisterHotKey(blocker.Handle,800,0x4000|(uint)baseline.FlyoutShortcut.Modifiers,(uint)Keys.F24),"Could not reserve conflict test key");
            try {
                var conflict=baseline.Clone(); conflict.FlyoutShortcut.Key=Keys.F24;
                Check(!registry.Replace(conflict,out error) && error!=null,"Hotkey conflict was not reported");
                Check(!NativeMethods.RegisterHotKey(blocker.Handle,801,0x4000|(uint)baseline.FlyoutShortcut.Modifiers,(uint)Keys.F23),"Previous hotkey was not restored after a conflict");
            } finally { NativeMethods.UnregisterHotKey(blocker.Handle,800); NativeMethods.UnregisterHotKey(blocker.Handle,801); }
            var working=registry.Snapshot();
            Check(registry.Restore(new System.Collections.Generic.Dictionary<int,Shortcut>())==null,"Could not restore a previously empty binding set");
            Check(NativeMethods.RegisterHotKey(blocker.Handle,802,0x4000|(uint)baseline.FlyoutShortcut.Modifiers,(uint)Keys.F23),"Restoring empty bindings leaked the candidate shortcut");
            NativeMethods.UnregisterHotKey(blocker.Handle,802);
            Check(registry.Restore(working)==null,"Could not restore the actual working binding snapshot");
        }
        passed.Add("Hotkey conflict reports correctly and restores the previous working binding.");
        TestEditor();
        TestStartupRecovery(Path.Combine(data,"startup-conflict"),true);
        TestStartupRecovery(Path.Combine(data,"startup-corrupt"),false);
        TestStartupRecovery(Path.Combine(data,"startup-future"),false,true);
        TestCompatibility(Path.Combine(data,"compatibility"));
        File.WriteAllLines(Path.Combine(data,"integration-result.txt"),new[]{"PASS"}.Concat(passed));
    }
    [STAThread] static int Main(string[] args) {
        NativeMethods.SetThreadDpiAwarenessContext(new IntPtr(-4)); Application.EnableVisualStyles();
        string directory=args.Length>0?args[0]:Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"integration-data");
        string hostedScript=null; int hosted=Array.IndexOf(args,"--hosted"); if(hosted>=0 && hosted+1<args.Length) hostedScript=Path.GetFullPath(args[hosted+1]);
        try {
            if(args.Contains("--compatibility-only")) {
                Check(NativeMethods.FindWindow(null,SixSlotSnap.ControllerCaption)==IntPtr.Zero,"Exit the running controller before compatibility tests.");
                data=directory; forms=new Form[0]; TestCompatibility(Path.Combine(directory,"compatibility"));
                Directory.CreateDirectory(directory); File.WriteAllLines(Path.Combine(directory,"integration-result.txt"),new[]{"PASS"}.Concat(passed));
            } else {int soak=Array.IndexOf(args,"--beta-soak"),cycles=0;if(soak>=0){if(soak+1>=args.Length||!int.TryParse(args[soak+1],out cycles)||cycles<10||cycles>200)throw new ArgumentException("Beta soak needs 10–200 cycles.");}Run(directory,hostedScript,cycles);}
            Console.WriteLine(string.Join(Environment.NewLine,new[]{"PASS"}.Concat(passed))); return 0;
        }
        catch(Exception e) { Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory,"integration-error.txt"),e.ToString()); Console.Error.WriteLine(e); return 1; }
        finally {
            if(controller!=null && !controller.HasExited) { NativeMethods.PostMessage(receiver,SixSlotSnap.QuitCommand,IntPtr.Zero,IntPtr.Zero); if(!controller.WaitForExit(3000)) controller.Kill(); }
            if(forms!=null) foreach(var f in forms) if(f!=null) f.Dispose();
        }
    }
}
