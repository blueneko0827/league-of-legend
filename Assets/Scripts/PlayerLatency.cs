using System;
using System.Collections.Generic;
using UnityEngine;

namespace MidLane
{
    // Local approximation of the total visible response delay, not a network stack.
    // Only human orders enter this queue; AI, cooldowns and damage keep their clocks.
    [DefaultExecutionOrder(-50)]
    public sealed class PlayerLatency : MonoBehaviour
    {
        public const int MaxMilliseconds = 1000;
        sealed class Pending
        {
            public Action Action;
            public Combatant Actor, Target;
            public int ActorLife, TargetLife;
            public bool HasTarget;
            public double Due;
        }
        readonly Queue<Pending> pending = new Queue<Pending>();
        LaneWorld world;
        double lastDue;
        public int PendingCount { get { return pending.Count; } }
        public void Initialize(LaneWorld lane) { world = lane; }
        public bool Submit(Action action, Combatant target = null)
        {
            var actor = world.Player;
            if (action == null || actor == null || !actor.Alive || world.Paused || world.RoundOver) return false;
            var order = new Pending { Action = action, Actor = actor, ActorLife = actor.Deaths,
                Target = target, TargetLife = target != null ? target.Deaths : 0, HasTarget = target != null };
            double now = Time.timeAsDouble;
            order.Due = Math.Max(now + world.SimulatedPingMs / 1000.0, pending.Count > 0 ? lastDue : now);
            if (world.SimulatedPingMs == 0 && pending.Count == 0) { Execute(order); return true; }
            lastDue = order.Due; pending.Enqueue(order); return true;
        }
        void Update() { Process(); }
        public void Process()
        {
            if (world == null) return;
            if (world.Player == null || !world.Player.Alive || world.RoundOver) { Clear(); return; }
            if (world.Paused) return;
            while (pending.Count > 0 && pending.Peek().Due <= Time.timeAsDouble)
                Execute(pending.Dequeue());
        }
        void Execute(Pending order)
        {
            if (order.Actor == null || order.Actor != world.Player || !order.Actor.Alive || order.Actor.Deaths != order.ActorLife) return;
            if (order.HasTarget && (order.Target == null || !order.Target.Alive || order.Target.Deaths != order.TargetLife)) return;
            order.Action();
        }
        public void Clear() { pending.Clear(); lastDue = 0; }
        void OnDisable() { Clear(); }
    }
}
