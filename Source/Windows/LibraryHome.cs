// SPDX-License-Identifier: MIT
using System;using System.IO;using System.Linq;using System.Collections;using System.Collections.Generic;using System.Threading.Tasks;using System.Security.Cryptography;
sealed partial class WorldWindow {
 string setupHomePath;
 string HomePreferencesFile {get{return Path.Combine(JagoSuiteBootstrap.Root,"Library","creative-home.txt");}}
 static string HomeHash(string file){using(var sha=SHA256.Create())using(var stream=File.OpenRead(file))return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","");}
 async Task HandleLibraryHome(Dictionary<string,object> m){string type=Convert.ToString(m["type"]),request=Convert.ToString(m["request"]);try{
 if(type=="project-home-known"){string file=HomePreferencesFile,path=File.Exists(file)?File.ReadAllText(file):null;ManagedPost(new{type="project-result",request,path,available=path!=null&&Directory.Exists(path)&&File.Exists(Path.Combine(path,".jago-library","library-id.txt"))});
 }else if(type=="project-home-connect"){string file=HomePreferencesFile;if(!File.Exists(file))throw new InvalidOperationException("Choose a home folder first.");string path=File.ReadAllText(file),marker=Path.Combine(path,".jago-library","library-id.txt");if(!File.Exists(marker))throw new InvalidOperationException("The remembered home is unavailable. Reconnect its drive or locate it in Manage sources.");await HandleManaged(new Dictionary<string,object>{{"type","restore-library-home"},{"path",path},{"id",File.ReadAllText(marker).Trim()}});ManagedPost(new{type="project-result",request,path});
 }else if(type=="project-home-pick"){
  string path=Program.Test!=null&&m.ContainsKey("path")?Convert.ToString(m["path"]):ModernFolder.Pick(Handle,"Choose or create your creative library folder");if(path==null){ManagedPost(new{type="project-result",request,cancelled=true});return;}
  path=Path.GetFullPath(path);if(!Directory.Exists(path)||(File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw new InvalidOperationException("Choose a regular folder on your drive.");
  string install=Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar),system=Environment.GetFolderPath(Environment.SpecialFolder.Windows);
  if(path==Path.GetPathRoot(path)||path.StartsWith(install+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||path.Equals(install,StringComparison.OrdinalIgnoreCase)||path.Equals(system,StringComparison.OrdinalIgnoreCase)||path.StartsWith(system+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Choose a creative folder outside the installed application and Windows folders.");
  setupHomePath=path;ManagedPost(new{type="project-result",request,path});
 }else if(type=="project-home-create"){
  if(setupHomePath==null)throw new InvalidOperationException("Choose a home folder first.");var folders=m["folders"] as ArrayList;if(folders==null||folders.Count>50)throw new InvalidOperationException("Choose up to 50 folders.");
  string previousPath=managedPath,previousId=managedId;try{managedPath=setupHomePath;string marker=Path.Combine(managedPath,".jago-library","library-id.txt");managedId=File.Exists(marker)?File.ReadAllText(marker).Trim():"r"+Guid.NewGuid().ToString("N").Substring(0,12);
  if(!System.Text.RegularExpressions.Regex.IsMatch(managedId,@"^r[a-f0-9]{12}$"))throw new InvalidDataException("This library's identifier is invalid.");
  foreach(object folder in folders)ManagedFolder(Convert.ToString(folder),false,false);
  foreach(object folder in folders)ManagedFolder(Convert.ToString(folder));
  ManagedFolder(".jago-library",true);if(!File.Exists(marker))File.WriteAllText(marker,managedId);await IndexManaged("library-home");Directory.CreateDirectory(Path.GetDirectoryName(HomePreferencesFile));File.WriteAllText(HomePreferencesFile,managedPath);ManagedPost(new{type="project-result",request,path=managedPath});}catch{managedPath=previousPath;managedId=previousId;throw;}
 }else if(type=="project-home-reveal"){
  if(String.IsNullOrEmpty(managedPath)||!Directory.Exists(managedPath))throw new InvalidOperationException("Your library home is unavailable. Locate it in Manage sources.");System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(managedPath){UseShellExecute=true});ManagedPost(new{type="project-result",request,ok=true});
 }else if(type=="project-home-review"){
  string destination=ManagedFolder(Convert.ToString(m["folder"]),false,false);var ids=m["ids"] as ArrayList;if(ids==null||ids.Count>2000)throw new InvalidOperationException("Select up to 2,000 files per collection.");var rows=new List<object>();long total=0;int missing=0,collisions=0;
  foreach(object value in ids){string key=Convert.ToString(value),file;if(!assetPaths.TryGetValue(key,out file)||!File.Exists(file)){missing++;rows.Add(new{id=key,status="Unavailable",name=key});continue;}if((File.GetAttributes(file)&FileAttributes.ReparsePoint)!=0){missing++;continue;}
   string folder=destination;if(m.ContainsKey("keepFolders")&&Convert.ToBoolean(m["keepFolders"])){string relative=key.Substring(key.IndexOf(':')+1);string parent=Path.GetDirectoryName(relative.Replace('/',Path.DirectorySeparatorChar));if(!String.IsNullOrEmpty(parent))folder=ManagedFolder(String.Join("/",new[]{Convert.ToString(m["folder"]),parent.Replace('\\','/')}.Where(x=>!String.IsNullOrEmpty(x))),false,false);}
   string target=Path.Combine(folder,Path.GetFileName(file));long size=new FileInfo(file).Length;bool same=Path.GetFullPath(file).Equals(Path.GetFullPath(target),StringComparison.OrdinalIgnoreCase);if(!same)total+=size;bool collision=!same&&File.Exists(target);bool identical=collision&&new FileInfo(target).Length==size&&await Task.Run(()=>HomeHash(file)==HomeHash(target));if(collision&&!identical)collisions++;rows.Add(new{id=key,name=Path.GetFileName(file),source=file,destination=target,bytes=size,status=same?"Already in this folder":identical?"Identical file: reuse verified copy":collision?"Existing name: copy gets a numbered name":"Copy and verify"});
  }
  long free=-1;try{free=new DriveInfo(Path.GetPathRoot(managedPath)).AvailableFreeSpace;}catch{}
  ManagedPost(new{type="project-result",request,rows,bytes=total,availableBytes=free,missing,collisions,enoughSpace=free<0||free>total});
 }
 }catch(Exception e){ManagedPost(new{type="project-result",request,error=e.Message});}}
}
