"""Render the official crit and passive Irelia sound events with wwiser + vgmstream.

python Tools/Import-IreliaPassiveAudio.py <base-bank-folder> --wwiser <wwiser.pyz> --vgmstream <vgmstream-cli>
Adds CritHit/PSparkles/PMaxSparkles/PMax WAVs next to the existing Irelia clips,
with the same common gain as the existing attack/Q clips (Assets/SourceAudio/.../Sources.json),
so the new cues sit at the original relative level. Existing clips are not touched.
"""
import argparse
import array
import hashlib
import json
import re
from pathlib import Path
import shutil
import struct
import subprocess
import sys
import wave

parser = argparse.ArgumentParser()
parser.add_argument('source', type=Path)
parser.add_argument('--wwiser', type=Path, required=True)
parser.add_argument('--vgmstream', type=Path, required=True)
args = parser.parse_args()
root = Path(__file__).resolve().parents[1]
work = root / 'TestResults/IreliaPassiveAudioImport'
raw = root / 'Assets/SourceAudio/IreliaOfficial'
output = root / 'Assets/Resources/Audio/Irelia'
work.mkdir(parents=True, exist_ok=True)
existing = json.loads((raw / 'Sources.json').read_text(encoding='utf-8'))
gain = existing['pcm_conversion']['common_gain']
for name in ('irelia_base_sfx_audio.bnk', 'irelia_base_sfx_events.bnk'):
    shutil.copy2(args.source / name, work / name)
# Hit material switch: the same default the basic attack hits use.
# Crit cast is not rendered: the simulator rolls the critical strike at impact, not at cast.
groups = [('CritHit', 'CritAttack_OnHit', '1153642577', 4),
          ('PSparkles', 'P_sparkles', None, 12), ('PMaxSparkles', 'P_max_sparkles', None, 7),
          ('PMax', 'P_Max_OnBuffActivate', None, 5)]
events = ['Play_sfx_Irelia_Irelia' + suffix for _, suffix, _, _ in groups]
(work / 'wwnames.txt').write_text('\n'.join(events) + '\n', encoding='utf-8')
subprocess.run([sys.executable, str(args.wwiser.resolve()), str(work / 'irelia_base_sfx_audio.bnk'),
                str(work / 'irelia_base_sfx_events.bnk'), '-d', 'none', '-g', '-gra', '-gd', '-go', str(work / 'txtp'),
                '-nl', str(work / 'wwnames.txt'), '-gf'] + events, capture_output=True, text=True, check=True)
for recipe in (work / 'txtp').glob('*.txtp'):
    # wwiser writes the bank path relative to its own working directory; point it at the local copy.
    text = recipe.read_text(encoding='utf-8')
    text = re.sub(r'^(\s*)[^\r\n#]*irelia_base_sfx_audio\.bnk', r'\1../irelia_base_sfx_audio.bnk', text, flags=re.MULTILINE)
    recipe.write_text(text, encoding='utf-8')
manifest = {'source': str(args.source), 'gain': gain, 'events': []}
for group, suffix, switch, count in groups:
    event = 'Play_sfx_Irelia_Irelia' + suffix
    candidates = sorted(p for p in (work / 'txtp').glob(event + '*.txtp')
                        if switch is None or '=' + switch + ']' in p.name)
    entry = {'group': group, 'event': event, 'default_switch': switch, 'clips': []}
    for index in range(1, count + 1):
        txtp = next(p for p in candidates if '{r' + str(index) + '}' in p.name)
        target = output / (group + '_' + str(index) + '.wav')
        temporary = work / (target.stem + '.float.wav')
        subprocess.run([str(args.vgmstream.resolve()), '-i', '-W', '4', '-o', str(temporary), str(txtp)],
                       check=True, capture_output=True)
        data = temporary.read_bytes(); offset = 12; samples = None
        while offset + 8 <= len(data):
            tag = data[offset:offset + 4]; size = struct.unpack_from('<I', data, offset + 4)[0]
            if tag == b'fmt ':
                fmt, channels, rate = struct.unpack_from('<HHI', data, offset + 8)
                assert fmt == 3, 'Expected IEEE float conversion'
            if tag == b'data':
                samples = array.array('f'); samples.frombytes(data[offset + 8:offset + 8 + size])
            offset += 8 + size + (size % 2)
        assert samples and channels in (1, 2)
        pcm = array.array('h', (round(max(-1, min(1, s * gain)) * 32767) for s in samples))
        with wave.open(str(target), 'wb') as wav:
            wav.setnchannels(channels); wav.setsampwidth(2); wav.setframerate(rate); wav.writeframes(pcm.tobytes())
        entry['clips'].append({'file': target.name, 'seconds': len(samples) / channels / rate, 'recipe': txtp.name,
                               'peak': max(abs(s) for s in samples) * gain,
                               'sha256': hashlib.sha256(target.read_bytes()).hexdigest()})
        meta = Path(str(target) + '.meta')
        if not meta.exists():
            guid = hashlib.md5(('MidLane/IreliaAudio/' + target.name).encode('utf-8')).hexdigest()
            meta.write_text('fileFormatVersion: 2\nguid: ' + guid + '''
AudioImporter:
  externalObjects: {}
  serializedVersion: 7
  defaultSettings:
    serializedVersion: 2
    loadType: 0
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: 0
    quality: 1
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
''', encoding='utf-8')
    manifest['events'].append(entry)
(raw / 'PassiveSources.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
meta = Path(str(raw / 'PassiveSources.json') + '.meta')
if not meta.exists():
    meta.write_text('fileFormatVersion: 2\nguid: ' + hashlib.md5(b'MidLane/IreliaAudio/PassiveSources.json').hexdigest() +
                    '\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',
                    encoding='utf-8')
print('Imported', sum(len(e['clips']) for e in manifest['events']), 'crit/passive WAV variations.')
