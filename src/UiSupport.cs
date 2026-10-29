using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Collections.Generic;

// Native page selection is retained behind an accessible, painted navigation rail.
public class NavigationTabs : TabControl {
    public NavigationTabs(bool sidebar) {SizeMode=TabSizeMode.Fixed;ItemSize=new Size(1,1);Appearance=TabAppearance.FlatButtons;}
    public override Rectangle DisplayRectangle {get{return new Rectangle(0,0,Width,Height);}}
    public void AddNavigation(Control container,bool sidebar) {
        var rail=new FlowLayoutPanel{Dock=sidebar?DockStyle.Left:DockStyle.Top,Width=168,Height=54,FlowDirection=sidebar?FlowDirection.TopDown:FlowDirection.LeftToRight,WrapContents=false,AutoScroll=true,Padding=new Padding(8),AccessibleName=sidebar?"Settings navigation":"General navigation"};
        foreach(TabPage page in TabPages) {
            var button=new NavigationButton{Text=page.Text,Width=sidebar?148:137,Height=38,Margin=new Padding(2,2,2,6),AccessibleName="Open "+page.Text,Selected=delegate{return SelectedTab==page;}};
            button.Click+=delegate{SelectedTab=page;};rail.Controls.Add(button);
        }
        SelectedIndexChanged+=delegate{rail.Invalidate(true);};container.Controls.Add(rail);BringToFront();
    }
}
public class NavigationButton : Button {
    public Func<bool> Selected;
    protected override void OnPaint(PaintEventArgs e) {
        var p=UiTheme.PaletteFor(FindForm());bool active=Selected!=null&&Selected();
        e.Graphics.Clear(p.Background);
        var bounds=new Rectangle(0,0,Width-1,Height-1);
        if(active)using(var brush=new SolidBrush(p.Panel))e.Graphics.FillRectangle(brush,bounds);
        if(active)using(var brush=new SolidBrush(p.Line))e.Graphics.FillRectangle(brush,0,9,3,Height-18);
        TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle(14,0,Width-20,Height),Enabled?p.Text:p.Muted,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
        if(Focused&&ShowFocusCues){bounds.Inflate(-4,-4);ControlPaint.DrawFocusRectangle(e.Graphics,bounds,p.Text,p.Background);}
    }
}
public class StudioButton : Button {
    bool hovered;
    protected override void OnMouseEnter(EventArgs e){hovered=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hovered=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnPaint(PaintEventArgs e) {
        var p=UiTheme.PaletteFor(FindForm());bool primary=Text=="Save changes"&&Enabled;
        Color fill=primary?p.Line:hovered&&Enabled?p.Selected:p.Panel;
        Color text=Enabled?p.Text:p.Muted;
        if(primary)text=(0.2126*fill.R+0.7152*fill.G+0.0722*fill.B)>145?Color.Black:Color.White;
        if(SystemInformation.HighContrast&&primary){fill=SystemColors.Highlight;text=SystemColors.HighlightText;}
        e.Graphics.Clear(p.Background);e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using(var path=new System.Drawing.Drawing2D.GraphicsPath()) {
            float w=Width-1,h=Height-1,r=8;
            path.AddArc(0,0,r,r,180,90);path.AddArc(w-r,0,r,r,270,90);path.AddArc(w-r,h-r,r,r,0,90);path.AddArc(0,h-r,r,r,90,90);path.CloseFigure();
            using(var brush=new SolidBrush(fill))e.Graphics.FillPath(brush,path);using(var pen=new Pen(p.Border))e.Graphics.DrawPath(pen,path);
        }
        TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,text,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
        if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,Rectangle.Inflate(ClientRectangle,-4,-4),text,fill);
    }
}

public static class UiTheme {
    public static AppearanceMode Mode=AppearanceMode.System;
    static readonly Dictionary<Form,Func<AppearanceMode>> forms=new Dictionary<Form,Func<AppearanceMode>>();
    static readonly Dictionary<Form,UiPalette> palettes=new Dictionary<Form,UiPalette>();
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
    [DllImport("dwmapi.dll")] static extern int DwmGetColorizationColor(out uint color,out bool opaque);
    public static bool SystemIsDark() {
        try {using(var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))return key!=null&&Convert.ToInt32(key.GetValue("AppsUseLightTheme",1))==0;}
        catch(System.Security.SecurityException){return false;}catch(UnauthorizedAccessException){return false;}catch(FormatException){return false;}catch(InvalidCastException){return false;}
    }
    public static Color Accent() {
        try{uint value;bool opaque;if(DwmGetColorizationColor(out value,out opaque)==0)return Color.FromArgb((int)((value>>16)&255),(int)((value>>8)&255),(int)(value&255));}catch(DllNotFoundException){}catch(EntryPointNotFoundException){}
        return SystemColors.Highlight;
    }
    public static UiPalette Resolve(AppearanceMode mode,bool contrast,bool systemDark,Color accent) {
        if(contrast)return UiPalette.ForContrast(true);
        bool dark=mode==AppearanceMode.Dark||(mode==AppearanceMode.System&&systemDark);
        return dark?new UiPalette{Background=Color.FromArgb(32,32,32),Panel=Color.FromArgb(45,45,45),Text=Color.FromArgb(245,245,245),Muted=Color.FromArgb(200,200,200),Border=Color.FromArgb(76,76,76),Line=accent,Selected=Color.FromArgb(62,62,62),SelectedText=Color.White}:
            new UiPalette{Background=Color.FromArgb(243,243,243),Panel=Color.White,Text=Color.FromArgb(26,26,26),Muted=Color.FromArgb(85,85,85),Border=Color.FromArgb(210,210,210),Line=accent,Selected=Color.FromArgb(224,235,247),SelectedText=Color.FromArgb(20,20,20)};
    }
    static AppearanceMode ModeFor(Form form) {
        Func<AppearanceMode> provider;
        if(form!=null&&forms.TryGetValue(form,out provider)&&provider!=null)return provider();
        var owner=form==null?null:form.Owner as SnapSettingsForm;return owner==null?Mode:owner.Draft.Appearance;
    }
    public static UiPalette PaletteFor(Form form){UiPalette palette;if(form!=null&&palettes.TryGetValue(form,out palette))return palette;return Resolve(ModeFor(form),SystemInformation.HighContrast,SystemIsDark(),Accent());}
    public static void SetMode(AppearanceMode mode){Mode=mode;foreach(var form in forms.Keys.ToArray())Refresh(form);}
    public static void Attach(Form form,Func<AppearanceMode> provider) {
        if(forms.ContainsKey(form)){forms[form]=provider;Refresh(form);return;}
        forms.Add(form,provider);
        UserPreferenceChangedEventHandler changed=delegate{QueueRefresh(form);};
        SystemEvents.UserPreferenceChanged+=changed;
        form.HandleCreated+=delegate{Refresh(form);};form.Shown+=delegate{Refresh(form);};
        form.Disposed+=delegate{SystemEvents.UserPreferenceChanged-=changed;forms.Remove(form);palettes.Remove(form);};
        Refresh(form);
    }
    static void QueueRefresh(Form form) {
        if(form.IsDisposed||!form.IsHandleCreated)return;
        try{form.BeginInvoke((MethodInvoker)delegate{if(!form.IsDisposed)Refresh(form);});}catch(InvalidOperationException){}
    }
    public static void Refresh(Form form) {
        if(form==null||form.IsDisposed)return;
        var palette=Resolve(ModeFor(form),SystemInformation.HighContrast,SystemIsDark(),Accent());palettes[form]=palette;UiPalette.Apply(form,palette);
        if(form.IsHandleCreated)try {
            int dark=!SystemInformation.HighContrast&&palette.Background.GetBrightness()<0.5f?1:0;
            DwmSetWindowAttribute(form.Handle,20,ref dark,4);int corners=SystemInformation.HighContrast?1:2;DwmSetWindowAttribute(form.Handle,33,ref corners,4);
        }catch(DllNotFoundException){}catch(EntryPointNotFoundException){}
        form.Invalidate(true);
    }
}

public class UiPalette {
    public Color Background,Panel,Text,Muted,Line,Selected,SelectedText,Border;
    public static UiPalette Current {get{return UiTheme.Resolve(UiTheme.Mode,SystemInformation.HighContrast,UiTheme.SystemIsDark(),UiTheme.Accent());}}
    public static UiPalette ForContrast(bool contrast) {
        return contrast?new UiPalette{Background=SystemColors.Window,Panel=SystemColors.Control,Text=SystemColors.WindowText,Muted=SystemColors.WindowText,Line=SystemColors.Highlight,Selected=SystemColors.Highlight,SelectedText=SystemColors.HighlightText,Border=SystemColors.WindowText}:
            UiTheme.Resolve(AppearanceMode.Dark,false,false,Color.FromArgb(0,120,212));
    }
    public static void Apply(Control root,UiPalette palette) {
        root.BackColor=palette.Background;root.ForeColor=palette.Text;
        var page=root as TabPage;if(page!=null)page.UseVisualStyleBackColor=false;
        var button=root as Button;if(button!=null){button.UseVisualStyleBackColor=false;button.FlatStyle=FlatStyle.Flat;button.BackColor=palette.Panel;button.FlatAppearance.BorderColor=palette.Border;button.FlatAppearance.MouseOverBackColor=palette.Selected;button.FlatAppearance.MouseDownBackColor=palette.Selected;}
        var text=root as TextBoxBase;if(text!=null){text.BackColor=palette.Panel;text.BorderStyle=BorderStyle.FixedSingle;}
        var list=root as ListBox;if(list!=null){list.BackColor=palette.Panel;list.BorderStyle=BorderStyle.FixedSingle;}
        var combo=root as ComboBox;if(combo!=null){
            combo.FlatStyle=FlatStyle.Flat;combo.BackColor=palette.Panel;
            if(combo.DrawMode==DrawMode.Normal){combo.DrawMode=DrawMode.OwnerDrawFixed;combo.DrawItem+=delegate(object sender,DrawItemEventArgs e){
                var colors=UiTheme.PaletteFor(combo.FindForm());bool selected=(e.State&DrawItemState.Selected)!=0;
                using(var brush=new SolidBrush(selected?colors.Selected:colors.Panel))e.Graphics.FillRectangle(brush,e.Bounds);
                string label=e.Index>=0?combo.GetItemText(combo.Items[e.Index]):combo.Text;
                TextRenderer.DrawText(e.Graphics,label,combo.Font,e.Bounds,selected?colors.SelectedText:colors.Text,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);e.DrawFocusRectangle();
            };}
        }
        var number=root as NumericUpDown;if(number!=null){number.BackColor=palette.Panel;number.BorderStyle=BorderStyle.FixedSingle;}
        var grid=root as DataGridView;if(grid!=null){
            grid.EnableHeadersVisualStyles=false;grid.BackgroundColor=palette.Panel;grid.BorderStyle=BorderStyle.None;grid.GridColor=palette.Border;
            grid.DefaultCellStyle.BackColor=palette.Panel;grid.DefaultCellStyle.ForeColor=palette.Text;grid.DefaultCellStyle.SelectionBackColor=palette.Selected;grid.DefaultCellStyle.SelectionForeColor=palette.SelectedText;
            grid.ColumnHeadersDefaultCellStyle.BackColor=palette.Background;grid.ColumnHeadersDefaultCellStyle.ForeColor=palette.Text;grid.ColumnHeadersDefaultCellStyle.SelectionBackColor=palette.Selected;grid.ColumnHeadersDefaultCellStyle.SelectionForeColor=palette.SelectedText;
            grid.ColumnHeadersBorderStyle=DataGridViewHeaderBorderStyle.Single;grid.CellBorderStyle=DataGridViewCellBorderStyle.SingleHorizontal;
            grid.RowTemplate.Height=32;grid.ColumnHeadersHeight=36;
        }
        foreach(Control child in root.Controls)Apply(child,palette);
        var card=root as LayoutCard;if(card!=null)card.ApplyPalette(palette);
        var canvas=root as VisualLayoutCanvas;if(canvas!=null)canvas.ApplyPalette(palette);
    }
}

public static class UiLayout {
    public static void ScrollContent(ScrollableControl page) {
        if(page.Controls.Count==0||page.Controls.Cast<Control>().Any(c=>c.Dock!=DockStyle.None))return;
        page.AutoScroll=true;page.AutoScrollMinSize=new Size(page.Controls.Cast<Control>().Max(c=>c.Right)+12,page.Controls.Cast<Control>().Max(c=>c.Bottom)+12);
    }
    static void Prepare(Control root) {
        int order=0;
        foreach(Control child in root.Controls.Cast<Control>().OrderBy(c=>c.Top).ThenBy(c=>c.Left)) {
            child.TabIndex=order++;
            if(string.IsNullOrWhiteSpace(child.AccessibleName)&&!string.IsNullOrWhiteSpace(child.Text))child.AccessibleName=child.Text.Replace("&","");
            if(child is Label)child.TabStop=false;
            Prepare(child);
        }
        if(root is TabPage)ScrollContent((TabPage)root);
    }
    public static void Install(Form form) {
        UiTheme.Attach(form,null);
        Prepare(form);form.AutoScaleDimensions=new SizeF(96,96);form.AutoScaleMode=AutoScaleMode.Dpi;
        form.FormBorderStyle=FormBorderStyle.Sizable;form.MaximizeBox=true;form.MinimumSize=new Size(640,480);
        form.Shown+=delegate{FitToWorkArea(form,Screen.FromControl(form).WorkingArea);};
    }
    public static void FitToWorkArea(Form form,Rectangle work) {
        int width=Math.Max(1,work.Width-16),height=Math.Max(1,work.Height-16);
        form.MinimumSize=new Size(Math.Min(form.MinimumSize.Width,width),Math.Min(form.MinimumSize.Height,height));
        form.Size=new Size(Math.Min(form.Width,width),Math.Min(form.Height,height));
        form.Location=new Point(Math.Max(work.Left,Math.Min(form.Left,work.Right-form.Width)),Math.Max(work.Top,Math.Min(form.Top,work.Bottom-form.Height)));
    }
}
