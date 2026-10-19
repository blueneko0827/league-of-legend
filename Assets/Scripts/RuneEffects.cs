using System.Collections.Generic;
using UnityEngine;

namespace MidLane
{
    // Per-champion state. Virtual farm is never incremented by actual kills, CS or spell hits.
    public sealed class RuneEffects
    {
        readonly LaneWorld world;
        readonly Combatant self;
        public readonly IreliaRunePage Page;
        public bool Enabled = true;
        public bool Has(Rune rune) { return Enabled && self.Dummy==null && RuneRules.Has(self.Archetype,Page,rune); }
        public int LegendStacks { get; private set; }
        public int ManaflowStacks { get; private set; }
        public int TreasureAwarded { get; private set; }
        public bool? FinalTreasureReward { get; private set; }
        public bool SkillElixirConsumed { get; internal set; }
        int progressLevel=-1; bool progressHalf;
        int conqueror; float conquerorUntil;
        float presenceReady, scorchReady, cometReady, suddenReady, suddenUntil, unflinchingUntil;
        float boneReady, boneUntil; int boneCharges; Combatant boneSource;
        float secondWindUntil, manaTick;
        float shieldBashAmount, shieldBashUntil;
        float avariceUntil, forceUntil; bool avariceOwesGold;
        readonly HashSet<int> grantedElixirs=new HashSet<int>();
        readonly HashSet<int> pendingElixirs=new HashSet<int>();
        readonly Dictionary<int,float> conquerorCasts=new Dictionary<int,float>();
        readonly List<PendingHit> pending=new List<PendingHit>();
        readonly List<float> takedownHeals=new List<float>();
        sealed class PendingHit
        {
            public Combatant Target; public int Life; public float At, Raw, StartAt;
            public DamageType Type; public string Label; public bool Area;
            public Vector3 Point, Start; public GameObject Visual;
        }
        public RuneEffects(LaneWorld lane, Combatant owner)
        { world=lane; self=owner; Page=lane.SelectedRunePage; manaTick=Time.time+5; }
        public int ConquerorStacks { get { return Has(Rune.Conqueror)&&Time.time<conquerorUntil?conqueror:0; } }
        public float BoneCooldown { get { return Mathf.Max(0,boneReady-Time.time); } }
        public float ScorchCooldown { get { return Mathf.Max(0,scorchReady-Time.time); } }
        public float CometCooldown { get { return Mathf.Max(0,cometReady-Time.time); } }
        public bool AvariceActive { get { return Has(Rune.TripleTonic)&&Time.time<avariceUntil; } }
        public bool ForceActive { get { return Has(Rune.TripleTonic)&&Time.time<forceUntil; } }
        bool Supported { get { return Enabled && self.Dummy==null && self.Archetype!=Archetype.Siphon; } }
        bool AdaptiveIsAP
        {
            get
            {
                float ad=0,ap=0;
                if(self.Inventory!=null) foreach(var item in self.Inventory.Items) { ad+=item.damage; ap+=item.power; }
                if(self.Inventory!=null && self.Inventory.Has(3053)) ad+=self.BaseStats.AttackDamage.At(self.Level)*.45f;
                return ap>ad || ap==ad && self.Archetype==Archetype.Prism;
            }
        }
        public float BonusHealth { get { return !Supported?0:self.Archetype==Archetype.Blade?65:10*self.Level; } }
        public float BonusMana { get { return Has(Rune.Manaflow)?ManaflowStacks*25:0; } }
        public float BonusAttackSpeed { get { return !Supported?0:.10f+(Has(Rune.Alacrity)?.03f+.015f*LegendStacks:0); } }
        public int JackStacks
        {
            get
            {
                if(!Has(Rune.JackOfAllTrades)||self.Inventory==null) return 0;
                int signature=17;
                unchecked { foreach(var item in self.Inventory.Items) signature=signature*31+item.id; }
                if(signature==jackSignature) return cachedJackStacks;
                jackSignature=signature;
                int mask=0;
                foreach(var i in self.Inventory.Items)
                {
                    float[] stats={i.health,i.mana,i.damage+(i.id==3053?1:0),i.power,i.armor,i.resist,i.attackSpeed,
                        i.movement,i.movementPercent,i.haste,i.lifeSteal,i.crit,i.tenacity,i.hpRegen+i.hpRegenPercent,
                        i.mpRegen+i.mpRegenPercent,i.penetration,i.magicPenetration,i.armorPenetrationPercent,i.magicPenetrationPercent,i.omnivamp};
                    for(int j=0;j<stats.Length;j++) if(stats[j]>0) mask|=1<<j;
                }
                int count=0; for(;mask!=0;mask&=mask-1) count++; cachedJackStacks=count; return count;
            }
        }
        int jackSignature=int.MinValue, cachedJackStacks;
        float AdaptiveForce { get { int jack=JackStacks; return !Supported?0:9+(jack>=10?20:jack>=5?8:0)+
            (ForceActive?25:0)+ConquerorStacks*RuneRules.Scale(self.Level,1.8f,4); } }
        public float BonusAD { get { return AdaptiveIsAP?0:AdaptiveForce*.6f; } }
        public float BonusAP { get { return AdaptiveIsAP?AdaptiveForce:0; } }
        public float AbilityHaste { get { return JackStacks+(Has(Rune.Transcendence)?(self.Level>=8?10:self.Level>=5?5:0):0); } }
        public float BasicAbilityHaste { get { return Has(Rune.LegendHaste)?1.5f*LegendStacks:0; } }
        public float Resistance { get { return Has(Rune.Unflinching)&&Time.time<unflinchingUntil?10:0; } }

        public void SynchronizeProgress()
        {
            if(!Supported || world.Paused || !world.ProgressionEnabled) return;
            int level=self.Level; bool half=world.RuneHalfLevel;
            if(progressLevel!=level || progressHalf!=half)
            {
                progressLevel=level; progressHalf=half;
                LegendStacks=RuneRules.LegendStacks(level,half);
                int previousManaStacks=ManaflowStacks;
                ManaflowStacks=RuneRules.ManaflowStacks(level,half);
                if(previousManaStacks<10&&ManaflowStacks==10) manaTick=Time.time+5;
                self.RefreshRuneStats();
            }
            if(Has(Rune.TreasureHunter))
            {
                if(level>=18 && !FinalTreasureReward.HasValue) FinalTreasureReward=Random.value<.5f;
                int gold=RuneRules.TreasureGold(level,FinalTreasureReward==true);
                if(gold>TreasureAwarded) { self.GrantGold(gold-TreasureAwarded); TreasureAwarded=gold; }
            }
            if(Has(Rune.TripleTonic)&&!world.PracticeModeActive)
            {
                if(level>=3) OfferElixir(2151); if(level>=6) OfferElixir(2152); if(level>=9) OfferElixir(2150);
                DeliverElixirs();
            }
        }
        void OfferElixir(int id) { if(grantedElixirs.Add(id)) pendingElixirs.Add(id); }
        void DeliverElixirs()
        {
            foreach(int id in new[]{2151,2152,2150})
            {
                if(!pendingElixirs.Contains(id)) continue;
                if(self.Inventory.Items.Count>=6)
                {
                    // All three elixirs are manual in this simulator; wait for a free slot.
                    continue;
                }
                var item=self.Inventory.Get(id);
                if(item!=null) { self.Inventory.Items.Add(item); pendingElixirs.Remove(id); }
            }
        }
        public bool UseElixir(int id)
        {
            if(!Has(Rune.TripleTonic)||world.PracticeModeActive||!self.Alive||world.Paused || !self.Inventory.Has(id)) return false;
            if(id!=2150&&id!=2151&&id!=2152) return false;
            if(id==2151 && !world.ProgressionEnabled) return false;
            if(id==2150 && !self.GrantRuneSkillPoint()) return false;
            self.Inventory.Items.RemoveAt(self.Inventory.Items.FindIndex(x=>x.id==id));
            float until=world.PvpModeActive?float.PositiveInfinity:Time.time+60;
            if(id==2151)
            {
                avariceUntil=until; avariceOwesGold=!world.PvpModeActive;
                if(world.PvpModeActive) self.GrantGold(60);
            }
            if(id==2152) forceUntil=until;
            return true;
        }
        public void NotifyDash() { if(Has(Rune.SuddenImpact)) suddenUntil=Time.time+4; }
        public void NotifyCrowdControl(float duration) { if(Has(Rune.Unflinching)) unflinchingUntil=Mathf.Max(unflinchingUntil,Time.time+duration+2); }
        public void NotifyShield(float amount, float duration)
        {
            if(!Has(Rune.ShieldBash)||amount<=0) return;
            if(Time.time>=shieldBashUntil) shieldBashAmount=0;
            shieldBashAmount=Mathf.Max(shieldBashAmount,amount);
            shieldBashUntil=Mathf.Max(shieldBashUntil,Time.time+duration+2);
        }
        public float DamageMultiplier(Combatant target, DamageType type)
        {
            if(type==DamageType.True||target.Kind!=UnitKind.Champion) return 1;
            float mult=1;
            if(Has(Rune.LastStand)&&self.Health<self.MaxHealth*.6f)
                mult*=1+Mathf.Lerp(.05f,.11f,Mathf.InverseLerp(.6f,.3f,self.Health/self.MaxHealth));
            if(Has(Rune.CutDown)&&target.Health>target.MaxHealth*.6f) mult*=1.08f;
            return mult;
        }
        public float ReduceIncoming(float damage, Combatant source, bool basic, bool ability, int castId)
        {
            if(!Has(Rune.BonePlating)||source.Kind!=UnitKind.Champion||source!=boneSource||Time.time>=boneUntil||boneCharges<=0||(!basic&&!ability)) return damage;
            if(!basic && !boneCasts.Add(castId)) return damage;
            boneCharges--; return Mathf.Max(0,damage-RuneRules.Scale(self.Level,30,60));
        }
        readonly HashSet<int> boneCasts=new HashSet<int>();
        public void OnReceivedDamage(Combatant source, float healthDamage)
        {
            if(source.Kind!=UnitKind.Champion||healthDamage<=0) return;
            if(Has(Rune.SecondWind)) secondWindUntil=Time.time+10;
            if(Has(Rune.BonePlating)&&Time.time>=boneReady)
            { boneSource=source; boneCharges=3; boneUntil=Time.time+1.5f; boneReady=Time.time+55; boneCasts.Clear(); }
        }
        public void OnOutgoingHit(Combatant target, float damage, bool basic, bool ability, int castId, bool runeDamage)
        {
            if(target.Kind!=UnitKind.Champion||damage<=0) return;
            if(ConquerorStacks==12) self.Heal(damage*(self.Archetype==Archetype.Prism?.05f:.08f));
            if(runeDamage) return;
            if(Has(Rune.PresenceOfMind)&&Time.time>=presenceReady)
            { presenceReady=Time.time+8; self.RestoreResource(RuneRules.Scale(self.Level,6,50)*(self.Archetype==Archetype.Prism?.8f:1)); }
            if(!basic&&!ability) return;
            if(Has(Rune.Conqueror))
            {
                float next; bool valid=basic||!conquerorCasts.TryGetValue(castId,out next)||Time.time>=next;
                if(valid)
                {
                    if(!basic) conquerorCasts[castId]=Time.time+4;
                    if(Time.time>=conquerorUntil) conqueror=0;
                    conqueror=Mathf.Min(12,conqueror+(basic&&self.Archetype==Archetype.Prism?1:2));
                }
                conquerorUntil=Time.time+5;
            }
            if(ability&&Has(Rune.Scorch)&&Time.time>=scorchReady)
            { scorchReady=Time.time+10; Queue(target,RuneRules.Scale(self.Level,20,40),DamageType.Magic,1,"Scorch"); }
            if(ability&&Has(Rune.Comet)&&Time.time>=cometReady)
            {
                cometReady=Time.time+RuneRules.Scale(self.Level,20,8);
                float distance=Vector3.Distance(self.Position,target.Position);
                float raw=(RuneRules.Scale(self.Level,15,100)+.1f*Mathf.Max(0,self.AttackDamage-self.BaseStats.AttackDamage.At(self.Level))+.05f*self.AbilityPower)*(1+Mathf.Clamp01(distance/7.5f));
                DamageType type=.1f*Mathf.Max(0,self.AttackDamage-self.BaseStats.AttackDamage.At(self.Level))>.05f*self.AbilityPower?DamageType.Physical:DamageType.Magic;
                var hit=Queue(target,raw,type,.825f,"Arcane Comet"); hit.Area=true; hit.Point=target.Position; hit.Start=self.Position+Vector3.up;
                hit.Visual=new GameObject("Arcane Comet"); hit.Visual.transform.SetParent(self.transform.parent);
                world.Shape("Comet",PrimitiveType.Sphere,hit.Visual.transform,Vector3.zero,Vector3.one*.3f,new Color(.4f,.6f,1));
            }
            if(Has(Rune.SuddenImpact)&&Time.time<suddenUntil&&Time.time>=suddenReady)
            { suddenReady=Time.time+10; suddenUntil=0; Queue(target,RuneRules.Scale(self.Level,20,80),DamageType.True,0,"Sudden Impact"); }
            if(basic&&Has(Rune.ShieldBash)&&shieldBashAmount>0&&Time.time<shieldBashUntil)
            {
                Queue(target,RuneRules.Scale(self.Level,5,30)+.025f*Mathf.Max(0,self.MaxHealth-self.BaseStats.Health.At(self.Level))+.15f*shieldBashAmount,
                    AdaptiveIsAP?DamageType.Magic:DamageType.Physical,0,"Shield Bash"); shieldBashAmount=0;
            }
        }
        PendingHit Queue(Combatant target,float raw,DamageType type,float delay,string label)
        { var hit=new PendingHit { Target=target,Life=target.Deaths,Raw=raw,Type=type,At=Time.time+delay,StartAt=Time.time,Label=label }; pending.Add(hit); return hit; }
        public void OnTakedown()
        {
            if(Has(Rune.PresenceOfMind)||Has(Rune.Triumph)) takedownHeals.Add(Time.time+1);
            if(Has(Rune.Transcendence)&&self.Level>=11)
                for(int slot=0;slot<3;slot++) self.Cooldowns.Start(slot,Time.time,self.Cooldowns.Remaining(slot,Time.time)*.8f);
        }
        public void ResetCooldowns()
        { presenceReady=scorchReady=cometReady=suddenReady=boneReady=0; boneCharges=0; boneSource=null; boneCasts.Clear(); }
        public void OnAnyChampionDeath()
        {
            ResetCooldowns();
            if(world.PvpModeActive) { avariceUntil=forceUntil=0; avariceOwesGold=false; }
        }
        public void OnDeath()
        { conqueror=0; conquerorUntil=unflinchingUntil=secondWindUntil=shieldBashUntil=shieldBashAmount=0; conquerorCasts.Clear(); }
        public void Tick(float dt)
        {
            if(!Enabled||world.Paused||world.RoundOver) return;
            SynchronizeProgress();
            if(avariceOwesGold&&Time.time>=avariceUntil&&world.ProgressionEnabled)
            { self.GrantGold(60); avariceOwesGold=false; }
            if(self.Alive)
            {
                if(Has(Rune.SecondWind)&&Time.time<secondWindUntil) self.Heal((self.MaxHealth-self.Health)*.004f*dt);
                if(Has(Rune.Manaflow)&&ManaflowStacks==10&&Time.time>=manaTick)
                { self.RestoreResource((self.MaxResource-self.Resource)*.01f); manaTick=Time.time+5; }
                else if(ManaflowStacks<10) manaTick=Time.time+5;
            }
            for(int i=takedownHeals.Count-1;i>=0;i--)
            {
                if(Time.time<takedownHeals[i]) continue;
                takedownHeals.RemoveAt(i);
                if(Has(Rune.PresenceOfMind)&&self.Alive) self.RestoreResource(self.MaxResource*.15f);
                if(Has(Rune.Triumph))
                {
                    if(self.Alive) self.Heal(self.MaxHealth*.025f+(self.MaxHealth-self.Health)*.05f);
                    if(world.ProgressionEnabled) self.GrantGold(20);
                }
            }
            // Remove before resolving: a proc can kill and reset rune state without mutating this iteration.
            for(int i=pending.Count-1;i>=0;i--)
            {
                var hit=pending[i];
                if(hit.Visual!=null) hit.Visual.transform.position=Vector3.Lerp(hit.Start,hit.Point,Mathf.InverseLerp(hit.StartAt,hit.At,Time.time))+Vector3.up*Mathf.Sin(Mathf.InverseLerp(hit.StartAt,hit.At,Time.time)*Mathf.PI)*1.5f;
                if(Time.time<hit.At) continue;
                pending.RemoveAt(i); if(hit.Visual!=null) Object.Destroy(hit.Visual);
                if(hit.Area)
                {
                    foreach(var target in world.Units.ToArray())
                        if(self.ValidEnemy(target)&&target.Kind!=UnitKind.Tower && Vector3.Distance(hit.Point,target.Position)<=1.4f+target.Radius)
                        {
                            if(target.Mel!=null&&target.Mel.ReflectTargeted(self,hit.Raw,hit.Type)) continue;
                            target.TakeDamage(hit.Raw,self,hit.Type,hitLabel:hit.Label,runeDamage:true);
                        }
                }
                else if(hit.Target!=null&&hit.Target.Deaths==hit.Life&&self.ValidEnemy(hit.Target))
                    hit.Target.TakeDamage(hit.Raw,self,hit.Type,hitLabel:hit.Label,runeDamage:true);
            }
        }
    }
}
