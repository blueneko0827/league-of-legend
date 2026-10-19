"""Decode the supplied R missile direction textures for Unity's hold-to-aim preview.

The package does not contain the complete LoL targeting UI. Unity reconstructs the
arrow outline and uses these original textures as its trail and tip.
"""
import hashlib
import io
import json
from pathlib import Path
import shutil
import struct
import sys
import uuid
from PIL import Image

root = Path(__file__).resolve().parents[1]
source = Path(sys.argv[1])
raw = root / 'Assets/SourceModels/IreliaOfficial/R'
output = root / 'Assets/Resources/Models/IreliaOfficial/R'
entries = []
for filename, decoded in [('irelia_base_r_mis_glow_lines.tex', 'AimTrail.png'),
                          ('irelia_base_r_mis_glow.tex', 'AimTip.png')]:
    data = (source / filename).read_bytes()
    assert data[:4] == b'TEX\0'
    width, height = struct.unpack_from('<HH', data, 4)
    assert data[9] == 12, 'Expected the supplied DXT5 R texture'
    size = ((width + 3) // 4) * ((height + 3) // 4) * 16
    header = struct.pack('<7I', 124, 0x81007, height, width, size, 0, 0) + bytes(44)
    header += struct.pack('<II4s5I', 32, 4, b'DXT5', 0, 0, 0, 0, 0)
    header += struct.pack('<5I', 0x1000, 0, 0, 0, 0)
    image = Image.open(io.BytesIO(b'DDS ' + header + data[-size:])).convert('RGBA')
    shutil.copy2(source / filename, raw / filename)
    image.save(output / decoded)
    for asset in [raw / filename, output / decoded]:
        meta = Path(str(asset) + '.meta')
        if meta.exists(): continue
        guid = uuid.uuid4().hex
        if asset.suffix == '.png':
            template = (output / 'WallEnergy.png.meta').read_text(encoding='utf-8-sig')
            import re
            meta.write_text(re.sub(r'guid: [a-f0-9]+', 'guid: ' + guid, template, count=1), encoding='utf-8')
        else:
            meta.write_text(f'fileFormatVersion: 2\nguid: {guid}\n', encoding='utf-8')
    entries.append({'file': filename, 'sha256': hashlib.sha256(data).hexdigest(), 'decoded': decoded})
manifest = raw / 'Sources.json'
info = json.loads(manifest.read_text(encoding='utf-8-sig'))
info['aim_preview'] = {'source': str(source), 'assets': entries,
    'presentation': 'Reconstructed ground arrow with original R missile trail/tip textures; not the complete original targeting UI.'}
manifest.write_text(json.dumps(info, indent=2) + '\n', encoding='utf-8')
print('Imported original R direction textures: AimTrail.png and AimTip.png')
