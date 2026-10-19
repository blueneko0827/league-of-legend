using System.Collections.Generic;
using UnityEngine;

namespace MidLane
{
    public sealed class MelVolley : MonoBehaviour
    {
        LaneWorld world; MelAbilities mel; Combatant source;
        Vector3 origin, center;
        int rank, count, next, castId, life;
        float ap, castAt;
        GameObject telegraph;
        GameObject[] missiles;
        readonly HashSet<Combatant> hit = new HashSet<Combatant>();
        public Vector3 Center { get { return center; } }
        public int ResolvedMissiles { get { return next; } }
        public void Initialize(LaneWorld lane, MelAbilities owner, Vector3 point)
        {
            world = lane; mel = owner; source = mel.Actor; origin = source.Position; center = point;
            rank = source.Progress.Rank(0); ap = source.AbilityPower; castId = source.SpellCastId; life = source.Deaths;
            count = MelRules.QMissiles(rank); castAt = Time.time + MelRules.QCastTime;
            telegraph = MelVisuals.Ring(world, transform, "Radiant Volley · 250 AoE", MelRules.QAreaRadius, new Color(1, .65f, .2f));
            telegraph.transform.position = center;
            missiles = new GameObject[count];
            for (int i = 0; i < count; i++)
            {
                missiles[i] = world.Shape("Radiant Volley · missile " + i, PrimitiveType.Sphere, transform,
                    origin + Vector3.up * 1.3f, Vector3.one * .16f, new Color(1, .85f, .35f));
                missiles[i].SetActive(false);
            }
        }
        // Reconstructed spread stays inside the 250-unit target area. Each flare has its own 200-unit blast.
        public static Vector3 ImpactOffset(int index, int count)
        {
            float radius = (MelRules.QAreaRadius - MelRules.QExplosionRadius) * index / Mathf.Max(1, count - 1);
            float angle = index * 2.39996323f;
            return new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
        }
        public Combatant Owner { get { return source; } }
        float LaunchAt(int index) { return castAt + index * MelRules.QDuration / Mathf.Max(1,count-1); }
        float ArrivalAt(int index) { return LaunchAt(index) + Vector3.Distance(origin,center+ImpactOffset(index,count)) / MelRules.QSpeed; }
        void Update()
        {
            if (world.Paused) return;
            if (source == null || !source.gameObject.activeInHierarchy || world.RoundOver || Time.time < castAt && (!source.Alive || source.Deaths != life)) { Destroy(gameObject); return; }
            // Visual travel uses the same launch/arrival clocks as damage; no instant full-length tracer.
            for (int i = next; i < count; i++)
            {
                float launch = LaunchAt(i), arrival = ArrivalAt(i);
                missiles[i].SetActive(Time.time >= launch);
                float t = arrival > launch ? Mathf.Clamp01((Time.time - launch) / (arrival - launch)) : 1;
                missiles[i].transform.position = Vector3.Lerp(origin + Vector3.up * 1.3f,
                    center + ImpactOffset(i,count) + Vector3.up * .1f, t);
            }
            while (next < count && Time.time >= ArrivalAt(next))
            {
                missiles[next].SetActive(false);
                Vector3 point = center + ImpactOffset(next++, count);
                var ring = MelVisuals.Ring(world, transform, "Flare · 200 radius", MelRules.QExplosionRadius, new Color(1, .8f, .4f));
                ring.transform.position = point; Destroy(ring, .12f);
                foreach (Combatant target in world.Units)
                {
                    if (!source.ValidEnemy(target) || target.Kind == UnitKind.Tower) continue;
                    float reach = MelRules.QExplosionRadius + target.Radius;
                    if ((target.Position - point).sqrMagnitude > reach * reach) continue;
                    source.DealAbilityDamage(target, MelRules.QDamage(rank, ap, hit.Add(target)), DamageType.Magic, castId, 0);
                }
            }
            if (next == count) { telegraph.SetActive(false); enabled = false; Destroy(gameObject, .15f); }
        }
    }
}
