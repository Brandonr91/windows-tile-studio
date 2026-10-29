using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Thread=System.Threading.Thread;
using System.Windows.Forms;
using System.Xml.Linq;

public class AccessibilityUiTests {
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] static extern bool AttachThreadInput(uint first,uint second,bool attach);
    static void Pump(int ms){var until=DateTime.UtcNow.AddMilliseconds(ms);while(DateTime.UtcNow<until){Application.DoEvents();Thread.Sleep(5);}}
    static void Focus(Form form,Control target){uint ignored;uint other=NativeMethods.GetWindowThreadProcessId(NativeMethods.GetForegroundWindow(),out ignored),own=GetCurrentThreadId();bool attached=other!=own&&other!=0&&AttachThreadInput(own,other,true);try{form.Activate();NativeMethods.SetForegroundWindow(form.Handle);target.Focus();}finally{if(attached)AttachThreadInput(own,other,false);}Pump(100);Check(NativeMethods.GetForegroundWindow()==form.Handle&&target.Focused,"Keyboard fixture did not obtain foreground/focus");}
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static Control Find(Control root,string name){if(root.AccessibleName==name||root.Text==name)return root;foreach(Control child in root.Controls){var value=Find(child,name);if(value!=null)return value;}return null;}
    static bool Key(Control canvas,Keys key){object[] values={Message.Create(canvas.Handle,0,IntPtr.Zero,IntPtr.Zero),key};return (bool)typeof(VisualLayoutCanvas).GetMethod("ProcessCmdKey",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(canvas,values);}
    static void Render(Control control,string path){using(var image=new Bitmap(control.Width,control.Height)){control.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(path);}}
    [STAThread] public static int Main(string[] args) {
        NativeMethods.SetThreadDpiAwarenessContext(new IntPtr(-4));Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);Application.EnableVisualStyles();
        string root=args.Length>0?args[0]:Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"accessibility-ui-data");Directory.CreateDirectory(root);
        try {
            var source=AppSettings.Defaults();var original=source.Profiles[0].Clone();
            using(var editor=new VisualLayoutDialog(original)) {
                editor.Show();editor.Activate();editor.Canvas.Focus();Application.DoEvents();var canvas=editor.Canvas;
                Focus(editor,canvas);SendKeys.SendWait("{RIGHT}");Pump(150);Check(!canvas.Session.Profile.GeometryMatches(original),"Actual keyboard input did not reach the canvas");Key(canvas,Keys.Control|Keys.Z);
                SendKeys.SendWait("{TAB}");Pump(100);Check(!canvas.Focused,"Tab trapped focus in the canvas");canvas.Focus();
                Check(Key(canvas,Keys.Right)&&Math.Abs(canvas.Session.Profile.Columns[0].Width-original.Columns[0].Width-0.01)<1e-8,"Arrow key did not adjust selected width");
                Check(original.Columns.SelectMany(c=>c.Rows).SequenceEqual(canvas.Session.Profile.Columns.SelectMany(c=>c.Rows)),"Width keyboard change altered heights");
                Key(canvas,Keys.Control|Keys.Z);Check(canvas.Session.Profile.GeometryMatches(original),"Keyboard Undo failed");Key(canvas,Keys.Control|Keys.Y);Check(!canvas.Session.Profile.GeometryMatches(original),"Keyboard Redo failed");Key(canvas,Keys.Control|Keys.Z);
                Key(canvas,Keys.PageDown);Key(canvas,Keys.PageDown);Key(canvas,Keys.Down);
                Check(Math.Abs(canvas.Session.Profile.Columns[0].Rows[0]-0.51)<1e-8&&canvas.Session.Profile.Columns[1].Rows.SequenceEqual(original.Columns[1].Rows)&&canvas.Session.Profile.Columns[2].Rows.SequenceEqual(original.Columns[2].Rows),"Keyboard height nudge escaped adjacent column");
                Key(canvas,Keys.Shift|Keys.Up);Check(Math.Abs(canvas.Session.Profile.Columns[0].Rows[0]-0.509)<1e-8,"Fine keyboard nudge was not one tenth percent");
                for(int i=0;i<110;i++)Key(canvas,Keys.Down);Check(canvas.Session.Profile.Columns[0].Rows.All(r=>r>=0.04-1e-9),"Keyboard movement crossed segment floor");
                Check(canvas.AccessibilityObject.Role==AccessibleRole.Slider&&canvas.AccessibilityObject.Value==canvas.SelectionText,"Accessible divider value is missing");
                UiLayout.FitToWorkArea(editor,new Rectangle(0,0,800,600));Check(editor.Right<=800&&editor.Bottom<=600,"Visual editor exceeded small work area");
                Render(editor,Path.Combine(root,"keyboard-editor.png"));canvas.ApplyPalette(UiPalette.ForContrast(true));Check(canvas.BackColor==SystemColors.Window&&canvas.ForeColor==SystemColors.WindowText,"Canvas ignored high-contrast palette");Render(editor,Path.Combine(root,"contrast-editor.png"));editor.Close();
            }
            using(var single=new VisualLayoutCanvas(LayoutProfile.Create("one","One",1,1)))Check(single.SelectionText.Contains("no internal dividers")&&!Key(single,Keys.Right),"One-zone keyboard fallback failed");
            using(var uneven=new VisualLayoutCanvas(LayoutProfile.Create("mixed","Mixed",2,4))) {
                for(int i=0;i<18;i++){Key(uneven,Keys.PageDown);Key(uneven,Keys.Up);Key(uneven,Keys.Left);}uneven.Session.Profile.Validate();Check(uneven.SelectionText.Length>0,"Mixed-row divider cycling failed");
            }
            var privateSettings=source.Clone();privateSettings.Profiles[0].Name="SECRET_LAYOUT_OR_TITLE";privateSettings.Monitors.Add(new MonitorConfiguration{Id="SECRET_MONITOR",ActiveProfileId="six-grid"});
            var display=new DisplayDescriptor{Id="SECRET_MONITOR",Name="SECRET_USER_PATH",Handle=new IntPtr(98765),Work=new SixSlotSnap.RECT{R=5120,B=2160}};
            string report=DiagnosticReport.Create(privateSettings,new[]{display},4,false,false);var xml=XDocument.Parse(report);
            Check(!report.Contains("SECRET")&&!report.Contains("98765")&&!report.Contains(Environment.UserName+"\\"),"Diagnostic report exposed identity fields");
            Check((int)xml.Root.Element("Controller").Attribute("assignedCount")==4&&xml.Root.Element("Layouts").Elements().Count()==6,"Report aggregates are wrong");
            string reportPath=Path.Combine(root,"diagnostic.xml");DiagnosticReport.Save(reportPath,report);DiagnosticReport.Save(reportPath,report);Check(File.ReadAllText(reportPath)==report&&!Directory.GetFiles(root,"*.tmp-*").Any(),"Atomic report export failed or left a temp file");
            string blocked=Path.Combine(root,"blocked-report");Directory.CreateDirectory(blocked);bool refused=false;try{DiagnosticReport.Save(blocked,report);}catch(IOException){refused=true;}catch(UnauthorizedAccessException){refused=true;}Check(refused&&File.ReadAllText(reportPath)==report&&!Directory.GetFiles(root,"*.tmp-*").Any(),"Failed export changed another report or left temp files");
            using(var settings=new SnapSettingsForm(source,null,null,root,()=>report)) {
                settings.ConfirmAction=delegate{return true;};settings.Show();settings.SelectGeneral();var groups=(TabControl)Find(settings,"General settings categories");groups.SelectedIndex=3;Application.DoEvents();
                UiLayout.FitToWorkArea(settings,new Rectangle(0,0,800,600));Application.DoEvents();
                Check(settings.Right<=800&&settings.Bottom<=600,"Settings exceeded small work area");var page=groups.SelectedTab;var button=Find(settings,"Preview diagnostic report");page.ScrollControlIntoView(button);Application.DoEvents();
                Check(button.Visible&&page.AutoScroll&&page.AutoScrollMinSize.Height>0,"Support export is unreachable in a small viewport");
                Render(settings,Path.Combine(root,"small-settings.png"));Exception failure=null;
                using(var timer=new Timer{Interval=80}) {
                    timer.Tick+=delegate{timer.Stop();try{var preview=Application.OpenForms.Cast<Form>().Single(f=>f.Text=="Diagnostic report preview");Check(preview.Controls.OfType<TextBox>().Single().Text==report,"Report preview changed contents");preview.Close();}catch(Exception e){failure=e;foreach(var f in Application.OpenForms.Cast<Form>().Where(f=>f!=settings).ToArray())f.Dispose();}};
                    timer.Start();((Button)button).PerformClick();
                }
                if(failure!=null)throw failure;Check(source.Profiles[0].GeometryMatches(original),"Support preview altered live settings");
                var save=Find(settings,"Save changes");Check(settings.RectangleToScreen(settings.ClientRectangle).Contains(save.RectangleToScreen(save.ClientRectangle)),"Save is unreachable after clamping");settings.Close();
            }
            using(var workspace=new WorkspaceDialog(Path.Combine(root,"absent-workspaces.xml"),(n,t)=>null,(w,f)=>null,p=>"",p=>{})) {
                workspace.Show();UiLayout.FitToWorkArea(workspace,new Rectangle(0,0,800,600));Application.DoEvents();var grid=Find(workspace,"Workspace restore preview");var restore=Find(workspace,"Restore workspace");Check(grid.Height>25&&workspace.RectangleToScreen(workspace.ClientRectangle).Contains(restore.RectangleToScreen(restore.ClientRectangle)),"Small workspace clipped grid/actions");Render(workspace,Path.Combine(root,"small-workspace.png"));workspace.Close();
            }
            using(var flyout=new SnapFlyout(source,"Disposable target",1,800,600)) {
                flyout.ShowUnfocused();flyout.ApplyPalette(UiPalette.ForContrast(true));flyout.Navigate(Keys.Right,false);flyout.Navigate(Keys.Down,false);flyout.Navigate(Keys.Tab,false);flyout.Navigate(Keys.Tab,true);
                Check(flyout.AccessibleDescription.Contains("zone")&&flyout.BackColor==SystemColors.Window,"Flyout selection/contrast feedback missing");Render(flyout,Path.Combine(root,"contrast-flyout.png"));
            }
            var mixed=AppSettings.Defaults();mixed.Profiles[0]=LayoutProfile.Create("six-grid","Uneven",2,2);mixed.Profiles[0].Columns[0].Rows=new System.Collections.Generic.List<double>{0.25,0.25,0.25,0.25};
            for(int i=6;i<12;i++)mixed.Profiles.Add(LayoutProfile.Create("custom-"+i,"Custom "+i,4,4));
            using(var flyout=new SnapFlyout(mixed,"Disposable target",1,800,600)) {
                int slot=-1;string selected=null;flyout.ZoneSelected=(id,index)=>{selected=id;slot=index;};flyout.ShowUnfocused();Application.DoEvents();
                flyout.Navigate(Keys.Down,false);flyout.Navigate(Keys.Enter,false);Check(slot==2,"Uneven-grid Down failed");flyout.Navigate(Keys.Up,false);flyout.Navigate(Keys.Enter,false);Check(slot==0,"Uneven-grid Up failed");flyout.Navigate(Keys.Right,false);flyout.Navigate(Keys.Enter,false);Check(slot==1,"Uneven-grid Right failed");flyout.Navigate(Keys.Left,false);flyout.Navigate(Keys.Enter,false);Check(slot==0,"Uneven-grid Left failed");
                for(int i=0;i<11;i++)flyout.Navigate(Keys.Tab,false);Application.DoEvents();var cards=flyout.Controls.OfType<FlowLayoutPanel>().Single();Check(cards.VerticalScroll.Value>0,"Selected layout did not scroll into constrained flyout viewport");flyout.Navigate(Keys.Tab,false);flyout.Navigate(Keys.Enter,false);Check(selected==mixed.Profiles[0].Id,"Forward layout wrap failed");flyout.Navigate(Keys.Tab,true);flyout.Navigate(Keys.Enter,false);Check(selected==mixed.Profiles[11].Id,"Reverse layout wrap failed");
            }
            Console.WriteLine("PASS: keyboard divider selection/nudges/fine step/undo/redo/floors, accessible value, mixed grids, high-contrast palette, constrained 800x600 dialogs/scrolling, diagnostic privacy/export/preview and unchanged source geometry. OS contrast and startup were not changed.");return 0;
        }catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
