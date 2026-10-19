from pathlib import Path
import os,subprocess,time,json
r=Path(__file__).resolve().parents[2];out=Path(os.environ.get('JAGO_PROJECT_TEST_WORK',str(Path(__import__('tempfile').gettempdir()))))/('Board-native-'+time.strftime('%H%M%S'));out.mkdir();env=os.environ.copy();env['JAGO_SUITE_ROOT']=str(out/'Suite');
(out/'probe.js').write_text('''(async()=>{try{let wait=async fn=>{for(let i=0;i<250;i++){if(fn())return;await new Promise(r=>setTimeout(r,100));}throw Error('Timed out');};await wait(()=>window.JagoBoard?.active);if(!document.body.classList.contains('boardOnly'))throw Error('Focused route missing');referenceAddSimple('note');if(!await JagoBoard.flush())throw Error('Native recovery failed');$('boardQuickTop').checked=true;$('boardQuickTop').dispatchEvent(new Event('change'));await wait(()=>$('referencePin').checked);await wait(()=>$('referenceName'));if(referenceCurrent().items.length!==1)throw Error('New board shortcut replaced the existing board');$('dialog').close();window.testResult={ok:true,checks:['Launcher reuses the running native Library','Focused board entry','Native recovery write','Native always-on-top control','New board task opens the naming dialog without losing current work']};}catch(e){window.testResult={ok:false,error:e.stack};}})();''',encoding='utf-8')
si=subprocess.STARTUPINFO();si.dwFlags=subprocess.STARTF_USESHOWWINDOW;si.wShowWindow=0
p=subprocess.Popen([str(r/'Jago-Library.exe'),'--test',str(out)],env=env,startupinfo=si)
try:
 time.sleep(2);subprocess.run([str(r/'Jago-Board.exe'),'--no-shell'],env=env,timeout=8);time.sleep(3);subprocess.run([str(r/'Jago-Board.exe'),'--new','--no-shell'],env=env,timeout=8);p.wait(timeout=45);result=json.loads((out/'result.json').read_text(encoding='utf-8-sig'));assert result['ok'],result;print(json.dumps({'folder':str(out),'result':result},indent=2))
finally:
 if p.poll() is None:p.terminate()
