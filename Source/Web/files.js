// SPDX-License-Identifier: MIT
const LibraryFiles=(()=>{
 function save(blob,name){const a=document.createElement('a'),url=URL.createObjectURL(blob);a.href=url;a.download=name;a.click();setTimeout(()=>URL.revokeObjectURL(url),30000);return url}
 const safe=s=>String(s).replace(/[<>:"/\\|?*\x00-\x1f]/g,'').slice(0,80)||'Jago library';

 // Small uncompressed ZIP writer: no external runtime, suitable for artwork and project kits.
 const table=Uint32Array.from({length:256},(_,n)=>{for(let k=0;k<8;k++)n=n&1?0xedb88320^(n>>>1):n>>>1;return n>>>0});function crc(data){let c=0xffffffff;for(const v of data)c=table[(c^v)&255]^(c>>>8);return(c^0xffffffff)>>>0}
 async function zip(files){const chunks=[],central=[];let offset=0;for(const file of files){const name=new TextEncoder().encode(file.name),data=new Uint8Array(await file.blob.arrayBuffer()),head=new Uint8Array(30+name.length),v=new DataView(head.buffer),sum=crc(data);v.setUint32(0,0x04034b50,true);v.setUint16(4,20,true);v.setUint16(6,2048,true);v.setUint32(14,sum,true);v.setUint32(18,data.length,true);v.setUint32(22,data.length,true);v.setUint16(26,name.length,true);head.set(name,30);chunks.push(head,data);const c=new Uint8Array(46+name.length),d=new DataView(c.buffer);d.setUint32(0,0x02014b50,true);d.setUint16(4,20,true);d.setUint16(6,20,true);d.setUint16(8,2048,true);d.setUint32(16,sum,true);d.setUint32(20,data.length,true);d.setUint32(24,data.length,true);d.setUint16(28,name.length,true);d.setUint32(42,offset,true);c.set(name,46);central.push(c);offset+=head.length+data.length}const end=new Uint8Array(22),v=new DataView(end.buffer),length=central.reduce((n,c)=>n+c.length,0);v.setUint32(0,0x06054b50,true);v.setUint16(8,files.length,true);v.setUint16(10,files.length,true);v.setUint32(12,length,true);v.setUint32(16,offset,true);return new Blob([...chunks,...central,end],{type:'application/zip'})}
 return{save,safe,zip};
})();
