using System;
using System.Collections.Generic;
using System.Linq;

public class ColumnDefinition {
    public double Width=1;
    public List<double> Rows=new List<double>();
    public ColumnDefinition Clone() { return new ColumnDefinition{Width=Width,Rows=new List<double>(Rows)}; }
}

public class LayoutProfile {
    public string Id=Guid.NewGuid().ToString("N");
    public string Name="Custom layout";
    public List<ColumnDefinition> Columns=new List<ColumnDefinition>();
    public Shortcut Shortcut=new Shortcut();
    public int SlotCount { get { return Columns.Sum(c=>c.Rows.Count); } }
    public override string ToString() { return Name; }
    public LayoutProfile Clone() { return new LayoutProfile{Id=Id,Name=Name,Columns=Columns.Select(c=>c.Clone()).ToList(),Shortcut=Shortcut.Clone()}; }
    public static LayoutProfile Create(string id,string name,int columns,int rows) {
        var p=new LayoutProfile{Id=id,Name=name};
        for(int c=0;c<columns;c++) p.Columns.Add(new ColumnDefinition{Width=1.0/columns,Rows=Enumerable.Repeat(1.0/rows,rows).ToList()});
        return p;
    }
    public void Normalize() {
        double total=Columns.Sum(c=>c.Width);
        foreach(var c in Columns) { c.Width/=total; double rows=c.Rows.Sum(); for(int r=0;r<c.Rows.Count;r++) c.Rows[r]/=rows; }
    }
    public void Validate() {
        if(string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(Name) || Name.Length>48) throw new ArgumentException("Each layout needs a name of 1–48 characters.");
        if(Columns==null || Columns.Count<1 || Columns.Count>4) throw new ArgumentException("Use between 1 and 4 columns.");
        foreach(var c in Columns) {
            if(c==null || !FinitePositive(c.Width) || c.Rows==null || c.Rows.Count<1 || c.Rows.Count>4 || c.Rows.Any(v=>!FinitePositive(v)))
                throw new ArgumentException("Each column needs a positive width and 1–4 positive row proportions.");
        }
        if(SlotCount>16) throw new ArgumentException("The basic version supports up to 16 zones per layout.");
        Normalize();
        if(Columns.Any(c=>c.Width<0.04-1e-9 || c.Rows.Any(v=>v<0.04-1e-9))) throw new ArgumentException("Every column and row must occupy at least 4% of its dimension.");
    }
    static bool FinitePositive(double d) { return d>0 && !double.IsNaN(d) && !double.IsInfinity(d); }
}

public class LayoutModel {
    public SixSlotSnap.RECT Work;
    public LayoutProfile Profile;
    public LayoutModel(LayoutProfile profile) { Profile=profile; }
    public bool Locate(int slot,out int column,out int row) {
        int index=0,max=Profile.Columns.Max(c=>c.Rows.Count);
        for(int r=0;r<max;r++) for(int c=0;c<Profile.Columns.Count;c++) if(r<Profile.Columns[c].Rows.Count) {
            if(index++==slot) { column=c; row=r; return true; }
        }
        column=row=-1; return false;
    }
    public int IndexOf(int column,int row) {
        for(int i=0;i<Profile.SlotCount;i++) { int c,r; Locate(i,out c,out r); if(c==column && r==row) return i; }
        return -1;
    }
    static int Pixel(int origin,int size,double fraction) { return origin+(int)Math.Round(size*fraction,MidpointRounding.AwayFromZero); }
    public SixSlotSnap.RECT RectFor(int slot) {
        int c,r; if(!Locate(slot,out c,out r)) throw new ArgumentOutOfRangeException("slot");
        var column=Profile.Columns[c];
        double x=Profile.Columns.Take(c).Sum(v=>v.Width),y=column.Rows.Take(r).Sum();
        int w=Work.R-Work.L,h=Work.B-Work.T;
        return new SixSlotSnap.RECT{L=Pixel(Work.L,w,x),R=c==Profile.Columns.Count-1?Work.R:Pixel(Work.L,w,x+column.Width),T=Pixel(Work.T,h,y),B=r==column.Rows.Count-1?Work.B:Pixel(Work.T,h,y+column.Rows[r])};
    }
    static bool Boundary(IList<double> parts,int boundary,double desired,double min) {
        if(boundary<=0 || boundary>=parts.Count) return false;
        double start=0; for(int i=0;i<boundary-1;i++) start+=parts[i];
        double end=start+parts[boundary-1]+parts[boundary];
        min=Math.Max(0.04,Math.Min(min,(end-start)/2));
        double split=Math.Max(start+min,Math.Min(end-min,desired));
        if(Math.Abs(parts[boundary-1]-(split-start))<1e-9) return false;
        parts[boundary-1]=split-start; parts[boundary]=end-split; return true;
    }
    public bool Adjust(int slot,SixSlotSnap.RECT before,SixSlotSnap.RECT after) {
        int c,r,w=Work.R-Work.L,h=Work.B-Work.T;
        if(!Locate(slot,out c,out r) || w<=0 || h<=0) return false;
        bool changed=false; var widths=Profile.Columns.Select(v=>v.Width).ToList();
        if(Math.Abs((after.R-after.L)-(before.R-before.L))>1) {
            if(Math.Abs(after.L-before.L)>1) changed|=Boundary(widths,c,(after.L-Work.L)/(double)w,400.0/w);
            if(Math.Abs(after.R-before.R)>1) changed|=Boundary(widths,c+1,(after.R-Work.L)/(double)w,400.0/w);
        }
        for(int i=0;i<widths.Count;i++) Profile.Columns[i].Width=widths[i];
        var rows=Profile.Columns[c].Rows;
        if(Math.Abs((after.B-after.T)-(before.B-before.T))>1) {
            if(Math.Abs(after.T-before.T)>1) changed|=Boundary(rows,r,(after.T-Work.T)/(double)h,200.0/h);
            if(Math.Abs(after.B-before.B)>1) changed|=Boundary(rows,r+1,(after.B-Work.T)/(double)h,200.0/h);
        }
        return changed;
    }
}
