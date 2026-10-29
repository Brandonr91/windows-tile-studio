using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

public class UiPalette {
    public Color Background,Panel,Text,Muted,Line,Selected,SelectedText,Border;
    public static UiPalette Current {get{return ForContrast(SystemInformation.HighContrast);}}
    public static UiPalette ForContrast(bool contrast) {
        return contrast?new UiPalette{Background=SystemColors.Window,Panel=SystemColors.Control,Text=SystemColors.WindowText,Muted=SystemColors.WindowText,Line=SystemColors.Highlight,Selected=SystemColors.Highlight,SelectedText=SystemColors.HighlightText,Border=SystemColors.WindowText}:
            new UiPalette{Background=Color.FromArgb(24,28,35),Panel=Color.FromArgb(47,66,88),Text=Color.White,Muted=Color.FromArgb(195,207,224),Line=Color.FromArgb(75,201,231),Selected=Color.FromArgb(28,112,183),SelectedText=Color.White,Border=Color.FromArgb(130,151,177)};
    }
    public static void Apply(Control root,UiPalette palette) {
        root.BackColor=palette.Background;root.ForeColor=palette.Text;
        var button=root as Button;if(button!=null){button.UseVisualStyleBackColor=false;button.BackColor=palette.Panel;button.FlatAppearance.BorderColor=palette.Border;button.FlatAppearance.MouseOverBackColor=palette.Selected;button.FlatAppearance.MouseDownBackColor=palette.Selected;}
        foreach(Control child in root.Controls)Apply(child,palette);
        var card=root as LayoutCard;if(card!=null)card.ApplyPalette(palette);
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
