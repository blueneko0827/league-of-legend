using UnityEngine;
namespace MidLane
{
    public sealed partial class Combatant
    {
        public RuneEffects Runes { get; private set; }
        internal void RefreshRuneStats()
        {
            if(Kind!=UnitKind.Champion||Progress==null||Dummy!=null) return;
            // Virtual Manaflow adds capacity, not an immediate mana refill.
            float resource=Resource; ApplyChampionLevel(); Resource=Mathf.Min(resource,MaxResource);
        }
        internal bool GrantRuneSkillPoint()
        {
            if(Runes==null||Level>=18||!Progress.GrantBonusSkillPoint(Archetype)) return false;
            Runes.SkillElixirConsumed=true; ApplyChampionLevel(); return true;
        }
    }
}
