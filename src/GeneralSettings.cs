using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

public partial class SnapSettingsForm {
    TabPage generalPage;
    Label generalShortcut;
    public void SelectGeneral() {((TabControl)generalPage.Parent).SelectedTab=generalPage;}
    void ConfigureGeneral(TabPage page,TabPage bar,StartupRegistration login,string dataDirectory,Func<string> diagnostics) {
        var groups=new TabControl{Dock=DockStyle.Fill,AccessibleName="General settings categories"};
        var startup=new TabPage("Startup & tray");var keys=new TabPage("Flyout shortcut");var support=new TabPage("Support");
        groups.TabPages.AddRange(new[]{startup,bar,keys,support});page.Controls.Add(groups);
        foreach(TabPage child in groups.TabPages)child.AutoScroll=true;
        startup.Controls.Add(StartupControls.Create(login,Confirm,Error));
        var locked=new CheckBox{Text="Lock bar position (prevent dragging)",AutoSize=true,Location=new Point(24,250),AccessibleName="Lock Layouts bar position",Checked=Draft.LauncherPositionLocked};
        locked.CheckedChanged+=delegate{Draft.LauncherPositionLocked=locked.Checked;dirty=true;};bar.Controls.Add(locked);
        keys.Controls.Add(new Label{Text="Open the layout flyout",AutoSize=true,Location=new Point(24,28)});
        generalShortcut=new Label{Text=Draft.FlyoutShortcut.ToString(),Bounds=new Rectangle(24,70,680,28),AccessibleName="Current flyout shortcut"};keys.Controls.Add(generalShortcut);
        var change=new Button{Text="Change shortcut...",Bounds=new Rectangle(24,115,190,34),AccessibleName="Change flyout shortcut"};keys.Controls.Add(change);
        change.Click+=delegate{using(var dialog=new ShortcutDialog(Draft.FlyoutShortcut))if(dialog.ShowDialog(this)==DialogResult.OK){if(!dialog.Value.Enabled){Error("Keep a flyout shortcut","Assign a shortcut to open the layout flyout.");return;}SetShortcut(0,dialog.Value);}};
        keys.Controls.Add(new Label{Text="This is the same opening shortcut shown in Hotkeys. Changes apply on Save changes.\nConflicts are checked before saving. Cancel keeps the current shortcuts.\n\nThe tray menu remains available if a shortcut conflicts with another app.",Bounds=new Rectangle(24,174,790,120)});
        support.Controls.Add(new Label{Text="Windows Tile Studio  "+SixSlotSnap.ProductVersion,AutoSize=true,Location=new Point(24,28)});
        support.Controls.Add(new Label{Text="Runs locally, without an AI service or telemetry.\n\nClosing settings keeps the manager running. Use tray Exit to quit.\nLinked height resizing affects only the adjacent pair in its column.\n\nLocal settings, workspace files and logs can contain personal paths, display identities\nand optional window titles. Review files before sharing them.",Bounds=new Rectangle(24,70,790,150)});
        var folderButton=new Button{Text="Open data folder",Bounds=new Rectangle(24,240,180,34),AccessibleName="Open settings data folder",Enabled=dataDirectory!=null};
        var logButton=new Button{Text="Open local log",Bounds=new Rectangle(222,240,180,34),AccessibleName="Open local status log",Enabled=dataDirectory!=null};support.Controls.AddRange(new Control[]{folderButton,logButton});
        folderButton.Click+=delegate{try{if(!Directory.Exists(dataDirectory))throw new IOException("The data folder is unavailable.");Process.Start(new ProcessStartInfo{FileName=dataDirectory,UseShellExecute=true});}catch(Exception e){Error("Could not open data folder",e.GetBaseException().Message);}};
        logButton.Click+=delegate{try{string log=Path.Combine(dataDirectory,"status.log");if(!File.Exists(log))throw new IOException("No local status log exists yet.");Process.Start(new ProcessStartInfo{FileName=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"System32","notepad.exe"),Arguments="\""+log+"\"",UseShellExecute=false});}catch(Exception e){Error("Could not open log",e.GetBaseException().Message);}};
        support.Controls.Add(new Label{Text="To share layouts, use Share layouts: its export excludes app/window and display identities.",Bounds=new Rectangle(24,305,790,65)});
        var report=new Button{Text="Preview diagnostic report...",Bounds=new Rectangle(24,375,235,34),AccessibleName="Preview diagnostic report",Enabled=diagnostics!=null};support.Controls.Add(report);
        report.Click+=delegate{try{DiagnosticReport.ShowPreview(this,diagnostics());}catch(Exception e){Error("Report unavailable",e.GetBaseException().Message);}};
        support.Controls.Add(new Label{Text="Report contains version, environment, counts, grid shapes and process-memory/CPU samples.\nNo logs, titles, app paths, layout names, shortcuts, window IDs or monitor identities. Nothing is uploaded.",Bounds=new Rectangle(24,425,790,52)});
        UiLayout.ScrollContent(startup.Controls[0] as ScrollableControl);
    }
}

public static class StartupControls {
    public static Control Create(StartupRegistration startup,Func<string,string,bool> confirm,Action<string,string> error) {
        var page=new Panel{Dock=DockStyle.Top,Height=465,Width=820,AccessibleName="Startup and tray options"};
        page.Controls.Add(new Label{Text="Start with Windows (optional)",AutoSize=true,Location=new Point(24,25)});
        page.Controls.Add(new Label{Text="Starts this manager quietly for your account, retaining your saved Layouts bar visibility.\nIt never restores a workspace or launches saved apps automatically.\nClosing/minimizing the bar hides it to tray; tray Exit quits. Startup actions apply immediately,\neven if you later Cancel other settings edits.",Bounds=new Rectangle(24,65,770,82)});
        var label=new Label{Bounds=new Rectangle(24,155,770,54),AccessibleName="Login startup status"};page.Controls.Add(label);
        var command=new TextBox{Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Bounds=new Rectangle(24,216,770,88),AccessibleName="Login startup command"};page.Controls.Add(command);
        var enable=new Button{Text="Enable for this installation...",Bounds=new Rectangle(24,325,245,35),AccessibleName="Enable login startup"};
        var disable=new Button{Text="Disable / remove startup...",Bounds=new Rectangle(281,325,235,35),AccessibleName="Disable login startup"};
        var refresh=new Button{Text="Refresh status",Bounds=new Rectangle(528,325,145,35)};page.Controls.AddRange(new Control[]{enable,disable,refresh});
        if(startup==null){label.Text="Startup controls are unavailable in this preview. Open settings from the running manager.";enable.Enabled=disable.Enabled=refresh.Enabled=false;return page;}
        Action update=delegate{try{label.Text=startup.Status;command.Text=startup.Launch.Command;}catch(Exception e){label.Text="Startup status unavailable: "+e.GetBaseException().Message;}};update();refresh.Click+=delegate{update();};
        enable.Click+=delegate{try{if(!confirm("Enable login startup?","Register the displayed command for this Windows account? An older Tile Studio entry may be replaced. This applies immediately; workspace restore and app relaunch stay manual."))return;startup.Enable(true);update();}catch(Exception e){error("Startup action failed",e.GetBaseException().Message);update();}};
        disable.Click+=delegate{try{if(!confirm("Remove login startup?","Remove this app's login startup entry now? Tile Studio keeps running; future login launch stops. Unrelated entries are preserved."))return;startup.Disable();update();}catch(Exception e){error("Startup action failed",e.GetBaseException().Message);update();}};
        return page;
    }
}
