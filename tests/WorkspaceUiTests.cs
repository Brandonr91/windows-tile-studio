using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

public class WorkspaceUiTests {
    class MemoryStartup : IStartupValue {public string Value;public int Writes;public string Read(){return Value;}public void Write(string value){Value=value;Writes++;}public void Remove(){Value=null;Writes++;}}
    static void Check(bool test,string message){if(!test)throw new Exception(message);}
    static Control Find(Control control,string name){if(control.Text==name||control.AccessibleName==name)return control;foreach(Control child in control.Controls){var found=Find(child,name);if(found!=null)return found;}return null;}
    static void Render(Control control,string path){using(var image=new Bitmap(control.Width,control.Height)){control.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(path);}}
    [STAThread] public static int Main(string[] args) {
        NativeMethods.SetThreadDpiAwarenessContext(new IntPtr(-4));Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);Application.EnableVisualStyles();
        string root=args.Length>0?args[0]:Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"workspace-ui-data");Directory.CreateDirectory(root);
        try {
            string exe=Path.Combine(root,"sample-app.exe"),path=Path.Combine(root,"workspaces.xml");File.WriteAllText(exe,"fixture; never executed");
            var profile=AppSettings.Defaults().Profiles[0];var display=new DisplayDescriptor{Id="test-display",Name="Test display",Work=new SixSlotSnap.RECT{R=5120,B=2160}};
            var live=new LiveWorkspaceWindow{Handle=new IntPtr(123),ProcessId=1,ProcessStarted=99,ApplicationPath=exe,WindowClass="Test",Title="Local test window"};
            Func<string,bool,SavedWorkspace> capture=delegate(string name,bool titles){return new SavedWorkspace{Name=name,MatchTitles=titles,Displays={new SavedWorkspaceDisplay{MonitorId=display.Id,DisplayName=display.Name,Layout=profile.Clone(),Windows={new SavedPlacement{ApplicationPath=exe,WindowClass="Test",WindowTitle=titles?live.Title:null,Slot=0}}}}};};
            var memory=new MemoryStartup();var login=new StartupRegistration(new StartupLaunchSpec{Executable=exe,DataDirectory=root},memory);int restored=0,launched=0;Exception failure=null;
            using(var dialog=new WorkspaceDialog(path,capture,(saved,fallback)=>WorkspacePlanner.Build(saved,new[]{live},new[]{display},new[]{profile},fallback?display.Id:null),plan=>{restored=plan.SelectedRows().Count;return "Restored fixture only.";},entry=>launched++,login))using(var timer=new Timer{Interval=100}) {
                dialog.ConfirmAction=delegate{return true;};string error=null;dialog.ReportError=delegate(string title,string message){error=message;};
                timer.Tick+=delegate {
                    timer.Stop();try {
                        Check(memory.Writes==0,"Opening startup page enabled startup");((TextBox)Find(dialog,"Workspace name")).Text="UI fixture";((Button)Find(dialog,"Capture workspace")).PerformClick();Check(File.Exists(path)&&WorkspaceStore.Load(path).Workspaces.Count==1,"Capture UI did not save the workspace");
                        ((Button)Find(dialog,"Preview workspace")).PerformClick();var grid=(DataGridView)Find(dialog,"Workspace restore preview");Check(grid.Rows.Count==1&&Convert.ToBoolean(grid.Rows[0].Cells["apply"].Value),"Restore preview did not show the unique match");
                        var page=dialog.Controls.OfType<TabControl>().Single().SelectedTab;var restoreButton=Find(dialog,"Restore workspace");Check(page.RectangleToScreen(page.ClientRectangle).Contains(restoreButton.RectangleToScreen(restoreButton.ClientRectangle)),"Restore button was clipped outside the workspace page");Check(grid.Right<=page.ClientSize.Width&&grid.Bottom<=page.ClientSize.Height,"Preview grid overflowed its page");Check(((ComboBox)Find(dialog,"Saved workspace")).GetItemText(((ComboBox)Find(dialog,"Saved workspace")).SelectedItem)=="UI fixture","Saved workspace name was not displayed");Render(dialog,Path.Combine(root,"workspace-preview.png"));
                        ((Button)Find(dialog,"Restore workspace")).PerformClick();Check(restored==1&&launched==0&&error==null,"Restore launched apps or failed its selected preview");
                        ((Button)Find(dialog,"Preview workspace")).PerformClick();((Button)Find(dialog,"Launch missing workspace app")).PerformClick();Check(launched==0&&error!=null,"Existing app was relaunched");error=null;
                        var tabs=dialog.Controls.OfType<TabControl>().Single();tabs.SelectedTab=tabs.TabPages.Cast<TabPage>().Single(p=>p.Text=="Login startup");Application.DoEvents();Render(dialog,Path.Combine(root,"startup-controls.png"));
                        dialog.ConfirmAction=delegate{return false;};((Button)Find(dialog,"Enable login startup")).PerformClick();Check(memory.Writes==0,"Canceled enable wrote startup");
                        dialog.ConfirmAction=delegate{return true;};((Button)Find(dialog,"Enable login startup")).PerformClick();Check(memory.Writes==1&&memory.Value==login.Launch.Command&&error==null,"Explicit enable did not write the isolated store");
                        ((Button)Find(dialog,"Disable login startup")).PerformClick();Check(memory.Writes==2&&memory.Value==null,"Explicit disable failed");dialog.Close();
                    }catch(Exception e){failure=e;dialog.Dispose();}
                };dialog.Shown+=delegate{timer.Start();};Application.Run(dialog);
            }
            if(failure!=null)throw failure;
            // Exercise real Windows shortcut serialization only in this fresh temporary directory.
            string shortcutFolder=Path.Combine(root,"isolated-startup-"+Guid.NewGuid().ToString("N"));var shortcut=new UserStartupValue(shortcutFolder);var registration=new StartupRegistration(new StartupLaunchSpec{Executable=exe,DataDirectory=root},shortcut);
            Check(shortcut.Read()==null,"Fresh shortcut store was not empty");registration.Enable(false);Check(string.Equals(shortcut.Read(),registration.Launch.Command,StringComparison.OrdinalIgnoreCase),"Native shortcut changed the startup command");registration.Disable();Check(!File.Exists(shortcut.ShortcutPath),"Native shortcut removal failed");
            string script=Path.Combine(root,"sample hosted.ps1");File.WriteAllText(script,"# fixture; never executed");var hosted=new StartupRegistration(new StartupLaunchSpec{Executable=exe,SourceScript=script,DataDirectory=root},shortcut);hosted.Enable(false);Check(string.Equals(shortcut.Read(),hosted.Launch.Command,StringComparison.OrdinalIgnoreCase),"Hosted shortcut lost source/data arguments");hosted.Disable();
            File.WriteAllText(shortcut.ShortcutPath,"unrelated malformed shortcut");bool refused=false;try{registration.Enable(true);}catch(InvalidOperationException){refused=true;}Check(refused&&File.ReadAllText(shortcut.ShortcutPath)=="unrelated malformed shortcut","An unrelated shortcut was overwritten");
            Console.WriteLine("PASS: real-loop workspace capture/preview/selected restore (injected), explicit launch guard, startup cancel/enable/disable (memory), painting and native shortcut roundtrip/removal in a temporary folder. Real login startup was not changed.");return 0;
        }catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
