using System;
using System.Collections.Generic;
using UnityEngine;

namespace MidLane
{
    public sealed partial class LaneWorld : MonoBehaviour
    {
        public SandboxSettings Settings { get; private set; }
        public ScenePresentationConfig Presentation { get; private set; }
        public Combatant Player { get; private set; }
        public Combatant Opponent { get; private set; }
        public Camera View { get; private set; }
        public FlashAudio FlashSounds { get; private set; }
        public bool RoundOver { get; private set; }
        public string Result { get; private set; }
        public float StartedAt { get; private set; }
        public float NextWave { get; private set; }
        public int Wave { get; private set; }
        public bool PvpModeActive { get { return Settings != null && Settings.pvpMode; } }
        public int PvpDeaths { get; private set; }
        int pvpBonusLevels, pvpProgressDeaths;
        public int PvpProgressDeaths { get { return pvpProgressDeaths; } }
        public int PvpLevel { get { return Mathf.Min(18, 1 + pvpProgressDeaths / PvpProgressionRules.DeathsPerLevel + pvpBonusLevels); } }
        public float MatchTime { get { return SwiftplayRules.OpeningClock + Time.time - StartedAt; } }
        public float ElapsedSeconds { get { return Mathf.Max(0, Time.time - StartedAt); } }
        public readonly List<Combatant> Units = new List<Combatant>();
        public readonly List<Skillshot> Shots = new List<Skillshot>();
        public readonly List<BasicProjectile> BasicProjectiles = new List<BasicProjectile>();
        public readonly List<BladeLink> BladeLinks = new List<BladeLink>();
        public readonly List<BladeWave> BladeWaves = new List<BladeWave>();
        public readonly List<BladeWall> BladeWalls = new List<BladeWall>();
        public readonly List<ChampionNotice> Notices = new List<ChampionNotice>();
        public sealed class ChampionNotice
        {
            public Combatant Actor;
            public string Text;
            public Color Color;
            public float StartedAt;
        }
        public void ShowNotice(Combatant actor, string message, Color color)
        {
            if (actor != Player) return;
            Notices.Add(new ChampionNotice { Actor = actor, Text = message, Color = color, StartedAt = Time.time });
            if (Notices.Count > 8) Notices.RemoveAt(0);
        }
        public void ChampionLeveled(Combatant actor) { if(actor.Runes!=null) actor.Runes.SynchronizeProgress(); ShowNotice(actor, "Level Up", new Color(1, 0.85f, 0.3f)); }
        public event Action<Combatant, int, float> OnAbilityUsed;
        public bool Paused { get; private set; }
        public Team PlayerTeam { get { return Player != null ? Player.Team : Team.Blue; } }
        public readonly TowerNavigation Navigation = new TowerNavigation();
        public readonly MinionSpacing Spacing = new MinionSpacing();
        public void ToggleTestMode()
        {
            Settings.testMode = !Settings.testMode;
            if (!Settings.testMode) return;
            if (!Player.Alive) Player.Respawn();
            Player.ItemEffects.CancelRebirthForPractice();
            Player.Heal(Player.MaxHealth);
        }
        Transform arenaRoot, roundRoot;
        readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();

        void Awake()
        {
            var storedSettings = Resources.Load<SandboxSettings>("SandboxSettings");
            Settings = storedSettings != null ? Instantiate(storedSettings) : null;
            if (Settings == null) Settings = ScriptableObject.CreateInstance<SandboxSettings>();
            Presentation = ScenePresentationConfig.Load();
            InitializePracticeOptions();
            EnforcePracticeVisualMode();
            FlashSounds = gameObject.AddComponent<FlashAudio>(); FlashSounds.Initialize(this);
            ApplyMapDimensions();
            BuildArena(); Restart();
            gameObject.AddComponent<PlayerCommands>().Initialize(this);
            gameObject.AddComponent<PracticeHud>().Initialize(this);
            gameObject.AddComponent<ShopHud>().Initialize(this);
            gameObject.AddComponent<VisualTuningPanel>().Initialize(this);
        }

        void Update()
        {
            Notices.RemoveAll(notice => notice.Actor == null || Time.time - notice.StartedAt > 2.5f);
            TickPracticeMode();
            if (Paused || RoundOver || !Settings.spawnWaves || Time.time < NextWave) return;
            float waveTime = SwiftplayRules.OpeningClock + NextWave - StartedAt;
            NextWave += GetWaveInterval(waveTime);
            SpawnWave(waveTime);
        }
        public float GetWaveInterval(float matchTime) { return SwiftplayRules.WaveInterval(matchTime) + Mathf.Max(0, Settings.extraWaveInterval); }
        public void TogglePvpMode()
        {
            // Practice-only visual edition: mode switching is unavailable.
            EnforcePracticeVisualMode();
        }

        void LateUpdate()
        {
            if (Paused || RoundOver) return;
            Spacing.Resolve(this);
            // Observe final gameplay positions, including dashes, Flash and collision corrections.
            foreach (Combatant champion in Units)
                if (champion != null && champion.Kind == UnitKind.Champion) champion.TickPvpIdleReset();
            if (NormalplayModeActive) SynchronizeNormalplayProgress();
            else if (Settings.keepOpponentProgress && Opponent != null)
            {
                Opponent.CatchUpProgress(Player);
                Player.CatchUpProgress(Opponent);
            }
            SynchronizeRunes();
        }

        // Shared by the timed spawner and validation; complete, symmetric waves.
        public void SpawnWave(float matchTime)
        {
            if (Paused || RoundOver || PvpModeActive || PracticeModeActive) return;
            Wave++;
            MinionType[] composition = SwiftplayRules.Composition(Wave, matchTime);
            for (int team = 0; team < 2; team++)
            {
                int sign = team == 0 ? 1 : -1;
                int meleeIndex = 0, casterIndex = 0;
                for (int i = 0; i < composition.Length; i++)
                {
                    MinionType type = composition[i];
                    float offset = type == MinionType.Melee ? 5 : type == MinionType.Cannon ? 3.5f : 2;
                    float z = type == MinionType.Cannon ? 0 : ((type == MinionType.Melee ? meleeIndex++ : casterIndex++) - 1) * 1.25f;
                    string name = type == MinionType.Melee ? "Súbdito guerrero" : type == MinionType.Caster ? "Súbdito mago" : "Súbdito de cañón";
                    Combatant minion = Spawn((Team)team, UnitKind.Minion, Archetype.Blade, name,
                        new Vector3(-sign * (Settings.halfLength - offset), 0, z), type);
                    minion.gameObject.AddComponent<LaneUnitBrain>().Initialize(this, minion);
                }
            }
        }

        void ApplyMapDimensions()
        { Settings.halfLength = TowerToTowerRules.HalfLength; Settings.halfWidth = Presentation.lane_width_unity / 2; }

        public void Restart()
        {
            EnforcePracticeVisualMode();
            var commands = GetComponent<PlayerCommands>(); if (commands != null) commands.ResetPendingInputs();
            var summonerHud = GetComponent<PracticeHud>();
            if (summonerHud != null) { if (summonerHud.LatencyFieldFocused) summonerHud.EndLatencyEdit(); summonerHud.CloseSummonerPicker(); summonerHud.CloseRunePicker(); summonerHud.ResetShieldBars(); }
            ApplyMapDimensions();
            Time.timeScale = 1; Paused = false;
            if (FlashSounds != null) FlashSounds.StopAll();
            if (roundRoot != null)
            {
                // Disable immediately: Destroy itself is deferred until the end of the frame.
                roundRoot.gameObject.SetActive(false); Destroy(roundRoot.gameObject);
            }
            Units.Clear(); Shots.Clear(); BasicProjectiles.Clear(); BladeLinks.Clear(); BladeWaves.Clear(); BladeWalls.Clear(); Notices.Clear(); OnAbilityUsed = null; Navigation.Clear(); Spacing.Clear();
            PracticeDummies.Clear(); PvpQDummy = null; practiceResetArmed = false; practiceResetAt = 0;
            roundRoot = new GameObject("Round").transform; roundRoot.SetParent(transform);
            RoundOver = false; Result = ""; Wave = 0; StartedAt = Time.time; PvpDeaths = 0; pvpBonusLevels = pvpProgressDeaths = 0;
            if (PracticeModeActive) Settings.pvpMode = false;
            if (PvpModeActive || PracticeModeActive) Settings.spawnWaves = false;
            NextWave = StartedAt + SwiftplayRules.FirstWaveTime - SwiftplayRules.OpeningClock;
            Team playerTeam = Settings.randomizePlayerSide ? (UnityEngine.Random.value < 0.5f ? Team.Blue : Team.Red) : Settings.fixedPlayerSide;
            int playerSign = playerTeam == Team.Blue ? -1 : 1;
            Player = Spawn(playerTeam, UnitKind.Champion, Archetype.Blade, "Irelia", FountainPoint(playerTeam));
            Player.Summoners.ConfigureLoadout(Settings.dSummoner, Settings.fSummoner);
            string label = LeagueChampionStats.For(Settings.opponent).Name;
            Team enemyTeam = playerTeam == Team.Blue ? Team.Red : Team.Blue;
            Opponent = null;
            if (PracticeModeActive) CreatePracticeTargets(enemyTeam);
            else
            {
                Opponent = Spawn(enemyTeam, UnitKind.Champion, Settings.opponent, label, FountainPoint(enemyTeam));
                Opponent.gameObject.AddComponent<ChampionBrain>().Initialize(this, Opponent);
            }
            for (int team = 0; team < 2; team++)
            {
                Combatant tower = Spawn((Team)team, UnitKind.Tower, Archetype.Brawler, "Torre",
                    new Vector3((team == 0 ? -1 : 1) * TowerToTowerRules.HalfLength, 0, 0));
                tower.gameObject.AddComponent<LaneUnitBrain>().Initialize(this, tower);
                if (PracticeModeActive) tower.GetComponent<LaneUnitBrain>().enabled = false;
            }
            ApplyPvpStartingItems(); CreatePvpQDummy();
            SynchronizeRunes();
            View.GetComponent<IsometricCamera>().SetLocked(true);
        }

        public void TogglePause() { Paused = !Paused; Time.timeScale = Paused ? 0 : 1; }
        void OnDestroy()
        {
            RestorePracticeAudio();
            Time.timeScale = 1;
            foreach (Material material in materials.Values) Destroy(material);
            if (Settings != null) Destroy(Settings);
        }
        public Vector3 Clamp(Vector3 point, float radius = 0)
        {
            return new Vector3(Mathf.Clamp(point.x, -TowerToTowerRules.HalfLength + radius, TowerToTowerRules.HalfLength - radius), 0,
                Mathf.Clamp(point.z, -Settings.halfWidth + radius, Settings.halfWidth - radius));
        }

        public Vector3 AvoidTower(Combatant actor, Vector3 point)
        {
            point = Clamp(point, actor.Radius);
            foreach (Combatant unit in Units)
            {
                if (unit == null || !unit.Alive || unit.Kind != UnitKind.Tower) continue;
                Vector3 offset = point - unit.Position;
                float radius = actor.Radius + unit.Radius;
                if (offset.sqrMagnitude >= radius * radius) continue;
                if (offset.sqrMagnitude < 0.01f) offset = Vector3.forward;
                point = unit.Position + offset.normalized * radius;
            }
            return Clamp(point, actor.Radius);
        }
        // Place recovery beside the end turret, wholly inside the compact field.
        public Vector3 FountainPoint(Team team) { return new Vector3((team == Team.Blue ? -1 : 1) * (TowerToTowerRules.HalfLength - 1.5f), 0, 2.5f); }
        public bool InFountain(Combatant actor) { return Vector3.Distance(actor.Position, FountainPoint(actor.Team)) <= TowerToTowerRules.RecoveryRadius; }

        void BuildArena()
        {
            arenaRoot = new GameObject("Arena").transform; arenaRoot.SetParent(transform);
            Shape("Ground", PrimitiveType.Cube, arenaRoot, new Vector3(0, -0.35f, 0),
                new Vector3(Settings.halfLength * 2 + 80, 0.5f, Settings.halfWidth * 2 + 80), new Color(0.09f, 0.18f, 0.11f), true);
            var terrain = new GameObject("Lane terrain"); terrain.transform.SetParent(arenaRoot, false);
            terrain.AddComponent<LaneTerrain>().Build(this);
            LaneBoundaryVisuals.Build(this, arenaRoot);
            Sun = new GameObject("Sun").AddComponent<Light>(); Sun.transform.SetParent(arenaRoot);
            Sun.type = LightType.Directional; Sun.shadows = LightShadows.Soft;
            ApplyLighting();
            View = new GameObject("Isometric Camera").AddComponent<Camera>(); View.transform.SetParent(arenaRoot);
            View.tag = "MainCamera";
            View.gameObject.AddComponent<AudioListener>();
            View.nearClipPlane = 0.1f; View.farClipPlane = 100;
            View.clearFlags = CameraClearFlags.SolidColor; View.backgroundColor = new Color(0.035f, 0.065f, 0.1f);
            View.gameObject.AddComponent<IsometricCamera>().Initialize(this);
            for (int team = 0; team < 2; team++)
            {
                var circle = new GameObject("Recovery circle " + (Team)team); circle.transform.SetParent(arenaRoot, false);
                circle.transform.localPosition = FountainPoint((Team)team);
                circle.AddComponent<RecoveryCircle>().Build((Team)team, TowerToTowerRules.RecoveryRadius);
            }
        }

        Combatant Spawn(Team team, UnitKind kind, Archetype archetype, string label, Vector3 point, MinionType minionType = MinionType.Melee)
        {
            var root = new GameObject(label); root.transform.SetParent(roundRoot); root.transform.position = point;
            var model = new GameObject("Model").transform; model.SetParent(root.transform, false);
            Color color = team == Team.Blue ? new Color(0.2f, 0.8f, 1f) : new Color(1f, 0.35f, 0.4f);
            if (kind == UnitKind.Tower)
                PracticeModels.CreateTower(this, model, team);
            else if (kind == UnitKind.Minion)
                MinionVisuals.Create(this, model, team, minionType);
            else if (archetype == Archetype.Blade)
                PracticeModels.CreateIrelia(this, model, team);
            else
                PracticeModels.CreateChampionBody(this, model, team, archetype);
            var collider = root.AddComponent<CapsuleCollider>();
            collider.height = TowerToTowerRules.SelectionHeight(kind, minionType);
            collider.radius = TowerToTowerRules.Radius(kind, minionType);
            collider.center = Vector3.up * (collider.height / 2);
            var actor = root.AddComponent<Combatant>(); actor.Initialize(this, team, kind, archetype, label, minionType);
            model.rotation = Quaternion.LookRotation(team == Team.Blue ? Vector3.right : Vector3.left);
            var tilt = model.GetComponentInChildren<CameraFacingVisualTilt>(); if (tilt != null) tilt.Apply();
            Units.Add(actor); return actor;
        }

        public GameObject Shape(string label, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 scale, Color color, bool collider = false)
        {
            GameObject shape = GameObject.CreatePrimitive(type); shape.name = label; shape.transform.SetParent(parent, false);
            shape.transform.localPosition = localPosition; shape.transform.localScale = scale;
            var hitbox = shape.GetComponent<Collider>();
            if (!collider) { hitbox.enabled = false; Destroy(hitbox); }
            shape.GetComponent<Renderer>().sharedMaterial = Material(color); return shape;
        }
        public Material Material(Color color)
        {
            Material material;
            if (materials.TryGetValue(color, out material)) return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader); material.color = color;
            materials.Add(color, material); return material;
        }
        public void SpawnSkillshot(Combatant owner, Vector3 direction, float range, float width, float damage, float delay, float stun, bool mark, bool pierce)
        {
            var shot = new GameObject("Skillshot").AddComponent<Skillshot>(); shot.transform.SetParent(roundRoot);
            shot.Initialize(this, owner, direction, range, width, damage, delay, stun, mark, pierce); Shots.Add(shot);
        }
        public BasicProjectile CreateBasicProjectile(Combatant owner, Combatant target, float damage)
        {
            var shot = new GameObject("Ataque de súbdito").AddComponent<BasicProjectile>();
            shot.transform.SetParent(roundRoot); shot.Initialize(this, owner, target, damage); return shot;
        }
        public void FlashLine(Vector3 from, Vector3 to, Color color)
        {
            GameObject flash = Shape("Hit", PrimitiveType.Cube, roundRoot, (from + to) * 0.5f,
                new Vector3(0.08f, 0.08f, Vector3.Distance(from, to)), color);
            if ((to - from).sqrMagnitude > 0.01f) flash.transform.rotation = Quaternion.LookRotation(to - from);
            Destroy(flash, 0.1f);
        }
        public BladeLink CreateBladeLink(BladeDuelist owner, Vector3 first)
        {
            var link = new GameObject("E Dueto perfecto").AddComponent<BladeLink>(); link.transform.SetParent(roundRoot);
            link.Initialize(this, owner, first); BladeLinks.Add(link); return link;
        }
        public void CreateBladeSlash(BladeDuelist owner, Vector3 direction, float damage)
        {
            var wave = new GameObject("W Danza de Insurrección").AddComponent<BladeWave>(); wave.transform.SetParent(roundRoot);
            wave.Initialize(this, owner, direction, false, damage); BladeWaves.Add(wave);
        }
        public void CreateBladeUltimate(BladeDuelist owner, Vector3 direction)
        {
            var wave = new GameObject("R Filo de Vanguardia").AddComponent<BladeWave>(); wave.transform.SetParent(roundRoot);
            wave.Initialize(this, owner, direction, true, 50 + 75 * owner.Rank(3) + owner.Actor.AbilityPower); BladeWaves.Add(wave);
        }
        public BladeWall CreateBladeWall(BladeDuelist owner, Vector3 center, Vector3 direction, int castId = 0, Combatant reflectedSource = null, float reflectedMultiplier = 1)
        {
            var wall = new GameObject("R Muro de cuchillas").AddComponent<BladeWall>(); wall.transform.SetParent(roundRoot);
            wall.Initialize(this, owner, center, direction, castId, reflectedSource, reflectedMultiplier); BladeWalls.Add(wall); return wall;
        }
        public void AbilityUsed(Combatant actor, int slot)
        {
            actor.RecordPvpActivity();
            PracticeAbilityUsed(actor);
            if (OnAbilityUsed != null) OnAbilityUsed(actor, slot, Time.time + actor.AbilityCooldown(slot));
        }
        public void DamageApplied(Combatant source, Combatant target, float damage, int abilitySlot = -1)
        {
            if (damage > 0) { source.RecordPvpActivity(); target.RecordPvpActivity(); }
            if (source.Kind != UnitKind.Champion || target.Kind != UnitKind.Champion || damage <= 0) return;
            if (PvpModeActive && source == Player && target == Opponent && target.Sett != null)
            { var brain = target.GetComponent<ChampionBrain>(); if (brain != null) brain.ReceivePvpOpener(source); }
            foreach (Combatant unit in Units)
                if (unit != null && unit.Alive && (unit.Kind == UnitKind.Tower || unit.Kind == UnitKind.Minion) && unit.Team == target.Team &&
                    Vector3.Distance(unit.Position, source.Position) <= unit.AttackReach(source))
                { var brain = unit.GetComponent<LaneUnitBrain>(); if (brain != null) brain.Provoke(source); }
        }
        public void UnitDied(Combatant actor, Combatant killer)
        {
            if (actor.Kind == UnitKind.Minion)
                foreach (Combatant champion in Units)
                    if (champion != null && champion.Alive && champion.Kind == UnitKind.Champion && champion.Team != actor.Team &&
                        Vector3.Distance(champion.Position, actor.Position) <= Player.Blade.Tuning.experienceRadius)
                        champion.GainExperience(actor.MinionStats.Experience);
            if (actor.Kind == UnitKind.Champion)
            {
                foreach (Combatant champion in Units)
                    if (champion != null && champion.Kind == UnitKind.Champion && champion.Team != actor.Team)
                    {
                        if (champion != killer && actor.WasDamagedBy(champion, 10)) champion.GrantAssist();
                        if (champion == killer || actor.WasDamagedBy(champion, 3)) champion.ItemEffects.OnTakedown();
                        if ((champion == killer || actor.WasDamagedBy(champion, 3)) && champion.Runes!=null) champion.Runes.OnTakedown();
                    }
                Navigation.Forget(actor);
                if (killer == Player) ShowNotice(Player, "Unstopable", new Color(1, 0.48f, 0.18f));
                actor.ScheduleRespawn(Settings.usePracticeRespawnTime ? Mathf.Max(0, Settings.practiceRespawnSeconds) : SwiftplayRules.RespawnSeconds(actor.Level, MatchTime));
                if (PvpModeActive && (actor == Player || actor == Opponent))
                {
                    PvpDeaths++;
                    if (ProgressionEnabled)
                    { pvpProgressDeaths++; ApplyPvpProgression(); }
                    if (killer == Player || killer == Opponent) killer.RestoreAfterPvpKill();
                }
                // Every champion death starts a fresh duel for both sides, even after a tower kill.
                foreach (Combatant champion in Units)
                    if (champion != null && champion.Kind == UnitKind.Champion)
                    {
                        champion.ResetCombatCooldowns();
                        if(champion.Runes!=null) champion.Runes.OnAnyChampionDeath();
                        var brain = champion.GetComponent<ChampionBrain>();
                        if (brain != null) { brain.ForgetObservedCooldowns(); brain.ResetPvpEngagement(); }
                    }
            }
            else if (actor.Kind == UnitKind.Minion) StartCoroutine(RemoveCorpse(actor));
        }
        System.Collections.IEnumerator RemoveCorpse(Combatant actor)
        {
            yield return new WaitForSeconds(2);
            Units.Remove(actor); if (actor != null) Destroy(actor.gameObject);
        }
    }
}
