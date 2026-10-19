from pathlib import Path
import subprocess,time,json,shutil,os
from pptx import Presentation
from pptx.util import Inches
from openpyxl import Workbook
repo=Path(__file__).resolve().parents[2];work=Path(os.environ.get('JAGO_TEST_WORK',str(Path(__file__).parent/'Run')));fixture=work/'Fixture'
slides=Presentation()
for title,body in [('Creative project','Slide previews preserve positioning and artwork.'),('Production plan','Two slides should render as two PDF pages.')]:
    slide=slides.slides.add_slide(slides.slide_layouts[1]);slide.shapes.title.text=title;slide.placeholders[1].text=body
slides.slides[0].shapes.add_picture(str(fixture/'banner.png'), Inches(1),Inches(3),width=Inches(6))
slides.save(fixture/'Project slides.pptx')
book=Workbook();sheet=book.active;sheet.title='Budget';sheet.append(['Deliverable','Cost']);sheet.append(['Poster',240]);sheet.append(['Animation',360]);sheet.append(['Total','=SUM(B2:B3)']);sheet.column_dimensions['A'].width=35;sheet.column_dimensions['B'].width=15;sheet.print_options.horizontalCentered=True;sheet.print_area='A1:B4';book.save(fixture/'Project budget.xlsx')
print('Slides and spreadsheet fixtures added.',flush=True)
test=work/('native-ui-'+time.strftime('%H%M%S'));test.mkdir();shutil.copytree(fixture,test/'Fixture');engine_path=Path(os.environ.get('JAGO_OFFICE_ENGINE',str(repo/'Tools/Documents/LibreOffice/program/soffice.exe')));(test/'document-engine.txt').write_text(str(engine_path),encoding='utf-8')
(test/'probe.js').write_text(r'''
(async()=>{const checks=[],sleep=ms=>new Promise(r=>setTimeout(r,ms));
const wait=async(fn,why,ms=25000)=>{const until=Date.now()+ms;while(Date.now()<until){if(fn())return;await sleep(100);}throw Error(why);};
const assert=(ok,why)=>{if(!ok)throw Error(why);checks.push(why);};
const signal=name=>LibraryFiles.save(new Blob(['ready']),name+'.txt');
try{
 const errors=[];window.addEventListener('error',e=>errors.push(e.message));
 chrome.webview.postMessage({type:'test-folder'});await wait(()=>state.assets.some(a=>a.name==='Creative brief.docx'),'initial source index');
 const source=state.roots.find(r=>r.path?.endsWith('Fixture'));assert(source&&source.watching,'live watcher active');assert(state.assets.some(a=>a.relativePath?.includes('Beyond old limit/notes.txt')),'deep linked subfolders indexed');
 rootFilter=source.id;sourceSubfolder='Project/Artwork';renderLibrary();assert(filtered().some(a=>a.name==='notes.txt'),'nested folder filter includes descendants');sourceSubfolder='';
 signal('phase-create');await wait(()=>state.assets.some(a=>a.name==='live.txt'),'new file failed to appear');let live=state.assets.find(a=>a.name==='live.txt');live.favourite=true;live.tags=['retain'];createCollection('Live project');state.collections[0].assets=[live.id];const before=live.stamp;
 signal('phase-edit');await wait(()=>state.assets.find(a=>a.name==='live.txt')?.stamp!==before,'modified file stamp not updated');assert(live.favourite,'favourite kept on edit');
 signal('phase-rename');await wait(()=>state.assets.some(a=>a.name==='renamed.txt'),'rename not reconciled');live=state.assets.find(a=>a.name==='renamed.txt');assert(live.favourite&&live.tags[0]==='retain','favourite and tags preserved on rename');assert(state.collections[0].assets.includes(live.id),'project membership follows rename');
 signal('phase-delete');await wait(()=>live.missing,'deletion not flagged');assert(state.assets.includes(live),'deleted reference retained for recovery');
 sourceForget(source,true);assert(!state.assets.some(a=>a.name==='renamed.txt'),'missing entry can be cleared');
 const originalBrief=state.assets.find(a=>a.name==='Creative brief.docx');originalBrief.favourite=true;
 signal('phase-relocate');await sleep(1000);chrome.webview.postMessage({type:'source-refresh',id:source.id});await wait(()=>source.status==='missing','moved source should be unavailable');assert(state.assets.filter(a=>a.rootId===source.id).every(a=>a.missing),'unavailable folder preserves recoverable entries');
 const restored=JSON.parse(JSON.stringify(state));restored.roots.find(r=>r.id===source.id).path=source.path.replace(/Fixture$/,'Fixture Moved');await JagoLibrary.loadProject(new File([JSON.stringify(restored)],'restored.jagolibrary'));await wait(()=>state.roots.find(r=>r.id===source.id)?.status==='ready'&&!state.assets.find(a=>a.name==='Creative brief.docx')?.missing,'source relocation not restored');assert(state.assets.find(a=>a.name==='Creative brief.docx').favourite,'project reload and moved folder retain favourite');
 currentCollection='';rootFilter=source.id;tagFilter='retain';$('search').value='absent';renderLibrary();document.querySelector('[data-type=pdf]').dispatchEvent(new MouseEvent('dblclick',{bubbles:true}));assert(type==='documents'&&rootFilter===''&&tagFilter===''&&$('search').value==='','category double-click clears narrowing filters');
 await wait(()=>documentEngineAvailable,'document renderer unavailable');const brief=state.assets.find(a=>a.name==='Creative brief.docx');const pdf=await renderedDocument(brief);const {renderer}=await pdfReady();const task=renderer.getDocument({data:pdf.slice(),isEvalSupported:false});const doc=await task.promise;assert(doc.numPages===2,'Word document renders two pages');const text=(await(await doc.getPage(1)).getTextContent()).items.map(t=>t.str).join(' ');assert(text.includes('Deliverables')&&text.includes('Budget'),'export PDF has selectable table text');await task.destroy();
 for(const [name,minimum]of [['Project slides.pptx',2],['Project budget.xlsx',1],['Long document.docx',3],['Sample.rtf',1]]){const bytes=await renderedDocument(state.assets.find(a=>a.name===name));const t=renderer.getDocument({data:bytes.slice(),isEvalSupported:false});const d=await t.promise;assert(d.numPages>=minimum,name+' renders expected pages');await t.destroy();}
 const second=await selectDocumentPDF(pdf,'2');const t=renderer.getDocument({data:second.slice(),isEvalSupported:false});assert((await t.promise).numPages===1,'page-range export selects requested page');await t.destroy();LibraryFiles.save(new Blob([second],{type:'application/pdf'}),'Range-test.pdf');
 selected.clear();selected.add(brief.id);selected.add(state.assets.find(a=>a.name==='Sample.rtf').id);await batchDocumentPDF();await $('documentsBegin').onclick();assert($('documentsProgress').textContent.includes('2 PDFs'),'batch documents produce separate PDFs');
 await previewOfficeDocument(brief);await wait(()=>$('pdfReaderStatus')?.textContent.includes('Page'),'rendered preview not ready');assert(!!document.querySelector('.pdfReader08 .primary'),'document PDF export accessible in reader');documentPageArtwork(brief,pdf,0);await $('docArtLibrary').onclick();assert(state.assets.find(a=>a.id===focus)?.type==='image','document page becomes Library artwork');assert(!errors.length,'native UI has no script errors');window.testResult={ok:true,checks,errors};
}catch(e){window.testResult={ok:false,error:e.message,stack:e.stack,checks};}})();
''',encoding='utf-8')
subprocess.run(['powershell','-NoProfile','-ExecutionPolicy','Bypass','-File',str(repo/'Build.ps1')],check=True)
si=subprocess.STARTUPINFO();si.dwFlags=subprocess.STARTF_USESHOWWINDOW;si.wShowWindow=0
proc=subprocess.Popen([str(repo/'Jago-Library.exe'),'--test',str(test)],startupinfo=si)
steps=[('phase-create',lambda:(test/'Fixture/live.txt').write_text('First live file',encoding='utf-8')),('phase-edit',lambda:(test/'Fixture/live.txt').write_text('Edited live file with a longer body',encoding='utf-8')),('phase-rename',lambda:(test/'Fixture/live.txt').rename(test/'Fixture/renamed.txt')),('phase-delete',lambda:(test/'Fixture/renamed.txt').unlink()),('phase-relocate',lambda:(test/'Fixture').rename(test/'Fixture Moved'))]
for phase,action in steps:
    deadline=time.monotonic()+50
    while not (test/(phase+'.txt')).exists() and proc.poll() is None and time.monotonic()<deadline:time.sleep(.15)
    if not(test/(phase+'.txt')).exists():break
    action();print(phase+' complete',flush=True)
try:proc.wait(timeout=90)
except subprocess.TimeoutExpired:proc.kill();print('Native UI probe exceeded timeout',flush=True)
print('Results folder:',test,flush=True)
if(test/'result.txt').exists():print((test/'result.txt').read_text(),flush=True)
if(test/'result.json').exists():print((test/'result.json').read_text(),flush=True)
(work/'latest-native-test.txt').write_text(str(test),encoding='utf-8')
if not(test/'result.txt').exists() or not(test/'result.txt').read_text().startswith('PASS'):raise SystemExit(1)
