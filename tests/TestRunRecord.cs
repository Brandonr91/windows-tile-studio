using System;
using System.IO;
using System.Xml.Linq;
using System.Threading;

// Test-only bookkeeping. A failed/interrupted run cannot reuse a previous PASS.
public sealed class TestRunRecord {
    readonly string root;
    readonly XElement record;
    public readonly string DirectoryPath,Id;
    public TestRunRecord(string directory,string suite,string mode){
        root=Path.GetFullPath(directory);Id=DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ")+"-"+Guid.NewGuid().ToString("N").Substring(0,8);
        DirectoryPath=Path.Combine(root,"runs",Id);Directory.CreateDirectory(DirectoryPath);
        string revision="unknown",dirty="unknown",source=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-source.xml");
        if(File.Exists(source)){var provenance=XElement.Load(source);revision=(string)provenance.Attribute("revision")??revision;dirty=(string)provenance.Attribute("dirty")??dirty;}
        record=new XElement("TestRun",new XAttribute("schema",1),new XAttribute("id",Id),new XAttribute("suite",suite),new XAttribute("mode",mode),new XAttribute("version",SixSlotSnap.ProductVersion),new XAttribute("revision",revision),new XAttribute("sourceDirty",dirty),new XAttribute("startedUtc",DateTime.UtcNow.ToString("o")),new XAttribute("status","running"),new XAttribute("directory","runs/"+Id));
        Publish();
    }
    static void Atomic(string path,XElement xml){string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{xml.Save(temp);if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}}
    void Publish(){
        Atomic(Path.Combine(DirectoryPath,"run.xml"),record);
        var until=DateTime.UtcNow.AddSeconds(3);FileStream held=null;
        while(held==null){try{held=new FileStream(Path.Combine(root,".latest-run.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}catch(IOException){if(DateTime.UtcNow>=until)throw;Thread.Sleep(20);}}
        using(held){
            string latest=Path.Combine(root,"latest-run.xml");
            if(File.Exists(latest)){var current=XElement.Load(latest);if((string)current.Attribute("id")!=Id&&DateTime.Parse((string)current.Attribute("startedUtc"),null,System.Globalization.DateTimeStyles.RoundtripKind)>DateTime.Parse((string)record.Attribute("startedUtc"),null,System.Globalization.DateTimeStyles.RoundtripKind))return;}
            Atomic(latest,record);
        }
    }
    public void Complete(bool passed){record.SetAttributeValue("status",passed?"passed":"failed");record.SetAttributeValue("finishedUtc",DateTime.UtcNow.ToString("o"));record.SetAttributeValue(passed?"error":"result",null);record.SetAttributeValue(passed?"result":"error",passed?"integration-result.txt":"integration-error.txt");Publish();}
}
