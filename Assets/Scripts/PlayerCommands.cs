using UnityEngine;

namespace MidLane
{
    public sealed class PlayerCommands : MonoBehaviour
    {
        LaneWorld world;
        public string Feedback { get; private set; }
        public Combatant HoveredTarget { get; private set; }
        Combatant inspectedTarget;
        public Combatant InspectedTarget
        {
            get
            {
                if (inspectedTarget == null || !inspectedTarget.Alive || !inspectedTarget.gameObject.activeInHierarchy || !world.Units.Contains(inspectedTarget))
                    inspectedTarget = null;
                return inspectedTarget;
            }
        }
        float feedbackUntil;
        bool keyboardCharge;
        public bool DummyPlacementActive { get; private set; }
        IreliaRAim rAim;
        public PlayerLatency Latency { get; private set; }
        static readonly KeyCode[] SkillKeys = { KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R };
        public void Initialize(LaneWorld lane)
        {
            world = lane; rAim = gameObject.AddComponent<IreliaRAim>(); rAim.Initialize(lane);
            Latency = gameObject.AddComponent<PlayerLatency>(); Latency.Initialize(lane);
        }
        public void ResetPendingInputs()
        { keyboardCharge = DummyPlacementActive = false; HoveredTarget = inspectedTarget = null; if (rAim != null) rAim.Cancel(); if (Latency != null) Latency.Clear(); }
        public bool InspectAtScreenPoint(Vector2 point)
        {
            var hud = world.GetComponent<PracticeHud>();
            if (ShopHud.Open || DummyPlacementActive || hud.LatencyFieldFocused || hud.RunePickerOpen || hud.SummonerPickerOpen ||
                !world.View.pixelRect.Contains(point) || PracticeHud.BlocksPointer(new Vector2(point.x, Screen.height - point.y))) return false;
            inspectedTarget = TargetPicker.ResolveForInspection(world, point);
            return true;
        }
        public void CancelForRunePicker()
        { CancelDummyPlacement(); HoveredTarget=null; if(rAim!=null) rAim.Cancel(); if(Latency!=null) Latency.Clear(); }
        public bool ToggleDummyPlacement()
        {
            if (DummyPlacementActive) { CancelDummyPlacement(); return true; }
            if (!world.CanRepositionPvpDummy || ShopHud.Open || world.GetComponent<PracticeHud>().SummonerPickerOpen || world.GetComponent<PracticeHud>().RunePickerOpen) return false;
            rAim.Cancel(); Latency.Clear(); HoveredTarget = null; DummyPlacementActive = true; return true;
        }
        public void CancelDummyPlacement() { DummyPlacementActive = false; }
        public bool PlaceDummyAtScreenPoint(Vector2 point)
        {
            if (!DummyPlacementActive || ShopHud.Open || !world.CanRepositionPvpDummy ||
                PracticeHud.BlocksPointer(new Vector2(point.x, Screen.height - point.y))) return false;
            var plane = new Plane(Vector3.up, Vector3.zero); float enter;
            Ray ray = world.View.ScreenPointToRay(point);
            if (!plane.Raycast(ray, out enter) || !world.RepositionPvpDummy(ray.GetPoint(enter)))
            { ShowFeedback("Elegí un punto libre dentro de la línea."); return false; }
            DummyPlacementActive = false; ShowFeedback("Posición del dummy guardada."); return true;
        }
        void OnDisable() { ResetPendingInputs(); }
        public bool RequestMove(Vector3 point)
        {
            rAim.Cancel(); var actor = world.Player;
            if (!Latency.Submit(() => actor.MoveTo(point))) return false;
            // Order voice lines play on the click, as in LoL's client.
            if (actor.Audio != null) actor.Audio.MoveOrdered(point);
            return true;
        }
        public bool RequestAttack(Combatant target)
        {
            rAim.Cancel(); var actor = world.Player;
            if (!actor.ValidEnemy(target) || !Latency.Submit(() => actor.OrderAttack(target), target)) return false;
            if (actor.Audio != null) actor.Audio.AttackOrdered(target);
            return true;
        }
        public bool RequestStop()
        { rAim.Cancel(); var actor = world.Player; return Latency.Submit(() => actor.Stop()); }
        public bool RequestRecall()
        { ShowFeedback("Recall deshabilitado por ahora."); return false; }
        public bool RequestAbility(int slot, Vector3 aim, Combatant target = null)
        {
            if (slot < 0 || slot > 3) return false;
            var actor = world.Player;
            return Latency.Submit(() =>
            {
                actor.CancelRecall();
                bool cast = actor.Blade != null ? actor.Blade.Command(slot, aim, target) : actor.Cast(slot, aim, target);
                if (!cast) ShowFeedback(actor.Blade != null && actor.Blade.Rank(slot) == 0 ?
                    "Aprendé la habilidad con Ctrl + " + SkillKeys[slot] + " o el botón +" :
                    slot == 0 ? QFeedback(target) : "Habilidad no disponible: maná, enfriamiento o acción en curso");
            }, slot == 0 ? target : null);
        }
        public bool RequestWAim(Vector3 aim)
        { var actor = world.Player; return Latency.Submit(() => { if (actor.Blade != null) actor.Blade.AimW(aim); }); }
        public bool RequestWRelease(Vector3 aim)
        { var actor = world.Player; return Latency.Submit(() => { if (actor.Blade != null) actor.Blade.RequestWRelease(aim); }); }
        public bool RequestSummoner(int slot, Vector3 aim, Combatant target = null)
        {
            if (slot < 0 || slot > 1) return false;
            var actor = world.Player; SummonerSpell spell = actor.Summoners.Equipped(slot);
            return Latency.Submit(() =>
            {
                if (actor.Summoners.Equipped(slot) != spell) return;
                if (!actor.Summoners.CastSlot(slot, aim, target)) ShowFeedback(spell + ": no disponible; revisá enfriamiento, objetivo y alcance");
            }, spell == SummonerSpell.Ignite || spell == SummonerSpell.Exhaust ? target : null);
        }
        public bool RequestItem(int slot)
        {
            var actor = world.Player;
            if (slot < 0 || slot >= actor.Inventory.Items.Count) return false;
            int id = actor.Inventory.Items[slot].id;
            return Latency.Submit(() => { if (slot < actor.Inventory.Items.Count && actor.Inventory.Items[slot].id == id) actor.UseItem(slot); });
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5)) { rAim.Cancel(); world.Restart(); return; }
            ShopHud shop = world.GetComponent<ShopHud>();
            PracticeHud hud = world.GetComponent<PracticeHud>();
            if (hud.LatencyFieldFocused)
            {
                rAim.Cancel(); CancelDummyPlacement();
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Escape)) hud.EndLatencyEdit();
                return;
            }
            if (Input.GetKeyDown(KeyCode.P) && (!ShopHud.Open || !shop.SearchHasFocus)) shop.Toggle();
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (DummyPlacementActive) { CancelDummyPlacement(); return; }
                if (rAim.IsAiming) { rAim.Cancel(); return; }
                if (hud.RunePickerOpen) { hud.CloseRunePicker(); return; }
                if (hud.SummonerPickerOpen) { hud.CloseSummonerPicker(); return; } else if (ShopHud.Open) shop.Close(); else world.TogglePause();
            }
            if (ShopHud.Open) { rAim.Cancel(); CancelDummyPlacement(); HoveredTarget = null; return; }
            if (Time.unscaledTime > feedbackUntil) Feedback = "";
            bool upgrade = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool placeDummy = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            if (placeDummy && Input.GetKeyDown(KeyCode.B))
            { if (!ToggleDummyPlacement()) ShowFeedback("Alt+B: reubicar el dummy solo está disponible en PvP sin pausa."); return; }
            if (DummyPlacementActive)
            {
                if (!world.CanRepositionPvpDummy) { CancelDummyPlacement(); return; }
                if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) { CancelDummyPlacement(); return; }
                if (Input.GetMouseButtonDown(0)) PlaceDummyAtScreenPoint(Input.mousePosition);
                return;
            }
            if (upgrade) rAim.Cancel();
            if (upgrade && world.Player.Blade != null && !hud.RunePickerOpen)
                for (int slot = 0; slot < SkillKeys.Length; slot++)
                    if (Input.GetKeyDown(SkillKeys[slot]) && !world.Player.Blade.Learn(slot))
                        ShowFeedback(world.PvpModeActive ? "Modo PvP: las habilidades mejoran automáticamente con las bajas totales." :
                            "No se puede mejorar: revisá los puntos, el nivel y el rango máximo.");
            if (Input.GetMouseButtonDown(0)) InspectAtScreenPoint(Input.mousePosition);
            if (world.Paused || world.RoundOver || !world.Player.Alive) { rAim.Cancel(); if (!world.Player.Alive) keyboardCharge = false; return; }
            if(!hud.RunePickerOpen) for (int slot = 0; slot < 6; slot++) if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + slot))) RequestItem(slot);
            if (world.Player.IsInStasis) { rAim.Cancel(); return; }
            Vector2 mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            Ray ray = world.View.ScreenPointToRay(Input.mousePosition);
            HoveredTarget = PracticeHud.BlocksPointer(mouse) ? null : TargetPicker.Resolve(world, new Vector2(Input.mousePosition.x, Input.mousePosition.y));
            Combatant target = HoveredTarget;
            var plane = new Plane(Vector3.up, Vector3.zero);
            float enter; Vector3 ground = plane.Raycast(ray, out enter) ? ray.GetPoint(enter) : world.Player.Position;
            ground.y = 0;
            // Movement and summoners stay inside the lane. Abilities aim at the cursor itself, as in
            // LoL: the E's blades can land outside the lane (river, jungle) within their 775 range,
            // and the W/R directions are not bent by the lane edge.
            Vector3 aim = world.Clamp(ground), spellAim = ground;
            var blade = world.Player.Blade;
            if (keyboardCharge && blade != null)
            {
                RequestWAim(spellAim);
                if (!Input.GetKey(KeyCode.W)) { RequestWRelease(spellAim); keyboardCharge = false; }
            }
            if(hud.RunePickerOpen)
            {
                rAim.Cancel();
                if((Input.GetMouseButtonDown(0)||Input.GetMouseButtonDown(1)||Input.GetMouseButtonDown(2))&&!hud.RunePickerContainsScreenPoint(mouse)) hud.CloseRunePicker();
                return;
            }
            if (hud.SummonerPickerOpen)
            {
                rAim.Cancel();
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) && !hud.SummonerPickerContainsScreenPoint(mouse))
                    hud.CloseSummonerPicker();
                return;
            }
            if (Input.GetKeyDown(KeyCode.S)) RequestStop();
            if (Input.GetKeyDown(KeyCode.B)) { RequestRecall(); return; }
            bool cancelledAim = Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
            if (cancelledAim) rAim.Cancel();
            if (rAim.IsAiming)
            {
                rAim.Aim(spellAim);
                if (!Input.GetKey(KeyCode.R))
                {
                    if (PracticeHud.BlocksPointer(mouse)) rAim.Cancel();
                    else
                    {
                        Combatant caster; Vector3 point;
                        if (!rAim.TakeAim(out caster, out point) || !RequestAbility(3, point)) ShowFeedback("R no disponible: maná, enfriamiento o control de masas");
                    }
                }
            }
            if (PracticeHud.BlocksPointer(mouse)) return;
            if (!upgrade) for (int slot = 0; slot < 2; slot++)
                if (Input.GetKeyDown(slot == 0 ? KeyCode.D : KeyCode.F)) RequestSummoner(slot, aim, target);
            if (Input.GetMouseButtonDown(1))
            {
                if (world.Player.ValidEnemy(target)) RequestAttack(target);
                else RequestMove(aim);
            }
            if (Input.GetMouseButtonDown(2))
            {
                Combatant best = target; float closest = 3;
                foreach (Combatant unit in world.Units)
                {
                    if (target != null) break;
                    if (!world.Player.ValidEnemy(unit)) continue;
                    float distance = Vector3.Distance(aim, unit.Position);
                    if (distance < closest) { closest = distance; best = unit; }
                }
                if (best != null) RequestAttack(best); else RequestMove(aim);
            }
            if (upgrade) return;
            for (int slot = 0; slot < SkillKeys.Length; slot++)
                if (Input.GetKeyDown(SkillKeys[slot]))
                {
                    bool cast = slot == 3 && blade != null ? !cancelledAim && BeginR(spellAim) :
                        RequestAbility(slot, spellAim, target);
                    if (cast && slot == 1 && blade != null) keyboardCharge = true;
                    if (!cast)
                        ShowFeedback(blade != null && blade.Rank(slot) == 0 ? "Aprendé la habilidad con Ctrl + " + SkillKeys[slot] + " o el botón +" :
                            slot == 0 ? QFeedback(target) :
                            "Habilidad no disponible: maná, enfriamiento o acción en curso");
                }
        }
        // R as in LoL's quick cast: on the key press, toward the cursor, with no aiming arrow.
        // "r_aim_indicator" (ScenePresentation.json) restores holding R to aim with the arrow.
        bool BeginR(Vector3 aim)
        {
            bool arrow = world.Presentation.r_aim_indicator;
            if (!rAim.Begin(aim, arrow)) return false;
            if (arrow) return true;
            Combatant caster; Vector3 point;
            return rAim.TakeAim(out caster, out point) && RequestAbility(3, point);
        }
        string QFeedback(Combatant target)
        {
            if (target == null) return null;
            if (target.Kind == UnitKind.Tower) return "Q no puede apuntar a una torre";
            if (Vector3.Distance(world.Player.Position, target.Position) > world.Player.Blade.Tuning.qRange + target.Radius)
                return "Q: " + target.DisplayName + " está fuera de alcance";
            if (world.Player.Cooldowns.Remaining(0, Time.time) > 0) return "Q está en enfriamiento";
            if (world.Player.Resource < world.Player.AbilityCost(0)) return "Q: maná insuficiente";
            return "Q: no disponible durante esta acción o control de masas";
        }
        void ShowFeedback(string message) { if (string.IsNullOrEmpty(message)) return; Feedback = message; feedbackUntil = Time.unscaledTime + 2.5f; }
    }
}
