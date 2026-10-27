using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Linq;

// Compiled only by Set-LegacySupersession.ps1 into an explicitly selected
// legacy installation. The original controller is preserved alongside it.
class LegacyRedirect {
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern IntPtr FindWindow(string name,string caption);
    static string Quote(string value) { if(value.Contains("\"")) throw new ArgumentException("Invalid path."); return "\""+value.TrimEnd('\\')+"\""; }
    [STAThread] static int Main() {
        try {
            var config=XElement.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"SixSlotSnap.supersession.xml"));
            string script=(string)config.Element("ManagerScript"),data=(string)config.Element("DataDirectory");
            if(config.Name!="TileStudioSupersession" || string.IsNullOrEmpty(script) || !File.Exists(script) || string.IsNullOrEmpty(data)) throw new IOException("The preferred Windows Tile Studio installation is unavailable. Restore it, or disable legacy supersession to use the original controller.");
            if(FindWindow(null,"SixSlotSnap.Controller.v2")!=IntPtr.Zero) return 0;
            string powershell=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"System32","WindowsPowerShell","v1.0","powershell.exe");
            Process.Start(new ProcessStartInfo(powershell,"-NoProfile -STA -ExecutionPolicy Bypass -File "+Quote(script)+" -DataDirectory "+Quote(data)){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden});
            return 0;
        } catch(Exception e) { MessageBox.Show(e.Message,"Windows Tile Studio — legacy launcher",MessageBoxButtons.OK,MessageBoxIcon.Warning); return 1; }
    }
}
