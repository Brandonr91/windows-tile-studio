using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

class GridTests {
    static int checks;
    static void Check(bool condition,string reason) { checks++; if(!condition) throw new Exception(reason); }
    static void Reject(Action action,string reason) { try { action(); } catch(ArgumentException) { checks++; return; } throw new Exception(reason); }
    static void Cover(LayoutModel g) {
        int count=g.Profile.SlotCount; Check(count>=1 && count<=16,"Invalid zone count");
        for(int c=0;c<g.Profile.Columns.Count;c++) {
            int rows=g.Profile.Columns[c].Rows.Count;
            for(int r=0;r<rows;r++) {
                int i=g.IndexOf(c,r); int actualColumn,actualRow; Check(g.Locate(i,out actualColumn,out actualRow) && actualColumn==c && actualRow==r,"Zone index mapping changed");
                var slot=g.RectFor(i); Check(slot.R>slot.L && slot.B>slot.T,"Collapsed zone");
                Check(slot.L>=g.Work.L && slot.R<=g.Work.R && slot.T>=g.Work.T && slot.B<=g.Work.B,"Zone outside monitor");
                if(r==0) Check(slot.T==g.Work.T,"Top gap"); else Check(g.RectFor(g.IndexOf(c,r-1)).B==slot.T,"Gap between rows");
                if(r==rows-1) Check(slot.B==g.Work.B,"Bottom gap");
                if(c==0) Check(slot.L==g.Work.L,"Left gap"); else Check(g.RectFor(g.IndexOf(c-1,0)).R==slot.L,"Gap between columns");
                if(c==g.Profile.Columns.Count-1) Check(slot.R==g.Work.R,"Right gap");
                var top=g.RectFor(g.IndexOf(c,0)); Check(top.L==slot.L && top.R==slot.R,"Column widths diverged between rows");
            }
        }
        var copy=g.Profile.Clone(); copy.Validate();
    }
    static void Exercise(LayoutProfile profile,int x,int y) {
        profile=profile.Clone(); profile.Validate();
        var g=new LayoutModel(profile){Work=new SixSlotSnap.RECT{L=x,T=y,R=x+5120,B=y+2160}};
        Cover(g);
        for(int slot=0;slot<profile.SlotCount;slot++) {
            int c,r; g.Locate(slot,out c,out r);
            var before=g.RectFor(slot); var moved=before; moved.L+=35; moved.R+=35; moved.T+=40; moved.B+=40;
            Check(!g.Adjust(slot,before,moved),"Move was treated as resizing");
            var heights=profile.Columns.Select(col=>col.Rows.ToArray()).ToArray();
            var width=before; if(c>0) width.L+=40; else width.R-=40;
            g.Adjust(slot,before,width);
            Check(g.HeightAffectedSlots.Length==0,"Width-only resize claimed a height boundary");
            for(int k=0;k<heights.Length;k++) Check(heights[k].SequenceEqual(profile.Columns[k].Rows),"Column resizing changed a row divider");
            Cover(g);
            before=g.RectFor(slot); var height=before; if(r>0) height.T+=30; else height.B+=30;
            var widths=profile.Columns.Select(col=>col.Width).ToArray();
            bool heightChanged=g.Adjust(slot,before,height);
            Check(g.WidthAffectedSlots.Length==0,"Height-only resize claimed a width boundary");
            Check(g.HeightAffectedSlots.Length==(heightChanged?2:0),"A height boundary did not own exactly two windows");
            foreach(int affected in g.HeightAffectedSlots) { int affectedColumn,affectedRow; g.Locate(affected,out affectedColumn,out affectedRow); Check(affectedColumn==c,"Height scope reached a flanking column"); }
            Check(widths.SequenceEqual(profile.Columns.Select(col=>col.Width)),"Row resizing changed column widths");
            for(int k=0;k<heights.Length;k++) if(k!=c) Check(heights[k].SequenceEqual(profile.Columns[k].Rows),"Unrelated column row dividers moved");
            Cover(g);
        }
        var random=new Random(42);
        for(int n=0;n<150;n++) {
            int slot=random.Next(profile.SlotCount); int c,r; g.Locate(slot,out c,out r);
            var before=g.RectFor(slot); var after=before;
            if(c>0) after.L+=random.Next(-5000,5000); else after.R+=random.Next(-5000,5000);
            if(r>0) after.T+=random.Next(-3000,3000); else after.B+=random.Next(-3000,3000);
            g.Adjust(slot,before,after); Cover(g);
            Check(g.HeightAffectedSlots.Length<=2,"An adjustment resized more than one height pair");
        }
    }
    static void Config(string root) {
        Directory.CreateDirectory(root); string path=Path.Combine(root,"test-settings.xml");
        var s=AppSettings.Defaults(); s.Validate(); SettingsStore.Save(path,s);
        string warning; var loaded=SettingsStore.Load(path,out warning);
        Check(warning==null && loaded.Profiles.Count==6,"Settings round trip failed");
        Check(loaded.FlyoutShortcut.ToString()=="Ctrl+Alt+Win+Space","Flyout default changed");
        var candidate=s.Clone(); candidate.SlotShortcuts[0]=candidate.FlyoutShortcut.Clone(); Reject(candidate.Validate,"Duplicate shortcut accepted");
        candidate=s.Clone(); candidate.Profiles[0].Columns[0].Width=double.NaN; Reject(candidate.Validate,"NaN weight accepted");
        candidate=s.Clone(); candidate.FlyoutShortcut=new Shortcut(); Reject(candidate.Validate,"Unreachable flyout accepted");
        candidate=s.Clone(); candidate.Profiles[1].Id=candidate.Profiles[0].Id; Reject(candidate.Validate,"Duplicate profile ID accepted");
        candidate=s.Clone(); candidate.Profiles[0].Columns[0].Rows[0]=0; Reject(candidate.Validate,"Zero row accepted");
        candidate=s.Clone(); candidate.SlotShortcuts[0]=new Shortcut{Modifiers=ShortcutModifiers.Shift,Key=Keys.A}; Reject(candidate.Validate,"Shift-only global shortcut accepted");
        s.Profiles[0].Name="Saved custom layout"; SettingsStore.Save(path,s);
        Check(File.Exists(path+".bak"),"Settings replacement lost the previous copy");
        Check(SettingsStore.Load(path,out warning).Profiles[0].Name==s.Profiles[0].Name,"Edited settings not saved");
        File.WriteAllText(path,"not xml"); var recovered=SettingsStore.Load(path,out warning);
        Check(warning!=null && recovered.Profiles.Count==6 && File.ReadAllText(path)=="not xml","Malformed settings were not preserved");
        string legacy=Path.Combine(root,"legacy-layout.txt"); File.WriteAllLines(legacy,new[]{"0.3","0.7","0.45","0.6","0.55"});
        var migrated=AppSettings.Defaults(); SettingsStore.MigrateLegacy(legacy,migrated); migrated.Validate();
        Check(Math.Abs(migrated.Profiles[0].Columns[1].Width-0.4)<1e-9 && migrated.Profiles[0].Columns[1].Rows[0]==0.6,"Legacy ratios did not migrate");
    }
    static void HeightPair() {
        var profile=LayoutProfile.Create("pair","Pair isolation",1,3); var model=new LayoutModel(profile){Work=new SixSlotSnap.RECT{L=0,T=0,R=1500,B=2160}};
        var top=model.RectFor(0); var middle=model.RectFor(1); var proposed=middle; proposed.T-=30; proposed.B+=70;
        Check(model.Adjust(1,middle,proposed),"Middle-row resize was ignored");
        Check(model.HeightAffectedSlots.SequenceEqual(new[]{1,2}),"A two-edge app adjustment affected more than the selected adjacent pair");
        var unchanged=model.RectFor(0); Check(top.T==unchanged.T && top.B==unchanged.B,"Height adjustment moved the uninvolved third window");
        Cover(model);
    }
    static int Main() {
        try {
            foreach(var p in AppSettings.Defaults().Profiles) { Exercise(p,0,0); Exercise(p,-5120,40); }
            for(int c=1;c<=4;c++) for(int r=1;r<=4;r++) Exercise(LayoutProfile.Create("test","Test",c,r),0,0);
            var ragged=LayoutProfile.Create("ragged","Mixed column rows",3,2); ragged.Columns[1].Rows.Add(1); ragged.Columns[2].Rows.RemoveAt(1); Exercise(ragged,-5120,-2160);
            Config(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-data"));
            HeightPair();
            Console.WriteLine("PASS: layout geometry, independent row dividers, mixed row counts, settings round-trip/recovery, duplicate hotkeys, and legacy migration ("+checks+" checks). No desktop windows moved."); return 0;
        } catch(Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
