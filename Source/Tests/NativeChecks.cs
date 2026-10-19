using System;using System.IO;using System.Linq;using System.Threading;using System.Threading.Tasks;
static class NativeChecks {
 static void Require(bool value,string message){if(!value)throw new Exception(message);}
 static int Main(string[] args){try{Run(args).GetAwaiter().GetResult();Console.WriteLine("PASS: native scanner and office rendering");return 0;}catch(Exception e){Console.WriteLine(e);return 1;}}
 static async Task Run(string[] args){string work=args[0],engine=args[1],fixture=Path.Combine(work,"Fixture"),test=Path.Combine(work,"native-checks");Directory.CreateDirectory(test);
  var snapshot=LibrarySourceScanner.Scan(fixture);Require(snapshot.Complete,"scan incomplete");Require(snapshot.Files.Any(f=>f.Relative.EndsWith("Beyond old limit/notes.txt")),"deep subfolder missing");Require(snapshot.Folders.Any(f=>f.EndsWith("Beyond old limit")),"folder tree missing");Require(snapshot.Files.All(f=>f.Stamp!=null),"modification stamp missing");
  var only=LibrarySourceScanner.Scan(fixture,new[]{"Sample.rtf"});Require(only.Files.Count==1&&only.Files[0].Relative=="Sample.rtf","individual file indexing leaked sibling assets");Require(LibrarySourceScanner.Scan(Path.Combine(test,"Missing folder")).Missing,"missing source unreported");
  foreach(var name in new[]{"Creative brief.docx","Long document.docx","Sample.rtf"}){var destination=Path.Combine(test,Path.GetFileNameWithoutExtension(name));byte[] pdf=await OfficeRenderer.Convert(engine,Path.Combine(fixture,name),destination,CancellationToken.None);Require(pdf.Length>1000&&System.Text.Encoding.ASCII.GetString(pdf,0,5)=="%PDF-","invalid PDF");File.WriteAllBytes(Path.Combine(test,Path.GetFileNameWithoutExtension(name)+".pdf"),pdf);Console.WriteLine(name+" -> PDF ("+pdf.Length+" bytes)");}
  bool invalid=false;try{await OfficeRenderer.Convert(engine,Path.Combine(fixture,"banner.png"),Path.Combine(test,"invalid"),CancellationToken.None);}catch(InvalidOperationException){invalid=true;}Require(invalid,"unsupported format accepted");
 }
}
