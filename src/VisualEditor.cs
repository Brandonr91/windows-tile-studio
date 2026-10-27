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
    bool dragging,vertical;
    int column,boundary;
    Rectangle Area {get{return new Rectangle(18,18,Math.Max(1,Width-36),Math.Max(1,Height-36));}}
    public VisualLayoutCanvas(LayoutProfile profile) {
        Session=new VisualLayoutSession(profile);DoubleBuffered=true;BackColor=Color.FromArgb(24,28,35);ForeColor=Color.White;TabStop=true;
        AccessibleName="Visual layout dividers";AccessibleDescription="Drag a vertical divider for column widths or a horizontal divider for its adjacent pair. Escape cancels a drag.";
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
    void Notify() {Invalidate();if(Changed!=null)Changed();}
    protected override void OnMouseDown(MouseEventArgs e) {
        base.OnMouseDown(e);if(e.Button!=MouseButtons.Left||!Hit(e.Location,out vertical,out column,out boundary))return;
        Focus();Session.Begin();dragging=true;Capture=true;
    }
    protected override void OnMouseMove(MouseEventArgs e) {
        base.OnMouseMove(e);
        if(dragging) {var a=Area;if(Session.Move(vertical,column,boundary,vertical?(e.X-a.Left)/(double)a.Width:(e.Y-a.Top)/(double)a.Height))Notify();}
        else {bool v;int c,b;Cursor=Hit(e.Location,out v,out c,out b)?(v?Cursors.VSplit:Cursors.HSplit):Cursors.Default;}
    }
    void End(bool cancel) {if(!dragging)return;dragging=false;Session.End(cancel);Capture=false;Notify();}
    protected override void OnMouseUp(MouseEventArgs e) {base.OnMouseUp(e);if(e.Button==MouseButtons.Left)End(false);}
    protected override void OnMouseCaptureChanged(EventArgs e) {base.OnMouseCaptureChanged(e);if(dragging&&!Capture)End(false);}
    protected override bool ProcessCmdKey(ref Message message,Keys key) {if(key==Keys.Escape&&dragging){End(true);return true;}return base.ProcessCmdKey(ref message,key);}
    protected override void Dispose(bool disposing) {if(disposing){Changed=null;dragging=false;Session.End(true);}base.Dispose(disposing);}
    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e);if(Width<40||Height<40)return;var a=Area;
        var model=new LayoutModel(Session.Profile){Work=new SixSlotSnap.RECT{L=a.Left,T=a.Top,R=a.Right,B=a.Bottom}};
        for(int i=0;i<Session.Profile.SlotCount;i++) {
            var r=model.RectFor(i);var box=Rectangle.FromLTRB(r.L+3,r.T+3,r.R-3,r.B-3);if(box.Width<1||box.Height<1)continue;
            using(var brush=new SolidBrush(Color.FromArgb(47,66,88)))e.Graphics.FillRectangle(brush,box);
            TextRenderer.DrawText(e.Graphics,(i+1).ToString(),Font,box,ForeColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);
        }
        using(var pen=new Pen(Color.FromArgb(75,201,231),3)) {
            double x=a.Left;
            for(int c=0;c<Session.Profile.Columns.Count;c++) {
                var def=Session.Profile.Columns[c];double right=x+def.Width*a.Width,y=a.Top;
                for(int r=0;r<def.Rows.Count-1;r++){y+=def.Rows[r]*a.Height;e.Graphics.DrawLine(pen,(float)x,(float)y,(float)right,(float)y);}
                if(c<Session.Profile.Columns.Count-1)e.Graphics.DrawLine(pen,(float)right,a.Top,(float)right,a.Bottom);x=right;
            }
        }
    }
}

public class VisualLayoutDialog : Form {
    public readonly VisualLayoutCanvas Canvas;
    public LayoutProfile Result {get;private set;}
    public VisualLayoutDialog(LayoutProfile profile) {
        Text="Edit layout visually";AccessibleName=Text;ClientSize=new Size(760,510);MinimumSize=new Size(650,450);StartPosition=FormStartPosition.CenterParent;
        var hint=new Label{Text="Drag the blue dividers. Horizontal dividers change only their adjacent pair in that column.\nThis is a draft; real windows change only after Save changes in settings.",Dock=DockStyle.Top,Height=54,Padding=new Padding(12,10,12,0)};
        Canvas=new VisualLayoutCanvas(profile){Dock=DockStyle.Fill};var footer=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=48,Padding=new Padding(10,6,0,0)};
        var undo=new Button{Text="Undo",Width=90,Enabled=false};var redo=new Button{Text="Redo",Width=90,Enabled=false};var use=new Button{Text="Use proportions",Width=150};var cancel=new Button{Text="Cancel",Width=90,DialogResult=DialogResult.Cancel};
        footer.Controls.AddRange(new Control[]{undo,redo,use,cancel});Controls.Add(Canvas);Controls.Add(footer);Controls.Add(hint);Canvas.BringToFront();
        Canvas.Changed=delegate{undo.Enabled=Canvas.Session.CanUndo;redo.Enabled=Canvas.Session.CanRedo;};
        undo.Click+=delegate{Canvas.Session.Undo();Canvas.Invalidate();Canvas.Changed();};redo.Click+=delegate{Canvas.Session.Redo();Canvas.Invalidate();Canvas.Changed();};
        use.Click+=delegate{Canvas.Session.End(false);Result=Canvas.Session.Profile.Clone();Result.Validate();DialogResult=DialogResult.OK;Close();};CancelButton=cancel;
    }
}
