using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

public interface IStartupValue {
    string Read();
    void Write(string value);
    void Remove();
}
public class UserStartupValue : IStartupValue {
    public readonly string ShortcutPath;
    public UserStartupValue():this(Environment.GetFolderPath(Environment.SpecialFolder.Startup)){}
    public UserStartupValue(string folder){ShortcutPath=Path.Combine(folder,"Windows Tile Studio.lnk");}
    static object Member(object value,string name,BindingFlags flags,params object[] args){return value.GetType().InvokeMember(name,flags,null,value,args);}
    static object Shell(){var type=Type.GetTypeFromProgID("WScript.Shell");if(type==null)throw new InvalidOperationException("Windows shortcut support is unavailable.");return Activator.CreateInstance(type);}
    static void Release(object value){if(value!=null&&Marshal.IsComObject(value))Marshal.FinalReleaseComObject(value);}
    public string Read() {
        if(Directory.Exists(ShortcutPath))return "[startup shortcut path is a directory; preserved]";
        if(!File.Exists(ShortcutPath))return null;object shell=null,link=null;
        try{shell=Shell();link=Member(shell,"CreateShortcut",BindingFlags.InvokeMethod,ShortcutPath);string target=Convert.ToString(Member(link,"TargetPath",BindingFlags.GetProperty)),args=Convert.ToString(Member(link,"Arguments",BindingFlags.GetProperty));return "\""+target+"\" "+args;}
        catch{return "[unreadable startup shortcut; preserved]";}finally{Release(link);Release(shell);}
    }
    public void Write(string value) {
        if(!value.StartsWith("\"",StringComparison.Ordinal))throw new ArgumentException("Invalid startup command.");int end=value.IndexOf('"',1);if(end<2)throw new ArgumentException("Invalid startup command.");
        string target=value.Substring(1,end-1),args=value.Substring(end+1).TrimStart(),folder=Path.GetDirectoryName(ShortcutPath);Directory.CreateDirectory(folder);string temp=Path.Combine(folder,Guid.NewGuid().ToString("N")+".lnk");object shell=null,link=null;
        try {
            shell=Shell();link=Member(shell,"CreateShortcut",BindingFlags.InvokeMethod,temp);Member(link,"TargetPath",BindingFlags.SetProperty,target);Member(link,"Arguments",BindingFlags.SetProperty,args);Member(link,"WorkingDirectory",BindingFlags.SetProperty,Path.GetDirectoryName(target));Member(link,"WindowStyle",BindingFlags.SetProperty,7);Member(link,"Description",BindingFlags.SetProperty,"Windows Tile Studio login startup");Member(link,"Save",BindingFlags.InvokeMethod);Release(link);link=null;
            if(File.Exists(ShortcutPath))File.Replace(temp,ShortcutPath,null);else File.Move(temp,ShortcutPath);
        }finally{Release(link);Release(shell);if(File.Exists(temp))File.Delete(temp);}
    }
    public void Remove(){if(File.Exists(ShortcutPath))File.Delete(ShortcutPath);}
}
public class StartupLaunchSpec {
    public string Executable,SourceScript,InstalledScript,DataDirectory;
    public string UnavailableReason;
    static string Quote(string value){if(string.IsNullOrWhiteSpace(value)||value.IndexOfAny(new[]{'"','\r','\n','\0'})>=0||!Path.IsPathRooted(value))throw new ArgumentException("Startup needs valid absolute installation/data paths.");string trimmed=value.TrimEnd('\\');return "\""+trimmed+new string('\\',(value.Length-trimmed.Length)*2)+"\"";}
    public string Command {
        get {
            if(UnavailableReason!=null)throw new InvalidOperationException(UnavailableReason);
            string executable=Quote(Executable),data=Quote(DataDirectory);
            if(InstalledScript!=null)return executable+" -NoProfile -STA -ExecutionPolicy Bypass -WindowStyle Hidden -File "+Quote(InstalledScript)+" -StartupOwner WindowsTileStudio";
            if(SourceScript!=null)return executable+" -NoProfile -STA -ExecutionPolicy Bypass -WindowStyle Hidden -File "+Quote(SourceScript)+" -RunInProcess -DataDirectory "+data+" -StartupOwner WindowsTileStudio";
            return executable+" --data-dir "+data+" --startup-owner WindowsTileStudio";
        }
    }
    public void ValidateFiles(){var command=Command;if(!string.Equals(Path.GetExtension(Executable),".exe",StringComparison.OrdinalIgnoreCase)||!File.Exists(Executable)||SourceScript!=null&&(!string.Equals(Path.GetExtension(SourceScript),".ps1",StringComparison.OrdinalIgnoreCase)||!File.Exists(SourceScript))||InstalledScript!=null&&(!string.Equals(Path.GetExtension(InstalledScript),".ps1",StringComparison.OrdinalIgnoreCase)||!File.Exists(InstalledScript))||!Directory.Exists(DataDirectory))throw new InvalidOperationException("This installation or data folder is missing. Restore its files before enabling login startup.");}
    public static string InstalledLauncher(string sourceRoot,string dataDirectory) {
        try {
            if(sourceRoot==null)return null;var version=new DirectoryInfo(Path.GetFullPath(sourceRoot));
            if(version.Parent==null||version.Parent.Name!="versions"||version.Parent.Parent==null||!Regex.IsMatch(version.Name,@"^(?:[0-9a-f]{40}|(?:local|archive)-[0-9a-f]{24})$"))return null;
            string root=version.Parent.Parent.FullName,marker=Path.Combine(root,".windows-tile-studio.json"),script=Path.Combine(root,"Launch-Installed.ps1");
            if(!string.Equals(Path.GetFullPath(dataDirectory).TrimEnd('\\'),Path.Combine(root,"data"),StringComparison.OrdinalIgnoreCase)||!File.Exists(marker)||new FileInfo(marker).Length>4096||!File.Exists(script)||!File.Exists(Path.Combine(root,"current.json")))return null;
            string identity=File.ReadAllText(marker);if(!Regex.IsMatch(identity,"\"schema\"\\s*:\\s*1(?:\\s*[,}])")||!Regex.IsMatch(identity,"\"repository\"\\s*:\\s*\"Brandonr91/windows-tile-studio\""))return null;
            return script;
        }catch(IOException){return null;}catch(UnauthorizedAccessException){return null;}catch(ArgumentException){return null;}
    }
}
public class StartupRegistration {
    readonly IStartupValue store;
    public readonly StartupLaunchSpec Launch;
    public StartupRegistration(StartupLaunchSpec launch,IStartupValue value){Launch=launch;store=value;}
    public string Current {get{return store.Read();}}
    public static bool Owned(string command){return command!=null&&(command.EndsWith(" --startup-owner WindowsTileStudio",StringComparison.Ordinal)||command.EndsWith(" -StartupOwner WindowsTileStudio",StringComparison.Ordinal));}
    public string Status {
        get {string current=Current;if(current==null)return "Login startup is off.";if(string.Equals(current,Launch.Command,StringComparison.OrdinalIgnoreCase))return "A login startup shortcut is registered for this installation. Windows startup settings can override it.";return Owned(current)?"Login startup points to another Tile Studio installation.":"A different command occupies this startup entry; it will not be changed.";}
    }
    public void Enable(bool replaceOwned) {
        Launch.ValidateFiles();string current=Current;
        if(current!=null&&!string.Equals(current,Launch.Command,StringComparison.OrdinalIgnoreCase)&&(!Owned(current)||!replaceOwned))throw new InvalidOperationException("An existing startup command was preserved. Confirm replacement of a Tile Studio entry, or manage the unrelated entry in Windows.");
        store.Write(Launch.Command);
    }
    public void Disable() {
        string current=Current;if(current==null)return;if(!Owned(current)&&!string.Equals(current,Launch.Command,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("The unrelated startup entry was preserved.");store.Remove();
    }
}
