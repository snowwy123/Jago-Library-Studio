from pathlib import Path
import subprocess,os,time,json
r=Path(__file__).resolve().parents[2];out=Path(os.environ.get('JAGO_PROJECT_TEST_WORK',str(Path(__import__('tempfile').gettempdir()))))/('Board-cold-'+time.strftime('%H%M%S'));out.mkdir();env=os.environ.copy();env['JAGO_SUITE_ROOT']=str(out/'Suite');si=subprocess.STARTUPINFO();si.dwFlags=subprocess.STARTF_USESHOWWINDOW;si.wShowWindow=0
results=[]
for phase in ['empty','restore']:
 code="if(referenceCurrent().items.length!==0)throw Error('First board is not empty');referenceAddSimple('note');referenceCurrent().items[0].text='Cold saved';mark();if(!await JagoBoard.flush())throw Error('Save failed');" if phase=='empty' else "if(referenceCurrent().items[0]?.text!=='Cold saved')throw Error('Cold recovery lost the note');"
 (out/'probe.js').write_text("(async()=>{try{for(let i=0;i<200&&!window.JagoBoard?.active;i++)await new Promise(r=>setTimeout(r,100));if(!JagoBoard.active)throw Error('Board not active');"+code+"window.suiteTestResult={ok:true,phase:'"+phase+"'};}catch(e){window.suiteTestResult={ok:false,error:e.stack};}})();",encoding='utf-8')
 p=subprocess.run([str(r/'Jago-Library.exe'),'--board','--suite-test',str(out)],env=env,startupinfo=si,timeout=45);assert p.returncode==0;d=json.loads((out/'result.json').read_text(encoding='utf-8-sig'));assert d['ok'],d;results.append(d);time.sleep(.5)
(out/'cold-results.json').write_text(json.dumps(results,indent=2));print(json.dumps({'folder':str(out),'results':results}))
