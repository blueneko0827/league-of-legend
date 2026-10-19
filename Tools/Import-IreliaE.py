"""Import the supplied official E convergence mesh and textures (not emitter definitions)."""
import hashlib
import io
import json
import re
from pathlib import Path
import shutil
import struct
import sys
import uuid
from PIL import Image

root = Path(__file__).resolve().parents[1]
source = Path(sys.argv[1])
raw = root / 'Assets/SourceModels/IreliaOfficial/E'
output = root / 'Assets/Resources/Models/IreliaOfficial/E'
raw.mkdir(parents=True, exist_ok=True)
output.mkdir(parents=True, exist_ok=True)
for folder in (raw, output):
    meta = Path(str(folder) + '.meta')
    if not meta.exists():
        meta.write_text(f'fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n', encoding='utf-8')
entries = []
for name, decoded in [('irelia_base_e_beam.scb', None),
                      ('irelia_base_e_beam_mult.tex', 'BeamMask.png'),
                      ('irelia_base_e_mis.tex', 'Missile.png'),
                      ('irelia_base_e_blade_stun_trail.tex', 'Trail.png'),
                      ('irelia_base_e_stun_beam.tex', 'StunBeam.png')]:
    data = (source / name).read_bytes()
    shutil.copy2(source / name, raw / name)
    entry = {'file': name, 'sha256': hashlib.sha256(data).hexdigest()}
    if decoded:
        assert data[:4] == b'TEX\0'
        width, height = struct.unpack_from('<HH', data, 4)
        fmt = data[9]
        assert fmt in (10, 12)
        size = ((width + 3) // 4) * ((height + 3) // 4) * (16 if fmt == 12 else 8)
        header = struct.pack('<7I', 124, 0x81007, height, width, size, 0, 0) + bytes(44)
        header += struct.pack('<II4s5I', 32, 4, b'DXT5' if fmt == 12 else b'DXT1', 0, 0, 0, 0, 0)
        header += struct.pack('<5I', 0x1000, 0, 0, 0, 0)
        Image.open(io.BytesIO(b'DDS ' + header + data[-size:])).convert('RGBA').save(output / decoded)
        entry['decoded'] = decoded
        meta = Path(str(output / decoded) + '.meta')
        if not meta.exists():
            template = (output.parent / 'R/WallEnergy.png.meta').read_text(encoding='utf-8-sig')
            meta.write_text(re.sub(r'guid: \w+', 'guid: ' + uuid.uuid4().hex, template, count=1), encoding='utf-8')
    meta = Path(str(raw / name) + '.meta')
    if not meta.exists():
        meta.write_text(f'fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nDefaultImporter:\n  externalObjects: {{}}\n', encoding='utf-8')
    entries.append(entry)
(raw / 'Sources.json').write_text(json.dumps({
    'source': str(source), 'assets': entries,
    'presentation': 'Original SCB beam mesh and TEX textures. Unity reconstructs the convergence, layering and fade synchronized to existing E gameplay timing. The supplied package has no original particle emitter definitions; this is not an exact import of the LoL particle system.',
    'gameplay': 'No changes to damage, collision width, stun, marks, or cast/convergence timing.'
}, indent=2) + '\n', encoding='utf-8')
print('Imported official E convergence mesh and four textures.')
