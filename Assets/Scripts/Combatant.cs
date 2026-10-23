using UnityEngine;
using System.Collections.Generic;

namespace MidLane
{
    public sealed partial class Combatant : MonoBehaviour
    {
        public Team Team { get; private set; }
        public UnitKind Kind { get; private set; }
        public MinionType MinionType { get; private set; }
        public MinionStats MinionStats { get; private set; }
        public Archetype Archetype { get; private set; }
        public string DisplayName { get; private set; }
        public float MaxHealth { get; private set; }
        public float Health { get; private set; }
        float armor, magicResist, attackDamage, percentArmorPenetration, percentMagicPenetration;
        public float Armor { get { return armor + BoardingPartyResistance + (ItemEffects != null ? ItemEffects.TerminusResistance : 0) + (Runes!=null?Runes.Resistance:0); } private set { armor = value; } }
        public float MagicResist { get { return magicResist + BoardingPartyResistance + (ItemEffects != null ? ItemEffects.TerminusResistance : 0) + (Runes!=null?Runes.Resistance:0); } private set { magicResist = value; } }
        public float BoardingPartyResistance { get { return CombatItemEffects.BoardingPartyResistance(world, this); } }
        public float Resource { get; private set; }
        public float MaxResource { get; private set; }
        public float AttackDamage { get { float raw=attackDamage+(Runes!=null?Runes.BonusAD:0); return ItemEffects != null ? ItemEffects.AdjustAttackDamage(raw) : raw; } private set { attackDamage = value; } }
        float attackRange;
        public float AttackRange { get { return attackRange + (Sett != null ? Sett.BonusAttackRange : 0); } private set { attackRange = value; } }
        public float MoveSpeed { get; private set; }
        public float Shield { get; private set; }
        public float TotalShield { get { return Shield + (Summoners != null ? Summoners.BarrierShield : 0) + (ItemEffects != null ? ItemEffects.TotalShield : 0); } }
        public CombatItemEffects ItemEffects { get; private set; }
        public SummonerSpells Summoners { get; private set; }
        public TrainingDummy Dummy { get; private set; }
        public bool UnlimitedMana { get { return world.PracticeModeActive && this == world.Player; } }
        public int SpellCastId { get; private set; }
        public int Stacks { get { return Blade != null ? Blade.Stacks : 0; } }
        public BladeDuelist Blade { get; private set; }
        public SettAbilities Sett { get; private set; }
        public MelAbilities Mel { get; private set; }
        public ChampionProgress Progress { get; private set; }
        public ChampionStatProfile BaseStats { get; private set; }
        public ItemInventory Inventory { get; private set; }
        float abilityPower;
        public float AbilityPower { get { return (abilityPower+(Runes!=null?Runes.BonusAP*(Inventory!=null&&Inventory.Has(3089)?1.3f:1):0)) * (1 + (ItemEffects != null ? ItemEffects.BlackfireAPBonus : 0)); } private set { abilityPower = value; } }
        float abilityHaste, bonusAttackSpeed;
        public float AbilityHaste { get { return abilityHaste+(Runes!=null?Runes.AbilityHaste:0); } private set { abilityHaste=value; } }
        public float BonusAttackSpeed { get { return bonusAttackSpeed+(Runes!=null?Runes.BonusAttackSpeed:0); } private set { bonusAttackSpeed=value; } }
        public float LifeSteal { get; private set; }
        public float Omnivamp { get; private set; }
        public float CritChance { get; private set; }
        public float Tenacity { get; private set; }
        public float ArmorPenetration { get; private set; }
        public float MagicPenetration { get; private set; }
        public float PercentArmorPenetration { get { return 1 - (1 - percentArmorPenetration) * (1 - (ItemEffects != null ? ItemEffects.TerminusPenetration : 0)); } private set { percentArmorPenetration = value; } }
        public float PercentMagicPenetration { get { return 1 - (1 - percentMagicPenetration) * (1 - (ItemEffects != null ? ItemEffects.TerminusPenetration : 0)); } private set { percentMagicPenetration = value; } }
        float healthRegenBonus, manaRegenBonus;
        public int Level { get { return Progress != null ? Progress.Level : 1; } }
        public float HealthRegeneration { get { return Kind == UnitKind.Champion ? BaseStats.HealthRegeneration.At(Level) + healthRegenBonus + (Sett != null ? Sett.PassiveHealthRegeneration : 0) +
            (!BaseStats.UsesMana && ItemEffects != null && Inventory.Has(1056) ? ItemEffects.DoranDrainRegeneration * Inventory.Get(1056).Value("ManaToHealthConversion", .45f) : 0) : 0; } }
        public float ManaRegeneration { get { return Kind == UnitKind.Champion ? BaseStats.ManaRegeneration.At(Level) + manaRegenBonus + (BaseStats.UsesMana && ItemEffects != null ? ItemEffects.DoranDrainRegeneration : 0) : 0; } }
        public float BaseAttackSpeed { get { return Kind == UnitKind.Champion ? Mathf.Min(2.5f, BaseStats.AttackSpeedAt(Level, BonusAttackSpeed)) : 1 / attackInterval; } }
        public bool CanShop { get { return Kind == UnitKind.Champion; } }
        public bool CanBuy { get { return CanShop; } }
        public bool IsRecalling { get { return recallAt > 0; } }
        public float RecallRemaining { get { return Mathf.Max(0, recallAt - Time.time); } }
        public bool HealthPotionActive { get { return Time.time < potionUntil; } }
        public int Gold { get; private set; }
        public long TotalGoldEarned { get; private set; }
        public int LastHits { get; private set; }
        public int Kills { get; private set; }
        public int Deaths { get; private set; }
        public int Assists { get; private set; }
        public float RespawnAt { get; private set; }
        public float RespawnRemaining { get { return Alive ? 0 : Mathf.Max(0, RespawnAt - Time.time); } }
        public bool Alive { get { return Health > 0; } }
        public bool IsInStasis { get { return ItemEffects != null && ItemEffects.IsInStasis; } }
        public bool Targetable { get { return gameObject.activeInHierarchy && Alive && !IsInStasis && Time.time >= untargetableUntil && (ItemEffects == null || !ItemEffects.IsReviving); } }
        public bool IsStunned { get { return Time.time < stunnedUntil; } }
        public bool IsRooted { get { return Time.time < rootedUntil; } }
        public float RootRemaining { get { return Mathf.Max(0, rootedUntil - Time.time); } }
        public float StunRemaining { get { return Mathf.Max(0, stunnedUntil - Time.time); } }
        public float ActionLockRemaining { get { return Mathf.Max(0, busyUntil - Time.time); } }
        public bool CanAct { get { return CanActDuringDash && (Blade == null || !Blade.IsDashing); } }
        // CanAct without Irelia's Q dash: in LoL she can cast her other abilities during it.
        public bool CanActDuringDash { get { return gameObject.activeInHierarchy && Alive && !IsInStasis && (ItemEffects == null || !ItemEffects.IsReviving) && !world.RoundOver && !world.Paused && !IsStunned && Time.time >= busyUntil && (Blade == null || !Blade.IsCharging); } }
        public bool Marked { get { return Time.time < markUntil || Dummy != null && Dummy.PracticeMark; } }
        public bool AttackReady { get { return Time.time >= nextAttack && windingTarget == null; } }
        public float AttackWindupDuration { get { return Sett != null ? Sett.AttackWindupDuration : Kind == UnitKind.Champion ?
            (Blade != null ? Blade.AttackInterval : 1 / BaseAttackSpeed) * BaseStats.AttackWindupFraction : MinionStats.Windup; } }
        public bool IsWindingAttack { get { return windingTarget != null; } }
        public int BasicAttackSequence { get; private set; }
        public int BasicAttackVariant { get; private set; } = 1;
        public IreliaAudio Audio { get; private set; }
        public Combatant PendingAttackTarget { get { return windingTarget; } }
        public float AttackWindupRemaining { get { return windingTarget == null ? 0 : Mathf.Max(0, windupEnds - Time.time); } }
        public float AttackCooldownRemaining { get { return Mathf.Max(0, nextAttack - Time.time); } }
        public float Radius { get { return TowerToTowerRules.Radius(Kind, MinionType); } }
        public float SelectionHeight { get { return TowerToTowerRules.SelectionHeight(Kind, MinionType); } }
        // Mouse selection and HUD anchors follow presentation; gameplay always uses Radius.
        public float VisualScale { get { return Dummy != null ? 1 : Kind == UnitKind.Champion ? world.Presentation.visual_mesh_multiplier : BoardingPartyResistance > 0 ? 1.1f : 1; } }
        public float VisualHeight
        {
            get
            {
                if (Dummy != null) return Dummy.VisualHeight;
                if (Kind == UnitKind.Tower) return world.Presentation.TowerVisualHeight;
                if (Kind == UnitKind.Minion) return world.Presentation.MinionVisualHeight(MinionType) * VisualScale;
                return SelectionHeight * VisualScale;
            }
        }
        public Quaternion VisualTiltRotation { get { return Kind == UnitKind.Tower ? Quaternion.identity : CameraFacingVisualTilt.WorldRotation(world.View, world.Presentation.model_tilt_degrees); } }
        public Vector3 VisualTop { get { return Position + VisualTiltRotation * (Vector3.up * VisualHeight); } }
        public float TowerAggroRadius { get { return TowerToTowerRules.TowerAggroRadius; } }
        public float AttackReach(Combatant target)
        { return Kind == UnitKind.Tower ? TowerAggroRadius : AttackRange + Radius + (target != null ? target.Radius : 0); }
        public Vector3 Velocity { get; private set; }
        public bool HasWalkingOrder { get { return moving && CanAct && !IsRooted && !IsWindingAttack && (Blade==null || !Blade.IsDashing); } }
        public Vector3 WalkingDirection { get { return HasWalkingOrder ? (destination-Position).normalized : Vector3.zero; } }
        public Combatant AttackTarget { get; private set; }
        public readonly AbilityClock Cooldowns = new AbilityClock();
        public Vector3 Position { get { return transform.position; } }
        public Color Color { get { return Team == Team.Blue ? new Color(0.2f, 0.8f, 1f) : new Color(1f, 0.35f, 0.4f); } }

        LaneWorld world;
        Vector3 destination;
        float windingReach, nextAttack, windupEnds, busyUntil, stunnedUntil, slowUntil, slowFactor = 1;
        float shieldUntil, untargetableUntil, markUntil, attackInterval, rootedUntil;
        float passiveGold, recallAt, spellbladeUntil, spellbladeReadyAt, itemSpeedUntil, potionUntil, potionRate;
        int normalplayGoldAwarded = NormalplayRules.StartingGold, minionScalingLevel;
        public int NormalplayPendingLevelGold { get { return Level >= 18 ? 0 : System.Math.Max(0, NormalplayRules.GoldAtLevel(Level + 1) - normalplayGoldAwarded); } }
        float itemSpeedBonus;
        float grievousUntil;
        readonly Dictionary<Combatant, float> championDamageAt = new Dictionary<Combatant, float>();
        readonly Dictionary<Combatant, int> borkHits = new Dictionary<Combatant, int>();
        float borkReadyAt, borkLastHit;
        int refillCharges = 2;
        Combatant windingTarget;
        bool moving;
        bool nextBasicAttack2;
        float pvpIdleSince;
        Vector3 pvpIdlePosition;
        bool pvpIdleTracked, pvpIdleReset;
        Transform model;
        IreliaModelAnimation modelAnimation;
        SettModelAnimation settAnimation;
        MelModelAnimation melAnimation;
        MinionModelAnimation minionAnimation;
        public Bounds SelectionBounds
        {
            get
            {
                float visualScale = VisualHeight / SelectionHeight;
                var bounds = new Bounds(Position + Vector3.up * (VisualHeight / 2),
                    new Vector3(Radius * 2 * visualScale, VisualHeight, Radius * 2 * visualScale));
                return CameraFacingVisualTilt.RotateBounds(bounds, Position, VisualTiltRotation);
            }
        }

        public void Initialize(LaneWorld owner, Team team, UnitKind kind, Archetype archetype, string label, MinionType minionType = MinionType.Melee)
        {
            world = owner; Team = team; Kind = kind; Archetype = archetype; DisplayName = label;
            MaxHealth = kind == UnitKind.Tower ? 9000 : 0;
            Armor = MagicResist = kind == UnitKind.Tower ? 60 : 0;
            AttackDamage = kind == UnitKind.Tower ? 182 : 0;
            MoveSpeed = 0;
            AttackRange = kind == UnitKind.Tower ? TowerToTowerRules.TowerAttackRange : 0;
            attackInterval = kind == UnitKind.Tower ? 1 / 0.833f : 1;
            Health = MaxHealth; MaxResource = 0; Resource = 0; destination = Position;
            if (kind == UnitKind.Minion)
            {
                MinionType = minionType; MinionStats = MidLane.MinionStats.For(minionType);
                MaxHealth = Health = MinionStats.Health; AttackDamage = MinionStats.Damage;
                MoveSpeed = 3.5f; AttackRange = MinionStats.Range; attackInterval = 1 / MinionStats.AttackSpeed;
                if (world.NormalplayModeActive && world.Player != null) ScaleNormalplayMinion(world.Player.Level);
            }
            if (kind == UnitKind.Champion)
            {
                BaseStats = LeagueChampionStats.For(archetype);
                Inventory = new ItemInventory(LeagueItemCatalog.Items);
                Runes = new RuneEffects(world,this);
                InitializeProgress(world.NormalplayModeActive ? NormalplayRules.StartingLevel : SwiftplayRules.StartingLevel);
                Gold = world.PvpModeActive ? PvpProgressionRules.StartingGold : world.NormalplayModeActive ? NormalplayRules.StartingGold : SwiftplayRules.StartingGold; TotalGoldEarned = Gold;
                ItemEffects = new CombatItemEffects(world, this);
                Summoners = gameObject.AddComponent<SummonerSpells>(); Summoners.Initialize(world, this);
                gameObject.AddComponent<IreliaMarkVisual>().Initialize(world, this);
                if (archetype != Archetype.Blade) ChampionSkillPlan.SpendAvailable(Progress, archetype);
            }
            model = transform.Find("Model");
            if (kind == UnitKind.Champion && archetype == Archetype.Blade)
            {
                Blade = gameObject.AddComponent<BladeDuelist>(); Blade.Initialize(world, this);
                Audio = gameObject.AddComponent<IreliaAudio>(); Audio.Initialize(world, this);
                gameObject.AddComponent<IreliaVfx>().Initialize(world, this);
            }
            if (kind == UnitKind.Champion && archetype == Archetype.Prism)
            { Mel = gameObject.AddComponent<MelAbilities>(); Mel.Initialize(world, this); }
            if (model != null)
            {
                if (Kind == UnitKind.Minion && MinionType == MinionType.Cannon) model.localScale = Vector3.one * VisualScale;
                modelAnimation = model.GetComponent<IreliaModelAnimation>(); if (modelAnimation != null) modelAnimation.Bind(world, this);
                settAnimation = model.GetComponent<SettModelAnimation>(); if (settAnimation != null) settAnimation.Bind(world, this);
                minionAnimation = model.GetComponent<MinionModelAnimation>(); if (minionAnimation != null) minionAnimation.Bind(world, this);
                melAnimation = model.GetComponent<MelModelAnimation>(); if (melAnimation != null) melAnimation.Bind(world, this);
                if (Kind == UnitKind.Champion && Archetype == Archetype.Brawler) { Sett = gameObject.AddComponent<SettAbilities>(); Sett.Initialize(world, this); }
            }
        }

        void Update()
        {
            if (world.Paused) { Velocity = Vector3.zero; return; }
            if (ItemEffects != null) ItemEffects.Tick(Time.deltaTime);
            if (Runes != null) Runes.Tick(Time.deltaTime);
            if (ItemEffects != null && ItemEffects.IsReviving) { Velocity = Vector3.zero; return; }
            if (Kind == UnitKind.Champion && Dummy == null && !world.PvpModeActive && !world.NormalplayModeActive && world.ProgressionEnabled && world.MatchTime >= 65)
            { passiveGold += Time.deltaTime * 2.04f; int amount = Mathf.FloorToInt(passiveGold); GrantGold(amount); passiveGold -= amount; }
            if (!Alive)
            {
                Velocity = Vector3.zero;
                if (Kind == UnitKind.Champion && Time.time >= RespawnAt) Respawn();
                return;
            }
            if (world.RoundOver) { Velocity = Vector3.zero; return; }
            if (IsRecalling && Time.time >= recallAt)
            { recallAt = 0; transform.position = world.FountainPoint(Team); destination = Position; busyUntil = 0; world.Navigation.Forget(this); if (this == world.Player) world.View.GetComponent<IsometricCamera>().OnPlayerRespawn(); }
            float dt = Time.deltaTime;
            Vector3 before = Position;
            ResolveAttackWindup();
            if (Blade != null) Blade.Tick();
            Resource = Mathf.Min(MaxResource, Resource + dt * ManaRegeneration);
            Heal(dt * HealthRegeneration);
            if (Time.time < potionUntil) Heal(potionRate * dt);
            if (Time.time >= shieldUntil) Shield = 0;

            Vector3 yieldGoal=Position;
            bool yielding=Kind==UnitKind.Minion && CanAct && !IsRooted && !IsWindingAttack && world.Spacing.TryYieldGoal(world,this,out yieldGoal);
            if(yielding)
                transform.position=world.Navigation.Step(world,this,yieldGoal,MoveSpeed*dt);
            else if (CanAct)
            {
                if (AttackTarget != null && !ValidEnemy(AttackTarget)) { AttackTarget = null; moving = false; }
                if (AttackTarget != null)
                {
                    float distance = Vector3.Distance(Position, AttackTarget.Position);
                    if (distance <= AttackReach(AttackTarget))
                    {
                        moving = false;
                        TryAttack(AttackTarget);
                    }
                    else { destination = AttackTarget.Position; moving = Kind != UnitKind.Tower; }
                }
                if (moving && windingTarget == null && !IsRooted)
                {
                    float speed = (MoveSpeed + (Sett != null ? Sett.QMoveSpeedBonus : 0) + (Mel != null ? Mel.MoveSpeedBonus : 0) + (Time.time < itemSpeedUntil ? itemSpeedBonus : 0) + (ItemEffects != null ? ItemEffects.MoveSpeedBonus : 0)) * (Time.time < slowUntil ? slowFactor : 1);
                    transform.position = world.Navigation.Step(world, this, destination, speed * dt);
                    if (Vector3.Distance(Position, destination) < 0.05f) moving = false;
                }
            }
            Velocity = dt > 0 ? (Position - before) / dt : Vector3.zero;
            if (model != null)
            {
                if (Velocity.sqrMagnitude > 0.1f)
                    model.rotation = Kind == UnitKind.Minion ? Quaternion.RotateTowards(model.rotation, Quaternion.LookRotation(Velocity), 540 * dt) : Quaternion.LookRotation(Velocity);
                if (Kind == UnitKind.Minion && MinionType == MinionType.Cannon) model.localScale = Vector3.one * VisualScale;
                else if ((modelAnimation != null || settAnimation != null || melAnimation != null || minionAnimation != null) && IsWindingAttack && AttackTarget != null && (AttackTarget.Position - Position).sqrMagnitude > 0.01f)
                    model.rotation = Quaternion.LookRotation(AttackTarget.Position - Position);
                // Placeholder animation, isolated from hitboxes and ground position.
                model.localPosition = Dummy != null || modelAnimation != null || settAnimation != null || melAnimation != null || minionAnimation != null ? Vector3.zero : Vector3.up * (Mathf.Sin(Time.time * 12) * Mathf.Min(Velocity.magnitude, 1) * 0.045f);
            }
        }

        public bool ValidEnemy(Combatant other) { return gameObject.activeInHierarchy && other != null && other.Team != Team && other.Targetable; }
        internal void RemoveMinionFromPractice()
        {
            if (Kind != UnitKind.Minion) return;
            Health = 0; Stop(); gameObject.SetActive(false);
        }
        internal void MoveForAbility(Vector3 point)
        {
            if (!Alive || IsInStasis || Kind == UnitKind.Tower) return;
            CancelRecall(); destination = world.Clamp(point, Radius); moving = true; AttackTarget = null; CancelAttackWindup();
        }
        public void MoveTo(Vector3 point)
        {
            if (!Alive || IsInStasis || Kind == UnitKind.Tower) return;
            CancelRecall(); destination = world.Clamp(point, Radius); moving = true; AttackTarget = null; CancelAttackWindup();
            if (Blade != null) Blade.MovementOrdered();
        }
        public void Stop() { if (Blade != null) Blade.CancelInput(); CancelRecall(); moving = false; AttackTarget = null; CancelAttackWindup(); }
        public void CancelAttackWindup()
        {
            // Input can arrive before this actor's Update on the impact frame.
            // A completed preparation lands first; an unfinished one is still cancelled.
            ResolveAttackWindup();
            if (windingTarget != null) nextAttack = Time.time;
            windingTarget = null;
        }
        public void ResolveAttackWindup()
        {
            if (world.Paused || windingTarget == null || Time.time < windupEnds) return;
            Combatant target = windingTarget; windingTarget = null;
            if (!CanAct || !ValidEnemy(target) || Vector3.Distance(Position, target.Position) > windingReach + (Kind == UnitKind.Tower ? 0 : 0.4f))
            { nextAttack = Time.time; return; }
            bool critical = false;
            if (Kind == UnitKind.Minion && MinionType != MinionType.Melee)
                world.CreateBasicProjectile(this, target, AttackDamage);
            else if (Mel != null && Dummy == null)
            {
                MelBolt.Create(world, this, target, AttackDamage * (Random.value < CritChance ? 2 : 1), DamageType.Physical, -1, SpellCastId, 0, "AA", true);
                Mel.OnAttackHit(target);
            }
            else
            {
                if (Blade == null) world.FlashLine(Position + Vector3.up, target.Position + Vector3.up, Color);
                critical = Kind == UnitKind.Champion && Random.value < CritChance;
                float raw = AttackDamage * (critical ? 2 : 1);
                if (Kind == UnitKind.Champion) ApplyAttackHit(target, raw); else target.TakeDamage(BasicDamageAgainst(target, raw), this);
                if (Audio != null) Audio.BasicHit(BasicAttackVariant, critical);
            }
            if (Blade != null) Blade.OnBasicAttackHit(target, critical);
        }
        internal void OrderAttackAfterAbility(Combatant other) { if (ValidEnemy(other)) { CancelRecall(); AttackTarget = other; moving = false; } }
        internal void HoldAttackPosition() { AttackTarget = null; moving = false; }
        public void OrderAttack(Combatant other) { if (!IsInStasis && ValidEnemy(other)) { if (Blade != null) Blade.CancelInput(); CancelRecall(); AttackTarget = other; moving = false; } }
        public bool TryAttack(Combatant target)
        {
            if (!CanAct || !AttackReady || !ValidEnemy(target) || Vector3.Distance(Position, target.Position) > AttackReach(target))
                return false;
            float interval = Blade != null ? Blade.AttackInterval : Kind == UnitKind.Champion ? 1 / BaseAttackSpeed : attackInterval;
            windingReach = AttackReach(target);
            windingTarget = target; windupEnds = Time.time + (Kind == UnitKind.Champion ? AttackWindupDuration : Kind == UnitKind.Minion ? MinionStats.Windup : 0.22f);
            BasicAttackSequence++;
            if (Blade != null)
            {
                BasicAttackVariant = nextBasicAttack2 ? 2 : 1; nextBasicAttack2 = !nextBasicAttack2;
                if (Audio != null) Audio.BasicCast(BasicAttackVariant);
            }
            nextAttack = Time.time + (Sett != null ? Sett.AttackRecovery : interval);
            return true;
        }

        public float TakeDamage(float raw, Combatant source, DamageType type = DamageType.Physical, bool basicAttack = false, bool abilityDamage = false, bool persistentDamage = false, int abilityCastId = 0, string hitLabel = null, int abilitySlot = -1, bool runeDamage = false)
        {
            // Permanent mid-lane practice: towers stay targetable and keep attacking.
            if (!gameObject.activeInHierarchy || Kind == UnitKind.Tower || !Alive || IsInStasis || (!Targetable && (!persistentDamage || (ItemEffects != null && ItemEffects.IsReviving))) || source == null || !source.gameObject.activeInHierarchy || source.Team == Team) return 0;
            if (this == world.Player && world.Settings.testMode) return 0;
            if (abilityDamage && source.ItemEffects != null) raw = source.ItemEffects.ModifyAbilityDamage(raw, abilityCastId == 0 ? source.SpellCastId : abilityCastId);
            if (raw > 0) CancelRecall();
            float amount = PreviewIncomingDamage(raw, source, type, basicAttack);
            if(Runes!=null) amount=Runes.ReduceIncoming(amount,source,basicAttack,abilityDamage||hitLabel=="Q",abilityCastId==0?source.SpellCastId:abilityCastId);
            float runeHitDamage=amount;
            bool positiveHit = amount > 0;
            if (positiveHit && Audio != null && Blade.DamageReduction(type) > 0) Audio.WBlock();
            int targetLife = Deaths;
            // A hit absorbed by a shield still counts for assists, kill credit and turret aggro.
            if (amount > 0 && source.Kind == UnitKind.Champion) championDamageAt[source] = Time.time;
            world.DamageApplied(source, this, amount, abilityDamage && !persistentDamage ? abilitySlot : -1);
            if (amount > 0 && source.ItemEffects != null) source.ItemEffects.ApplyShieldReaver(this);
            if (Summoners != null) amount = Summoners.AbsorbBarrier(amount);
            float absorbed = Mathf.Min(Shield, amount); Shield -= absorbed; amount -= absorbed;
            if (ItemEffects != null) amount = ItemEffects.ReceiveDamage(amount, type, source);
            float effectiveDamage = Mathf.Min(amount, Health);
            Health = Dummy != null ? MaxHealth : Mathf.Max(0, Health - amount);
            if (positiveHit && ItemEffects != null) ItemEffects.OnIncomingHit(source, type);
            if (Dummy != null) Dummy.RecordDamage(amount, hitLabel ?? (abilityDamage ? "Habilidad" : basicAttack ? "AA" : "Impacto"), type);
            world.PracticeDamageDealt(source, amount);
            if (source.ItemEffects != null)
            {
                bool area = abilityDamage && abilitySlot >= 0 && !(source.Archetype == Archetype.Siphon && abilitySlot == 0);
                source.ItemEffects.OnDamageDealt(this, effectiveDamage, persistentDamage || area, hitLabel != "Ignite");
                if (abilityDamage && raw > 0) source.ItemEffects.OnAbilityHit(this);
            }
            if (Sett != null) Sett.ReceiveDamage(amount);
            if(Runes!=null) Runes.OnReceivedDamage(source,effectiveDamage);
            if(source.Runes!=null) source.Runes.OnOutgoingHit(this,Mathf.Min(runeHitDamage,effectiveDamage+Mathf.Max(0,runeHitDamage-amount)),basicAttack,
                abilityDamage||hitLabel=="Q",abilityCastId==0?source.SpellCastId:abilityCastId,runeDamage);
            if (Health <= 0)
            {
                if (ItemEffects != null && ItemEffects.TryRebirth()) { Health = 1; Stop(); if (Blade != null) Blade.CancelTransient(); return amount; }
                Stop();
                Combatant credited = source;
                if (Kind == UnitKind.Champion && source.Kind != UnitKind.Champion)
                {
                    float newest = Time.time - 15;
                    foreach (var hit in championDamageAt) if (hit.Key != null && hit.Value >= newest) { credited = hit.Key; newest = hit.Value; }
                }
                credited.Reward(this);
                if (ItemEffects != null) ItemEffects.OnDeath();
                if (Runes != null) Runes.OnDeath();
                if (Blade != null) Blade.CancelTransient();
                var hitbox = GetComponent<Collider>(); if (hitbox != null) hitbox.enabled = false;
                if (model != null) model.localRotation = Quaternion.Euler(0, 0, 90);
                world.UnitDied(this, credited);
            }
            if (positiveHit && !persistentDamage && source.ItemEffects != null)
                source.ItemEffects.OnEclipseHit(this, basicAttack, abilityDamage || hitLabel == "Q",
                    abilityCastId == 0 ? source.SpellCastId : abilityCastId, targetLife);
            if (positiveHit && Alive && source.Mel != null && (basicAttack || abilityDamage && abilitySlot >= 0)) source.Mel.RecordHit(this);
            return amount;
        }
        internal float PreviewIncomingDamage(float raw, Combatant source, DamageType type, bool basicAttack = false)
        {
            if (source.Summoners != null) raw *= source.Summoners.OutgoingDamageMultiplier(type);
            if (source.ItemEffects != null) raw = source.ItemEffects.ModifyOutgoingDamage(raw, this, type);
            if (source.Runes != null) raw *= source.Runes.DamageMultiplier(this,type);
            float reduced = Blade != null ? raw * (1 - Blade.DamageReduction(type)) : raw;
            if (Inventory != null && Inventory.Has(2051) && source.Kind == UnitKind.Champion) reduced = Mathf.Max(0, reduced - 15);
            if (Inventory != null && (Inventory.Has(3047) || Inventory.Has(3174)) && basicAttack) reduced *= .9f;
            float defense = type == DamageType.Magic ? MagicResist : Armor;
            if (defense > 0 && source.Kind == UnitKind.Champion)
                defense = Mathf.Max(0, defense * (1 - (type == DamageType.Magic ? source.PercentMagicPenetration : source.PercentArmorPenetration)) - (type == DamageType.Magic ? source.MagicPenetration : source.ArmorPenetration));
            return type == DamageType.True ? Mathf.Max(0, reduced) : CombatRules.Mitigate(reduced, defense);
        }
        public bool WasDamagedBy(Combatant actor, float seconds) { float at; return championDamageAt.TryGetValue(actor, out at) && Time.time - at <= seconds; }
        public void GrantAssist() { Assists++; }
        public void ApplyGrievousWounds(float seconds) { grievousUntil = Mathf.Max(grievousUntil, Time.time + seconds); }
        public void CompleteGuardianRebirth() { Health = Mathf.Min(MaxHealth, BaseStats.Health.At(Level) * 0.5f); Resource = MaxResource; ResetAttackTimer(); }
        public float DealAbilityDamage(Combatant target, float raw, DamageType type, int castId = 0, int slot = -1) { return target.TakeDamage(raw, this, type, false, true, false, castId, slot >= 0 ? "QWER"[slot].ToString() : "Pasiva", slot); }
        void Reward(Combatant victim)
        {
            if (Kind != UnitKind.Champion) return;
            // PvP income comes exclusively from shared level progression.
            if (!world.PvpModeActive && !world.NormalplayModeActive)
                GrantGold(victim.Kind == UnitKind.Minion ? victim.MinionStats.Gold : victim.Kind == UnitKind.Champion ? 300 : 150);
            if (victim.Kind == UnitKind.Minion) LastHits++;
            if (victim.Kind == UnitKind.Champion) { Kills++; if (Audio != null) Audio.ChampionKilled(); }
        }
        public void ResetAbilityCooldowns()
        {
            for (int slot = 0; slot < 4; slot++) Cooldowns.Reset(slot);
            if (Summoners != null) Summoners.ResetCooldowns();
        }
        public void ResetCombatCooldowns()
        {
            ResetAbilityCooldowns();
            spellbladeReadyAt = borkReadyAt = 0; borkHits.Clear(); borkLastHit = 0;
            if (ItemEffects != null) ItemEffects.ResetCooldowns();
        }
        internal void RecordPvpActivity()
        {
            if (Kind != UnitKind.Champion || !world.PvpModeActive) return;
            pvpIdleSince = Time.time; pvpIdlePosition = Position; pvpIdleTracked = true; pvpIdleReset = false;
        }
        internal void TickPvpIdleReset()
        {
            // The player shares Practice's spell/damage timer instead of the movement-based bot timer.
            if (this == world.Player) { pvpIdleTracked = false; return; }
            if (!world.PvpModeActive || !Alive || (ItemEffects != null && ItemEffects.IsReviving))
            { pvpIdleTracked = false; return; }
            if (!pvpIdleTracked || (Position - pvpIdlePosition).sqrMagnitude > .000001f)
            { RecordPvpActivity(); return; }
            if (!pvpIdleReset && Time.time - pvpIdleSince >= 6)
            { ResetAbilityCooldowns(); pvpIdleReset = true; }
        }
        public void ScheduleRespawn(float duration)
        {
            if (Audio != null) Audio.Died();
            if (Sett != null) Sett.Cancel();
            if (Mel != null) Mel.ClearTemporaryEffects();
            Deaths++; RespawnAt = Time.time + duration;
            if (Summoners != null) Summoners.ClearTemporaryEffects();
            Shield = 0; markUntil = stunnedUntil = rootedUntil = slowUntil = untargetableUntil = busyUntil = recallAt = potionUntil = itemSpeedUntil = spellbladeUntil = 0;
            if (Blade != null) Blade.ResetPassive();
        }
        public void Respawn()
        {
            if (Kind != UnitKind.Champion || Alive) return;
            nextBasicAttack2 = false; BasicAttackVariant = 1;
            Health = MaxHealth; Resource = MaxResource; RespawnAt = 0;
            championDamageAt.Clear(); grievousUntil = 0;
            transform.position = world.PvpModeActive && this == world.Opponent && Sett != null ? Vector3.zero : world.FountainPoint(Team);
            destination = Position; Stop(); ResetAttackTimer();
            var hitbox = GetComponent<Collider>(); if (hitbox != null) hitbox.enabled = true;
            if (model != null) { model.localPosition = Vector3.zero; model.rotation = Quaternion.LookRotation(Team == Team.Blue ? Vector3.right : Vector3.left); }
            if (this == world.Player) world.View.GetComponent<IsometricCamera>().OnPlayerRespawn();
            if (Audio != null && this == world.Player) Audio.Respawned();
        }
        public void Heal(float amount) { if (Alive) Health = Mathf.Min(MaxHealth, Health + Mathf.Max(0, amount) * (Time.time < grievousUntil ? 0.6f : 1)); }
        public void HealFromLifesteal(float amount)
        {
            if (!Alive) return;
            float effective = Mathf.Max(0, amount) * (Time.time < grievousUntil ? .6f : 1);
            float before = Health; Heal(amount);
            if (ItemEffects != null) ItemEffects.OnLifestealOverflow(Mathf.Max(0, effective - (Health - before)));
        }
        public void Stun(float seconds)
        {
            if (!Targetable) return;
            CancelRecall(); stunnedUntil = Mathf.Max(stunnedUntil, Time.time + Mathf.Max(0.3f, seconds * (1 - Tenacity)));
            if(Runes!=null) Runes.NotifyCrowdControl(stunnedUntil-Time.time);
            CancelAttackWindup();
            if (Blade != null) Blade.InterruptDash();
        }
        public void Slow(float factor, float seconds)
        {
            slowFactor = Time.time < slowUntil ? Mathf.Min(slowFactor, Mathf.Clamp01(factor)) : Mathf.Clamp01(factor);
            slowUntil = Mathf.Max(slowUntil, Time.time + Mathf.Max(0.3f, seconds * (1 - Tenacity)));
            if(Runes!=null) Runes.NotifyCrowdControl(slowUntil-Time.time);
        }
        public void Root(float seconds)
        {
            if (!Targetable) return;
            CancelRecall(); rootedUntil = Mathf.Max(rootedUntil, Time.time + Mathf.Max(.3f, seconds * (1 - Tenacity)));
            if(Runes!=null) Runes.NotifyCrowdControl(rootedUntil-Time.time);
            if (Blade != null) Blade.InterruptDash();
        }
        public void Mark(float seconds) { markUntil = Time.time + seconds; }
        public void ClearMark() { markUntil = 0; if (Dummy != null) Dummy.ConsumePracticeMark(); }
        public bool SpendResource(float amount)
        {
            if (UnlimitedMana) return true;
            if (Resource < amount) return false;
            Resource -= amount; return true;
        }
        internal void RestoreResource(float amount) { Resource = Mathf.Min(MaxResource, Resource + Mathf.Max(0, amount)); }
        internal void SetAbilityShield(float amount, float duration) { Shield = amount * (ItemEffects != null ? ItemEffects.ShieldReceivedMultiplier : 1); shieldUntil = Time.time + duration; if(Runes!=null) Runes.NotifyShield(Shield,duration); }
        internal void ScaleAbilityShield(float factor) { Shield *= factor; if (Sett != null) Sett.ScaleShieldDecay(factor); }
        internal void ReduceAbilityShield(float amount) { Shield = Mathf.Max(0, Shield - amount); }
        internal void Suppress(float seconds) { Stop(); stunnedUntil = Mathf.Max(stunnedUntil, Time.time + seconds); if(Runes!=null) Runes.NotifyCrowdControl(seconds); }
        public void LockActions(float seconds) { busyUntil = Mathf.Max(busyUntil, Time.time + seconds); }
        public void ResetAttackTimer() { ResolveAttackWindup(); nextAttack = Time.time; windingTarget = null; }
        public void InitializeProgress(int level)
        {
            Progress = world.PracticeModeActive && Archetype == Archetype.Blade ? ChampionProgress.ForPvp(6, Archetype) : world.PvpModeActive ? ChampionProgress.ForPvp(world.PvpLevel, Archetype) : new ChampionProgress(level);
            if (Archetype == Archetype.Blade) Progress.UseAutomaticIreliaSkills();
            ApplyChampionLevel(true);
        }
        internal void ResetPvpEconomy()
        {
            Gold = PvpProgressionRules.StartingGold; TotalGoldEarned = Gold; passiveGold = 0;
            potionUntil = potionRate = 0; refillCharges = 2;
            Inventory.Items.Clear(); ItemEffects = new CombatItemEffects(world, this);
            Runes=new RuneEffects(world,this);
        }
        internal void EquipPvpStarter(int id)
        {
            // Loadout initialization also runs when the mode is selected while paused.
            int gold = Gold; string reason;
            if (!Inventory.Buy(id, ref gold, out reason)) { Debug.LogError("No se pudo equipar el starter PvP: " + reason); return; }
            Gold = gold; ItemEffects.OnInventoryChanged(); ApplyChampionLevel();
        }
        internal void SetPracticeGold() { Gold = 50000; TotalGoldEarned = 50000; }
        internal void ConfigureTrainingDummy(TrainingDummy dummy, float movementSpeed = 3.35f)
        {
            Dummy = dummy; MaxHealth = Health = 1000; Armor = MagicResist = AttackDamage = AttackRange = 0;
            Runes=null;
            MoveSpeed = movementSpeed; MaxResource = Resource = 0;
        }
        internal void ApplyPvpLevel(int level)
        {
            bool leveled = level > Level;
            Progress = ChampionProgress.ForPvp(level, Archetype); ApplyChampionLevel();
            if(Runes!=null&&Runes.SkillElixirConsumed) Progress.GrantBonusSkillPoint(Archetype);
            if (!leveled) return;
            world.ChampionLeveled(this);
            if (ItemEffects != null) ItemEffects.OnLevelUp();
        }
        internal void RestoreAfterChampionKill()
        { if (Alive) { Health = MaxHealth; Resource = MaxResource; ResetCombatCooldowns(); } }
        internal void GrantTrainingLevel()
        {
            if (!world.ProgressionEnabled || Progress == null || Level >= 18) return;
            if (Progress.PvpSkills) { ApplyPvpLevel(Level + 1); return; }
            Progress.AddExperience(Progress.NextLevelExperience - Progress.Experience);
            ApplyChampionLevel(); world.ChampionLeveled(this);
            if (ItemEffects != null) ItemEffects.OnLevelUp();
            if (Archetype != Archetype.Blade) ChampionSkillPlan.SpendAvailable(Progress, Archetype);
            if (world.NormalplayModeActive && this == world.Player) world.SynchronizeNormalplayProgress();
        }
        internal void GrantNormalplayProgressGold()
        {
            if (!world.NormalplayModeActive || !world.ProgressionEnabled || world.Paused) return;
            int target = NormalplayRules.GoldAtProgress(Level, Progress.Experience);
            int gold = System.Math.Max(0, target - normalplayGoldAwarded);
            normalplayGoldAwarded += gold; GrantGold(gold);
        }
        internal void MatchNormalplayProgress(Combatant leader)
        {
            if (Progress == null || leader == null || Progress.MatchExperience(leader.Progress) <= 0) return;
            ApplyChampionLevel(); world.ChampionLeveled(this);
            if (ItemEffects != null) ItemEffects.OnLevelUp();
            ChampionSkillPlan.SpendAvailable(Progress, Archetype);
        }
        internal void ScaleNormalplayMinion(int level)
        {
            if (!world.NormalplayModeActive || Kind != UnitKind.Minion || minionScalingLevel == level) return;
            float fraction = MaxHealth > 0 ? Health / MaxHealth : 1;
            MinionStats = NormalplayRules.Minion(MinionType, level);
            MaxHealth = MinionStats.Health; Health = MaxHealth * fraction; AttackDamage = MinionStats.Damage;
            minionScalingLevel = level;
        }
        public void GrantGold(int amount)
        {
            if (amount > 0 && !world.ProgressionEnabled) return;
            Gold = (int)System.Math.Max(0, System.Math.Min(100000, (long)Gold + amount));
            TotalGoldEarned += System.Math.Max(0, amount);
        }
        public void CatchUpProgress(Combatant leader)
        {
            if (!world.ProgressionEnabled || world.PvpModeActive || world.NormalplayModeActive || world.Paused || Kind != UnitKind.Champion || Progress == null || leader == null || leader.Progress == null) return;
            // Only raise progression. Preserve learned ranks, cooldowns, inventory and death timer.
            float experience = leader.Progress.TotalExperience - Progress.TotalExperience;
            if (Progress.AddExperience(experience) > 0)
            {
                ApplyChampionLevel();
                world.ChampionLeveled(this);
                if (ItemEffects != null) ItemEffects.OnLevelUp();
                if (Archetype != Archetype.Blade) ChampionSkillPlan.SpendAvailable(Progress, Archetype);
            }
            long missingGold = leader.TotalGoldEarned - TotalGoldEarned;
            if (missingGold > 0) GrantGold((int)System.Math.Min(int.MaxValue, missingGold));
        }
        public bool BuyItem(int id, out string reason)
        {
            reason = "Compra no disponible para esta unidad o mientras la partida está en pausa.";
            if (!CanBuy || world.Paused) return false;
            ItemDefinition item = Inventory.Get(id);
            if (item != null && !item.effectsImplemented) { reason = "Este objeto aÃºn no tiene todos sus efectos implementados."; return false; }
            int gold = Gold; if (!Inventory.Buy(id, ref gold, out reason)) return false;
            Gold = gold; ItemEffects.OnInventoryChanged(); ApplyChampionLevel();
            if (Audio != null && this == world.Player) Audio.ItemBought(id);
            return true;
        }
        public bool SellItem(int slot)
        {
            if (!CanShop || world.Paused) return false;
            return SellInventoryItem(slot);
        }
        internal bool SellBotStarter()
        {
            if ((!world.PvpModeActive && (!world.NormalplayModeActive || !CanShop)) || this != world.Opponent || !Alive || world.Paused) return false;
            int slot = Inventory.Items.FindIndex(item => item.starter);
            return slot >= 0 && SellInventoryItem(slot);
        }
        bool SellInventoryItem(int slot)
        {
            int gold = Gold; if (!Inventory.Sell(slot, ref gold)) return false;
            Gold = gold; ItemEffects.OnInventoryChanged(); ApplyChampionLevel(); return true;
        }
        public const float RecallChannelDuration = 3;
        public void BeginRecall()
        {
            // Temporarily disabled: the lane no longer has a recovery destination.
        }
        public void CancelRecall() { if (recallAt <= 0) return; recallAt = 0; busyUntil = Time.time; }
        public void NotifySpellCast()
        {
            SpellCastId++;
            if (Mel != null) Mel.OnSpellCast();
            RecordPvpActivity();
            world.PracticeAbilityUsed(this);
            if (Inventory != null && (Inventory.Has(3057) || Inventory.Has(3078)) && Time.time >= spellbladeReadyAt) spellbladeUntil = Time.time + 10;
        }
        public float ApplyAttackHit(Combatant target, float raw, bool countsAsAttack = true)
        {
            if (!ValidEnemy(target)) return 0;
            if (!countsAsAttack) raw = ItemEffects.ModifyAbilityDamage(raw, SpellCastId);
            if (Sett != null && countsAsAttack) raw = Sett.EmpowerAttack(target, raw);
            float preHitHealth = target.Health;
            if (Time.time < spellbladeUntil)
            {
                raw += BaseStats.AttackDamage.At(Level) * (Inventory.Has(3078) ? 2 : 1);
                spellbladeUntil = 0; spellbladeReadyAt = Time.time + 1.5f;
            }
            if (target.Kind == UnitKind.Tower) raw += AbilityPower * 0.2f;
            if (target.Kind == UnitKind.Minion && Inventory != null && Inventory.Has(1056)) raw += Inventory.Get(1056).Value("BonusDamage", 5);
            float dealt = target.TakeDamage(raw, this, DamageType.Physical, countsAsAttack, hitLabel: countsAsAttack ? "AA" : "Q");
            if(target.Kind==UnitKind.Minion && Runes!=null && Runes.AvariceActive && target.Alive)
                dealt+=target.TakeDamage(5,this,DamageType.True,hitLabel:"Elixir of Avarice",runeDamage:true);
            if (Inventory != null && target.Kind != UnitKind.Tower)
            {
                if (Inventory.Has(1043)) dealt += target.TakeDamage(15, this);
                if (Inventory.Has(3153))
                {
                    ItemDefinition item = Inventory.Get(3153);
                    float bonus = preHitHealth * item.Value(AttackRange > 3 ? "RangedValue" : "MeleeValue");
                    if (target.Kind == UnitKind.Minion) bonus = Mathf.Clamp(bonus, 15, item.Value("MonsterDamageCap", 100));
                    dealt += target.TakeDamage(bonus, this);
                    if (target.Alive && target.Kind == UnitKind.Champion && Time.time >= borkReadyAt)
                    {
                        if (Time.time - borkLastHit > item.Value("AttackCounterDuration", 6)) borkHits.Clear();
                        int count; borkHits.TryGetValue(target, out count); borkHits[target] = count + 1; borkLastHit = Time.time;
                        if (count >= 2) { target.Slow(1 + item.Value("MoveSpeedMod", -0.3f), item.Value("MoveSpeedDuration", 1)); borkHits.Clear(); borkReadyAt = Time.time + item.Value("Cooldown", 15); }
                    }
                }
                if (Inventory.Has(3091)) dealt += target.TakeDamage(45, this, DamageType.Magic);
                dealt += ItemEffects.OnAttackHit(target);
            }
            else if (Inventory != null && target.Kind == UnitKind.Tower) dealt += ItemEffects.OnHullbreakerHit(target);
            if (Inventory != null && (Inventory.Has(3078) || Inventory.Has(3044)))
            {
                ItemDefinition rage = Inventory.Get(Inventory.Has(3078) ? 3078 : 3044);
                itemSpeedBonus = rage.Value("MoveSpeedBonus", 20) / 100;
                if (!Inventory.Has(3078) && AttackRange > 3) itemSpeedBonus *= rage.Value("RangedMod", 0.5f);
                itemSpeedUntil = Time.time + rage.Value("MoveSpeedDuration", 2);
            }
            if (target.Kind != UnitKind.Tower) HealFromLifesteal(dealt * LifeSteal);
            return dealt;
        }
        public bool UseItem(int slot)
        {
            if (!Alive || IsInStasis || world.Paused || slot < 0 || slot >= Inventory.Items.Count) return false;
            int id = Inventory.Items[slot].id;
            if(Runes!=null && (id==2150||id==2151||id==2152)) return Runes.UseElixir(id);
            if (ItemEffects != null && ItemEffects.UseActive(id)) return true;
            if (Time.time < potionUntil) return false;
            if (id == 2003) { Inventory.Items.RemoveAt(slot); potionRate = 120f / 15; potionUntil = Time.time + 15; return true; }
            if (id == 2031 && refillCharges > 0) { refillCharges--; potionRate = 100f / 12; potionUntil = Time.time + 12; return true; }
            return false;
        }
        public void GainExperience(float amount)
        {
            if (world.NormalplayModeActive && this != world.Player) return;
            if (world.ProgressionEnabled && Alive && Progress != null && Progress.AddExperience(amount) > 0)
            {
                ApplyChampionLevel();
                world.ChampionLeveled(this);
                if (ItemEffects != null) ItemEffects.OnLevelUp();
                if (Archetype != Archetype.Blade) ChampionSkillPlan.SpendAvailable(Progress, Archetype);
            }
            if (world.NormalplayModeActive && this == world.Player) world.SynchronizeNormalplayProgress();
        }
        public float BasicDamageAgainst(Combatant target, float raw)
        {
            if (Kind == UnitKind.Minion && target.Kind == UnitKind.Tower)
                return raw * (MinionType == MinionType.Cannon ? 0.84f : 0.6f);
            return raw;
        }
        void ApplyChampionLevel(bool initial = false)
        {
            float oldMax = MaxHealth, oldMana = MaxResource, oldHealth = Health;
            float hp = 0, mp = 0, ad = 0, ap = 0, armor = 0, mr = 0, ms = 0, msPercent = 0, hpRegPercent = 0, mpRegPercent = 0;
            abilityHaste = bonusAttackSpeed = LifeSteal = Omnivamp = CritChance = healthRegenBonus = manaRegenBonus = ArmorPenetration = MagicPenetration = PercentArmorPenetration = PercentMagicPenetration = 0;
            float tenacityMultiplier = 1;
            ChampionStatValues reference = ChampionStatReference.AtLevel(Archetype, Level);
            // Resolve the same per-level reference in every mode; retain intrinsic bases for item scaling.
            // These offsets are applied once before item modifiers, never copied from the current inventory.
            hp = reference.maxHealth - BaseStats.Health.At(Level);
            mp = reference.resourceMax - BaseStats.Mana.At(Level);
            ad = reference.attackDamage - BaseStats.AttackDamage.At(Level);
            armor = reference.armor - BaseStats.Armor.At(Level);
            mr = reference.magicResist - BaseStats.MagicResist.At(Level);
            ms = reference.moveSpeed - BaseStats.MovementSpeedUnits;
            ap = reference.abilityPower; abilityHaste = reference.abilityHaste;
            bonusAttackSpeed = (reference.attackSpeed - BaseStats.AttackSpeedAt(Level)) / BaseStats.AttackSpeedRatio;
            LifeSteal = reference.lifeSteal; Omnivamp = reference.omnivamp; CritChance = reference.critChance;
            healthRegenBonus = reference.healthRegenRate - BaseStats.HealthRegeneration.At(Level);
            manaRegenBonus = reference.resourceRegenRate - BaseStats.ManaRegeneration.At(Level);
            ArmorPenetration = reference.physicalLethality; MagicPenetration = reference.magicPenetrationFlat;
            PercentArmorPenetration = 1 - reference.armorPenetrationPercent;
            PercentMagicPenetration = 1 - reference.magicPenetrationPercent;
            tenacityMultiplier = 1 - Mathf.Clamp01(reference.tenacity / 100);
            if (Inventory != null) foreach (ItemDefinition item in Inventory.Items)
            {
                hp += item.health; mp += item.mana; ad += item.damage; ap += item.power; armor += item.armor; mr += item.resist;
                ms += item.movement; msPercent += item.movementPercent; abilityHaste += item.haste; bonusAttackSpeed += item.attackSpeed;
                LifeSteal += item.lifeSteal; CritChance += item.crit; tenacityMultiplier *= 1 - item.tenacity;
                Omnivamp += item.omnivamp;
                healthRegenBonus += item.hpRegen; manaRegenBonus += item.mpRegen; hpRegPercent += item.hpRegenPercent; mpRegPercent += item.mpRegenPercent;
                ArmorPenetration += item.penetration; MagicPenetration += item.magicPenetration;
                PercentArmorPenetration = 1 - (1 - percentArmorPenetration) * (1 - item.armorPenetrationPercent);
                PercentMagicPenetration = 1 - (1 - percentMagicPenetration) * (1 - item.magicPenetrationPercent);
            }
            if (Inventory != null && Inventory.Has(3112)) { if (BaseStats.UsesMana) manaRegenBonus += 2; else healthRegenBonus += 3; }
            if(Runes!=null) { hp+=Runes.BonusHealth; mp+=Runes.BonusMana; }
            if (Inventory != null && Inventory.Has(3083)) hp *= 1 + Inventory.Get(3083).Value("HPAmp");
            healthRegenBonus += BaseStats.HealthRegeneration.At(Level) * hpRegPercent;
            manaRegenBonus += BaseStats.ManaRegeneration.At(Level) * mpRegPercent;
            AbilityPower = ap + (Archetype == Archetype.Siphon ? hp / 40 : 0);
            if (Inventory != null && Inventory.Has(3089)) abilityPower *= 1.3f;
            if (Archetype == Archetype.Siphon) hp += ap * 1.6f;
            Tenacity = Mathf.Clamp01(1 - tenacityMultiplier); CritChance = Mathf.Clamp01(CritChance);
            MaxHealth = BaseStats.Health.At(Level) + hp;
            MaxResource = BaseStats.UsesMana ? BaseStats.Mana.At(Level) + mp : 0;
            Armor = BaseStats.Armor.At(Level) + armor;
            MagicResist = BaseStats.MagicResist.At(Level) + mr;
            AttackDamage = BaseStats.AttackDamage.At(Level) + ad;
            MoveSpeed = ItemInventory.MovementSpeed((BaseStats.MovementSpeedUnits + ms) * (1 + msPercent)) * LeagueChampionStats.WorldUnitsPerLeagueUnit;
            AttackRange = reference.attackRange * LeagueChampionStats.WorldUnitsPerLeagueUnit;
            Health = initial ? MaxHealth : oldHealth <= 0 ? 0 : Mathf.Clamp(Health + MaxHealth - oldMax, 1, MaxHealth);
            Resource = initial ? MaxResource : Mathf.Clamp(Resource + MaxResource - oldMana, 0, MaxResource);
        }

        public float AbilityCost(int slot) { return Blade != null ? Blade.AbilityCost(slot) : RivalAbilities.Cost(Archetype, slot, Progress.Rank(slot)); }
        public float HasteForAbility(int slot) { return AbilityHaste + (slot<3&&Runes!=null?Runes.BasicAbilityHaste:0) + (slot < 3 && Inventory != null && Inventory.Has(3161) ? Inventory.Get(3161).Value("AHBase") : 0); }
        public float AbilityCooldown(int slot)
        {
            if (Blade != null) return Blade.AbilityCooldown(slot);
            return RivalAbilities.Cooldown(Archetype, slot, Progress.Rank(slot)) * 100 / (100 + HasteForAbility(slot));
        }
        public bool Ready(int slot)
        {
            if (Dummy != null || Kind != UnitKind.Champion || slot < 0 || slot > 3) return false;
            if (Blade != null) return Blade.Ready(slot);
            bool instantW = Mel != null && slot == 1 && Alive && Targetable && !IsStunned && !world.Paused && !world.RoundOver;
            return Progress.Rank(slot) > 0 && (instantW || CanAct && windingTarget == null) &&
                Cooldowns.Ready(slot, Time.time) && Resource >= AbilityCost(slot) &&
                (Mel == null || Mel.ValidCast(slot)) && !(Sett != null && slot == 3 && IsRooted);
        }

        public bool Cast(int slot, Vector3 aim, Combatant target = null)
        {
            if (slot < 0 || slot > 3 || Kind != UnitKind.Champion) return false;
            if (Blade != null) return Blade.Cast(slot, aim, target);
            if (!Ready(slot)) return false;
            float distance = target == null ? float.PositiveInfinity : Vector3.Distance(Position, target.Position);
            if (Sett != null && !Sett.ValidCast(slot, target)) return false;
            if (slot == 0 && Archetype == Archetype.Siphon && (!ValidEnemy(target) || distance > 6.5f)) return false;
            Cooldowns.Start(slot, Time.time, AbilityCooldown(slot)); Resource -= AbilityCost(slot);
            NotifySpellCast();
            if (Mel == null || slot != 1) Stop();
            if (Mel == null) busyUntil = Time.time + 0.15f;
            Vector3 direction = aim - Position; direction.y = 0;
            if (direction.sqrMagnitude < 0.01f) direction = Team == Team.Blue ? Vector3.right : Vector3.left;
            direction.Normalize();
            if (Archetype == Archetype.Brawler)
            {
                Sett.Cast(slot, direction, target);
            }
            else if (Archetype == Archetype.Siphon)
            {
                if (slot == 0) { DealAbilityDamage(target, 85, DamageType.Magic); Heal(65); world.FlashLine(Position + Vector3.up, target.Position + Vector3.up, Color); }
                if (slot == 1) { untargetableUntil = Time.time + 1.2f; busyUntil = Time.time + 0.15f; Health = Mathf.Max(1, Health - MaxHealth * 0.07f); }
                if (slot == 2) Shoot(direction, 7, 0.8f, 110, 0.4f, 0, false, true);
                if (slot == 3) { Shoot(direction, 9, 1.1f, 160, 0.55f, 0, false, true); Heal(90); }
            }
            else if (Mel != null) Mel.Cast(slot, aim, direction);
            world.AbilityUsed(this, slot);
            return true;
        }
        void Shoot(Vector3 direction, float range, float width, float damage, float delay, float stun, bool mark, bool pierce)
        {
            busyUntil = Time.time + delay;
            world.SpawnSkillshot(this, direction, range, width, damage, delay, stun, mark, pierce);
        }
    }
}
