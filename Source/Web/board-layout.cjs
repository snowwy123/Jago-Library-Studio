// SPDX-License-Identifier: MIT
(function(root,factory){const api=factory();if(typeof module==='object'&&module.exports)module.exports=api;else root.JagoBoardLayout=api;})(typeof globalThis!=='undefined'?globalThis:this,()=>{
 const bounds=a=>a.length?{x:Math.min(...a.map(i=>i.x)),y:Math.min(...a.map(i=>i.y)),w:Math.max(...a.map(i=>i.x+i.w))-Math.min(...a.map(i=>i.x)),h:Math.max(...a.map(i=>i.y+i.h))-Math.min(...a.map(i=>i.y))}:{x:0,y:0,w:0,h:0};
 const overlaps=(a,b,g=0)=>a.x<b.x+b.w+g&&a.x+a.w+g>b.x&&a.y<b.y+b.h+g&&a.y+a.h+g>b.y;
 function arrange(items,options={}){
  const list=items.filter(i=>!i.locked).map(i=>({...i}));if(!list.length)return [];
  const gap=Math.max(0,Math.min(256,Number(options.gap??24))),size=options.size||'keep',shape=options.shape||'square';
  const area=list.reduce((n,i)=>n+i.w*i.h,0)/list.length,aw=list.reduce((n,i)=>n+i.w,0)/list.length,ah=list.reduce((n,i)=>n+i.h,0)/list.length;
  for(const i of list){const ratio=i.w/i.h;if(size==='area'){i.w=Math.sqrt(area*ratio);i.h=i.w/ratio;}else if(size==='width'){i.w=aw;i.h=i.w/ratio;}else if(size==='height'){i.h=ah;i.w=i.h*ratio;}const shrink=Math.min(1,8192/i.w,8192/i.h);i.w*=shrink;i.h*=shrink;}
  let arranged;
  if(shape==='row'||shape==='column'){let cursor=0;const cross=Math.max(...list.map(i=>shape==='row'?i.h:i.w));arranged=list.map(i=>{const next={...i,x:shape==='row'?cursor:(cross-i.w)/2,y:shape==='row'?(cross-i.h)/2:cursor};cursor+=(shape==='row'?i.w:i.h)+gap;return next;});}
  else if(shape==='grid'){const cols=Math.max(1,Math.min(list.length,Math.round(Number(options.columns)||4))),widths=Array(cols).fill(0),heights=[];list.forEach((i,n)=>{widths[n%cols]=Math.max(widths[n%cols],i.w);heights[Math.floor(n/cols)]=Math.max(heights[Math.floor(n/cols)]||0,i.h);});arranged=list.map((i,n)=>{const c=n%cols,r=Math.floor(n/cols);return{...i,x:widths.slice(0,c).reduce((a,v)=>a+v+gap,0)+(widths[c]-i.w)/2,y:heights.slice(0,r).reduce((a,v)=>a+v+gap,0)+(heights[r]-i.h)/2};});}
  else{const target=shape==='landscape'?16/9:shape==='portrait'?9/16:1,total=list.reduce((n,i)=>n+(i.w+gap)*(i.h+gap),0),widest=Math.max(...list.map(i=>i.w)),ideal=Math.sqrt(total*target);let best=Infinity;
   for(let n=0;n<=40;n++){const width=Math.max(widest,ideal*(.45+n*.045));let x=0,y=0,row=0;const candidate=list.map(i=>{if(x&&x+i.w>width){x=0;y+=row+gap;row=0;}const item={...i,x,y};x+=i.w+gap;row=Math.max(row,i.h);return item;});const box=bounds(candidate),empty=1-list.reduce((sum,i)=>sum+i.w*i.h,0)/(box.w*box.h),score=Math.abs(Math.log((box.w/box.h)/target))+.18*empty;if(score<best){best=score;arranged=candidate;}}
  }
  const origin=options.origin||bounds(items),block=bounds(arranged),obstacles=options.obstacles||[];let x=Number(origin.x)||0,y=Number(origin.y)||0;
  if(obstacles.some(i=>overlaps({x,y,w:block.w,h:block.h},i,gap))){const occupied=bounds(obstacles);x=occupied.x+occupied.w+gap;}
  return arranged.map(i=>({...i,x:i.x+x,y:i.y+y}));
 }
 function exportBox(items,options={}){const box=bounds(items);if(!items.length){box.w=1080;box.h=720;}const padding=Number(options.padding??40);let width=options.width==null?null:Number(options.width),height=options.height==null?null:Number(options.height);if(!Number.isFinite(padding)||padding<0||padding>512)throw Error('Choose padding between 0 and 512 pixels.');if(width===null&&height===null){const scale=Math.min(1,4096/(box.w+padding*2),4096/(box.h+padding*2),Math.sqrt(16000000/((box.w+padding*2)*(box.h+padding*2))));width=Math.max(64,Math.ceil((box.w+padding*2)*scale));height=Math.max(64,Math.ceil((box.h+padding*2)*scale));return{width,height,scale,x:(width-box.w*scale)/2-box.x*scale,y:(height-box.h*scale)/2-box.y*scale};}
  if(!Number.isInteger(width)||!Number.isInteger(height)||width<64||height<64||width>8192||height>8192||width*height>32000000)throw Error('Use whole-pixel dimensions from 64 to 8192, up to 32 million pixels.');if(padding*2>=Math.min(width,height))throw Error('Reduce the padding so your references fit inside the image.');const scale=Math.min((width-padding*2)/Math.max(1,box.w),(height-padding*2)/Math.max(1,box.h));return{width,height,scale,x:(width-box.w*scale)/2-box.x*scale,y:(height-box.h*scale)/2-box.y*scale};
 }
 return{arrange,bounds,overlaps,exportBox};
});
