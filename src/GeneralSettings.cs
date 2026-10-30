using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

public partial class SnapSettingsForm {
    Control StudioFind(Control root,string text) {
        foreach(Control child in root.Controls){if(child.Text==text||child.AccessibleName==text)return child;var result=StudioFind(child,text);if(result!=null)return result;}return null;
    }
    static void StudioMove(Control control,Control parent,int x,int y,int width,int height){parent.Controls.Add(control);control.Bounds=new Rectangle(x,y,width,height);}
    static Label StudioHeading(string text,int x,int y){return new Label{Text=text,AutoSize=true,Location=new Point(x,y),Font=new Font("Segoe UI",12,FontStyle.Regular)};}
    void StudioWrap(TabPage page) {
        var content=page.Controls.Cast<Control>().ToArray();int height=Math.Max(420,content.Max(c=>c.Bottom)+24);
        var surface=new StudioPanel{Bounds=new Rectangle(24,20,936,height),AccessibleName=page.Text+" options card"};
        foreach(var control in content){control.Dock=DockStyle.None;surface.Controls.Add(control);}page.Controls.Add(surface);
    }
    void ConfigureStudioLayout(TabPage page) {
        // Retain existing editor controls and handlers; only the composition changes.
        var nameLabel=StudioFind(page,"Layout name") as Label;
        var columnLabel=StudioFind(page,"Columns");var rowLabel=StudioFind(page,"Rows per column");
        var reshape=StudioFind(page,"Set grid size");var visual=StudioFind(page,"Edit proportions visually");
        var update=StudioFind(page,"Update preview");var undo=StudioFind(page,"Undo last grid change");
        var example=page.Controls.OfType<Label>().Single(c=>c.Text.StartsWith("Examples:"));
        var guidance=page.Controls.OfType<Label>().Single(c=>c.Text.StartsWith("Each column"));
        var add=StudioFind(page,"Add layout");var remove=StudioFind(page,"Remove");
        var overview=new StudioPanel{Bounds=new Rectangle(24,20,560,410),AccessibleName="Layout overview card"};
        var catalog=new StudioPanel{Bounds=new Rectangle(24,450,560,220),AccessibleName="Saved layout library"};
        var inspector=new StudioPanel{Bounds=new Rectangle(604,20,380,650),AccessibleName="Layout configuration card"};
        page.Controls.AddRange(new Control[]{overview,catalog,inspector});
        overview.Controls.Add(StudioHeading("Shape your layout",24,18));
        overview.Controls.Add(new Label{Text="Drag a divider. Each column keeps independent heights.",Bounds=new Rectangle(24,53,512,24)});
        StudioMove(preview,overview,18,85,524,230);StudioMove(visual,overview,24,354,250,36);visual.Text="Open larger editor…";
        var hint=new Label{Text="Page Up/Down selects · arrows adjust · Ctrl+Z/Y undoes/redoes",Bounds=new Rectangle(24,319,512,30)};overview.Controls.Add(hint);
        StudioMove(undo,overview,290,354,246,36);
        catalog.Controls.Add(StudioHeading("Your layouts",24,18));
        StudioMove(layouts,catalog,20,54,372,146);StudioMove(add,catalog,410,58,130,36);StudioMove(remove,catalog,410,108,130,36);
        inspector.Controls.Add(StudioHeading("Layout settings",24,18));
        StudioMove(nameLabel,inspector,24,62,332,22);StudioMove(name,inspector,24,91,332,28);
        inspector.Controls.Add(StudioHeading("Grid structure",24,145));
        StudioMove(columnLabel,inspector,24,187,100,22);StudioMove(columns,inspector,24,216,98,30);
        StudioMove(rowLabel,inspector,148,187,180,22);StudioMove(rows,inspector,148,216,98,30);StudioMove(reshape,inspector,24,263,332,36);
        var advanced=new CheckBox{Text="Advanced: numeric weights",AutoSize=true,Location=new Point(24,327),AccessibleName="Show advanced proportions"};inspector.Controls.Add(advanced);
        var precision=new Panel{Bounds=new Rectangle(24,370,332,254),Visible=false,AccessibleName="Advanced proportions"};inspector.Controls.Add(precision);
        StudioMove(proportions,precision,0,0,332,132);StudioMove(example,precision,0,144,332,48);StudioMove(update,precision,0,205,332,36);
        advanced.CheckedChanged+=delegate{precision.Visible=advanced.Checked;};proportions.Columns[2].HeaderText="Row weights";
        example.Text="Widths 1, 2, 1 make a wider center.\nRows 2, 1 make a taller top window.";
        StudioMove(guidance,page,32,692,940,62);guidance.Text="Height changes affect only the adjacent pair in their column.\nLayout edits stay in this draft until Save changes. Live windows shows current assignments separately.";
        layouts.DrawMode=DrawMode.OwnerDrawFixed;layouts.ItemHeight=36;
        layouts.DrawItem+=delegate(object sender,DrawItemEventArgs e){
            if(e.Index<0)return;var p=UiTheme.PaletteFor(this);var profile=(LayoutProfile)layouts.Items[e.Index];bool active=(e.State&DrawItemState.Selected)!=0;
            using(var brush=new SolidBrush(active?p.Selected:p.Panel))e.Graphics.FillRectangle(brush,e.Bounds);
            var model=new LayoutModel(profile);model.Work=new SixSlotSnap.RECT{L=e.Bounds.Left+10,T=e.Bounds.Top+9,R=e.Bounds.Left+40,B=e.Bounds.Bottom-9};
            using(var pen=new Pen(active?p.Text:p.Muted))for(int i=0;i<profile.SlotCount;i++){var r=model.RectFor(i);e.Graphics.DrawRectangle(pen,r.L,r.T,Math.Max(1,r.R-r.L-2),Math.Max(1,r.B-r.T-2));}
            TextRenderer.DrawText(e.Graphics,profile.Name,layouts.Font,new Rectangle(e.Bounds.Left+54,e.Bounds.Top,e.Bounds.Width-60,e.Bounds.Height),p.Text,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);e.DrawFocusRectangle();
        };
    }
    TabPage generalPage;
    Label generalShortcut;
    public void SelectGeneral() {((TabControl)generalPage.Parent).SelectedTab=generalPage;}
    void ConfigureGeneral(TabPage page,TabPage bar,StartupRegistration login,string dataDirectory,Func<string> diagnostics) {
        var groups=new NavigationTabs(false){Dock=DockStyle.Fill,AccessibleName="General settings categories"};
        var startup=new TabPage("Startup & tray");var keys=new TabPage("Flyout shortcut");var support=new TabPage("Support");
        groups.TabPages.AddRange(new[]{startup,bar,keys,support});page.Controls.Add(groups);
        var appearance=new TabPage("Appearance");groups.TabPages.Add(appearance);
        appearance.Controls.Add(new Label{Text="Appearance",Font=new Font(Font.FontFamily,18,FontStyle.Regular),AutoSize=true,Location=new Point(24,26)});
        appearance.Controls.Add(new Label{Text="Choose the appearance of your manager",AutoSize=true,Location=new Point(24,78)});
        var theme=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Bounds=new Rectangle(24,118,300,34),AccessibleName="App appearance"};
        theme.Items.AddRange(new object[]{"System — follow Windows","Light","Dark"});theme.SelectedIndex=(int)Draft.Appearance;appearance.Controls.Add(theme);
        for(int i=0;i<3;i++){int index=i;var swatch=new ThemeChoice{Choice=(AppearanceMode)i,Text=i==0?"Follow Windows":i==1?"Light":"Dark",Bounds=new Rectangle(24+i*240,180,218,156),AccessibleName="Use "+((AppearanceMode)i)+" appearance",Selected=delegate{return theme.SelectedIndex==index;}};swatch.Click+=delegate{theme.SelectedIndex=index;};appearance.Controls.Add(swatch);}
        appearance.Controls.Add(new Label{Text="System follows Windows app mode and accent color automatically.\nLight and Dark override the Windows app mode.\nWindows high contrast always takes priority.",Bounds=new Rectangle(24,374,760,84)});
        appearance.Controls.Add(new Label{Text="Preview changes here immediately. Choose Save changes to keep them.\nCancel restores your saved appearance. Your tiled windows stay in place.",Bounds=new Rectangle(24,480,760,70)});
        theme.SelectedIndexChanged+=delegate{Draft.Appearance=(AppearanceMode)theme.SelectedIndex;dirty=true;UiTheme.Refresh(this);};
        foreach(TabPage child in groups.TabPages)child.AutoScroll=true;
        startup.Controls.Add(StartupControls.Create(login,Confirm,Error));
        var locked=new CheckBox{Text="Lock bar position (prevent dragging)",AutoSize=true,Location=new Point(24,250),AccessibleName="Lock Layouts bar position",Checked=Draft.LauncherPositionLocked};
        locked.CheckedChanged+=delegate{Draft.LauncherPositionLocked=locked.Checked;dirty=true;};bar.Controls.Add(locked);
        keys.Controls.Add(new Label{Text="Open the layout flyout",AutoSize=true,Location=new Point(24,28)});
        generalShortcut=new Label{Text=Draft.FlyoutShortcut.ToString(),Bounds=new Rectangle(24,70,680,28),AccessibleName="Current flyout shortcut"};keys.Controls.Add(generalShortcut);
        var change=new StudioButton{Text="Change shortcut...",Bounds=new Rectangle(24,115,190,34),AccessibleName="Change flyout shortcut"};keys.Controls.Add(change);
        change.Click+=delegate{using(var dialog=new ShortcutDialog(Draft.FlyoutShortcut))if(dialog.ShowDialog(this)==DialogResult.OK){if(!dialog.Value.Enabled){Error("Keep a flyout shortcut","Assign a shortcut to open the layout flyout.");return;}SetShortcut(0,dialog.Value);}};
        keys.Controls.Add(new Label{Text="This is the same opening shortcut shown in Hotkeys. Changes apply on Save changes.\nConflicts are checked before saving. Cancel keeps the current shortcuts.\n\nThe tray menu remains available if a shortcut conflicts with another app.",Bounds=new Rectangle(24,174,790,120)});
        support.Controls.Add(new Label{Text="Windows Tile Studio  "+SixSlotSnap.ProductVersion,AutoSize=true,Location=new Point(24,28)});
        support.Controls.Add(new Label{Text="Runs locally, without an AI service or telemetry.\n\nClosing settings keeps the manager running. Use tray Exit to quit.\nLinked height resizing affects only the adjacent pair in its column.\n\nLocal settings, workspace files and logs can contain personal paths, display identities\nand optional window titles. Review files before sharing them.",Bounds=new Rectangle(24,70,790,150)});
        var folderButton=new StudioButton{Text="Open data folder",Bounds=new Rectangle(24,240,180,34),AccessibleName="Open settings data folder",Enabled=dataDirectory!=null};
        var logButton=new StudioButton{Text="Open local log",Bounds=new Rectangle(222,240,180,34),AccessibleName="Open local status log",Enabled=dataDirectory!=null};support.Controls.AddRange(new Control[]{folderButton,logButton});
        folderButton.Click+=delegate{try{if(!Directory.Exists(dataDirectory))throw new IOException("The data folder is unavailable.");Process.Start(new ProcessStartInfo{FileName=dataDirectory,UseShellExecute=true});}catch(Exception e){Error("Could not open data folder",e.GetBaseException().Message);}};
        logButton.Click+=delegate{try{string log=Path.Combine(dataDirectory,"status.log");if(!File.Exists(log))throw new IOException("No local status log exists yet.");Process.Start(new ProcessStartInfo{FileName=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"System32","notepad.exe"),Arguments="\""+log+"\"",UseShellExecute=false});}catch(Exception e){Error("Could not open log",e.GetBaseException().Message);}};
        support.Controls.Add(new Label{Text="To share layouts, use Share layouts: its export excludes app/window and display identities.",Bounds=new Rectangle(24,305,790,65)});
        var report=new StudioButton{Text="Preview diagnostic report...",Bounds=new Rectangle(24,375,235,34),AccessibleName="Preview diagnostic report",Enabled=diagnostics!=null};support.Controls.Add(report);
        report.Click+=delegate{try{DiagnosticReport.ShowPreview(this,diagnostics());}catch(Exception e){Error("Report unavailable",e.GetBaseException().Message);}};
        support.Controls.Add(new Label{Text="Report contains version, environment, counts, grid shapes and process-memory/CPU samples.\nNo logs, titles, app paths, layout names, shortcuts, window IDs or monitor identities. Nothing is uploaded.",Bounds=new Rectangle(24,425,790,52)});
        UiLayout.ScrollContent(startup.Controls[0] as ScrollableControl);
        foreach(TabPage child in groups.TabPages)StudioWrap(child);
        groups.AddNavigation(page,false);
    }
}

public static class StartupControls {
    public static Control Create(StartupRegistration startup,Func<string,string,bool> confirm,Action<string,string> error) {
        var page=new Panel{Dock=DockStyle.Top,Height=465,Width=820,AccessibleName="Startup and tray options"};
        page.Controls.Add(new Label{Text="Start with Windows (optional)",AutoSize=true,Location=new Point(24,25)});
        page.Controls.Add(new Label{Text="Starts this manager quietly for your account, retaining your saved Layouts bar visibility.\nIt never restores a workspace or launches saved apps automatically.\nClosing/minimizing the bar hides it to tray; tray Exit quits. Startup actions apply immediately,\neven if you later Cancel other settings edits.",Bounds=new Rectangle(24,65,770,82)});
        var label=new Label{Bounds=new Rectangle(24,155,770,54),AccessibleName="Login startup status"};page.Controls.Add(label);
        var command=new TextBox{Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Bounds=new Rectangle(24,216,770,88),AccessibleName="Login startup command"};page.Controls.Add(command);
        var enable=new StudioButton{Text="Enable for this installation...",Bounds=new Rectangle(24,325,245,35),AccessibleName="Enable login startup"};
        var disable=new StudioButton{Text="Disable / remove startup...",Bounds=new Rectangle(281,325,235,35),AccessibleName="Disable login startup"};
        var refresh=new StudioButton{Text="Refresh status",Bounds=new Rectangle(528,325,145,35)};page.Controls.AddRange(new Control[]{enable,disable,refresh});
        if(startup==null){label.Text="Startup controls are unavailable in this preview. Open settings from the running manager.";enable.Enabled=disable.Enabled=refresh.Enabled=false;return page;}
        Action update=delegate{try{label.Text=startup.Status;command.Text=startup.Launch.Command;}catch(Exception e){label.Text="Startup status unavailable: "+e.GetBaseException().Message;}};update();refresh.Click+=delegate{update();};
        enable.Click+=delegate{try{if(!confirm("Enable login startup?","Register the displayed command for this Windows account? An older Tile Studio entry may be replaced. This applies immediately; workspace restore and app relaunch stay manual."))return;startup.Enable(true);update();}catch(Exception e){error("Startup action failed",e.GetBaseException().Message);update();}};
        disable.Click+=delegate{try{if(!confirm("Remove login startup?","Remove this app's login startup entry now? Tile Studio keeps running; future login launch stops. Unrelated entries are preserved."))return;startup.Disable();update();}catch(Exception e){error("Startup action failed",e.GetBaseException().Message);update();}};
        return page;
    }
}
