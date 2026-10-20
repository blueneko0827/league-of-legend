using System.Collections.Generic;
using UnityEngine;

namespace MidLane
{
    // Presentation only: plays Irelia's official basic attack, passive and Q particle systems
    // (Riot's emitter definitions, see RiotVfx*). It observes the committed gameplay state and
    // the BladeDuelist hit events; it never changes damage, timing or targeting.
    [DefaultExecutionOrder(250)]
    public sealed class IreliaVfx : MonoBehaviour
    {
        // Animation graph clips whose particle events drive the swipes (IreliaVfx.json "events").
        const string Attack1Clip = "irelia_attack_01", Attack1EmpoweredClip = "irelia_attack_01_avar", Attack2Clip = "irelia_attack_02";
        const float AnimationFrameRate = 30;
        LaneWorld world;
        Combatant actor;
        BladeDuelist blade;
        Transform model;
        IreliaRigAnimation rig;
        readonly Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
        readonly RiotVfxPlayer[] stackGlows = new RiotVfxPlayer[4];
        readonly int[] stackGlowStyle = new int[4];
        readonly List<Pending> pending = new List<Pending>();
        readonly List<RiotVfxPlayer> swipes = new List<RiotVfxPlayer>();
        RiotVfxPlayer dash, buff;
        int swipeSequence = -1, landedSequence = -1;
        int observedBasic, observedQ, observedStacks, observedDeaths;
        Combatant lastQVictim;
        float lastQVictimAt = -100;

        struct Pending { public float at; public string system; public string bone; public float rate; }

        public void Initialize(LaneWorld lane, Combatant owner)
        {
            world = lane; actor = owner; blade = owner.Blade;
            model = transform.Find("Model");
            rig = model != null ? model.GetComponentInChildren<IreliaRigAnimation>() : null;
            observedBasic = actor.BasicAttackSequence; observedQ = blade.QSequence; observedDeaths = actor.Deaths;
            blade.OnHitLanded += HitLanded; blade.QLanded += QLanded;
            if (Enabled) RiotVfxLibrary.For(world);
        }

        void OnDestroy()
        {
            if (blade != null) { blade.OnHitLanded -= HitLanded; blade.QLanded -= QLanded; }
            StopPersistent();
        }

        bool Enabled { get { return world != null && world.Presentation.official_vfx; } }

        void LateUpdate()
        {
            if (actor == null || world == null || world.Paused) return;
            if (!actor.Alive || actor.Deaths != observedDeaths)
            {
                StopPersistent(); pending.Clear(); swipes.Clear(); swipeSequence = -1; observedDeaths = actor.Deaths;
                observedBasic = actor.BasicAttackSequence; observedQ = blade.QSequence; observedStacks = 0;
                return;
            }
            if (!Enabled) { StopPersistent(); return; }
            ObserveBasicAttack();
            ObserveQ();
            ObservePassive();
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                if (Time.time < pending[i].at) continue;
                Pending spawn = pending[i]; pending.RemoveAt(i);
                RiotVfxPlayer swipe = RiotVfxPlayer.Play(world, spawn.system, BonePose(spawn.bone, true, true), spawn.rate);
                if (swipe != null) swipes.Add(swipe);
            }
        }

        // Basic attack swipes, from the official animation events of the clip being played.
        void ObserveBasicAttack()
        {
            swipes.RemoveAll(player => player == null);
            // A windup cancelled before its impact (move order, Q, stun) takes its swipe with it,
            // as the interrupted animation would; a landed attack lets the swipe finish.
            if (!actor.IsWindingAttack && swipeSequence >= 0 && landedSequence != swipeSequence)
            {
                foreach (RiotVfxPlayer swipe in swipes) swipe.Stop();
                swipes.Clear(); pending.Clear(); swipeSequence = -1;
            }
            if (!actor.IsWindingAttack || actor.BasicAttackSequence == observedBasic) { if (!actor.IsWindingAttack) observedBasic = actor.BasicAttackSequence; return; }
            observedBasic = actor.BasicAttackSequence;
            swipeSequence = actor.BasicAttackSequence; swipes.Clear();
            int variant = actor.BasicAttackVariant;
            bool empowered = blade.Stacks == 4;
            // mScalePlaySpeedWithAnimation: the swipe follows the clip's retimed playback, whose
            // contact frame is fitted to the real windup (IreliaRigAnimation.Tick).
            float contact = variant == 2 ? 1f / 3 : (rig != null && rig.AttackClip != null ? .14f * rig.AttackClip.length : .3f);
            float rate = Mathf.Clamp(contact / Mathf.Max(.05f, actor.AttackWindupRemaining), .5f, 3);
            string clip = variant == 2 ? Attack2Clip : empowered ? Attack1EmpoweredClip : Attack1Clip;
            RiotVfxLibraryData data = RiotVfxLibrary.For(world).Data;
            if (data == null || data.events == null) return;
            foreach (RiotVfxEventData e in data.events)
            {
                if (e.clip != clip) continue;
                if (e.system.StartsWith("Irelia_Base_P_") && !empowered) continue;
                pending.Add(new Pending { at = Time.time + e.frame / AnimationFrameRate / rate, system = e.system, bone = e.bone,
                    rate = e.scaleWithAnimation ? rate : 1 });
            }
        }

        void ObserveQ()
        {
            if (blade.QSequence != observedQ && blade.IsDashing)
            {
                observedQ = blade.QSequence;
                if (dash != null) dash.Instance.StopBound();
                dash = RiotVfxPlayer.Play(world, "Irelia_Base_Q_Dash", DashPose, 1, model != null ? model.gameObject : null);
            }
            // The leading edge and body glow ride on Irelia only while she dashes.
            if (dash != null && !blade.IsDashing) { dash.Instance.StopBound(); dash = null; }
        }

        RiotVfxPose lastDashPose;
        RiotVfxPose DashPose()
        {
            // The effect outlives Irelia across a Practice restart: keep its last pose.
            if (actor == null || blade == null) return lastDashPose;
            Transform bone = Bone("C_BUFFBONE_GLB_LAYOUT_LOC");
            Vector3 position = bone != null ? bone.position : actor.Position;
            Vector3 direction = blade.QDirection.sqrMagnitude > .001f ? blade.QDirection : Facing() * Vector3.forward;
            lastDashPose = new RiotVfxPose(position, actor.VisualTiltRotation * Quaternion.LookRotation(Flat(direction)));
            return lastDashPose;
        }

        // Passive: glow on one blade per stack, the activation burst and aura at four stacks.
        void ObservePassive()
        {
            int stacks = blade.Stacks;
            if (stacks == 4 && observedStacks < 4)
            {
                if (buff != null) buff.Stop();
                buff = RiotVfxPlayer.Play(world, "Irelia_Base_P_buf", BonePose("BUFFBONE_GLB_GROUND_LOC", true), 1, model != null ? model.gameObject : null);
                if (actor.Audio != null) actor.Audio.PassiveMaxed();
                // The hit that completed the stacks shows the 4-stack proc on its target.
                if (lastQVictim != null && Time.time - lastQVictimAt < .25f)
                {
                    PlayOnTarget("Irelia_Base_P_Hit_4stack_left", lastQVictim, blade.QDirection, true);
                    if (actor.Audio != null) actor.Audio.PassiveMaxedHit();
                }
            }
            if (stacks < 4 && buff != null) { buff.Stop(); buff = null; }
            observedStacks = stacks;
            for (int i = 0; i < stackGlows.Length; i++)
            {
                int style = stacks == 4 ? 4 : i < stacks ? 1 : 0;
                if (style == stackGlowStyle[i] && (style == 0 || stackGlows[i] != null)) continue;
                if (stackGlows[i] != null) stackGlows[i].Stop();
                stackGlows[i] = null; stackGlowStyle[i] = style;
                if (style == 0) continue;
                string bone = "Sword" + (i + 1) + "_Skn";
                stackGlows[i] = RiotVfxPlayer.Play(world, style == 4 ? "Irelia_Base_P_Blades_Stacks_4" : "Irelia_Base_P_Blades_Stacks_1_2_3",
                    BonePose(bone, false));
            }
        }

        void HitLanded(Combatant target, bool empowered, bool critical, bool fromQ)
        {
            if (!Enabled || target == null) return;
            Vector3 direction = target.Position - actor.Position;
            if (!fromQ)
            {
                landedSequence = actor.BasicAttackSequence;
                string system = critical ? "Irelia_Base_BA_Hit_Crit" : actor.BasicAttackVariant == 2 ? "Irelia_Base_BA_Hit_02" : "Irelia_Base_BA_Hit";
                PlayOnTarget(system, target, direction);
            }
            if (empowered)
            {
                PlayOnTarget(actor.BasicAttackVariant == 2 && !fromQ ? "Irelia_Base_P_Hit_04" : "Irelia_Base_P_Hit_02", target, direction, true);
                if (actor.Audio != null) actor.Audio.PassiveEmpoweredHit();
            }
        }

        void QLanded(Combatant target, bool refunded)
        {
            if (!Enabled || target == null) return;
            lastQVictim = target; lastQVictimAt = Time.time;
            PlayOnTarget("Irelia_Base_Q_tar", target, blade.QDirection);
            RiotVfxPlayer.Play(world, "Irelia_Base_Q_heal", BonePose("C_BUFFBONE_GLB_CENTER_LOC", true));
            // A marked target or a kill refunds Q: the mark bursts above the target.
            if (refunded) RiotVfxPlayer.Play(world, "Irelia_Base_Q_Proc", TargetBonePose(target, "C_Buffbone_Glb_Overhead_Loc", blade.QDirection));
        }

        // Spell/attack hits use the target's chest bone (mHitBoneName). The passive hit systems
        // are authored from the ground (their flashes are offset 80 units up), so they use it.
        void PlayOnTarget(string system, Combatant target, Vector3 direction, bool ground = false)
        {
            RiotVfxPlayer.Play(world, system, ground ? TargetBonePose(target, "Buffbone_Glb_Ground_Loc", direction, 0) :
                TargetBonePose(target, "C_Buffbone_Glb_Chest_Loc", direction));
        }

        // Hit effects use the target's official bone and face along the attack. Without the bone,
        // the point falls back to a fraction of the target's visual height.
        public static System.Func<RiotVfxPose> TargetBonePose(Combatant target, string boneName, Vector3 direction, float fallbackHeight = .55f)
        {
            Transform bone = RiotVfxPlayer.FindBone(target != null ? target.transform : null, boneName);
            Quaternion facing = Quaternion.LookRotation(Flat(direction));
            Vector3 last = target != null ? target.Position + Vector3.up * target.VisualHeight * fallbackHeight : Vector3.zero;
            return () =>
            {
                if (bone != null) last = bone.position;
                else if (target != null) last = target.Position + Vector3.up * target.VisualHeight * fallbackHeight;
                return new RiotVfxPose(last, facing);
            };
        }

        // Character-attached effects: bone position. Buff bones take the character's facing and
        // camera tilt; the floating blades keep their own animated orientation.
        // boneFrame: animation-event particles live in the bone's frame, and Riot's root-level
        // buff bones (C_Buffbone_Glb_Layout_Loc, Buffbone_Glb_Ground_Loc...) are yawed 180°
        // in irelia_base.skl, which the swipes' own 180° birth rotation undoes.
        System.Func<RiotVfxPose> BonePose(string boneName, bool facing, bool boneFrame = false)
        {
            Transform bone = Bone(boneName);
            Quaternion yaw = boneFrame ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
            var last = new RiotVfxPose(actor.Position, Facing() * yaw);
            return () =>
            {
                if (actor == null) return last;
                if (bone != null) last = new RiotVfxPose(bone.position, facing ? Facing() * yaw : bone.rotation);
                else last = new RiotVfxPose(actor.Position, Facing() * yaw);
                return last;
            };
        }

        Quaternion Facing()
        {
            Transform source = model != null ? model : actor.transform;
            return actor.VisualTiltRotation * Quaternion.LookRotation(Flat(source.forward));
        }

        Transform Bone(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            Transform bone;
            if (!bones.TryGetValue(name, out bone)) bones[name] = bone = RiotVfxPlayer.FindBone(model, name);
            return bone;
        }

        static Vector3 Flat(Vector3 direction)
        {
            direction.y = 0;
            return direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.forward;
        }

        void StopPersistent()
        {
            for (int i = 0; i < stackGlows.Length; i++) { if (stackGlows[i] != null) stackGlows[i].Stop(); stackGlows[i] = null; stackGlowStyle[i] = 0; }
            if (buff != null) buff.Stop();
            buff = null;
            if (dash != null) dash.Instance.StopBound();
            dash = null;
        }
    }
}
