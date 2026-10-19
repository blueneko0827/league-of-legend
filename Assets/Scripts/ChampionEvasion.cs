using System.Collections.Generic;
using UnityEngine;
namespace MidLane
{
    public sealed partial class ChampionBrain
    {
        sealed class EResponse
        {
            public float ReactAt;
            public bool Attempt, Committed;
            public Vector3 Point;
        }
        readonly Dictionary<BladeLink, EResponse> eResponses = new Dictionary<BladeLink, EResponse>();
        public float EDodgeChance { get { return world.PvpModeActive && self.Archetype == Archetype.Brawler ? 0 : world.Settings.difficulty == Difficulty.Expert ? .55f : world.Settings.difficulty == Difficulty.Hard ? .4f : .25f; } }
        bool TryDodgeBladeLinks()
        {
            if (world.PvpModeActive && self.Archetype == Archetype.Brawler) { eResponses.Clear(); return false; }
            if (world.BladeLinks.Count == 0) { eResponses.Clear(); return false; }
            foreach (BladeLink link in world.BladeLinks)
            {
                if (link == null || !link.HasSecond) continue;
                EResponse response;
                if (!eResponses.TryGetValue(link, out response))
                {
                    if (!link.Threatens(self)) continue;
                    response = new EResponse {
                        // Notice the visible second blade, then react; never read a future placement.
                        ReactAt = Time.time + Reaction + Random.Range(.04f, .1f),
                        Attempt = Random.value < EDodgeChance * (self.IsWindingAttack ? .7f : 1)
                    };
                    eResponses.Add(link, response);
                }
                // A failed decision is final for this cast, rather than rerolled every frame.
                if (!response.Attempt || Time.time < response.ReactAt) continue;
                if (!response.Committed)
                {
                    if (!link.Threatens(self)) continue;
                    Vector3 side = Vector3.Cross(link.Second - link.First, Vector3.up).normalized;
                    if (side.sqrMagnitude < .01f) side = Vector3.forward;
                    if (Vector3.Dot(self.Position - link.First, side) < 0) side = -side;
                    Vector3 point = world.Clamp(self.Position + side * Random.Range(1.35f, 1.85f), self.Radius);
                    if (TowerRange(EnemyTower(), point)) { response.Attempt = false; continue; }
                    response.Point = point; response.Committed = true;
                }
                MoveOrHold(response.Point); Decision = "Intentar esquivar E"; return true;
            }
            return false;
        }
    }
}
