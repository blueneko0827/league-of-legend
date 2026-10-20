using System.Collections.Generic;
using UnityEngine;

namespace MidLane
{
    public sealed class TrainingDummy : MonoBehaviour
    {
        public sealed class DamageNotice { public float Amount, StartedAt; public string Label; public DamageType Type; }
        public readonly List<DamageNotice> Notices = new List<DamageNotice>();
        public Combatant Actor { get; private set; }
        public bool Mobile { get; private set; }
        public bool Patrolling { get; private set; }
        // X/Z rectangle, including the actor's full gameplay circle.
        public Rect PatrolBounds { get; private set; }
        public Vector3 PatrolDestination { get; private set; }
        bool hasPatrolDestination;
        public Vector3 Home { get; private set; }
        public TrainingDummyModel Visual { get; private set; }
        public float TotalDamage { get; private set; }
        public float LastDamageAt { get; private set; } = float.NegativeInfinity;
        // Read-only statistics for the Practice Tool style labels (last hit, DPS, total).
        public float LastHitAmount { get; private set; }
        public float SequenceStartedAt { get; private set; }
        public float DamagePerSecond { get { return TotalDamage <= 0 ? 0 : TotalDamage / Mathf.Max(1, LastDamageAt - SequenceStartedAt); } }
        public bool PracticeMark { get; private set; }
        public bool PermanentMark { get; private set; }
        public const float MovementRadius = 2.2f;
        LaneWorld world;
        readonly HashSet<Object> observed = new HashSet<Object>();
        float evadeAt = float.PositiveInfinity, returnAt = float.PositiveInfinity;
        Vector3 evadePoint;
        bool returning, rearmMark;
        public void Initialize(LaneWorld lane, Combatant actor, bool mobile, Vector3 home)
        { world = lane; Actor = actor; Mobile = mobile; Home = home; PracticeMark = true; Visual = TrainingDummyModel.Create(lane, transform.Find("Model")); }
        public void EnablePatrol(Rect bounds)
        { Patrolling = Mobile = true; PatrolBounds = bounds; hasPatrolDestination = false; }
        void TickPatrol()
        {
            if (!Actor.CanAct) return;
            if (!hasPatrolDestination || Vector3.Distance(Actor.Position, PatrolDestination) < .08f)
            {
                float radius = Actor.Radius;
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    PatrolDestination = new Vector3(Random.Range(PatrolBounds.xMin + radius, PatrolBounds.xMax - radius), 0,
                        Random.Range(PatrolBounds.yMin + radius, PatrolBounds.yMax - radius));
                    if (Vector3.Distance(Actor.Position, PatrolDestination) >= 1) break;
                }
                hasPatrolDestination = true;
            }
            Actor.MoveTo(PatrolDestination);
        }
        internal void EnablePermanentMark() { PermanentMark = PracticeMark = true; }
        internal void RelocatePermanentTarget(Vector3 point)
        {
            if (!PermanentMark || Mobile) return;
            Actor.Stop(); Home = point; Actor.transform.position = point;
        }
        internal void ConsumePracticeMark() { if (!PermanentMark) PracticeMark = false; }
        public void RecordDamage(float amount, string label, DamageType type)
        {
            if (amount <= 0) return;
            if (Time.time - LastDamageAt >= 5) { TotalDamage = 0; SequenceStartedAt = Time.time; }
            TotalDamage += amount; LastDamageAt = Time.time; LastHitAmount = amount;
            rearmMark = true;
            Visual.PlayHit();
            Notices.Add(new DamageNotice { Amount = amount, Label = label, Type = type, StartedAt = Time.time });
            if (Notices.Count > 10) Notices.RemoveAt(0);
            if (Mobile && !Patrolling) { returnAt = Time.time + 6; returning = false; }
        }
        void Update()
        {
            if (world == null || world.Paused) return;
            if (rearmMark && Time.time - LastDamageAt >= 6) { PracticeMark = true; rearmMark = false; }
            if (Time.time - LastDamageAt >= 5) { TotalDamage = 0; LastHitAmount = 0; }
            Notices.RemoveAll(n => Time.time - n.StartedAt >= 1.5f);
            if (!Mobile) return;
            if (Patrolling) { TickPatrol(); return; }
            if (Time.time >= returnAt)
            {
                returning = true; evadeAt = float.PositiveInfinity;
                if (Actor.CanAct) Actor.MoveTo(Home);
                if (Vector3.Distance(Actor.Position, Home) < .06f)
                { Actor.Stop(); returning = false; returnAt = float.PositiveInfinity; }
                return;
            }
            if (!returning)
            {
                foreach (BladeLink link in world.BladeLinks)
                    if (link != null && link.HasSecond && link.Threatens(Actor)) Observe(link, link.Second - link.First);
                foreach (BladeWave wave in world.BladeWaves)
                    if (wave != null && wave.Threatens(Actor)) Observe(wave, wave.Direction);
                if (world.BladeLinks.Count == 0 && world.BladeWaves.Count == 0) observed.Clear();
                if (Time.time >= evadeAt && Actor.CanAct)
                {
                    Actor.MoveTo(evadePoint); evadeAt = float.PositiveInfinity;
                    if (float.IsPositiveInfinity(returnAt)) returnAt = Time.time + 6;
                }
            }
        }
        void Observe(Object id, Vector3 direction)
        {
            // React only once to an actual cast, with imperfect accuracy and human delay.
            if (!observed.Add(id) || Random.value > .45f || !float.IsPositiveInfinity(evadeAt)) return;
            Vector3 side = Vector3.Cross(direction.normalized, Vector3.up);
            if (side.sqrMagnitude < .01f) side = Vector3.forward;
            Vector3 offset = Actor.Position + side * (Random.value < .5f ? -1 : 1) * Random.Range(1f, 1.5f) - Home;
            offset += new Vector3(Random.Range(-.3f, .3f), 0, Random.Range(-.3f, .3f));
            evadePoint = world.Clamp(Home + Vector3.ClampMagnitude(offset, MovementRadius), Actor.Radius);
            evadeAt = Time.time + Random.Range(.18f, .32f);
        }
    }
}
