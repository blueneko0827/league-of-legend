"""Import the official Irelia particle definitions used by basic attacks, passive, Q, W and E.

Usage: python Tools/Import-IreliaVfx.py [CharacterAssets/Irelia/Base/Original]
Requires Pillow. Everything else is read here with the Python standard library.

Reads Riot's skin VFX bins (VfxSystemDefinitionData), the animation graph particle
events, the TEX textures and SCB meshes they reference, and writes:
  Assets/Resources/Vfx/Irelia/IreliaVfx.json   emitter data + meshes + animation events
  Assets/Resources/Vfx/Irelia/Textures/*.png   decoded textures (largest mip)
  Assets/Resources/Models/IreliaOfficial/BladesPassive4.png   4-stack blades texture
Unity plays the JSON with RiotVfx*.cs. GUIDs are derived from the file names, so a
re-import keeps every reference.
"""
import hashlib
import io
import json
import re
import struct
import sys
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / 'CharacterAssets/Irelia/Base/Original'
OUTPUT = ROOT / 'Assets/Resources/Vfx/Irelia'
TEXTURES = OUTPUT / 'Textures'
SYSTEMS = re.compile(r'^Irelia_Base_(BA|P|Q|W|E|R)_')
# Screen-space refraction needs a grab pass the project does not have; skipped, not faked.
SKIPPED_EMITTER_KEYS = ('distortionDefinition',)


def fnv1a(text):
    h = 0x811C9DC5
    for byte in text.lower().encode('ascii'):
        h = ((h ^ byte) * 0x01000193) & 0xFFFFFFFF
    return h


def xxh64(data, seed=0):
    p1, p2, p3, p4, p5 = (0x9E3779B185EBCA87, 0xC2B2AE3D27D4EB4F, 0x165667B19E3779F9,
                          0x85EBCA77C2B2AE63, 0x27D4EB2F165667C5)
    mask = (1 << 64) - 1
    rotl = lambda x, r: ((x << r) | (x >> (64 - r))) & mask
    def rnd(acc, value):
        return (rotl((acc + value * p2) & mask, 31) * p1) & mask
    def merge(acc, value):
        return (((acc ^ rnd(0, value)) * p1) + p4) & mask
    n, i = len(data), 0
    if n >= 32:
        v = [(seed + p1 + p2) & mask, (seed + p2) & mask, seed, (seed - p1) & mask]
        while i + 32 <= n:
            for k in range(4):
                v[k] = rnd(v[k], struct.unpack_from('<Q', data, i + 8 * k)[0])
            i += 32
        h = (rotl(v[0], 1) + rotl(v[1], 7) + rotl(v[2], 12) + rotl(v[3], 18)) & mask
        for k in range(4):
            h = merge(h, v[k])
    else:
        h = (seed + p5) & mask
    h = (h + n) & mask
    while i + 8 <= n:
        h = (rotl(h ^ rnd(0, struct.unpack_from('<Q', data, i)[0]), 27) * p1 + p4) & mask
        i += 8
    if i + 4 <= n:
        h = (rotl(h ^ (struct.unpack_from('<I', data, i)[0] * p1 & mask), 23) * p2 + p3) & mask
        i += 4
    while i < n:
        h = (rotl(h ^ (data[i] * p5 & mask), 11) * p1) & mask
        i += 1
    h ^= h >> 33; h = (h * p2) & mask; h ^= h >> 29; h = (h * p3) & mask; h ^= h >> 32
    return h


# Field and class names used by the emitters imported here (hashed with FNV-1a).
NAMES = '''VfxSystemDefinitionData complexEmitterDefinitionData simpleEmitterDefinitionData particleName
VfxEmitterDefinitionData emitterName rate lifetime particleLifetime particleLinger timeBeforeFirstEmission
isSingleParticle bindWeight importance texture blendMode pass alphaRef miscRenderFlags disableBackfaceCull
birthColor Color birthScale0 scale0 birthRotation0 rotation0 birthRotationalVelocity0 birthVelocity velocity
birthDrag drag worldAcceleration birthOrbitalVelocity EmitterPosition SpawnShape FlexShapeDefinition
isUniformScale isRotationEnabled isLocalOrientation particleIsLocalOrientation isDirectionOriented
directionVelocityScale directionVelocityMinScale isGroundLayer isRandomStartFrame numFrames texDiv frameRate
startFrame uvMode particleUVScrollRate birthUvScrollRate birthUVOffset uvRotation particleColorTexture
alphaErosionDefinition erosionDriveCurve erosionFeatherIn erosionFeatherOut erosionSliceWidth erosionMapName
erosionMapAddressMode erosionMapChannelMixer distortionDefinition textureMult paletteDefinition
primitive VfxPrimitiveMesh VfxPrimitiveArbitraryQuad VfxPrimitiveCameraUnbound VfxPrimitiveArbitraryTrail
VfxPrimitiveCameraTrail VfxPrimitiveAttachedMesh VfxPrimitiveBeam VfxPrimitiveCameraQuad VfxPrimitiveRay
VfxPrimitivePlanarProjection mMesh VfxMeshDefinitionData mSimpleMeshName mLockMeshToAttachment mTrail
VfxTrailDefinitionData mBirthTilingSize mMode mCutoff mSmoothingMode mMaxAddedPerFrame
ValueFloat ValueVector2 ValueVector3 ValueColor IntegratedValueVector2 IntegratedValueVector3
constantValue dynamics times values probabilityTables VfxAnimatedFloatVariableData
VfxAnimatedVector2fVariableData VfxAnimatedVector3fVariableData VfxAnimatedColorVariableData
VfxProbabilityTableData keyTimes keyValues singleValue VfxShapeBox VfxShapeSphere VfxShapeLegacy
VfxShapeCylinder emitOffset emitRotationAngles emitRotationAxes Size radius height flags
VfxFlexShapeDefinitionData scaleBirthScaleByBoundObjectSize scaleEmitOffsetByBoundObjectSize
VfxAlphaErosionDefinitionData soundOnCreateDefault soundPersistentDefault
AnimationGraphData mClipDataMap AtomicClipData mEventDataMap ParticleEventData mEffectKey mStartFrame
mEndFrame mParticleEventDataPairList ParticleEventDataPair mBoneName mTargetBoneName mIsKillEvent
mScalePlaySpeedWithAnimation mAnimationResourceData AnimationResourceData mAnimationFilePath
ResourceResolver resourceMap VfxTextureMultDefinitionData birthUvScrollRateMult birthUVOffsetMult texDivMult
uvScrollClampMult childParticleSetDefinition VfxChildParticleSetDefinitionData childrenIdentifiers
VfxChildIdentifier effectKey'''.split()
NAME = {fnv1a(n): n for n in NAMES}
BONES = {fnv1a(n): n for n in ('C_BUFFBONE_GLB_LAYOUT_LOC', 'BUFFBONE_GLB_GROUND_LOC', 'C_BUFFBONE_GLB_CENTER_LOC',
                                 'C_BUFFBONE_GLB_CHEST_LOC', 'C_BUFFBONE_GLB_GROUND_LOC', 'C_BUFFBONE_GLB_OVERHEAD_LOC',
                                 'root')}


class Reader:
    def __init__(self, data):
        self.d, self.i = data, 0
    def take(self, fmt):
        values = struct.unpack_from('<' + fmt, self.d, self.i)
        self.i += struct.calcsize('<' + fmt)
        return values if len(values) > 1 else values[0]
    def text(self):
        n = self.take('H'); s = self.d[self.i:self.i + n].decode('utf-8'); self.i += n; return s


# Riot property-bin value types (current numbering).
BASIC = {1: '?', 2: 'b', 3: 'B', 4: 'h', 5: 'H', 6: 'i', 7: 'I', 8: 'q', 9: 'Q', 10: 'f', 11: '2f', 12: '3f',
         13: '4f', 14: '16f', 15: '4B', 17: 'I', 18: 'Q', 132: 'I', 135: 'B'}


def read_value(r, t):
    if t == 0:
        return None
    if t in BASIC:
        value = r.take(BASIC[t])
        return list(value) if isinstance(value, tuple) else value
    if t == 16:
        return r.text()
    if t in (128, 129):
        vt = r.take('B'); r.take('I'); count = r.take('I')
        return [read_value(r, vt) for _ in range(count)]
    if t in (130, 131):
        cls = r.take('I')
        if cls == 0:
            return None
        r.take('I'); count = r.take('H')
        obj = {'@class': NAME.get(cls, f'{cls:08x}')}
        for _ in range(count):
            key, value = read_field(r)
            obj[key] = value
        return obj
    if t == 133:
        vt = r.take('B'); count = r.take('B')
        return read_value(r, vt) if count else None
    if t == 134:
        kt, vt = r.take('BB'); r.take('I'); count = r.take('I')
        return [(read_value(r, kt), read_value(r, vt)) for _ in range(count)]
    raise ValueError(f'Unsupported bin type {t} at {r.i}')


def read_field(r):
    h = r.take('I'); t = r.take('B')
    return NAME.get(h, f'{h:08x}'), read_value(r, t)


def read_bin(path):
    r = Reader(path.read_bytes())
    magic = r.d[:4]
    if magic == b'PTCH':
        r.i = 12
    elif magic != b'PROP':
        raise ValueError(f'{path}: not a property bin')
    assert r.d[r.i:r.i + 4] == b'PROP'; r.i += 4
    version = r.take('I')
    if version >= 2:
        for _ in range(r.take('I')):
            r.text()
    count = r.take('I')
    types = [r.take('I') for _ in range(count)]
    entries = []
    for t in types:
        r.take('I'); key = r.take('I'); obj = {'@class': NAME.get(t, f'{t:08x}'), '@key': key}
        for _ in range(r.take('H')):
            name, value = read_field(r)
            obj[name] = value
        entries.append(obj)
    return entries


def decode_tex(data):
    if data[:4] != b'TEX\0':
        raise ValueError('Expected TEX texture')
    width, height = struct.unpack_from('<HH', data, 4); fmt = data[9]
    if fmt in (10, 11, 12):
        size = ((width + 3) // 4) * ((height + 3) // 4) * (16 if fmt == 12 else 8)
        header = struct.pack('<7I', 124, 0x81007, height, width, size, 0, 0) + bytes(44)
        header += struct.pack('<II4s5I', 32, 4, b'DXT5' if fmt == 12 else b'DXT1', 0, 0, 0, 0, 0)
        header += struct.pack('<5I', 0x1000, 0, 0, 0, 0)
        # Mipmaps are stored smallest first: the full-size level is at the end.
        return Image.open(io.BytesIO(b'DDS ' + header + data[-size:])).convert('RGBA')
    if fmt == 20:
        b, g, r, a = Image.frombytes('RGBA', (width, height), data[-width * height * 4:]).split()
        return Image.merge('RGBA', (r, g, b, a))
    raise ValueError(f'Unsupported TEX format {fmt}')


def read_scb(data):
    if data[:8] != b'r3d2Mesh':
        raise ValueError('Expected SCB mesh')
    major, minor = struct.unpack_from('<HH', data, 8)
    vertex_count, face_count, _ = struct.unpack_from('<III', data, 140)
    i = 176
    vertex_type = 0
    if (major, minor) >= (3, 2):
        vertex_type = struct.unpack_from('<I', data, i)[0]; i += 4
    positions = [struct.unpack_from('<3f', data, i + 12 * k) for k in range(vertex_count)]; i += 12 * vertex_count
    colors = None
    if vertex_type >= 1:
        colors = [struct.unpack_from('<4B', data, i + 4 * k) for k in range(vertex_count)]; i += 4 * vertex_count
    i += 12  # central point
    out_pos, out_uv, out_col, indices = [], [], [], []
    for _ in range(face_count):
        face = struct.unpack_from('<3I', data, i); i += 12 + 64
        uv = struct.unpack_from('<6f', data, i); i += 24
        if len(set(face)) < 3:
            continue
        base = len(out_pos) // 3
        # LoL -> Unity: mirror X like the character importer, so the winding is reversed.
        for corner in (0, 2, 1):
            x, y, z = positions[face[corner]]
            out_pos += [round(-x, 3), round(y, 3), round(z, 3)]
            out_uv += [round(uv[corner], 5), round(uv[corner + 3], 5)]
            if colors:
                # SCB stores BGRA bytes.
                b, g, r, a = colors[face[corner]]
                out_col += [round(r / 255, 4), round(g / 255, 4), round(b / 255, 4), round(a / 255, 4)]
        indices += [base, base + 1, base + 2]
    return {'positions': out_pos, 'uvs': out_uv, 'colors': out_col, 'indices': indices}


def guid(name):
    return hashlib.md5(('MidLane/IreliaVfx/' + name).encode('utf-8')).hexdigest()


def write_meta(path, text):
    meta = Path(str(path) + '.meta')
    if not meta.exists():
        meta.write_text(text, encoding='utf-8')


def folder_meta(folder):
    write_meta(folder, f'fileFormatVersion: 2\nguid: {guid(folder.name + "/")}\nfolderAsset: yes\n'
               'DefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')


# ---------------------------------------------------------------------------------------------
files = {p.name.lower(): p for p in SOURCE.rglob('*') if p.is_file()}
systems = {}
# Unresolved/ holds bins whose original path is unknown (e.g. P_Hit_4stack_right); some are
# not property bins at all, so unreadable ones are skipped.
for path in (sorted((SOURCE / 'data/characters/irelia').glob('*.bin')) + [SOURCE / 'data/characters/irelia/skins/skin0.bin'] +
             sorted((SOURCE / 'LongNames').glob('*.bin')) +
             sorted((SOURCE / 'Unresolved').glob('*.bin'))):
    try:
        entries = read_bin(path)
    except (ValueError, struct.error, UnicodeDecodeError, AssertionError):
        continue
    for entry in entries:
        if entry['@class'] == 'VfxSystemDefinitionData' and SYSTEMS.match(entry.get('particleName', '')):
            systems.setdefault(entry['particleName'], entry)


def build_resolver():
    """Effect keys (child particles, animation events) resolve through the skin's ResourceResolver."""
    found = {}
    def walk(obj):
        if isinstance(obj, dict):
            for key, v in obj.items():
                if key == 'resourceMap':
                    for k, target in v:
                        found[k] = target
                else:
                    walk(v)
        elif isinstance(obj, list):
            for v in obj:
                walk(v)
    for entry in read_bin(SOURCE / 'data/characters/irelia/skins/skin0.bin'):
        walk(entry)
    return found


RESOLVER = build_resolver()
SYSTEM_BY_ENTRY = {entry['@key']: name for name, entry in systems.items()}


def child_systems(e):
    """Systems a particle spawns (childParticleSetDefinition, or its newer unnamed equivalent)."""
    keys = [c.get('effectKey') for c in ((e.get('childParticleSetDefinition') or {}).get('childrenIdentifiers') or [])
            if isinstance(c, dict)]
    keys += [c.get('effectKey') for c in ((e.get('a03664c8') or {}).get('663f55e6') or []) if isinstance(c, dict)]
    names = [SYSTEM_BY_ENTRY.get(RESOLVER.get(k, k)) for k in keys if k is not None]
    return [n for n in names if n]


def value(obj, size):
    """ValueFloat/Vector/Color -> {c, t, v, p}. Dynamics replace the constant when present."""
    if not isinstance(obj, dict):
        return None
    flat = lambda x: [float(x)] if not isinstance(x, list) else [float(c) for c in x]
    out = {'c': flat(obj.get('constantValue', [0] * size if size > 1 else 0))}
    dyn = obj.get('dynamics')
    if isinstance(dyn, dict) and dyn.get('times'):
        keys = sorted(zip([float(t) for t in dyn['times']], dyn['values']), key=lambda k: k[0])
        out['t'] = [t for t, _ in keys]
        out['v'] = [c for _, v in keys for c in flat(v)]
        tables = []
        for table in dyn.get('probabilityTables') or []:
            table = table or {}
            tables.append({'t': [float(t) for t in table.get('keyTimes') or []],
                           'v': [float(v) for v in table.get('keyValues') or []]})
        if tables:
            out['p'] = tables
    return out


textures, meshes, notes = {}, {}, []
# Textures of other champions or skins that some Irelia emitters borrow and that are not in the
# supplied files, with the Irelia texture of the same kind that stands in for each (soft glow for
# glows and flashes, a mote for sparks).
SUBSTITUTE_TEXTURES = {
    'zoe_base_z_spark': 'irelia_base_z_glow_bright',           # W_Hit: sparks (streaks along their flight)
    'zoe_base_q_impactglow': 'irelia_base_z_spotlight',        # E_Blades: dark circle under the blade
}
# Not replaced: Irelia_Skin16_Glow (W_Swipe_Empowered/Center): a soft glow stretched along the
# slash showed as a sharp spike pointing ahead of Irelia for a frame when the W is released.
# Not replaced either: the dust textures (TahmKench smoke, Kaisa recall dust). Irelia has no soft smoke
# texture, and her cloudy noise drawn instead shows square patches on the ground.


def texture_ref(path):
    if not path:
        return ''
    name = Path(path.replace('\\', '/')).stem.lower()
    if name not in textures:
        source = files.get(name + '.tex') or files.get(name + '.dds')
        if source is None:
            notes.append(f'Not in the supplied files: {path} (emitter skipped, or erosion off for an erosion map).')
            textures[name] = None
            return ''
        image = decode_tex(source.read_bytes()) if source.suffix == '.tex' else Image.open(source).convert('RGBA')
        alpha = image.getchannel('A').getextrema()
        textures[name] = {'source': source, 'image': image, 'opaqueAlpha': alpha[0] == alpha[1]}
    return name if textures[name] else ''


def color_ramp(path):
    """particleColorTexture: a horizontal color-over-life ramp, sampled on the middle row."""
    name = texture_ref(path)
    if not name:
        return None
    image = textures[name]['image']
    w, h = image.size
    t = [k / 15 for k in range(16)]
    v = []
    for k in t:
        v += [round(c / 255, 4) for c in image.getpixel((min(w - 1, int(k * (w - 1) + .5)), h // 2))]
    return {'c': v[:4], 't': t, 'v': v}


PRIMITIVES = {'VfxPrimitiveArbitraryQuad': 'quad', 'VfxPrimitiveMesh': 'mesh', 'VfxPrimitiveArbitraryTrail': 'trail',
              'VfxPrimitiveCameraTrail': 'cameraTrail', 'VfxPrimitiveAttachedMesh': 'attachedMesh',
              'VfxPrimitiveBeam': 'beam', 'VfxPrimitiveRay': 'ray'}


def emitter(e):
    out = {'name': e.get('emitterName', ''), 'skip': ''}
    for key in SKIPPED_EMITTER_KEYS:
        if key in e:
            out['skip'] = key
    prim = e.get('primitive') or {}
    out['primitive'] = PRIMITIVES.get(prim.get('@class'), 'billboard')
    mesh_def = prim.get('mMesh') or {}
    mesh_path = mesh_def.get('mSimpleMeshName', '')
    if mesh_path:
        name = Path(mesh_path.replace('\\', '/')).stem.lower()
        if name not in meshes and name + '.scb' not in files:
            notes.append(f'Not in the supplied files: {mesh_path} (emitter skipped).')
            out['skip'] = 'missing mesh'
        else:
            if name not in meshes:
                meshes[name] = read_scb(files[name + '.scb'].read_bytes())
            out['mesh'] = name
            if out['primitive'] == 'beam':
                z = meshes[name]['positions'][2::3]
                out['beamLength'] = max(z) - min(z)
    trail = prim.get('mTrail') or {}
    if trail:
        out['trailTiling'] = value(trail.get('mBirthTilingSize'), 3)
        out['trailMode'] = int(trail.get('mMode', 0))
        out['trailCutoff'] = float(trail.get('mCutoff', 0))
    out['texture'] = texture_ref(e.get('texture', ''))
    missing = Path(str(e.get('texture', '')).replace('\\', '/')).stem.lower()
    if e.get('texture') and not out['texture'] and missing in SUBSTITUTE_TEXTURES:
        # Another champion's or skin's texture that is not in the supplied files, replaced by the
        # closest Irelia texture so the emitter still plays (compared with in-game footage).
        out['texture'] = texture_ref(SUBSTITUTE_TEXTURES[missing] + '.tex')
        notes.append(f'{missing}: replaced by {out["texture"]} (texture not in the supplied files).')
    if e.get('texture') and not out['texture']:
        # A white quad would not resemble the missing (other champion's) texture: skip it.
        out['skip'] = 'missing texture'
    out['blend'] = int(e.get('blendMode', 0))
    out['pass'] = int(e.get('pass', 0))
    out['alphaRef'] = int(e.get('alphaRef', 0))
    out['twoSided'] = bool(e.get('disableBackfaceCull', False))
    out['single'] = bool(e.get('isSingleParticle', 0))
    out['delay'] = float(e.get('timeBeforeFirstEmission', 0))
    out['lifetime'] = float(e['lifetime']) if e.get('lifetime') is not None else -1.0
    out['linger'] = float(e['particleLinger']) if e.get('particleLinger') is not None else -1.0
    out['bindWeight'] = float((e.get('bindWeight') or {}).get('constantValue', 0))
    for key, src, size in [('rate', 'rate', 1), ('particleLifetime', 'particleLifetime', 1),
                           ('birthColor', 'birthColor', 4), ('color', 'Color', 4),
                           ('birthScale', 'birthScale0', 3), ('scale', 'scale0', 3),
                           ('birthRotation', 'birthRotation0', 3), ('rotation', 'rotation0', 3),
                           ('rotationalVelocity', 'birthRotationalVelocity0', 3),
                           ('birthVelocity', 'birthVelocity', 3), ('velocity', 'velocity', 3),
                           ('birthDrag', 'birthDrag', 3), ('drag', 'drag', 3),
                           ('acceleration', 'worldAcceleration', 3), ('emitterPosition', 'EmitterPosition', 3),
                           ('uvScroll', 'particleUVScrollRate', 2), ('birthUvScroll', 'birthUvScrollRate', 2),
                           ('birthUvOffset', 'birthUVOffset', 2), ('uvRotation', 'uvRotation', 1)]:
        v = value(e.get(src), size)
        if v is not None:
            out[key] = v
    ramp = color_ramp(e.get('particleColorTexture', ''))
    if ramp:
        out['colorRamp'] = ramp
    shape = e.get('SpawnShape') or {}
    cls = shape.get('@class', '')
    out['shape'] = {'VfxShapeBox': 'box', 'VfxShapeSphere': 'sphere', 'VfxShapeCylinder': 'cylinder'}.get(cls, 'point')
    if 'Size' in shape:
        out['shapeSize'] = [float(c) for c in shape['Size']]
    if 'radius' in shape:
        out['shapeRadius'] = float(shape['radius'])
    if 'height' in shape:
        out['shapeHeight'] = float(shape['height'])
    if 'flags' in shape:
        out['shapeFlags'] = int(shape['flags'])
    offset = shape.get('emitOffset')
    if isinstance(offset, list):
        out['shapeOffset'] = {'c': [float(c) for c in offset]}
    elif isinstance(offset, dict):
        out['shapeOffset'] = value(offset, 3)
    if shape.get('emitRotationAngles'):
        out['shapeRotationAngles'] = [value(a, 1) for a in shape['emitRotationAngles']]
        out['shapeRotationAxes'] = [c for axis in shape.get('emitRotationAxes') or [] for c in axis]
    flex = e.get('FlexShapeDefinition') or {}
    out['flexScale'] = float(flex.get('scaleBirthScaleByBoundObjectSize', 0))
    out['flexOffset'] = float(flex.get('scaleEmitOffsetByBoundObjectSize', 0))
    for key, src in [('uniformScale', 'isUniformScale'), ('rotationEnabled', 'isRotationEnabled'),
                     ('localOrientation', 'isLocalOrientation'), ('particleLocalOrientation', 'particleIsLocalOrientation'),
                     ('directionOriented', 'isDirectionOriented'), ('groundLayer', 'isGroundLayer'),
                     ('randomStartFrame', 'isRandomStartFrame')]:
        out[key] = bool(e.get(src, 0))
    # isLocalOrientation defaults to on in Riot's data; only an explicit 0 turns it off.
    out['localOrientation'] = bool(e.get('isLocalOrientation', 1))
    out['directionVelocityScale'] = float(e.get('directionVelocityScale', 0))
    out['directionVelocityMinScale'] = float(e.get('directionVelocityMinScale', 0))
    div = e.get('texDiv') or [1, 1]
    out['texDiv'] = [float(div[0]), float(div[1])]
    out['numFrames'] = int(e.get('numFrames', 0))
    out['frameRate'] = float(e.get('frameRate', 0))
    out['startFrame'] = int(e.get('startFrame', 0))
    out['uvMode'] = int(e.get('uvMode', 0))
    mult = e.get('textureMult')
    if isinstance(mult, dict) and texture_ref(mult.get('textureMult', '')):
        div = mult.get('texDivMult') or [1, 1]
        out['textureMult'] = {'texture': texture_ref(mult['textureMult']), 'texDiv': [float(div[0]), float(div[1])],
                              'birthUvScroll': value(mult.get('birthUvScrollRateMult'), 2),
                              'birthUvOffset': value(mult.get('birthUVOffsetMult'), 2)}
    children = child_systems(e)
    if children:
        out['children'] = children
    orbital = value(e.get('birthOrbitalVelocity'), 3)
    if orbital is not None:
        out['orbitalVelocity'] = orbital
    # paletteDefinition (class unnamed): the texture's red channel picks a color from one row of a
    # palette texture with 'count' rows (W swipes recolor greyscale textures with irelia_Base_P_gradient).
    palette = e.get('paletteDefinition')
    if isinstance(palette, dict) and texture_ref(palette.get('ffa711fb', '')):
        index = value(palette.get('8bd46035'), 3)
        out['palette'] = {'texture': texture_ref(palette['ffa711fb']), 'count': int(palette.get('9e52382f', 1)),
                          'index': float(index['c'][0]) if index else 0.0}
    erosion = e.get('alphaErosionDefinition')
    if isinstance(erosion, dict):
        name = texture_ref(erosion.get('erosionMapName', ''))
        out['erosion'] = {'map': name, 'drive': value(erosion.get('erosionDriveCurve'), 1) or {'c': [0]},
                          'featherIn': float(erosion.get('erosionFeatherIn', 0)),
                          'featherOut': float(erosion.get('erosionFeatherOut', .1)),
                          'sliceWidth': float(erosion.get('erosionSliceWidth', 0)),
                          'useAlpha': bool(name and not textures[name]['opaqueAlpha'])}
    return out


library = {'version': 1, 'source': 'CharacterAssets/Irelia/Base/Original (skin0 VFX bins)', 'systems': [],
           'meshes': [], 'textures': [], 'events': [], 'notes': notes}
for name in sorted(systems):
    entry = systems[name]
    emitters = [emitter(e) for e in entry.get('complexEmitterDefinitionData') or []]
    emitters += [emitter(e) for e in entry.get('simpleEmitterDefinitionData') or []]
    library['systems'].append({'name': name, 'sound': entry.get('soundOnCreateDefault', ''),
                               'emitters': emitters})

# Matched by eye to in-game footage (Handoff/ARTIST_NOTES.md), where our reading of a field still
# differs from LoL: the W shield bubble keeps its filled look and rim for most of its life (its
# erosion runs slower) and the ghost blade images around it stay faint; at four passive stacks the
# body flash and the rays that burst out of Irelia are orange-gold, as in the game (LoL tints them
# through its bloom, which the project does not have).
GOLD_FLASH = {'c': [1, .66, .25, .45], 't': [0, 1], 'v': [1, .66, .25, .45, 1, .55, .14, 0]}
# The soft glow around her at four stacks reads like candlelight in the footage: warm orange-gold,
# wider than her body, gently pulsing (two overlapping particles a second fade in and out); at
# full strength its round texture showed as a disc (a "coin") on her body.
ORANGE_HAZE = {'c': [1, .66, .28, .26], 't': [0, .5, 1], 'v': [1, .66, .28, 0, 1, .66, .28, .26, 1, .6, .22, 0]}
GOLD_RAYS = {'c': [1, .8, .4, 1], 't': [0, 1], 'v': [1, .88, .52, 1, 1, .52, .1, 0]}
# W_Hit: in the footage the hit is a white-hot burst with a blue halo, about as wide as the target
# and brightest in its first tenth of a second, with sparks streaking out; read literally its star
# starts invisible and only turns blue, and the sparks are specks.
W_HIT_STAR = {'c': [1, 1, 1, 1], 't': [0, .2, .55, 1], 'v': [1, 1, 1, 1, .82, .9, 1, 1, .35, .5, 1, .55, .3, .4, 1, 0]}
# R_Hit_Aoe: when the R detonates, the game lays a big pale Irelia-mark triangle on the floor
# with one corner pointing where the R flew; read literally it turned sideways (its particle
# orientation would follow the world, not the wall).
ARTIST_TWEAKS = {
    ('Irelia_Base_W_Hit', 'large'): {'color': W_HIT_STAR, 'size': 1.6},
    ('Irelia_Base_W_Hit', 'glow'): {'size': 1.5},
    ('Irelia_Base_W_Hit', 'glow1'): {'size': 1.5},
    ('Irelia_Base_W_Hit', 'burst'): {'size': 2.2},
    ('Irelia_Base_W_DR', 'Ripple'): {'erosionDrive': .6},
    ('Irelia_Base_W_DR', 'top2'): {'alpha': .6},
    ('Irelia_Base_P_buf', 'Avatar_Burst'): {'color': GOLD_FLASH},
    ('Irelia_Base_P_buf', 'glow1'): {'color': ORANGE_HAZE, 'size': 1.35},
    ('Irelia_Base_P_buf', 'Sparks1'): {'color': GOLD_RAYS},
    ('Irelia_Base_P_buf', 'Sparks2'): {'color': GOLD_RAYS, 'alpha': .45, 'rate': .4},
    ('Irelia_Base_R_Hit_Aoe', 'large_yellow'): {'localOrientation': True, 'birthRotation': [90.0, 180.0, 0.0]},
}
for system in library['systems']:
    for e in system['emitters']:
        tweak = ARTIST_TWEAKS.get((system['name'], e['name']))
        if not tweak:
            continue
        if 'erosionDrive' in tweak and e.get('erosion'):
            drive = e['erosion']['drive']
            drive['c'] = [v * tweak['erosionDrive'] for v in drive.get('c') or []]
            drive['v'] = [v * tweak['erosionDrive'] for v in drive.get('v') or []]
        if 'color' in tweak:
            e['color'] = json.loads(json.dumps(tweak['color']))
        if 'size' in tweak and e.get('birthScale'):
            e['birthScale']['c'] = [v * tweak['size'] for v in e['birthScale']['c']]
            if e['birthScale'].get('v'):
                e['birthScale']['v'] = [v * tweak['size'] for v in e['birthScale']['v']]
        if 'rate' in tweak and e.get('rate'):
            e['rate']['c'] = [v * tweak['rate'] for v in e['rate']['c']]
            if e['rate'].get('v'):
                e['rate']['v'] = [v * tweak['rate'] for v in e['rate']['v']]
        if 'alpha' in tweak:
            curve = e.get('color')
            if curve:
                curve['c'][3] *= tweak['alpha']
                curve['v'] = [v * tweak['alpha'] if i % 4 == 3 else v for i, v in enumerate(curve.get('v') or [])]
        if 'localOrientation' in tweak:
            e['localOrientation'] = tweak['localOrientation']
        if 'birthRotation' in tweak:
            e['birthRotation'] = {'c': [float(v) for v in tweak['birthRotation']]}
        notes.append(f"{system['name']}/{e['name']}: adjusted to match in-game footage {sorted(tweak)}.")

# The R in the game footage bursts: its explosion throws a spray of rays, the hit throws a fan of
# streaks forward, the wall's hit a crown of spikes and shards. Its single-particle emitters
# emit their whole rate at once (a burst), not one particle.
for system in library['systems']:
    if not system['name'].startswith('Irelia_Base_R_'):
        continue
    for e in system['emitters']:
        rate = (e.get('rate') or {}).get('c') or [1]
        if e.get('single') and rate[0] > 1.01:
            e['burst'] = True
            notes.append(f"{system['name']}/{e['name']}: emits a burst of {rate[0]:g} particles, as in the game footage.")


# ---------------------------------------------------------------------------------------------
# Four-stack passive, matched to in-game footage (Handoff/ARTIST_NOTES.md). LoL draws part of this
# look through its bloom, which the project does not have, so Irelia_Base_P_buf gets one more
# emitter with a texture drawn here:
#  - Beams: while she keeps four stacks, soft candle-gold beams spread from her body center in a
#    loose circle. Each beam is its own particle (about nine a second, each living about half to
#    one second) with a random direction, length, brightness and a slow drift, so the pattern is
#    irregular and always changing, as in the footage; each beam is brightest at her center and
#    fades toward its end, and they follow her as she moves.
# (The gold laurel wreath seen under Irelia in the footage is the Conqueror rune's fully stacked
# effect: it appears when that rune's buff reaches 12 stacks, not with Irelia's passive.)


class GeneratedSource:
    def __init__(self, image):
        buffer = io.BytesIO(); image.save(buffer, 'PNG'); self.data = buffer.getvalue()

    def read_bytes(self):
        return self.data


def add_generated_texture(name, image):
    alpha = image.getchannel('A').getextrema()
    textures[name] = {'source': GeneratedSource(image), 'image': image, 'opaqueAlpha': alpha[0] == alpha[1]}


def beam_texture(size=256):
    """One soft beam from the center of the quad to its right edge, fading along its length."""
    import math
    out = Image.new('RGBA', (size, size))
    px = out.load()
    for y in range(size):
        for x in range(size // 2, size):
            d = ((x + .5) / size - .5) * 2               # 0 at the center, 1 at the edge
            across = abs((y + .5) / size - .5) * 2
            width = .035 + .07 * d
            a = math.exp(-(across / width) ** 2) * (1 - d) ** 1.5 * min(1, d / .06)
            if a < .004:
                continue
            px[x, y] = (255, int(255 * (.82 - .22 * d)), int(255 * (.45 - .25 * d)), int(255 * min(1, a)))
    add_generated_texture('irelia_p_beam_gold', out)


buf = next((system for system in library['systems'] if system['name'] == 'Irelia_Base_P_buf'), None)
if buf is not None:
    beam_texture()
    beams = {'name': 'Beams', 'skip': '', 'primitive': 'billboard', 'texture': 'irelia_p_beam_gold', 'blend': 4,
             'pass': 6, 'alphaRef': 0, 'twoSided': False, 'single': False, 'delay': 0.0, 'lifetime': -1.0,
             'linger': .4, 'bindWeight': 1.0, 'rate': {'c': [9.0]},
             'particleLifetime': {'c': [.75], 't': [0], 'v': [.75], 'p': [{'t': [0, 1], 'v': [.65, 1.35]}]},
             'birthColor': {'c': [1, 1, 1, 1], 't': [0], 'v': [1, 1, 1, 1],
                            'p': [{'t': [], 'v': []}, {'t': [], 'v': []}, {'t': [], 'v': []}, {'t': [0, 1], 'v': [.3, 1]}]},
             'color': {'c': [1, .72, .32, .36], 't': [0, .35, 1], 'v': [1, .74, .34, 0, 1, .72, .32, .36, 1, .6, .22, 0]},
             'birthScale': {'c': [150.0, 150.0, 1.0], 't': [0], 'v': [150.0, 150.0, 1.0],
                            'p': [{'t': [0, 1], 'v': [.55, 1.25]}, {'t': [], 'v': []}, {'t': [], 'v': []}]},
             'scale': {'c': [1, 1, 1], 't': [0, 1], 'v': [.85, .85, .85, 1.1, 1.1, 1.1]},
             'birthRotation': {'c': [1.0, 0.0, 0.0], 't': [0], 'v': [1.0, 0.0, 0.0],
                               'p': [{'t': [0, 1], 'v': [0, 360]}, {'t': [], 'v': []}, {'t': [], 'v': []}]},
             'rotationalVelocity': {'c': [1.0, 0.0, 0.0], 't': [0], 'v': [1.0, 0.0, 0.0],
                                    'p': [{'t': [0, 1], 'v': [-14, 14]}, {'t': [], 'v': []}, {'t': [], 'v': []}]},
             'shape': 'point', 'shapeOffset': {'c': [0.0, 115.0, 0.0]}, 'flexScale': 0.0, 'flexOffset': 0.0,
             'uniformScale': True, 'rotationEnabled': False, 'localOrientation': False,
             'particleLocalOrientation': False, 'directionOriented': False, 'groundLayer': False,
             'randomStartFrame': False, 'directionVelocityScale': 0.0, 'directionVelocityMinScale': 0.0,
             'texDiv': [1.0, 1.0], 'numFrames': 0, 'frameRate': 0.0, 'startFrame': 0, 'uvMode': 0}
    buf['emitters'].append(beams)
    notes.append('Irelia_Base_P_buf/Beams: added to match in-game footage (generated texture irelia_p_beam_gold).')

# Animation graph particle events (which clip spawns which swipe, on which frame).
anm_paths = {}
for path in SOURCE.rglob('*.anm'):
    rel = path.relative_to(SOURCE).as_posix().lower()
    anm_paths[xxh64(rel.encode('utf-8'))] = path.stem.lower()
resolver = {}
for entry in read_bin(SOURCE / 'data/characters/irelia/skins/skin0.bin'):
    def walk(obj):
        if isinstance(obj, dict):
            for key, v in obj.items():
                if key == 'resourceMap':
                    for k, target in v:
                        resolver[k] = target
                else:
                    walk(v)
        elif isinstance(obj, list):
            for v in obj:
                walk(v)
    walk(entry)
# Effect keys resolve through the skin's ResourceResolver to the system entry.
system_by_entry = {entry['@key']: name for name, entry in systems.items()}
for entry in read_bin(SOURCE / 'data/characters/irelia/animations/skin0.bin'):
    for _, clip in entry.get('mClipDataMap') or []:
        if not isinstance(clip, dict):
            continue
        anm = anm_paths.get((clip.get('mAnimationResourceData') or {}).get('mAnimationFilePath'), '')
        for _, event in clip.get('mEventDataMap') or []:
            if not isinstance(event, dict) or event.get('@class') != 'ParticleEventData':
                continue
            key = event.get('mEffectKey')
            system = system_by_entry.get(resolver.get(key, key))
            if not system or not anm:
                continue
            pairs = event.get('mParticleEventDataPairList') or [{}]
            library['events'].append({'clip': anm, 'system': system, 'frame': float(event.get('mStartFrame', 0)),
                                      'bone': BONES.get((pairs[0] or {}).get('mBoneName'), ''),
                                      'scaleWithAnimation': bool(event.get('mScalePlaySpeedWithAnimation', False))})
library['events'].sort(key=lambda e: (e['clip'], e['frame'], e['system']))

OUTPUT.mkdir(parents=True, exist_ok=True); TEXTURES.mkdir(parents=True, exist_ok=True)
folder_meta(OUTPUT.parent); folder_meta(OUTPUT); folder_meta(TEXTURES)
template = (ROOT / 'Assets/Resources/Models/IreliaOfficial/E/Trail.png.meta').read_text(encoding='utf-8-sig')
# Particle textures tile (UV scroll) and keep their authored alpha.
template = template.replace('    wrapU: 1\n    wrapV: 1\n    wrapW: 1', '    wrapU: 0\n    wrapV: 0\n    wrapW: 0')
# Raw (gamma) values: RiotParticle.shader reproduces LoL's gamma-space look (see shader).
template = template.replace('    sRGBTexture: 1', '    sRGBTexture: 0')
for name, info in sorted(textures.items()):
    if not info:
        continue
    target = TEXTURES / (name + '.png')
    info['image'].save(target)
    write_meta(target, re.sub(r'guid: \w+', 'guid: ' + guid('Textures/' + name), template, count=1))
    library['textures'].append({'name': name, 'width': info['image'].size[0], 'height': info['image'].size[1],
                                'sha256': hashlib.sha256(info['source'].read_bytes()).hexdigest()})
for name, mesh in sorted(meshes.items()):
    library['meshes'].append(dict(name=name, **mesh))
# The blades' material switches its diffuse to this texture at 4 passive stacks
# (Irelia_Blades_Passive_inst: Diffuse_Texture driven by the IreliaPassiveStacks buff counter).
# Same import settings as the regular blades texture (Models/IreliaOfficial/Texture1.png).
blades_source = files.get('irelia_base_blades_passive_4_texture.tex')
if blades_source:
    target = ROOT / 'Assets/Resources/Models/IreliaOfficial/BladesPassive4.png'
    decode_tex(blades_source.read_bytes()).save(target)
    blades_meta = (ROOT / 'Assets/Resources/Models/IreliaOfficial/Texture1.png.meta').read_text(encoding='utf-8-sig')
    write_meta(target, re.sub(r'guid: \w+', 'guid: ' + guid('BladesPassive4'), blades_meta, count=1))
else:
    notes.append('Not in the supplied files: irelia_base_blades_passive_4_texture.tex (4-stack blades keep their texture).')
target = OUTPUT / 'IreliaVfx.json'
target.write_text(json.dumps(library, separators=(',', ':')) + '\n', encoding='utf-8')
write_meta(target, f'fileFormatVersion: 2\nguid: {guid("IreliaVfx.json")}\nTextScriptImporter:\n  externalObjects: {{}}\n'
           '  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
print(f'Imported {len(library["systems"])} systems, {sum(len(s["emitters"]) for s in library["systems"])} emitters, '
      f'{len(library["textures"])} textures, {len(meshes)} meshes, {len(library["events"])} animation events.')
for note in notes:
    print('  note:', note)
