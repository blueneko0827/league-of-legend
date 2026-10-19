"""Render official Irelia attack/Q/W/E/R events using wwiser + vgmstream.

python Tools/Import-IreliaAudio.py <base-bank-folder> --wwiser <wwiser.pyz> --vgmstream <vgmstream-cli.exe>
The tools remain under TestResults; Unity only needs the resulting WAV files.
"""
import argparse
import array
import hashlib
import json
from pathlib import Path
import re
import shutil
import struct
import subprocess
import sys
import uuid
import wave

parser = argparse.ArgumentParser()
parser.add_argument('source', type=Path)
parser.add_argument('--wwiser', type=Path, required=True)
parser.add_argument('--vgmstream', type=Path, required=True)
args = parser.parse_args()
root = Path(__file__).resolve().parents[1]
work = root / 'TestResults/IreliaAudioImport'
raw = root / 'Assets/SourceAudio/IreliaOfficial'
output = root / 'Assets/Resources/Audio/Irelia'
for folder in (work, raw, output): folder.mkdir(parents=True, exist_ok=True)
sources = []
for name in ('irelia_base_sfx_audio.bnk', 'irelia_base_sfx_events.bnk'):
    source = args.source / name
    shutil.copy2(source, work / name); shutil.copy2(source, raw / name)
    sources.append({'file': name, 'sha256': hashlib.sha256(source.read_bytes()).hexdigest()})
# Defaults read from the supplied bank's switch containers, not guessed filenames.
groups = [('Attack1Cast', 'BasicAttack_OnCast', '711736988', 4),
          ('Attack2Cast', 'BasicAttack2_OnCast', '711736988', 4),
          ('Attack1Hit', 'BasicAttack_OnHit', '1153642577', 5),
          ('Attack2Hit', 'BasicAttack2_OnHit', '1153642577', 5),
          ('QCast', 'Q_OnCast', None, 7), ('QDash', 'Q_missilelaunch', None, 7),
          ('QHit', 'Q_hit', None, 7),
          ('WCharge', 'W_OnCast', None, 4), ('WCharged', 'WCharged_cast', None, 1),
          ('WRelease', 'W2_OnCast', None, 11), ('WHit', 'W2_hit', None, 2),
          ('WBlock', 'WDefence_block_self', None, 6),
          ('EFirst', 'EMissile_OnMissileCast', None, 10), ('ESecond', 'E2_buffactivate', None, 5),
          ('ELaunch', 'EMissile_OnMissileLaunch', None, 8), ('EConverge', 'ESecondary_OnMissileCast', None, 5),
          ('EHit', 'ESecondary_hit', None, 5), ('EHitMinion', 'ESecondary_hit_minion', None, 8),
          ('RCast', 'R_OnCast', None, 1), ('RLaunch', 'R_onmissilelaunch', None, 1),
          ('RHit', 'R_OnHit_champ', None, 1), ('RHitMinion', 'R_OnHit_minion', None, 8),
          ('RWallHit', 'R2_hit', None, 7)]
events = ['Play_sfx_Irelia_Irelia' + suffix for _, suffix, _, _ in groups]
(work / 'wwnames.txt').write_text('\n'.join(events) + '\n', encoding='utf-8')
cmd = [sys.executable, str(args.wwiser.resolve()), str(work / sources[0]['file']), str(work / sources[1]['file']),
       '-d', 'none', '-g', '-gra', '-gd', '-go', str(work / 'txtp'), '-nl', str(work / 'wwnames.txt'), '-gf'] + events
result = subprocess.run(cmd, capture_output=True, text=True, check=True)
(work / 'wwiser.log').write_text(result.stdout + result.stderr, encoding='utf-8')
for recipe in (work / 'txtp').glob('*.txtp'):
    # Resolve the local copy explicitly: wwiser may discover the preserved bank
    # under Assets and emit a path relative to its own working directory.
    text = recipe.read_text(encoding='utf-8')
    text = re.sub(r'^(\s*)[^\r\n#]*irelia_base_sfx_audio\.bnk', r'\1../irelia_base_sfx_audio.bnk', text, flags=re.MULTILINE)
    recipe.write_text(text, encoding='utf-8')
manifest = {'source': str(args.source), 'banks': sources, 'events': [],
            'tools': {'wwiser': 'https://github.com/bnnm/wwiser/releases/tag/v20260808',
                      'vgmstream': 'https://github.com/vgmstream/vgmstream/releases/tag/r2117'},
            'notes': ['Default switch variants are used for basic casts and hit materials.',
                      'Attack 1 and 2 share audio in the supplied base bank; both event mappings are retained.',
                      'Q cast preserves both layers. Seven deterministic paired variants are rendered.',
                      'Original mixer buses/init bank are absent; Unity sets the final playback volume.',
                      'R champion impact already contains the wall activation layer; it is not played twice.',
                      'W charge is stopped on release/cancel. Full-charge cue follows the gameplay threshold.',
                      'Q_Hit_reset references object 223180206 absent from the supplied base bank; no replacement sound is invented.']}
rendered = []
for group, suffix, switch, count in groups:
    event = 'Play_sfx_Irelia_Irelia' + suffix
    candidates = sorted(p for p in (work / 'txtp').glob(event + '*.txtp')
                        if (p.stem == event or p.name.startswith(event + ' '))
                        and (switch is None or '=' + switch + ']' in p.name))
    assert candidates, event
    entry = {'group': group, 'event': event, 'default_switch': switch, 'clips': []}
    for index in range(1, count + 1):
        if group == 'QCast':
            # Each of the two random layers contains seven original sounds.
            # Preserve their relative gain and choose a paired variant reproducibly.
            text = candidates[0].read_text(encoding='utf-8').replace('-R7>1', '-R7>' + str(index))
            txtp = work / 'txtp' / ('QCastRendered' + str(index) + '.txtp')
            txtp.write_text(text, encoding='utf-8')
        elif count == 1:
            txtp = candidates[0]
        else:
            txtp = next(p for p in candidates if '{r' + str(index) + '}' in p.name)
        target = output / (group + '_' + str(index) + '.wav')
        temporary = work / (target.stem + '.float.wav')
        subprocess.run([str(args.vgmstream.resolve()), '-i', '-W', '4', '-o', str(temporary), str(txtp)], check=True, capture_output=True)
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
        clip = {'file': target.name, 'seconds': len(samples) / channels / rate, 'recipe': txtp.name}
        entry['clips'].append(clip); rendered.append((target, channels, rate, samples, clip))
    manifest['events'].append(entry)
basic_peak = max(abs(sample) for target, _, _, samples, _ in rendered
                 if target.stem.startswith(('Attack', 'Q')) for sample in samples)
ability_peak = max(abs(sample) for target, _, _, samples, _ in rendered
                   if not target.stem.startswith(('Attack', 'Q')) for sample in samples)
gain = min(1, .95 / basic_peak)
ability_gain = min(gain, .95 / ability_peak)
for target, channels, rate, samples, clip in rendered:
    clip_gain = gain if target.stem.startswith(('Attack', 'Q')) else ability_gain
    pcm = array.array('h', (round(max(-1, min(1, sample * clip_gain)) * 32767) for sample in samples))
    with wave.open(str(target), 'wb') as wav:
        wav.setnchannels(channels); wav.setsampwidth(2); wav.setframerate(rate); wav.writeframes(pcm.tobytes())
    clip['sha256'] = hashlib.sha256(target.read_bytes()).hexdigest()
manifest['pcm_conversion'] = {'float_peak': basic_peak, 'common_gain': gain, 'wer_float_peak': ability_peak,
                              'wer_common_gain': ability_gain, 'max_output_peak': .95,
                              'note': 'Preserve existing attack/Q levels; common W/E/R gain prevents PCM clipping.'}
(raw / 'Sources.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
# Complete, current-version audio import metadata, avoiding a dependency on Wwise in Unity.
for p in output.glob('*.wav'):
    meta = Path(str(p) + '.meta')
    if not meta.exists():
        meta.write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid4().hex + '''
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
for folder in [root / 'Assets/SourceAudio', raw, root / 'Assets/Resources/Audio', output]:
    meta = Path(str(folder) + '.meta')
    if not meta.exists(): meta.write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid4().hex + '\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n', encoding='utf-8')
for p in raw.iterdir():
    if p.suffix == '.meta': continue
    meta = Path(str(p) + '.meta')
    if not meta.exists(): meta.write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid4().hex + '\nDefaultImporter:\n  externalObjects: {}\n', encoding='utf-8')
print('Imported', sum(len(e['clips']) for e in manifest['events']), 'native WAV variations for attacks and Q/W/E/R.')
