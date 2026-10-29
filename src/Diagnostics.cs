using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;

// Allowlisted aggregates only. Never read logs, window titles or workspace files.
public static class DiagnosticReport {
    public static string Create(AppSettings settings,IList<DisplayDescriptor> displays,int assigned,bool recovery,bool hosted) {
        using(var process=Process.GetCurrentProcess()) {
            var root=new XElement("TileStudioDiagnostic",new XAttribute("schema",1),
                new XElement("Application",new XAttribute("version",SixSlotSnap.ProductVersion),new XAttribute("mode",hosted?"SourceHosted":"Compiled")),
                new XElement("Environment",new XAttribute("reportedOsVersion",Environment.OSVersion.Version),new XAttribute("clrVersion",Environment.Version),new XAttribute("processBits",IntPtr.Size*8),new XAttribute("highContrast",SystemInformation.HighContrast)),
                new XElement("Controller",new XAttribute("assignedCount",Math.Max(0,assigned)),new XAttribute("recoveryRequired",recovery),new XAttribute("barEnabled",settings.ShowLauncherTab),new XAttribute("barLocked",settings.LauncherPositionLocked)),
                new XElement("ProcessSample",new XAttribute("workingSetBytes",process.WorkingSet64),new XAttribute("privateBytes",process.PrivateMemorySize64),new XAttribute("cpuMilliseconds",(long)process.TotalProcessorTime.TotalMilliseconds)),
                new XElement("Layouts",settings.Profiles.Select(p=>new XElement("Layout",new XAttribute("columns",p.Columns.Count),new XAttribute("rows",string.Join(",",p.Columns.Select(c=>c.Rows.Count)))))),
                new XElement("Displays",displays.Select(d=>new XElement("Display",new XAttribute("workWidth",d.Work.R-d.Work.L),new XAttribute("workHeight",d.Work.B-d.Work.T)))));
            return new XDocument(root).ToString();
        }
    }
    public static void Save(string path,string report) {
        string temp=path+".tmp-"+Guid.NewGuid().ToString("N");
        try{File.WriteAllText(temp,report,new UTF8Encoding(false));if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}
    }
    public static void ShowPreview(IWin32Window owner,string report) {
        using(var form=new Form{Text="Diagnostic report preview",ClientSize=new System.Drawing.Size(800,550),StartPosition=FormStartPosition.CenterParent}) {
            var text=new TextBox{Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Both,WordWrap=false,Dock=DockStyle.Fill,Text=report,AccessibleName="Diagnostic report preview"};
            var footer=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=48,Padding=new Padding(8)};var save=new Button{Text="Save report...",AutoSize=true};var close=new Button{Text="Close",DialogResult=DialogResult.Cancel};footer.Controls.AddRange(new Control[]{save,close});
            form.Controls.Add(text);form.Controls.Add(footer);form.CancelButton=close;UiLayout.Install(form);
            save.Click+=delegate{using(var dialog=new SaveFileDialog{Filter="Diagnostic XML (*.xml)|*.xml",FileName="tile-studio-diagnostic.xml",OverwritePrompt=true})if(dialog.ShowDialog(form)==DialogResult.OK){try{Save(dialog.FileName,report);}catch(Exception e){MessageBox.Show(form,"Report could not be saved. "+e.GetBaseException().Message,"Save report",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}};
            form.ShowDialog(owner);
        }
    }
}
