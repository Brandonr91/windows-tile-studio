using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

public class EditorUiTests {
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static Control Find(Control root,string name){if(root.AccessibleName==name||root.Text==name)return root;foreach(Control c in root.Controls){var result=Find(c,name);if(result!=null)return result;}return null;}
    static void Pointer(Control canvas,string method,int x,int y){typeof(Control).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(canvas,new object[]{new MouseEventArgs(MouseButtons.Left,1,x,y,0)});}
    static void Render(Control control,string path){using(var bitmap=new Bitmap(control.Width,control.Height)){control.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size));if(path!=null)bitmap.Save(path);}}
    [STAThread] public static int Main(string[] args) {
        NativeMethods.SetThreadDpiAwarenessContext(new IntPtr(-4));Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);Application.EnableVisualStyles();
        string root=args.Length>0?args[0]:Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"editor-ui-data");Directory.CreateDirectory(root);
        try {
            var source=AppSettings.Defaults();var original=source.Profiles[0].Clone();Exception failure=null;
            using(var dialog=new VisualLayoutDialog(original))using(var timer=new Timer{Interval=80}) {
                timer.Tick+=delegate {
                    timer.Stop();try {
                        var canvas=dialog.Canvas;int w=canvas.Width-36,h=canvas.Height-36;int x=18+w/2,y=18+h/2;
                        Pointer(canvas,"OnMouseDown",x,y);Pointer(canvas,"OnMouseMove",x,y+30);Pointer(canvas,"OnMouseUp",x,y+30);
                        Check(!canvas.Session.Profile.GeometryMatches(original),"Canvas mouse drag did not change preview");
                        Check(canvas.Session.Profile.Columns[0].Rows.SequenceEqual(original.Columns[0].Rows)&&canvas.Session.Profile.Columns[2].Rows.SequenceEqual(original.Columns[2].Rows),"Canvas drag affected flanking heights");
                        var edited=canvas.Session.Profile.Clone();((Button)Find(dialog,"Undo")).PerformClick();Check(canvas.Session.Profile.GeometryMatches(original),"Visual Undo button failed");
                        ((Button)Find(dialog,"Redo")).PerformClick();Check(canvas.Session.Profile.GeometryMatches(edited),"Visual Redo button failed");
                        var beforeEscape=canvas.Session.Profile.Clone();x=18+(int)(w*canvas.Session.Profile.Columns[0].Width);
                        Pointer(canvas,"OnMouseDown",x,30);Pointer(canvas,"OnMouseMove",x+40,30);
                        var message=Message.Create(canvas.Handle,0,IntPtr.Zero,IntPtr.Zero);object[] arguments={message,Keys.Escape};typeof(VisualLayoutCanvas).GetMethod("ProcessCmdKey",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(canvas,arguments);
                        Check(canvas.Session.Profile.GeometryMatches(beforeEscape),"Escape failed to cancel the active canvas gesture");
                        Render(dialog,Path.Combine(root,"visual-editor.png"));((Button)Find(dialog,"Use proportions")).PerformClick();
                        Check(dialog.Result!=null&&!dialog.Result.GeometryMatches(original)&&source.Profiles[0].GeometryMatches(original),"Visual acceptance applied to live/source settings");
                    }catch(Exception e){failure=e;dialog.Dispose();}
                };dialog.Shown+=delegate{timer.Start();};Application.Run(dialog);
            }
            if(failure!=null)throw failure;
            string portable=Path.Combine(root,"sample.tilelayouts.xml");PortableLayouts.Export(portable,source);
            using(var settings=new SnapSettingsForm(source)) {
                settings.ConfirmAction=delegate{return true;};settings.Show();Application.DoEvents();
                var button=(Button)Find(settings,"Edit proportions visually");Exception modalFailure=null;
                using(var timer=new Timer{Interval=80}) {
                    timer.Tick+=delegate{timer.Stop();var visual=Application.OpenForms.OfType<VisualLayoutDialog>().Single();try{visual.Canvas.Session.Begin();visual.Canvas.Session.Move(true,-1,1,0.45);visual.Canvas.Session.End(false);((Button)Find(visual,"Use proportions")).PerformClick();}catch(Exception e){modalFailure=e;visual.Dispose();}};
                    timer.Start();button.PerformClick();
                }
                if(modalFailure!=null)throw modalFailure;
                Check(Math.Abs(settings.Draft.Profiles[0].Columns[0].Width-0.45)<1e-8&&source.Profiles[0].GeometryMatches(original),"Settings visual dialog failed draft isolation");
                ((Button)Find(settings,"Undo last grid change")).PerformClick();Check(settings.Draft.Profiles[0].GeometryMatches(original),"Settings grid Undo did not restore visual edit");
                settings.ImportPortable(portable,false,false);Check(settings.Draft.Profiles.Count==12&&source.Profiles.Count==6,"Portable import did not remain in the draft");
                string bad=Path.Combine(root,"bad.xml");File.WriteAllText(bad,"invalid");bool rejected=false;try{settings.ImportPortable(bad,true,true);}catch(Exception){rejected=true;}
                Check(rejected&&settings.Draft.Profiles.Count==12,"Failed import changed the draft");
                var tabs=(TabControl)settings.Controls.OfType<TabControl>().Single();tabs.SelectedTab=tabs.TabPages.Cast<TabPage>().Single(p=>p.Text=="Share layouts");Application.DoEvents();
                Render(settings,Path.Combine(root,"share-layouts.png"));((Button)Find(settings,"Undo portable import")).PerformClick();Check(settings.Draft.Profiles.Count==6,"Undo import failed");
                settings.ConfirmAction=delegate{return false;};settings.Close();Check(!settings.IsDisposed&&settings.Visible,"Changed portable draft did not protect unsaved close");
                settings.ConfirmAction=delegate{return true;};settings.Close();Check(source.Profiles[0].GeometryMatches(original)&&source.Profiles.Count==6,"Cancel/discard changed input settings");
            }
            Console.WriteLine("PASS: real-loop canvas pointer drag, pair-only heights, Undo/Redo/Escape, paint, visual acceptance, settings draft isolation/grid undo, portable import/failure/undo/discard and sharing-page rendering.");return 0;
        } catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
