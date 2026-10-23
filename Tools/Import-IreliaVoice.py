"""Import Irelia's official English voice lines for gameplay (Hito 2.3).

python Tools/Import-IreliaVoice.py <voice-bank-folder> --wwiser <wwiser.pyz> --vgmstream <vgmstream-cli>

<voice-bank-folder> is assets/sounds/wwise2016/vo/en_us/characters/irelia/skins/base
(irelia_base_vo_events.bnk + irelia_base_vo_audio.wpk). The event bank decides which lines
belong to each event and to each value of the Irelia_P_Stacks switch (passive stacks 0-3 or
max); the WPK holds the media. Nothing is picked by hand: every clip comes from the event's
container. Levels follow the bank's own volume and make-up gain on each line's path, relative
to the R lines already imported by Tools/Extract-IreliaRVoice.py (left untouched), so every
voice keeps its original level against R.
Writes WAVs to Assets/Resources/Audio/IreliaVoice and the manifest to
Assets/SourceAudio/IreliaOfficial/VoiceSources.json.
"""
import argparse
import array
import hashlib
import json
from pathlib import Path
import shutil
import struct
import subprocess
import sys
import wave
import xml.etree.ElementTree as ET

parser = argparse.ArgumentParser()
parser.add_argument('source', type=Path)
parser.add_argument('--wwiser', type=Path, required=True)
parser.add_argument('--vgmstream', type=Path, required=True)
args = parser.parse_args()
root = Path(__file__).resolve().parents[1]
work = root / 'TestResults/IreliaVoiceLines'
output = root / 'Assets/Resources/Audio/IreliaVoice'
raw = root / 'Assets/SourceAudio/IreliaOfficial'
work.mkdir(parents=True, exist_ok=True)


def fnv(name):
    # Wwise IDs are the 32-bit FNV-1 hash of the lowercase name.
    h = 2166136261
    for c in name.lower().encode():
        h = (h * 16777619) & 0xffffffff
        h ^= c
    return h


P_STACKS = fnv('Irelia_P_Stacks')
P0, PMAX = fnv('Irelia_P0'), fnv('Irelia_PMax')
# Second switch of the Q lines at passive max. Its name is not in the bank; the
# Spell3DPEnd_01/02/03 events set it to these three values (and reset the Q sequence).
Q_SET = 1967880116
Q_SETS = (751793041, 751793042, 751793043)

# (clip prefix, event, switch values, part of the container, expected count)
# part: None = every line under the event; ('items', a, b) = Q sequence entries a..b-1.
CUES = [
    # Basic attack efforts. BasicAttack, BasicAttack2 and CritAttack cast3D share this container.
    ('BasicAttack', 'Play_vo_Irelia_IreliaBasicAttack_cast3D', {P_STACKS: P0}, None, 4),
    ('BasicAttackMax', 'Play_vo_Irelia_IreliaBasicAttack_cast3D', {P_STACKS: PMAX}, None, 4),
    ('Q', 'Play_vo_Irelia_IreliaQ_cast3D', {P_STACKS: P0}, None, 4),
    # Passive max: an 8-step sequence, four lines of the current set and four random efforts.
    ('QMaxA', 'Play_vo_Irelia_IreliaQ_cast3D', {P_STACKS: PMAX, Q_SET: Q_SETS[0]}, ('items', 0, 4), 4),
    ('QMaxB', 'Play_vo_Irelia_IreliaQ_cast3D', {P_STACKS: PMAX, Q_SET: Q_SETS[1]}, ('items', 0, 4), 4),
    ('QMaxC', 'Play_vo_Irelia_IreliaQ_cast3D', {P_STACKS: PMAX, Q_SET: Q_SETS[2]}, ('items', 0, 4), 4),
    ('QMax', 'Play_vo_Irelia_IreliaQ_cast3D', {P_STACKS: PMAX, Q_SET: Q_SETS[0]}, ('items', 4, 5), 4),
    # W only speaks at passive max (stacks 0-3 have no child in the switch).
    ('WMax', 'Play_vo_Irelia_IreliaW_cast3D', {P_STACKS: PMAX}, None, 4),
    ('W2Max', 'Play_vo_Irelia_IreliaW2_cast3D', {P_STACKS: PMAX}, None, 4),
    # E and E2 cast3D share one container.
    ('E', 'Play_vo_Irelia_IreliaE_cast3D', {}, None, 5),
    ('MoveFirst', 'Play_vo_Irelia_Move2DFirst', {}, None, 5),
    ('Move', 'Play_vo_Irelia_Move2DStandard', {}, None, 50),
    ('MoveLong', 'Play_vo_Irelia_Move2DLong', {}, None, 3),
    ('AttackOrder', 'Play_vo_Irelia_Attack2DGeneral', {}, None, 40),
    ('BuyBFSword', 'Play_vo_Irelia_BuyItem2DBFSword', {}, None, 3),
    ('BuyRuinedKing', 'Play_vo_Irelia_BuyItem2DBladeOfTheRuinedKing', {}, None, 4),
    ('BuyTitanicHydra', 'Play_vo_Irelia_BuyItem2DTitanicHydra', {}, None, 3),
    ('BuyTrinityForce', 'Play_vo_Irelia_BuyItem2DTrinityForce', {}, None, 4),
    ('Kill', 'Play_vo_Irelia_Kill3DGeneral', {}, None, 7),
    ('Death', 'Play_vo_Irelia_Death3D', {}, None, 7),
    ('Respawn', 'Play_vo_Irelia_Respawn2DGeneral', {}, None, 5),
]
R_EVENT = 'Play_vo_Irelia_IreliaR_cast3D'

bank = args.source / 'irelia_base_vo_events.bnk'
package = args.source / 'irelia_base_vo_audio.wpk'
shutil.copy2(bank, work / bank.name)
subprocess.run([sys.executable, str(args.wwiser.resolve()), bank.name, '-d', 'xml', '-dn', 'vo_events'],
               cwd=work, capture_output=True, text=True, check=True)
items = ET.parse(work / 'vo_events.xml').getroot().find(".//list[@name='listLoadedItem']")


def field(element, name, default=None):
    found = element.find(".//field[@name='" + name + "']")
    return int(found.get('value')) if found is not None else default


nodes = {}
for element in items:
    uid = field(element, 'ulID')
    node = {'type': element.get('name'), 'parent': field(element, 'DirectParentID'), 'gain': 0.0}
    params = element.find(".//object[@name='NodeInitialParams']")
    if params is not None:
        bundle = params.find("object[@name='AkPropBundle<AkPropValue,unsigned char>']")
        for prop in bundle.iter('object'):
            pid = prop.find("field[@name='pID']")
            if pid is not None and ('[Volume]' in pid.get('valuefmt', '') or '[MakeUpGain]' in pid.get('valuefmt', '')):
                node['gain'] += float(prop.find("field[@name='pValue']").get('value'))
    if node['type'] == 'CAkSound':
        node['source'] = field(element, 'sourceID')
    elif node['type'] == 'CAkRanSeqCntr':
        node['mode'] = field(element, 'eMode'); node['random'] = field(element, 'eRandomMode')
        node['avoid'] = field(element, 'wAvoidRepeatCount')
        node['playlist'] = [int(f.get('value')) for f in element.iter('field') if f.get('name') == 'ulPlayID']
    elif node['type'] == 'CAkSwitchCntr':
        node['group'] = field(element, 'ulGroupID'); node['default'] = field(element, 'ulDefaultSwitch')
        node['switches'] = {field(package_, 'ulSwitchID'): [int(f.get('value')) for f in package_.iter('field') if f.get('name') == 'NodeID']
                            for package_ in element.iter('object') if package_.get('name') == 'CAkSwitchPackage'}
    elif node['type'] == 'CAkEvent':
        node['actions'] = [int(f.get('value')) for f in element.iter('field') if f.get('name') == 'ulActionID']
    elif node['type'] == 'CAkActionPlay':
        node['target'] = field(element, 'idExt')
    nodes[uid] = node


def path_gain(uid):
    # Volume + make-up gain of the sound and every container above it (dB).
    total = 0.0
    while uid in nodes:
        total += nodes[uid]['gain']; uid = nodes[uid]['parent']
    return total


def sounds(uid, switches):
    node = nodes[uid]
    if node['type'] == 'CAkSound':
        return [uid]
    if node['type'] == 'CAkSwitchCntr':
        value = switches.get(node['group'], node['default'])
        return [s for child in node['switches'].get(value, []) for s in sounds(child, switches)]
    if node['type'] == 'CAkRanSeqCntr':
        found = []
        for child in node['playlist']:
            for s in sounds(child, switches):
                if s not in found: found.append(s)
        return found
    raise AssertionError('Unexpected node ' + node['type'])


def event_target(name):
    event = nodes[fnv(name)]
    plays = [nodes[a]['target'] for a in event['actions'] if nodes[a]['type'] == 'CAkActionPlay']
    assert len(plays) == 1, name
    return plays[0]


def container(uid, switches):
    # The random/sequence container that picks the line (the event's own selection rules).
    node = nodes[uid]
    while node['type'] == 'CAkSwitchCntr':
        children = node['switches'].get(switches.get(node['group'], node['default']), [])
        assert len(children) == 1
        uid = children[0]; node = nodes[uid]
    return uid, node


data = package.read_bytes()
assert data[:4] == b'r3d2'
version, count = struct.unpack_from('<II', data, 4)
media = {}
for i in range(count):
    offset = struct.unpack_from('<I', data, 12 + i * 4)[0]
    start, size, name_length = struct.unpack_from('<III', data, offset)
    name = data[offset + 12:offset + 12 + name_length * 2].decode('utf-16le').rstrip('\0')
    media[int(name.split('.')[0])] = (start, size)

# Reference: the R lines (same bank) and the gain they were converted with.
r_sounds = sounds(event_target(R_EVENT), {})
r_gains = {round(path_gain(s), 3) for s in r_sounds}
assert len(r_sounds) == 8 and len(r_gains) == 1, 'R voice container changed; review before importing'
r_reference = json.loads((output / 'Sources.json').read_text(encoding='utf-8'))
assert sorted(v['media_id'] for v in r_reference['variants']) == sorted(nodes[s]['source'] for s in r_sounds)
r_db = r_gains.pop(); r_linear = r_reference['pcm_conversion']['common_gain']


def decode(media_id):
    start, size = media[media_id]
    wem = work / (str(media_id) + '.wem'); wem.write_bytes(data[start:start + size])
    temporary = work / (str(media_id) + '.float.wav')
    subprocess.run([str(args.vgmstream.resolve()), '-i', '-W', '4', '-o', str(temporary), str(wem)], check=True, capture_output=True)
    raw_bytes = temporary.read_bytes(); offset = 12; samples = None
    while offset + 8 <= len(raw_bytes):
        tag = raw_bytes[offset:offset + 4]; length = struct.unpack_from('<I', raw_bytes, offset + 4)[0]
        if tag == b'fmt ':
            fmt, channels, rate = struct.unpack_from('<HHI', raw_bytes, offset + 8)
            assert fmt == 3, 'Expected IEEE float conversion'
        if tag == b'data':
            samples = array.array('f'); samples.frombytes(raw_bytes[offset + 8:offset + 8 + length])
        offset += 8 + length + length % 2
    assert samples and channels in (1, 2)
    if channels == 2:
        # One Move2DFirst line is stored as near-identical stereo; the voices play as one mono source.
        samples = array.array('f', ((samples[i] + samples[i + 1]) / 2 for i in range(0, len(samples), 2)))
    return rate, samples, channels


META = '''AudioImporter:
  externalObjects: {}
  serializedVersion: 7
  defaultSettings:
    serializedVersion: 2
    loadType: 1
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: 1
    quality: 0.7
    conversionMode: 0
    preloadAudioData: 1
  platformSettingOverrides: {}
  forceToMono: 1
  normalize: 0
  ambisonic: 0
  loadInBackground: 0
  userData:
  assetBundleName:
  assetBundleVariant:
'''
manifest = {'language': 'en_US', 'source': str(args.source),
            'banks': [{'file': p.name, 'sha256': hashlib.sha256(p.read_bytes()).hexdigest()} for p in (bank, package)],
            'reference': {'event': R_EVENT, 'path_gain_db': r_db, 'common_gain': r_linear,
                          'note': 'Each clip = decoded media x common_gain x 10^((path_gain_db - R path_gain_db)/20).'},
            'cues': []}
peak = 0
for prefix, event, switches, part, expected in CUES:
    target = event_target(event)
    if part is None:
        chosen = sounds(target, switches); picker_id, picker = container(target, switches)
    else:
        sequence_id, sequence = container(target, switches)
        assert sequence['mode'] == 1 and len(sequence['playlist']) == 8, 'Q passive-max sequence changed'
        entries = sequence['playlist'][part[1]:part[2]]
        chosen = [s for entry in entries for s in sounds(entry, switches)]
        picker_id, picker = (sequence_id, sequence) if part[1] == 0 else container(entries[0], switches)
    assert len(chosen) == expected, (prefix, len(chosen))
    entry = {'prefix': prefix, 'event': event, 'event_id': fnv(event),
             'switches': {str(k): v for k, v in switches.items()},
             'selection': 'sequence' if picker['mode'] == 1 else ('shuffle' if picker['random'] == 1 else 'random'),
             'avoid_repeat': picker['avoid'], 'clips': []}
    for index, sound in enumerate(chosen, 1):
        media_id = nodes[sound]['source']
        rate, samples, channels = decode(media_id)
        gain_db = path_gain(sound)
        gain = r_linear * 10 ** ((gain_db - r_db) / 20)
        clip_peak = max(abs(s) for s in samples) * gain; peak = max(peak, clip_peak)
        assert clip_peak <= 1, (prefix, media_id, clip_peak)
        path = output / (prefix + '_' + str(index) + '.wav')
        pcm = array.array('h', (round(s * gain * 32767) for s in samples))
        with wave.open(str(path), 'wb') as wav:
            wav.setnchannels(1); wav.setsampwidth(2); wav.setframerate(rate); wav.writeframes(pcm.tobytes())
        entry['clips'].append({'file': path.name, 'media_id': media_id, 'seconds': len(samples) / rate,
                               'source_channels': channels, 'path_gain_db': gain_db, 'peak': round(clip_peak, 4),
                               'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
        meta = Path(str(path) + '.meta')
        if not meta.exists():
            guid = hashlib.md5(('MidLane/IreliaVoice/' + path.name).encode('utf-8')).hexdigest()
            meta.write_text('fileFormatVersion: 2\nguid: ' + guid + '\n' + META, encoding='utf-8')
    manifest['cues'].append(entry)
manifest['max_output_peak'] = round(peak, 4)
target = raw / 'VoiceSources.json'
target.write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
meta = Path(str(target) + '.meta')
if not meta.exists():
    meta.write_text('fileFormatVersion: 2\nguid: ' + hashlib.md5(b'MidLane/IreliaVoice/VoiceSources.json').hexdigest() +
                    '\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',
                    encoding='utf-8')
print('Imported', sum(len(c['clips']) for c in manifest['cues']), 'voice lines in', len(manifest['cues']),
      'groups; highest peak', round(peak, 3))
