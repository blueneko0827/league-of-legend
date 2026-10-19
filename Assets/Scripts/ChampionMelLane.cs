using UnityEngine;

namespace MidLane
{
    public sealed partial class ChampionBrain
    {
        bool MelLaneMode { get { return world.Settings.spawnWaves && !world.PvpModeActive; } }
        Vector3 melLaneAnchor, melTradeOrigin, melFarmQPoint;
        Combatant melPrepared;
        bool melHasWave, melReturning;
        float melFarmFireAt, melPreparedImpact, melLaneTradeUntil, melFarmQAt, melFarmQDamage;
        public Vector3 MelFarmPosition { get { return melLaneAnchor; } }
        public Combatant MelPreparedMinion { get { return melPrepared; } }
        public float MelTradeRemaining { get { return Mathf.Max(0,melLaneTradeUntil-Time.time); } }

        void ResetMelLane()
        {
            melLaneAnchor = new Vector3(-Forward*3,0,0); melTradeOrigin = melLaneAnchor;
            melHasWave = melReturning = false; melPrepared = null;
            melFarmFireAt = melPreparedImpact = melLaneTradeUntil = melFarmQAt = melFarmQDamage = 0;
        }
        Vector3 MelHome { get { return Vector3.left * Forward; } }
        Vector3 SafeMelLanePoint(Vector3 point)
        {
            point = world.Clamp(world.AvoidTower(self,point),self.Radius);
            Combatant tower = EnemyTower();
            return TowerRange(tower,point) ? TowerExit(tower) : point;
        }
        Vector3 LimitMelLanePoint(Vector3 point)
        {
            // The live wave and the start of this trade both bound an offensive excursion.
            point = melLaneAnchor + Vector3.ClampMagnitude(point-melLaneAnchor,MelDecisionRules.LaneLeash);
            if (melLaneTradeUntil > Time.time)
                point = melTradeOrigin + Vector3.ClampMagnitude(point-melTradeOrigin,MelDecisionRules.LaneLeash);
            return SafeMelLanePoint(point);
        }
        Vector3 MelFarmPoint(Combatant minion)
        { return SafeMelLanePoint(minion.Position+MelHome*(self.AttackReach(minion)-.2f)); }

        void UpdateMelLaneAnchor()
        {
            melWave = null; float nearest = float.PositiveInfinity;
            foreach (Combatant unit in world.Units)
                if (unit != null && unit.Kind == UnitKind.Minion && self.ValidEnemy(unit))
                {
                    float d = (unit.Position-self.Position).sqrMagnitude;
                    if (d < nearest) { nearest = d; melWave = unit; }
                }
            melHasWave = melWave != null;
            if (!melHasWave)
            {
                // Follow our advancing wave up to mid; don't choose Irelia as the neutral objective.
                Combatant front = null;
                foreach (Combatant unit in world.Units)
                    if (unit != null && unit.Alive && unit.Kind == UnitKind.Minion && unit.Team == self.Team &&
                        (front == null || unit.Position.x*Forward > front.Position.x*Forward)) front = unit;
                Vector3 stage = front == null ? new Vector3(-Forward*3,0,0) : front.Position+MelHome*2;
                melLaneAnchor = SafeMelLanePoint(stage); return;
            }
            Vector3 center = Vector3.zero; int count = 0;
            foreach (Combatant unit in world.Units)
                if (unit != null && unit.Kind == UnitKind.Minion && self.ValidEnemy(unit) && Vector3.Distance(unit.Position,melWave.Position) <= 6)
                { center += unit.Position; count++; }
            center /= Mathf.Max(1,count);
            Vector3 point = center+MelHome*(self.AttackRange+.5f), casters = Vector3.zero; int casterCount = 0;
            foreach (Combatant unit in world.Units)
                if (unit != null && unit.Alive && unit.Kind == UnitKind.Minion && unit.Team == self.Team &&
                    unit.MinionType == MinionType.Caster && Vector3.Distance(unit.Position,center) < 9)
                { casters += unit.Position; casterCount++; }
            if (casterCount > 0) point = Vector3.Lerp(point,casters/casterCount+MelHome*.85f,.45f);
            melLaneAnchor = SafeMelLanePoint(point);
        }
        bool MelFarmSafe(Combatant player,Vector3 point)
        {
            if (TowerRange(EnemyTower(),point)) return false;
            if (!player.Targetable || Vector3.Distance(player.Position,point) > 7) return true;
            float hp = self.Health/self.MaxHealth, enemyHp = player.Health/player.MaxHealth;
            bool exposed = hp < .45f && enemyHp > hp+.2f || ThreatDamage(player,point,1.5f) > (self.Health+self.TotalShield)*.7f;
            return !exposed;
        }
        float MelBasicMinionDamage(Combatant minion)
        {
            float damage = minion.PreviewIncomingDamage(self.BasicDamageAgainst(minion,self.AttackDamage),self,DamageType.Physical,true);
            damage += minion.PreviewIncomingDamage(self.Mel.Stacks*MelRules.PassiveProjectile(self.Level,self.AbilityPower)*.5f,self,DamageType.Magic);
            return damage;
        }
        float MelAttackArrival(Combatant minion,Vector3 point)
        {
            bool inRange = Vector3.Distance(self.Position,minion.Position) <= self.AttackReach(minion);
            float walk = inRange ? 0 : Vector3.Distance(self.Position,point)/self.MoveSpeed;
            return Mathf.Max(walk,self.AttackCooldownRemaining)+self.AttackWindupDuration+
                Vector3.Distance(inRange?self.Position:point,minion.Position)/MelRules.BasicMissileSpeed;
        }
        public float PredictMelMinionHealth(Combatant minion,float horizon)
        {
            float hp = minion.Health;
            foreach (BasicProjectile shot in world.BasicProjectiles)
                if (shot != null && shot.Target == minion && shot.Source != null && shot.TimeToImpact <= horizon)
                    hp -= minion.PreviewIncomingDamage(shot.Source.BasicDamageAgainst(minion,shot.RawDamage),shot.Source,DamageType.Physical,true);
            foreach (MelBolt bolt in self.Mel.Projectiles)
                if (bolt != null && bolt.Target == minion && bolt.TimeToImpact <= horizon)
                    hp -= minion.PreviewIncomingDamage(bolt.RawDamage,self,bolt.Type,bolt.IsBasic);
            if (melFarmQAt >= Time.time && melFarmQAt <= Time.time+horizon &&
                Vector3.Distance(minion.Position,melFarmQPoint) <= MelRules.QExplosionRadius+minion.Radius)
                hp -= minion.PreviewIncomingDamage(melFarmQDamage,self,DamageType.Magic);
            foreach (Combatant unit in world.Units)
            {
                if (unit == null || unit == self || !unit.Alive || !unit.enabled || unit.Team == minion.Team) continue;
                bool winding = unit.PendingAttackTarget == minion;
                if (!winding && unit.AttackTarget != minion) continue;
                float travel = unit.Kind == UnitKind.Minion && unit.MinionType != MinionType.Melee ?
                    Vector3.Distance(unit.Position,minion.Position)/(unit.MinionType == MinionType.Cannon?12:6.5f) : 0;
                float damage = minion.PreviewIncomingDamage(unit.BasicDamageAgainst(minion,unit.AttackDamage),unit,DamageType.Physical,true);
                if (winding && unit.AttackWindupRemaining+travel <= horizon) hp -= damage;
                // Stable minion attack cycles can be forecast; don't invent future champion decisions.
                if (unit.Kind != UnitKind.Minion || Vector3.Distance(unit.Position,minion.Position) > unit.AttackReach(minion)) continue;
                float next = Mathf.Max(unit.AttackCooldownRemaining,winding?unit.AttackWindupRemaining:0)+unit.AttackWindupDuration+travel;
                for (; next <= horizon; next += 1/unit.BaseAttackSpeed) hp -= damage;
            }
            return hp;
        }
        void FindMelLaneFarm(Combatant player,Combatant tower)
        {
            Combatant previous = melFarm;
            UpdateMelLaneAnchor(); melFarm = melPrepared = null;
            float best = float.PositiveInfinity, ready = 0, impact = 0;
            foreach (Combatant unit in world.Units)
            {
                if (unit == null || unit.Kind != UnitKind.Minion || !self.ValidEnemy(unit) || Vector3.Distance(unit.Position,melLaneAnchor) > 14) continue;
                Vector3 point = MelFarmPoint(unit);
                if (!MelFarmSafe(player,point)) continue;
                float arrival = MelAttackArrival(unit,point), hit = MelBasicMinionDamage(unit)*MelDecisionRules.LastHitSafety(world.Settings.difficulty);
                float end = MelDecisionRules.FarmLookahead(world.Settings.difficulty);
                for (float time = arrival; time <= end; time += .1f)
                {
                    float hp = PredictMelMinionHealth(unit,time);
                    if (hp <= 0) break;
                    if (hp > hit) continue;
                    float score = time-(unit.MinionType == MinionType.Cannon?.35f:0);
                    if (score < best)
                    {
                        best = score; melFarm = melPrepared = unit;
                        Vector3 launch = Vector3.Distance(self.Position,unit.Position) <= self.AttackReach(unit) ? self.Position : point;
                        float flight = self.AttackWindupDuration+Vector3.Distance(launch,unit.Position)/MelRules.BasicMissileSpeed;
                        ready = Time.time+Mathf.Max(0,time-flight); impact = Time.time+time;
                    }
                    break;
                }
            }
            if (melFarm != null)
            {
                if (melFarm != previous) ready = Mathf.Max(ready,Time.time+MelDecisionRules.FarmReaction(world.Settings.difficulty));
                melFarmFireAt = ready; melPreparedImpact = impact;
            }
        }
        void BeginMelLaneTrade()
        {
            if (melLaneTradeUntil > Time.time) return;
            melLaneTradeUntil = Time.time+MelDecisionRules.TradeDuration; melTradeOrigin = melLaneAnchor;
            melReturning = false;
        }
        void EndMelLaneTrade()
        {
            melLaneTradeUntil = 0; melTradeRest = Time.time+MelDecisionRules.ReturnRest; melReturning = true;
        }
        bool MelLaneKillWindow(Combatant player,Combatant tower)
        {
            if (!player.Targetable || world.Settings.testMode || TowerRange(tower,player.Position) || self.Health < self.MaxHealth*.4f) return false;
            float distance = Vector3.Distance(self.Position,player.Position);
            float damage = distance <= self.AttackReach(player)+.5f ? player.PreviewIncomingDamage(self.AttackDamage,self,DamageType.Physical,true) : 0;
            if (self.Ready(0) && distance <= MelRules.QRange)
                damage += player.PreviewIncomingDamage(MelRules.QDamage(self.Progress.Rank(0),self.AbilityPower,true),self,DamageType.Magic);
            if (self.Ready(2) && distance <= MelRules.ERange && (player.IsRooted || player.IsStunned))
                damage += player.PreviewIncomingDamage(MelRules.EDamage(self.Progress.Rank(2),self.AbilityPower),self,DamageType.Magic);
            return damage > (player.Health+player.TotalShield+player.HealthRegeneration*2)*1.15f &&
                ThreatDamage(player,self.Position,1.5f) < (self.Health+self.TotalShield)*.6f;
        }
        void ThinkMelLane(Combatant player,Combatant tower)
        {
            bool kill = MelLaneKillWindow(player,tower);
            bool urgent = melPrepared != null && melPreparedImpact-Time.time < 1.1f;
            if (!kill && TryMelFarmVolley(player)) return;
            if (!kill && TryMelLaneLastHit(player)) return;
            if (!player.Targetable)
            {
                melCombo = MelCombo.None; melLaneTradeUntil = 0;
                if (CanSiege(tower,player)) { Plan(LaneTactic.Push,"Mel: asediar con cobertura de la oleada"); self.OrderAttack(tower); }
                else MelHoldWave(player);
                return;
            }
            if (!urgent && TryMelUltimate(player,false)) return;
            float distance = Vector3.Distance(self.Position,player.Position);
            bool closeThreat = distance < 3 && self.WasDamagedBy(player,1);
            bool inLeash = Vector3.Distance(player.Position,melLaneAnchor) <= MelDecisionRules.LaneLeash+self.AttackReach(player);
            bool canTrade = !urgent && !melReturning && Time.time >= melTradeRest && inLeash &&
                MelFarmSafe(player,self.Position) && (self.Resource > self.MaxResource*.25f || !self.Ready(0) && !self.Ready(2));
            if (melCombo != MelCombo.None && melLaneTradeUntil == 0) BeginMelLaneTrade();
            if (!kill && (melLaneTradeUntil > 0 && Time.time >= melLaneTradeUntil ||
                Vector3.Distance(self.Position,melLaneAnchor) > MelDecisionRules.LaneLeash || !inLeash || urgent))
            {
                if (melCombo != MelCombo.None || melLaneTradeUntil > 0) { melCombo = MelCombo.None; EndMelLaneTrade(); }
                MelHoldWave(player); return;
            }
            if (kill && (TryMelUltimate(player,false) || TryIgnite(player,tower) || TryMelFlash(player,tower))) return;
            if (melCombo != MelCombo.None && ContinueMelCombo(player)) return;
            if ((canTrade || kill || closeThreat) && Time.time >= melTradeRest)
            {
                if (distance < 3 && self.Health > self.MaxHealth*.6f && self.Cast(0,MelAim(player,MelRules.QSpeed)))
                { BeginMelLaneTrade(); melCombo = MelCombo.CloseSnare; Plan(LaneTactic.Trade,"Mel: intercambio corto junto a la oleada"); return; }
                bool exposed = player.IsWindingAttack || player.Blade != null && player.Blade.IsDashing;
                if (world.Settings.difficulty == Difficulty.Expert && player.Blade != null && player.Blade.IsDashing && self.Ready(2))
                {
                    Vector3 intercept = player.Blade.PredictDashPosition(MelRules.ECastTime+distance/MelRules.ESpeed);
                    if (Vector3.Distance(self.Position,intercept) <= MelRules.ERange && StartMelSnare(intercept)) return;
                }
                if (distance <= MelRules.ERange && (distance <= 6 || exposed && Time.time >= melFishAt && RollMelFishing()))
                {
                    if (StartMelSnare(MelAim(player,MelRules.ESpeed))) return;
                    if (distance <= MelRules.QRange && self.Cast(0,MelAim(player,MelRules.QSpeed)))
                    { BeginMelLaneTrade(); EndMelLaneTrade(); Plan(LaneTactic.Trade,"Mel: Q de desgaste y volver a farmear"); return; }
                }
                if (distance <= self.AttackReach(player) && self.AttackReady)
                {
                    self.HoldAttackPosition();
                    if (self.TryAttack(player)) { BeginMelLaneTrade(); Plan(LaneTactic.Trade,"Mel: básico de castigo sin abandonar la oleada"); return; }
                }
            }
            if (melLaneTradeUntil > 0) MelKite(player,false); else MelHoldWave(player);
        }
        bool TryMelLaneLastHit(Combatant player)
        {
            if (melFarm == null || !self.ValidEnemy(melFarm)) return false;
            Vector3 point = MelFarmPoint(melFarm);
            if (!MelFarmSafe(player,point)) return false;
            float distance = Vector3.Distance(self.Position,melFarm.Position);
            float hitTime = Mathf.Max(self.AttackCooldownRemaining,0)+self.AttackWindupDuration+distance/MelRules.BasicMissileSpeed;
            float hp = PredictMelMinionHealth(melFarm,hitTime);
            if (hp <= 0) { melFarm = melPrepared = null; return false; }
            if (melCombo != MelCombo.None || melLaneTradeUntil > 0) EndMelCombo();
            Plan(LaneTactic.Farm,"Mel: preparar último golpe de la oleada");
            if (distance > self.AttackReach(melFarm)) MoveOrHold(point);
            else
            {
                self.HoldAttackPosition();
                if (Time.time >= melFarmFireAt && hp <= MelBasicMinionDamage(melFarm)*MelDecisionRules.LastHitSafety(world.Settings.difficulty)) self.TryAttack(melFarm);
            }
            return true;
        }
        bool TryMelFarmVolley(Combatant player)
        {
            if (!self.Ready(0)) return false;
            float reserve = (self.Progress.Rank(2)>0?self.AbilityCost(2):0)+(self.Progress.Rank(1)>0?self.AbilityCost(1):0);
            if (self.Resource-self.AbilityCost(0) < reserve*.7f) return false;
            Combatant chosen = null; int best = 0;
            foreach (Combatant unit in world.Units)
            {
                if (unit == null || unit.Kind != UnitKind.Minion || !self.ValidEnemy(unit) || Vector3.Distance(self.Position,unit.Position) > MelRules.QRange) continue;
                float arrival = MelRules.QCastTime+Vector3.Distance(self.Position,unit.Position)/MelRules.QSpeed;
                float hp = PredictMelMinionHealth(unit,arrival), q = unit.PreviewIncomingDamage(MelRules.QDamage(self.Progress.Rank(0),self.AbilityPower,true),self,DamageType.Magic);
                if (hp <= 0 || hp > q) continue;
                bool safeAuto = MelFarmSafe(player,MelFarmPoint(unit));
                float basicAt = MelAttackArrival(unit,MelFarmPoint(unit));
                bool autoCanSave = safeAuto && basicAt <= 1.1f && PredictMelMinionHealth(unit,basicAt) > 0;
                int cluster = 0;
                foreach (Combatant other in world.Units)
                    if (other != null && other.Kind == UnitKind.Minion && self.ValidEnemy(other) && Vector3.Distance(other.Position,unit.Position) <= MelRules.QExplosionRadius+other.Radius)
                    {
                        float predicted = PredictMelMinionHealth(other,arrival);
                        if (predicted > 0 && predicted <= other.PreviewIncomingDamage(MelRules.QDamage(self.Progress.Rank(0),self.AbilityPower,true),self,DamageType.Magic)) cluster++;
                    }
                bool valuable = unit.MinionType == MinionType.Cannon || cluster >= 2 || !safeAuto;
                if (!valuable || autoCanSave && cluster < 2) continue;
                int score = cluster+(unit.MinionType == MinionType.Cannon?4:0);
                if (score > best) { best = score; chosen = unit; }
            }
            if (chosen == null || !self.Cast(0,chosen.Position)) return false;
            melCombo = MelCombo.None; melLaneTradeUntil = 0; melReturning = true;
            melFarmQPoint = chosen.Position; melFarmQAt = Time.time+MelRules.QCastTime+Vector3.Distance(self.Position,chosen.Position)/MelRules.QSpeed;
            melFarmQDamage = MelRules.QDamage(self.Progress.Rank(0),self.AbilityPower,true);
            Plan(LaneTactic.Farm,"Mel: Q para asegurar farm sin regalarse"); return true;
        }
        void MelHoldWave(Combatant player)
        {
            Vector3 point = melPrepared != null && self.ValidEnemy(melPrepared) ? MelFarmPoint(melPrepared) : melLaneAnchor;
            if (player.Targetable && !MelFarmSafe(player,point) && Vector3.Distance(player.Position,point) < 7)
                point += MelHome*2.5f;
            point = SafeMelLanePoint(point);
            if (melReturning && Vector3.Distance(self.Position,melLaneAnchor) < .8f && Time.time >= melTradeRest) melReturning = false;
            self.HoldAttackPosition(); MoveOrHold(point);
            Plan(LaneTactic.Farm,melPrepared != null ? "Mel: anticipar el próximo last hit" : "Mel: conservar posición con la oleada");
        }
    }
}
