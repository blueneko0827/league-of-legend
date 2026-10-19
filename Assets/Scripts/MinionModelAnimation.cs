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
        public void Configure(GameObject model, string name)
        {
            rig = model; string path = OfficialLaneUnitAssets.Root + name + "/";
            IdleClip = Resources.Load<AnimationClip>(path + "Idle");
            RunClip = Resources.Load<AnimationClip>(path + "Run");
            AttackClip = Resources.Load<AnimationClip>(path + "Attack");
            var list = new System.Collections.Generic.List<Transform> { rig.transform };
            foreach (Transform child in rig.transform) list.Add(child);
            roots = list.ToArray(); positions = new Vector3[roots.Length]; scales = new Vector3[roots.Length]; rotations = new Quaternion[roots.Length];
            for (int i = 0; i < roots.Length; i++)
            { positions[i] = roots[i].localPosition; scales[i] = roots[i].localScale; rotations[i] = roots[i].localRotation; }
        }
        public void Bind(LaneWorld lane, Combatant owner)
        { world = lane; actor = owner; observedAttack = owner.BasicAttackSequence; }
        void LateUpdate()
        {
            if (actor == null || world.Paused || !actor.Alive || actor.IsStunned) return;
            float dt = Time.deltaTime; bool moving = actor.Velocity.sqrMagnitude > .0004f;
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
                Sample(AttackClip, Mathf.Min(time, AttackClip.length));
                if (attackTime >= cycle) attacking = false;
            }
            else
            {
                string pose = moving ? "Run" : "Idle"; if (Pose != pose) cycleTime = 0; Pose = pose;
                var clip = moving ? RunClip : IdleClip;
                cycleTime = (cycleTime + dt * (moving ? Mathf.Clamp(actor.Velocity.magnitude / 3.25f, .1f, 2) : 1)) % clip.length;
                Sample(clip, cycleTime);
            }
        }
        void Sample(AnimationClip clip, float time)
        {
            clip.SampleAnimation(rig, time);
            // Discard authored object/root motion; navigation stays entirely on the gameplay root.
            for (int i = 0; i < roots.Length; i++)
            { roots[i].localPosition = positions[i]; roots[i].localScale = scales[i]; roots[i].localRotation = rotations[i]; }
        }
    }
}
