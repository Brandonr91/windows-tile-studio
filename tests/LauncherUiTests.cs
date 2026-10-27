using System;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

class ProbeLauncher : LauncherTab {
    public ProbeLauncher():base("Ctrl+Alt+Win+Space",1.25) {}
    public void ChangeDpi(int dpi) {
        IntPtr buffer=Marshal.AllocHGlobal(16);
        try {
            var rect=new SixSlotSnap.RECT{L=Left,T=Top,R=Right,B=Bottom};Marshal.StructureToPtr(rect,buffer,false);
            var message=Message.Create(Handle,0x2e0,new IntPtr(dpi|(dpi<<16)),buffer);base.WndProc(ref message);
        } finally {Marshal.FreeHGlobal(buffer);}
    }
}

public class LauncherUiTests {
    [DllImport("user32.dll")] static extern uint GetGuiResources(IntPtr process,uint flags);
    [STAThread] public static int Main(string[] args) {
        NativeMethods.SetThreadDpiAwarenessContext(new IntPtr(-4));
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);Application.EnableVisualStyles();
        Exception failure=null;
        try {
            using(var launcher=new ProbeLauncher()) using(var timer=new Timer{Interval=60}) {
                launcher.Location=new Point(600,400);int step=0;int[] dpi={120,96,144,192,120,96,120};uint warmedGdi=0;
                timer.Tick+=delegate {
                    try {
                        launcher.ChangeDpi(dpi[step%dpi.Length]);
                        if(Math.Abs(launcher.Controls[0].Font.Size-12*dpi[step%dpi.Length]/96f)>0.01) throw new Exception("The label retained the previous DPI's font size.");
                        launcher.Controls[0].Font.GetHeight();launcher.Refresh();
                        using(var image=new Bitmap(launcher.Width,launcher.Height)) {
                            launcher.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));
                            if(args.Length>0 && step==69) image.Save(args[0]);
                        }
                        if(step==6) warmedGdi=GetGuiResources(Process.GetCurrentProcess().Handle,0);
                        if(++step>=70) {
                            if(GetGuiResources(Process.GetCurrentProcess().Handle,0)>warmedGdi+4) throw new Exception("Repeated DPI transitions grew the launcher GDI resource count.");
                            int hides=0;launcher.HideRequested=delegate{hides++;};
                            launcher.Close();if(launcher.Visible || launcher.IsDisposed || hides!=1)throw new Exception("Close did not hide the launcher without disposing it.");
                            launcher.ShowUnfocused();
                            foreach(Control control in launcher.Controls)if(control.AccessibleName=="Hide Layouts bar to tray")((Button)control).PerformClick();
                            if(launcher.Visible || hides!=2)throw new Exception("Close button did not hide the launcher.");
                            launcher.ShowUnfocused();
                            var minimize=Message.Create(launcher.Handle,0x112,new IntPtr(0xf020),IntPtr.Zero);
                            NativeMethods.PostMessage(launcher.Handle,minimize.Msg,minimize.WParam,minimize.LParam);
                            Application.DoEvents();if(launcher.Visible || hides!=3)throw new Exception("Minimize did not hide the launcher to tray.");
                            timer.Stop();launcher.Dispose();
                        }
                    } catch(Exception e) {failure=e;timer.Stop();launcher.Dispose();}
                };
                launcher.Shown+=delegate{timer.Start();};Application.Run(launcher);
            }
            if(failure!=null) throw failure;
            Console.WriteLine("PASS: launcher paint through 70 DPI transitions, correct fonts/bounded GDI, close button/WM_CLOSE/minimize hide without disposal, restore and final disposal.");
            return 0;
        } catch(Exception e) {Console.Error.WriteLine(e);return 1;}
    }
}
