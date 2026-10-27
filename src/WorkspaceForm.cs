using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

public class WorkspaceDialog : Form {
    public Func<string,string,bool> ConfirmAction;
    public Action<string,string> ReportError;
    readonly string path;
    readonly Func<string,bool,SavedWorkspace> capture;
    readonly Func<SavedWorkspace,bool,WorkspaceRestorePlan> preview;
    readonly Func<WorkspaceRestorePlan,string> restore;
    readonly Action<SavedPlacement> launch;
    readonly StartupRegistration startup;
    WorkspaceLibrary library;
    WorkspaceRestorePlan plan;
    readonly ComboBox saved=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList};
    readonly TextBox name=new TextBox();
    readonly CheckBox titles=new CheckBox{Text="Save exact window titles for matching (local file)",AutoSize=true};
    readonly CheckBox fallback=new CheckBox{Text="Use current display for disconnected saved displays",AutoSize=true};
    readonly DataGridView grid=new DataGridView();
    readonly Label status=new Label{AutoEllipsis=true};
    bool loading;
    class Choice {public string Label{get;set;}public long Value{get;set;}}
    bool Confirm(string title,string text){return ConfirmAction!=null?ConfirmAction(title,text):MessageBox.Show(this,text,title,MessageBoxButtons.YesNo,MessageBoxIcon.Question,MessageBoxDefaultButton.Button2)==DialogResult.Yes;}
    void Error(string text){status.Text=text;if(ReportError!=null)ReportError("Workspace action failed",text);else MessageBox.Show(this,text,"Workspace action failed",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
    public WorkspaceDialog(string file,Func<string,bool,SavedWorkspace> captureWorkspace,Func<SavedWorkspace,bool,WorkspaceRestorePlan> previewWorkspace,Func<WorkspaceRestorePlan,string> restoreWorkspace,Action<SavedPlacement> launchApp,StartupRegistration login) {
        path=file;capture=captureWorkspace;preview=previewWorkspace;restore=restoreWorkspace;launch=launchApp;startup=login;
        Text="Windows Tile Studio — workspaces & startup";AccessibleName=Text;ClientSize=new Size(980,670);MinimumSize=new Size(900,650);StartPosition=FormStartPosition.CenterScreen;AutoScaleMode=AutoScaleMode.None;
        var tabs=new TabControl{Dock=DockStyle.Fill};var page=new TabPage("Workspaces");var loginPage=new TabPage("Login startup");tabs.TabPages.AddRange(new[]{page,loginPage});Controls.Add(tabs);
        page.Controls.Add(new Label{Text="Capture only assigned windows. Workspace files stay local and contain app paths and display identities.\nTitles are saved only if selected. Capture, deletion, restore and startup actions apply immediately, outside layout drafts.",Bounds=new Rectangle(18,16,930,45)});
        page.Controls.Add(new Label{Text="Name",Bounds=new Rectangle(18,80,50,22)});name.Bounds=new Rectangle(75,76,178,27);name.Text="My workspace";name.AccessibleName="Workspace name";page.Controls.Add(name);titles.Location=new Point(270,80);page.Controls.Add(titles);
        var captureButton=new Button{Text="Capture assigned windows",Bounds=new Rectangle(18,118,220,32),AccessibleName="Capture workspace"};page.Controls.Add(captureButton);
        saved.Bounds=new Rectangle(18,174,300,29);saved.AccessibleName="Saved workspace";page.Controls.Add(saved);
        var refresh=new Button{Text="Preview / refresh",Bounds=new Rectangle(333,173,150,31),AccessibleName="Preview workspace"};page.Controls.Add(refresh);
        var remove=new Button{Text="Delete saved workspace",Bounds=new Rectangle(495,173,190,31)};page.Controls.Add(remove);
        fallback.Location=new Point(18,216);page.Controls.Add(fallback);
        grid.Bounds=new Rectangle(18,252,930,258);grid.Anchor=AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Right|AnchorStyles.Bottom;grid.AllowUserToAddRows=false;grid.AllowUserToDeleteRows=false;grid.RowHeadersVisible=false;grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect;grid.MultiSelect=false;grid.AccessibleName="Workspace restore preview";
        grid.Columns.Add(new DataGridViewCheckBoxColumn{Name="apply",HeaderText="Restore",FillWeight=45});grid.Columns.Add(new DataGridViewTextBoxColumn{Name="app",HeaderText="App",ReadOnly=true,FillWeight=80});grid.Columns.Add(new DataGridViewTextBoxColumn{Name="slot",HeaderText="Zone",ReadOnly=true,FillWeight=35});grid.Columns.Add(new DataGridViewTextBoxColumn{Name="display",HeaderText="Target display",ReadOnly=true,FillWeight=100});grid.Columns.Add(new DataGridViewTextBoxColumn{Name="status",HeaderText="Status",ReadOnly=true,FillWeight=140});grid.Columns.Add(new DataGridViewComboBoxColumn{Name="window",HeaderText="Matching window (choose if ambiguous)",FillWeight=180});page.Controls.Add(grid);
        var apply=new Button{Text="Restore selected windows...",Bounds=new Rectangle(18,527,225,34),Anchor=AnchorStyles.Bottom|AnchorStyles.Left,AccessibleName="Restore workspace"};page.Controls.Add(apply);
        var launchButton=new Button{Text="Launch selected missing app...",Bounds=new Rectangle(259,527,240,34),Anchor=AnchorStyles.Bottom|AnchorStyles.Left,AccessibleName="Launch missing workspace app"};page.Controls.Add(launchButton);
        status.Bounds=new Rectangle(18,573,930,44);status.Anchor=AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;status.Text="Choose a saved workspace, then preview. Restore never launches apps automatically.";status.AccessibleName="Workspace status";page.Controls.Add(status);
        // Dock the preview between fixed controls and actions. Anchoring before
        // a TabPage gets its final size can stretch the grid over its footer.
        var top=new Panel{Dock=DockStyle.Top,Height=248};var footer=new Panel{Dock=DockStyle.Bottom,Height=108};
        foreach(Control control in page.Controls.Cast<Control>().ToArray())if(control!=grid&&control!=apply&&control!=launchButton&&control!=status)top.Controls.Add(control);
        var actions=new FlowLayoutPanel{Dock=DockStyle.Top,Height=50,Padding=new Padding(15,6,0,0)};apply.Anchor=launchButton.Anchor=AnchorStyles.Top|AnchorStyles.Left;actions.Controls.AddRange(new Control[]{apply,launchButton});
        status.Anchor=AnchorStyles.Top|AnchorStyles.Left;status.Dock=DockStyle.Fill;status.Padding=new Padding(18,3,18,3);footer.Controls.Add(status);footer.Controls.Add(actions);status.BringToFront();
        grid.Anchor=AnchorStyles.Top|AnchorStyles.Left;grid.Dock=DockStyle.Fill;page.Controls.Add(footer);page.Controls.Add(top);grid.BringToFront();
        try{library=WorkspaceStore.Load(path);Refill(null);}catch(Exception e){library=new WorkspaceLibrary();status.Text="Workspace file could not be read; it was preserved. Restore workspaces.xml.bak or fix the file, then reopen. "+e.GetBaseException().Message;captureButton.Enabled=remove.Enabled=refresh.Enabled=apply.Enabled=launchButton.Enabled=false;}
        captureButton.Click+=delegate {
            try {
                var item=capture(name.Text.Trim(),titles.Checked);item.Validate();var existing=library.Workspaces.FirstOrDefault(w=>string.Equals(w.Name.Trim(),item.Name.Trim(),StringComparison.OrdinalIgnoreCase));
                if(existing!=null&&!Confirm("Replace saved workspace?","Replace the existing saved workspace '"+item.Name+"'? Real windows will not move."))return;
                if(existing!=null)item.Id=existing.Id;var candidate=new WorkspaceLibrary{Workspaces=library.Workspaces.Where(w=>w!=existing).Concat(new[]{item}).ToList()};WorkspaceStore.Save(path,candidate);library=candidate;Refill(item.Id);status.Text="Captured "+item.Displays.Sum(d=>d.Windows.Count)+" supported assigned windows. Preview before restoring.";
            }catch(Exception e){Error(e.GetBaseException().Message);}
        };
        remove.Click+=delegate {var item=saved.SelectedItem as SavedWorkspace;if(item==null||!Confirm("Delete workspace?","Delete saved workspace '"+item.Name+"'? No apps or windows will be closed."))return;try{var candidate=new WorkspaceLibrary{Workspaces=library.Workspaces.Where(w=>w!=item).ToList()};WorkspaceStore.Save(path,candidate);library=candidate;Refill(null);status.Text="Saved workspace deleted. Windows were not changed.";}catch(Exception e){Error(e.GetBaseException().Message);}};
        refresh.Click+=delegate{RefreshPreview();};saved.SelectedIndexChanged+=delegate{if(!loading){plan=null;grid.Rows.Clear();}};fallback.CheckedChanged+=delegate{plan=null;grid.Rows.Clear();status.Text="Display choice changed. Preview again before restoring.";};
        grid.CurrentCellDirtyStateChanged+=delegate{if(grid.IsCurrentCellDirty)grid.CommitEdit(DataGridViewDataErrorContexts.Commit);};
        grid.CellValueChanged+=delegate(object sender,DataGridViewCellEventArgs e){if(loading||plan==null||e.RowIndex<0)return;var row=plan.Rows[e.RowIndex];if(e.ColumnIndex==grid.Columns["window"].Index){long handle=Convert.ToInt64(grid.Rows[e.RowIndex].Cells["window"].Value);row.Chosen=row.Candidates.FirstOrDefault(w=>w.Handle.ToInt64()==handle);row.ManualChoice=true;row.Selected=row.Issue==null&&row.Chosen!=null;grid.Rows[e.RowIndex].Cells["apply"].Value=row.Selected;grid.Rows[e.RowIndex].Cells["status"].Value=row.Status;}else if(e.ColumnIndex==grid.Columns["apply"].Index)row.Selected=Convert.ToBoolean(grid.Rows[e.RowIndex].Cells["apply"].Value);};
        grid.DataError+=delegate(object sender,DataGridViewDataErrorEventArgs e){e.ThrowException=false;status.Text="A window choice is no longer valid. Refresh the preview.";};
        apply.Click+=delegate {
            try {
                grid.EndEdit();if(plan==null)throw new InvalidOperationException("Preview the workspace first.");var rows=plan.SelectedRows();
                if(!Confirm("Restore workspace?","Move "+rows.Count+" selected windows to the previewed displays and saved proportions? Other managed slots on affected displays may be disconnected without moving their windows. No apps will be launched."))return;
                status.Text=restore(plan);plan=null;grid.Rows.Clear();
            }catch(Exception e){Error(e.GetBaseException().Message);}
        };
        launchButton.Click+=delegate {
            try {
                if(plan==null||grid.CurrentRow==null)throw new InvalidOperationException("Preview, then select a missing-app row.");var row=plan.Rows[grid.CurrentRow.Index];
                if(row.Candidates.Count!=0)throw new InvalidOperationException("This app already has matching windows. Choose one in the preview.");
                if(!Confirm("Launch missing app?","Start this executable without arguments?\n"+row.Saved.ApplicationPath+"\nDocuments, browser pages and app sessions will not be recreated. Wait for it to open, then preview again; no window will be restored automatically."))return;
                launch(row.Saved);status.Text="Launch requested. Wait for the app, then preview again. No window was moved.";
            }catch(Exception e){Error(e.GetBaseException().Message);}
        };
        AddStartup(loginPage);
    }
    void Refill(string id){loading=true;saved.DataSource=null;saved.DataSource=library.Workspaces;if(id!=null)saved.SelectedItem=library.Workspaces.First(w=>w.Id==id);loading=false;plan=null;grid.Rows.Clear();}
    void RefreshPreview() {
        try {
            var item=saved.SelectedItem as SavedWorkspace;if(item==null)throw new InvalidOperationException("Capture or select a saved workspace first.");plan=preview(item,fallback.Checked);loading=true;grid.Rows.Clear();
            foreach(var row in plan.Rows) {
                int index=grid.Rows.Add();var cells=grid.Rows[index].Cells;cells["apply"].Value=row.Selected;cells["app"].Value=Path.GetFileName(row.Saved.ApplicationPath);cells["slot"].Value=row.Saved.Slot+1;string savedName=row.SavedDisplay.DisplayName??("Saved display "+(plan.Workspace.Displays.IndexOf(row.SavedDisplay)+1));cells["display"].Value=row.Target==null?savedName+" disconnected":savedName+" → "+row.Target.Name+(row.Target.Id==row.SavedDisplay.MonitorId?"":" (fallback)");cells["status"].Value=row.Status;
                var choices=new List<Choice>{new Choice{Label="Choose a window...",Value=0}};choices.AddRange(row.Candidates.Select(w=>new Choice{Label=w.ToString(),Value=w.Handle.ToInt64()}));
                cells["window"]=new DataGridViewComboBoxCell{DataSource=choices,DisplayMember="Label",ValueMember="Value",ValueType=typeof(long),Value=row.Chosen==null?0L:row.Chosen.Handle.ToInt64()};
            }
            status.Text="Review matches and target displays. Ambiguous windows require a choice; duplicates and stale windows are rejected.";
        }catch(Exception e){plan=null;grid.Rows.Clear();Error(e.GetBaseException().Message);}finally{loading=false;}
    }
    void AddStartup(TabPage page) {
        page.Controls.Add(new Label{Text="Optional launch at login",Bounds=new Rectangle(24,25,500,30)});
        page.Controls.Add(new Label{Text="This registers only this Tile Studio installation for your Windows account. It does not restore workspaces\nor launch saved apps. Your hidden launcher preference is retained. Nothing is enabled automatically.",Bounds=new Rectangle(24,70,900,60)});
        var label=new Label{Bounds=new Rectangle(24,150,900,60),AccessibleName="Login startup status"};page.Controls.Add(label);
        var command=new TextBox{Multiline=true,ReadOnly=true,Bounds=new Rectangle(24,230,900,105),AccessibleName="Login startup command"};page.Controls.Add(command);
        var enable=new Button{Text="Enable for this installation...",Bounds=new Rectangle(24,365,245,35),AccessibleName="Enable login startup"};var disable=new Button{Text="Disable / remove startup...",Bounds=new Rectangle(287,365,235,35),AccessibleName="Disable login startup"};var refresh=new Button{Text="Refresh status",Bounds=new Rectangle(540,365,150,35)};page.Controls.AddRange(new Control[]{enable,disable,refresh});
        Action update=delegate{try{label.Text=startup.Status;command.Text=startup.Launch.Command;}catch(Exception e){label.Text="Startup status unavailable: "+e.GetBaseException().Message;}};update();refresh.Click+=delegate{update();};
        enable.Click+=delegate{try{if(!Confirm("Enable login startup?","Register the displayed command for this Windows account? An older Tile Studio entry may be replaced. Workspace restore and app relaunch remain manual."))return;startup.Enable(true);update();}catch(Exception e){Error(e.GetBaseException().Message);update();}};
        disable.Click+=delegate{try{if(!Confirm("Remove login startup?","Remove this app's login startup entry? Tile Studio will keep running now; future login launch will stop. Unrelated entries are preserved."))return;startup.Disable();update();}catch(Exception e){Error(e.GetBaseException().Message);update();}};
    }
}
