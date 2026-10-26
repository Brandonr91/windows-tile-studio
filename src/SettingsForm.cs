using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

public class ShortcutDialog : Form {
    readonly CheckBox ctrl=new CheckBox{Text="Ctrl",AutoSize=true},alt=new CheckBox{Text="Alt",AutoSize=true},shift=new CheckBox{Text="Shift",AutoSize=true},win=new CheckBox{Text="Win",AutoSize=true};
    readonly ComboBox key=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=150};
    public Shortcut Value;
    public ShortcutDialog(Shortcut initial) {
        Text="Choose shortcut"; Font=new Font("Segoe UI",9); ClientSize=new Size(390,166); FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false; MinimizeBox=false; StartPosition=FormStartPosition.CenterParent;
        var modifiers=new FlowLayoutPanel{Bounds=new Rectangle(15,17,360,30)}; modifiers.Controls.AddRange(new Control[]{ctrl,alt,shift,win}); Controls.Add(modifiers);
        ctrl.Checked=(initial.Modifiers&ShortcutModifiers.Control)!=0; alt.Checked=(initial.Modifiers&ShortcutModifiers.Alt)!=0; shift.Checked=(initial.Modifiers&ShortcutModifiers.Shift)!=0; win.Checked=(initial.Modifiers&ShortcutModifiers.Windows)!=0;
        var allowed=new List<Keys>(); allowed.Add(Keys.None);
        for(int i=0;i<10;i++) allowed.Add((Keys)((int)Keys.D0+i)); for(int i=0;i<26;i++) allowed.Add((Keys)((int)Keys.A+i)); for(int i=0;i<24;i++) allowed.Add((Keys)((int)Keys.F1+i));
        allowed.AddRange(new[]{Keys.Space,Keys.Tab,Keys.Left,Keys.Right,Keys.Up,Keys.Down,Keys.Home,Keys.End,Keys.PageUp,Keys.PageDown});
        foreach(var k in allowed) key.Items.Add(k); key.SelectedItem=initial.Key; if(key.SelectedIndex<0) key.SelectedIndex=0;
        key.AccessibleName="Shortcut key"; key.Location=new Point(15,62); Controls.Add(key);
        var save=new Button{Text="Use shortcut",Bounds=new Rectangle(235,115,135,31)}; Controls.Add(save); AcceptButton=save;
        var cancel=new Button{Text="Cancel",Bounds=new Rectangle(135,115,90,31),DialogResult=DialogResult.Cancel}; Controls.Add(cancel); CancelButton=cancel;
        save.Click+=delegate {
            var value=new Shortcut{Modifiers=(ctrl.Checked?ShortcutModifiers.Control:0)|(alt.Checked?ShortcutModifiers.Alt:0)|(shift.Checked?ShortcutModifiers.Shift:0)|(win.Checked?ShortcutModifiers.Windows:0),Key=(Keys)key.SelectedItem};
            try { value.Validate(); Value=value; DialogResult=DialogResult.OK; Close(); } catch(Exception e) { MessageBox.Show(this,e.Message,"Shortcut",MessageBoxButtons.OK,MessageBoxIcon.Information); }
        };
    }
}

public class SnapSettingsForm : Form {
    public Func<AppSettings,string> SaveRequested;
    public readonly AppSettings Draft;
    readonly ListBox layouts=new ListBox();
    readonly TextBox name=new TextBox();
    readonly NumericUpDown columns=new NumericUpDown{Minimum=1,Maximum=4};
    readonly NumericUpDown rows=new NumericUpDown{Minimum=1,Maximum=4};
    readonly DataGridView proportions=new DataGridView();
    readonly DataGridView shortcuts=new DataGridView();
    readonly Panel preview=new Panel();
    readonly CheckBox launcher=new CheckBox{Text="Show the small layout button at the top of the screen",AutoSize=true};
    LayoutProfile selected;
    bool refreshing;
    public SnapSettingsForm(AppSettings source) {
        Draft=source.Clone(); Text="Six Slot Snap — layouts and hotkeys"; AccessibleName="Six Slot Snap settings";
        AutoScaleMode=AutoScaleMode.None; Font=new Font("Segoe UI",9); ClientSize=new Size(900,625); FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false; StartPosition=FormStartPosition.CenterScreen;
        var tabs=new TabControl{Dock=DockStyle.Fill}; Controls.Add(tabs);
        var layoutPage=new TabPage("Layouts"); var shortcutPage=new TabPage("Hotkeys"); tabs.TabPages.AddRange(new[]{layoutPage,shortcutPage});
        var footer=new Panel{Dock=DockStyle.Bottom,Height=57}; Controls.Add(footer); tabs.BringToFront();
        var save=new Button{Text="Save changes",Anchor=AnchorStyles.Right|AnchorStyles.Top,Bounds=new Rectangle(742,12,140,32)}; footer.Controls.Add(save);
        var cancel=new Button{Text="Cancel",Anchor=AnchorStyles.Right|AnchorStyles.Top,Bounds=new Rectangle(636,12,96,32),DialogResult=DialogResult.Cancel}; footer.Controls.Add(cancel); CancelButton=cancel;
        footer.Controls.Add(new Label{Text="Changes stay local. No startup or system settings are changed.",AutoSize=true,Location=new Point(15,20)});
        layouts.Bounds=new Rectangle(12,16,216,393); layouts.AccessibleName="Saved layouts"; layoutPage.Controls.Add(layouts);
        var add=new Button{Text="Add layout",Bounds=new Rectangle(12,420,104,31)}; var remove=new Button{Text="Remove",Bounds=new Rectangle(124,420,104,31)}; layoutPage.Controls.Add(add); layoutPage.Controls.Add(remove);
        layoutPage.Controls.Add(new Label{Text="Layout name",Bounds=new Rectangle(250,18,125,22)}); name.Bounds=new Rectangle(380,15,450,26); name.AccessibleName="Layout name"; layoutPage.Controls.Add(name);
        layoutPage.Controls.Add(new Label{Text="Columns",Bounds=new Rectangle(250,59,90,22)}); columns.Bounds=new Rectangle(340,56,65,26); columns.AccessibleName="Number of columns"; layoutPage.Controls.Add(columns);
        layoutPage.Controls.Add(new Label{Text="Rows per column",Bounds=new Rectangle(425,59,130,22)}); rows.Bounds=new Rectangle(555,56,65,26); rows.AccessibleName="Default rows per column"; layoutPage.Controls.Add(rows);
        var reshape=new Button{Text="Set grid size",Bounds=new Rectangle(650,54,180,30)}; layoutPage.Controls.Add(reshape);
        proportions.Bounds=new Rectangle(250,102,580,147); proportions.AllowUserToAddRows=false; proportions.AllowUserToDeleteRows=false; proportions.RowHeadersVisible=false; proportions.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill; proportions.AccessibleName="Column and row proportions";
        proportions.Columns.Add(new DataGridViewTextBoxColumn{Name="column",HeaderText="Column",ReadOnly=true,FillWeight=55});
        proportions.Columns.Add(new DataGridViewTextBoxColumn{Name="width",HeaderText="Width weight",FillWeight=75}); proportions.Columns.Add(new DataGridViewTextBoxColumn{Name="rows",HeaderText="Row weights, separated by commas",FillWeight=180}); layoutPage.Controls.Add(proportions);
        layoutPage.Controls.Add(new Label{Text="Examples: widths 1, 2, 1 create a wider center; rows 2, 1 create a taller top window.",Bounds=new Rectangle(250,258,605,38)});
        var update=new Button{Text="Update preview",Bounds=new Rectangle(250,302,170,30)}; layoutPage.Controls.Add(update);
        preview.Bounds=new Rectangle(250,344,300,172); preview.BackColor=Color.FromArgb(24,28,35); layoutPage.Controls.Add(preview);
        layoutPage.Controls.Add(new Label{Text="Each column keeps its own row dividers.\n\nPicking a zone in another layout moves only the selected window. Re-snap other windows to link them to that layout.",Bounds=new Rectangle(570,350,270,130)});
        launcher.Location=new Point(250,512); launcher.Checked=Draft.ShowLauncherTab; layoutPage.Controls.Add(launcher);
        shortcuts.Dock=DockStyle.Fill; shortcuts.AllowUserToAddRows=false; shortcuts.AllowUserToDeleteRows=false; shortcuts.RowHeadersVisible=false; shortcuts.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill; shortcuts.AccessibleName="Global hotkey assignments";
        shortcuts.Columns.Add(new DataGridViewTextBoxColumn{Name="command",HeaderText="Action",ReadOnly=true,FillWeight=170}); shortcuts.Columns.Add(new DataGridViewTextBoxColumn{Name="binding",HeaderText="Shortcut",ReadOnly=true,FillWeight=160});
        shortcuts.Columns.Add(new DataGridViewButtonColumn{Name="change",HeaderText="",Text="Change",UseColumnTextForButtonValue=true,FillWeight=60}); shortcuts.Columns.Add(new DataGridViewButtonColumn{Name="clear",HeaderText="",Text="Clear",UseColumnTextForButtonValue=true,FillWeight=55}); shortcutPage.Controls.Add(shortcuts);
        shortcuts.CellContentClick+=EditShortcut;
        layouts.SelectedIndexChanged+=delegate {
            if(refreshing) return;
            LayoutProfile next=layouts.SelectedItem as LayoutProfile;
            if(selected!=null && next!=selected && !ReadEditor()) { refreshing=true; layouts.SelectedItem=selected; refreshing=false; return; }
            selected=next; FillEditor();
        };
        add.Click+=delegate {
            if(!ReadEditor()) return;
            if(Draft.Profiles.Count>=12) { MessageBox.Show(this,"The basic version supports up to 12 layouts."); return; }
            int n=1; while(Draft.Profiles.Any(p=>p.Name=="Custom "+n)) n++;
            var created=LayoutProfile.Create(Guid.NewGuid().ToString("N"),"Custom "+n,3,2); Draft.Profiles.Add(created); RefreshList(created); RefreshShortcuts();
        };
        remove.Click+=delegate {
            if(selected==null || Draft.Profiles.Count<=1) return;
            Draft.Profiles.Remove(selected); selected=null; RefreshList(Draft.Profiles[0]); RefreshShortcuts();
        };
        reshape.Click+=delegate {
            if(selected==null) return;
            int nextColumns=(int)columns.Value,nextRows=(int)rows.Value;
            if(!ReadEditor()) return;
            selected.Columns=LayoutProfile.Create("temporary","temporary",nextColumns,nextRows).Columns;
            RefreshList(selected); RefreshShortcuts();
        };
        update.Click+=delegate { if(ReadEditor()) { RefreshList(selected); FillEditor(); RefreshShortcuts(); } };
        save.Click+=delegate {
            if(!ReadEditor()) return;
            Draft.ShowLauncherTab=launcher.Checked;
            try { Draft.Validate(); string error=SaveRequested==null?null:SaveRequested(Draft); if(error!=null) { MessageBox.Show(this,error,"Changes were not saved",MessageBoxButtons.OK,MessageBoxIcon.Warning); return; } DialogResult=DialogResult.OK; Close(); }
            catch(Exception e) { MessageBox.Show(this,e.Message,"Check settings",MessageBoxButtons.OK,MessageBoxIcon.Warning); }
        };
        RefreshList(Draft.Profiles.First(p=>p.Id==Draft.ActiveProfileId)); RefreshShortcuts();
    }
    void RefreshList(LayoutProfile select) { refreshing=true; layouts.DataSource=null; layouts.DataSource=Draft.Profiles; layouts.SelectedItem=select; selected=select; refreshing=false; FillEditor(); }
    void FillEditor() {
        if(selected==null) return;
        name.Text=selected.Name; columns.Value=selected.Columns.Count; rows.Value=selected.Columns[0].Rows.Count;
        proportions.Rows.Clear(); for(int i=0;i<selected.Columns.Count;i++) proportions.Rows.Add(i+1,selected.Columns[i].Width.ToString("0.########",CultureInfo.InvariantCulture),string.Join(", ",selected.Columns[i].Rows.Select(v=>v.ToString("0.########",CultureInfo.InvariantCulture))));
        while(preview.Controls.Count>0) preview.Controls[0].Dispose();
        var card=new LayoutCard(selected,true,1); card.Dock=DockStyle.Fill; card.ZoneSelected=delegate {}; preview.Controls.Add(card);
    }
    bool ReadEditor() {
        if(selected==null) return true;
        try {
            proportions.EndEdit(); var copy=selected.Clone(); copy.Name=name.Text.Trim(); copy.Columns.Clear();
            foreach(DataGridViewRow row in proportions.Rows) {
                double width=double.Parse(Convert.ToString(row.Cells[1].Value),CultureInfo.InvariantCulture);
                var weights=Convert.ToString(row.Cells[2].Value).Split(',').Select(v=>double.Parse(v.Trim(),CultureInfo.InvariantCulture)).ToList();
                copy.Columns.Add(new ColumnDefinition{Width=width,Rows=weights});
            }
            copy.Validate(); selected.Name=copy.Name; selected.Columns=copy.Columns; return true;
        } catch(Exception e) { MessageBox.Show(this,e.Message,"Check layout",MessageBoxButtons.OK,MessageBoxIcon.Warning); return false; }
    }
    void RefreshShortcuts() {
        shortcuts.Rows.Clear(); shortcuts.Rows.Add("Open layout flyout",Draft.FlyoutShortcut);
        for(int i=0;i<16;i++) shortcuts.Rows.Add("Snap to zone "+(i+1),Draft.SlotShortcuts[i]);
        foreach(var p in Draft.Profiles) shortcuts.Rows.Add("Open "+p.Name+" layout",p.Shortcut);
    }
    Shortcut GetShortcut(int row) { if(row==0) return Draft.FlyoutShortcut; if(row<=16) return Draft.SlotShortcuts[row-1]; return Draft.Profiles[row-17].Shortcut; }
    void SetShortcut(int row,Shortcut value) { if(row==0) Draft.FlyoutShortcut=value; else if(row<=16) Draft.SlotShortcuts[row-1]=value; else Draft.Profiles[row-17].Shortcut=value; RefreshShortcuts(); }
    void EditShortcut(object sender,DataGridViewCellEventArgs e) {
        if(e.RowIndex<0 || e.ColumnIndex<2) return;
        if(e.ColumnIndex==3) { if(e.RowIndex==0) { MessageBox.Show(this,"Keep a shortcut assigned to open the flyout."); return; } SetShortcut(e.RowIndex,new Shortcut()); return; }
        using(var dialog=new ShortcutDialog(GetShortcut(e.RowIndex))) if(dialog.ShowDialog(this)==DialogResult.OK) SetShortcut(e.RowIndex,dialog.Value);
    }
}
