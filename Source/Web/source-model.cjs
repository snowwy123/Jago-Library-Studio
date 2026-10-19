// SPDX-License-Identifier: MIT
// Shared by the application and its reconciliation regression tests.
(function(root,factory){if(typeof module==='object'&&module.exports)module.exports=factory();else root.JagoSourceModel=factory();})(typeof globalThis==='object'?globalThis:this,()=>{
 const under=(path,parent)=>path===parent||path.startsWith(parent+'/');
 function remap(path,renames){for(const move of renames||[])if(under(path,move.from))path=move.to+path.slice(move.from.length);return path;}
 function reconcile(state,snapshot){
  const rid=snapshot.root.id;let root=state.roots.find(r=>r.id===rid);if(!root){root={...snapshot.root};state.roots.push(root);}else Object.assign(root,snapshot.root);
  const moved=new Map(),changed=new Set(),existing=new Map(),all=new Map(state.assets.map(a=>[a.id,a]));
  root.excluded=(root.excluded||[]).map(p=>remap(p,snapshot.renames));
  for(const a of state.assets){if(a.rootId!==rid)continue;const next=remap(a.relativePath||a.id.slice(rid.length+1),snapshot.renames);if(next!==a.relativePath){const before=a.id;a.relativePath=next;a.id=rid+':'+next;moved.set(before,a.id);}existing.set(a.id,a);}
  const seen=new Set();for(const incoming of snapshot.assets){if((root.excluded||[]).includes(incoming.relativePath))continue;seen.add(incoming.id);const old=existing.get(incoming.id);if(!old&&all.has(incoming.id))continue;if(old){if(old.stamp!==incoming.stamp||old.url!==incoming.url||old.preview!==incoming.preview&&!old.manualPreview||old.missing){changed.add(old.id);delete old.documentPreview;delete old.previewMethod;if(!old.manualPreview)delete old.preview;}const attached=old.manualPreview?old.preview:null;Object.assign(old,incoming,{missing:false});if(typeof old.displayName==='string')old.name=old.displayName;if(attached)old.preview=attached;}else{const a={...incoming,tags:[],added:Date.now(),missing:false};state.assets.push(a);existing.set(a.id,a);changed.add(a.id);}}
  if(snapshot.complete||snapshot.missing)for(const a of existing.values())if(!seen.has(a.id)&&!a.missing){a.missing=true;changed.add(a.id);}
  if(moved.size){for(const pair of state.fontPairings||[])for(const f of pair.fonts||[])f.id=moved.get(f.id)||f.id;for(const track of state.suiteAudio||[])if(track.assetId)track.assetId=moved.get(track.assetId)||track.assetId;if(state.projectHome){state.projectHome.pins=(state.projectHome.pins||[]).map(k=>moved.get(k)||k);state.projectHome.cover=moved.get(state.projectHome.cover)||state.projectHome.cover;}for(const c of state.collections||[])c.assets=(c.assets||[]).map(k=>moved.get(k)||k);state.shortlist=(state.shortlist||[]).map(k=>moved.get(k)||k);for(const layout of [state.layout,...(state.boards||[]).map(b=>b.layout)])for(const layer of layout?.layers||[])for(const k of ['asset','assetId','fontAsset'])if(moved.has(layer[k]))layer[k]=moved.get(layer[k]);}
  for(const view of state.savedViews||[]){const filters=view.filters||view;if(filters.root===rid)filters.subfolder=remap(filters.subfolder||'',snapshot.renames);}
  return {root,moved,changed};
 }
 return {under,remap,reconcile};
});
