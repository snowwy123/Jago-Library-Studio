// SPDX-License-Identifier: MIT
using System;
using System.IO;
using System.Web.Script.Serialization;
using System.Windows.Media;
using System.Windows.Media.Imaging;

// One image per process. The parent supplies time/memory limits before "go".
static class ImagePreviewWorker {
 [STAThread] static int Main(string[] args) {
  try {
   if(args.Length!=3 || Console.ReadLine()!="go")return 2;
   int edge;
   if(!Int32.TryParse(args[2],out edge)||edge<320||edge>4096)throw new IOException("Invalid preview size.");
   using(var stream=new FileStream(args[0],FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)) {
    if(stream.Length>536870912)throw new IOException("This image exceeds the 512 MB source limit.");
    var decoder=BitmapDecoder.Create(stream,BitmapCreateOptions.DelayCreation,BitmapCacheOption.None);
    var frame=decoder.Frames[0];int width=frame.PixelWidth,height=frame.PixelHeight;
    if(width<1||height<1||(long)width*height>256000000)throw new IOException("This image exceeds the 256 megapixel preview limit.");
    int orientation=1;try{var metadata=frame.Metadata as BitmapMetadata;var value=metadata==null?null:metadata.GetQuery("/app1/ifd/{ushort=274}");if(value!=null)orientation=Convert.ToInt32(value);}catch{}
    stream.Position=0;
    var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.StreamSource=stream;
    if(Math.Max(width,height)>edge){if(width>=height)image.DecodePixelWidth=edge;else image.DecodePixelHeight=edge;}
    image.EndInit();image.Freeze();
    BitmapSource result=image;
    if(orientation>=2&&orientation<=8){
     var transforms=new TransformGroup();
     if(orientation==3)transforms.Children.Add(new RotateTransform(180));
     if(orientation==5||orientation==6)transforms.Children.Add(new RotateTransform(90));
     if(orientation==7||orientation==8)transforms.Children.Add(new RotateTransform(270));
     if(orientation==2||orientation==5||orientation==7)transforms.Children.Add(new ScaleTransform(-1,1));
     if(orientation==4)transforms.Children.Add(new ScaleTransform(1,-1));
     result=new TransformedBitmap(image,transforms);result.Freeze();
     if(orientation>=5){int old=width;width=height;height=old;}
    }
    var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(result));
    using(var output=File.Create(args[1]))encoder.Save(output);
    Console.Write(new JavaScriptSerializer().Serialize(new{width=result.PixelWidth,height=result.PixelHeight,sourceWidth=width,sourceHeight=height}));
   }
   return 0;
  }catch(Exception error){Console.Error.Write(error is OutOfMemoryException?"This image could not be previewed within the memory budget.":error is FileFormatException||error is NotSupportedException?"This image could not be decoded. Try exporting a fresh PNG or JPEG.":error.Message);return 1;}
 }
}
