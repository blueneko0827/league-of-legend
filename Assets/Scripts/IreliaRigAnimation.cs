using UnityEngine;

namespace MidLane
{
    public enum IreliaPose { Idle, Run, Attack, Frozen, WCharge, WRelease, WRecovery, QDash, QToIdle, QToRun, QChain,
        EFirst, ESecond, ERecovery, RCast, RRecovery }

    // Presentation only. Sample the supplied rig without root motion or animation events.
    public sealed class IreliaRigAnimation : MonoBehaviour
    {
        public AnimationClip IdleClip { get; private set; }
        public AnimationClip RunClip { get; private set; }
        public AnimationClip AttackClip { get; private set; }
        public AnimationClip Attack2Clip { get; private set; }
        public AnimationClip QDashClip { get; private set; }
        public AnimationClip QToIdleClip { get; private set; }
        public AnimationClip QToRunClip { get; private set; }
        public AnimationClip QChainClip { get; private set; }
        public AnimationClip EFirstClip { get; private set; }
        public AnimationClip EFirstRunClip { get; private set; }
        public AnimationClip EFirstToIdleClip { get; private set; }
        public AnimationClip ESecondClip { get; private set; }
        public AnimationClip ESecondRunClip { get; private set; }
        public AnimationClip ESecondToIdleClip { get; private set; }
        public AnimationClip RCastClip { get; private set; }
        public AnimationClip RHoldClip { get; private set; }
        public AnimationClip RToIdleClip { get; private set; }
        public AnimationClip RToRunClip { get; private set; }
        public bool EOverlayActive { get; private set; }
        public AnimationClip EAnimationClip { get; private set; }
        public float EAnimationTime { get; private set; }
        public int AttackVariant { get; private set; } = 1;
        public AnimationClip CurrentAttackClip { get { return AttackVariant == 2 && Attack2Clip != null ? Attack2Clip : AttackClip; } }
        public AnimationClip WChargeClip { get; private set; }
        public AnimationClip WReleaseClip { get; private set; }
        public AnimationClip WRecoveryClip { get; private set; }
        public IreliaPose Pose { get; private set; }
        public float CycleTime { get; private set; }
        public bool Walking { get { return Pose == IreliaPose.Run; } }
        Transform[] joints;
        Transform pelvis;
        Vector3 pelvisRest;
        Vector3[] positions, scales, previousPositions, previousScales;
        Quaternion[] rotations, previousRotations;
        float attackTime, attackWindup, attackCycle;
        bool nextAttack2;
        bool[] upperBody;
        Vector3[] layerPositions, layerScales;
        Quaternion[] layerRotations;
        const float BlendSeconds = 0.08f;
        public void Configure(AnimationClip idle, AnimationClip run, AnimationClip attack,
            AnimationClip wCharge = null, AnimationClip wRelease = null, AnimationClip wRecovery = null,
            AnimationClip attack2 = null, AnimationClip qDash = null, AnimationClip qToIdle = null,
            AnimationClip qToRun = null, AnimationClip qChain = null)
        {
            IdleClip = idle; RunClip = run; AttackClip = attack;
            WChargeClip = wCharge; WReleaseClip = wRelease; WRecoveryClip = wRecovery;
            Attack2Clip = attack2; QDashClip = qDash; QToIdleClip = qToIdle; QToRunClip = qToRun; QChainClip = qChain;
            joints = GetComponentsInChildren<Transform>();
            positions = new Vector3[joints.Length]; scales = new Vector3[joints.Length]; rotations = new Quaternion[joints.Length];
            previousPositions = new Vector3[joints.Length]; previousScales = new Vector3[joints.Length]; previousRotations = new Quaternion[joints.Length];
            for (int i = 0; i < joints.Length; i++)
            {
                positions[i] = joints[i].localPosition; scales[i] = joints[i].localScale; rotations[i] = joints[i].localRotation;
                if (joints[i].name == "Root") pelvis = joints[i];
            }
            pelvisRest = pelvis.localPosition;
            ResetPose();
        }
        public void ConfigureEAndR(AnimationClip eFirst, AnimationClip eFirstRun, AnimationClip eFirstIdle,
            AnimationClip eSecond, AnimationClip eSecondRun, AnimationClip eSecondIdle,
            AnimationClip rCast, AnimationClip rHold, AnimationClip rIdle, AnimationClip rRun)
        {
            EFirstClip = eFirst; EFirstRunClip = eFirstRun; EFirstToIdleClip = eFirstIdle;
            ESecondClip = eSecond; ESecondRunClip = eSecondRun; ESecondToIdleClip = eSecondIdle;
            RCastClip = rCast; RHoldClip = rHold; RToIdleClip = rIdle; RToRunClip = rRun;
            upperBody = new bool[joints.Length]; layerPositions = new Vector3[joints.Length];
            layerRotations = new Quaternion[joints.Length]; layerScales = new Vector3[joints.Length];
            Transform spine = System.Array.Find(joints, joint => joint.name == "Spine1");
            for (int i = 0; i < joints.Length; i++) upperBody[i] = spine != null && (joints[i] == spine || joints[i].IsChildOf(spine));
        }
        public void ClearEOverlay() { EOverlayActive = false; EAnimationClip = null; EAnimationTime = 0; }
        public bool TickE(bool second, float elapsed, float releaseDelay, bool walking, bool preserveBase, float dt)
        {
            ClearEOverlay();
            AnimationClip clip = second ? ESecondClip : EFirstClip;
            if (clip == null || upperBody == null || float.IsInfinity(elapsed) || elapsed < 0) return false;
            const float contact = 0.15f;
            float castDuration = releaseDelay + clip.length - contact;
            float sample;
            if (elapsed < castDuration)
                sample = elapsed <= releaseDelay ? elapsed / Mathf.Max(.001f, releaseDelay) * contact : contact + elapsed - releaseDelay;
            else
            {
                clip = walking ? (second ? ESecondRunClip : EFirstRunClip) : (second ? ESecondToIdleClip : EFirstToIdleClip);
                float recovery = elapsed - castDuration;
                if (clip == null || recovery >= (walking ? .18f : .4f)) return false;
                sample = Mathf.Min(recovery, clip.length - .001f);
            }
            IreliaPose basePose = Pose; float baseTime = CycleTime;
            if (preserveBase) for (int i = 0; i < joints.Length; i++)
            { layerPositions[i] = joints[i].localPosition; layerRotations[i] = joints[i].localRotation; layerScales[i] = joints[i].localScale; }
            Sample(clip, sample, dt, elapsed >= .06f);
            if (preserveBase)
            {
                for (int i = 0; i < joints.Length; i++) if (!upperBody[i])
                { joints[i].localPosition = layerPositions[i]; joints[i].localRotation = layerRotations[i]; joints[i].localScale = layerScales[i]; }
                Pose = basePose; CycleTime = baseTime;
            }
            else { Pose = elapsed < castDuration ? (second ? IreliaPose.ESecond : IreliaPose.EFirst) : IreliaPose.ERecovery; CycleTime = sample; }
            EOverlayActive = true; EAnimationClip = clip; EAnimationTime = sample;
            return true;
        }
        public bool TickR(float elapsed, float castDuration, bool walking, float dt)
        {
            if (RCastClip == null || RToIdleClip == null || RToRunClip == null || float.IsInfinity(elapsed) || elapsed < 0) return false;
            AnimationClip clip; float sample;
            if (elapsed < castDuration)
            { Pose = IreliaPose.RCast; clip = RCastClip; sample = elapsed / Mathf.Max(.001f, castDuration) * Mathf.Max(0, clip.length - .001f); }
            else
            {
                float recovery = elapsed - castDuration;
                if (recovery >= (walking ? .18f : .45f)) return false;
                Pose = IreliaPose.RRecovery; clip = walking ? RToRunClip : RToIdleClip; sample = Mathf.Min(recovery, clip.length - .001f);
            }
            CycleTime = sample;
            Sample(clip, sample, dt, elapsed >= .06f, Pose == IreliaPose.RRecovery ? RHoldClip : null);
            return true;
        }
        public void Tick(IreliaPose desired, float speed, float dt, bool attackStarted, float windup, float interval, int requestedAttackVariant = 0)
        {
            if (desired == IreliaPose.Frozen) { Pose = desired; return; }
            if (attackStarted)
            {
                AttackVariant = requestedAttackVariant == 1 || requestedAttackVariant == 2 ? requestedAttackVariant : nextAttack2 ? 2 : 1;
                nextAttack2 = AttackVariant == 1;
                attackTime = 0; attackWindup = Mathf.Max(0.04f, windup); attackCycle = Mathf.Max(attackWindup + 0.04f, interval * 0.85f);
            }
            if (desired != Pose) CycleTime = 0;
            Pose = desired;
            AnimationClip clip = desired == IreliaPose.Run ? RunClip : desired == IreliaPose.Attack ? CurrentAttackClip : IdleClip;
            float sample;
            if (desired == IreliaPose.Attack)
            {
                attackTime += dt;
                // Contact lands at the basic's actual windup; recovery fits its attack interval.
                // Each official swing has its own contact frame; both land at the actual basic windup.
                float contact = AttackVariant == 2 ? (1f / 3) / clip.length : 0.14f;
                sample = attackTime <= attackWindup ? attackTime / attackWindup * contact :
                    Mathf.Lerp(contact, 1, Mathf.InverseLerp(attackWindup, attackCycle, attackTime));
                sample = Mathf.Clamp01(sample) * Mathf.Max(0, clip.length - 0.001f); CycleTime = sample;
            }
            else { CycleTime = Mathf.Repeat(CycleTime + dt * (desired == IreliaPose.Run ? Mathf.Clamp(speed / 3.35f, 0.1f, 2.5f) : 1), clip.length); sample = CycleTime; }
            Sample(clip, sample, dt, desired == IreliaPose.Attack && attackTime >= 0.06f);
        }
        public bool TickQ(bool dashing, bool chained, float progress, float elapsed, float recoveryElapsed, bool walking, float dt)
        {
            if (QDashClip == null || QToIdleClip == null || QToRunClip == null) return false;
            AnimationClip clip; float sample; AnimationClip basis = null;
            if (dashing)
            {
                Pose = IreliaPose.QDash; clip = QDashClip;
                sample = Mathf.Clamp01(progress) * Mathf.Max(0, clip.length - 0.001f);
                if (chained && QChainClip != null && elapsed < QChainClip.length)
                { Pose = IreliaPose.QChain; basis = QDashClip; clip = QChainClip; sample = elapsed; }
            }
            else
            {
                clip = walking ? QToRunClip : QToIdleClip;
                float duration = walking ? Mathf.Min(0.18f, clip.length) : clip.length;
                if (recoveryElapsed < 0 || recoveryElapsed >= duration) return false;
                Pose = walking ? IreliaPose.QToRun : IreliaPose.QToIdle; sample = recoveryElapsed;
            }
            CycleTime = sample;
            Sample(clip, sample, dt, dashing && elapsed >= 0.04f, basis);
            return true;
        }
        public bool TickW(bool charging, float chargeElapsed, float releaseElapsed, float releaseDelay, float dt)
        {
            if (WChargeClip == null || WReleaseClip == null || WRecoveryClip == null) return false;
            AnimationClip clip; float sample;
            if (charging)
            {
                Pose = IreliaPose.WCharge; clip = WChargeClip;
                // The source contains the entrance and held guard. Never replay the entrance while held.
                sample = Mathf.Min(chargeElapsed, clip.length - 0.001f);
            }
            else
            {
                // The forward blade sweep ends at 0.3 s in spell2_2, before the rig blades
                // disappear into the projectile effect. Retiming that contact
                // to the existing gameplay release delay preserves responsive taps and damage timing.
                const float contact = 0.3f;
                float releaseDuration = releaseDelay + WReleaseClip.length - contact;
                if (releaseElapsed < 0 || releaseElapsed >= releaseDuration + WRecoveryClip.length) return false;
                if (releaseElapsed < releaseDuration)
                {
                    Pose = IreliaPose.WRelease; clip = WReleaseClip;
                    sample = releaseElapsed <= releaseDelay ? releaseElapsed / Mathf.Max(0.001f, releaseDelay) * contact :
                        contact + releaseElapsed - releaseDelay;
                }
                else { Pose = IreliaPose.WRecovery; clip = WRecoveryClip; sample = releaseElapsed - releaseDuration; }
            }
            CycleTime = sample;
            Sample(clip, sample, dt, !charging && releaseElapsed >= releaseDelay);
            return true;
        }
        void Sample(AnimationClip clip, float sample, float dt, bool exact, AnimationClip basis = null)
        {
            for (int i = 0; i < joints.Length; i++)
            { previousPositions[i] = joints[i].localPosition; previousRotations[i] = joints[i].localRotation; previousScales[i] = joints[i].localScale; }
            // Q's chain transition animates fewer bones. Start from its dash pose so omitted
            // tracks cannot inherit a previous W/attack pose.
            if (basis != null) basis.SampleAnimation(gameObject, Mathf.Min(sample, basis.length));
            clip.SampleAnimation(gameObject, sample);
            for (int i = 0; i < joints.Length; i++)
                if (joints[i] == transform || joints[i].parent == transform)
                { joints[i].localPosition = positions[i]; joints[i].localRotation = rotations[i]; joints[i].localScale = scales[i]; }
            Vector3 p = pelvis.localPosition; p.x = pelvisRest.x; p.z = pelvisRest.z; pelvis.localPosition = p;
            float blend = exact ? 1 : 1 - Mathf.Exp(-dt / BlendSeconds);
            for (int i = 0; i < joints.Length; i++)
            {
                joints[i].localPosition = Vector3.Lerp(previousPositions[i], joints[i].localPosition, blend);
                joints[i].localRotation = Quaternion.Slerp(previousRotations[i], joints[i].localRotation, blend);
                joints[i].localScale = Vector3.Lerp(previousScales[i], joints[i].localScale, blend);
            }
        }
        public void ResetPose()
        {
            Pose = IreliaPose.Idle; CycleTime = attackTime = 0;
            AttackVariant = 1; nextAttack2 = false;
            ClearEOverlay();
            if (joints == null) return;
            for (int i = 0; i < joints.Length; i++)
            { joints[i].localPosition = positions[i]; joints[i].localRotation = rotations[i]; joints[i].localScale = scales[i]; }
            IdleClip.SampleAnimation(gameObject, 0);
        }
    }
}
