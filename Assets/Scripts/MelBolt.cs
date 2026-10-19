using UnityEngine;

namespace MidLane
{
    public sealed class MelBolt : MonoBehaviour
    {
        LaneWorld world; Combatant source, target;
        float raw, launchAt; int slot, castId, life;
        DamageType type; string label; bool basic;
        public static MelBolt Create(LaneWorld world, Combatant source, Combatant target, float raw, DamageType type,
            int slot, int castId, float delay, string label, bool basic = false)
        {
            var bolt = new GameObject("Mel · " + label).AddComponent<MelBolt>(); bolt.transform.SetParent(source.transform.parent);
            bolt.world = world; bolt.source = source; bolt.target = target; bolt.raw = raw; bolt.type = type;
            bolt.slot = slot; bolt.castId = castId; bolt.launchAt = Time.time + delay; bolt.label = label; bolt.life = target.Deaths; bolt.basic = basic;
            bolt.transform.position = source.Position + Vector3.up * .8f;
            if (source.Mel != null) source.Mel.Projectiles.Add(bolt);
            world.Shape(label, PrimitiveType.Sphere, bolt.transform, Vector3.zero, Vector3.one * (basic ? .2f : .12f), new Color(1, .8f, .3f));
            return bolt;
        }
        public Combatant Owner { get { return source; } }
        public Combatant Target { get { return target; } }
        public float RawDamage { get { return raw; } }
        public DamageType Type { get { return type; } }
        public bool IsBasic { get { return basic; } }
        public float TimeToImpact { get { return target == null ? float.PositiveInfinity : Mathf.Max(0,launchAt-Time.time) +
            Vector3.Distance(transform.position,target.Position+Vector3.up*.8f)/(basic?MelRules.BasicMissileSpeed:MelRules.PassiveMissileSpeed); } }
        void OnDestroy() { if (source != null && source.Mel != null) source.Mel.Projectiles.Remove(this); }
        void Update()
        {
            if (world.Paused) return;
            if (source == null || !source.ValidEnemy(target) || target.Deaths != life || world.RoundOver) { Destroy(gameObject); return; }
            if (Time.time < launchAt) return;
            Vector3 aim = target.Position + Vector3.up * .8f;
            transform.position = Vector3.MoveTowards(transform.position, aim, (basic ? MelRules.BasicMissileSpeed : MelRules.PassiveMissileSpeed) * Time.deltaTime);
            if (Vector3.Distance(transform.position, aim) > .04f) return;
            if (target.Mel != null && target.Mel.ReflectTargeted(source, raw, type, castId)) { Destroy(gameObject); return; }
            if (basic) source.ApplyAttackHit(target, raw);
            else if (slot >= 0) source.DealAbilityDamage(target, raw, type, castId, slot);
            else
            {
                target.TakeDamage(raw, source, type, hitLabel: label);
                if (source.Mel != null) source.Mel.RecordHit(target);
            }
            Destroy(gameObject);
        }
    }
}
