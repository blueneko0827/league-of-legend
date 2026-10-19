using System.Collections.Generic;
using UnityEngine;

namespace MidLane
{
    public sealed class MelEclipse : MonoBehaviour
    {
        struct Victim { public Combatant Unit; public int Life; }
        LaneWorld world; MelAbilities mel; Combatant source;
        float at, ap; int rank, castId, life;
        readonly List<Victim> victims = new List<Victim>();
        public void Initialize(LaneWorld lane, MelAbilities owner)
        {
            world = lane; mel = owner; source = mel.Actor;
            at = Time.time + MelRules.RCastTime; rank = source.Progress.Rank(3); ap = source.AbilityPower; castId = source.SpellCastId; life = source.Deaths;
            foreach (Combatant unit in world.Units)
                if (source.ValidEnemy(unit) && mel.OverwhelmStacks(unit) > 0)
                {
                    victims.Add(new Victim { Unit = unit, Life = unit.Deaths });
                    var ring = MelVisuals.Ring(world, transform, "Golden Eclipse · marked", unit.Radius + .25f, new Color(1, .85f, .3f));
                    ring.transform.SetParent(unit.transform, false); Destroy(ring, MelRules.RCastTime);
                }
        }
        public Combatant Owner { get { return source; } }
        void Update()
        {
            if (world.Paused) return;
            if (source == null || !source.gameObject.activeInHierarchy || !source.Alive || source.Deaths != life || world.RoundOver) { Destroy(gameObject); return; }
            if (Time.time < at) return;
            foreach (Victim victim in victims)
            {
                Combatant unit = victim.Unit;
                if (!source.ValidEnemy(unit) || victim.Life != unit.Deaths) continue;
                int stacks = mel.OverwhelmStacks(unit);
                if (stacks == 0) continue;
                world.FlashLine(unit.Position + Vector3.up * 4, unit.Position + Vector3.up * .2f, new Color(1, .85f, .3f));
                source.DealAbilityDamage(unit, MelRules.RDamage(rank, stacks, ap) * (unit.Kind == UnitKind.Minion ? .5f : 1), DamageType.Magic, castId, 3);
            }
            Destroy(gameObject);
        }
    }
}
