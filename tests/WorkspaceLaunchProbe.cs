using System;
using System.IO;
class WorkspaceLaunchProbe {
    static void Main(string[] args){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"launch-probe-args.txt"),args.Length.ToString());}
}
