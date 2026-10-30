using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class NonactivatingForm : Form {
    protected override bool ShowWithoutActivation { get { return true; } }
    protected override CreateParams CreateParams { get { var p=base.CreateParams; p.ExStyle|=0x08000080; return p; } }
    protected override void WndProc(ref Message m) { if(m.Msg==0x21) { m.Result=new IntPtr(3); return; } base.WndProc(ref m); }
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr window,int command);
    public void ShowUnfocused() { Show(); ShowWindow(Handle,4); }
}

public class LayoutCard : UserControl {
    public Action<string,int> ZoneSelected;
    readonly LayoutProfile profile;
    readonly double scale;
    readonly List<Button> zones=new List<Button>();
    UiPalette palette=UiPalette.Current;
    int highlighted=-1;
    public void ApplyPalette(UiPalette value){palette=value;BackColor=value.Background;ForeColor=value.Text;foreach(Control child in Controls)child.ForeColor=value.Text;Highlight(highlighted);}
    protected override void OnSystemColorsChanged(EventArgs e){base.OnSystemColorsChanged(e);if(zones!=null)ApplyPalette(UiPalette.Current);}
    public void Highlight(int slot) {
        highlighted=slot;
        for(int i=0;i<zones.Count;i++) {
            zones[i].BackColor=i==slot?palette.Selected:palette.Panel;
            ((StudioButton)zones[i]).Emphasized=i==slot;zones[i].Invalidate();
            zones[i].ForeColor=i==slot?palette.SelectedText:palette.Text;
            zones[i].AccessibleDescription=i==slot?"Selected zone":"Zone";
            zones[i].FlatAppearance.BorderColor=i==slot?palette.Text:palette.Border;
            zones[i].FlatAppearance.BorderSize=i==slot?2:1;
        }
    }
    public LayoutCard(LayoutProfile source,bool active,double dpiScale) {
        profile=source.Clone(); scale=dpiScale;
        Width=S(238); Height=S(148); Margin=new Padding(S(7));
        BackColor=active?Color.FromArgb(43,55,71):Color.FromArgb(35,39,47);
        AccessibleName=profile.Name+" layout";
        var title=new Label{Text=profile.Name,AutoEllipsis=true,ForeColor=Color.White,Bounds=new Rectangle(S(10),S(8),Width-S(20),S(23))}; Controls.Add(title);
        var model=new LayoutModel(profile); model.Work=new SixSlotSnap.RECT{L=S(10),T=S(39),R=Width-S(10),B=Height-S(12)};
        for(int i=0;i<profile.SlotCount;i++) {
            int slot=i; var r=model.RectFor(i);
            int padX=Math.Min(S(2),Math.Max(0,(r.R-r.L-1)/3)),padY=Math.Min(S(2),Math.Max(0,(r.B-r.T-1)/3));
            var button=new StudioButton{Text=r.B-r.T>=S(16)?(i+1).ToString():"",AccessibleName=profile.Name+" zone "+(i+1),TabStop=false,FlatStyle=FlatStyle.Flat,ForeColor=Color.White,BackColor=Color.FromArgb(63,71,84),Bounds=new Rectangle(r.L+padX,r.T+padY,Math.Max(1,r.R-r.L-2*padX),Math.Max(1,r.B-r.T-2*padY))};
            button.FlatAppearance.BorderColor=Color.FromArgb(91,107,129); button.FlatAppearance.MouseOverBackColor=Color.FromArgb(44,116,181);
            button.Click+=delegate { if(ZoneSelected!=null) ZoneSelected(profile.Id,slot); };
            Controls.Add(button);
            zones.Add(button);
        }
        ApplyPalette(UiPalette.Current);
    }
    int S(int n) { return (int)Math.Round(n*scale); }
}

public class SnapFlyout : NonactivatingForm {
    public Action<string,int> ZoneSelected;
    public Action ManageRequested;
    public Action DismissRequested;
    readonly List<LayoutCard> layoutCards=new List<LayoutCard>();
    readonly List<LayoutProfile> profiles=new List<LayoutProfile>();
    readonly FlowLayoutPanel cardPanel;
    readonly Label keyboardHint;
    int selectedCard,selectedSlot;
    public void KeyboardAvailable(bool available) { keyboardHint.Text=available?"Arrows: zone · Tab: layout · Enter: place":"Keyboard navigation unavailable · use mouse"; }
    void Highlight() { for(int i=0;i<layoutCards.Count;i++) layoutCards[i].Highlight(i==selectedCard?selectedSlot:-1); cardPanel.ScrollControlIntoView(layoutCards[selectedCard]);AccessibleDescription="Selected "+profiles[selectedCard].Name+", zone "+(selectedSlot+1)+". Arrows select a zone; Tab changes layout; Enter places; Escape closes.";if(IsHandleCreated)AccessibilityNotifyClients(AccessibleEvents.DescriptionChange,-1); }
    public void ApplyPalette(UiPalette value){UiPalette.Apply(this,value);}
    protected override void OnSystemColorsChanged(EventArgs e){base.OnSystemColorsChanged(e);if(layoutCards!=null)ApplyPalette(UiPalette.Current);}
    public void Navigate(Keys key,bool reverse) {
        if(key==Keys.Tab) { selectedCard=(selectedCard+(reverse?profiles.Count-1:1))%profiles.Count; selectedSlot=0; }
        else if(key==Keys.Enter) { if(ZoneSelected!=null) ZoneSelected(profiles[selectedCard].Id,selectedSlot); return; }
        else {
            var model=new LayoutModel(profiles[selectedCard]); model.Work=new SixSlotSnap.RECT{R=10000,B=10000};
            var start=model.RectFor(selectedSlot); double x=(start.L+start.R)/2.0,y=(start.T+start.B)/2.0,best=double.MaxValue;
            int candidate=selectedSlot;
            for(int i=0;i<profiles[selectedCard].SlotCount;i++) {
                var r=model.RectFor(i); double dx=(r.L+r.R)/2.0-x,dy=(r.T+r.B)/2.0-y;
                double forward=key==Keys.Left?-dx:key==Keys.Right?dx:key==Keys.Up?-dy:dy;
                double sideways=key==Keys.Left||key==Keys.Right?Math.Abs(dy):Math.Abs(dx);
                if(forward<=0) continue; double score=forward+sideways*4;
                if(score<best) { best=score; candidate=i; }
            }
            selectedSlot=candidate;
        }
        Highlight();
    }
    public SnapFlyout(AppSettings settings,string target,double scale,int availableWidth,int availableHeight) {
        Text="Windows Tile Studio — choose a layout";
        FormBorderStyle=FormBorderStyle.None; ShowInTaskbar=false; TopMost=true; AutoScaleMode=AutoScaleMode.None;
        BackColor=Color.FromArgb(24,28,35); ForeColor=Color.White; Font=new Font("Segoe UI",9);
        AccessibleName="Windows Tile Studio layout flyout";
        Func<int,int> s=n=>(int)Math.Round(n*scale);
        int count=settings.Profiles.Count,columns=Math.Max(1,Math.Min(3,(availableWidth-s(32))/s(252)));
        Width=Math.Min(availableWidth-s(16),s(columns*252+40));
        Height=Math.Min(availableHeight-s(32),s(146+Math.Min(3,(int)Math.Ceiling(count/(double)columns))*162));
        var header=new Label{Text="Choose a layout and zone",Font=new Font("Segoe UI",12,FontStyle.Bold),Bounds=new Rectangle(s(18),s(12),Width-s(70),s(28))}; Controls.Add(header);
        var close=new Button{Text="×",AccessibleName="Close layout flyout",FlatStyle=FlatStyle.Flat,Bounds=new Rectangle(Width-s(46),s(10),s(30),s(28)),ForeColor=Color.White,BackColor=BackColor,TabStop=false}; close.FlatAppearance.BorderSize=0; close.Click+=delegate { if(DismissRequested!=null) DismissRequested(); }; Controls.Add(close);
        Controls.Add(new Label{Text="Place "+target+" · "+settings.FlyoutShortcut+" opens this panel",ForeColor=Color.FromArgb(170,184,204),AutoEllipsis=true,Bounds=new Rectangle(s(18),s(43),Width-s(36),s(22))});
        var cards=new FlowLayoutPanel{Bounds=new Rectangle(s(7),s(71),Width-s(14),Height-s(119)),AutoScroll=true,WrapContents=true,BackColor=BackColor};
        foreach(var profile in settings.Profiles) {
            var card=new LayoutCard(profile,profile.Id==settings.ActiveProfileId,scale);
            card.ZoneSelected=delegate(string id,int slot) { if(ZoneSelected!=null) ZoneSelected(id,slot); }; cards.Controls.Add(card);
            profiles.Add(profile.Clone()); layoutCards.Add(card);
        }
        cardPanel=cards;
        selectedCard=Math.Max(0,profiles.FindIndex(p=>p.Id==settings.ActiveProfileId));
        Controls.Add(cards);
        keyboardHint=new Label{ForeColor=Color.FromArgb(175,198,222),AutoEllipsis=true,Bounds=new Rectangle(s(18),s(64),Width-s(36),s(22))};
        cards.Top=s(91); cards.Height=Height-s(139); Controls.Add(keyboardHint); KeyboardAvailable(false); Highlight();
        var manage=new Button{Text="Manage layouts & hotkeys",UseMnemonic=false,AccessibleName="Manage layouts and hotkeys",FlatStyle=FlatStyle.Flat,ForeColor=Color.White,BackColor=Color.FromArgb(43,52,66),Bounds=new Rectangle(s(17),Height-s(40),s(214),s(28)),TabStop=false};
        manage.FlatAppearance.BorderColor=Color.FromArgb(80,96,119); manage.Click+=delegate { if(ManageRequested!=null) ManageRequested(); }; Controls.Add(manage);
        Controls.Add(new Label{Text="Esc or click outside to close",ForeColor=Color.FromArgb(160,174,194),TextAlign=ContentAlignment.MiddleRight,Bounds=new Rectangle(s(240),Height-s(39),Width-s(257),s(26))});
        ApplyPalette(UiPalette.Current); UiTheme.Attach(this,null);
    }
}

public class LauncherTab : NonactivatingForm {
    protected override void OnSystemColorsChanged(EventArgs e){base.OnSystemColorsChanged(e);UiPalette.Apply(this,UiPalette.Current);}
    public Action HideRequested;
    Button closeButton;
    void HideToTray() { Hide();if(HideRequested!=null)HideRequested(); }
    protected override void OnFormClosing(FormClosingEventArgs e) {
        if(e.CloseReason==CloseReason.UserClosing) {e.Cancel=true;HideToTray();return;}
        base.OnFormClosing(e);
    }
    // Labels/native paint callbacks can retain a previous Font after a change.
    // Keep each DPI's font alive until all child controls have been disposed.
    readonly Dictionary<int,Font> scaleFonts=new Dictionary<int,Font>();
    public static Size SizeForScale(double scale) { return new Size(Math.Max(1,(int)Math.Round(270*scale)),Math.Max(1,(int)Math.Round(27*scale))); }
    void SetScale(double scale) {
        int dpi=Math.Max(48,Math.Min(768,(int)Math.Round(96*scale)));scale=dpi/96.0;
        Size=SizeForScale(scale);Font next;
        if(!scaleFonts.TryGetValue(dpi,out next)) {next=new Font("Segoe UI",(float)(12*scale),FontStyle.Regular,GraphicsUnit.Pixel);scaleFonts.Add(dpi,next);}
        Font=next;foreach(Control child in Controls) child.Font=next;
        if(closeButton!=null)closeButton.Width=(int)Math.Round(27*scale);
    }
    protected override void OnHandleCreated(EventArgs e) {
        base.OnHandleCreated(e); uint dpi=NativeMethods.GetDpiForWindow(Handle); if(dpi>0) SetScale(dpi/96.0);
        var work=DisplayInventory.ForWindow(Handle).Work;
        Location=ClampPosition(Location,Size,Rectangle.FromLTRB(work.L,work.T,work.R,work.B));
    }
    protected override void WndProc(ref Message m) {
        if(m.Msg==0x10 || (m.Msg==0x112 && (m.WParam.ToInt64()&0xfff0)==0xf020)) {HideToTray();m.Result=IntPtr.Zero;return;}
        if(m.Msg==0x2e0) {
            var suggested=(SixSlotSnap.RECT)System.Runtime.InteropServices.Marshal.PtrToStructure(m.LParam,typeof(SixSlotSnap.RECT));
            SetScale((m.WParam.ToInt64()&0xffff)/96.0);
            var work=DisplayInventory.ForPoint(suggested.L,suggested.T).Work;
            Location=ClampPosition(new Point(suggested.L,suggested.T),Size,Rectangle.FromLTRB(work.L,work.T,work.R,work.B)); m.Result=IntPtr.Zero; return;
        }
        base.WndProc(ref m);
    }
    protected override void Dispose(bool disposing) {
        base.Dispose(disposing);
        if(disposing) {foreach(var font in scaleFonts.Values) font.Dispose();scaleFonts.Clear();}
    }
    public Action OpenRequested;
    public Action<Point> PositionChanged;
    Point pressPoint,startLocation;
    bool pressed,moved;
    public static Point ClampPosition(Point point,Size size,Rectangle work) {
        return new Point(Math.Max(work.Left,Math.Min(point.X,work.Right-size.Width)),Math.Max(work.Top,Math.Min(point.Y,work.Bottom-size.Height)));
    }
    public LauncherTab(string shortcut,double scale,bool positionLocked=false) {
        Text="Windows Tile Studio — layout button";
        StartPosition=FormStartPosition.Manual;
        FormBorderStyle=FormBorderStyle.None; ShowInTaskbar=false; TopMost=true; AutoScaleMode=AutoScaleMode.None;
        SetScale(scale); BackColor=Color.FromArgb(33,39,49); ForeColor=Color.FromArgb(210,222,239); AccessibleName="Open custom Snap layouts";
        var label=new Label{Dock=DockStyle.Fill,Text="Layouts  ·  "+shortcut,TextAlign=ContentAlignment.MiddleCenter,Cursor=positionLocked?Cursors.Hand:Cursors.SizeAll,AccessibleName=positionLocked?"Open custom Snap layouts; position locked":"Open custom Snap layouts; drag to move"};
        label.MouseDown+=delegate(object sender,MouseEventArgs e) {
            if(e.Button!=MouseButtons.Left) return;
            pressed=true; moved=false; pressPoint=Cursor.Position; startLocation=Location; label.Capture=true;
        };
        label.MouseMove+=delegate {
            if(!pressed) return; Point current=Cursor.Position;
            if(!moved && Math.Abs(current.X-pressPoint.X)<SystemInformation.DragSize.Width/2 && Math.Abs(current.Y-pressPoint.Y)<SystemInformation.DragSize.Height/2) return;
            moved=true;
            if(positionLocked) return;
            var work=DisplayInventory.ForPoint(current.X,current.Y).Work;
            Location=ClampPosition(new Point(startLocation.X+current.X-pressPoint.X,startLocation.Y+current.Y-pressPoint.Y),Size,Rectangle.FromLTRB(work.L,work.T,work.R,work.B));
        };
        label.MouseUp+=delegate(object sender,MouseEventArgs e) {
            if(e.Button!=MouseButtons.Left || !pressed) return;
            pressed=false; label.Capture=false;
            if(moved) { if(!positionLocked && PositionChanged!=null) PositionChanged(Location); }
            else if(OpenRequested!=null) OpenRequested();
        };
        label.MouseCaptureChanged+=delegate { if(pressed && !label.Capture) { pressed=false; if(!positionLocked && moved && PositionChanged!=null) PositionChanged(Location); } };
        Controls.Add(label);
        closeButton=new Button{Text="×",Dock=DockStyle.Right,Width=Height,FlatStyle=FlatStyle.Flat,TabStop=false,Cursor=Cursors.Hand,ForeColor=ForeColor,BackColor=BackColor,AccessibleName="Hide Layouts bar to tray"};
        closeButton.FlatAppearance.BorderSize=0;closeButton.Click+=delegate{HideToTray();};Controls.Add(closeButton);label.BringToFront();
        UiPalette.Apply(this,UiPalette.Current); UiTheme.Attach(this,null);
    }
}
