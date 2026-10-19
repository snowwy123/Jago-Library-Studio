// SPDX-License-Identifier: MIT
using System;using System.IO;using System.Drawing;using System.Collections.Generic;using System.Threading.Tasks;
sealed partial class WorldWindow {
 async Task HandleImageText(Dictionary<string,object> message){string request=Convert.ToString(message["request"]);try{string data=Convert.ToString(message["data"]);if(data.Length>22000000)throw new Exception("Choose an image under 16 MB for text recognition.");byte[] bytes=Convert.FromBase64String(data);using(var stream=new MemoryStream(bytes))using(var image=new Bitmap(stream)){if((long)image.Width*image.Height>40000000)throw new Exception("Choose an image below 40 million pixels.");var result=await OfflineImageText.Recognise(image);ManagedPost(new{type="image-text-result",request=request,ok=true,result=result});}}catch(Exception error){ManagedPost(new{type="image-text-result",request=request,ok=false,error=error.Message});}}
}
