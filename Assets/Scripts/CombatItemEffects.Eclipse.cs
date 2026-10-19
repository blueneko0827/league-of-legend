using System.Collections.Generic;
using UnityEngine;

namespace MidLane
{
    public sealed partial class CombatItemEffects
    {
        sealed class EclipseHits
        {
            public float Expires;
            public int Count, Life;
            public readonly HashSet<int> Casts = new HashSet<int>();
        }
        readonly Dictionary<Combatant, EclipseHits> eclipseHits = new Dictionary<Combatant, EclipseHits>();
        float eclipseReadyAt, eclipseShield, eclipseShieldUntil;

        public void OnEclipseHit(Combatant target, bool basicAttack, bool abilityHit, int castId, int targetLife)
        {
            ItemDefinition item = Item(6692);
            if (item == null || !self.Alive || target.Kind != UnitKind.Champion || target.Team == self.Team ||
                (!basicAttack && (!abilityHit || castId < 0)) || Time.time < eclipseReadyAt) return;
            EclipseHits hits;
            if (!eclipseHits.TryGetValue(target, out hits) || Time.time > hits.Expires || hits.Life != targetLife)
            {
                hits = new EclipseHits { Expires = Time.time + item.Value("WindowDuration", 2), Life = targetLife };
                eclipseHits[target] = hits;
            }
            // An ability and all its on-hits/ticks count once, while separate basics count individually.
            if (!basicAttack && !hits.Casts.Add(castId)) return;
            if (++hits.Count < 2) return;
            eclipseReadyAt = Time.time + item.Value("Cooldown", 6); eclipseHits.Clear();
            float bonusAD = Mathf.Max(0, self.AttackDamage - self.BaseStats.AttackDamage.At(self.Level));
            eclipseShield = (item.Value("MeleeBaseShield", 150) + bonusAD * item.Value("MeleeBonusADShieldRatio", .4f)) *
                (Ranged ? item.Value("RangedShieldMult", .5f) : 1) * ShieldReceivedMultiplier;
            eclipseShieldUntil = Time.time + item.Value("ShieldDuration", 2);
            if(self.Runes!=null) self.Runes.NotifyShield(eclipseShield,item.Value("ShieldDuration",2));
            if (target.Alive)
                target.TakeDamage(target.MaxHealth * item.Value("MeleePercMaxHP", .08f) *
                    (Ranged ? item.Value("RangedPercMaxHPMult", .625f) : 1), self, DamageType.Physical, hitLabel: "Eclipse");
        }
    }
}
