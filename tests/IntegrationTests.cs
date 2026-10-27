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
    [DllImport("user32.dll")] static extern void NotifyWinEvent(uint evt,IntPtr window,int objectId,int childId);
    [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr parent,NativeMethods.EnumProc callback,IntPtr parameter);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr window,StringBuilder text,int max);
    [DllImport("user32.dll")] static extern IntPtr GetParent(IntPtr window);
    [DllImport("user32.dll",SetLastError=true)] static extern IntPtr SendMessageTimeout(IntPtr window,uint message,IntPtr wparam,IntPtr lparam,uint flags,uint timeout,out IntPtr result);
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] static extern bool AttachThreadInput(uint first,uint second,bool attach);
    static readonly List<string> passed=new List<string>();
    static string data;
    static IntPtr receiver;
    static Process controller;
    static Form[] forms;
    static void Pump(int milliseconds) { var end=DateTime.UtcNow.AddMilliseconds(milliseconds); while(DateTime.UtcNow<end) { Application.DoEvents(); Thread.Sleep(10); } }
    static void Check(bool good,string text) { if(!good) throw new Exception(text); }
    static void Wait(Func<bool> ready,string error,int seconds=8) { var end=DateTime.UtcNow.AddSeconds(seconds); while(DateTime.UtcNow<end) { Application.DoEvents(); try { if(ready()) return; } catch(IOException) {} catch(System.Xml.XmlException) {} Thread.Sleep(20); } throw new Exception(error); }
    static XElement State() { return XElement.Load(Path.Combine(data,"runtime-state.xml")); }
    static bool Open() { return (bool)State().Attribute("flyoutVisible"); }
    static void Command(int message,int value,IntPtr target) { Check(NativeMethods.PostMessage(receiver,message,new IntPtr(value),target),"Could not send controller command"); Pump(160); }
    static SixSlotSnap.RECT Bounds(IntPtr w) { SixSlotSnap.RECT rect; Check(NativeMethods.VisibleRect(w,out rect),"Cannot read test window bounds"); return rect; }
    static bool Equal(SixSlotSnap.RECT a,SixSlotSnap.RECT b) { return Math.Abs(a.L-b.L)<=1 && Math.Abs(a.R-b.R)<=1 && Math.Abs(a.T-b.T)<=1 && Math.Abs(a.B-b.B)<=1; }
    static LayoutModel Model() { string warning; var s=SettingsStore.Load(Path.Combine(data,"settings.xml"),out warning); Check(warning==null,"Test settings invalid: "+warning); return new LayoutModel(s.Profiles.First(p=>p.Id==s.ActiveProfileId)){Work=NativeMethods.Monitor(forms[0].Handle).Work}; }
    static void CheckAll() { var g=Model(); for(int i=0;i<6;i++) Check(Equal(Bounds(forms[i].Handle),g.RectFor(i)),"Linked bounds wrong for test zone "+(i+1)); }
    static void Move(int index,SixSlotSnap.RECT wanted) {
        SixSlotSnap.RECT outer; NativeMethods.GetWindowRect(forms[index].Handle,out outer); var v=Bounds(forms[index].Handle);
        int l=v.L-outer.L,t=v.T-outer.T,r=outer.R-v.R,b=outer.B-v.B;
        Check(NativeMethods.SetWindowPos(forms[index].Handle,IntPtr.Zero,wanted.L-l,wanted.T-t,wanted.R-wanted.L+l+r,wanted.B-wanted.T+t+b,0x14),"Cannot resize test window");
    }
    static void Focus(IntPtr target) {
        uint process; uint other=NativeMethods.GetWindowThreadProcessId(NativeMethods.GetForegroundWindow(),out process),own=GetCurrentThreadId();
        bool attach=other!=own && other!=0; if(attach) AttachThreadInput(own,other,true);
        NativeMethods.ShowWindow(target,5); NativeMethods.SetForegroundWindow(target); if(attach) AttachThreadInput(own,other,false); Pump(100);
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
        var original=AppSettings.Defaults(); bool saved=false;
        using(var editor=new SnapSettingsForm(original)) {
            editor.StartPosition=FormStartPosition.Manual; editor.Location=new Point(-30000,-30000); editor.ShowInTaskbar=false; editor.Show(); Pump(50);
            ((Button)Find(editor,c=>c is Button && c.Text=="Add layout")).PerformClick();
            ((TextBox)Find(editor,c=>c is TextBox && c.AccessibleName=="Layout name")).Text="Editor acceptance test";
            ((NumericUpDown)Find(editor,c=>c is NumericUpDown && c.AccessibleName=="Number of columns")).Value=2;
            ((NumericUpDown)Find(editor,c=>c is NumericUpDown && c.AccessibleName=="Default rows per column")).Value=3;
            ((Button)Find(editor,c=>c is Button && c.Text=="Set grid size")).PerformClick();
            var custom=editor.Draft.Profiles.Last();
            Check(custom.Name=="Editor acceptance test" && custom.Columns.Count==2 && custom.Columns.All(c=>c.Rows.Count==3),"Grid-size edit lost the name or dimensions");
            editor.SaveRequested=delegate(AppSettings draft) { SettingsStore.Save(Path.Combine(data,"editor-test-settings.xml"),draft); saved=true; return null; };
            ((Button)Find(editor,c=>c is Button && c.Text=="Save changes")).PerformClick();
            Check(saved && original.Profiles.Count==6,"Editor save failed or changed the input settings before save");
        }
        passed.Add("Settings UI adds a layout, preserves its name through grid changes, and saves a 2-column/3-row profile.");
    }
    static void Run(string directory,string hostedScript) {
        data=directory; Directory.CreateDirectory(data);
        Check(NativeMethods.FindWindow(null,SixSlotSnap.ControllerCaption)==IntPtr.Zero,"Exit the running controller before integration tests.");
        var settings=AppSettings.Defaults(); settings.ShowLauncherTab=false; SettingsStore.Save(Path.Combine(data,"settings.xml"),settings);
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
        for(int i=0;i<6;i++) Command(SixSlotSnap.SnapCommand,i+1,forms[i].Handle);
        CheckAll(); Check(State().Elements("Zone").Count()==6,"Not all six zones were assigned"); passed.Add("Six external-process test windows placed and tracked.");
        for(int row=0;row<2;row++) for(int edge=0;edge<2;edge++) {
            int index=1+row*3; var beforeModel=Model(); var beforeRows=beforeModel.Profile.Columns.Select(c=>c.Rows.ToArray()).ToArray();
            NotifyWinEvent(10,forms[index].Handle,0,0); Pump(100); var r=Bounds(forms[index].Handle); if(edge==0) r.L+=50; else r.R-=45; Move(index,r); Pump(180);
            NotifyWinEvent(11,forms[index].Handle,0,0); Pump(450); CheckAll(); var after=Model();
            for(int c=0;c<3;c++) Check(after.Profile.Columns[c].Rows.SequenceEqual(beforeRows[c]),"Width resize changed an independent row divider");
        }
        for(int c=0;c<3;c++) for(int side=0;side<2;side++) {
            int index=c+side*3; var previous=Model();
            NotifyWinEvent(10,forms[index].Handle,0,0); Pump(100); var r=Bounds(forms[index].Handle); if(side==0) r.B+=75; else r.T-=35; Move(index,r); Pump(180);
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
        NotifyWinEvent(10,forms[1].Handle,0,0); Pump(100); var middleHeight=Bounds(forms[1].Handle); middleHeight.B+=60; Move(1,middleHeight); Pump(180);
        NotifyWinEvent(11,forms[1].Handle,0,0); Pump(450);
        foreach(int i in new[]{0,2,3,5}) { var actual=Bounds(forms[i].Handle); Check(actual.T==heightBaseline[i].T && actual.B==heightBaseline[i].B,"Height drag reset a flanking pair to stale saved heights"); }
        heightBaseline=forms.Select(f=>Bounds(f.Handle)).ToArray();
        NotifyWinEvent(10,forms[1].Handle,0,0); Pump(100); var middleWidth=Bounds(forms[1].Handle); middleWidth.L+=35; Move(1,middleWidth); Pump(180);
        NotifyWinEvent(11,forms[1].Handle,0,0); Pump(450);
        for(int i=0;i<6;i++) { var actual=Bounds(forms[i].Handle); Check(actual.T==heightBaseline[i].T && actual.B==heightBaseline[i].B,"Width drag changed a window's independent height"); }
        Command(SixSlotSnap.SnapCommand,1,forms[0].Handle); Command(SixSlotSnap.SnapCommand,4,forms[3].Handle); CheckAll();
        passed.Add("Height resizing preserves actual flanking heights even when saved ratios are stale; width resizing preserves all existing heights.");

        heightBaseline=forms.Select(f=>Bounds(f.Handle)).ToArray(); var beforeGrouped=Model();
        NotifyWinEvent(10,forms[1].Handle,0,0); Pump(100);
        var sourceHeight=Bounds(forms[1].Handle); sourceHeight.B+=55; Move(1,sourceHeight); Pump(100);
        // Simulate native Snap grouping resizing both flanks and emitting
        // a second start/end. It must not hijack the active middle pair.
        NotifyWinEvent(10,forms[0].Handle,0,0);
        foreach(int flank in new[]{0,2}) { var upper=Bounds(forms[flank].Handle); var lower=Bounds(forms[flank+3].Handle); upper.B+=80; lower.T=upper.B; Move(flank,upper); Move(flank+3,lower); }
        Pump(180); NotifyWinEvent(11,forms[0].Handle,0,0); Pump(80);
        sourceHeight=Bounds(forms[1].Handle); sourceHeight.B+=20; Move(1,sourceHeight); Pump(180);
        NotifyWinEvent(11,forms[1].Handle,0,0); Pump(450); CheckAll();
        foreach(int i in new[]{0,2,3,5}) { var actual=Bounds(forms[i].Handle); Check(actual.T==heightBaseline[i].T && actual.B==heightBaseline[i].B,"Grouped native height movement reached a flanking pair"); }
        var afterGrouped=Model(); foreach(int c in new[]{0,2}) Check(afterGrouped.Profile.Columns[c].Rows.SequenceEqual(beforeGrouped.Profile.Columns[c].Rows),"Grouped native events changed a flanking column's row ratios");
        passed.Add("Native-style grouped flanking resizes are corrected; extra neighbor start/end events cannot take ownership of the active height pair.");
        Focus(forms[0].Handle); Check(NativeMethods.GetForegroundWindow()==forms[0].Handle,"Test window could not acquire foreground");
        Command(SixSlotSnap.ShowCommand,0,forms[0].Handle); Wait(Open,"Flyout did not open"); Pump(2200);
        Check(Open(),"Flyout disappeared without a selection or outside click"); Check(NativeMethods.GetForegroundWindow()==forms[0].Handle,"Flyout took foreground focus");
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
        }
        passed.Add("Hotkey conflict reports correctly and restores the previous working binding.");
        TestEditor();
        File.WriteAllLines(Path.Combine(data,"integration-result.txt"),new[]{"PASS"}.Concat(passed));
    }
    [STAThread] static int Main(string[] args) {
        NativeMethods.SetThreadDpiAwarenessContext(new IntPtr(-4)); Application.EnableVisualStyles();
        string directory=args.Length>0?args[0]:Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"integration-data");
        string hostedScript=null; int hosted=Array.IndexOf(args,"--hosted"); if(hosted>=0 && hosted+1<args.Length) hostedScript=Path.GetFullPath(args[hosted+1]);
        try { Run(directory,hostedScript); Console.WriteLine(string.Join(Environment.NewLine,new[]{"PASS"}.Concat(passed))); return 0; }
        catch(Exception e) { Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory,"integration-error.txt"),e.ToString()); Console.Error.WriteLine(e); return 1; }
        finally {
            if(controller!=null && !controller.HasExited) { NativeMethods.PostMessage(receiver,SixSlotSnap.QuitCommand,IntPtr.Zero,IntPtr.Zero); if(!controller.WaitForExit(3000)) controller.Kill(); }
            if(forms!=null) foreach(var f in forms) if(f!=null) f.Dispose();
        }
    }
}
