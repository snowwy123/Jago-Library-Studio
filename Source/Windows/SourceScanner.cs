// SPDX-License-Identifier: MIT
using System;using System.Threading;using System.IO;using System.Collections.Generic;using System.Linq;
internal sealed class SourceFile { public string Relative,Path,Stamp;public long Size; }
internal sealed class SourceSnapshot { public readonly List<SourceFile> Files=new List<SourceFile>();public readonly List<string> Folders=new List<string>();public readonly List<string> Warnings=new List<string>();public bool Complete=true,Missing=false; }
internal static class LibrarySourceScanner {
 internal const int FileLimit=60000,FolderLimit=10000,DepthLimit=32;
 internal static SourceSnapshot Scan(string root,IEnumerable<string> only=null,CancellationToken cancellation=default(CancellationToken),Action<int> progress=null){
  var result=new SourceSnapshot();root=System.IO.Path.GetFullPath(root).TrimEnd(System.IO.Path.DirectorySeparatorChar)+System.IO.Path.DirectorySeparatorChar;
  if(!Directory.Exists(root)){result.Missing=true;return result;}
  try{if((File.GetAttributes(root)&FileAttributes.ReparsePoint)!=0)throw new IOException("Linked roots cannot be junctions or symbolic links.");}catch(Exception e){result.Complete=false;result.Warnings.Add(e.Message);return result;}
  Action<string> add=file=>{cancellation.ThrowIfCancellationRequested();try{var info=new FileInfo(file);if(!info.Exists||info.Name.StartsWith("~$")||(info.Attributes&FileAttributes.ReparsePoint)!=0)return;
   if(info.Length>2000000000){result.Complete=false;result.Warnings.Add("Some files exceed the 2 GB indexing limit.");return;}
   string full=Path.GetFullPath(file);if(!full.StartsWith(root,StringComparison.OrdinalIgnoreCase))return;
   result.Files.Add(new SourceFile{Path=full,Relative=full.Substring(root.Length).Replace('\\','/'),Size=info.Length,Stamp=info.LastWriteTimeUtc.Ticks.ToString()+":"+info.Length});if(result.Files.Count%500==0&&progress!=null)progress(result.Files.Count);
  }catch(UnauthorizedAccessException){result.Complete=false;}catch(IOException){result.Complete=false;}};
  if(only!=null){foreach(var rel in only){if(result.Files.Count>=FileLimit){result.Complete=false;break;}try{add(Path.Combine(root,rel.Replace('/',Path.DirectorySeparatorChar)));}catch{result.Complete=false;}}return result;}
  var stack=new Stack<Tuple<string,int>>();stack.Push(Tuple.Create(root,0));
  while(stack.Count>0){cancellation.ThrowIfCancellationRequested();var item=stack.Pop();try{
   foreach(var file in Directory.EnumerateFiles(item.Item1)){if(result.Files.Count>=FileLimit){result.Complete=false;result.Warnings.Add("Showing the first 60,000 files. Link a smaller project folder to see the rest.");return result;}add(file);}
   foreach(var dir in Directory.EnumerateDirectories(item.Item1)){
    if(Path.GetFileName(dir).Equals(".jago-library",StringComparison.OrdinalIgnoreCase)||(File.GetAttributes(dir)&FileAttributes.ReparsePoint)!=0)continue;
    if(result.Folders.Count>=FolderLimit||item.Item2>=DepthLimit){result.Complete=false;continue;}
    result.Folders.Add(dir.Substring(root.Length).Replace('\\','/'));stack.Push(Tuple.Create(dir,item.Item2+1));
   }
  }catch(UnauthorizedAccessException){result.Complete=false;}catch(IOException){result.Complete=false;}}
  if(!result.Complete&&result.Warnings.Count==0)result.Warnings.Add("Some folders were unavailable or beyond the indexing limits. Existing entries have been kept.");
  result.Folders.Sort(StringComparer.OrdinalIgnoreCase);return result;
 }
}
