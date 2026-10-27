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
        var renamed=s.Profiles[0].Clone(); renamed.Name="Appearance-only example";
        Check(s.Profiles[0].GeometryMatches(renamed),"A renamed layout must not trigger geometry reflow");
        renamed.Columns[0].Rows[0]+=0.05;
        Check(!s.Profiles[0].GeometryMatches(renamed),"Changed proportions must trigger geometry reflow");
        string warning; var loaded=SettingsStore.Load(path,out warning);
        Check(warning==null && loaded.Profiles.Count==6,"Settings round trip failed");
        Check(loaded.FlyoutShortcut.ToString()=="Ctrl+Alt+Win+Space","Flyout default changed");
        Check(!loaded.LauncherPositionSet,"Old/default settings must use the clock-area launcher position");
        s.LauncherPositionSet=true; s.LauncherX=-1200; s.LauncherY=800;
        var positionCopy=s.Clone(); SettingsStore.Save(path,positionCopy); loaded=SettingsStore.Load(path,out warning);
        Check(warning==null && loaded.LauncherPositionSet && loaded.LauncherX==-1200 && loaded.LauncherY==800,"Dragged launcher position did not survive clone/save/load");
        var constrained=LauncherTab.ClampPosition(new System.Drawing.Point(-3000,3000),new System.Drawing.Size(340,34),new System.Drawing.Rectangle(-1920,0,1920,1040));
        Check(constrained.X==-1920 && constrained.Y==1006,"Launcher was not kept inside a negative-origin work area");
        var candidate=s.Clone(); candidate.SlotShortcuts[0]=candidate.FlyoutShortcut.Clone(); Reject(candidate.Validate,"Duplicate shortcut accepted");
        candidate=s.Clone(); candidate.Profiles[0].Columns[0].Width=double.NaN; Reject(candidate.Validate,"NaN weight accepted");
        candidate=s.Clone(); candidate.FlyoutShortcut=new Shortcut(); Reject(candidate.Validate,"Unreachable flyout accepted");
        candidate=s.Clone(); candidate.Profiles[1].Id=candidate.Profiles[0].Id; Reject(candidate.Validate,"Duplicate profile ID accepted");
        candidate=s.Clone(); candidate.Profiles[0].Columns[0].Rows[0]=0; Reject(candidate.Validate,"Zero row accepted");
        candidate=s.Clone(); candidate.SlotShortcuts[0]=new Shortcut{Modifiers=ShortcutModifiers.Shift,Key=Keys.A}; Reject(candidate.Validate,"Shift-only global shortcut accepted");
        s.Profiles[0].Name="Saved custom layout"; SettingsStore.Save(path,s);
        Check(File.Exists(path+".bak"),"Settings replacement lost the previous copy");
        Check(SettingsStore.Load(path,out warning).Profiles[0].Name==s.Profiles[0].Name,"Edited settings not saved");
        string beforeFailedSave=File.ReadAllText(path); bool saveFailed=false;
        using(var locked=File.Open(path,FileMode.Open,FileAccess.Read,FileShare.None)) {
            try { SettingsStore.Save(path,s); } catch(IOException) { saveFailed=true; }
        }
        Check(saveFailed && File.ReadAllText(path)==beforeFailedSave,"A blocked save replaced the original settings");
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
    static void MinimumConstraints() {
        var p=LayoutProfile.Create("minimums","Minimum constraints",2,3); var model=new LayoutModel(p){Work=new SixSlotSnap.RECT{R=2000,B=1200}};
        model.MinimumWidths[1]=800; var source=model.RectFor(0); var proposed=source;proposed.R=1800;
        Check(model.Adjust(0,source,proposed),"A feasible constrained width did not adjust");
        var neighbor=model.RectFor(1);Check(neighbor.R-neighbor.L>=800,"A neighbor's advertised width minimum was violated");
        var top=model.RectFor(0);model.MinimumHeights[4]=350;source=model.RectFor(2);proposed=source;proposed.B=1100;
        Check(model.Adjust(2,source,proposed),"A feasible constrained height did not adjust");
        neighbor=model.RectFor(4);Check(neighbor.B-neighbor.T>=350 && model.RectFor(0).B==top.B,"Height minimum negotiation changed a nonadjacent window");
        var before=p.Clone();model.MinimumHeights[2]=700;model.MinimumHeights[4]=700;source=model.RectFor(2);proposed=source;proposed.B+=100;
        Check(!model.Adjust(2,source,proposed) && p.GeometryMatches(before),"Impossible height constraints changed the grid");
        model.MinimumWidths[0]=1400;model.MinimumWidths[1]=1400;source=model.RectFor(0);proposed=source;proposed.R+=100;
        Check(!model.Adjust(0,source,proposed) && p.GeometryMatches(before),"Impossible width constraints changed the grid");Cover(model);
    }
    static void Monitors(string root) {
        var settings=AppSettings.Defaults();var state=new MonitorWorkspace(settings);
        var primary=new DisplayDescriptor{Id="sim-primary",Name="Primary",Primary=true,Bounds=new SixSlotSnap.RECT{R=5120,B=2160},Work=new SixSlotSnap.RECT{R=5120,B=2112}};
        var secondary=new DisplayDescriptor{Id="sim-secondary",Name="Left",Bounds=new SixSlotSnap.RECT{L=-2560,T=-500,R=0,B=940},Work=new SixSlotSnap.RECT{L=-2560,T=-500,R=0,B=900}};
        Check(state.UpdateDisplays(new[]{primary,secondary}),"Initial display inventory ignored");
        state.Activate(primary);state.Active.Model.Profile.Columns[1].Rows=new System.Collections.Generic.List<double>{0.6,0.4};state.Active.Windows[0]=new IntPtr(101);state.Capture();
        var first=state.Active;state.Activate(secondary);state.SelectProfile("four-grid");
        state.Active.Model.Profile.Columns[0].Rows=new System.Collections.Generic.List<double>{0.7,0.3};state.Active.Windows[0]=new IntPtr(202);var second=state.Active;state.Capture();
        Cover(first.Model);Cover(second.Model);
        state.Activate(primary);Check(state.Active.Windows[0]==new IntPtr(101) && state.Active.Model.Profile.Id=="six-grid" && state.Active.Model.Profile.Columns[1].Rows[0]==0.6,"Monitor switching lost independent profile/assignment/ratios");
        Check(second.Windows[0]==new IntPtr(202) && second.Model.Profile.Columns[0].Rows[0]==0.7,"Primary switch overwrote secondary");
        var beforeSecond=second.Model.Profile.Clone();var source=first.Model.RectFor(1);var after=source;after.B+=60;first.Model.Adjust(1,source,after);
        Check(second.Model.Profile.GeometryMatches(beforeSecond) && first.Model.HeightAffectedSlots.Length==2,"Height resizing crossed monitors");state.Capture();
        string path=Path.Combine(root,"monitor-settings.xml");SettingsStore.Save(path,settings);string warning;var loaded=SettingsStore.Load(path,out warning);
        Check(warning==null && loaded.Monitors.Count==2,"Monitor settings did not round trip");
        var restart=new MonitorWorkspace(loaded);restart.UpdateDisplays(new[]{primary,secondary});restart.Activate(secondary);
        Check(restart.Active.Model.Profile.Id=="four-grid" && restart.Active.Model.Profile.Columns[0].Rows[0]==0.7 && restart.Active.Windows.All(w=>w==IntPtr.Zero),"Restart lost profile or persisted transient HWNDs");
        var cloned=loaded.Clone();cloned.Monitors[0].Profiles[0].Columns[0].Rows[0]+=0.1;
        Check(!cloned.Monitors[0].Profiles[0].GeometryMatches(loaded.Monitors[0].Profiles[0]),"Monitor clone shares mutable geometry");
        cloned=loaded.Clone();cloned.Monitors.Add(cloned.Monitors[0].Clone());Reject(cloned.Validate,"Duplicate monitor identity accepted");
        state.Activate(secondary);state.UpdateDisplays(new[]{primary});
        Check(state.Active==first && !second.Connected && second.Windows.All(w=>w==IntPtr.Zero) && first.Windows[0]==new IntPtr(101),"Disconnect disturbed the other display or retained live ownership");
        state.UpdateDisplays(new[]{primary,secondary});state.Activate(secondary);
        Check(second.Connected && second.NeedsRecovery && second.Windows.All(w=>w==IntPtr.Zero) && second.Model.Profile.Columns[0].Rows[0]==0.7,"Reconnect lost saved geometry or resurrected HWNDs");
        first.NeedsReflow=false;second.NeedsReflow=false;
        var changed=new DisplayDescriptor{Id=secondary.Id,Name=secondary.Name,Bounds=secondary.Bounds,Work=new SixSlotSnap.RECT{L=-2560,T=-500,R=0,B=850}};
        state.UpdateDisplays(new[]{primary,changed});Check(second.NeedsReflow && !first.NeedsReflow && second.Model.Work.B==850,"Work-area change affected the wrong display");Cover(second.Model);
        state.UpdateDisplays(new[]{primary,changed});Check(state.Active==second,"Unchanged topology lost active display");
        state.Activate(primary);state.Capture();var opened=settings.Clone();opened.Profiles=settings.Profiles.Select(p=>state.Resolve(primary.Id,p.Id)).ToList();var draft=opened.Clone();
        first.Model.Profile.Columns[0].Rows=new System.Collections.Generic.List<double>{0.55,0.45};
        draft.ShowLauncherTab=false;draft.Profiles[0].Name="Renamed on both displays";
        var edited=draft.Profiles.First(p=>p.Id=="four-grid");edited.Columns[0].Rows=new System.Collections.Generic.List<double>{0.8,0.2};
        state.PrepareDraft(draft,opened);draft.Validate();
        Check(draft.Profiles[0].Columns[0].Rows[0]==0.55,"Appearance save overwrote a concurrent resize");
        Check(!state.Rebind(draft),"Unchanged active geometry requested a reflow");
        Check(state.Resolve(primary.Id,"four-grid").Columns[0].Rows[0]==0.8 && state.Resolve(secondary.Id,"four-grid").Columns[0].Rows[0]==0.7,"Editing an inactive layout overwrote another monitor or was lost");
        var removed=draft.Clone();removed.Profiles.RemoveAll(p=>p.Id=="four-grid");removed.Validate();state.Rebind(removed);
        Check(second.Model.Profile.Id!="four-grid" && second.Windows.All(w=>w==IntPtr.Zero),"Removed profile left a stale monitor model");
        var reshaped=removed.Clone();reshaped.Profiles[0].Columns[0].Rows.Add(1);reshaped.Profiles[0].Normalize();reshaped.Validate();
        Check(state.Rebind(reshaped) && first.Windows.All(w=>w==IntPtr.Zero) && first.Model.Profile.Columns[0].Rows.Count==3,"Shared grid shape change retained stale assignments");
        var old=AppSettings.Defaults();SettingsStore.Save(path,old);var xml=File.ReadAllText(path).Replace("  <Monitors />\r\n","").Replace("  <Monitors />\n","");File.WriteAllText(path,xml);
        old=SettingsStore.Load(path,out warning);Check(warning==null && old.Monitors.Count==0,"Pre-monitor schema migration failed");
        var migrated=new MonitorWorkspace(old);migrated.UpdateDisplays(new[]{primary});Check(migrated.Active.Model.Profile.Id==old.ActiveProfileId,"First monitor did not inherit old selected layout");
        Check(LauncherTab.SizeForScale(1.25)==new System.Drawing.Size(338,34) && LauncherTab.SizeForScale(2)==new System.Drawing.Size(540,54),"Launcher physical scaling failed");
    }
    static int Main() {
        try {
            foreach(var p in AppSettings.Defaults().Profiles) { Exercise(p,0,0); Exercise(p,-5120,40); }
            for(int c=1;c<=4;c++) for(int r=1;r<=4;r++) Exercise(LayoutProfile.Create("test","Test",c,r),0,0);
            var ragged=LayoutProfile.Create("ragged","Mixed column rows",3,2); ragged.Columns[1].Rows.Add(1); ragged.Columns[2].Rows.RemoveAt(1); Exercise(ragged,-5120,-2160);
            Config(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-data"));
            HeightPair();
            MinimumConstraints();
            Monitors(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-data"));
            Console.WriteLine("PASS: layout geometry, independent row dividers, mixed row counts, settings round-trip/recovery, duplicate hotkeys, and legacy migration ("+checks+" checks). No desktop windows moved."); return 0;
        } catch(Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
