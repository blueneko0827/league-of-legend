using System.Collections.Generic;
using UnityEngine;
namespace MidLane
{
    public sealed partial class LaneWorld
    {
        public bool PracticeModeActive { get { return Settings != null && Settings.practiceMode; } }
        public TrainingDummy PvpQDummy { get; private set; }
        public readonly List<TrainingDummy> PracticeDummies = new List<TrainingDummy>();
        public bool PracticeMinionModelsActive { get; private set; }
        internal void TogglePracticeMinionModels()
        {
            if (!PracticeModeActive || Paused || RoundOver) return;
            bool enable = !PracticeMinionModelsActive;
            foreach (var dummy in PracticeDummies)
            {
                if (dummy == null) continue;
                bool center = !dummy.Mobile && dummy.Home.sqrMagnitude < .001f;
                dummy.SetMinionAppearance(enable, center ? MinionType.Melee : MinionType.Caster);
            }
            PracticeMinionModelsActive = enable;
        }
        public bool PermanentPracticeMarks
        {
            get
            {
                if (!PracticeModeActive || PracticeDummies.Count == 0) return false;
                foreach (var dummy in PracticeDummies)
                    if (dummy == null || !dummy.PermanentMark) return false;
                return true;
            }
        }
        internal void TogglePermanentPracticeMarks()
        {
            if (!PracticeModeActive || Paused || RoundOver) return;
            bool enable = !PermanentPracticeMarks;
            foreach (var dummy in PracticeDummies)
                if (dummy != null)
                {
                    if (enable) dummy.EnablePermanentMark();
                    else dummy.DisablePermanentMark();
                }
            if (enable && Player != null) Player.Cooldowns.Reset(2);
        }
        public const float PracticeCooldownResetSeconds = 3.5f;
        bool practiceResetArmed;
        float practiceResetAt;
        public float PracticeCooldownResetRemaining { get { return practiceResetArmed ? Mathf.Max(0, practiceResetAt - Time.time) : 0; } }
        public void TogglePracticeMode()
        {
            EnforcePracticeVisualMode();
        }
        void CreatePracticeTargets(Team team)
        {
            Player.SetPracticeGold(); Player.transform.position = new Vector3(-4, 0, 0); Player.Stop();
            CreatePracticeDummy(team, Vector3.zero, false);
            CreatePracticeDummy(team, new Vector3(4, 0, 2.5f), true);
            CreatePracticeDummy(team, new Vector3(0, 0, -3), false);
            CreatePracticeDummy(team, new Vector3(0, 0, 3), false);
            CreatePracticeDummy(team, new Vector3(-8, 0, 0), true, true);
        }
        TrainingDummy CreatePracticeDummy(Team team, Vector3 position, bool mobile, bool patrol = false, bool permanentMark = false)
        {
            var root = new GameObject(permanentMark ? "Dummy Q PvP" : patrol ? "Dummy de recorrido" : mobile ? "Dummy móvil" : "Dummy fijo"); root.transform.SetParent(roundRoot); root.transform.position = position;
            var model = new GameObject("Model").transform; model.SetParent(root.transform, false);
            var collider = root.AddComponent<CapsuleCollider>(); collider.height = 1.8f; collider.radius = .65f; collider.center = Vector3.up * .9f;
            var actor = root.AddComponent<Combatant>(); actor.Initialize(this, team, UnitKind.Champion, Archetype.Prism, root.name);
            var dummy = root.AddComponent<TrainingDummy>(); actor.ConfigureTrainingDummy(dummy, patrol ? 3.65f : 3.35f); dummy.Initialize(this, actor, mobile, position);
            if (patrol) dummy.EnablePatrol(new Rect(-12, -4, 8, 8));
            model.rotation = Quaternion.LookRotation(Player.Position - position);
            Units.Add(actor);
            if (permanentMark) dummy.EnablePermanentMark(); else PracticeDummies.Add(dummy);
            return dummy;
        }
        void CreatePvpQDummy()
        {
            if (!PvpModeActive || PvpQDummy != null) return;
            Vector3 recovery = FountainPoint(Player.Team);
            // Reflect recovery across the lane centerline, beside Irelia's actual team turret.
            Vector3 position = new Vector3(recovery.x, 0, -recovery.z);
            Team targetTeam = Player.Team == Team.Blue ? Team.Red : Team.Blue;
            PvpQDummy = CreatePracticeDummy(targetTeam, position, false, permanentMark: true);
            RestorePvpDummyPosition();
        }
        public void PracticeAbilityUsed(Combatant actor)
        {
            if ((!PracticeModeActive && !PvpModeActive) || actor != Player) return;
            practiceResetArmed = true; practiceResetAt = Time.time + PracticeCooldownResetSeconds;
        }
        public void PracticeDamageDealt(Combatant source, float amount)
        {
            if ((PracticeModeActive || PvpModeActive) && source == Player && amount > 0 && practiceResetArmed) practiceResetAt = Time.time + PracticeCooldownResetSeconds;
        }
        void TickPracticeMode()
        {
            if ((!PracticeModeActive && !PvpModeActive) || Paused || !practiceResetArmed) return;
            // PvP uses the same player-only schedule; dead/reviving time must not carry over.
            if (Player == null || !Player.Alive || Player.ItemEffects.IsReviving)
            { practiceResetArmed = false; return; }
            if (Time.time < practiceResetAt) return;
            Player.ResetAbilityCooldowns(); practiceResetArmed = false;
        }
    }
}
