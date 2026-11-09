# SPDX-License-Identifier: MIT
from pathlib import Path
import os, tempfile, subprocess, shutil, json, base64, hashlib
from PIL import Image, ImageDraw
repo=Path(__file__).resolve().parents[2]
run=Path(tempfile.mkdtemp(prefix='jago-library-workflows-',dir=os.environ.get('JAGO_PROJECT_TEST_WORK')))
fixture=run/'Fixture';fixture.mkdir()
im=Image.new('RGB',(640,360),'#20383d');draw=ImageDraw.Draw(im);draw.ellipse((180,40,460,320),fill='#d4aa6c');im.save(fixture/'Reference.png');im.thumbnail((384,384));im.save(fixture/'Cover.png')
(fixture/'Project.jagodither').write_text(json.dumps({'_jagoPreview':{'version':1,'png':base64.b64encode((fixture/'Cover.png').read_bytes()).decode(),'width':im.width,'height':im.height},'name':'Original cover fixture','version':1}),encoding='utf-8')
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
hashes={p.name:sha(p) for p in fixture.iterdir()}
shutil.copy2(repo/'Source/Native-probes/library-workflows.js',run/'probe.js')
env=os.environ.copy();env['JAGO_SUITE_ROOT']=str(run/'Suite');si=subprocess.STARTUPINFO();si.dwFlags=subprocess.STARTF_USESHOWWINDOW;si.wShowWindow=0
proc=subprocess.Popen([str(repo/'Jago-Library.exe'),'--test',str(run)],env=env,startupinfo=si)
try:
 proc.wait(timeout=90);result=json.loads((run/'result.json').read_text(encoding='utf-8-sig'));assert result['ok'],result
 with Image.open(run/'Reference board.png') as output:assert output.size==(512,512)
 assert hashes=={p.name:sha(p) for p in fixture.iterdir()}
 assert sha(repo/'index.html')==sha(next((run/'Application').glob('*/index.html')))
 result.update(squarePngDecoded=True,originalFilesUnchanged=True,nativeHtmlMatches=True);(run/'verification.json').write_text(json.dumps(result,indent=2),encoding='utf-8');print(json.dumps({'folder':str(run),'result':result},indent=2))
finally:
 if proc.poll() is None:proc.terminate()
