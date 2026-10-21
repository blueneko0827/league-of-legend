using UnityEngine;

namespace MidLane
{
    // Presentation only: attack timing, missiles and damage remain owned by Combatant.
    public sealed class MinionModelAnimation : MonoBehaviour
    {
        public AnimationClip IdleClip { get; private set; }
        public AnimationClip RunClip { get; private set; }
        public AnimationClip AttackClip { get; private set; }
        public string Pose { get; private set; } = "Idle";
        GameObject rig; LaneWorld world; Combatant actor;
        Transform[] roots; Vector3[] positions, scales; Quaternion[] rotations;
        int observedAttack; float cycleTime, attackTime, windup, cycle; bool attacking;
        float nextSampleAt;
        bool wasVisible;
        CameraFacingVisualTilt tilt;
        public void Configure(GameObject model, string name)
        {
            rig = model; var assets = OfficialLaneUnitAssets.Load(name);
            IdleClip = assets.Idle; RunClip = assets.Run; AttackClip = assets.Attack;
            tilt = GetComponentInChildren<CameraFacingVisualTilt>(true);
            if (tilt != null) tilt.SampleDriven = true;
            var list = new System.Collections.Generic.List<Transform> { rig.transform };
            foreach (Transform child in rig.transform) list.Add(child);
            roots = list.ToArray(); positions = new Vector3[roots.Length]; scales = new Vector3[roots.Length]; rotations = new Quaternion[roots.Length];
            for (int i = 0; i < roots.Length; i++)
            { positions[i] = roots[i].localPosition; scales[i] = roots[i].localScale; rotations[i] = roots[i].localRotation; }
            // Resolve native animation bindings before gameplay, including the first cannon.
            Sample(RunClip, 0); Sample(AttackClip, 0); Sample(IdleClip, 0);
        }
        public void Bind(LaneWorld lane, Combatant owner)
        {
            world = lane; actor = owner; observedAttack = owner.BasicAttackSequence;
            cycleTime = attackTime = nextSampleAt = 0; attacking = wasVisible = false; Pose = "Idle";
            Sample(IdleClip, 0);
        }
        public void Unbind() { actor = null; world = null; attacking = false; nextSampleAt = 0; }
        void LateUpdate()
        {
            if (actor == null || world.Paused || !actor.Alive || actor.IsStunned) return;
            float dt = Time.deltaTime; bool moving = actor.Velocity.sqrMagnitude > .04f;
            bool newAttack = observedAttack != actor.BasicAttackSequence;
            if (observedAttack != actor.BasicAttackSequence)
            {
                observedAttack = actor.BasicAttackSequence; attacking = true; attackTime = 0;
                windup = actor.AttackWindupDuration; cycle = 1 / actor.BaseAttackSpeed;
            }
            if (attacking && !actor.IsWindingAttack && moving) attacking = false;
            if (attacking)
            {
                Pose = "Attack"; attackTime += dt;
                float contact = AttackClip.length * .35f;
                float time = attackTime <= windup ? attackTime / Mathf.Max(.001f, windup) * contact :
                    contact + (attackTime - windup) / Mathf.Max(.001f, cycle - windup) * (AttackClip.length - contact);
                if (ShouldSample(newAttack, true)) Sample(AttackClip, Mathf.Min(time, AttackClip.length));
                if (attackTime >= cycle) attacking = false;
            }
            else
            {
                string pose = moving ? "Run" : "Idle"; bool changed = Pose != pose;
                if (changed) cycleTime = 0; Pose = pose;
                var clip = moving ? RunClip : IdleClip;
                cycleTime = (cycleTime + dt * (moving ? Mathf.Clamp(actor.Velocity.magnitude / 3.25f, .1f, 2) : 1)) % clip.length;
                if (ShouldSample(changed, false)) Sample(clip, cycleTime);
            }
        }
        bool ShouldSample(bool changed, bool attack)
        {
            Vector3 screen = world.View.WorldToViewportPoint(actor.Position + Vector3.up * actor.VisualHeight * .5f);
            bool visible = screen.z > 0 && screen.x > -.12f && screen.x < 1.12f && screen.y > -.2f && screen.y < 1.2f;
            changed |= visible && !wasVisible; wasVisible = visible;
            if (!changed && !(visible && attack) && Time.time < nextSampleAt) return false;
            // Visible movement remains at up to 60 Hz; attacks sample every visible frame.
            // Off-screen rigs keep their clocks but need only a sparse presentation update.
            float interval = visible ? 1f / 60 : .1f;
            nextSampleAt = changed || (visible && attack) ? Time.time + interval : Mathf.Max(Time.time, nextSampleAt + interval);
            return true;
        }
        void Sample(AnimationClip clip, float time)
        {
            clip.SampleAnimation(rig, time);
            // Discard authored object/root motion; navigation stays entirely on the gameplay root.
            for (int i = 0; i < roots.Length; i++)
            { roots[i].localPosition = positions[i]; roots[i].localScale = scales[i]; roots[i].localRotation = rotations[i]; }
            if (tilt != null) tilt.InvalidatePose();
        }
    }
}
