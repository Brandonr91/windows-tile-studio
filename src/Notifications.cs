using System;
using System.Windows.Forms;

// Per-process presentation policy. Logging and recovery never depend on popups.
public sealed class NotificationDelivery {
    public readonly bool Quiet;
    public int PopupCount {get;private set;}
    public int LoggedCount {get;private set;}
    readonly Action<string> log;
    readonly Action<int,string,string,ToolTipIcon> show;
    public NotificationDelivery(bool quiet,Action<string> write,Action<int,string,string,ToolTipIcon> display){Quiet=quiet;log=write;show=display;}
    public void Post(int duration,string title,string message,ToolTipIcon icon,bool popup=true){
        log("Notification ["+title+"] "+message);LoggedCount++;
        if(!Quiet&&popup){show(duration,title,message,icon);PopupCount++;}
    }
}
