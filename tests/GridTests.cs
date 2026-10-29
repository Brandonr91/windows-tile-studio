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
        var originalXml=File.ReadAllText(path);string future=originalXml.Replace("</AppSettings>","<FutureSetting>keep-private-value</FutureSetting></AppSettings>");File.WriteAllText(path,future);SettingsStore.Load(path,out warning);Check(warning!=null&&File.ReadAllText(path)==future,"Unknown settings fields were silently discarded");
        future=originalXml.Replace("<AppSettings ","<AppSettings futureAttribute=\"keep-private-value\" ");File.WriteAllText(path,future);SettingsStore.Load(path,out warning);Check(warning!=null&&File.ReadAllText(path)==future,"Unknown settings attributes were silently discarded");
        File.WriteAllText(path,"<!DOCTYPE AppSettings [<!ENTITY test 'private'>]><AppSettings>&test;</AppSettings>");SettingsStore.Load(path,out warning);Check(warning!=null,"Settings XML accepted a DTD");
        File.WriteAllText(path,new string(' ',1048577));SettingsStore.Load(path,out warning);Check(warning!=null,"Oversized settings were accepted");File.WriteAllText(path,originalXml);loaded=SettingsStore.Load(path,out warning);
        Check(!loaded.LauncherPositionSet,"Old/default settings must use the clock-area launcher position");
        Check(!loaded.LauncherPositionLocked,"Old/default settings must leave the bar draggable");
        s.LauncherPositionLocked=true;SettingsStore.Save(path,s.Clone());loaded=SettingsStore.Load(path,out warning);
        Check(warning==null&&loaded.LauncherPositionLocked,"Bar position lock did not survive clone/XML roundtrip");
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
    static void VisualDrafts() {
        var source=LayoutProfile.Create("draft","Draft",3,3);var original=source.Clone();var session=new VisualLayoutSession(source);
        session.Begin();Check(session.Move(false,1,2,0.8),"Visual row divider did not move");session.End(false);
        Check(source.GeometryMatches(original),"Visual edit mutated the source layout");
        Check(session.Profile.Columns[0].Rows.SequenceEqual(original.Columns[0].Rows) && session.Profile.Columns[2].Rows.SequenceEqual(original.Columns[2].Rows),"Visual row edit affected flanking columns");
        Check(session.Profile.Columns[1].Rows[0]==original.Columns[1].Rows[0],"Visual row edit affected a nonadjacent row");
        var changed=session.Profile.Clone();session.Undo();Check(session.Profile.GeometryMatches(original)&&session.CanRedo,"Visual undo lost the gesture snapshot");session.Redo();Check(session.Profile.GeometryMatches(changed),"Visual redo failed");
        session.Begin();session.Move(true,-1,1,0.6);session.End(true);Check(session.Profile.GeometryMatches(changed),"Escape/cancel did not restore visual draft");
        session.Begin();session.Move(true,-1,1,-100);session.End(false);Check(Math.Abs(session.Profile.Columns[0].Width-0.04)<1e-8,"Visual width did not clamp at 4%");
        Check(session.Profile.Columns.All(c=>c.Rows.SequenceEqual(changed.Columns[session.Profile.Columns.IndexOf(c)].Rows)),"Visual width edited row heights");
        Check(!session.Move(true,0,0,0.3)&&!session.Move(false,99,1,0.4)&&!session.Move(true,0,1,double.NaN),"Visual editor accepted invalid edges");
        var random=new Random(711);for(int i=0;i<200;i++){bool widths=i%2==0;session.Begin();session.Move(widths,random.Next(3),random.Next(1,3),random.NextDouble()*2-0.5);session.End(false);session.Profile.Validate();Cover(new LayoutModel(session.Profile){Work=new SixSlotSnap.RECT{L=-3000,T=-100,R=2120,B=2060}});}
    }
    static void Portable(string root) {
        string path=Path.Combine(root,"portable.tilelayouts.xml");var current=AppSettings.Defaults();
        current.ShowLauncherTab=false;current.LauncherPositionSet=true;current.LauncherX=12345;current.Monitors.Add(new MonitorConfiguration{Id="PRIVATE-MONITOR-ID",ActiveProfileId=current.ActiveProfileId});
        PortableLayouts.Export(path,current);string xml=File.ReadAllText(path);var bundle=PortableLayouts.Load(path);
        Check(bundle.Version==1&&bundle.Profiles.Count==6,"Portable round trip failed");
        Check(!xml.Contains("PRIVATE-MONITOR-ID")&&!xml.Contains("LauncherX")&&!xml.Contains("ShowLauncherTab")&&!xml.Contains("Monitors"),"Portable export leaked machine/appearance data");
        var added=PortableLayouts.Candidate(current,bundle,false,false);
        Check(added.Profiles.Count==12&&added.Profiles.Select(p=>p.Id).Distinct().Count()==12&&added.Profiles.Select(p=>p.Name.ToLowerInvariant()).Distinct().Count()==12,"Add copies did not generate independent IDs/names");
        Check(added.Profiles.Skip(6).All(p=>!p.Shortcut.Enabled)&&current.Profiles.Count==6,"Import without shortcuts mutated current settings or kept copied keys");
        Check(added.LauncherX==12345&&!added.ShowLauncherTab&&added.Monitors[0].Id=="PRIVATE-MONITOR-ID","Portable import replaced local appearance/monitor identity");
        var replaced=PortableLayouts.Candidate(current,bundle,true,true);Check(replaced.Profiles.Count==6&&!replaced.Profiles.Any(p=>current.Profiles.Any(c=>c.Id==p.Id)),"Replacement reused local monitor geometry identity");
        Check(replaced.FlyoutShortcut.ToString()==bundle.FlyoutShortcut.ToString(),"Portable global shortcuts were lost");
        Reject(()=>PortableLayouts.Candidate(added,bundle,false,false),"Import beyond profile limit succeeded");
        var conflict=PortableLayouts.FromSettings(current);conflict.Profiles[0].Shortcut=new Shortcut{Modifiers=ShortcutModifiers.Control|ShortcutModifiers.Alt,Key=Keys.F24};current.Profiles[1].Shortcut=conflict.Profiles[0].Shortcut.Clone();
        Reject(()=>PortableLayouts.Candidate(current,conflict,false,true),"Duplicate imported/existing hotkey accepted");
        var invalid=PortableLayouts.FromSettings(AppSettings.Defaults());invalid.Profiles[0].Columns[0].Width=double.NaN;Reject(()=>PortableLayouts.Candidate(current,invalid,true,false),"Invalid portable geometry accepted");
        string[] malformed={"not xml",xml.Replace("<Version>1</Version>","<Version>999</Version>"),xml.Replace("<Version>1</Version>",""),xml.Replace("</TileStudioLayouts>","<Monitors /></TileStudioLayouts>"),"<!DOCTYPE x [<!ENTITY y SYSTEM 'file:///nonexistent'>]><TileStudioLayouts>&y;</TileStudioLayouts>",new string(' ',PortableLayouts.MaximumBytes+1)};
        foreach(string bad in malformed){File.WriteAllText(path,bad);bool rejected=false;try{PortableLayouts.Load(path);}catch(Exception){rejected=true;}Check(rejected,"Malformed/unsafe portable XML accepted");}
        File.WriteAllText(path,string.Concat(Enumerable.Repeat("<nested>",40))+string.Concat(Enumerable.Repeat("</nested>",40)));bool depthRejected=false;try{PortableLayouts.Load(path);}catch(ArgumentException){depthRejected=true;}Check(depthRejected,"Deep portable XML was not rejected before deserialization");
        File.WriteAllText(path,xml);var before=File.ReadAllText(path);bool failed=false;using(var locked=File.Open(path,FileMode.Open,FileAccess.Read,FileShare.None)){try{PortableLayouts.Export(path,AppSettings.Defaults());}catch(IOException){failed=true;}}
        Check(failed&&File.ReadAllText(path)==before,"Failed portable export replaced the destination");
    }
    class FakeStartup : IStartupValue {public string Value;public int Writes,Removals;public string Read(){return Value;}public void Write(string value){Value=value;Writes++;}public void Remove(){Value=null;Removals++;}}
    static void WorkspacesAndStartup(string root) {
        var profile=AppSettings.Defaults().Profiles[0].Clone();string exe=@"C:\Apps\Editor.exe";
        var saved=new SavedWorkspace{Name="Work",MatchTitles=true,Displays={new SavedWorkspaceDisplay{MonitorId="monitor-a",Layout=profile,Windows={new SavedPlacement{ApplicationPath=exe,WindowClass="Editor",WindowTitle="One",Slot=0},new SavedPlacement{ApplicationPath=exe,WindowClass="Editor",WindowTitle="Two",Slot=3}}}}};
        var display=new DisplayDescriptor{Id="monitor-a",Name="A",Primary=true,Work=new SixSlotSnap.RECT{R=5120,B=2160}};
        var one=new LiveWorkspaceWindow{Handle=new IntPtr(101),ProcessId=10,ProcessStarted=100,ApplicationPath=exe,WindowClass="Editor",Title="One"};
        var two=new LiveWorkspaceWindow{Handle=new IntPtr(102),ProcessId=10,ProcessStarted=100,ApplicationPath=exe,WindowClass="Editor",Title="Two"};
        var windows=new[]{one,two};var layouts=new[]{profile};var plan=WorkspacePlanner.Build(saved,windows,new[]{display},layouts,null);
        Check(plan.SelectedRows().Count==2&&plan.Rows[0].Chosen==one&&plan.Rows[1].Chosen==two,"Exact-title workspace preview did not uniquely match windows");
        saved.MatchTitles=false;plan=WorkspacePlanner.Build(saved,windows,new[]{display},layouts,null);Check(plan.Rows.All(r=>!r.Selected&&r.Chosen==null),"Ambiguous app windows were matched by order");
        plan.Rows[0].Chosen=one;plan.Rows[0].Selected=true;plan.Rows[1].Chosen=one;plan.Rows[1].Selected=true;Reject(()=>plan.SelectedRows(),"Duplicate window selection accepted");plan.Rows[1].Chosen=two;Check(plan.SelectedRows().Count==2,"Manual disambiguation failed");
        plan=WorkspacePlanner.Build(saved,new[]{one},new[]{display},layouts,null);Check(plan.Rows.All(r=>!r.Selected),"One current window was automatically assigned to several saved slots");
        plan=WorkspacePlanner.Build(saved,new LiveWorkspaceWindow[0],new[]{display},layouts,null);Check(plan.Rows.All(r=>r.Candidates.Count==0&&!r.Selected),"Missing apps were marked ready");
        var primary=new DisplayDescriptor{Id="other-monitor",Name="Primary",Primary=true,Work=new SixSlotSnap.RECT{L=-2560,R=0,B=1400}};
        plan=WorkspacePlanner.Build(saved,windows,new[]{primary},layouts,null);Check(plan.Rows.All(r=>r.Target==null&&r.Issue!=null),"Disconnected display silently fell back");
        saved.MatchTitles=true;saved.Displays[0].Windows[0].WindowTitle="One";saved.Displays[0].Windows[1].WindowTitle="Two";plan=WorkspacePlanner.Build(saved,windows,new[]{primary},layouts,primary.Id);Check(plan.SelectedRows().Count==2&&plan.Rows.All(r=>r.Target==primary),"Explicit display fallback failed");
        plan=WorkspacePlanner.Build(saved,windows,new[]{display},new LayoutProfile[0],null);Check(plan.Rows.All(r=>r.Issue!=null&&!r.Selected),"Removed saved layout was silently replaced");
        var other=profile.Clone();other.Columns[0].Rows.Add(1);other.Normalize();plan=WorkspacePlanner.Build(saved,windows,new[]{display},new[]{other},null);Check(plan.Rows.All(r=>r.Issue!=null),"Changed saved grid was accepted");
        var stale=new LiveWorkspaceWindow{Handle=one.Handle,ProcessId=one.ProcessId,ProcessStarted=999,ApplicationPath=one.ApplicationPath,WindowClass=one.WindowClass};Check(!WorkspaceApps.SameInstance(one,stale),"Reused process/window identity accepted");
        Check(!WorkspaceApps.ValidPath(@"\\server\app.exe")&&!WorkspaceApps.ValidPath(@"relative.exe")&&!WorkspaceApps.ValidPath(@"C:\app.cmd")&&!WorkspaceApps.ValidPath("C:\\app.exe\n"),"Unsafe relaunch identity accepted");
        saved.MatchTitles=false;string file=Path.Combine(root,"workspaces.xml");var library=new WorkspaceLibrary{Workspaces={saved}};WorkspaceStore.Save(file,library);string xml=File.ReadAllText(file);
        Check(!xml.Contains("<WindowTitle>")&&!xml.Contains("<Handle>")&&!xml.Contains("<ProcessId>"),"Workspace persisted titles without consent or transient handles");
        Check(WorkspaceStore.Load(file).Workspaces.Count==1,"Workspace round trip failed");WorkspaceStore.Save(file,library);Check(File.Exists(file+".bak"),"Workspace replacement lost backup");
        bool failed=false;using(var locked=File.Open(file,FileMode.Open,FileAccess.Read,FileShare.None)){try{WorkspaceStore.Save(file,library);}catch(IOException){failed=true;}}Check(failed&&File.ReadAllText(file)==xml,"Failed workspace save replaced the file");
        File.WriteAllText(file,"broken");bool rejected=false;try{WorkspaceStore.Load(file);}catch(Exception){rejected=true;}Check(rejected&&File.ReadAllText(file)=="broken","Corrupt workspace file was not preserved");
        foreach(string bad in new[]{xml.Replace("<Version>1</Version>","<Version>999</Version>"),"<!DOCTYPE root [<!ENTITY x SYSTEM 'file:///never-read'>]><TileStudioWorkspaces>&x;</TileStudioWorkspaces>",new string(' ',WorkspaceStore.MaximumBytes+1),string.Concat(Enumerable.Repeat("<nested>",40))+string.Concat(Enumerable.Repeat("</nested>",40))}) {File.WriteAllText(file,bad);bool badRejected=false;try{WorkspaceStore.Load(file);}catch(Exception){badRejected=true;}Check(badRejected&&File.ReadAllText(file)==bad,"Unsafe/unsupported workspace XML was accepted or changed");}
        var browser=new LiveWorkspaceWindow{Handle=new IntPtr(103),ProcessId=11,ProcessStarted=200,ApplicationPath=@"C:\Apps\Browser.exe",WindowClass="Browser",Title="Browser"};
        var collision=new SavedWorkspace{Name="Fallback collision",MatchTitles=true,Displays={new SavedWorkspaceDisplay{MonitorId="gone-one",Layout=profile.Clone(),Windows={new SavedPlacement{ApplicationPath=exe,WindowClass="Editor",WindowTitle="One",Slot=0}}},new SavedWorkspaceDisplay{MonitorId="gone-two",Layout=profile.Clone(),Windows={new SavedPlacement{ApplicationPath=browser.ApplicationPath,WindowClass="Browser",WindowTitle="Browser",Slot=1}}}}};
        var colliding=WorkspacePlanner.Build(collision,new[]{one,browser},new[]{primary},layouts,primary.Id);Reject(()=>colliding.SelectedRows(),"Two saved displays were silently merged into a fallback screen");
        string app=Path.Combine(root,"startup-app.exe"),script=Path.Combine(root,"Start hosted.ps1");File.WriteAllText(app,"test fixture, never launched");File.WriteAllText(script,"# fixture, never launched");
        var spec=new StartupLaunchSpec{Executable=app,DataDirectory=root};var memory=new FakeStartup();var startup=new StartupRegistration(spec,memory);
        Check(startup.Current==null&&memory.Writes==0&&startup.Status.Contains("off"),"Startup was enabled during inspection");startup.Enable(false);Check(memory.Value==spec.Command&&memory.Writes==1,"Explicit startup enable failed");startup.Disable();Check(memory.Value==null&&memory.Removals==1,"Startup removal failed");
        memory.Value="unrelated startup";RejectInvalid(()=>startup.Enable(true),"Unrelated startup was overwritten");RejectInvalid(startup.Disable,"Unrelated startup was removed");Check(memory.Value=="unrelated startup","Startup conflict changed the entry");
        memory.Value="old installation --startup-owner WindowsTileStudio";RejectInvalid(()=>startup.Enable(false),"Older startup replaced without explicit approval");startup.Enable(true);Check(memory.Value==spec.Command,"Explicit owned startup update failed");
        var hosted=new StartupLaunchSpec{Executable=app,SourceScript=script,DataDirectory=root};Check(hosted.Command.Contains("-File \"")&&hosted.Command.Contains("-RunInProcess")&&hosted.Command.EndsWith("-StartupOwner WindowsTileStudio"),"Hosted startup command lost source/data/owner marker");
        var trailing=new StartupLaunchSpec{Executable=app,DataDirectory=root+"\\"};Check(trailing.Command.Contains(root+"\\\\\""),"Trailing data path was not safely quoted");
        var missing=new StartupRegistration(new StartupLaunchSpec{Executable=app,DataDirectory=root,SourceScript=Path.Combine(root,"missing.ps1")},memory);RejectInvalid(()=>missing.Enable(true),"Missing hosted source was registered");
    }
    static void RejectInvalid(Action action,string reason){try{action();}catch(InvalidOperationException){checks++;return;}throw new Exception(reason);}
    static int Main() {
        try {
            foreach(var p in AppSettings.Defaults().Profiles) { Exercise(p,0,0); Exercise(p,-5120,40); }
            for(int c=1;c<=4;c++) for(int r=1;r<=4;r++) Exercise(LayoutProfile.Create("test","Test",c,r),0,0);
            var ragged=LayoutProfile.Create("ragged","Mixed column rows",3,2); ragged.Columns[1].Rows.Add(1); ragged.Columns[2].Rows.RemoveAt(1); Exercise(ragged,-5120,-2160);
            Config(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-data"));
            HeightPair();
            MinimumConstraints();
            Monitors(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-data"));
            VisualDrafts();Portable(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-data"));
            WorkspacesAndStartup(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-data"));
            Console.WriteLine("PASS: layout geometry, independent row dividers, mixed row counts, settings round-trip/recovery, duplicate hotkeys, and legacy migration ("+checks+" checks). No desktop windows moved."); return 0;
        } catch(Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
