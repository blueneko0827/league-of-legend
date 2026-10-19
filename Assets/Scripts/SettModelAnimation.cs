using UnityEngine;

namespace MidLane
{
    public enum SettPose { Idle, Run, Attack, Frozen }

    // Presentation only: both punch timings, damage and Q resets remain owned by Combatant/SettAbilities.
    public sealed class SettModelAnimation : MonoBehaviour
    {
        public AnimationClip IdleClip { get; private set; }
        public AnimationClip RunClip { get; private set; }
        public AnimationClip AttackClip { get; private set; }
        public AnimationClip AttackStartClip { get; private set; }
        public bool Walking { get { return Pose == SettPose.Run; } }
        public SettPose Pose { get; private set; }
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
            IdleClip=Resources.Load<AnimationClip>(SettModelAssets.Root+"Idle");
            RunClip=Resources.Load<AnimationClip>(SettModelAssets.Root+"Run");
            AttackClip=Resources.Load<AnimationClip>(SettModelAssets.Root+"Attack");
            AttackStartClip=Resources.Load<AnimationClip>(SettModelAssets.Root+"AttackStart");
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
            { attackActive=false;observedAttack=actor.BasicAttackSequence;Pose=SettPose.Frozen;return; }
            if (actor.IsInStasis || actor.IsStunned || actor.ItemEffects.IsReviving)
            { attackActive=false;Pose=SettPose.Frozen;return; }
            float dt=Time.deltaTime;
            if (actor.BasicAttackSequence!=observedAttack)
            {
                observedAttack=actor.BasicAttackSequence;attackActive=true;attackTime=0;
                windup=actor.AttackWindupDuration;cycle=1/actor.BaseAttackSpeed;
            }
            bool moving=actor.CanAct && !actor.IsWindingAttack && actor.Velocity.sqrMagnitude>.0004f;
            if (attackActive && !actor.IsWindingAttack && (moving || actor.AttackCooldownRemaining<=0 || actor.ActionLockRemaining>0)) attackActive=false;
            if (actor.IsWindingAttack && actor.PendingAttackTarget!=null)
            {
                Vector3 facing=actor.PendingAttackTarget.Position-actor.Position;facing.y=0;
                if (facing.sqrMagnitude>.0001f) transform.rotation=Quaternion.LookRotation(facing);
            }
            if (attackActive)
            {
                attackTime+=dt;
                // The official attack is split into preparation and impact/recovery clips.
                // Scale their phases to the simulation's actual windup and attack interval.
                AnimationClip clip=attackTime<=windup ? AttackStartClip : AttackClip;
                float sample=attackTime<=windup ? attackTime/Mathf.Max(.001f,windup)*clip.length :
                    (attackTime-windup)/Mathf.Max(.001f,cycle-windup)*clip.length;
                Pose=SettPose.Attack;CycleTime=Mathf.Min(sample,clip.length-.001f);Sample(clip,CycleTime,dt);
                if (attackTime>=cycle) attackActive=false;
            }
            else
            {
                SettPose pose=moving?SettPose.Run:SettPose.Idle;
                if (pose!=Pose) CycleTime=0;
                Pose=pose;AnimationClip clip=moving?RunClip:IdleClip;
                CycleTime=(CycleTime+dt*(moving?Mathf.Clamp(actor.Velocity.magnitude/3.4f,.1f,2):1))%clip.length;
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
