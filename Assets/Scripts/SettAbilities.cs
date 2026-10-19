using System.Collections.Generic;
using UnityEngine;

namespace MidLane
{
    // User-supplied Tower to Tower geometry; timing/damage retain the existing profile.
    public sealed class SettAbilities : MonoBehaviour
    {
        public const float WRange = PracticeAbilityGeometry.SettWLength, WConeAngle = 27;
        public const float WTrueHalfWidth = PracticeAbilityGeometry.SettWTrueHalfWidth, WFarHalfWidth = PracticeAbilityGeometry.SettWFarHalfWidth;
        // A 27-degree opening between the flanks, not a triangle starting at Sett's center.
        public static readonly float WNearHalfWidth = WFarHalfWidth - WRange * Mathf.Tan(WConeAngle * .5f * Mathf.Deg2Rad);
        public const float ERange = PracticeAbilityGeometry.SettELength, EHalfWidth = PracticeAbilityGeometry.SettEHalfWidth;
        public const float RRange = PracticeAbilityGeometry.SettRGrabRange, RRadius = PracticeAbilityGeometry.SettRImpactRadius;
        public bool NextPunchIsRight { get; private set; }
        public bool QEmpowered { get { return qAttacks > 0 && Time.time <= qUntil; } }
        public float BonusAttackRange { get { return (QEmpowered ? PracticeAbilityGeometry.SettQRange : NextPunchIsRight ? PracticeAbilityGeometry.SettRightRange : PracticeAbilityGeometry.SettLeftRange) - PracticeAbilityGeometry.SettLeftRange; } }
        public float Grit { get; private set; }
        public float MaxGrit { get { return self.MaxHealth * .5f; } }
        public float GritFraction { get { return Mathf.Clamp01(Grit / Mathf.Max(1, MaxGrit)); } }
        public float HaymakerDamage { get { return CalculateHaymakerDamage(Grit); } }
        float CalculateHaymakerDamage(float grit)
        { return 60 + self.Progress.Rank(1) * 20 + grit * (.25f + (self.AttackDamage - self.BaseStats.AttackDamage.At(self.Level)) * .0025f); }
        public float RightPunchBonus { get { return 5 * self.Level + 0.55f * (self.AttackDamage - self.BaseStats.AttackDamage.At(self.Level)); } }
        public float AttackWindupDuration { get { return self.BaseStats.AttackWindupFraction / self.BaseAttackSpeed / (NextPunchIsRight ? 8 : 1); } }
        // A left punch is followed by the fast right; a right restores the ordinary recovery.
        public float AttackRecovery { get { return 1 / self.BaseAttackSpeed / (NextPunchIsRight ? 1 : 8); } }
        public float PassiveHealthRegeneration
        {
            get
            {
                float perStep = self.Level >= 16 ? .4f : self.Level >= 11 ? .2f : self.Level >= 6 ? .1f : .03f;
                // Client MissingHealthUnit = 1%; .03/.10/.20/.40 per 1% per second.
                return Mathf.Min(95, Mathf.Floor((1 - self.Health / self.MaxHealth) * 100 + .0001f)) * perStep;
            }
        }
        public float QBonusDamage(Combatant target)
        {
            int rank = self.Progress.Rank(0);
            return rank * 10 + target.MaxHealth * (0.01f + self.AttackDamage * (0.00005f + rank * 0.00005f));
        }
        public bool BlocksFlash { get { return pendingSlot == 2 || pendingSlot == 3; } }
        public bool Casting { get { return pendingSlot >= 0; } }
        public Vector3 Direction { get; private set; }
        public float QMoveSpeedBonus
        {
            get
            {
                Combatant target = self.AttackTarget;
                return Time.time < qSpeedUntil && target != null && target.Kind == UnitKind.Champion && self.ValidEnemy(target)
                    ? self.MoveSpeed * 0.3f : 0;
            }
        }
        LaneWorld world; Combatant self, carried;
        int pendingSlot = -1, castId, life, carriedLife, qAttacks;
        float resolveAt, qUntil, qSpeedUntil, gritDecayAt, storedGrit, shieldAt, shieldAmount, rightPunchUntil;
        Vector3 origin, landing, carryStart, targetStart;
        GameObject warning;
        readonly List<Mesh> meshes = new List<Mesh>();
        readonly List<Material> materials = new List<Material>();
        public void Initialize(LaneWorld lane, Combatant actor) { world = lane; self = actor; }
        public bool ValidCast(int slot, Combatant target)
        { return slot != 3 || (self.ValidEnemy(target) && target.Kind == UnitKind.Champion && Vector3.Distance(self.Position, target.Position) <= RRange); }
        public void Cast(int slot, Vector3 direction, Combatant target)
        {
            Direction = direction; castId = self.SpellCastId; life = self.Deaths;
            if (slot == 0)
            {
                NextPunchIsRight = false; qAttacks = 2; qUntil = Time.time + 5; qSpeedUntil = Time.time + 1.5f;
                self.ResetAttackTimer(); if (self.ValidEnemy(target)) self.OrderAttack(target); return;
            }
            pendingSlot = slot; origin = self.Position;
            float delay = slot == 1 ? 0.75f : slot == 2 ? 0.25f : 0.25f + 1.5f;
            resolveAt = Time.time + delay; self.LockActions(delay);
            warning = new GameObject(slot == 1 ? "Haymaker area" : slot == 2 ? "Facebreaker area" : "The Show Stopper landing");
            warning.transform.SetParent(world.transform, true);
            if (slot == 1)
            {
                storedGrit = Grit; Grit = 0; shieldAt = Time.time; shieldAmount = storedGrit;
                self.SetAbilityShield(storedGrit, 3);
                shieldAmount = self.Shield;
                // The physical flanks start wider than the central true-damage strip.
                Polygon(new[] { new Vector2(0, -WNearHalfWidth), new Vector2(WRange, -WFarHalfWidth), new Vector2(WRange, WFarHalfWidth), new Vector2(0, WNearHalfWidth) }, self.Color * 0.7f);
                Polygon(new[] { new Vector2(0, -WTrueHalfWidth), new Vector2(WRange, -WTrueHalfWidth), new Vector2(WRange, WTrueHalfWidth), new Vector2(0, WTrueHalfWidth) }, new Color(1, 0.8f, 0.25f));
            }
            else if (slot == 2)
                Polygon(new[] { new Vector2(-ERange, -EHalfWidth), new Vector2(ERange, -EHalfWidth), new Vector2(ERange, EHalfWidth), new Vector2(-ERange, EHalfWidth) }, self.Color * 0.7f);
            else
            {
                carryStart = self.Position; targetStart = target.Position;
                carried = target; carriedLife = target.Deaths; landing = world.AvoidTower(target, target.Position + direction * PracticeAbilityGeometry.SettRDisplacement);
                carried.Suppress(delay); var circle = new Vector2[48];
                for (int i = 0; i < circle.Length; i++) { float a = i * Mathf.PI * 2 / circle.Length; circle[i] = new Vector2(Mathf.Cos(a) * RRadius, Mathf.Sin(a) * RRadius); }
                origin = landing; Polygon(circle, self.Color * 0.65f);
            }
        }
        public void ReceiveDamage(float damage)
        { Grit = Mathf.Min(MaxGrit, Grit + damage); gritDecayAt = Time.time + 4; }
        internal void ScaleShieldDecay(float factor) { shieldAmount *= factor; }
        public float EmpowerAttack(Combatant target, float raw)
        {
            if (NextPunchIsRight) raw += RightPunchBonus;
            NextPunchIsRight = !NextPunchIsRight; rightPunchUntil = Time.time + 2;
            if (!QEmpowered) return raw;
            qAttacks--; return raw + QBonusDamage(target);
        }
        public float PreviewAttackDamage(Combatant target)
        {
            return self.AttackDamage + (NextPunchIsRight ? RightPunchBonus : 0) + (QEmpowered ? QBonusDamage(target) : 0);
        }
        // Same handedness, damage and timing as actual attacks; no state is consumed.
        public float PreviewAttackSequenceDamage(Combatant target, float seconds, bool activateQ)
        {
            bool right = !activateQ && NextPunchIsRight;
            int empowered = activateQ ? 2 : QEmpowered ? qAttacks : 0;
            float start = activateQ ? 0 : self.AttackCooldownRemaining, damage = 0;
            float interval = 1 / self.BaseAttackSpeed;
            for (int hit = 0; hit < 40; hit++)
            {
                float impact = start + interval * self.BaseStats.AttackWindupFraction / (right ? 8 : 1);
                if (impact > seconds) break;
                damage += self.AttackDamage + (right ? RightPunchBonus : 0) + (empowered > 0 ? QBonusDamage(target) : 0);
                if (empowered > 0) empowered--;
                start = Mathf.Max(impact, start + interval / (right ? 1 : 8)); right = !right;
            }
            return damage;
        }
        public bool ContainsW(Vector3 point, float radius, bool centerOnly = false)
        {
            Vector3 d = point - origin; float forward = Vector3.Dot(d, Direction), side = Mathf.Abs(Vector3.Dot(d, Vector3.Cross(Direction, Vector3.up)));
            if (centerOnly) return CombatRules.CircleIntersectsRectangle(forward, side, radius, 0, WRange, WTrueHalfWidth);
            return CombatRules.CircleIntersectsTrapezoid(forward, side, radius, WRange, WNearHalfWidth, WFarHalfWidth);
        }
        bool ContainsE(Combatant target)
        {
            Vector3 d = target.Position - origin;
            float x = Mathf.Max(0, Mathf.Abs(Vector3.Dot(d, Direction)) - ERange);
            float y = Mathf.Max(0, Mathf.Abs(Vector3.Dot(d, Vector3.Cross(Direction, Vector3.up))) - EHalfWidth);
            return x * x + y * y <= target.Radius * target.Radius;
        }
        void Update()
        {
            if (world == null || world.Paused) return;
            if (NextPunchIsRight && !QEmpowered && Time.time >= rightPunchUntil && !self.IsWindingAttack) NextPunchIsRight = false;
            if (!self.Alive || self.Deaths != life && Casting) { Cancel(); return; }
            if (Time.time >= gritDecayAt) Grit = Mathf.Max(0, Grit - self.MaxHealth * 0.5f * 0.3f * Time.deltaTime);
            if (Time.time > shieldAt + 0.75f && Time.time < shieldAt + 3 && shieldAmount > 0)
                self.ReduceAbilityShield(shieldAmount / 2.25f * Time.deltaTime);
            if (!Casting) return;
            if (pendingSlot == 1 && warning != null)
            { warning.transform.position += self.Position - origin; origin = self.Position; }
            if (pendingSlot == 3)
            {
                if (carried == null || !carried.Targetable || carried.Deaths != carriedLife) { Cancel(); return; }
                float progress = Mathf.Clamp01(1 - (resolveAt - Time.time) / 1.5f);
                self.transform.position = world.AvoidTower(self, Vector3.Lerp(carryStart, landing - Direction * (self.Radius + carried.Radius), progress));
                carried.transform.position = Vector3.Lerp(targetStart, landing, progress);
            }
            if (Time.time < resolveAt) return;
            int slot = pendingSlot; pendingSlot = -1;
            if (slot == 1) ResolveW(); else if (slot == 2) ResolveE(); else ResolveR();
            ClearVisuals();
        }
        void ResolveW()
        {
            float damage = CalculateHaymakerDamage(storedGrit);
            foreach (Combatant target in world.Units)
                if (target.Kind != UnitKind.Tower && self.ValidEnemy(target) && ContainsW(target.Position, target.Radius))
                    self.DealAbilityDamage(target, damage, ContainsW(target.Position, target.Radius, true) ? DamageType.True : DamageType.Physical, castId);
        }
        void ResolveE()
        {
            origin = self.Position;
            var victims = new List<Combatant>(); bool front = false, back = false;
            foreach (Combatant target in world.Units)
                if (target.Kind != UnitKind.Tower && self.ValidEnemy(target) && ContainsE(target))
                { victims.Add(target); if (Vector3.Dot(target.Position - origin, Direction) >= 0) front = true; else back = true; }
            foreach (Combatant target in victims)
            {
                Vector3 away = (target.Position - origin).normalized;
                self.DealAbilityDamage(target, 30 + self.Progress.Rank(2) * 20 + self.AttackDamage * 0.6f, DamageType.Physical, castId);
                if (!target.Alive) continue;
                if (target.Blade != null) target.Blade.InterruptDash();
                target.Stop(); target.transform.position = world.AvoidTower(target, origin + away * 1.5f);
                if (front && back) target.Stun(1); else target.Slow(0.3f, 0.5f);
            }
        }
        void ResolveR()
        {
            float bonusHealth = Mathf.Max(0, carried.MaxHealth - carried.BaseStats.Health.At(carried.Level));
            int rank = self.Progress.Rank(3);
            float damage = 100 + rank * 100 + (self.AttackDamage - self.BaseStats.AttackDamage.At(self.Level)) * 1.2f + bonusHealth * (0.2f + rank * 0.1f);
            foreach (Combatant target in world.Units)
                if (target.Kind != UnitKind.Tower && self.ValidEnemy(target) && Vector3.Distance(target.Position, landing) <= RRadius + target.Radius)
                {
                    float scale = target == carried ? 1 : Mathf.Lerp(1, 0.5f, Mathf.InverseLerp(1.25f, RRadius, Vector3.Distance(target.Position, landing)));
                    self.DealAbilityDamage(target, damage * scale, DamageType.Physical, castId);
                    if (target.Alive) target.Slow(0.01f, 1);
                }
        }
        void Polygon(Vector2[] points, Color color)
        {
            var vertices = new Vector3[points.Length]; var triangles = new int[(points.Length - 2) * 3];
            Vector3 side = Vector3.Cross(Direction, Vector3.up);
            for (int i = 0; i < points.Length; i++) vertices[i] = origin + Direction * points[i].x + side * points[i].y + Vector3.up * (0.055f + meshes.Count * 0.005f);
            for (int i = 0; i < points.Length - 2; i++) { triangles[i * 3] = 0; triangles[i * 3 + 1] = i + 2; triangles[i * 3 + 2] = i + 1; }
            var mesh = new Mesh { vertices = vertices, triangles = triangles }; mesh.RecalculateNormals(); meshes.Add(mesh);
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit")); material.SetColor("_BaseColor", color); material.SetFloat("_Cull", 0); materials.Add(material);
            var area = new GameObject("Area"); area.transform.SetParent(warning.transform, false);
            area.AddComponent<MeshFilter>().sharedMesh = mesh; area.AddComponent<MeshRenderer>().sharedMaterial = material;
        }
        void ClearVisuals()
        {
            if (warning != null) Destroy(warning);
            foreach (Mesh mesh in meshes) Destroy(mesh); meshes.Clear();
            foreach (Material material in materials) Destroy(material); materials.Clear();
        }
        public void Cancel() { pendingSlot = -1; qAttacks = 0; NextPunchIsRight = false; Grit = 0; shieldAmount = 0; ClearVisuals(); }
        void OnDestroy() { ClearVisuals(); }
    }
}
