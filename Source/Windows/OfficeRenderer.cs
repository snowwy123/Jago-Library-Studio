// SPDX-License-Identifier: MIT
using System;using System.IO;using System.Linq;using System.Text;using System.Threading;using System.Threading.Tasks;using System.Diagnostics;using System.Security.Cryptography;using System.IO.Compression;using System.Xml;using System.Xml.Linq;
internal static class OfficeRenderer {
 internal static readonly string[] Extensions={".docx",".doc",".odt",".rtf",".pptx",".ppt",".odp",".xlsx",".xls",".ods"};
 internal static async Task<byte[]> Convert(string engine,string source,string work,CancellationToken cancel){
  if(!Extensions.Contains(Path.GetExtension(source).ToLowerInvariant()))throw new InvalidOperationException("Choose a Word, OpenDocument, RTF, PowerPoint or Excel document.");
  var original=new FileInfo(source);if(!original.Exists||original.Length>20000000||(original.Attributes&FileAttributes.ReparsePoint)!=0)throw new InvalidOperationException("Choose an available document under 20 MB.");
  Directory.CreateDirectory(work);string profile=Path.Combine(work,"profile"),user=Path.Combine(profile,"user"),input=Path.Combine(work,"document"+original.Extension.ToLowerInvariant());Directory.CreateDirectory(user);
  // A private profile never inherits trusted macro locations or modifies the user's office preferences.
  File.WriteAllText(Path.Combine(user,"registrymodifications.xcu"),"<?xml version=\"1.0\"?><oor:items xmlns:oor=\"http://openoffice.org/2001/registry\"><item oor:path=\"/org.openoffice.Office.Common/Security/Scripting\"><prop oor:name=\"MacroSecurityLevel\" oor:op=\"fuse\"><value>3</value></prop><prop oor:name=\"DisableMacrosExecution\" oor:op=\"fuse\"><value>true</value></prop><prop oor:name=\"SecureURL\" oor:op=\"fuse\"><value/></prop></item><item oor:path=\"/org.openoffice.Office.Writer/Content/Update\"><prop oor:name=\"Link\" oor:op=\"fuse\"><value>0</value></prop></item></oor:items>");
  using(var src=new FileStream(source,FileMode.Open,FileAccess.Read,FileShare.Read))using(var dest=File.Create(input))await src.CopyToAsync(dest);
  if(new[]{".docx",".pptx",".xlsx"}.Contains(original.Extension.ToLowerInvariant()))StripExternalResources(input);cancel.ThrowIfCancellationRequested();string args="-env:UserInstallation="+new Uri(profile+Path.DirectorySeparatorChar).AbsoluteUri+" --headless --norestore --nodefault --nofirststartwizard --convert-to pdf --outdir \""+work+"\" \""+input+"\"";
  var start=new ProcessStartInfo(engine,args){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=Path.GetDirectoryName(engine),RedirectStandardError=true,RedirectStandardOutput=true};
  using(var process=new Process{StartInfo=start})using(var job=new OfficeProcessJob()){
   process.Start();job.Attach(process);var error=process.StandardError.ReadToEndAsync();var output=process.StandardOutput.ReadToEndAsync();var finished=Task.Run(()=>process.WaitForExit());var cancelled=Task.Delay(120000,cancel);
   if(await Task.WhenAny(finished,cancelled)!=finished){job.Dispose();try{process.Kill();}catch{}if(cancel.IsCancellationRequested)throw new OperationCanceledException();throw new TimeoutException("Document rendering took too long. Try a smaller document.");}
   string stderr=await error;await output;cancel.ThrowIfCancellationRequested();string pdf=Path.Combine(work,"document.pdf");if(process.ExitCode!=0||!File.Exists(pdf))throw new InvalidOperationException("The document could not be rendered. It may be encrypted, damaged or unsupported.");
   if(new FileInfo(pdf).Length>20000000)throw new InvalidOperationException("The PDF exceeds the 20 MB preview limit. Reduce large embedded images.");return File.ReadAllBytes(pdf);
  }
 }
 internal static void StripExternalResources(string input){using(var archive=ZipFile.Open(input,ZipArchiveMode.Update)){
  var relationships=archive.Entries.Where(e=>e.FullName.EndsWith(".rels",StringComparison.OrdinalIgnoreCase)).ToArray();if(relationships.Length>2500)throw new InvalidOperationException("Document relationships are too complex.");
  foreach(var entry in relationships){if(entry.Length>1000000)throw new InvalidOperationException("Document relationships exceed the rendering limit.");XDocument xml;using(var stream=entry.Open())using(var reader=XmlReader.Create(stream,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=1000000}))xml=XDocument.Load(reader);
   var external=xml.Descendants().Where(e=>e.Name.LocalName=="Relationship"&&(string)e.Attribute("TargetMode")=="External"&&!((string)e.Attribute("Type")??"").EndsWith("/hyperlink",StringComparison.OrdinalIgnoreCase)).ToArray();if(external.Length==0)continue;foreach(var relation in external)relation.Remove();string name=entry.FullName;entry.Delete();using(var output=archive.CreateEntry(name).Open())xml.Save(output);
  }
 }}
}
// Kill only this conversion and its children on cancellation or timeout.
internal sealed class OfficeProcessJob:IDisposable {
 IntPtr handle;
 [System.Runtime.InteropServices.DllImport("kernel32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode)]static extern IntPtr CreateJobObject(IntPtr attributes,string name);
 [System.Runtime.InteropServices.DllImport("kernel32.dll")]static extern bool SetInformationJobObject(IntPtr job,int type,IntPtr info,uint length);
 [System.Runtime.InteropServices.DllImport("kernel32.dll")]static extern bool AssignProcessToJobObject(IntPtr job,IntPtr process);
 [System.Runtime.InteropServices.DllImport("kernel32.dll")]static extern bool CloseHandle(IntPtr obj);
 public OfficeProcessJob(long memoryLimit=0){handle=CreateJobObject(IntPtr.Zero,null);if(handle==IntPtr.Zero)throw new IOException("Could not create a document conversion job.");var limits=new byte[IntPtr.Size==8?144:112];BitConverter.GetBytes(memoryLimit>0?0x2100u:0x2000u).CopyTo(limits,16);if(memoryLimit>0){if(IntPtr.Size==8)BitConverter.GetBytes(memoryLimit).CopyTo(limits,112);else BitConverter.GetBytes((uint)memoryLimit).CopyTo(limits,96);}var ptr=System.Runtime.InteropServices.Marshal.AllocHGlobal(limits.Length);try{System.Runtime.InteropServices.Marshal.Copy(limits,0,ptr,limits.Length);if(!SetInformationJobObject(handle,9,ptr,(uint)limits.Length))throw new IOException("Could not isolate document conversion.");}finally{System.Runtime.InteropServices.Marshal.FreeHGlobal(ptr);}}
 internal void Attach(Process process){if(!AssignProcessToJobObject(handle,process.Handle)){try{process.Kill();}catch{}throw new IOException("Could not isolate the document renderer.");}}
 public void Dispose(){if(handle!=IntPtr.Zero){CloseHandle(handle);handle=IntPtr.Zero;}}
}
