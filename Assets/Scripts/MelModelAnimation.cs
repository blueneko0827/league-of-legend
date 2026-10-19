using UnityEngine;

namespace MidLane
{
    public enum MelPose { Idle, Run, Attack, Frozen }

    // Native clips are sampled for presentation only; projectiles and damage stay in Combatant/MelBolt.
    public sealed class MelModelAnimation : MonoBehaviour
    {
        public AnimationClip IdleClip { get; private set; }
        public AnimationClip RunClip { get; private set; }
        public AnimationClip AttackClip { get; private set; }
        public MelPose Pose { get; private set; }
        public float CycleTime { get; private set; }
        GameObject rig; LaneWorld world; Combatant actor;
        Transform[] joints; Transform pelvis;
        Vector3[] restPosition,restScale,previousPosition,previousScale;
        Quaternion[] restRotation,previousRotation;
        Vector3 pelvisRest;
        int observedAttack;
        float attackTime,windup,cycle;
        bool attackActive;
        public void Configure(GameObject model)
        {
            rig=model;
            IdleClip=Resources.Load<AnimationClip>(MelModelAssets.Root+"Idle");
            RunClip=Resources.Load<AnimationClip>(MelModelAssets.Root+"Run");
            AttackClip=Resources.Load<AnimationClip>(MelModelAssets.Root+"Attack");
            joints=rig.GetComponentsInChildren<Transform>();
            restPosition=new Vector3[joints.Length];restScale=new Vector3[joints.Length];restRotation=new Quaternion[joints.Length];
            previousPosition=new Vector3[joints.Length];previousScale=new Vector3[joints.Length];previousRotation=new Quaternion[joints.Length];
            for (int i=0;i<joints.Length;i++)
            {
                restPosition[i]=joints[i].localPosition;restScale[i]=joints[i].localScale;restRotation[i]=joints[i].localRotation;
                if (joints[i].name=="Root") pelvis=joints[i];
            }
            if (pelvis!=null) pelvisRest=pelvis.localPosition;
        }
        public void Bind(LaneWorld lane,Combatant owner)
        { world=lane;actor=owner;observedAttack=owner.BasicAttackSequence; }
        void LateUpdate()
        {
            if (actor==null || world.Paused) return;
            if (!actor.Alive)
            { attackActive=false;observedAttack=actor.BasicAttackSequence;Pose=MelPose.Frozen;return; }
            if (actor.IsInStasis || actor.IsStunned || actor.ItemEffects.IsReviving)
            { attackActive=false;Pose=MelPose.Frozen;return; }
            float dt=Time.deltaTime;
            if (actor.BasicAttackSequence!=observedAttack)
            {
                observedAttack=actor.BasicAttackSequence;attackActive=true;attackTime=0;
                windup=actor.AttackWindupDuration;cycle=1/actor.BaseAttackSpeed;
            }
            bool moving=actor.Velocity.sqrMagnitude>.0004f;
            if (attackActive && !actor.IsWindingAttack && (moving || actor.AttackCooldownRemaining<=0 || actor.ActionLockRemaining>0)) attackActive=false;
            if (actor.IsWindingAttack && actor.PendingAttackTarget!=null)
            {
                Vector3 facing=actor.PendingAttackTarget.Position-actor.Position;facing.y=0;
                if (facing.sqrMagnitude>.0001f) transform.rotation=Quaternion.LookRotation(facing);
            }
            if (attackActive)
            {
                attackTime+=dt;
                const float contact=.25f;
                float sample=attackTime<=windup?attackTime/Mathf.Max(.001f,windup)*contact:
                    contact+(attackTime-windup)/Mathf.Max(.001f,cycle-windup)*(AttackClip.length-contact);
                Pose=MelPose.Attack;CycleTime=Mathf.Min(sample,AttackClip.length-.001f);Sample(AttackClip,CycleTime,dt);
                if (attackTime>=cycle) attackActive=false;
            }
            else
            {
                MelPose pose=moving?MelPose.Run:MelPose.Idle;
                if (pose!=Pose) CycleTime=0;
                Pose=pose;AnimationClip clip=moving?RunClip:IdleClip;
                CycleTime=(CycleTime+dt*(moving?Mathf.Clamp(actor.Velocity.magnitude/3.3f,.1f,2):1))%clip.length;
                Sample(clip,CycleTime,dt);
            }
        }
        void Sample(AnimationClip clip,float time,float dt)
        {
            for (int i=0;i<joints.Length;i++)
            { previousPosition[i]=joints[i].localPosition;previousScale[i]=joints[i].localScale;previousRotation[i]=joints[i].localRotation; }
            clip.SampleAnimation(rig,time);
            // Keep imported object transforms and planar root motion out of navigation/hitboxes.
            for (int i=0;i<joints.Length;i++)
                if (joints[i]==rig.transform || joints[i].parent==rig.transform)
                { joints[i].localPosition=restPosition[i];joints[i].localScale=restScale[i];joints[i].localRotation=restRotation[i]; }
            if (pelvis!=null) { Vector3 p=pelvis.localPosition;p.x=pelvisRest.x;p.z=pelvisRest.z;pelvis.localPosition=p; }
            float blend=1-Mathf.Exp(-dt/.08f);
            for (int i=0;i<joints.Length;i++)
            {
                joints[i].localPosition=Vector3.Lerp(previousPosition[i],joints[i].localPosition,blend);
                joints[i].localScale=Vector3.Lerp(previousScale[i],joints[i].localScale,blend);
                joints[i].localRotation=Quaternion.Slerp(previousRotation[i],joints[i].localRotation,blend);
            }
        }
    }
}
