using UnityEngine;

namespace MidLane
{
    public sealed partial class ChampionBrain
    {
        enum MelCombo { None, WaitSnare, Attack, Volley, CloseSnare, CloseBarrier, CloseAttack, Eclipse }
        MelCombo melCombo;
        MelSnare melSnare;
        Combatant melFarm, melWave;
        Vector3 melComboAim;
        float melMacroAt, melActionAt, melSnareDeadline, melTradeRest, melOrbitSide = 1;
        bool melMissFollowup, melFlashOpportunity, melSnareHit;
        int melPlayerLife = -1;
        public float MelWReaction { get { return HumanReaction(MelDecisionRules.WReaction(world.Settings.difficulty)); } }

        void ResetMelPlan()
        {
            melCombo = MelCombo.None; melSnare = null; melFarm = melWave = null;
            melMacroAt = melActionAt = melTradeRest = melFishAt = melOrbitAt = 0; melFlashOpportunity = false;
            melDefenseCue = null; melDefenseAt = -1; melDefenseSpent = false;
            eResponses.Clear();
            melPlayerLife = world.Player.Deaths;
            ResetMelLane();
        }
        void UpdateMelBrain()
        {
            Combatant player = world.Player, tower = EnemyTower();
            if (melPlayerLife != player.Deaths) { ResetMelPlan(); recovering = false; }
            for (int i = 0; i < 4; i++)
                if (observeAt[i] > 0 && Time.time >= observeAt[i])
                { observedReady[i] = pendingReady[i]; observeAt[i] = 0; }
            // W may be used during Mel's own cast/windup, but never while stunned.
            UpdateMelDefense(player);
            if (world.Settings.difficulty == Difficulty.Expert && self.CanAct && !self.IsWindingAttack &&
                !self.Mel.Reflecting && TryDodgeBladeLinks()) return;
            if (Time.time >= melMacroAt)
            {
                melMacroAt = Time.time + MelDecisionRules.MacroInterval;
                float dt = Time.time - lastSampleAt;
                estimatedVelocity = dt > 0 ? Vector3.ClampMagnitude((player.Position - lastPlayerPosition) / dt, player.MoveSpeed) : Vector3.zero;
                lastSampleAt = Time.time; lastPlayerPosition = player.Position;
                UpdateLaneWavePlan(player);
                if (MelLaneMode) FindMelLaneFarm(player,tower); else FindMelFarm(tower);
            }
            if (!self.CanAct || Time.time < melActionAt) return;
            if (TryLaneDefense(player)) { melActionAt = Time.time + .08f; return; }
            if (self.IsWindingAttack) return;
            melActionAt = Time.time + .08f;
            if (TowerRange(tower, self.Position) && (world.PvpModeActive || TowerTargetsUs(tower) || !CanSiege(tower, player)))
            { melCombo = MelCombo.None; Plan(LaneTactic.Retreat, "Mel: salir del alcance de torre"); MoveOrHold(TowerExit(tower)); return; }
            if (TryMelUltimate(player, true)) return;
            if (MelLaneMode) { ThinkMelLane(player,tower); return; }
            if (melFarm != null && self.ValidEnemy(melFarm) && !TowerRange(tower, FarmPoint(melFarm)))
            {
                melCombo = MelCombo.None; Plan(LaneTactic.Farm, "Mel: asegurar último golpe con básico");
                if (Vector3.Distance(self.Position, melFarm.Position) <= self.AttackReach(melFarm))
                { self.HoldAttackPosition(); self.TryAttack(melFarm); }
                else ApproachFarm(melFarm);
                return;
            }
            if (!player.Targetable)
            {
                melCombo = MelCombo.None;
                if (!world.PvpModeActive && melWave != null) { Plan(LaneTactic.Farm, "Mel: preparar siguiente last hit"); ApproachFarm(melWave); }
                else if (!world.PvpModeActive && CanSiege(tower, player))
                { Plan(LaneTactic.Push, "Mel: asediar con cobertura"); self.OrderAttack(tower); }
                else { Plan(LaneTactic.Zone, "Mel: esperar rival"); MoveOrHold(new Vector3(-Forward * 3, 0, 0)); }
                return;
            }
            float distance = Vector3.Distance(self.Position, player.Position);
            if (TryMelUltimate(player, false) || TryIgnite(player, tower) || TryMelFlash(player, tower)) return;
            if (ContinueMelCombo(player)) return;
            // Low-health retreat is spacing toward the tower, not an automatic trip to the fountain.
            if (self.Health < self.MaxHealth * .35f && world.Settings.difficulty != Difficulty.Expert && distance < 3)
            { Plan(LaneTactic.Retreat, "Mel: kitear hacia su torre"); MelKite(player, true); return; }
            if (Time.time >= melTradeRest && distance < 3 && self.Health > self.MaxHealth * .6f && self.Ready(0) && HumanCombatCast(0, MelAim(player, MelRules.QSpeed)))
            { melCombo = MelCombo.CloseSnare; Plan(LaneTactic.Trade, "Mel: Q → E → pasiva"); return; }
            if (world.Settings.difficulty == Difficulty.Expert && player.Blade != null && player.Blade.IsDashing && self.Ready(2))
            {
                // Read only the dash already visible in the world, never a queued input or a future target.
                Vector3 intercept = player.Blade.PredictDashPosition(MelRules.ECastTime + distance / MelRules.ESpeed);
                if (Vector3.Distance(self.Position, intercept) <= MelRules.ERange && StartMelSnare(intercept)) return;
            }
            bool exposed = player.IsWindingAttack || player.Blade != null && player.Blade.IsDashing;
            if (Time.time >= melTradeRest && distance <= MelRules.ERange &&
                (distance <= 6 || exposed && Time.time >= melFishAt && RollMelFishing()))
            {
                if (StartMelSnare(MelAim(player, MelRules.ESpeed))) return;
                if (distance <= MelRules.QRange && HumanCombatCast(0, MelAim(player, MelRules.QSpeed)))
                { melTradeRest = Time.time + .8f; Plan(LaneTactic.Trade, "Mel: Q de desgaste"); return; }
            }
            if (distance <= self.AttackReach(player) && self.AttackReady && !TowerRange(tower, self.Position))
            { self.HoldAttackPosition(); self.TryAttack(player); Plan(LaneTactic.Trade, "Mel: básico y kiteo"); return; }
            if (melWave != null && distance > 6 && Vector3.Distance(self.Position,melWave.Position) > self.AttackReach(melWave))
            { Plan(LaneTactic.Farm,"Mel: acercarse a la oleada sin empujarla"); ApproachFarm(melWave); return; }
            Plan(LaneTactic.Zone, "Mel: spacing y farmeo lateral"); MelKite(player, false);
        }
        float melFishAt;
        bool RollMelFishing() { melFishAt = Time.time + MelDecisionRules.MacroInterval; return Random.value < .3f; }

        void FindMelFarm(Combatant tower)
        {
            melFarm = melWave = null; float best = float.PositiveInfinity, nearest = float.PositiveInfinity;
            foreach (Combatant unit in world.Units)
            {
                if (unit == null || unit.Kind != UnitKind.Minion || !self.ValidEnemy(unit)) continue;
                float distance = Vector3.Distance(self.Position, unit.Position);
                if (distance > 12 || TowerRange(tower, FarmPoint(unit))) continue;
                if (distance < nearest) { nearest = distance; melWave = unit; }
                float horizon = Mathf.Max(self.AttackCooldownRemaining, Mathf.Max(0, distance - self.AttackReach(unit)) / self.MoveSpeed) +
                    self.AttackWindupDuration + distance / MelRules.BasicMissileSpeed;
                float hp = PredictMinionHealth(unit, horizon);
                float hit = unit.PreviewIncomingDamage(self.BasicDamageAgainst(unit, self.AttackDamage), self, DamageType.Physical);
                if (self.Mel.Stacks > 0) hit += unit.PreviewIncomingDamage(self.Mel.Stacks * MelRules.PassiveProjectile(self.Level, self.AbilityPower) * .5f, self, DamageType.Magic);
                if (hp <= 0 || hp > hit || horizon > 2) continue;
                float score = horizon - (unit.MinionType == MinionType.Cannon ? .2f : 0);
                if (score < best) { best = score; melFarm = unit; }
            }
        }
        bool StartMelSnare(Vector3 aim)
        {
            if (!HumanCombatCast(2, aim)) return false;
            if (MelLaneMode) BeginMelLaneTrade();
            melSnare = self.Mel.LastSnare; melComboAim = aim; melSnareHit = false;
            melSnareDeadline = Time.time + MelRules.ECastTime + Mathf.Min(MelRules.ERange, Vector3.Distance(self.Position, aim) + world.Player.Radius + MelRules.ECoreRadius) / MelRules.ESpeed + .1f;
            // One roll per cast, not a fresh chance every frame after a miss.
            melMissFollowup = !(Human != null && Human.SimplifyCombos) && Random.value < MelDecisionRules.MissInertia(world.Settings.difficulty);
            melCombo = MelCombo.WaitSnare; Plan(LaneTactic.Trade, "Mel: E → básico → Q"); return true;
        }
        bool ContinueMelCombo(Combatant player)
        {
            if (melCombo == MelCombo.None) return false;
            float distance = Vector3.Distance(self.Position, player.Position);
            if (melCombo == MelCombo.WaitSnare)
            {
                if (melSnare != null && melSnare.HasRooted(player)) { melSnareHit = true; melCombo = MelCombo.Attack; }
                else if (Time.time < melSnareDeadline) { MelKite(player, false); return true; }
                else if (melMissFollowup) melCombo = MelCombo.Volley;
                else { EndMelCombo(); MelKite(player, false); return true; }
            }
            if (melCombo == MelCombo.CloseSnare)
            { if (distance <= MelRules.ERange) HumanCombatCast(2, MelAim(player, MelRules.ESpeed)); melCombo = MelCombo.CloseBarrier; return true; }
            if (melCombo == MelCombo.CloseBarrier)
            {
                // Expert doesn't waste its defensive W solely to generate passive missiles.
                if (world.Settings.difficulty != Difficulty.Expert) HumanCombatCast(1, self.Position);
                melCombo = MelCombo.CloseAttack;
            }
            if (melCombo == MelCombo.Attack || melCombo == MelCombo.CloseAttack)
            {
                MelCombo next = melCombo == MelCombo.Attack ? MelCombo.Volley : MelCombo.Eclipse;
                if (distance <= self.AttackReach(player) && self.AttackReady)
                { self.HoldAttackPosition(); if (self.TryAttack(player)) { melCombo = next; return true; } }
                // No indefinite pursuit: fall back to the ranged spell if the auto cannot land.
                melCombo = next;
            }
            if (melCombo == MelCombo.Volley)
            {
                Vector3 aim = melSnareHit ? MelAim(player, MelRules.QSpeed) : melComboAim;
                EndMelCombo(); if (HumanCombatCast(0, aim)) return true;
            }
            if (melCombo == MelCombo.Eclipse) { EndMelCombo(); if (TryMelUltimate(player, false)) return true; }
            return false;
        }
        void EndMelCombo()
        {
            melCombo = MelCombo.None;
            if (MelLaneMode) EndMelLaneTrade(); else melTradeRest = Time.time + .8f;
        }
        Vector3 MelAim(Combatant player, float speed)
        {
            float lead = world.Settings.difficulty == Difficulty.Expert ? 1 : world.Settings.difficulty == Difficulty.Hard ? .6f : .2f;
            float error = world.Settings.difficulty == Difficulty.Expert ? .2f : world.Settings.difficulty == Difficulty.Hard ? .55f : 1;
            float travel = (speed == MelRules.ESpeed ? MelRules.ECastTime : MelRules.QCastTime) + Vector3.Distance(self.Position, player.Position) / speed;
            Vector3 velocity = player.IsRooted || player.IsStunned ? Vector3.zero : estimatedVelocity;
            return world.Clamp(player.Position + velocity * (travel * lead * HumanPrediction) + HumanAimOffset(error));
        }
        bool TryMelUltimate(Combatant player, bool lethalOnly)
        {
            int marks = self.Mel.OverwhelmStacks(player);
            if (!player.Targetable || !self.Ready(3) || !MelDecisionRules.CanUseUltimate(marks, player.Health / player.MaxHealth, player.IsRooted || player.IsStunned)) return false;
            float damage = player.PreviewIncomingDamage(MelRules.RDamage(self.Progress.Rank(3), marks, self.AbilityPower), self, DamageType.Magic);
            float execute = player.PreviewIncomingDamage(MelRules.ExecuteDamage(self.Progress.Rank(3), marks + 1, self.AbilityPower), self, DamageType.Magic);
            bool lethal = damage + execute >= player.Health + player.TotalShield + player.HealthRegeneration * MelRules.RCastTime;
            if (!lethal && (lethalOnly || player.Health > player.MaxHealth * .4f || !player.IsRooted && !player.IsStunned)) return false;
            if (!HumanCombatCast(3, player.Position)) return false;
            EndMelCombo(); Plan(LaneTactic.AllIn, "Mel: Eclipse Dorado sobre marcas"); return true;
        }
        void MelKite(Combatant player, bool straightHome)
        {
            Vector3 away = MelAbilities.Flat(self.Position - player.Position).normalized;
            if (away.sqrMagnitude < .01f) away = Vector3.left * Forward;
            Vector3 lateral = Vector3.Cross(away, Vector3.up);
            if (Time.time >= melOrbitAt) { melOrbitAt = Time.time + .8f; melOrbitSide = Random.value < .5f ? -1 : 1; }
            float desired = (melWave != null ? 6.5f : self.AttackReach(player) - .35f) * HumanTether;
            Vector3 point = player.Position + away * desired;
            if (straightHome) point = self.Position + (world.FountainPoint(self.Team) - self.Position).normalized * 2;
            else if (world.Settings.difficulty == Difficulty.Hard) point += lateral * melOrbitSide * 1.25f;
            else if (world.Settings.difficulty == Difficulty.Expert) point += lateral * melOrbitSide * 2;
            // Stay outside executable allied minions: they are visible Q stepping stones.
            if (!straightHome && Time.time >= observedReady[0] && player.Blade != null)
                foreach (Combatant minion in world.Units)
                    if (minion != null && minion.Alive && minion.Kind == UnitKind.Minion && minion.Team == self.Team &&
                        Vector3.Distance(player.Position,minion.Position) <= player.Blade.Tuning.qRange &&
                        minion.Health <= Physical(player,minion,player.Blade.QRawDamage(minion)) && Vector3.Distance(point,minion.Position) < 3)
                        point += (point-minion.Position).normalized * 1.5f;
            point = world.Clamp(world.AvoidTower(self, point), self.Radius);
            if (MelLaneMode && !straightHome) point = LimitMelLanePoint(point);
            if (TowerRange(EnemyTower(),point)) point = TowerExit(EnemyTower());
            self.HoldAttackPosition(); MoveOrHold(point);
        }
        float melOrbitAt;
        bool TryMelFlash(Combatant player, Combatant tower)
        {
            float distance = Vector3.Distance(self.Position, player.Position);
            float hp = player.Health + player.TotalShield + player.HealthRegeneration;
            float hit = Physical(self,player,self.AttackDamage) + player.PreviewIncomingDamage(self.Mel.Stacks * MelRules.PassiveProjectile(self.Level,self.AbilityPower),self,DamageType.Magic);
            bool snareFollowup = melCombo == MelCombo.WaitSnare && melSnare != null;
            float snareDamage = snareFollowup ? player.PreviewIncomingDamage(MelRules.EDamage(self.Progress.Rank(2),self.AbilityPower),self,DamageType.Magic) : 0;
            bool opportunity = player.Targetable && player.Health < player.MaxHealth * .2f && distance > self.AttackReach(player) + .7f &&
                distance <= self.AttackReach(player) + SummonerRules.FlashRange - .2f && hit + snareDamage >= hp && self.AttackReady &&
                Vector3.Dot(player.Velocity,(player.Position-self.Position).normalized) > .5f;
            if (!opportunity) { melFlashOpportunity = false; return false; }
            // Global R needs no Flash. Don't spend Flash if a ready spell can finish from here.
            if (melFlashOpportunity || self.Summoners.FlashRemaining > 0 || self.Ready(3) || self.Ready(0) && distance <= MelRules.QRange) return false;
            melFlashOpportunity = true;
            if (Random.value >= MelDecisionRules.OffensiveFlashChance(world.Settings.difficulty)) return false;
            Vector3 landing = world.Clamp(player.Position - (player.Position-self.Position).normalized * (self.AttackReach(player)-.15f),self.Radius);
            if (TowerRange(tower, landing) || TowerRange(tower,player.Position) || !self.Summoners.Flash(landing)) return false;
            self.HoldAttackPosition(); self.TryAttack(player);
            Plan(LaneTactic.AllIn,snareFollowup ? "Mel: E → Flash → remate" : "Mel: Flash ofensivo para rematar"); return true;
        }
    }
}
