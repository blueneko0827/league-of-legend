using UnityEngine;

namespace MidLane
{
    public sealed class BasicProjectile : MonoBehaviour
    {
        LaneWorld world;
        Combatant source, target;
        float damage, speed;
        int targetDeaths;
        public Combatant Source { get { return source; } }
        public Combatant Target { get { return target; } }
        public float RawDamage { get { return damage; } }
        public float TimeToImpact { get { return target == null || target.Deaths != targetDeaths ? float.PositiveInfinity : Vector3.Distance(transform.position, target.Position + Vector3.up * 0.65f) / speed; } }
        public void Initialize(LaneWorld lane, Combatant owner, Combatant victim, float raw)
        {
            world = lane; source = owner; target = victim; damage = raw;
            targetDeaths = victim.Deaths;
            world.BasicProjectiles.Add(this);
            speed = owner.MinionType == MinionType.Cannon ? 12 : 6.5f;
            transform.position = source.Position + Vector3.up * 0.65f;
            float size = owner.MinionType == MinionType.Cannon ? 0.22f : 0.12f;
            world.Shape("Proyectil", PrimitiveType.Sphere, transform, Vector3.zero, Vector3.one * size, source.Color);
        }
        void OnDestroy() { if (world != null) world.BasicProjectiles.Remove(this); }
        void Update()
        {
            if (world.Paused || world.RoundOver) return;
            if (source == null || !source.ValidEnemy(target) || target.Deaths != targetDeaths) { Destroy(gameObject); return; }
            Vector3 aim = target.Position + Vector3.up * 0.65f;
            transform.position = Vector3.MoveTowards(transform.position, aim, speed * Time.deltaTime);
            if (Vector3.Distance(transform.position, aim) > 0.05f) return;
            if (target.Mel != null && target.Mel.ReflectTargeted(source, damage, DamageType.Physical)) { Destroy(gameObject); return; }
            target.TakeDamage(source.BasicDamageAgainst(target, damage), source); Destroy(gameObject);
        }
    }
}
