using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;

class WorkspaceNativeTests {
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static SixSlotSnap.RECT Bounds(IntPtr window){SixSlotSnap.RECT r;Check(NativeMethods.VisibleRect(window,out r),"Fixture bounds unavailable");return r;}
    static bool Near(SixSlotSnap.RECT a,SixSlotSnap.RECT b){return Math.Abs(a.L-b.L)<=2&&Math.Abs(a.T-b.T)<=2&&Math.Abs(a.R-b.R)<=2&&Math.Abs(a.B-b.B)<=2;}
    static void Wait(Func<bool> ready,string message,int seconds=10){var until=DateTime.UtcNow.AddSeconds(seconds);while(DateTime.UtcNow<until){try{if(ready())return;}catch(IOException){}catch(System.Xml.XmlException){}Thread.Sleep(30);}throw new Exception(message);}
    static void HiddenDialogs(string hosted) {
        Check(NativeMethods.FindWindow(null,SixSlotSnap.ControllerCaption)==IntPtr.Zero,"Stop the daily controller before hidden-dialog tests.");
        foreach(bool workspaces in new[]{true,false}) {
            string root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"hidden-dialog-data",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);var settings=AppSettings.Defaults();settings.ShowLauncherTab=false;foreach(var key in settings.SlotShortcuts)key.Key=Keys.None;settings.FlyoutShortcut.Key=Keys.F23;SettingsStore.Save(Path.Combine(root,"settings.xml"),settings);
            string file=hosted==null?Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"SixSlotSnap.exe"):Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"System32","WindowsPowerShell","v1.0","powershell.exe");
            string arguments=hosted==null?"--no-recover --data-dir \""+root+"\"":"-NoProfile -STA -ExecutionPolicy Bypass -File \""+hosted+"\" -RunInProcess -NoRecover -DataDirectory \""+root+"\"";
            using(var process=Process.Start(new ProcessStartInfo(file,arguments){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden})) {
                IntPtr receiver=IntPtr.Zero;
                try {
                    string runtime=Path.Combine(root,"runtime-state.xml");Wait(()=>File.Exists(runtime),"Hidden controller did not start",20);using(var stream=File.Open(runtime,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))receiver=new IntPtr((long)XElement.Load(stream).Attribute("controller"));
                    NativeMethods.PostMessage(receiver,workspaces?SixSlotSnap.WorkspacesCommand:SixSlotSnap.SettingsCommand,IntPtr.Zero,IntPtr.Zero);IntPtr dialog=IntPtr.Zero;string title=workspaces?"Windows Tile Studio — workspaces":"Windows Tile Studio — settings";
                    Wait(()=>{dialog=NativeMethods.FindWindow(null,title);return dialog!=IntPtr.Zero&&NativeMethods.IsWindowVisible(dialog);},"First dialog stayed hidden after a background launch");NativeMethods.PostMessage(dialog,0x10,IntPtr.Zero,IntPtr.Zero);Wait(()=>!NativeMethods.IsWindow(dialog),"First dialog did not close");
                }finally{if(!process.HasExited){NativeMethods.PostMessage(receiver,SixSlotSnap.QuitCommand,IntPtr.Zero,IntPtr.Zero);if(!process.WaitForExit(3000))process.Kill();}}
            }
        }
        Console.WriteLine("PASS: first workspace and settings dialogs are visible after a hidden "+(hosted==null?"compiled":"source-hosted")+" startup with the launcher disabled.");
    }
    [STAThread] static int Main(string[] args) {
        NativeMethods.SetThreadDpiAwarenessContext(new IntPtr(-4));Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);Application.EnableVisualStyles();
        if(args.Length>0&&args[0]=="--hidden-dialogs"){try{HiddenDialogs(args.Length>1?Path.GetFullPath(args[1]):null);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
        if(args.Length==2&&args[0]=="--targets") {
            using(var a=new Form{Text="Workspace disposable Alpha",StartPosition=FormStartPosition.Manual,Bounds=new Rectangle(300,300,700,500),MinimumSize=new Size(200,150)})using(var b=new Form{Text="Workspace disposable Beta",StartPosition=FormStartPosition.Manual,Bounds=new Rectangle(400,400,700,500),MinimumSize=new Size(200,150)})using(var timer=new System.Windows.Forms.Timer{Interval=50}) {
                a.Show();b.Show();File.WriteAllLines(args[1]+".tmp",new[]{a.Handle.ToInt64().ToString(),b.Handle.ToInt64().ToString()});File.Move(args[1]+".tmp",args[1]);timer.Tick+=delegate{if(a.IsDisposed&&b.IsDisposed)Application.ExitThread();};timer.Start();Application.Run();return 0;
            }
        }
        Process targets=null;SixSlotSnap app=null;Exception failure=null;
        try {
            Check(NativeMethods.FindWindow(null,SixSlotSnap.ControllerCaption)==IntPtr.Zero,"Stop the daily controller before workspace native tests.");
            string root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"workspace-native-data",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);string handles=Path.Combine(root,"handles.txt");
            targets=Process.Start(new ProcessStartInfo(Assembly.GetExecutingAssembly().Location,"--targets \""+handles+"\""){UseShellExecute=false});var until=DateTime.UtcNow.AddSeconds(8);while(!File.Exists(handles)&&DateTime.UtcNow<until)Thread.Sleep(20);Check(File.Exists(handles),"Disposable app windows did not start");var windows=File.ReadAllLines(handles).Select(s=>new IntPtr(long.Parse(s))).ToArray();
            var settings=AppSettings.Defaults();settings.ShowLauncherTab=false;foreach(var key in settings.SlotShortcuts)key.Key=Keys.None;settings.FlyoutShortcut.Key=Keys.F23;SettingsStore.Save(Path.Combine(root,"settings.xml"),settings);
            app=new SixSlotSnap(root,false);
            using(var timer=new System.Windows.Forms.Timer{Interval=100}) {
                timer.Tick+=delegate {
                    timer.Stop();try {
                        Check(app.SnapTo(windows[0],"six-grid",0)&&app.SnapTo(windows[1],"six-grid",3),"Fixture initial snaps failed");var before=windows.Select(Bounds).ToArray();
                        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                        var read=typeof(SixSlotSnap).GetMethod("ReadDashboard",flags);var retry=typeof(SixSlotSnap).GetMethod("RetryDashboard",flags);
                        var make=typeof(SixSlotSnap).GetMethod("CaptureRetry",flags);var stage=typeof(SixSlotSnap).GetMethod("SetPendingRetry",flags);
                        var live=(LiveDashboardState)read.Invoke(app,null);Check(live.Displays.Sum(d=>d.Tiles.Count(t=>t.Status=="Occupied"))==2,"Dashboard lost actual native assignments");
                        var pending=make.Invoke(app,new object[]{windows[0],"six-grid",0});stage.Invoke(app,new[]{pending});live=(LiveDashboardState)read.Invoke(app,null);
                        Check(live.RetryToken!=null&&((string)retry.Invoke(app,new object[]{"expired-token"})).Contains("expired"),"Dashboard accepted a stale token");
                        Check(((string)retry.Invoke(app,new object[]{live.RetryToken})).StartsWith("Placement succeeded"),"Dashboard explicit retry failed");
                        Check(windows.Select(Bounds).Zip(before,(a,b)=>Near(a,b)).All(v=>v),"Retry changed another frame");
                        pending=make.Invoke(app,new object[]{windows[1],"six-grid",0});stage.Invoke(app,new[]{pending});live=(LiveDashboardState)read.Invoke(app,null);
                        Check(((string)retry.Invoke(app,new object[]{live.RetryToken})).Contains("another window"),"Retry replaced a newly occupied zone");
                        var started=pending.GetType().GetField("Started");started.SetValue(pending,(long)started.GetValue(pending)-1);Check(((LiveDashboardState)read.Invoke(app,null)).RetryToken==null,"Stale process generation remained retryable");
                        stage.Invoke(app,new object[]{null});
                        var capture=app.CaptureWorkspace("Native fixture",true);Check(capture.Displays.Sum(d=>d.Windows.Count)==2,"Capture included unassigned windows or lost fixture windows");
                        string saved=Path.Combine(root,"workspaces.xml");WorkspaceStore.Save(saved,new WorkspaceLibrary{Workspaces={capture}});capture=WorkspaceStore.Load(saved).Workspaces.Single();
                        NativeMethods.SetWindowPos(windows[0],IntPtr.Zero,500,400,900,500,0x14);NativeMethods.SetWindowPos(windows[1],IntPtr.Zero,700,500,850,550,0x14);
                        var plan=app.PreviewWorkspace(capture,false);Check(plan.SelectedRows().Count==2,"Native identity preview did not uniquely match the disposable titles");string result=app.RestoreWorkspace(plan);
                        Check(result.StartsWith("Restored 2")&&Near(Bounds(windows[0]),before[0])&&Near(Bounds(windows[1]),before[1]),"Workspace restore did not recover both saved frames");
                        plan=app.PreviewWorkspace(capture,false);var stale=plan.Rows.First(r=>r.Chosen.Handle==windows[0]);stale.Chosen.ProcessStarted--;
                        NativeMethods.SetWindowPos(windows[0],IntPtr.Zero,600,450,750,500,0x14);var moved=Bounds(windows[0]);result=app.RestoreWorkspace(plan);
                        Check(result.Contains("skipped 1")&&Near(Bounds(windows[0]),moved),"Stale process instance moved a window");
                        Check(app.CaptureWorkspace("After partial restore",true).Displays.Sum(d=>d.Windows.Count)==2,"Partial restore disconnected compatible unselected slots");
                        Check(!targets.HasExited,"Restore terminated the app process");
                        string probeSource=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"WorkspaceLaunchProbe.exe");string probeFolder=Path.Combine(root,"probe app's folder");Directory.CreateDirectory(probeFolder);string probe=Path.Combine(probeFolder,"probe executable.exe");File.Copy(probeSource,probe);
                        WorkspaceApps.Launch(new SavedPlacement{ApplicationPath=probe,WindowClass="Fixture",Slot=0});string marker=Path.Combine(probeFolder,"launch-probe-args.txt");var wait=DateTime.UtcNow.AddSeconds(4);while(!File.Exists(marker)&&DateTime.UtcNow<wait)Thread.Sleep(20);Check(File.Exists(marker)&&File.ReadAllText(marker)=="0","Explicit launch did not preserve the executable path or added arguments");
                        Console.WriteLine("PASS: actual disposable-window capture/XML roundtrip/preview/restore, saved visible frames, stale-process refusal, no restore relaunch, and explicit argument-free launch of an inert probe. Real apps/startup were untouched.");
                    }catch(Exception e){failure=e;}finally{app.ExitThread();}
                };timer.Start();Application.Run(app);
            }
            if(failure!=null)throw failure;return 0;
        }catch(Exception e){Console.Error.WriteLine(e);return 1;}
        finally{if(app!=null){app.ExitThread();app.Dispose();}if(targets!=null&&!targets.HasExited){targets.CloseMainWindow();if(!targets.WaitForExit(2000))targets.Kill();}}
    }
}
