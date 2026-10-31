using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Linq;

// Opt-in only. Refuse any existing daily controller before constructing a context.
// No-recover contexts ensure existing user windows are never adopted.
public static class PhaseOneNativeTests {
    [DllImport("user32.dll",SetLastError=true)] static extern IntPtr SendMessageTimeout(IntPtr window,uint message,IntPtr a,IntPtr b,uint flags,uint timeout,out IntPtr result);
    static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
    static void Pump(int ms){var timer=Stopwatch.StartNew();while(timer.ElapsedMilliseconds<ms){Application.DoEvents();Thread.Sleep(10);}}
    static void Wait(Func<bool> ready,string reason){var clock=Stopwatch.StartNew();while(clock.ElapsedMilliseconds<5000){Pump(20);if(ready())return;}throw new Exception(reason);}
    static XElement State(string folder){using(var f=new FileStream(Path.Combine(folder,"runtime-state.xml"),FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))return XElement.Load(f);}
    static AppSettings Saved(string folder){string warning;var saved=SettingsStore.Load(Path.Combine(folder,"settings.xml"),out warning);Check(warning==null,"Unexpected Recovery: "+warning);return saved;}
    static void Hidden(string folder,string phase){var state=State(folder);Check(!(bool)state.Attribute("launcherVisible")&&!(bool)state.Attribute("launcherEnabled"),phase+" revealed a hidden bar");Check(!Saved(folder).ShowLauncherTab,phase+" changed saved visibility");}
    static void Stop(SixSlotSnap app){app.ExitThread();app.Dispose();Pump(60);}
    [STAThread] public static int Main(string[] args){
        // This check precedes all controller/window creation and never quits it.
        if(NativeMethods.FindWindow(null,SixSlotSnap.ControllerCaption)!=IntPtr.Zero){Console.Error.WriteLine("Native phase-1 checks require an approved interval without the daily controller. It was left running.");return 2;}
        SixSlotSnap app=null;
        string root=args.Length>0?Path.GetFullPath(args[0]):Path.Combine(Path.GetTempPath(),"WtsPhase1");
        string source=args.Length>1?Path.GetFullPath(args[1]):null;
        string folder=Path.Combine(root,Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try {
            NativeMethods.SetThreadDpiAwarenessContext(new IntPtr(-4));Application.EnableVisualStyles();
            app=new SixSlotSnap(folder,false,source,true);Hidden(folder,"Fresh startup");
            Check(!(bool)State(folder).Attribute("recoveryRequired"),"Unexpected shortcut/settings Recovery");
            using(var target=new Form{Text="Disposable phase-1 target",StartPosition=FormStartPosition.Manual,Location=new Point(-30000,-30000),ShowInTaskbar=false}) {
                target.Show();Pump(40);
                IntPtr foreground=NativeMethods.GetForegroundWindow();app.OpenFlyout(target.Handle,null);
                Check((bool)State(folder).Attribute("flyoutVisible"),"Hidden-bar selector was unavailable");
                Check(NativeMethods.GetForegroundWindow()==foreground,"Selector activated itself");
                app.HideFlyout();Hidden(folder,"Flyout dismissal");
            }
            int refreshes=(int)State(folder).Attribute("displayRefreshes");
            foreach(int notification in new[]{0x7e,0x1a,0x218}) {
                IntPtr notificationResult;Check(SendMessageTimeout(new IntPtr((long)State(folder).Attribute("controller")),(uint)notification,new IntPtr(notification==0x218?0x12:0),IntPtr.Zero,2,2000,out notificationResult)!=IntPtr.Zero,"Could not send synthetic topology notification");
                Wait(()=>(int)State(folder).Attribute("displayRefreshes")>refreshes,"Synthetic topology notification 0x"+notification.ToString("x")+" did not refresh inventory");
                refreshes=(int)State(folder).Attribute("displayRefreshes");Hidden(folder,"Display/work-area/synthetic resume");
            }
            var draft=Saved(folder);draft.Appearance=AppearanceMode.Dark;draft.Profiles[0].Name="Hidden preference preserved";
            var proportions=draft.Profiles[0].Clone();Check(app.ApplySettings(draft)==null,"Unrelated settings save failed");Hidden(folder,"Settings save");
            Stop(app);app=null;app=new SixSlotSnap(folder,false,source,true);Hidden(folder,"Restart");
            Check(Saved(folder).Profiles[0].GeometryMatches(proportions),"Restart changed saved proportions");
            draft=Saved(folder);draft.ShowLauncherTab=true;Check(app.ApplySettings(draft)==null,"Explicit show failed");
            Check((bool)State(folder).Attribute("launcherVisible"),"Explicit visibility preference was ignored");
            Stop(app);app=null;app=new SixSlotSnap(folder,false,source,true);
            var state=State(folder);Check((bool)state.Attribute("launcherVisible"),"Restart lost saved visible preference");
            NativeMethods.ShowWindow(new IntPtr((long)state.Attribute("launcherHandle")),6);Pump(150);Hidden(folder,"Bar minimize");
            draft=Saved(folder);draft.ShowLauncherTab=true;Check(app.ApplySettings(draft)==null,"Explicit show after minimize failed");state=State(folder);
            NativeMethods.PostMessage(new IntPtr((long)state.Attribute("launcherHandle")),0x10,IntPtr.Zero,IntPtr.Zero);Pump(150);Hidden(folder,"Bar close");
            Stop(app);app=null;app=new SixSlotSnap(folder,false,source,true);Hidden(folder,"Restart after bar close");
            Stop(app);app=null;
            File.WriteAllText(Path.Combine(folder,"phase1-result.txt"),"PASS: fresh hidden default; explicit visibility; restart/close; nonactivating on-demand selector; unrelated settings; synthetic topology/resume. No startup registrations or real windows changed. No physical docking/sleep claim.");
            Console.WriteLine("PASS: phase-1 native lifecycle. Result directory: "+folder);return 0;
        }catch(Exception e){File.WriteAllText(Path.Combine(folder,"phase1-error.txt"),e.ToString());Console.Error.WriteLine(e);return 1;}
        finally{if(app!=null)Stop(app);}
    }
}
