using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;

// External-process race fixture. Its native wait always has a rescue deadline.
public class PlacementRecoveryTests {
    const int Arm=0x8721;
    [DllImport("user32.dll")] static extern bool ReplyMessage(IntPtr result);
    [DllImport("user32.dll")] static extern void NotifyWinEvent(uint evt,IntPtr window,int objectId,int childId);
    [DllImport("user32.dll",SetLastError=true)] static extern IntPtr SendMessageTimeout(IntPtr w,uint m,IntPtr a,IntPtr b,uint flags,uint timeout,out IntPtr result);
    [DllImport("kernel32.dll")] static extern uint WaitForSingleObject(Microsoft.Win32.SafeHandles.SafeWaitHandle h,uint ms);
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] struct ProcessEntry {public uint Size,Usage,Id;public UIntPtr Heap;public uint Module,Threads,Parent;public int Priority;public uint Flags;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=260)]public string Name;}
    [DllImport("kernel32.dll")] static extern IntPtr CreateToolhelp32Snapshot(uint flags,uint pid);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern bool Process32FirstW(IntPtr snapshot,ref ProcessEntry entry);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern bool Process32NextW(IntPtr snapshot,ref ProcessEntry entry);
    static List<Process> Children(int parent){
        var result=new List<Process>();using(var snapshot=new Microsoft.Win32.SafeHandles.SafeFileHandle(CreateToolhelp32Snapshot(2,0),true)){
            var entry=new ProcessEntry{Size=(uint)Marshal.SizeOf(typeof(ProcessEntry))};if(Process32FirstW(snapshot.DangerousGetHandle(),ref entry))do{if(entry.Parent==parent&&(entry.Name.Equals("SixSlotSnap.exe",StringComparison.OrdinalIgnoreCase)||entry.Name.Equals("powershell.exe",StringComparison.OrdinalIgnoreCase)))try{var process=Process.GetProcessById((int)entry.Id);var held=process.Handle;if(!process.HasExited)result.Add(process);else process.Dispose();}catch(ArgumentException){}}while(Process32NextW(snapshot.DangerousGetHandle(),ref entry));
        }return result;
    }
    class ProbeForm : Form {
        public string Marker,Phase;
        public EventWaitHandle Gate;
        bool armed;
        int positions;
        protected override void WndProc(ref Message m) {
            if(m.Msg==Arm){armed=true;positions=0;m.Result=new IntPtr(1);return;}
            if(armed&&Phase=="restore"&&m.Msg==0x46&&++positions<2){base.WndProc(ref m);return;}
            if(armed&&m.Msg==(Phase=="query"?0x24:Phase=="changed"?0x47:0x46)) {
                armed=false;
                if(Phase=="query"){base.WndProc(ref m);ReplyMessage(m.Result);}
                PlacementRecoveryTests.Marker(Marker+".blocked","ready");WaitForSingleObject(Gate.SafeWaitHandle,15000);
                PlacementRecoveryTests.Marker(Marker+".resumed","ready");
                if(Phase=="restore")MaximumSize=Size.Empty;
                if(Phase=="query")return;
            }
            base.WndProc(ref m);
        }
    }
    static void Marker(string path,string value){File.WriteAllText(path+".tmp",value);File.Move(path+".tmp",path);}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Pump(int ms){var until=DateTime.UtcNow.AddMilliseconds(ms);while(DateTime.UtcNow<until){Application.DoEvents();Thread.Sleep(5);}}
    static void Wait(Func<bool> ready,string message,int ms=6000){var end=DateTime.UtcNow.AddMilliseconds(ms);while(DateTime.UtcNow<end){Application.DoEvents();try{if(ready())return;}catch(IOException){}catch(System.Xml.XmlException){}Thread.Sleep(10);}throw new Exception(message);}
    static SixSlotSnap.RECT Bounds(IntPtr w){SixSlotSnap.RECT r;Check(NativeMethods.VisibleRect(w,out r),"Cannot read fixture bounds");return r;}
    static SixSlotSnap.RECT Outer(IntPtr w){SixSlotSnap.RECT r;Check(NativeMethods.GetWindowRect(w,out r),"Cannot read fixture outer bounds");return r;}
    static bool Equal(SixSlotSnap.RECT a,SixSlotSnap.RECT b){return a.L==b.L&&a.R==b.R&&a.T==b.T&&a.B==b.B;}
    static XElement State(string path){using(var s=new FileStream(Path.Combine(path,"runtime-state.xml"),FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))return XElement.Load(s);}
    static bool Ping(IntPtr w){IntPtr result;return SendMessageTimeout(w,0x8701,IntPtr.Zero,IntPtr.Zero,3,150,out result)!=IntPtr.Zero;}
    static Process Start(string file,string args){return Process.Start(new ProcessStartInfo(file,args){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden});}
    static void Stop(Process p){if(p==null)return;try{if(!p.HasExited){p.Kill();p.WaitForExit(2000);}}finally{p.Dispose();}}
    static int Fixture(string[] args){using(var gate=EventWaitHandle.OpenExisting(args[2]))using(var form=new ProbeForm{Marker=args[1],Gate=gate,Phase=args[3],Text="Disposable freeze-after-query fixture",StartPosition=FormStartPosition.Manual,Location=new Point(260,260),Size=new Size(450,350)}){if(args[3]=="restore")form.MaximumSize=new Size(800,600);form.Shown+=delegate{Marker(args[1],form.Handle.ToInt64().ToString());};Application.Run(form);}return 0;}
    static int Position(string[] args){var w=new IntPtr(long.Parse(args[1]));File.WriteAllText(args[2]+".started","ready");NativeMethods.SetWindowPos(w,IntPtr.Zero,900,350,600,430,0x14);File.WriteAllText(args[2]+".done","ready");return 0;}
    static void CancellationExperiment(string root,string phase){
        Directory.CreateDirectory(root);string marker=Path.Combine(root,"probe-"+Guid.NewGuid().ToString("N")),name="Local\\WtsRace-"+Guid.NewGuid().ToString("N");Process fixture=null,worker=null;
        using(var gate=new EventWaitHandle(false,EventResetMode.ManualReset,name))try{
            fixture=Start(Application.ExecutablePath,"--fixture \""+marker+"\" \""+name+"\" "+phase);Wait(()=>File.Exists(marker),"Fixture startup failed");var w=new IntPtr(long.Parse(File.ReadAllText(marker)));var original=Bounds(w);IntPtr reply;
            Check(SendMessageTimeout(w,Arm,IntPtr.Zero,IntPtr.Zero,3,300,out reply)!=IntPtr.Zero,"Cannot arm fixture");
            if(phase=="query"){var info=new NativeMethods.MINMAXINFO();Check(NativeMethods.QueryMinMax(w,0x24,IntPtr.Zero,ref info,3,300,out reply)!=IntPtr.Zero,"Query was not replied to before freeze");}
            worker=Start(Application.ExecutablePath,"--position "+w.ToInt64()+" \""+marker+"\"");Wait(()=>File.Exists(marker+".blocked"),"Fixture did not block");Pump(200);Check(!worker.HasExited,"Worker did not block in SetWindowPos");
            Stop(worker);worker=null;var frozen=Bounds(w);gate.Set();Wait(()=>File.Exists(marker+".resumed")&&Ping(w),"Fixture did not resume");Pump(700);var after=Bounds(w);
            var report=new XElement("CancellationExperiment",new XAttribute("phase",phase),new XAttribute("unchangedBeforeResume",Equal(original,frozen)),new XAttribute("unchangedAfterResume",Equal(original,after)));
            report.Save(Path.Combine(root,"result.xml"));Console.WriteLine(report);
        }finally{gate.Set();Stop(worker);Stop(fixture);}
    }
    static void Run(string root,bool baseline,string hosted,string phase,bool crash,bool resize){
        Check(NativeMethods.FindWindow(null,SixSlotSnap.ControllerCaption)==IntPtr.Zero,"Stop daily controller before race test");Directory.CreateDirectory(root);string data=Path.Combine(root,"data");Directory.CreateDirectory(data);var settings=AppSettings.Defaults();settings.ShowLauncherTab=false;SettingsStore.Save(Path.Combine(data,"settings.xml"),settings);
        Process controller=null,fixture=null;var helpers=new List<Process>();Form[] others=new Form[5];string marker=Path.Combine(root,"probe-"+Guid.NewGuid().ToString("N")),name="Local\\WtsRace-"+Guid.NewGuid().ToString("N");
        using(var gate=new EventWaitHandle(false,EventResetMode.ManualReset,name))try {
            if(hosted==null)controller=Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"SixSlotSnap.exe"),"--no-recover --data-dir \""+data+"\"");
            else controller=Start(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),@"WindowsPowerShell\v1.0\powershell.exe"),"-NoProfile -STA -ExecutionPolicy Bypass -File \""+hosted+"\" -RunInProcess -NoRecover -DataDirectory \""+data+"\"");
            Wait(()=>File.Exists(Path.Combine(data,"runtime-state.xml"))&&(int)State(data).Attribute("pid")==controller.Id,"Controller startup failed",20000);var receiver=new IntPtr((long)State(data).Attribute("controller"));
            long helperPrivateBytes=0;if(!baseline){helpers=Children(controller.Id);Check(helpers.Count==2,"Expected two owned placement helpers, observed "+helpers.Count);helperPrivateBytes=helpers.Sum(p=>p.PrivateMemorySize64);}
            for(int i=0;i<5;i++){others[i]=new Form{Text="Disposable unaffected window "+i,Size=new Size(500,350),StartPosition=FormStartPosition.Manual,Location=new Point(300+i*25,300)};others[i].Show();NativeMethods.PostMessage(receiver,SixSlotSnap.SnapCommand,new IntPtr(i+2),others[i].Handle);int slot=i+2;Wait(()=>State(data).Elements("Zone").Any(z=>(int)z.Attribute("number")==slot&&(long)z.Attribute("window")==others[i].Handle.ToInt64()),"Fixture assignment failed");}
            var untouched=others.Select(f=>Bounds(f.Handle)).ToArray();fixture=Start(Application.ExecutablePath,"--fixture \""+marker+"\" \""+name+"\" "+phase);Wait(()=>File.Exists(marker),"External fixture startup failed");var window=new IntPtr(long.Parse(File.ReadAllText(marker)));var original=Bounds(window);var originalOuter=Outer(window);IntPtr reply;
            if(resize){NativeMethods.PostMessage(receiver,SixSlotSnap.SnapCommand,new IntPtr(1),window);Wait(()=>State(data).Elements("Zone").Any(z=>(long)z.Attribute("window")==window.ToInt64()),"Resize target did not join the grid");original=Bounds(window);originalOuter=Outer(window);}
            Check(SendMessageTimeout(window,Arm,IntPtr.Zero,IntPtr.Zero,3,300,out reply)!=IntPtr.Zero,"Cannot arm fixture");
            if(resize)NotifyWinEvent(10,others[0].Handle,0,0);else NativeMethods.PostMessage(receiver,SixSlotSnap.SnapCommand,new IntPtr(1),window);
            Wait(()=>File.Exists(marker+".blocked"),"Successful size reply/freeze not observed");
            if(resize){var frame=Outer(others[0].Handle);NativeMethods.SetWindowPos(others[0].Handle,IntPtr.Zero,frame.L+40,frame.T,frame.R-frame.L-40,frame.B-frame.T,0x14);NotifyWinEvent(11,others[0].Handle,0,0);Wait(()=>!(bool)State(data).Attribute("resizing")&&State(data).Attribute("placementIssue")!=null,"Linked resize did not fail safely",4000);}
            if(crash){controller.Kill();Check(controller.WaitForExit(2000),"Disposable controller did not exit");Wait(()=>helpers.All(p=>p.HasExited),"Helpers survived parent termination");gate.Set();Wait(()=>File.Exists(marker+".resumed")&&Ping(window),"Target did not resume after parent termination");Pump(400);Check(Equal(original,Bounds(window)),"A window move arrived after parent termination");Console.WriteLine("PASS: parent termination closes helper jobs without a delayed target move.");return;}
            if(!resize)Check(!Ping(window),"Fixture still dispatches messages");var frozenOuter=Outer(window);var clock=Stopwatch.StartNew();NativeMethods.PostMessage(receiver,SixSlotSnap.SettingsCommand,IntPtr.Zero,IntPtr.Zero);IntPtr dialog=IntPtr.Zero;
            if(baseline)Pump(700);else Wait(()=>{dialog=NativeMethods.FindWindow(null,"Windows Tile Studio — settings");return dialog!=IntPtr.Zero&&NativeMethods.IsWindowVisible(dialog);},"Queued settings command did not respond",1500);
            dialog=NativeMethods.FindWindow(null,"Windows Tile Studio — settings");bool responsive=dialog!=IntPtr.Zero&&NativeMethods.IsWindowVisible(dialog);clock.Stop();
            if(baseline)Check(!responsive,"Baseline did not reproduce manager block");else Check(responsive,"Manager blocked after successful size query");
            for(int i=0;i<5;i++)Check(Equal(Bounds(others[i].Handle),untouched[i]),"Unrelated fixture moved while target froze");
            gate.Set();Wait(()=>File.Exists(marker+".resumed")&&Ping(receiver),"Controller did not recover after fixture resume");Pump(700);
            dialog=NativeMethods.FindWindow(null,"Windows Tile Studio — settings");if(dialog!=IntPtr.Zero){NativeMethods.PostMessage(dialog,0x10,IntPtr.Zero,IntPtr.Zero);Wait(()=>!NativeMethods.IsWindow(dialog),"Fixture settings did not close");}bool moved=!Equal(original,Bounds(window));bool lateMovement=!Equal(frozenOuter,Outer(window));if(!baseline){Check(Equal(frozenOuter,Outer(window)),"An additional move arrived after resume");if(phase=="query"||phase=="changing")Check(!moved,"Timed-out target moved after resume");Check(!State(data).Elements("Zone").Any(z=>(long)z.Attribute("window")==window.ToInt64()),"Timed-out target was falsely assigned");NativeMethods.PostMessage(receiver,SixSlotSnap.SnapCommand,new IntPtr(1),window);Wait(()=>State(data).Elements("Zone").Any(z=>(long)z.Attribute("window")==window.ToInt64()),"Explicit retry after resume did not succeed");}
            for(int i=0;i<5;i++)Check(Equal(Bounds(others[i].Handle),untouched[i]),"Unrelated fixture moved after resume");
            if(resize){NativeMethods.PostMessage(receiver,SixSlotSnap.SnapCommand,new IntPtr(2),others[0].Handle);Wait(()=>State(data).Elements("Zone").Count()==6,"Explicit source retry did not reconnect the grid");}
            var report=new XElement("PlacementRecovery",new XAttribute("baseline",baseline),new XAttribute("phase",phase),new XAttribute("linkedResize",resize),new XAttribute("helperPrivateBytesAtStart",helperPrivateBytes),new XAttribute("responsiveDuringFreeze",responsive),new XAttribute("heartbeatMilliseconds",clock.ElapsedMilliseconds),new XAttribute("targetChangedBeforeResume",!Equal(originalOuter,frozenOuter)),new XAttribute("lateMovementAfterResume",lateMovement),new XAttribute("frameDiffersFromOriginal",moved),new XAttribute("unrelatedWindowsUnchanged",true));report.Save(Path.Combine(root,"result.xml"));Console.WriteLine(report);
            if(!baseline)foreach(var child in Children(controller.Id)){if(!helpers.Any(p=>!p.HasExited&&p.Id==child.Id))helpers.Add(child);else child.Dispose();}
            NativeMethods.PostMessage(receiver,SixSlotSnap.QuitCommand,IntPtr.Zero,IntPtr.Zero);Wait(()=>controller.HasExited,"Controller did not exit");
            if(!baseline)Wait(()=>helpers.All(p=>p.HasExited),"Helpers survived normal controller exit");
        }finally{gate.Set();foreach(var f in others)if(f!=null)f.Dispose();Stop(fixture);Stop(controller);foreach(var helper in helpers)Stop(helper);}
    }
    [STAThread] public static int Main(string[] args){NativeMethods.SetThreadDpiAwarenessContext(new IntPtr(-4));Application.EnableVisualStyles();try{
        if(args[0]=="--fixture")return Fixture(args);if(args[0]=="--position")return Position(args);
        if(args[0]=="--cancel-experiment"){CancellationExperiment(Path.GetFullPath(args[1]),args[2]);return 0;}
        int at=Array.IndexOf(args,"--hosted"),phaseAt=Array.IndexOf(args,"--phase");Run(Path.GetFullPath(args[0]),args.Contains("--baseline"),at>=0?Path.GetFullPath(args[at+1]):null,phaseAt>=0?args[phaseAt+1]:"query",args.Contains("--crash-parent"),args.Contains("--resize-race"));return 0;
    }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
