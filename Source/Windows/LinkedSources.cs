// SPDX-License-Identifier: MIT
using System;using System.Threading;using System.IO;using System.Linq;using System.Collections;using System.Collections.Generic;using System.Threading.Tasks;using System.Windows.Forms;using Microsoft.Web.WebView2.Core;
sealed partial class WorldWindow {
 sealed class LinkedSource:IDisposable {
  internal string Id,Path,Name;internal bool Managed,FilesOnly,Busy,Pending=true,Detached;internal string Status;internal HashSet<string> Only=new HashSet<string>(StringComparer.OrdinalIgnoreCase);internal DateTime Due=DateTime.UtcNow;
  internal CancellationTokenSource Cancellation;internal FileSystemWatcher Watcher;internal readonly List<object> Renames=new List<object>();internal readonly Dictionary<string,object> Metadata=new Dictionary<string,object>();
  public void Dispose(){Detached=true;if(Cancellation!=null)Cancellation.Cancel();if(Watcher!=null){Watcher.Dispose();Watcher=null;}}
 }
 readonly Dictionary<string,LinkedSource> linkedSources=new Dictionary<string,LinkedSource>();System.Windows.Forms.Timer sourceTimer;DateTime sourcePoll=DateTime.UtcNow;
 void StartSources(){if(sourceTimer!=null)return;sourceTimer=new System.Windows.Forms.Timer{Interval=500};sourceTimer.Tick+=async(s,e)=>{if(closing)return;bool poll=(DateTime.UtcNow-sourcePoll).TotalSeconds>30;if(poll)sourcePoll=DateTime.UtcNow;foreach(var root in linkedSources.Values.ToArray()){
  if(poll&&(root.Status=="missing"&&Directory.Exists(root.Path)||root.Status!="missing"&&!Directory.Exists(root.Path)||root.Watcher==null))root.Pending=true;
  if(root.Pending&&!root.Busy&&DateTime.UtcNow>=root.Due)await RefreshSource(root);
 }};sourceTimer.Start();FormClosed+=(s,e)=>{sourceTimer.Stop();sourceTimer.Dispose();foreach(var root in linkedSources.Values)root.Dispose();};}
 void WatchSource(LinkedSource root){if(root.Watcher!=null||!Directory.Exists(root.Path)||root.Detached)return;try{
  var watcher=new FileSystemWatcher(root.Path){IncludeSubdirectories=!root.FilesOnly,NotifyFilter=NotifyFilters.FileName|NotifyFilters.DirectoryName|NotifyFilters.LastWrite|NotifyFilters.Size,InternalBufferSize=32768};
  Action<FileSystemEventArgs> queue=change=>{if(closing||root.Detached||IsDisposed)return;try{BeginInvoke(new Action(()=>{if(root.Detached)return;var rename=change as RenamedEventArgs;if(rename!=null){string prefix=root.Path.TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;if(rename.OldFullPath.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)&&rename.FullPath.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)){string old=rename.OldFullPath.Substring(prefix.Length).Replace('\\','/'),next=rename.FullPath.Substring(prefix.Length).Replace('\\','/');if(root.FilesOnly&&root.Only.Remove(old))root.Only.Add(next);if(root.Renames.Count<1000)root.Renames.Add(new{from=old,to=next});}}root.Pending=true;root.Due=DateTime.UtcNow.AddMilliseconds(650);}));}catch(InvalidOperationException){}};
  watcher.Created+=(s,e)=>queue(e);watcher.Deleted+=(s,e)=>queue(e);watcher.Changed+=(s,e)=>queue(e);watcher.Renamed+=(s,e)=>queue(e);watcher.Error+=(s,e)=>queue(new FileSystemEventArgs(WatcherChangeTypes.All,root.Path,""));watcher.EnableRaisingEvents=true;root.Watcher=watcher;
 }catch(IOException){}catch(UnauthorizedAccessException){} }
 LinkedSource RegisterSource(string id,string path,string name,bool managed,bool filesOnly,IEnumerable<string> only){
  if(!System.Text.RegularExpressions.Regex.IsMatch(id??"",@"^r[a-f0-9]{12}$")||!System.IO.Path.IsPathRooted(path))throw new InvalidOperationException("That folder reference is invalid.");
  path=System.IO.Path.GetFullPath(path);LinkedSource root;if(linkedSources.TryGetValue(id,out root)){
   if(!root.Path.Equals(path,StringComparison.OrdinalIgnoreCase)){root.Dispose();linkedSources.Remove(id);root=null;}
   else{if(root.FilesOnly!=filesOnly&&root.Watcher!=null){root.Watcher.Dispose();root.Watcher=null;}root.FilesOnly=filesOnly;root.Only.Clear();if(filesOnly&&only!=null)foreach(var item in only)root.Only.Add(item);root.Pending=true;WatchSource(root);return root;}
  }
  root=new LinkedSource{Id=id,Path=path,Name=name,Managed=managed,FilesOnly=filesOnly};if(only!=null)foreach(var item in only)root.Only.Add(item);linkedSources[id]=root;StartSources();WatchSource(root);return root;
 }
 async Task RefreshSource(LinkedSource root){if(root.Detached||root.Busy)return;root.Pending=false;root.Busy=true;root.Cancellation=new CancellationTokenSource();ManagedPost(new{type="source-progress",id=root.Id,name=root.Name,count=0,stage="Checking folder"});var renames=root.Renames.ToArray();try{
  root.Renames.Clear();var only=root.FilesOnly?root.Only.ToArray():null;
  var snapshot=await Task.Run(()=>LibrarySourceScanner.Scan(root.Path,only,root.Cancellation.Token,count=>{if(!closing&&!root.Detached)try{BeginInvoke(new Action(()=>{if(!closing&&!root.Detached)ManagedPost(new{type="source-progress",id=root.Id,name=root.Name,count,stage="Indexing files"});}));}catch(InvalidOperationException){}}));if(root.Detached||closing)return;
  if(snapshot.Missing){root.Status="missing";if(root.Watcher!=null){root.Watcher.Dispose();root.Watcher=null;}}
  else{root.Status=snapshot.Complete?"ready":"partial";WatchSource(root);browser.CoreWebView2.SetVirtualHostNameToFolderMapping(root.Id+".jagolibrary.example",root.Path,CoreWebView2HostResourceAccessKind.Allow);}
  var assets=await Task.Run(()=>snapshot.Files.Select(file=>{
   root.Cancellation.Token.ThrowIfCancellationRequested();string kind=Kind(file.Path),key=root.Id+":"+file.Relative;object metadata=null;
   lock(root.Metadata){if(kind=="font"&&!root.Metadata.TryGetValue(file.Stamp+file.Relative,out metadata)){metadata=FontInfo.Metadata(file.Path);root.Metadata[file.Stamp+file.Relative]=metadata;}}
   assetPaths[key]=file.Path;string preview=null;if(kind=="special"&&!JagoProjectPreviewNative.IsProject(file.Path))foreach(var suffix in new[]{".png",".jpg",".webp"}){string side=System.IO.Path.ChangeExtension(file.Path,suffix);if(File.Exists(side)){string rel=side.Substring(root.Path.TrimEnd(System.IO.Path.DirectorySeparatorChar).Length+1).Replace('\\','/');preview="https://"+root.Id+".jagolibrary.example/"+String.Join("/",rel.Split('/').Select(Uri.EscapeDataString));break;}}
   return new{id=key,rootId=root.Id,name=System.IO.Path.GetFileName(file.Path),type=kind,size=file.Size,stamp=file.Stamp,relativePath=file.Relative,url="https://"+root.Id+".jagolibrary.example/"+String.Join("/",file.Relative.Split('/').Select(Uri.EscapeDataString)),preview=preview,fontMeta=metadata,category=kind=="font"?FontInfo.Category(file.Path,System.IO.Path.GetFileNameWithoutExtension(file.Path)):null};
  }).ToArray());if(root.Detached||closing)return;
  ManagedPost(new{type="source-snapshot",root=new{id=root.Id,path=root.Path,name=root.Name,managed=root.Managed,filesOnly=root.FilesOnly,status=root.Status,watching=root.Watcher!=null,folders=snapshot.Folders,checkedAt=DateTime.UtcNow.ToString("o")},assets=assets,renames=renames,complete=snapshot.Complete,missing=snapshot.Missing,warnings=snapshot.Warnings.Distinct().Take(3).ToArray()});
 }catch(OperationCanceledException){root.Renames.InsertRange(0,renames);root.Pending=false;if(!closing&&!root.Detached)ManagedPost(new{type="source-cancelled",id=root.Id});}catch(Exception e){if(!closing&&!root.Detached)ManagedPost(new{type="source-error",id=root.Id,error=e.Message});}finally{root.Busy=false;root.Cancellation.Dispose();root.Cancellation=null;}}
 async Task HandleLinkedFolder(Dictionary<string,object> message){string type=Convert.ToString(message["type"]);if(type=="restore-folders")return;var paths=new List<string>();
  if(type=="folder"){string chosen=ModernFolder.Pick(Handle,"Link a folder of creative assets");if(chosen!=null)paths.Add(chosen);}
  else if(type=="test-folder"){if(Program.Test!=null)paths.Add(System.IO.Path.Combine(Program.Test,"Fixture"));}
  else{var array=message.ContainsKey("paths")?message["paths"] as ArrayList:null;if(array!=null)foreach(var item in array){var path=Convert.ToString(item);if(System.IO.Path.IsPathRooted(path))paths.Add(path);}}
  foreach(var path in paths){if(linkedSources.Values.Any(r=>r.Path.Equals(System.IO.Path.GetFullPath(path),StringComparison.OrdinalIgnoreCase)))continue;var root=RegisterSource(RootId(path),path,System.IO.Path.GetFileName(path.TrimEnd(System.IO.Path.DirectorySeparatorChar)),false,false,null);await RefreshSource(root);}
 }
 async Task HandleSources(Dictionary<string,object> message){string op=Convert.ToString(message["type"]);
  if(op=="source-companion-connect"){
   string prefs=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Jago Companion Preview","preferences.json"),shared=Path.Combine(JagoSuiteBootstrap.Root,"capture-settings.json"),path=null;
   if(message.ContainsKey("path"))path=Convert.ToString(message["path"]);
   if(String.IsNullOrEmpty(path)&&File.Exists(shared)){var data=new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(shared));if(data.ContainsKey("path"))path=Convert.ToString(data["path"]);}
   if(String.IsNullOrEmpty(path)&&File.Exists(prefs)){var data=new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(prefs));if(data.ContainsKey("CaptureFolder"))path=Convert.ToString(data["CaptureFolder"]);if(String.IsNullOrEmpty(path)&&data.ContainsKey("Home"))path=Path.Combine(Convert.ToString(data["Home"]),"Captures");}
   if(String.IsNullOrEmpty(path))path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"Jago","Companion","Captures");
   if(message.ContainsKey("choose")&&Convert.ToBoolean(message["choose"])){path=ModernFolder.Pick(Handle,"Choose the shared capture folder");if(path==null)return;}
   path=Path.GetFullPath(path);Directory.CreateDirectory(path);if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw new InvalidOperationException("Choose a regular capture folder.");Directory.CreateDirectory(JagoSuiteBootstrap.Root);File.WriteAllText(shared,new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new{path=path}));
   string id=RootId(path);var captureSource=RegisterSource(id,path,"Captures",false,false,null);await RefreshSource(captureSource);ManagedPost(new{type="companion-captures-connected",id=id,path=path});return;
  }

  if(op=="sources-register"){var roots=message.ContainsKey("roots")?message["roots"] as ArrayList:null;if(roots==null||roots.Count>100)return;var active=new HashSet<string>();foreach(Dictionary<string,object> item in roots){string id=Convert.ToString(item["id"]);active.Add(id);var only=item.ContainsKey("files")?item["files"] as ArrayList:null;RegisterSource(id,Convert.ToString(item["path"]),Convert.ToString(item["name"]),item.ContainsKey("managed")&&Convert.ToBoolean(item["managed"]),item.ContainsKey("filesOnly")&&Convert.ToBoolean(item["filesOnly"]),only==null?null:only.Cast<object>().Select(Convert.ToString));}foreach(var stale in linkedSources.Keys.Where(k=>!active.Contains(k)).ToArray()){linkedSources[stale].Dispose();linkedSources.Remove(stale);}return;}
  string key=message.ContainsKey("id")?Convert.ToString(message["id"]):"";LinkedSource source;if(!linkedSources.TryGetValue(key,out source))return;
  if(op=="source-cancel"){source.Pending=false;if(source.Cancellation!=null)source.Cancellation.Cancel();return;}if(op=="source-unlink"){source.Dispose();linkedSources.Remove(key);string path;foreach(var assetKey in assetPaths.Keys.Where(k=>k.StartsWith(key+":",StringComparison.Ordinal)).ToArray())assetPaths.TryRemove(assetKey,out path);browser.CoreWebView2.ClearVirtualHostNameToFolderMapping(key+".jagolibrary.example");return;}
  if(op=="source-relink"){string path=ModernFolder.Pick(Handle,"Locate the moved folder");if(path==null)return;if(source.Managed){string marker=System.IO.Path.Combine(path,".jago-library","library-id.txt");if(!File.Exists(marker)||File.ReadAllText(marker).Trim()!=source.Id)throw new InvalidOperationException("Choose the original library home containing its .jago-library catalog.");managedPath=path;managedId=source.Id;}source=RegisterSource(source.Id,path,source.Name,source.Managed,source.FilesOnly,source.Only.ToArray());}
  else if(op=="source-reveal"){if(Directory.Exists(source.Path))System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(source.Path){UseShellExecute=true});return;}
  await RefreshSource(source);
 }
}
