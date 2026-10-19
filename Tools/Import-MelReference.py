"""Freeze Mel's PC reference data; consumes already downloaded patch-pinned files.

Data Dragon does not expose the numeric spell calculations, and reports AD growth as 0.
The client record supplies those fields. Never substitute a 'latest' download silently.
"""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--data-dragon', type=Path, default=ROOT / 'TestResults/Mel-16.20.1.json')
parser.add_argument('--client', type=Path, default=ROOT / 'TestResults/Reference-mel.json')
args = parser.parse_args()
dd = json.loads(args.data_dragon.read_text(encoding='utf-8-sig'))
client = json.loads(args.client.read_text(encoding='utf-8-sig'))
assert dd['version'] == '16.20.1'
champion = dd['data']['Mel']
spells = {}
for name in ['Passive', 'Q', 'W', 'E', 'R']:
    key = f'Characters/Mel/Spells/Mel{name}Ability/Mel{name}'
    record = client[key]['mSpell']
    spells[name] = {
        'values': {value['name']: value.get('values', [0] * 7) for value in record.get('DataValues', [])},
        'calculations': record.get('mSpellCalculations', {}),
        'castTime': record.get('mCastTime', record.get('spellCastTime', 0)),
    }
    if name != 'Passive':
        dds = champion['spells']['QWER'.index(name)]
        spells[name].update(cooldown=dds['cooldown'], cost=dds['cost'], castRange=dds['range'])
projectiles = {}
for label, key in {
    'basic': 'Characters/Mel/Spells/MelBasicAttack',
    'passive': 'Characters/Mel/Spells/MelPassiveAbility/MelPassiveBonusMissileLeft',
    'q': 'Characters/Mel/Spells/MelQAbility/MelQMissile1',
    'e': 'Characters/Mel/Spells/MelEAbility/MelE',
}.items():
    record = client[key]['mSpell']
    movement = record['mMissileSpec']['movementComponent']
    projectiles[label] = {
        'record': key, 'movementType': movement['__type'], 'movementSpeed': movement['mSpeed'],
        'legacyMissileSpeed': record.get('missileSpeed'),
    }
result = {
    'patch': dd['version'],
    'dataDragonUrl': 'https://ddragon.leagueoflegends.com/cdn/16.20.1/data/en_US/champion/Mel.json',
    'clientUrl': 'https://raw.communitydragon.org/16.20/game/data/characters/mel/mel.bin.json',
    'dataDragonSHA256': hashlib.sha256(args.data_dragon.read_bytes()).hexdigest(),
    'clientSHA256': hashlib.sha256(args.client.read_bytes()).hexdigest(),
    'stats': champion['stats'],
    'attackDamageGrowthFromClient': client['Characters/Mel/CharacterRecords/Root']['damagePerLevelModifiable']['baseValue'],
    'abilities': spells,
    'projectiles': projectiles,
    'notes': [
        'DataValues arrays include a rank-zero element; mana and Data Dragon arrays do not.',
        'W rank 3 is 33 seconds in both pinned client and Data Dragon; 26.8 patch notes say 32.',
        'Use mMissileSpec movement speed, not legacy missileSpeed (Q 3800 vs 3000; passive 5000 vs 3000).',
        'Server-only scripts are not in Data Dragon: spread pattern, aura exposure and reflection routing are reconstructed.',
    ],
}
destination = ROOT / 'Assets/Resources/LeagueData/MelReference.json'
destination.write_text(json.dumps(result, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
print(f'Wrote {destination.name}: {dd["version"]}, passive + 4 spells')
