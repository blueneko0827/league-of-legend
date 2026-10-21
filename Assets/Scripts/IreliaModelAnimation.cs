using UnityEngine;

namespace MidLane
{
    // Presentation only: observes the actual combat state and never reads input or deals damage.
    public sealed class IreliaModelAnimation : MonoBehaviour
    {
        LaneWorld world;
        Combatant actor;
        Transform body;
        Transform[] blades;
        Vector3 bodyRest;
        Quaternion bodyRotation;
        Vector3[] bladeRest;
        Quaternion[] bladeRotations;
        float elapsed, attackAt = -100, attackDuration;
        bool wasAlive;
        ChampionWalkAnimation walk;
        IreliaRigAnimation rig;
        int observedWCastId = -1, suppressedWCastId = -1;
        int observedBasicSequence, observedQSequence, suppressedQSequence = -1;
        int observedRSequence, suppressedRSequence = -1, suppressedESequence = -1;
        Transform wGuard;
        Transform[] wGuardBlades;
        public void Configure(Transform bodyVisual, Transform[] bladeVisuals)
        {
            body = bodyVisual; blades = bladeVisuals; bodyRest = body.localPosition; bodyRotation = body.localRotation;
            bladeRest = new Vector3[blades.Length]; bladeRotations = new Quaternion[blades.Length];
            for (int i = 0; i < blades.Length; i++) { bladeRest[i] = blades[i].localPosition; bladeRotations[i] = blades[i].localRotation; }
        }
        public void Bind(LaneWorld lane, Combatant owner)
        {
            world = lane; actor = owner; wasAlive = actor.Alive;
            observedBasicSequence = actor.BasicAttackSequence; observedQSequence = actor.Blade.QSequence; observedWCastId = actor.Blade.WCastId;
            observedRSequence = actor.Blade.RSequence;
            walk = body.GetComponentInChildren<ChampionWalkAnimation>(); rig = body.GetComponentInChildren<IreliaRigAnimation>();
        }
        void LateUpdate()
        {
            if (actor == null || world.Paused) return;
            if (!actor.Alive) { ResetPose(); wasAlive = false; return; }
            if (!wasAlive) { ResetPose(); wasAlive = true; }
            elapsed += Time.deltaTime;
            bool attackStarted = actor.IsWindingAttack && actor.BasicAttackSequence != observedBasicSequence;
            if (attackStarted)
            { attackAt = Time.time; attackDuration = rig != null ? actor.Blade.AttackInterval * 0.85f : Mathf.Clamp(actor.Blade.AttackInterval * 0.45f, 0.15f, 0.6f); }
            observedBasicSequence = actor.BasicAttackSequence;
            float attack = Mathf.Clamp01((Time.time - attackAt) / Mathf.Max(0.001f, attackDuration));
            float strike = actor.IsStunned ? 0 : Mathf.Sin(attack * Mathf.PI);
            float movement = Mathf.Clamp01(actor.Velocity.magnitude / Mathf.Max(0.01f, actor.MoveSpeed));
            bool walking = movement > 0.02f && actor.Velocity.magnitude <= actor.MoveSpeed * 1.1f &&
                !actor.Blade.IsDashing && !actor.Blade.IsCharging && !actor.IsStunned;
            if (actor.Blade.IsCharging || observedWCastId != actor.Blade.WCastId)
            { attackAt = -100; observedWCastId = actor.Blade.WCastId; }
            if (actor.Blade.IsDashing || (observedQSequence != actor.Blade.QSequence && !attackStarted)) attackAt = -100;
            observedQSequence = actor.Blade.QSequence;
            if (observedRSequence != actor.Blade.RSequence) attackAt = -100;
            observedRSequence = actor.Blade.RSequence;
            if (walk != null) walk.Tick(walking, actor.Velocity.magnitude, Time.deltaTime);
            if (rig != null)
            {
                rig.ClearEOverlay();
                bool attacking = Time.time < attackAt + attackDuration && actor.AttackCooldownRemaining > 0 && !actor.Blade.BlocksActions && !walking;
                if (!actor.Blade.IsCharging && (walking || attacking || actor.IsStunned || actor.IsInStasis || actor.Blade.IsDashing || actor.SpellCastId != actor.Blade.WCastId))
                    suppressedWCastId = actor.Blade.WCastId;
                bool w = (actor.Blade.IsCharging || suppressedWCastId != actor.Blade.WCastId) &&
                    rig.TickW(actor.Blade.IsCharging, actor.Blade.WChargeElapsed, actor.Blade.WReleaseElapsed, actor.Blade.Tuning.wReleaseTime, Time.deltaTime);
                bool rWindup = actor.Blade.RAnimationElapsed < actor.Blade.Tuning.rCastTime;
                if (attacking || actor.IsStunned || actor.IsInStasis || actor.Blade.IsCharging || actor.Blade.IsDashing || actor.SpellCastId != actor.Blade.RSpellCastId)
                    suppressedRSequence = actor.Blade.RSequence;
                bool r = !w && !actor.IsStunned && !actor.IsInStasis && (rWindup || suppressedRSequence != actor.Blade.RSequence) &&
                    rig.TickR(actor.Blade.RAnimationElapsed, actor.Blade.Tuning.rCastTime, walking, Time.deltaTime);
                if (!actor.Blade.IsDashing && (attacking || actor.IsStunned || actor.IsInStasis || actor.Blade.IsCharging || actor.SpellCastId != actor.Blade.QFinishedSpellId))
                    suppressedQSequence = actor.Blade.QSequence;
                bool q = !w && !r && !actor.IsStunned && !actor.IsInStasis && (actor.Blade.IsDashing || suppressedQSequence != actor.Blade.QSequence) &&
                    rig.TickQ(actor.Blade.IsDashing, actor.Blade.QChained, actor.Blade.QDashProgress, actor.Blade.QElapsed,
                        actor.Blade.QRecoveryElapsed, walking, Time.deltaTime);
                if (w) transform.rotation = Quaternion.LookRotation(actor.Blade.WDirection);
                else if (r) { if (rWindup) transform.rotation = Quaternion.LookRotation(actor.Blade.RDirection); }
                else if (q) { if (actor.Blade.IsDashing) transform.rotation = Quaternion.LookRotation(actor.Blade.QDirection); }
                else rig.Tick(actor.IsStunned || actor.IsInStasis ? IreliaPose.Frozen : walking ? IreliaPose.Run : attacking ? IreliaPose.Attack : IreliaPose.Idle,
                        actor.Velocity.magnitude, Time.deltaTime, attackStarted, actor.AttackWindupRemaining, actor.Blade.AttackInterval, actor.BasicAttackVariant);
                if (actor.IsStunned || actor.IsInStasis || actor.Blade.IsCharging || actor.BasicAttackSequence > actor.Blade.EBasicSequence ||
                    (!actor.Blade.IsDashing && actor.SpellCastId != actor.Blade.ESpellCastId) ||
                    (r && actor.Blade.EAnimationElapsed > actor.Blade.RAnimationElapsed))
                    suppressedESequence = actor.Blade.ESequence;
                bool e = !w && !r && !actor.IsStunned && !actor.IsInStasis && suppressedESequence != actor.Blade.ESequence &&
                    rig.TickE(actor.Blade.ESecondCast, actor.Blade.EAnimationElapsed,
                        actor.Blade.ESecondCast ? actor.Blade.Tuning.eSecondCastTime : actor.Blade.Tuning.ePlacementTime,
                        walking, q || walking || attacking, Time.deltaTime);
                if (e && !q && !walking && !attacking) transform.rotation = Quaternion.LookRotation(actor.Blade.EDirection);
                // With the official W particles (IreliaVfx: W_DR shield) the placeholder guard blades stay off.
                UpdateWGuard(w && actor.Blade.IsCharging && !world.Presentation.official_vfx);
                if (actor.IsStunned) attackAt = -100;
            }
            float bob = (1 + Mathf.Sin(elapsed * (movement > 0 ? 9 : 2))) * (movement > 0 ? 0.025f : 0.007f);
            body.localPosition = bodyRest + Vector3.up * (rig != null || (walking && walk != null) ? 0 : bob);
            body.localRotation = bodyRotation * Quaternion.Euler(rig != null ? 0 : -strike * 10 - (actor.Blade.IsDashing ? 12 : 0), rig != null ? 0 : Mathf.Sin(elapsed * 4) * movement * 3, 0);
            for (int i = 0; i < blades.Length; i++)
            {
                Vector3 pose = bladeRest[i] + Vector3.up * (Mathf.Sin(elapsed * 2.2f + i * 1.3f) * 0.03f);
                Quaternion rotation = bladeRotations[i] * Quaternion.Euler(0, Mathf.Sin(elapsed * 2.2f + i) * 16, 0);
                if (actor.Blade.IsCharging)
                { pose += new Vector3(-Mathf.Sign(bladeRest[i].x) * 0.2f, 0.15f, 0.4f); rotation *= Quaternion.Euler(0, 0, (i % 2 == 0 ? 1 : -1) * 35); }
                else if (actor.Blade.IsDashing) pose += Vector3.back * 0.45f;
                else if (actor.Blade.WaitingSecondBlade && i >= 2) pose += new Vector3(0, 0.15f, 0.3f);
                else { pose += Vector3.forward * (strike * 0.65f) + Vector3.up * (strike * 0.15f); rotation *= Quaternion.Euler(0, strike * 150, 0); }
                blades[i].localPosition = pose; blades[i].localRotation = rotation;
            }
        }
        void ResetPose()
        {
            elapsed = 0; attackAt = -100;
            observedBasicSequence = actor != null ? actor.BasicAttackSequence : 0;
            observedQSequence = actor != null ? actor.Blade.QSequence : 0;
            observedWCastId = actor != null ? actor.Blade.WCastId : 0;
            suppressedWCastId = suppressedQSequence = -1;
            observedRSequence = actor != null ? actor.Blade.RSequence : 0;
            suppressedRSequence = suppressedESequence = -1;
            if (wGuard != null) wGuard.gameObject.SetActive(false);
            if (walk != null) walk.ResetPose();
            if (rig != null) rig.ResetPose();
            body.localPosition = bodyRest; body.localRotation = bodyRotation;
            for (int i = 0; i < blades.Length; i++) { blades[i].localPosition = bladeRest[i]; blades[i].localRotation = bladeRotations[i]; }
        }
        void UpdateWGuard(bool active)
        {
            // The official charge clip parks its rig blades underground; LoL draws a separate
            // particle effect here. Reuse the official blade mesh for that visual guard in Unity.
            if (active && wGuard == null)
            {
                wGuard = new GameObject("W Guard Blades").transform; wGuard.SetParent(transform, false);
                wGuard.localScale = Vector3.one * world.Presentation.visual_mesh_multiplier;
                wGuardBlades = new Transform[6];
                for (int i = 0; i < wGuardBlades.Length; i++)
                    wGuardBlades[i] = PracticeModels.CreateSpellBlade(world, wGuard, actor.Team, "Guard blade " + i, Vector3.zero).transform;
            }
            if (wGuard == null) return;
            wGuard.gameObject.SetActive(active);
            if (!active) return;
            float spin = actor.Blade.WChargeElapsed * 160;
            Quaternion tilt = Quaternion.Euler(18, 0, 0);
            for (int i = 0; i < wGuardBlades.Length; i++)
            {
                float angle = spin + i * 60, radians = angle * Mathf.Deg2Rad;
                wGuardBlades[i].localPosition = Vector3.up * 0.9f + tilt * new Vector3(Mathf.Sin(radians) * 0.8f, 0, Mathf.Cos(radians) * 0.8f);
                wGuardBlades[i].localRotation = tilt * Quaternion.Euler(0, angle, 60);
            }
        }
    }
}
