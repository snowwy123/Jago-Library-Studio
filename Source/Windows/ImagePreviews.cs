// SPDX-License-Identifier: MIT
using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Microsoft.Web.WebView2.Core;

sealed partial class WorldWindow {
 readonly SemaphoreSlim imagePreviewGate=new SemaphoreSlim(1,1);
 string ImagePreviewFolder(){
  string path=Path.Combine(Program.Root,"Image-previews");Directory.CreateDirectory(path);
  if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw new IOException("The preview cache cannot be a redirected folder.");
  return path;
 }
 async Task HandleImagePreview(Dictionary<string,object> message){
  string request=Convert.ToString(message["request"]);bool entered=false;
  try {
   string key=Convert.ToString(message["id"]),source;
   if(!assetPaths.TryGetValue(key,out source)||!File.Exists(source))throw new IOException("Original image unavailable. Reconnect its file or folder.");
   if(!new[]{".png",".jpg",".jpeg",".bmp",".gif",".tif",".tiff"}.Contains(Path.GetExtension(source).ToLowerInvariant()))throw new IOException("Use the browser decoder for this image format.");
   int requested=message.ContainsKey("edge")?Convert.ToInt32(message["edge"]):640;
   int edge=requested<=640?640:requested<=2048?2048:4096;
   entered=await imagePreviewGate.WaitAsync(60000);if(!entered)throw new IOException("Preview queue is busy. Try again shortly.");
   var info=new FileInfo(source);if(info.Length>536870912)throw new IOException("This image exceeds the 512 MB source limit.");
   long stamp=info.LastWriteTimeUtc.Ticks,length=info.Length;
   string identity="image-preview-v1|"+Path.GetFullPath(source).ToLowerInvariant()+"|"+length+"|"+stamp+"|"+edge;
   string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(identity))).Replace("-","").ToLowerInvariant();
   string folder=ImagePreviewFolder(),output=Path.Combine(folder,hash+".png"),meta=Path.Combine(folder,hash+".json");
   var json=new JavaScriptSerializer();Dictionary<string,object> dimensions;
   if(File.Exists(output)&&File.Exists(meta))dimensions=json.Deserialize<Dictionary<string,object>>(File.ReadAllText(meta));
   else {
    string temporary=output+".tmp",helper=Path.Combine(Program.Files,"Jago-Image-Preview.exe");
    try {
     var start=new ProcessStartInfo(helper){Arguments="\""+source+"\" \""+temporary+"\" "+edge,UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true};
     using(var process=new Process{StartInfo=start})using(var job=new OfficeProcessJob(1073741824L)){
      process.Start();job.Attach(process);process.StandardInput.WriteLine("go");process.StandardInput.Close();
      var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();var finished=Task.Run(()=>process.WaitForExit());
      if(await Task.WhenAny(finished,Task.Delay(20000))!=finished){job.Dispose();throw new IOException("This image took too long to preview. Try an optimised copy.");}
      string data=await stdout,error=await stderr;
      if(process.ExitCode!=0||!File.Exists(temporary))throw new IOException(String.IsNullOrWhiteSpace(error)?"This image could not be previewed within the memory budget.":error.Substring(0,Math.Min(300,error.Length)));
      dimensions=json.Deserialize<Dictionary<string,object>>(data);
     }
     info.Refresh();if(info.Length!=length||info.LastWriteTimeUtc.Ticks!=stamp)throw new IOException("Image changed while loading. Please retry.");
     if(File.Exists(output))File.Delete(output);File.Move(temporary,output);File.WriteAllText(meta,json.Serialize(dimensions));
    }finally{try{if(File.Exists(temporary))File.Delete(temporary);}catch{}}
   }
   File.SetLastWriteTimeUtc(output,DateTime.UtcNow);
   // Cache host is mapped before the initial navigation.
   ManagedPost(new{type="image-preview-result",request,url="https://jagoimagecache.example/"+hash+".png",width=dimensions["width"],height=dimensions["height"],sourceWidth=dimensions["sourceWidth"],sourceHeight=dimensions["sourceHeight"]});
   TrimImagePreviews(folder,output);
  }catch(Exception error){ManagedPost(new{type="image-preview-result",request,error=error.Message});}
  finally{if(entered)imagePreviewGate.Release();}
 }
 void TrimImagePreviews(string folder,string keep){
  try {
   var files=new DirectoryInfo(folder).GetFiles("*.png").Where(f=>System.Text.RegularExpressions.Regex.IsMatch(f.Name,"^[0-9a-f]{64}\\.png$")&&(f.Attributes&FileAttributes.ReparsePoint)==0).OrderBy(f=>f.LastWriteTimeUtc).ToList();
   long bytes=files.Sum(f=>f.Length);
   foreach(var file in files){if(bytes<=268435456)break;if(file.FullName==keep)continue;long size=file.Length;file.Delete();bytes-=size;string meta=Path.ChangeExtension(file.FullName,".json");if(File.Exists(meta)&&(File.GetAttributes(meta)&FileAttributes.ReparsePoint)==0)File.Delete(meta);}
  }catch{}
 }
}
