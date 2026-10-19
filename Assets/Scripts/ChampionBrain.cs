using UnityEngine;

namespace MidLane
{
    public enum LaneTactic { Farm, Trade, AllIn, Zone, Push, Retreat, Recover, GoldTrap }

    public sealed partial class ChampionBrain : MonoBehaviour
    {
        public string Decision { get; private set; }
        public LaneTactic Tactic { get; private set; }
        public string PurchaseDecision { get; private set; }
        public float Reaction { get { return FinancialConfig != null ? FinancialUtility.Reaction(financialBaseReaction,
            self.Health / self.MaxHealth, FinancialConfig.panic_health_threshold, FinancialConfig.panic_reaction_multiplier) : self != null && self.Mel != null ? MelWReaction : CombatRules.Reaction(world.Settings.difficulty); } }
        LaneWorld world;
        Combatant self;
        float nextThink, lastSampleAt;
        bool lastDuelMode;
        Vector3 lastPlayerPosition, estimatedVelocity;
        readonly float[] observedReady = new float[4];
        readonly float[] pendingReady = new float[4];
        readonly float[] observeAt = new float[4];

        public void Initialize(LaneWorld lane, Combatant actor)
        {
            world = lane; self = actor; Decision = "Evaluando la línea";
            if (actor.Archetype == Archetype.Brawler) InitializeFinancialBrain();
            if (actor.Mel != null) ResetMelPlan();
            nextThink = Time.time + Reaction; lastSampleAt = Time.time;
            lastPlayerPosition = world.Player.Position;
            world.OnAbilityUsed += ObserveCast;
        }
        void OnDestroy() { if (world != null) world.OnAbilityUsed -= ObserveCast; }
        public void ForgetObservedCooldowns()
        {
            for (int slot = 0; slot < 4; slot++)
                observedReady[slot] = pendingReady[slot] = observeAt[slot] = 0;
        }
        void ObserveCast(Combatant actor, int slot, float readyAt)
        {
            if (actor != world.Player || Vector3.Distance(self.Position, actor.Position) > 14) return;
            pendingReady[slot] = readyAt; observeAt[slot] = Time.time + Reaction;
            if (FinancialConfig != null && slot == 0) ObserveFinancialDash(actor);
        }
        void Update()
        {
            if (world.Paused) return;
            PurchaseItems();
            if (!self.Alive) { ResetLanePlan(); ResetFinancialPlan(); if (self.Mel != null) ResetMelPlan(); return; }
            if (lastDuelMode != DuelModeActive)
            {
                lastDuelMode = DuelModeActive; ResetLanePlan();
                if (self.Mel != null) ResetMelPlan();
                LastHitUtility = PokeUtility = GoldTrapUtility = AllInUtility = AllInRoi = KillProbability = DeathProbability = 0;
                GoldTrapMinion = null;
            }
            if (WaitForPvpOpener()) return;
            if (self.Mel != null) { UpdateMelBrain(); return; }
            // Each visible E has its own human reaction clock, advanced frame by frame.
            if (self.CanAct && TryDodgeBladeLinks()) return;
            if (FinancialConfig != null) { UpdateFinancialBrain(); return; }
            if (Time.time < nextThink) return;
            nextThink = Time.time + Reaction;
            for (int i = 0; i < 4; i++)
                if (observeAt[i] > 0 && Time.time >= observeAt[i])
                { observedReady[i] = pendingReady[i]; observeAt[i] = 0; }
            float sampleDt = Time.time - lastSampleAt;
            estimatedVelocity = sampleDt > 0 ? (world.Player.Position - lastPlayerPosition) / sampleDt : Vector3.zero;
            estimatedVelocity = Vector3.ClampMagnitude(estimatedVelocity, world.Player.MoveSpeed);
            lastSampleAt = Time.time; lastPlayerPosition = world.Player.Position;
            if (!self.CanAct) return;
            ThinkLane();
        }
        public void PurchaseItems()
        {
            if (world.Paused) return;
            if (!self.Alive || !self.CanBuy)
            { PurchaseDecision = "Compras: al volver a la fuente"; return; }
            string reason;
            if (self.Archetype == Archetype.Brawler || self.Archetype == Archetype.Prism)
            {
                int[] build = self.Archetype == Archetype.Brawler ? SettBuild : MelBuild;
                for (int index = 0; index < build.Length; index++)
                {
                    int id = build[index];
                    if (self.Inventory.Has(id)) continue;
                    if (index == 5) SellStarterForSixthItem(id);
                    for (int purchases = 0; purchases < 20; purchases++)
                    {
                        if (self.BuyItem(id, out reason)) break;
                        bool bought = false;
                        foreach (int component in self.Inventory.MissingRecipePurchases(id))
                            if (self.BuyItem(component, out reason)) { bought = true; break; }
                        if (!bought) break;
                    }
                    if (!self.Inventory.Has(id))
                    {
                        PurchaseDecision = "Próximo: " + self.Inventory.Get(id).name + " · " + self.Gold + "/" + self.Inventory.PurchaseCost(id) + " oro";
                        return;
                    }
                }
                PurchaseDecision = "Build completa";
                return;
            }
            int starter = 3112;
            if (!self.Inventory.Has(starter) && self.Inventory.Items.Count == 0) self.BuyItem(starter, out reason);
            bool boots = self.Inventory.Items.Exists(item => item.boots);
            if (!boots) self.BuyItem(1001, out reason);
            int completed = self.Archetype == Archetype.Brawler ? 3153 : 3089;
            if (!self.Inventory.Has(completed)) self.BuyItem(completed, out reason);
        }
        void SellStarterForSixthItem(int id)
        {
            if (!world.PvpModeActive && !world.NormalplayModeActive) return;
            ItemDefinition starter = self.Inventory.Items.Find(item => item.starter);
            if (starter == null) return;
            int budget = self.Gold + starter.sell;
            bool affordable = budget >= self.Inventory.PurchaseCost(id);
            foreach (int component in self.Inventory.MissingRecipePurchases(id))
                affordable |= budget >= self.Inventory.PurchaseCost(component);
            if (affordable) self.SellBotStarter();
        }
        public static readonly int[] MelBuild = { 2503, 4628, 3020, 4645, 3089, 3135 };
        public static readonly int[] SettBuild = { 6631, 3047, 2501, 3053, 3083, 3161 };

    }
}
