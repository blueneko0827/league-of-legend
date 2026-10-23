using UnityEngine;

namespace MidLane
{
    // Input preview only: holding R never spends mana, starts an animation or creates a hitbox.
    public sealed class IreliaRAim : MonoBehaviour
    {
        LaneWorld world;
        Combatant actor;
        Vector3 cursor;
        int actorLife;
        IreliaDirectionIndicator indicator;
        bool showIndicator;
        public bool IsAiming { get; private set; }
        public Vector3 Direction { get; private set; }
        public GameObject Indicator { get { return indicator != null ? indicator.gameObject : null; } }
        public float Range { get { return actor != null ? actor.Blade.Tuning.rRange : 0; } }
        public float Width { get { return actor != null ? actor.Blade.Tuning.rHalfWidth * 2 : 0; } }
        public void Initialize(LaneWorld lane) { world = lane; }
        // show: draw the aiming arrow (hold-to-aim mode); quick cast only takes the direction.
        public bool Begin(Vector3 point, bool show = true)
        {
            Cancel();
            Combatant player = world.Player;
            if (player == null || player.Blade == null) return false;
            actor = player; actorLife = actor.Deaths; cursor = point; IsAiming = true; showIndicator = show;
            if (!CanContinue()) { Cancel(); return false; }
            if (show && indicator == null) indicator=IreliaDirectionIndicator.Create(transform,"R · hold to aim");
            Present(); return true;
        }
        bool CanContinue()
        {
            return IsAiming && actor != null && actor == world.Player && actor.Deaths == actorLife && actor.Targetable &&
                !world.Paused && !world.RoundOver && !actor.IsInStasis && !actor.IsStunned && !actor.Blade.IsCharging &&
                actor.Blade.Rank(3) > 0 && actor.Cooldowns.Ready(3, Time.time) &&
                (actor.UnlimitedMana || actor.Resource >= actor.AbilityCost(3));
        }
        public void Aim(Vector3 point)
        {
            if (!CanContinue()) { Cancel(); return; }
            cursor = point; Present();
        }
        public bool Release()
        {
            Combatant caster; Vector3 point;
            if (!TakeAim(out caster, out point)) return false;
            return caster.Blade.Command(3, point, null);
        }
        public bool TakeAim(out Combatant caster, out Vector3 point)
        {
            caster = null; point = Vector3.zero;
            if (!CanContinue()) { Cancel(); return false; }
            Present(); caster = actor; point = actor.Position + Direction * Range; Cancel(); return true;
        }
        public void Cancel()
        { IsAiming = false; actor = null; if (indicator != null) indicator.Hide(); }
        void LateUpdate()
        {
            if (!IsAiming) return;
            if (!CanContinue()) { Cancel(); return; }
            Present();
        }
        void Present()
        {
            Vector3 delta = cursor - actor.Position; delta.y = 0;
            Direction = delta.sqrMagnitude > .0001f ? delta.normalized : actor.Team == Team.Blue ? Vector3.right : Vector3.left;
            if (indicator == null) return;
            if (showIndicator) indicator.Present(actor.Position,Direction,Range,Width); else indicator.Hide();
        }
        void OnDisable() { Cancel(); }
        void OnDestroy()
        {
            if (indicator != null) Destroy(indicator.gameObject);
        }
    }
}
