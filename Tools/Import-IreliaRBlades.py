"""Import the official R (Vanguard's Edge) blade wall: the IreliaBlades unit of the game files.

Usage: python Tools/Import-IreliaRBlades.py [CharacterAssets/Irelia/Base/Original]
Python standard library only.

In LoL the wall of blades left by the R is its own unit, IreliaBlades: one skinned mesh with 26
blades, one bone each (Sword1..Sword28 under Sword_Root), and four clips: cast (the blades rise and
spread from the impact to their places on the wall), cycle (they stand), death (they sink into
the ground) and deathcycle. The blades are rigid (each vertex follows one bone), so the wall is
written here as 26 small meshes, each in its bone's space, plus every bone's pose for each frame
of each clip, relative to the unit:

  Assets/Resources/Vfx/Irelia/IreliaRBlades.json

Units are LoL units, already mirrored on X like the rest of the project (LoL -> Unity); UVs are
flipped for Unity textures. The blades use Irelia's blades texture (irelia_base_blades_tx_cm,
Models/IreliaOfficial/Texture1.png) through the material Irelia_bladeglow_inst, whose rim colors
IreliaRWallBlade.shader reproduces. IreliaRBlades.cs plays it.
"""
import hashlib
import json
import math
import struct
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / 'CharacterAssets/Irelia/Base/Original'
BASE = SOURCE / 'assets/characters/ireliablades/skins/base'
TARGET = ROOT / 'Assets/Resources/Vfx/Irelia/IreliaRBlades.json'
CLIPS = ('cast', 'cycle', 'death', 'deathcycle')


def elf_hash(name):
    """Joint name hash used by the animation tracks."""
    h = 0
    for c in name.lower().encode('ascii'):
        h = ((h << 4) + c) & 0xFFFFFFFF
        g = h & 0xF0000000
        if g:
            h ^= g >> 24
        h &= ~g & 0xFFFFFFFF
    return h


class Reader:
    def __init__(self, data):
        self.d, self.i = data, 0

    def take(self, fmt):
        values = struct.unpack_from('<' + fmt, self.d, self.i)
        self.i += struct.calcsize('<' + fmt)
        return values if len(values) > 1 else values[0]


# --- skeleton (new SKL format) -------------------------------------------------------------------
def read_skl(path):
    r = Reader(path.read_bytes())
    _, token, version = r.take('3I')
    assert token == 0x22FD4FC3 and version == 0, 'unsupported skeleton'
    _, joint_count = r.take('2H')
    influence_count = r.take('I')
    joints_offset, _, influences_offset = r.take('3i')[0:3]
    joints = []
    for k in range(joint_count):
        r.i = joints_offset + k * 100
        flags, jid, parent, _ = r.take('H2hH')
        _, _ = r.take('If')
        local_t = r.take('3f'); local_s = r.take('3f'); local_r = r.take('4f')
        ibind_t = r.take('3f'); ibind_s = r.take('3f'); ibind_r = r.take('4f')
        name_offset = r.take('i')
        start = r.i - 4 + name_offset
        name = r.d[start:r.d.index(b'\0', start)].decode('ascii')
        joints.append({'id': jid, 'parent': parent, 'name': name, 'local': (local_t, local_r, local_s),
                       'ibind': (ibind_t, ibind_r, ibind_s)})
    r.i = influences_offset
    influences = [r.take('H') for _ in range(influence_count)]
    return joints, influences


# --- skinned mesh (SKN 4.x) ----------------------------------------------------------------------
def read_skn(path):
    r = Reader(path.read_bytes())
    assert r.take('I') == 0x00112233, 'not a skinned mesh'
    major, minor = r.take('2H')
    assert major == 4, 'unsupported mesh version %d.%d' % (major, minor)
    submeshes = r.take('I')
    r.i += submeshes * (64 + 16)
    r.take('I')                                  # flags
    index_count, vertex_count = r.take('2I')
    vertex_size, vertex_type = r.take('2I')
    r.i += 40                                    # bounding box and sphere
    indices = [r.take('H') for _ in range(index_count)]
    vertices = []
    for k in range(vertex_count):
        start = r.i
        position = r.take('3f'); influence = r.take('4B'); weight = r.take('4f')
        normal = r.take('3f'); uv = r.take('2f')
        r.i = start + vertex_size
        vertices.append((position, influence, weight, normal, uv))
    return vertices, indices


# --- animations (r3d2anmd v5 and r3d2canm) -------------------------------------------------------
def quat48(raw):
    bits = raw[0] | raw[1] << 8 | raw[2] << 16 | raw[3] << 24 | raw[4] << 32 | raw[5] << 40
    largest = (bits >> 45) & 3
    scale, offset = math.sqrt(2) / 32767, 1 / math.sqrt(2)
    a = ((bits >> 30) & 32767) * scale - offset
    b = ((bits >> 15) & 32767) * scale - offset
    c = (bits & 32767) * scale - offset
    d = math.sqrt(max(0.0, 1 - (a * a + b * b + c * c)))
    return [(d, a, b, c), (a, d, b, c), (a, b, d, c), (a, b, c, d)][largest]


def read_anm(path):
    """{joint hash: [(t, r, s) for each frame]} at the clip's frame rate, and its frame count."""
    data = path.read_bytes()
    r = Reader(data)
    signature = data[:8]; r.i = 8
    version = r.take('I')
    tracks = {}
    if signature == b'r3d2anmd':
        assert version == 5, 'unsupported animation version'
        r.take('4I')
        track_count, frame_count = r.take('2I')
        r.take('f')
        hashes_offset, _, _, vectors_offset, quats_offset, frames_offset = r.take('6i')
        vectors = [struct.unpack_from('<3f', data, vectors_offset + 12 + 12 * k)
                   for k in range((quats_offset - vectors_offset) // 12)]
        quats = [quat48(data[quats_offset + 12 + 6 * k:quats_offset + 18 + 6 * k])
                 for k in range((hashes_offset - quats_offset) // 6)]
        hashes = struct.unpack_from('<%dI' % track_count, data, hashes_offset + 12)
        r.i = frames_offset + 12
        for h in hashes:
            tracks[h] = []
        for _ in range(frame_count):
            for h in hashes:
                t, s, q = r.take('3H')
                tracks[h].append((vectors[t], quats[q], vectors[s]))
        return tracks, frame_count
    assert signature == b'r3d2canm', 'unknown animation format'
    r.take('3I')
    track_count, key_count = r.take('2I')
    r.take('I')
    max_time, fps = r.take('2f')
    r.take('6f')
    t_min, t_max, s_min, s_max = (r.take('3f') for _ in range(4))
    frames_offset, _, hashes_offset = r.take('3i')
    hashes = struct.unpack_from('<%dI' % track_count, data, hashes_offset + 12)
    keys = {h: {0: {}, 1: {}, 2: {}} for h in hashes}
    r.i = frames_offset + 12
    for _ in range(key_count):
        time, bits = r.take('2H')
        raw = data[r.i:r.i + 6]; r.i += 6
        frame = time / 65535 * max_time * fps
        kind = bits >> 14
        h = hashes[bits & 16383]
        if kind == 0:
            keys[h][0][frame] = quat48(raw)
        else:
            lo, hi = (t_min, t_max) if kind == 1 else (s_min, s_max)
            keys[h][kind][frame] = tuple(lo[k] + (hi[k] - lo[k]) / 65535 * (raw[2 * k] | raw[2 * k + 1] << 8)
                                         for k in range(3))
    frame_count = int(round(max_time * fps)) + 1

    def sample(track, frame, rotation):
        times = sorted(track)
        before = [t for t in times if t <= frame]; after = [t for t in times if t >= frame]
        a = before[-1] if before else after[0]; b = after[0] if after else before[-1]
        if a == b:
            return track[a]
        f = (frame - a) / (b - a)
        if rotation:
            return slerp(track[a], track[b], f)
        return tuple(track[a][k] + (track[b][k] - track[a][k]) * f for k in range(3))
    for h in hashes:
        tracks[h] = [(sample(keys[h][1], f, False), sample(keys[h][0], f, True), sample(keys[h][2], f, False))
                     for f in range(frame_count)]
    return tracks, frame_count


def slerp(a, b, f):
    dot = sum(x * y for x, y in zip(a, b))
    if dot < 0:
        b = tuple(-x for x in b); dot = -dot
    if dot > .9995:
        q = tuple(x + (y - x) * f for x, y in zip(a, b))
    else:
        theta = math.acos(dot)
        wa, wb = math.sin((1 - f) * theta) / math.sin(theta), math.sin(f * theta) / math.sin(theta)
        q = tuple(wa * x + wb * y for x, y in zip(a, b))
    n = math.sqrt(sum(x * x for x in q))
    return tuple(x / n for x in q)


# --- small matrix helpers (row-major 4x4 lists) --------------------------------------------------
def trs(t, q, s):
    x, y, z, w = q
    r = [[1 - 2 * (y * y + z * z), 2 * (x * y - w * z), 2 * (x * z + w * y)],
         [2 * (x * y + w * z), 1 - 2 * (x * x + z * z), 2 * (y * z - w * x)],
         [2 * (x * z - w * y), 2 * (y * z + w * x), 1 - 2 * (x * x + y * y)]]
    return [[r[i][0] * s[0], r[i][1] * s[1], r[i][2] * s[2], t[i]] for i in range(3)] + [[0, 0, 0, 1]]


def mul(a, b):
    return [[sum(a[i][k] * b[k][j] for k in range(4)) for j in range(4)] for i in range(4)]


def apply(m, v, w=1.0):
    return [m[i][0] * v[0] + m[i][1] * v[1] + m[i][2] * v[2] + m[i][3] * w for i in range(3)]


def decompose(m):
    """Matrix -> position, rotation (x, y, z, w), scale; no shear in these poses."""
    columns = [[m[i][j] for i in range(3)] for j in range(3)]
    scale = [math.sqrt(sum(c * c for c in column)) for column in columns]
    r = [[m[i][j] / scale[j] for j in range(3)] for i in range(3)]
    trace = r[0][0] + r[1][1] + r[2][2]
    if trace > 0:
        s = math.sqrt(trace + 1) * 2
        q = ((r[2][1] - r[1][2]) / s, (r[0][2] - r[2][0]) / s, (r[1][0] - r[0][1]) / s, s / 4)
    elif r[0][0] > r[1][1] and r[0][0] > r[2][2]:
        s = math.sqrt(1 + r[0][0] - r[1][1] - r[2][2]) * 2
        q = (s / 4, (r[0][1] + r[1][0]) / s, (r[0][2] + r[2][0]) / s, (r[2][1] - r[1][2]) / s)
    elif r[1][1] > r[2][2]:
        s = math.sqrt(1 + r[1][1] - r[0][0] - r[2][2]) * 2
        q = ((r[0][1] + r[1][0]) / s, s / 4, (r[1][2] + r[2][1]) / s, (r[0][2] - r[2][0]) / s)
    else:
        s = math.sqrt(1 + r[2][2] - r[0][0] - r[1][1]) * 2
        q = ((r[0][2] + r[2][0]) / s, (r[1][2] + r[2][1]) / s, s / 4, (r[1][0] - r[0][1]) / s)
    return [m[0][3], m[1][3], m[2][3]], list(q), scale


MIRROR = [[-1, 0, 0, 0], [0, 1, 0, 0], [0, 0, 1, 0], [0, 0, 0, 1]]


def mirrored(m):
    """LoL -> Unity: the project mirrors X (models, effects)."""
    return mul(mul(MIRROR, m), MIRROR)


def rounded(values, digits=4):
    return [round(v, digits) + 0.0 for v in values]


# -------------------------------------------------------------------------------------------------
joints, influences = read_skl(BASE / 'ireliablades.skl')
vertices, indices = read_skn(BASE / 'ireliablades.skn')
by_id = {j['id']: j for j in joints}
root = next(j for j in joints if j['parent'] < 0)
blades = [j for j in joints if j['parent'] == root['id']]

# Each blade's triangles, in its bone's space (inverse bind), mirrored on X (winding reversed).
meshes = []
for blade in blades:
    ibind = trs(*blade['ibind'])
    remap, positions, normals, uvs, triangles = {}, [], [], [], []
    for k in range(0, len(indices), 3):
        tri = indices[k:k + 3]
        if len(set(tri)) < 3 or influences[vertices[tri[0]][1][0]] != blade['id']:
            continue
        for v in tri:
            assert influences[vertices[v][1][0]] == blade['id'] and vertices[v][2][0] > .999, 'blade vertex not rigid'
        for v in (tri[0], tri[2], tri[1]):
            if v not in remap:
                remap[v] = len(remap)
                p, _, _, n, uv = vertices[v]
                local = apply(ibind, p); normal = apply(ibind, n, 0)
                length = math.sqrt(sum(c * c for c in normal)) or 1
                positions += [-local[0], local[1], local[2]]
                normals += [-normal[0] / length, normal[1] / length, normal[2] / length]
                uvs += [uv[0], 1 - uv[1]]
            triangles.append(remap[v])
    meshes.append({'bone': blade['name'], 'positions': rounded(positions, 3), 'normals': rounded(normals),
                   'uvs': rounded(uvs, 5), 'indices': triangles})

# Bone poses of every frame, relative to the unit (Sword_Root included), flattened for Unity's
# JsonUtility: frame after frame, bone after bone, 10 values each.
clips = []
for clip in CLIPS:
    path = BASE / 'animations' / ('irelia_spell4__blades_%s.anm' % clip)
    tracks, frame_count = read_anm(path)
    poses = []
    for f in range(frame_count):
        world = {}
        for joint in joints:   # parents come first in the skeleton
            t, q, s = tracks[elf_hash(joint['name'])][f] if elf_hash(joint['name']) in tracks else joint['local']
            local = trs(t, q, s)
            world[joint['id']] = mul(world[joint['parent']], local) if joint['parent'] >= 0 else local
        for joint in [root] + blades:
            p, q, s = decompose(mirrored(world[joint['id']]))
            poses += rounded(p, 3) + rounded(q, 5) + rounded(s, 4)
    clips.append({'name': clip, 'frames': frame_count, 'poses': poses,
                  'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})

library = {
    'version': 1,
    'fps': 30,
    'bones': [root['name']] + [b['name'] for b in blades],
    'pose': 'per frame, per bone: position (3), rotation x y z w (4), scale (3); bone 0 is Sword_Root',
    'blades': meshes,
    'clips': clips,
    'sources': [{'file': name, 'sha256': hashlib.sha256((BASE / name).read_bytes()).hexdigest()}
                for name in ('ireliablades.skl', 'ireliablades.skn')],
}
TARGET.write_text(json.dumps(library, separators=(',', ':')) + '\n', encoding='utf-8')
meta = Path(str(TARGET) + '.meta')
if not meta.exists():
    guid = hashlib.md5(b'MidLane/IreliaRBlades.json').hexdigest()
    meta.write_text(f'fileFormatVersion: 2\nguid: {guid}\nTextScriptImporter:\n  externalObjects: {{}}\n'
                    '  userData: \n  assetBundleName: \n  assetBundleVariant: \n', encoding='utf-8')
print('wrote', TARGET.relative_to(ROOT), '-', len(meshes), 'blades,',
      ', '.join('%s %d frames' % (c['name'], c['frames']) for c in clips))
