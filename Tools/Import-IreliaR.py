"""Decode supplied official Irelia R textures for the native Unity VFX bake.

Usage: python Tools/Import-IreliaR.py <ireliaLOL/base/particles>
Requires Pillow. SCB geometry is read by IreliaRVisualBake in Unity.
"""
import hashlib
import io
import json
from pathlib import Path
import shutil
import struct
import sys
from PIL import Image

source = Path(sys.argv[1])
output = Path(__file__).resolve().parents[1] / 'Assets/SourceModels/IreliaOfficial/R'
output.mkdir(parents=True, exist_ok=True)
files = ['irelia_base_r_mis_blades.scb', 'irelia_base_r_eam_line.tex',
         'irelia_base_r_groundlines_mesh_wall.scb']
entries = []
for name in files:
    data = (source / name).read_bytes()
    shutil.copy2(source / name, output / name)
    entry = {'file': name, 'sha256': hashlib.sha256(data).hexdigest()}
    if name.endswith('.tex'):
        if data[:4] != b'TEX\0':
            raise ValueError('Expected TEX texture')
        width, height = struct.unpack_from('<HH', data, 4)
        fmt = data[9]
        if fmt not in (10, 11, 12):
            raise ValueError(f'Unsupported compression: {fmt}')
        size = ((width + 3) // 4) * ((height + 3) // 4) * (16 if fmt == 12 else 8)
        header = struct.pack('<7I', 124, 0x81007, height, width, size, 0, 0) + bytes(44)
        header += struct.pack('<II4s5I', 32, 4, b'DXT5' if fmt == 12 else b'DXT1', 0, 0, 0, 0, 0)
        header += struct.pack('<5I', 0x1000, 0, 0, 0, 0)
        image = Image.open(io.BytesIO(b'DDS ' + header + data[-size:])).convert('RGBA')
        image.save(output / 'WallEnergy.png')
        entry['decoded'] = 'WallEnergy.png'
    entries.append(entry)

# This supplied SCB's first nine vertices are the bottom row of the open wall.
# Keep its authored proportions and offset relative to the struck champion.
# Reflect X as in the character importer; convert gameplay units at 100:1.
wall = (output / 'irelia_base_r_groundlines_mesh_wall.scb').read_bytes()
if wall[:8] != b'r3d2Mesh' or struct.unpack_from('<HH', wall, 8) != (3, 2):
    raise ValueError('Expected the supplied SCB 3.2 wall reference')
if struct.unpack_from('<II', wall, 140) != (55, 80):
    raise ValueError('Unexpected wall topology; review its bottom row before importing')
points = [{'x': -x / 100, 'y': z / 100}
          for x, y, z in struct.iter_unpack('<fff', wall[180:180 + 9 * 12])]
profile = output.parents[2] / 'Resources/LeagueData/IreliaRWall.json'
profile.write_text(json.dumps({'points': points}, indent=2) + '\n', encoding='utf-8')
(output / 'Sources.json').write_text(json.dumps({
    'source': str(source), 'assets': entries,
    'blade_texture': '../Raw/irelia_base_blades_tx_cm.tex (existing Material2)',
    'format_reference': 'https://github.com/LeagueToolkit/LeagueToolkit/tree/main/src/LeagueToolkit/Core',
    'wall_profile': {
        'output': 'Assets/Resources/LeagueData/IreliaRWall.json',
        'source_vertices': list(range(9)), 'units_to_unity': 0.01,
        'closed': False,
        'note': 'Gameplay boundary reconstructed from the bottom row of the supplied official visual mesh, not an independently verified server collision polygon.'
    },
    'presentation': 'Official R blade geometry, open wall proportions and energy texture. Unity reconstructs the planting, glow and spacing; original particle emitter definitions were not supplied.'
}, indent=2) + '\n', encoding='utf-8')
print(f'Imported {len(entries)} official R source assets into {output}')
