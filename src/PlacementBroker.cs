using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Win32.SafeHandles;

// Native calls run only in an owned, job-contained process. Never queue async
// window positions or abandon an in-process thread blocked in another app.
public sealed class PlacementBroker : IDisposable {
    const int Protocol=0x57545335,Deadline=250;
    readonly string sourceRoot;
    Task<Worker> startup,spare;
    Worker retained;
    volatile bool disposed;
    int sequence;
    bool calling;
    public PlacementBroker(string source){sourceRoot=source;startup=Launch();spare=Launch();}
    Task<Worker> Launch(){return Task.Factory.StartNew(delegate{var value=new Worker(sourceRoot);if(disposed){value.Dispose();throw new ObjectDisposedException("PlacementBroker");}return value;});}
    public void WaitForStartup(){try{Task.WaitAll(new Task[]{startup,spare},8000);}catch(AggregateException){}}
    public bool Ready {get{return startup!=null&&startup.Status==TaskStatus.RanToCompletion&&startup.Result.Active;}}
    public bool Position(IntPtr window,int x,int y,int width,int height,uint flags,out string error){return Execute(1,window,x,y,width,height,flags,out error);}
    public bool Show(IntPtr window,int command,out string error){return Execute(2,window,command,0,0,0,0,out error);}
    bool Execute(int operation,IntPtr window,int x,int y,int width,int height,uint flags,out string error){
        if(calling)throw new InvalidOperationException("Reentrant placement request.");
        calling=true;try{return ExecuteOne(operation,window,x,y,width,height,flags,out error);}finally{calling=false;}
    }
    bool ExecuteOne(int operation,IntPtr window,int x,int y,int width,int height,uint flags,out string error){
        error=null;if(disposed){error="Window placement has stopped.";return false;}
        if(retained!=null){if(!retained.Stop()){error="The placement helper could not be stopped. Further placement is paused.";return false;}retained=null;}
        if(startup!=null&&startup.Status==TaskStatus.RanToCompletion&&!startup.Result.Active)Replace(startup.Result);
        if(startup==null){startup=spare??Launch();spare=Launch();}
        if(!Ready){
            if(startup.IsFaulted){error="The placement helper could not start: "+startup.Exception.GetBaseException().Message+" No window request was queued. Retry or restart Tile Studio.";startup=null;}
            else error="The placement helper is starting. Try snapping again in a moment.";
            return false;
        }
        var worker=startup.Result;int token=unchecked(++sequence);if(token==0)token=++sequence;
        uint pid,thread=NativeMethods.GetWindowThreadProcessId(window,out pid);
        if(thread==0||!SetProp(window,worker.Lease,new IntPtr(token))){error="Windows blocked placement, or the selected window closed. Matching administrator privileges may be required.";return false;}
        try {
            int result=worker.Call(operation,window,pid,thread,x,y,width,height,flags,token);
            if(result!=0){error="The window changed, closed or rejected placement. Snap it again to retry.";return false;}
            return true;
        }catch(TimeoutException){
            bool stopped=Replace(worker);
            error=stopped?"The app stopped responding during placement. The placement helper was stopped; no retry was queued. Snap the app again after it responds.":"The placement helper could not be stopped. Further placement is paused.";
            return false;
        }catch(IOException e){
            Replace(worker);
            error="The placement helper disconnected: "+e.GetBaseException().Message+" Snap the app again to retry.";return false;
        }catch(ObjectDisposedException){error="Window placement was cancelled.";return false;}
        finally{if(GetProp(window,worker.Lease)==new IntPtr(token))RemoveProp(window,worker.Lease);}
    }
    bool Replace(Worker worker){
        startup=null;if(!worker.Stop()){retained=worker;return false;}
        startup=spare;spare=Launch();return true;
    }
    public void Dispose(){
        disposed=true;if(retained!=null)retained.Dispose();
        Cleanup(startup);Cleanup(spare);
    }
    static void Cleanup(Task<Worker> pending){
        if(pending==null)return;
        if(pending.Status==TaskStatus.RanToCompletion)pending.Result.Dispose();
        else pending.ContinueWith(delegate(Task<Worker> task){if(task.Status==TaskStatus.RanToCompletion)task.Result.Dispose();else if(task.IsFaulted){var ignored=task.Exception;}});
    }
    sealed class Worker : IDisposable {
        public readonly string Lease;
        readonly NamedPipeServerStream pipe;
        Process process;
        SafeFileHandle job;
        bool closed;
        public Worker(string source){
            string name="WtsPlacement-"+Guid.NewGuid().ToString("N");Lease="WindowsTileStudio."+name;
            var security=new PipeSecurity();security.SetAccessRuleProtection(true,false);security.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().User,PipeAccessRights.FullControl,AccessControlType.Allow));
            pipe=new NamedPipeServerStream(name,PipeDirection.InOut,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous,1024,1024,security);
            try {
                job=new SafeFileHandle(CreateJobObject(IntPtr.Zero,null),true);if(job.IsInvalid)throw new IOException("Could not create placement job.");
                var info=new ExtendedLimits();info.Basic.Flags=0x2000;
                if(!SetInformationJobObject(job,9,ref info,(uint)Marshal.SizeOf(info)))throw new IOException("Could not contain placement helper.");
                string assembly=typeof(SixSlotSnap).Assembly.Location;ProcessStartInfo start;
                if(!string.IsNullOrEmpty(assembly))start=new ProcessStartInfo(assembly,"--placement-worker "+name+" "+Process.GetCurrentProcess().Id);
                else {
                    if(string.IsNullOrEmpty(source)||!File.Exists(Path.Combine(source,"Start-Hosted.ps1")))throw new IOException("Hosted placement source is unavailable.");
                    start=new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),@"WindowsPowerShell\v1.0\powershell.exe"),"-NoProfile -STA -ExecutionPolicy Bypass -File \""+Path.Combine(source,"Start-Hosted.ps1")+"\" -RunInProcess -Action PlacementWorker -PlacementPipe "+name+" -PlacementParent "+Process.GetCurrentProcess().Id);
                }
                start.UseShellExecute=false;start.CreateNoWindow=true;start.WindowStyle=ProcessWindowStyle.Hidden;
                var connect=Task.Factory.FromAsync(pipe.BeginWaitForConnection,pipe.EndWaitForConnection,null);
                process=Process.Start(start);if(!AssignProcessToJobObject(job,process.Handle))throw new IOException("Could not contain placement helper process.");
                Await(connect,6000);
                uint peer;if(!GetNamedPipeClientProcessId(pipe.SafePipeHandle,out peer)||peer!=process.Id)throw new IOException("Unexpected placement pipe client.");
                byte[] ready=Read(8,2000);if(BitConverter.ToInt32(ready,0)!=Protocol||BitConverter.ToInt32(ready,4)!=process.Id)throw new IOException("Invalid placement handshake.");
                int length=BitConverter.ToInt32(Read(4,1000),0);if(length<1||length>64||Encoding.UTF8.GetString(Read(length,1000))!=SixSlotSnap.ProductVersion)throw new IOException("Placement helper version mismatch.");
            }catch{Dispose();throw;}
        }
        byte[] Read(int count,int timeout){
            var bytes=new byte[count];int offset=0;var clock=Stopwatch.StartNew();
            while(offset<count){var operation=pipe.ReadAsync(bytes,offset,count-offset);Await(operation,Math.Max(1,timeout-(int)clock.ElapsedMilliseconds));int got=operation.Result;if(got==0)throw new EndOfStreamException();offset+=got;}
            return bytes;
        }
        void Await(Task operation,int timeout){
            try{if(operation.Wait(timeout))return;}
            catch(AggregateException e){throw new IOException("Placement pipe failed.",e.GetBaseException());}
            pipe.Dispose();operation.ContinueWith(delegate(Task done){if(done.IsFaulted){var ignored=done.Exception;}});throw new TimeoutException();
        }
        public int Call(int operation,IntPtr window,uint pid,uint thread,int x,int y,int width,int height,uint flags,int token){
            byte[] packet;using(var memory=new MemoryStream())using(var writer=new BinaryWriter(memory)){writer.Write(operation);writer.Write(window.ToInt64());writer.Write(pid);writer.Write(thread);writer.Write(x);writer.Write(y);writer.Write(width);writer.Write(height);writer.Write(flags);writer.Write(token);packet=memory.ToArray();}
            var clock=Stopwatch.StartNew();Await(pipe.WriteAsync(packet,0,packet.Length),Deadline);
            byte[] reply=Read(12,Math.Max(1,Deadline-(int)clock.ElapsedMilliseconds));
            if(BitConverter.ToInt32(reply,0)!=token)throw new IOException("Stale placement response.");return BitConverter.ToInt32(reply,4);
        }
        public bool Stop(){
            if(!closed){closed=true;try{pipe.Dispose();}catch(IOException){}finally{if(job!=null){job.Dispose();job=null;}}}
            if(process==null)return true;
            try{if(!process.HasExited){process.Kill();if(!process.WaitForExit(500))return false;}process.Dispose();process=null;return true;}
            catch(System.ComponentModel.Win32Exception){if(process.HasExited){process.Dispose();process=null;return true;}return false;}catch(InvalidOperationException){return false;}
        }
        public bool Active {get{try{return !closed&&process!=null&&!process.HasExited;}catch(InvalidOperationException){return false;}}}
        public void Dispose(){Stop();}
    }
    public static int RunWorker(string name,int parent){
        Guid parsed;if(name==null||!name.StartsWith("WtsPlacement-",StringComparison.Ordinal)||!Guid.TryParseExact(name.Substring(13),"N",out parsed)||parent<=0)return 2;
        try {
            NativeMethods.SetThreadDpiAwarenessContext(new IntPtr(-4));
            using(var pipe=new NamedPipeClientStream(".",name,PipeDirection.InOut,PipeOptions.None)){
                pipe.Connect(6000);uint owner;if(!GetNamedPipeServerProcessId(pipe.SafePipeHandle,out owner)||owner!=parent)return 2;
                using(var reader=new BinaryReader(pipe))using(var writer=new BinaryWriter(pipe)){
                    writer.Write(Protocol);writer.Write(Process.GetCurrentProcess().Id);byte[] version=Encoding.UTF8.GetBytes(SixSlotSnap.ProductVersion);writer.Write(version.Length);writer.Write(version);writer.Flush();
                    while(true){
                        int op=reader.ReadInt32();var window=new IntPtr(reader.ReadInt64());uint pid=reader.ReadUInt32(),thread=reader.ReadUInt32();int x=reader.ReadInt32(),y=reader.ReadInt32(),w=reader.ReadInt32(),h=reader.ReadInt32();uint flags=reader.ReadUInt32();int token=reader.ReadInt32();
                        int result=1,nativeError=0;uint actualPid;uint actualThread=NativeMethods.GetWindowThreadProcessId(window,out actualPid);
                        if(GetProp(window,"WindowsTileStudio."+name)==new IntPtr(token)&&thread==actualThread&&pid==actualPid&&thread!=0&&!NativeMethods.IsHungAppWindow(window)){
                            if(op==1&&flags==0x14&&w>0&&h>0){result=NativeMethods.SetWindowPos(window,IntPtr.Zero,x,y,w,h,flags)?0:1;nativeError=Marshal.GetLastWin32Error();}
                            else if(op==2&&(x==9||x==3||x==6)){NativeMethods.ShowWindow(window,x);result=0;}
                        }
                        writer.Write(token);writer.Write(result);writer.Write(nativeError);writer.Flush();
                    }
                }
            }
        }catch(IOException){return 0;}catch(TimeoutException){return 2;}catch(UnauthorizedAccessException){return 2;}
    }
    [StructLayout(LayoutKind.Sequential)] struct BasicLimits {public long ProcessTime,JobTime;public uint Flags;public UIntPtr MinWorking,MaxWorking;public uint ProcessLimit;public UIntPtr Affinity;public uint Priority,Scheduling;}
    [StructLayout(LayoutKind.Sequential)] struct IoCounters {public ulong ReadOps,WriteOps,OtherOps,ReadBytes,WriteBytes,OtherBytes;}
    [StructLayout(LayoutKind.Sequential)] struct ExtendedLimits {public BasicLimits Basic;public IoCounters Io;public UIntPtr ProcessMemory,JobMemory,PeakProcess,PeakJob;}
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern IntPtr CreateJobObject(IntPtr attributes,string name);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool SetInformationJobObject(SafeFileHandle job,int info,ref ExtendedLimits limits,uint length);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool AssignProcessToJobObject(SafeFileHandle job,IntPtr process);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool GetNamedPipeClientProcessId(SafePipeHandle pipe,out uint process);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool GetNamedPipeServerProcessId(SafePipeHandle pipe,out uint process);
    [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern bool SetProp(IntPtr window,string key,IntPtr value);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern IntPtr GetProp(IntPtr window,string key);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern IntPtr RemoveProp(IntPtr window,string key);
}
