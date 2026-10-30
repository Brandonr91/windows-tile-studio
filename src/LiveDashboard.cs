using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// Ephemeral UI snapshots. Never serialized or included in portable/report data.
public class LiveTile {
    public int Number;
    public string Application,Status;
}
public partial class SixSlotSnap {
    class RetryPlacement {
        public string Token=Guid.NewGuid().ToString("N"),Display,Profile;
        public IntPtr Window;
        public int Slot;
        public uint Process,Thread;
        public long Started;
    }
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern bool SetProp(IntPtr window,string key,IntPtr value);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern IntPtr GetProp(IntPtr window,string key);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern IntPtr RemoveProp(IntPtr window,string key);
    RetryPlacement pendingRetry;
    static void ClearRetry(RetryPlacement value){if(value!=null&&GetProp(value.Window,"WindowsTileStudio.Retry."+value.Token)==new IntPtr(1))RemoveProp(value.Window,"WindowsTileStudio.Retry."+value.Token);}
    void SetPendingRetry(RetryPlacement value){ClearRetry(pendingRetry);pendingRetry=value;}
    LiveDashboardState dashboardCache;
    RetryPlacement CaptureRetry(IntPtr window,string profile,int slot) {
        if(!ValidTarget(window)||slot<0||slot>=16)return null;
        uint pid;uint thread=NativeMethods.GetWindowThreadProcessId(window,out pid);
        try{using(var p=Process.GetProcessById((int)pid)){var value=new RetryPlacement{Window=window,Process=pid,Thread=thread,Started=p.StartTime.ToUniversalTime().Ticks,Display=DisplayInventory.ForWindow(window).Id,Profile=profile,Slot=slot};return SetProp(window,"WindowsTileStudio.Retry."+value.Token,new IntPtr(1))?value:null;}}catch{return null;}
    }
    bool RetryIsCurrent(RetryPlacement value) {
        if(value==null||!ValidTarget(value.Window)||GetProp(value.Window,"WindowsTileStudio.Retry."+value.Token)!=new IntPtr(1))return false;
        uint pid;uint thread=NativeMethods.GetWindowThreadProcessId(value.Window,out pid);
        if(pid!=value.Process||thread!=value.Thread)return false;
        try{using(var p=Process.GetProcessById((int)pid))return p.StartTime.ToUniversalTime().Ticks==value.Started;}catch{return false;}
    }
    LiveDashboardState ReadDashboard() {
        if(geometryDepth>0)return dashboardCache;
        var result=new LiveDashboardState{ActiveDisplay=workspace.Active.Display.Id,PlacementReady=placement.Ready,Issue=lastPlacementIssue};
        foreach(var session in workspace.Sessions.Values) {
            var display=new LiveDisplay{Id=session.Display.Id,Name=session.Display.Name,Connected=session.Connected,Profile=session.Model.Profile.Clone()};
            for(int i=0;i<display.Profile.SlotCount;i++) {
                IntPtr w=session.Windows[i];string status=!session.Connected?"Disconnected":!NativeMethods.IsWindow(w)?"Empty":NativeMethods.IsIconic(w)?"Minimized":NativeMethods.IsZoomed(w)?"Maximized":NativeMethods.IsHungAppWindow(w)?"Not responding":"Occupied";
                if(w==IntPtr.Zero&&pendingRetry!=null&&pendingRetry.Display==display.Id&&pendingRetry.Profile==display.Profile.Id&&pendingRetry.Slot==i)status="Detached · retry explicitly";
                display.Tiles.Add(new LiveTile{Number=i+1,Status=status,Application=NativeMethods.IsWindow(w)?ProcessName(w):null});
            }
            result.Displays.Add(display);
        }
        if(RetryIsCurrent(pendingRetry))result.RetryToken=pendingRetry.Token;
        else if(pendingRetry!=null)result.Issue="The previous target closed or changed. Select the app and snap it explicitly; no app will be relaunched.";
        dashboardCache=result;return result;
    }
    string RetryDashboard(string token) {
        if(geometryDepth>0)return "A resize is still in progress. Retry after releasing the divider.";
        var value=pendingRetry;if(value==null||value.Token!=token||!RetryIsCurrent(value))return "That retry expired. Select the app and snap it explicitly.";
        MonitorSession session;
        if(!workspace.Sessions.TryGetValue(value.Display,out session)||!session.Connected||session.Model.Profile.Id!=value.Profile||value.Slot>=session.Model.Profile.SlotCount||DisplayInventory.ForWindow(value.Window).Id!=value.Display)return "The layout or display changed. Choose a zone explicitly in the flyout.";
        IntPtr occupant=session.Windows[value.Slot];
        if(occupant!=IntPtr.Zero&&occupant!=value.Window&&NativeMethods.IsWindow(occupant))return "This zone now has another window. Choose a zone explicitly in the flyout.";
        return SnapTo(value.Window,value.Profile,value.Slot)?"Placement succeeded. The window is linked again.":"Placement failed. Review the last placement outcome before retrying.";
    }
}
public class LiveDisplay {
    public string Id,Name;
    public bool Connected;
    public LayoutProfile Profile;
    public List<LiveTile> Tiles=new List<LiveTile>();
    public override string ToString(){return Name+(Connected?"":" (disconnected)");}
}
public class LiveDashboardState {
    public List<LiveDisplay> Displays=new List<LiveDisplay>();
    public string ActiveDisplay,Issue,RetryToken;
    public bool PlacementReady;
}
public class LiveDashboard : UserControl {
    readonly Func<LiveDashboardState> read;
    readonly Func<string,string> retry;
    readonly ComboBox display=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,AccessibleName="Live display"};
    readonly Label summary=new Label{AutoEllipsis=true,AccessibleName="Live window summary",AccessibleRole=AccessibleRole.StatusBar};
    readonly Label issue=new Label{AutoEllipsis=true,AccessibleName="Last placement outcome"};
    readonly Panel tiles=new Panel{AccessibleName="Live slot occupancy"};
    readonly Button retryButton=new StudioButton{Text="Retry last placement",AccessibleName="Retry last placement"};
    readonly Timer timer=new Timer{Interval=1000};
    readonly List<StudioPanel> cards=new List<StudioPanel>();
    LiveDashboardState state;
    LiveDisplay shown;
    bool reading;
    public LiveDashboard(Func<LiveDashboardState> snapshot,Func<string,string> retryPlacement) {
        read=snapshot;retry=retryPlacement;AutoScroll=true;AutoScrollMinSize=new Size(960,650);
        Controls.Add(new Label{Text="Live windows",Font=new Font("Segoe UI",18),AutoSize=true,Location=new Point(24,20)});
        Controls.Add(new Label{Text="Current assignments · updates while this page is open · independent of unsaved layout edits",Bounds=new Rectangle(24,66,920,26)});
        display.Bounds=new Rectangle(24,105,430,32);summary.Bounds=new Rectangle(478,105,466,42);
        tiles.Bounds=new Rectangle(24,163,920,340);issue.Bounds=new Rectangle(24,524,920,62);retryButton.Bounds=new Rectangle(24,600,210,36);
        var refresh=new StudioButton{Text="Refresh status",Bounds=new Rectangle(250,600,170,36),AccessibleName="Refresh live windows"};
        Controls.AddRange(new Control[]{display,summary,tiles,issue,retryButton,refresh});
        display.SelectedIndexChanged+=delegate{if(!reading)Render();};refresh.Click+=delegate{RefreshStatus();};
        retryButton.Click+=delegate{
            // Copy the token displayed to the human before any refresh/IPC can reenter.
            string token=state==null?null:state.RetryToken;
            if(token==null||retry==null)return;retryButton.Enabled=false;
            string result=retry(token);RefreshStatus();if(result!=null)issue.Text=result;
        };
        timer.Tick+=delegate{if(Visible&&!reading)RefreshStatus();};
        VisibleChanged+=delegate{timer.Enabled=Visible&&read!=null;if(Visible)RefreshStatus();};
    }
    public void RefreshStatus() {
        if(reading||IsDisposed||display.DroppedDown)return;reading=true;
        try {
            string selected=(display.SelectedItem as LiveDisplay)==null?null:((LiveDisplay)display.SelectedItem).Id;
            state=read==null?null:read();
            if(state!=null){
                var chosen=state.Displays.FirstOrDefault(d=>d.Id==selected)??state.Displays.FirstOrDefault(d=>d.Id==state.ActiveDisplay)??state.Displays.FirstOrDefault();
                if(!display.Items.Cast<LiveDisplay>().Select(d=>d.Id+":"+d.ToString()).SequenceEqual(state.Displays.Select(d=>d.Id+":"+d.ToString()))){display.Items.Clear();foreach(var item in state.Displays)display.Items.Add(item);}
                display.SelectedItem=display.Items.Cast<LiveDisplay>().FirstOrDefault(d=>chosen!=null&&d.Id==chosen.Id);
            }else display.Items.Clear();
            Render();
        } catch(Exception) {summary.Text="Status unavailable. Refresh to try again.";retryButton.Enabled=false;}
        finally{reading=false;}
    }
    void Render() {
        var selected=display.SelectedItem as LiveDisplay;
        shown=state==null||selected==null?null:state.Displays.FirstOrDefault(d=>d.Id==selected.Id);
        if(shown==null){summary.Text="Live status is available from the running manager.";issue.Text="Open the flyout to snap an app into a zone.";retryButton.Enabled=false;return;}
        summary.Text=shown.Profile.Name+" · "+shown.Tiles.Count(t=>t.Status=="Occupied")+" occupied / "+shown.Tiles.Count+" zones"+(state.PlacementReady?"":" · placement unavailable");
        issue.Text=state.Issue??"No placement error. Empty zones are ready for an explicit snap through the flyout or a zone shortcut.";
        retryButton.Enabled=retry!=null&&state.RetryToken!=null;
        // Keep controls/focus stable on periodic refreshes; rebuild only for grid shape.
        if(cards.Count!=shown.Tiles.Count){foreach(var c in cards)c.Dispose();cards.Clear();foreach(var tile in shown.Tiles){var card=new StudioPanel{Padding=new Padding(8)};card.Controls.Add(new Label{Dock=DockStyle.Fill,Padding=new Padding(16),AutoEllipsis=true});tiles.Controls.Add(card);cards.Add(card);}UiPalette.Apply(tiles,UiTheme.PaletteFor(FindForm()));}
        var model=new LayoutModel(shown.Profile){Work=new SixSlotSnap.RECT{L=0,T=0,R=tiles.Width,B=tiles.Height}};
        for(int i=0;i<cards.Count;i++){var tile=shown.Tiles[i];var r=model.RectFor(i);cards[i].Bounds=new Rectangle(r.L+4,r.T+4,Math.Max(1,r.R-r.L-8),Math.Max(1,r.B-r.T-8));string text="Zone "+tile.Number+"\n\n"+tile.Status+(string.IsNullOrEmpty(tile.Application)?"":"\n"+tile.Application);cards[i].Controls[0].Text=text;cards[i].Controls[0].AccessibleName=text;}
    }
    protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
}
