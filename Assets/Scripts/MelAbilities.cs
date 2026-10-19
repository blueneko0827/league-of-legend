using System.Collections.Generic;
using UnityEngine;

namespace MidLane
{
    public sealed class MelAbilities : MonoBehaviour
    {
        sealed class MarkState { public int Count, Life; public float Until; }
        LaneWorld world;
        Combatant self;
        int stacks;
        float stacksUntil, reflectUntil;
        GameObject barrier;
        readonly Dictionary<Combatant, MarkState> marks = new Dictionary<Combatant, MarkState>();
        readonly List<Combatant> tracked = new List<Combatant>();
        public readonly List<MelBolt> Projectiles = new List<MelBolt>();
        public int Stacks { get { return Time.time < stacksUntil ? stacks : 0; } }
        public bool Reflecting { get { return self.Alive && self.Dummy == null && !self.IsInStasis && Time.time < reflectUntil; } }
        public float MoveSpeedBonus
        {
            get
            {
                float bonus = MelRules.WSpeedBonus * Mathf.Clamp01((reflectUntil - Time.time) / MelRules.WDuration);
                float scale = LeagueChampionStats.WorldUnitsPerLeagueUnit;
                return Mathf.Max(0, ItemInventory.MovementSpeed(self.MoveSpeed / scale * (1 + bonus)) * scale - self.MoveSpeed);
            }
        }
        public Combatant Actor { get { return self; } }
        public MelSnare LastSnare { get; private set; }
        public void Initialize(LaneWorld lane, Combatant actor) { world = lane; self = actor; }
        public int OverwhelmStacks(Combatant target)
        {
            MarkState mark;
            return target != null && target.Alive && marks.TryGetValue(target, out mark) &&
                mark.Life == target.Deaths && Time.time < mark.Until ? mark.Count : 0;
        }
        public bool HasMarkedChampion
        {
            get { foreach (Combatant target in world.Units) if (self.ValidEnemy(target) && target.Kind == UnitKind.Champion && OverwhelmStacks(target) > 0) return true; return false; }
        }
        public bool ValidCast(int slot) { return slot != 3 || HasMarkedChampion; }
        public void OnSpellCast()
        { if (self.Dummy == null) { stacks = Mathf.Min(9, Stacks + 3); stacksUntil = Time.time + MelRules.StackDuration; } }
        public void Cast(int slot, Vector3 aim, Vector3 direction)
        {
            if (slot == 0)
            {
                self.LockActions(MelRules.QCastTime);
                var volley = new GameObject("Mel Q · Radiant Volley").AddComponent<MelVolley>();
                volley.transform.SetParent(transform.parent);
                volley.Initialize(world, this, self.Position + Vector3.ClampMagnitude(Flat(aim - self.Position), MelRules.QRange));
            }
            else if (slot == 1)
            {
                reflectUntil = Time.time + MelRules.WDuration;
                self.SetAbilityShield(MelRules.WShield(self.Progress.Rank(1), self.AbilityPower), MelRules.WDuration);
                if (barrier != null) Destroy(barrier);
                barrier = MelVisuals.Ring(world, transform, "Rebuttal · reflection", MelRules.WRadius, new Color(1, .8f, .2f));
            }
            else if (slot == 2)
            {
                self.LockActions(MelRules.ECastTime);
                var snare = new GameObject("Mel E · Solar Snare").AddComponent<MelSnare>();
                snare.transform.SetParent(transform.parent); snare.Initialize(world, this, direction); LastSnare = snare;
            }
            else
            {
                self.LockActions(MelRules.RCastTime);
                var eclipse = new GameObject("Mel R · Golden Eclipse").AddComponent<MelEclipse>();
                eclipse.transform.SetParent(transform.parent); eclipse.Initialize(world, this);
            }
        }
        public void OnAttackHit(Combatant target)
        {
            int count = Stacks; stacks = 0;
            for (int i = 0; i < count; i++)
                MelBolt.Create(world, self, target, MelRules.PassiveProjectile(self.Level, self.AbilityPower) * (target.Kind == UnitKind.Minion ? .5f : 1),
                    DamageType.Magic, -1, self.SpellCastId, i * .035f, "Searing Brilliance");
        }
        public void RecordHit(Combatant target)
        {
            if (self.Dummy != null || target == null || !target.Alive || target.Team == self.Team || target.Kind == UnitKind.Tower) return;
            MarkState mark;
            if (!marks.TryGetValue(target, out mark)) { mark = new MarkState(); marks[target] = mark; }
            mark.Count = OverwhelmStacks(target) + 1; mark.Life = target.Deaths; mark.Until = Time.time + MelRules.StackDuration;
            TryExecute(target);
        }
        public float StoredExecuteDamage(Combatant target)
        {
            float raw = MelRules.ExecuteDamage(self.Progress.Rank(3), OverwhelmStacks(target), self.AbilityPower);
            if (target.Kind == UnitKind.Minion) raw *= .5f;
            return target.PreviewIncomingDamage(raw, self, DamageType.Magic);
        }
        void TryExecute(Combatant target)
        {
            if (!self.ValidEnemy(target) || target.Dummy != null || OverwhelmStacks(target) == 0 || StoredExecuteDamage(target) < target.Health + target.TotalShield) return;
            marks.Remove(target);
            // Execute is not another damaging spell and must not recursively add a mark.
            target.TakeDamage((target.MaxHealth + target.TotalShield) * 2 + 1, self, DamageType.True, hitLabel: "Overwhelm");
        }
        void Update()
        {
            if (world.Paused || self.Dummy != null) return;
            if (barrier != null && !Reflecting) { Destroy(barrier); barrier = null; }
            tracked.Clear(); tracked.AddRange(marks.Keys);
            foreach (Combatant target in tracked)
            {
                if (target == null || OverwhelmStacks(target) == 0) marks.Remove(target);
                else TryExecute(target);
            }
        }
        public void ClearTemporaryEffects()
        { stacks = 0; stacksUntil = reflectUntil = 0; if (barrier != null) Destroy(barrier); barrier = null; }
        public static Vector3 Flat(Vector3 point) { point.y = 0; return point; }
        public bool CanIntercept(Combatant source)
        { return Reflecting && source != null && source.Team != self.Team && source.Kind == UnitKind.Champion; }
        // Flawless Duet is denied locally, not returned as a new targeted projectile.
        public bool BlocksBladeConvergence(Combatant source) { return CanIntercept(source); }
        public float ReflectionFactor(bool physical)
        { return MelRules.ReflectionMultiplier(self.Progress.Rank(1), self.AbilityPower, physical); }
        // A tracking projectile keeps its target lock when returned. Ground effects and dashes never call this.
        public bool ReflectTargeted(Combatant source, float raw, DamageType type, int castId = 0)
        {
            if (!CanIntercept(source)) return false;
            MelBolt.Create(world, self, source, raw * MelRules.ReflectionMultiplier(self.Progress.Rank(1), self.AbilityPower, type == DamageType.Physical),
                DamageType.Magic, 1, self.SpellCastId, 0, "Rebuttal");
            return true;
        }
    }

    static class MelVisuals
    {
        public static GameObject Ring(LaneWorld world, Transform parent, string name, float radius, Color color)
        {
            var root = new GameObject(name); root.transform.SetParent(parent, false);
            var line = root.AddComponent<LineRenderer>(); line.useWorldSpace = false; line.loop = true;
            line.positionCount = 64; line.startWidth = line.endWidth = .045f;
            line.sharedMaterial = world.Material(color);
            for (int i = 0; i < 64; i++) { float a = i * Mathf.PI * 2 / 64; line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, .09f, Mathf.Sin(a) * radius)); }
            return root;
        }
    }
}
