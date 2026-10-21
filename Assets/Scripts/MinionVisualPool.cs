using System.Collections.Generic;
using UnityEngine;

namespace MidLane
{
    // Pool the expensive visual rigs only. Every spawn gets a fresh logical actor,
    // so old projectiles, marks, aggro and damage-over-time cannot follow a reused life.
    public sealed class MinionVisualPool
    {
        readonly LaneWorld world;
        readonly Stack<Transform>[] available = new Stack<Transform>[6];
        readonly Dictionary<Transform, int> leased = new Dictionary<Transform, int>();
        Transform storage;
        int refillCursor;
        public int Created { get; private set; }
        public int Reused { get; private set; }
        public MinionVisualPool(LaneWorld owner) { world = owner; }
        static int Key(Team team, MinionType type) { return (team == Team.Blue ? 0 : 3) + (int)type; }
        void EnsureStorage()
        {
            if (storage != null) return;
            storage = new GameObject("Inactive minion visuals").transform; storage.SetParent(world.transform, false);
            storage.gameObject.SetActive(false);
            for (int i = 0; i < available.Length; i++) available[i] = new Stack<Transform>();
        }
        Transform Create(Team team, MinionType type)
        {
            var model = new GameObject("Model").transform; model.SetParent(storage, false);
            MinionVisuals.Create(world, model, team, type); Created++; return model;
        }
        public void Prepare()
        {
            EnsureStorage();
            // Two complete waves including cannons, before the match clock starts.
            for (int team = 0; team < 2; team++)
                foreach (var type in new[] { MinionType.Melee, MinionType.Caster, MinionType.Cannon })
                {
                    var bucket = available[Key((Team)team, type)];
                    int capacity = type == MinionType.Cannon ? 2 : 6;
                    while (bucket.Count < capacity) bucket.Push(Create((Team)team, type));
                }
        }
        public Transform Rent(Team team, MinionType type, Transform parent)
        {
            EnsureStorage(); int key = Key(team, type); Transform model = null;
            while (available[key].Count > 0 && model == null) model = available[key].Pop();
            if (model == null) model = Create(team, type); else Reused++;
            model.SetParent(parent, false); model.localPosition = Vector3.zero;
            model.localRotation = Quaternion.identity; model.localScale = Vector3.one;
            model.gameObject.SetActive(true); leased.Add(model, key); return model;
        }
        public void Replenish()
        {
            if (storage == null) return;
            // Restore the reserve gradually between waves, never a batch in one frame.
            for (int i = 0; i < 6; i++)
            {
                int key = refillCursor; refillCursor = (refillCursor + 1) % 6;
                var type = (MinionType)(key % 3);
                if (available[key].Count >= (type == MinionType.Cannon ? 2 : 6)) continue;
                available[key].Push(Create(key < 3 ? Team.Blue : Team.Red, type)); return;
            }
        }
        public void Return(Combatant actor)
        {
            if (actor == null) return;
            Transform model = actor.transform.Find("Model");
            if (model == null || !leased.TryGetValue(model, out int key)) return;
            leased.Remove(model); model.GetComponent<MinionModelAnimation>().Unbind();
            model.gameObject.SetActive(false); model.SetParent(storage, false);
            if (available[key].Count < 12) available[key].Push(model);
            else Object.Destroy(model.gameObject);
        }
        public void Clear()
        {
            // Leased rigs are still owned by the old round, which Restart destroys.
            leased.Clear();
            if (storage != null) Object.Destroy(storage.gameObject);
            storage = null; refillCursor = 0; Created = Reused = 0;
        }
    }
}
