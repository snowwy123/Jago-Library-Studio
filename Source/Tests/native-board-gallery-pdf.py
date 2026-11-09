# SPDX-License-Identifier: MIT
from pathlib import Path
import tempfile,subprocess,json,os,shutil
repo=Path(__file__).resolve().parents[2];w=Path(os.environ.get('JAGO_PROJECT_TEST_WORK',tempfile.gettempdir()));run=Path(tempfile.mkdtemp(prefix='native-gallery-pdf-',dir=w));shutil.copy2(repo/'Source/Native-probes/board-gallery-pdf.js',run/'probe.js');si=subprocess.STARTUPINFO();si.dwFlags=subprocess.STARTF_USESHOWWINDOW;si.wShowWindow=0
p=subprocess.run([str(repo/'Jago-Library.exe'),'--test',str(run)],startupinfo=si,env={**os.environ,'JAGO_SUITE_ROOT':str(run/'Suite')},timeout=90,check=True)
result=json.loads((run/'result.json').read_text(encoding='utf-8-sig'));assert result['ok'],result;print(json.dumps({'folder':str(run),'result':result},indent=2))
