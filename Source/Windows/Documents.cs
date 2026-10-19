// SPDX-License-Identifier: MIT
using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Threading;using System.Threading.Tasks;using System.Windows.Forms;using System.Reflection;using System.Security.Cryptography;
sealed partial class WorldWindow {
 CancellationTokenSource documentCancel;bool documentCloseHook;string documentRequest;bool documentBusy;string documentEngine;
 string FindDocumentEngine(){
  string bundled=Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),"Tools","Documents","LibreOffice","program","soffice.exe");
  if(File.Exists(bundled))return bundled;if(documentEngine!=null&&File.Exists(documentEngine))return documentEngine;
  string settings=Path.Combine(Program.Root,"document-engine.txt");if(File.Exists(settings)){string saved=File.ReadAllText(settings).Trim();if(Path.GetFileName(saved).Equals("soffice.exe",StringComparison.OrdinalIgnoreCase)&&File.Exists(saved)&&File.Exists(Path.Combine(Path.GetDirectoryName(saved),"soffice.bin")))return documentEngine=saved;}
  foreach(var dir in new[]{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}){string engine=Path.Combine(dir,"LibreOffice","program","soffice.exe");if(File.Exists(engine))return engine;}return null;
 }
 async Task HandleDocuments(Dictionary<string,object> message){if(!documentCloseHook){documentCloseHook=true;FormClosed+=(s,e)=>{if(documentCancel!=null)documentCancel.Cancel();};}string type=Convert.ToString(message["type"]),request=message.ContainsKey("request")?Convert.ToString(message["request"]):"";
  if(type=="document-status"||type=="document-engine-choose"){
   if(type=="document-engine-choose")using(var pick=new OpenFileDialog{Title="Choose LibreOffice's soffice.exe",Filter="LibreOffice renderer (soffice.exe)|soffice.exe"}){if(pick.ShowDialog(this)==DialogResult.OK){if(!Path.GetFileName(pick.FileName).Equals("soffice.exe",StringComparison.OrdinalIgnoreCase)||!File.Exists(Path.Combine(Path.GetDirectoryName(pick.FileName),"soffice.bin")))throw new InvalidOperationException("Choose soffice.exe inside LibreOffice's program folder.");documentEngine=pick.FileName;File.WriteAllText(Path.Combine(Program.Root,"document-engine.txt"),documentEngine);}}
   var engine=FindDocumentEngine();ManagedPost(new{type="document-status",request=request,available=engine!=null,engine=engine==null?null:"LibreOffice",bundled=engine!=null&&engine.StartsWith(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)});return;
  }
  if(type=="document-cancel"){if(documentCancel!=null&&documentRequest==request)documentCancel.Cancel();return;}
  if(type!="document-render")return;if(documentBusy){ManagedPost(new{type="document-result",request=request,error="Another document is rendering. Wait or cancel it first."});return;}
  string work=null;documentBusy=true;documentRequest=request;documentCancel=new CancellationTokenSource();try{
   string engine=FindDocumentEngine();if(engine==null)throw new InvalidOperationException("Connect LibreOffice to enable page-accurate previews and PDF conversion. DOCX layout previews work without it.");
   string source;string id=message.ContainsKey("id")?Convert.ToString(message["id"]):"";if(!assetPaths.TryGetValue(id,out source)){
    string name=Path.GetFileName(Convert.ToString(message["name"]));if(!OfficeRenderer.Extensions.Contains(Path.GetExtension(name).ToLowerInvariant()))throw new InvalidOperationException("Unsupported document format.");string data=Convert.ToString(message["data"]);if(data.Length>28000000)throw new InvalidOperationException("Convert a document under 20 MB.");work=Path.Combine(Program.Root,"Document-jobs",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(work);source=Path.Combine(work,"source"+Path.GetExtension(name));File.WriteAllBytes(source,Convert.FromBase64String(data));
   }
   if(!OfficeRenderer.Extensions.Contains(Path.GetExtension(source).ToLowerInvariant()))throw new InvalidOperationException("Unsupported document format.");
   string sourceStamp=new FileInfo(source).LastWriteTimeUtc.Ticks.ToString()+":"+new FileInfo(source).Length;string cacheRoot=Path.Combine(Program.Root,"Document-cache");Directory.CreateDirectory(cacheRoot);string hash;using(var sha=SHA256.Create())using(var input=new FileStream(source,FileMode.Open,FileAccess.Read,FileShare.Read)){if(input.Length>20000000)throw new InvalidOperationException("Convert a document under 20 MB.");hash=BitConverter.ToString(sha.ComputeHash(input)).Replace("-","");}
   string fingerprint=engine+File.GetLastWriteTimeUtc(engine).Ticks+Path.GetExtension(source).ToLowerInvariant()+"renderer-v1";using(var sha=SHA256.Create())hash+="-"+BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(fingerprint))).Replace("-","").Substring(0,16);
   string cached=Path.Combine(cacheRoot,hash+".pdf");byte[] pdf;if(File.Exists(cached))pdf=File.ReadAllBytes(cached);else{
    if(work==null)work=Path.Combine(Program.Root,"Document-jobs",Guid.NewGuid().ToString("N"));pdf=await OfficeRenderer.Convert(engine,source,work,documentCancel.Token);documentCancel.Token.ThrowIfCancellationRequested();var current=new FileInfo(source);if(!current.Exists||sourceStamp!=current.LastWriteTimeUtc.Ticks.ToString()+":"+current.Length)throw new InvalidOperationException("The original changed while rendering. Preview it again to use the latest version.");File.WriteAllBytes(cached,pdf);
    long total=0;foreach(var old in new DirectoryInfo(cacheRoot).GetFiles("*.pdf").OrderByDescending(f=>f.LastWriteTimeUtc)){total+=old.Length;if(total>160000000&&old.FullName!=cached)try{old.Delete();}catch{}}
   }
   documentCancel.Token.ThrowIfCancellationRequested();ManagedPost(new{type="document-result",request=request,data=Convert.ToBase64String(pdf),engine="LibreOffice",cached=File.Exists(cached)});
  }catch(OperationCanceledException){ManagedPost(new{type="document-result",request=request,error="Rendering cancelled. Your original was not changed.",cancelled=true});}
  catch(Exception e){ManagedPost(new{type="document-result",request=request,error=e.Message});}
  finally{documentCancel.Dispose();documentCancel=null;documentRequest=null;documentBusy=false;if(work!=null)try{Directory.Delete(work,true);}catch{}}
 }
}
