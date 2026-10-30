using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

// A draft gesture moves one boundary and preserves its adjacent pair's total.
public class VisualLayoutSession {
    public LayoutProfile Profile {get;private set;}
    readonly List<LayoutProfile> undo=new List<LayoutProfile>(),redo=new List<LayoutProfile>();
    LayoutProfile gesture;
    public bool CanUndo {get{return undo.Count>0;}}
    public bool CanRedo {get{return redo.Count>0;}}
    public VisualLayoutSession(LayoutProfile profile) {Profile=profile.Clone();Profile.Validate();}
    public void Begin() {if(gesture==null)gesture=Profile.Clone();}
    public bool Move(bool columns,int column,int boundary,double position) {
        if(double.IsNaN(position)||double.IsInfinity(position))return false;
        if(!columns && (column<0||column>=Profile.Columns.Count))return false;
        var parts=columns?Profile.Columns.Select(c=>c.Width).ToList():new List<double>(Profile.Columns[column].Rows);
        if(boundary<1||boundary>=parts.Count)return false;
        Begin();double start=parts.Take(boundary-1).Sum(),end=start+parts[boundary-1]+parts[boundary];
        double split=Math.Max(start+0.04,Math.Min(end-0.04,position));
        if(Math.Abs(parts[boundary-1]-(split-start))<1e-9)return false;
        parts[boundary-1]=split-start;parts[boundary]=end-split;
        if(columns)for(int c=0;c<parts.Count;c++)Profile.Columns[c].Width=parts[c];else Profile.Columns[column].Rows=parts;
        return true;
    }
    public void End(bool cancel) {
        if(gesture==null)return;
        if(cancel)Profile=gesture;else if(!gesture.GeometryMatches(Profile)) {undo.Add(gesture);if(undo.Count>50)undo.RemoveAt(0);redo.Clear();}
        gesture=null;
    }
    public void Undo() {End(true);if(!CanUndo)return;redo.Add(Profile.Clone());Profile=undo[undo.Count-1];undo.RemoveAt(undo.Count-1);}
    public void Redo() {End(true);if(!CanRedo)return;undo.Add(Profile.Clone());Profile=redo[redo.Count-1];redo.RemoveAt(redo.Count-1);}
}

public class VisualLayoutCanvas : Control {
    public readonly VisualLayoutSession Session;
    public Action Changed;
    public Action GestureEnded;
    bool dragging,vertical;
    int column,boundary;
    int selectedDivider;
    UiPalette palette=UiPalette.Current;
    class Divider {public bool Vertical;public int Column,Boundary;}
    List<Divider> Dividers {
        get {var list=new List<Divider>();for(int c=1;c<Session.Profile.Columns.Count;c++)list.Add(new Divider{Vertical=true,Column=-1,Boundary=c});for(int c=0;c<Session.Profile.Columns.Count;c++)for(int r=1;r<Session.Profile.Columns[c].Rows.Count;r++)list.Add(new Divider{Column=c,Boundary=r});return list;}
    }
    public string SelectionText {
        get {var list=Dividers;if(list.Count==0)return "One zone; no internal dividers. Use numeric settings to change the grid.";var d=list[selectedDivider%list.Count];double position=d.Vertical?Session.Profile.Columns.Take(d.Boundary).Sum(c=>c.Width):Session.Profile.Columns[d.Column].Rows.Take(d.Boundary).Sum();return (d.Vertical?"Column divider "+d.Boundary:"Column "+(d.Column+1)+", row divider "+d.Boundary)+", "+(position*100).ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+" percent.";}
    }
    public void ApplyPalette(UiPalette value){palette=value;BackColor=value.Background;ForeColor=value.Text;Invalidate();}
    protected override void OnSystemColorsChanged(EventArgs e){base.OnSystemColorsChanged(e);ApplyPalette(UiPalette.Current);}
    protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);Notify();}
    protected override void OnLostFocus(EventArgs e){base.OnLostFocus(e);Invalidate();}
    class CanvasAccessibility : ControlAccessibleObject {
        readonly VisualLayoutCanvas canvas;
        public CanvasAccessibility(VisualLayoutCanvas owner):base(owner){canvas=owner;}
        public override AccessibleRole Role {get{return AccessibleRole.Slider;}}
        public override string Value {get{return canvas.SelectionText;}set{}}
    }
    protected override AccessibleObject CreateAccessibilityInstance(){return new CanvasAccessibility(this);}
    Rectangle Area {get{return new Rectangle(18,18,Math.Max(1,Width-36),Math.Max(1,Height-36));}}
    public VisualLayoutCanvas(LayoutProfile profile) {
        Session=new VisualLayoutSession(profile);DoubleBuffered=true;BackColor=Color.FromArgb(24,28,35);ForeColor=Color.White;TabStop=true;
        AccessibleName="Visual layout dividers";AccessibleDescription="Page Up and Page Down select a divider. Arrow keys adjust by one percent; Shift adjusts by one tenth. Control Z undoes, Control Y redoes. Tab leaves the canvas; Escape cancels a drag.";
        ApplyPalette(UiPalette.Current);
        SetStyle(ControlStyles.ResizeRedraw,true);
    }
    bool Hit(Point point,out bool isVertical,out int col,out int edge) {
        var area=Area;isVertical=false;col=-1;edge=-1;
        if(!area.Contains(point))return false;double x=area.Left;
        for(int c=0;c<Session.Profile.Columns.Count-1;c++) {x+=Session.Profile.Columns[c].Width*area.Width;if(Math.Abs(point.X-x)<=6){isVertical=true;edge=c+1;return true;}}
        x=area.Left;
        for(int c=0;c<Session.Profile.Columns.Count;c++) {
            double right=x+Session.Profile.Columns[c].Width*area.Width,y=area.Top;
            if(point.X>=x && point.X<right)for(int r=0;r<Session.Profile.Columns[c].Rows.Count-1;r++) {y+=Session.Profile.Columns[c].Rows[r]*area.Height;if(Math.Abs(point.Y-y)<=6){col=c;edge=r+1;return true;}}
            x=right;
        }
        return false;
    }
    void Notify() {Invalidate();if(IsHandleCreated)AccessibilityNotifyClients(AccessibleEvents.ValueChange,-1);if(Changed!=null)Changed();}
    protected override void OnMouseDown(MouseEventArgs e) {
        base.OnMouseDown(e);if(e.Button!=MouseButtons.Left||!Hit(e.Location,out vertical,out column,out boundary))return;
        selectedDivider=Dividers.FindIndex(d=>d.Vertical==vertical&&d.Column==column&&d.Boundary==boundary);
        Focus();Session.Begin();dragging=true;Capture=true;
    }
    protected override void OnMouseMove(MouseEventArgs e) {
        base.OnMouseMove(e);
        if(dragging) {var a=Area;if(Session.Move(vertical,column,boundary,vertical?(e.X-a.Left)/(double)a.Width:(e.Y-a.Top)/(double)a.Height))Notify();}
        else {bool v;int c,b;Cursor=Hit(e.Location,out v,out c,out b)?(v?Cursors.VSplit:Cursors.HSplit):Cursors.Default;}
    }
    void End(bool cancel) {if(!dragging)return;dragging=false;Session.End(cancel);Capture=false;Notify();if(GestureEnded!=null)GestureEnded();}
    protected override void OnMouseUp(MouseEventArgs e) {base.OnMouseUp(e);if(e.Button==MouseButtons.Left)End(false);}
    protected override void OnMouseCaptureChanged(EventArgs e) {base.OnMouseCaptureChanged(e);if(dragging&&!Capture)End(false);}
    protected override bool ProcessCmdKey(ref Message message,Keys key) {
        if(key==Keys.Escape&&dragging){End(true);return true;}
        if(!dragging&&(key==(Keys.Control|Keys.Z)||key==(Keys.Control|Keys.Y))){if(key==(Keys.Control|Keys.Z))Session.Undo();else Session.Redo();Notify();if(GestureEnded!=null)GestureEnded();return true;}
        var list=Dividers;if(!dragging&&list.Count>0) {
            if(key==Keys.PageDown||key==Keys.PageUp){selectedDivider=(selectedDivider+(key==Keys.PageDown?1:list.Count-1))%list.Count;Notify();return true;}
            var code=key&Keys.KeyCode;bool modifiers=(key&Keys.Modifiers)==Keys.None||(key&Keys.Modifiers)==Keys.Shift;
            if(modifiers&&(code==Keys.Left||code==Keys.Right||code==Keys.Up||code==Keys.Down)) {
                var d=list[selectedDivider%list.Count];bool appropriate=d.Vertical?(code==Keys.Left||code==Keys.Right):(code==Keys.Up||code==Keys.Down);
                if(appropriate){double position=d.Vertical?Session.Profile.Columns.Take(d.Boundary).Sum(c=>c.Width):Session.Profile.Columns[d.Column].Rows.Take(d.Boundary).Sum();double step=(key&Keys.Shift)!=0?0.001:0.01;Session.Begin();Session.Move(d.Vertical,d.Column,d.Boundary,position+((code==Keys.Left||code==Keys.Up)?-step:step));Session.End(false);Notify();if(GestureEnded!=null)GestureEnded();}return true;
            }
        }
        return base.ProcessCmdKey(ref message,key);
    }
    protected override void Dispose(bool disposing) {if(disposing){Changed=null;GestureEnded=null;dragging=false;Session.End(true);}base.Dispose(disposing);}
    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e);if(Width<40||Height<40)return;var a=Area;
        var model=new LayoutModel(Session.Profile){Work=new SixSlotSnap.RECT{L=a.Left,T=a.Top,R=a.Right,B=a.Bottom}};
        for(int i=0;i<Session.Profile.SlotCount;i++) {
            var r=model.RectFor(i);var box=Rectangle.FromLTRB(r.L+3,r.T+3,r.R-3,r.B-3);if(box.Width<1||box.Height<1)continue;
            using(var path=StudioDrawing.Rounded(box,9))using(var brush=new SolidBrush(palette.Panel))using(var pen=new Pen(palette.Border)){e.Graphics.FillPath(brush,path);e.Graphics.DrawPath(pen,path);}
            TextRenderer.DrawText(e.Graphics,(i+1).ToString(),Font,box,ForeColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);
        }
        using(var pen=new Pen(palette.Line,3)) {
            double x=a.Left;
            for(int c=0;c<Session.Profile.Columns.Count;c++) {
                var def=Session.Profile.Columns[c];double right=x+def.Width*a.Width,y=a.Top;
                for(int r=0;r<def.Rows.Count-1;r++){y+=def.Rows[r]*a.Height;e.Graphics.DrawLine(pen,(float)x,(float)y,(float)right,(float)y);}
                if(c<Session.Profile.Columns.Count-1)e.Graphics.DrawLine(pen,(float)right,a.Top,(float)right,a.Bottom);x=right;
            }
        }
        var dividers=Dividers;
        if(dividers.Count>0){var d=dividers[selectedDivider%dividers.Count];using(var pen=new Pen(palette.Line,Focused?5:2)){pen.DashStyle=System.Drawing.Drawing2D.DashStyle.Dash;
            if(d.Vertical){float x=a.Left+(float)(Session.Profile.Columns.Take(d.Boundary).Sum(c=>c.Width)*a.Width);e.Graphics.DrawLine(pen,x,a.Top,x,a.Bottom);}
            else{float x=a.Left+(float)(Session.Profile.Columns.Take(d.Column).Sum(c=>c.Width)*a.Width),right=x+(float)(Session.Profile.Columns[d.Column].Width*a.Width),y=a.Top+(float)(Session.Profile.Columns[d.Column].Rows.Take(d.Boundary).Sum()*a.Height);e.Graphics.DrawLine(pen,x,y,right,y);}}}
        if(Focused)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(3,3,Width-6,Height-6),palette.Text,palette.Background);
    }
}

public class VisualLayoutDialog : Form {
    public readonly VisualLayoutCanvas Canvas;
    public LayoutProfile Result {get;private set;}
    public VisualLayoutDialog(LayoutProfile profile) {
        Text="Edit layout visually";AccessibleName=Text;ClientSize=new Size(760,510);MinimumSize=new Size(650,450);StartPosition=FormStartPosition.CenterParent;
        var hint=new Label{Text="Drag a divider, or focus the canvas: Page Up/Down selects; arrows adjust 1%; Shift adjusts 0.1%.\nCtrl+Z/Y: undo/redo. Tab leaves the canvas. Horizontal changes affect only their adjacent pair.\nThis is a draft; real windows change only after Save changes in settings.",Dock=DockStyle.Top,Height=76,Padding=new Padding(12,10,12,0)};
        Canvas=new VisualLayoutCanvas(profile){Dock=DockStyle.Fill};var footer=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=48,Padding=new Padding(10,6,0,0)};
        var undo=new StudioButton{Text="Undo",Width=90,Enabled=false};var redo=new StudioButton{Text="Redo",Width=90,Enabled=false};var use=new StudioButton{Text="Use proportions",Width=150};var cancel=new StudioButton{Text="Cancel",Width=90,DialogResult=DialogResult.Cancel};
        footer.Controls.AddRange(new Control[]{undo,redo,use,cancel});Controls.Add(Canvas);Controls.Add(footer);Controls.Add(hint);Canvas.BringToFront();
        var status=new Label{Text=Canvas.SelectionText,Dock=DockStyle.Bottom,Height=28,Padding=new Padding(10,4,0,0),AccessibleName="Selected visual divider",AccessibleRole=AccessibleRole.StatusBar};Controls.Add(status);status.BringToFront();
        Canvas.Changed=delegate{undo.Enabled=Canvas.Session.CanUndo;redo.Enabled=Canvas.Session.CanRedo;status.Text=Canvas.SelectionText;};
        undo.Click+=delegate{Canvas.Session.Undo();Canvas.Invalidate();Canvas.Changed();};redo.Click+=delegate{Canvas.Session.Redo();Canvas.Invalidate();Canvas.Changed();};
        use.Click+=delegate{Canvas.Session.End(false);Result=Canvas.Session.Profile.Clone();Result.Validate();DialogResult=DialogResult.OK;Close();};CancelButton=cancel;
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Margin=Padding.Empty,Padding=Padding.Empty};
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,76));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,28));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,48));
        foreach(Control control in new Control[]{hint,Canvas,status,footer}){Controls.Remove(control);control.Dock=DockStyle.Fill;control.Margin=Padding.Empty;}
        layout.Controls.Add(hint,0,0);layout.Controls.Add(Canvas,0,1);layout.Controls.Add(status,0,2);layout.Controls.Add(footer,0,3);Controls.Add(layout);
        UiLayout.Install(this);
    }
}
