"""Build the checked-in Swiftplay item catalogue from pinned public Riot/client JSON."""
import json, re
from pathlib import Path
root = Path(__file__).resolve().parents[1]
read = lambda name: json.loads((root / 'TestResults' / name).read_text(encoding='utf-8-sig'))
riot = read('Reference-Items-en_US.json')['data']
client = read('Reference-ClientItems.json')
map_data = read('Reference-Map.json')
description_overrides = json.loads((root / 'Tools/ItemDescriptionOverrides.json').read_text(encoding='utf-8'))
mode = map_data['Maps/Shipping/Map11/Modes/SWIFTPLAY']
allowed = {int(x.split('/')[-1]) for ref in mode['itemLists'] for x in map_data[ref].get('mItems', [])}
# Practice exception: sell the requested mid quest upgrades without their quest lock.
practice_boots = {3172, 3173, 3174}
allowed.update(practice_boots)
allowed.update({1055, 1056, 1086})  # PvP starters are also sold normally in the shop.
rune_elixirs = {2150, 2151, 2152}
allowed.update(rune_elixirs)  # Runtime grants them; they are never shop purchases.
fields = {'health':'mFlatHPPoolMod','mana':'mFlatMPPoolMod','damage':'mFlatPhysicalDamageMod','power':'mFlatMagicDamageMod','armor':'mFlatArmorMod','resist':'mFlatSpellBlockMod','attackSpeed':'mPercentAttackSpeedMod','movement':'mFlatMovementSpeedMod','movementPercent':'mPercentMovementSpeedMod','haste':'mAbilityHasteMod','lifeSteal':'mPercentLifeStealMod','crit':'mFlatCritChanceMod','tenacity':'mPercentTenacityItemMod','hpRegen':'mFlatHPRegenMod','mpRegen':'flatMPRegenMod','hpRegenPercent':'mPercentBaseHPRegenMod','mpRegenPercent':'percentBaseMPRegenMod','penetration':'PhysicalLethality','magicPenetration':'mFlatMagicPenetrationMod','armorPenetrationPercent':'mPercentArmorPenetrationMod','magicPenetrationPercent':'mPercentMagicPenetrationMod'}
implemented = {1055,1056,1086,1043,3044,3057,3078,3153,3091,3072,2051,3112,3177,3184,3047,3111,3006,3172,3173,3174,3009,3158,3020,3089,2003,2031,
               2501,3026,3053,3077,3083,3155,3156,3161,3302,6333,6631,6672,2503,2508,3802,4628,4645,3181,6695,6692,3157,2420,2421}
items = []
for id in sorted(allowed):
    item, data = riot.get(str(id)), client.get('Items/'+str(id))
    if not item or not data: continue
    description = re.sub('<[^>]+>', ' ', item['description'])
    plain_stats = '<passive>' not in item['description'] and '<active>' not in item['description'] and not item.get('requiredAlly')
    result = dict(id=id, name=item['name'], description=re.sub(r'\s+', ' ', description).strip(), price=item['gold']['total'], sell=item['gold']['sell'], components=[int(x) for x in item.get('from',[])], purchasable=item['gold']['purchasable'] and data.get('mItemDataAvailability',{}).get('mInStore',False), boots='Boots' in item['tags'] or id in practice_boots, guardian=id in {2051,3112,3177,3184}, legendary=data.get('epicness',0)>=5 and id not in practice_boots, effectsImplemented=id in implemented or plain_stats, values=[dict(name=x['mName'],value=x.get('mValue',0)) for x in data.get('mDataValues',[])])
    result.update({key:round(data.get(field,0),6) for key,field in fields.items()})
    if id in rune_elixirs:
        result.update(price=0, sell=0, legendary=False, purchasable=False, effectsImplemented=True)
    if id in {1055, 1056, 1086}:
        result['starter'] = True
        result['omnivamp'] = round(data.get('PercentOmnivampMod', 0), 6)
    # Flatten the supplied client level-breakpoint calculations used by these shields.
    if id in {3072, 3173, 3174}:
        calculation = data['mItemCalculations']['OvershieldCalc' if id == 3072 else 'ShieldAmountCalc']['mFormulaParts'][0]
        breakpoint = calculation['mBreakpoints'][0]
        result['values'].extend(dict(name=name, value=value) for name, value in {
            'ShieldBase': calculation['mLevel1Value'], 'ShieldGrowthStartLevel': breakpoint['mLevel'],
            'ShieldPerLevel': breakpoint['mBonusPerLevelAtAndAfter']}.items())
    # Retain the existing full explanations for effects omitted by Data Dragon.
    result['description'] = description_overrides.get(str(id), result['description'])
    # Named client fields omitted from Data Dragon's stats are intentionally retained.
    items.append(result)
out = root / 'Assets/Resources/LeagueData/Items.json'
out.parent.mkdir(parents=True, exist_ok=True)
out.write_text(json.dumps(dict(patch='16.20.1',locale='en_US',items=items),ensure_ascii=False,indent=2),encoding='utf-8')
print(f'Imported {len(items)} Swiftplay items from pinned Riot/client references.')
