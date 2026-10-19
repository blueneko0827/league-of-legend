using System.Collections.Generic;
using UnityEngine;

namespace MidLane
{
    public sealed class MelSnare : MonoBehaviour
    {
        sealed class Burn { public int Life; public float Until, NextTick; }
        LaneWorld world; Combatant source;
        Vector3 origin, direction;
        float castAt, lastTick, ap;
        int rank, castId, life;
        GameObject orb, aura;
        readonly HashSet<Combatant> rooted = new HashSet<Combatant>();
        readonly Dictionary<Combatant, Burn> burns = new Dictionary<Combatant, Burn>();
        readonly List<Combatant> burning = new List<Combatant>();
        public Vector3 Head { get { return At(Time.time); } }
        public float AuraRadius { get { return MelRules.EAuraRadius * Mathf.Clamp01((Time.time - castAt) / MelRules.EAuraGrowthTime); } }
        Vector3 At(float time) { return origin + direction * Mathf.Clamp((time - castAt) * MelRules.ESpeed, 0, MelRules.ERange); }
        public void Initialize(LaneWorld lane, MelAbilities mel, Vector3 heading)
        {
            world = lane; source = mel.Actor; origin = source.Position; direction = heading;
            rank = source.Progress.Rank(2); ap = source.AbilityPower; life = source.Deaths; castId = source.SpellCastId;
            castAt = lastTick = Time.time + MelRules.ECastTime;
            orb = world.Shape("Solar Snare · 70 core", PrimitiveType.Sphere, transform, origin + Vector3.up * .65f,
                Vector3.one * MelRules.ECoreRadius * 2, new Color(1, .9f, .3f));
            aura = MelVisuals.Ring(world, transform, "Solar Snare · 230 aura", MelRules.EAuraRadius, new Color(1, .65f, .2f));
            orb.SetActive(false); aura.SetActive(false);
        }
        public Combatant Owner { get { return source; } }
        public bool HasRooted(Combatant target) { return target != null && rooted.Contains(target); }
        void Update()
        {
            if (world.Paused) return;
            if (source == null || !source.gameObject.activeInHierarchy || world.RoundOver || Time.time < castAt && (!source.Alive || source.Deaths != life)) { Destroy(gameObject); return; }
            if (Time.time < castAt) return;
            float end = castAt + MelRules.ERange / MelRules.ESpeed + MelRules.EEndLinger;
            orb.SetActive(Time.time <= end); aura.SetActive(Time.time <= end);
            orb.transform.position = Head + Vector3.up * .65f;
            aura.transform.position = Head; aura.transform.localScale = Vector3.one * Mathf.Clamp01((Time.time - castAt) / MelRules.EAuraGrowthTime);
            // Small swept steps keep the narrow core and aura exposure stable at low frame rates.
            while (lastTick < Time.time)
            {
                float next = Mathf.Min(Time.time, lastTick + .025f);
                Vector3 from = At(lastTick), to = At(next);
                if (lastTick <= end)
                {
                    float radius = MelRules.EAuraRadius * Mathf.Clamp01((next - castAt) / MelRules.EAuraGrowthTime);
                    foreach (Combatant target in world.Units)
                    {
                        if (!source.ValidEnemy(target) || target.Kind == UnitKind.Tower) continue;
                        float distance = CombatRules.SegmentDistanceSquared(target.Position.x, target.Position.z, from.x, from.z, to.x, to.z);
                        if (!rooted.Contains(target) && distance <= Mathf.Pow(MelRules.ECoreRadius + target.Radius, 2))
                        {
                            rooted.Add(target);
                            source.DealAbilityDamage(target, MelRules.EDamage(rank, ap) * (target.Kind == UnitKind.Minion ? .5f : 1), DamageType.Magic, castId, 2);
                            if (target.Alive) target.Root(MelRules.ERoot);
                        }
                        if (!target.Alive || distance > Mathf.Pow(radius + target.Radius, 2)) continue;
                        target.Slow(1 - MelRules.ESlow, .25f);
                        Burn burn;
                        if (!burns.TryGetValue(target, out burn) || burn.Life != target.Deaths)
                        { burn = new Burn { Life = target.Deaths, NextTick = next }; burns[target] = burn; }
                        burn.Until = next + MelRules.EBurnDuration;
                    }
                }
                burning.Clear(); burning.AddRange(burns.Keys);
                foreach (Combatant target in burning)
                {
                    Burn burn = burns[target];
                    if (!source.ValidEnemy(target) || target.Deaths != burn.Life) { burns.Remove(target); continue; }
                    while (burn.NextTick <= next && burn.NextTick < burn.Until)
                    {
                        source.DealAbilityDamage(target, MelRules.ETickDamage(rank, ap) * (target.Kind == UnitKind.Minion ? .5f : 1), DamageType.Magic, castId, 2);
                        burn.NextTick += MelRules.ETickInterval;
                    }
                    if (next >= burn.Until) burns.Remove(target);
                }
                lastTick = next;
            }
            if (Time.time > end && burns.Count == 0) Destroy(gameObject);
        }
    }
}
