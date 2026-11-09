from pathlib import Path
import subprocess,shutil,time,json,hashlib,struct,zlib,os,tempfile
import numpy as np
from PIL import Image,ImageOps
repo=Path(__file__).resolve().parents[2];run=Path(tempfile.mkdtemp(prefix='jago-image-previews-',dir=os.environ.get('JAGO_IMAGE_TEST_WORK')));fixture=run/'Fixture';fixture.mkdir(parents=True)
rng=np.random.default_rng(12345);pixels=rng.integers(0,256,(6400,8000,3),dtype=np.uint8);Image.fromarray(pixels).save(fixture/'large.png',compress_level=0);del pixels
alpha=Image.new('RGBA',(512,256),(0,0,0,0));alpha.paste((40,180,90,128),(128,64,384,192));alpha.save(fixture/'alpha.png')
raw=Image.new('RGB',(80,40),'red');raw.paste('blue',(40,0,80,40));exif=raw.getexif();exif[274]=6;raw.save(fixture/'rotated.jpg',quality=95,exif=exif)
small=fixture/'alpha.png';data=bytearray(small.read_bytes());data[16:24]=struct.pack('>II',20000,20000);data[29:33]=struct.pack('>I',zlib.crc32(data[12:29]));(fixture/'excessive.png').write_bytes(data)
hashes={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in fixture.iterdir()}
probe=repo/'Source/Native-probes/board-smart-previews.js';shutil.copy2(probe,run/'probe.js')
si=subprocess.STARTUPINFO();si.dwFlags=subprocess.STARTF_USESHOWWINDOW;si.wShowWindow=0
p=subprocess.Popen([os.environ.get('JAGO_NATIVE_APP',str(repo/'Jago-Library.exe')),'--test',str(run)],startupinfo=si)
deleted=False;deadline=time.monotonic()+150
while p.poll() is None and time.monotonic()<deadline:
 marker=run/'cache-ready.txt'
 if marker.exists() and not deleted:
  import re
  name=marker.read_text().strip().rsplit('/',1)[-1]
  assert re.fullmatch('[0-9a-f]{64}\\.png',name)
  target=run/'Image-previews'/name
  assert target.resolve().parent==(run/'Image-previews').resolve()
  target.unlink();deleted=True
 time.sleep(.1)
if p.poll() is None:p.kill();raise RuntimeError('Native probe timed out')
assert deleted,'Cache eviction fixture not reached'
print(run);print((run/'result.txt').read_text());result=json.loads((run/'result.json').read_text(encoding='utf-8'));print(json.dumps(result));assert result['ok']
assert hashes=={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in fixture.iterdir()},'Originals changed'
for meta in (run/'Image-previews').glob('*.json'):
 info=json.loads(meta.read_text());preview=Image.open(meta.with_suffix('.png'));assert max(preview.size)<=4096
 if info['sourceWidth']==512:
  assert preview.convert('RGBA').getpixel((0,0))[3]==0
  assert abs(preview.convert('RGBA').getpixel((256,128))[3]-128)<=1
 if info['sourceWidth']==40 and info['sourceHeight']==80:
  expected=ImageOps.exif_transpose(Image.open(fixture/'rotated.jpg')).convert('RGB');assert preview.size==expected.size
  assert max(abs(a-b) for a,b in zip(preview.convert('RGB').getpixel((10,10)),expected.getpixel((10,10))))<3
assert sum(p.stat().st_size for p in (run/'Image-previews').glob('*.png'))<=268435456
print('Independent checks: unchanged source SHA-256, bounded image sizes, transparent alpha, EXIF orientation and cache size passed.')
