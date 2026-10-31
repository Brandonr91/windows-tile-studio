using System;
using System.Windows.Forms;
using System.IO;
using System.Globalization;
using System.Xml.Linq;

public sealed class LocalStatusLog {
    readonly string path,version;
    public readonly string RunId=Guid.NewGuid().ToString("N");
    public LocalStatusLog(string folder,string productVersion){path=Path.Combine(folder,"status.log");version=productVersion;}
    static string OneLine(string value){return (value??"").Replace("\r","\\r").Replace("\n","\\n");}
    public bool Write(string severity,string code,string message){
        string line=DateTime.UtcNow.ToString("o",CultureInfo.InvariantCulture)+" level="+OneLine(severity)+" code="+OneLine(code)+" version="+OneLine(version)+" run="+RunId+" "+OneLine(message)+Environment.NewLine;
        try{File.AppendAllText(path,line);return true;}catch(IOException){return false;}catch(UnauthorizedAccessException){return false;}
    }
}

// Per-process presentation policy. Logging and recovery never depend on popups.
public sealed class NotificationDelivery {
    public readonly bool Quiet;
    public int PopupCount {get;private set;}
    public int LoggedCount {get;private set;}
    readonly Action<string,string,string> log;
    readonly Action<int,string,string,ToolTipIcon> show;
    public NotificationDelivery(bool quiet,Action<string> write,Action<int,string,string,ToolTipIcon> display):this(quiet,delegate(string severity,string code,string text){write(text);},display){}
    public NotificationDelivery(bool quiet,Action<string,string,string> write,Action<int,string,string,ToolTipIcon> display){Quiet=quiet;log=write;show=display;}
    public void Post(int duration,string title,string message,ToolTipIcon icon,bool popup=true){
        string code=title=="Settings recovery"?"SETTINGS_RECOVERY":title=="Window does not fit"?"PLACEMENT_REJECTED":title=="Linked resizing unavailable"?"RESIZE_HOOK_UNAVAILABLE":"NOTIFICATION";
        log(icon==ToolTipIcon.Error?"ERROR":icon==ToolTipIcon.Warning?"WARN":"INFO",code,"Notification ["+title+"] "+message);LoggedCount++;
        if(!Quiet&&popup){show(duration,title,message,icon);PopupCount++;}
    }
}

// Immutable local evidence. Restoration failures must never replace the cause.
public sealed class WindowFailureTrace {
    public string First {get;private set;}
    public bool Record(XElement evidence) {
        if(First!=null)return false;
        First=evidence.ToString(SaveOptions.DisableFormatting);return true;
    }
}
