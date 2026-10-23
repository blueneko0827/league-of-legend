using System.Collections.Generic;
using UnityEngine;

namespace MidLane
{
    // Presentation only: plays Irelia's official basic attack, passive, Q, W, E and R particle systems
    // (Riot's emitter definitions, see RiotVfx*). It observes the committed gameplay state and
    // the BladeDuelist hit events; it never changes damage, timing or targeting.
    [DefaultExecutionOrder(250)]
    public sealed class IreliaVfx : MonoBehaviour
    {
        // Animation graph clips whose particle events drive the swipes (IreliaVfx.json "events").
        const string Attack1Clip = "irelia_attack_01", Attack1EmpoweredClip = "irelia_attack_01_avar", Attack2Clip = "irelia_attack_02";
        const float AnimationFrameRate = 30;
        LaneWorld world;
        Combatant actor;
        BladeDuelist blade;
        Transform model;
        IreliaRigAnimation rig;
        readonly Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
        readonly RiotVfxPlayer[] stackGlows = new RiotVfxPlayer[4];
        readonly int[] stackGlowStyle = new int[4];
        readonly List<Pending> pending = new List<Pending>();
        readonly List<RiotVfxPlayer> swipes = new List<RiotVfxPlayer>();
        RiotVfxPlayer dash, buff;
        int swipeSequence = -1, landedSequence = -1;
        int observedBasic, observedQ, observedStacks, observedDeaths;
        Combatant lastQVictim;
        float lastQVictimAt = -100;
        SkinnedMeshRenderer bladesRenderer;
        MaterialPropertyBlock bladesBlock;
        bool bladesMaxed;
        IreliaPassiveGlow passiveGlow;
        static readonly int BaseMap = Shader.PropertyToID("_BaseMap"), MainTex = Shader.PropertyToID("_MainTex");

        struct Pending { public float at; public string system; public string bone; public float rate; }

        // W: the shield while charging, the full-charge flash once, the slash on release.
        RiotVfxPlayer wShield, wFull;
        bool observedCharging, wFullShown;
        float wChargeFraction;
        // E: one set of effects per blade pair (BladeLink) cast by this Irelia.
        int observedE;
        readonly Dictionary<BladeLink, EVfx> eLinks = new Dictionary<BladeLink, EVfx>();
        readonly List<BladeLink> eFinished = new List<BladeLink>();
        // R: the blades' missile per R wave, then LoL's wall unit and its particles per blade wall.
        int observedR;
        readonly Dictionary<BladeWave, RiotVfxPlayer> rMissiles = new Dictionary<BladeWave, RiotVfxPlayer>();
        readonly List<BladeWave> rFlown = new List<BladeWave>();
        readonly Dictionary<BladeWall, RVfx> rWalls = new Dictionary<BladeWall, RVfx>();
        readonly List<BladeWall> rEnded = new List<BladeWall>();
        // Walls already shown and ended early (Irelia died): not shown again after a quick respawn.
        readonly HashSet<BladeWall> rShown = new HashSet<BladeWall>();

        sealed class RVfx
        {
            public IreliaRBlades blades;
            public RiotVfxPlayer lines;
            public readonly List<RiotVfxPlayer> glows = new List<RiotVfxPlayer>();
            public bool hostile, glowing, formed;
            // Seconds since the detonation (game time: it waits while the game is paused).
            public float age;
            public Vector3 impact, direction;
            // The wall ended (or Irelia died): the blades sink and the particles play out.
            public void End()
            {
                if (blades != null) blades.Die();
                if (lines != null) lines.Stop();
                foreach (RiotVfxPlayer glow in glows) if (glow != null) glow.Stop();
                glows.Clear(); lines = null;
            }
            // Practice restarted: everything goes at once.
            public void Remove()
            {
                End();
                if (blades != null) Object.Destroy(blades.gameObject);
                blades = null;
            }
        }
        // Effects that end on their own time (the converging blades after they reach the far end).
        readonly List<KeyValuePair<RiotVfxPlayer, float>> timedStops = new List<KeyValuePair<RiotVfxPlayer, float>>();

        sealed class EVfx
        {
            public BladeLink link;
            public RiotVfxPlayer missile1, missile2, blades1, blades2, indicator1, indicator2, beam, sparks, converge1, converge2;
            public bool firstLanded, secondLanded, converging;
            public RiotVfxPose pose1, pose2;
            // Irelia died or Practice restarted: everything goes.
            public void Stop()
            {
                Finish();
                foreach (RiotVfxPlayer player in new[] { beam, converge1, converge2 })
                    if (player != null) player.Stop();
            }
            // The blades met: the beam plays out on its own (its delayed dark slash comes after the
            // hit) and the flying blades finish crossing to the far end.
            public void Finish()
            {
                foreach (RiotVfxPlayer player in new[] { missile1, missile2, blades1, blades2, indicator1, indicator2, sparks })
                    if (player != null) player.Stop();
            }
        }

        public void Initialize(LaneWorld lane, Combatant owner)
        {
            world = lane; actor = owner; blade = owner.Blade;
            model = transform.Find("Model");
            rig = model != null ? model.GetComponentInChildren<IreliaRigAnimation>() : null;
            passiveGlow = new IreliaPassiveGlow(model);
            observedBasic = actor.BasicAttackSequence; observedQ = blade.QSequence; observedDeaths = actor.Deaths;
            observedE = blade.ESequence; observedR = blade.RSequence;
            blade.OnHitLanded += HitLanded; blade.QLanded += QLanded; blade.WLanded += WLanded; blade.ELanded += ELanded;
            blade.RWallHit += RWallHit;
            if (actor.Audio != null) actor.Audio.Played += SoundPlayed;
            if (Enabled) RiotVfxLibrary.For(world);
        }

        void OnDestroy()
        {
            if (blade != null)
            {
                blade.OnHitLanded -= HitLanded; blade.QLanded -= QLanded; blade.WLanded -= WLanded; blade.ELanded -= ELanded;
                blade.RWallHit -= RWallHit;
            }
            if (actor != null && actor.Audio != null) actor.Audio.Played -= SoundPlayed;
            StopPersistent();
            if (passiveGlow != null) passiveGlow.Destroy();
        }

        bool Enabled { get { return world != null && world.Presentation.official_vfx; } }

        void LateUpdate()
        {
            if (actor == null || world == null || world.Paused) return;
            if (!actor.Alive || actor.Deaths != observedDeaths)
            {
                // A standing blade wall sinks as when it ends; the rest stops at once.
                foreach (KeyValuePair<BladeWall, RVfx> pair in rWalls) { pair.Value.End(); if (pair.Key != null) rShown.Add(pair.Key); }
                rWalls.Clear();
                StopPersistent(); pending.Clear(); swipes.Clear(); swipeSequence = -1; observedDeaths = actor.Deaths;
                observedBasic = actor.BasicAttackSequence; observedQ = blade.QSequence; observedStacks = 0;
                observedE = blade.ESequence; observedR = blade.RSequence; observedCharging = false;
                return;
            }
            if (!Enabled) { StopPersistent(); return; }
            ObserveBasicAttack();
            ObserveQ();
            ObservePassive();
            ObserveW();
            ObserveE();
            ObserveR();
            for (int i = timedStops.Count - 1; i >= 0; i--)
            {
                if (Time.time < timedStops[i].Value) continue;
                if (timedStops[i].Key != null) timedStops[i].Key.Stop();
                timedStops.RemoveAt(i);
            }
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                if (Time.time < pending[i].at) continue;
                Pending spawn = pending[i]; pending.RemoveAt(i);
                RiotVfxPlayer swipe = RiotVfxPlayer.Play(world, spawn.system, BonePose(spawn.bone, true, true), spawn.rate);
                if (swipe != null) swipes.Add(swipe);
            }
        }

        // Basic attack swipes, from the official animation events of the clip being played.
        void ObserveBasicAttack()
        {
            swipes.RemoveAll(player => player == null);
            // A windup cancelled before its impact (move order, Q, stun) takes its swipe with it,
            // as the interrupted animation would; a landed attack lets the swipe finish.
            if (!actor.IsWindingAttack && swipeSequence >= 0 && landedSequence != swipeSequence)
            {
                foreach (RiotVfxPlayer swipe in swipes) swipe.Stop();
                swipes.Clear(); pending.Clear(); swipeSequence = -1;
            }
            if (!actor.IsWindingAttack || actor.BasicAttackSequence == observedBasic) { if (!actor.IsWindingAttack) observedBasic = actor.BasicAttackSequence; return; }
            observedBasic = actor.BasicAttackSequence;
            swipeSequence = actor.BasicAttackSequence; swipes.Clear();
            int variant = actor.BasicAttackVariant;
            bool empowered = blade.Stacks == 4;
            // mScalePlaySpeedWithAnimation: the swipe follows the clip's retimed playback, whose
            // contact frame is fitted to the real windup (IreliaRigAnimation.Tick).
            float contact = variant == 2 ? 1f / 3 : (rig != null && rig.AttackClip != null ? .14f * rig.AttackClip.length : .3f);
            float rate = Mathf.Clamp(contact / Mathf.Max(.05f, actor.AttackWindupRemaining), .5f, 3);
            string clip = variant == 2 ? Attack2Clip : empowered ? Attack1EmpoweredClip : Attack1Clip;
            RiotVfxLibraryData data = RiotVfxLibrary.For(world).Data;
            if (data == null || data.events == null) return;
            foreach (RiotVfxEventData e in data.events)
            {
                if (e.clip != clip) continue;
                if (e.system.StartsWith("Irelia_Base_P_") && !empowered) continue;
                pending.Add(new Pending { at = Time.time + e.frame / AnimationFrameRate / rate, system = e.system, bone = e.bone,
                    rate = e.scaleWithAnimation ? rate : 1 });
            }
        }

        void ObserveQ()
        {
            if (blade.QSequence != observedQ && blade.IsDashing)
            {
                observedQ = blade.QSequence;
                if (dash != null) dash.Instance.StopBound();
                dash = RiotVfxPlayer.Play(world, "Irelia_Base_Q_Dash", DashPose, 1, model != null ? model.gameObject : null);
            }
            // The leading edge and body glow ride on Irelia only while she dashes.
            if (dash != null && !blade.IsDashing) { dash.Instance.StopBound(); dash = null; }
        }

        RiotVfxPose lastDashPose;
        RiotVfxPose DashPose()
        {
            // The effect outlives Irelia across a Practice restart: keep its last pose.
            if (actor == null || blade == null) return lastDashPose;
            Transform bone = Bone("C_BUFFBONE_GLB_LAYOUT_LOC");
            Vector3 position = bone != null ? bone.position : actor.Position;
            Vector3 direction = blade.QDirection.sqrMagnitude > .001f ? blade.QDirection : Facing() * Vector3.forward;
            lastDashPose = new RiotVfxPose(position, actor.VisualTiltRotation * Quaternion.LookRotation(Flat(direction)));
            return lastDashPose;
        }

        // W (Defiant Dance): W_DR shield around Irelia while she charges, W_FullPower when the charge
        // completes, and the slash on release (the golden empowered one after a full charge).
        void ObserveW()
        {
            bool charging = blade.IsCharging;
            if (charging && !observedCharging)
            {
                if (wShield != null) wShield.Stop();
                wShield = RiotVfxPlayer.Play(world, "Irelia_Base_W_DR", BonePose("BUFFBONE_GLB_GROUND_LOC", true), 1, null, SelfBoundSize());
                wFullShown = false;
            }
            if (charging)
            {
                wChargeFraction = blade.ChargeFraction;
                if (!wFullShown && wChargeFraction >= 1)
                {
                    wFullShown = true;
                    wFull = RiotVfxPlayer.Play(world, "Irelia_Base_W_FullPower", BonePose("BUFFBONE_GLB_GROUND_LOC", true), 1, null, SelfBoundSize());
                }
            }
            else if (observedCharging)
            {
                if (wShield != null) wShield.Stop();
                if (wFull != null) wFull.Stop();
                wShield = wFull = null;
                // Released (not cancelled by a stun or death): the slash leaves along the aim.
                if (blade.WReleaseElapsed < .25f)
                    RiotVfxPlayer.PlayAt(world, wChargeFraction >= 1 ? "Irelia_Base_W_Swipe_Empowered" : "Irelia_Base_W_Swipe",
                        actor.Position, Quaternion.LookRotation(Flat(blade.WDirection)));
            }
            observedCharging = charging;
        }

        // FlexShape bound size for effects on Irelia herself: in the footage the W shield bubble
        // (flexScale) is about 1.1 times as tall as Irelia, which a bound of her height in LoL units
        // reproduces.
        float SelfBoundSize() { return actor.VisualHeight / RiotVfxPlayer.UnitScale(world); }

        void WLanded(Combatant target)
        {
            if (!Enabled || target == null) return;
            // W_Hit is authored from the ground (its flashes sit 100 units up), facing along the slash.
            RiotVfxPlayer player = RiotVfxPlayer.PlayAt(world, "Irelia_Base_W_Hit", target.Position, Quaternion.LookRotation(Flat(blade.WDirection)));
            if (player != null) player.DepthPull = HitDepthPull(target);
        }

        // A hit absorbed while charging (the same moment as the official block sound).
        void SoundPlayed(IreliaSoundCue cue, AudioClip clip)
        {
            if (cue != IreliaSoundCue.WBlock || !Enabled || actor == null || !actor.Alive) return;
            RiotVfxPlayer.Play(world, "Irelia_Base_W_Block", BonePose("BUFFBONE_GLB_GROUND_LOC", true), 1, null, SelfBoundSize());
        }

        // E (Flawless Duet): cast swirl on Irelia (animation event of spell3 / spell3_b), each blade's
        // missile, the blade standing at its point with its ground ring, then the beam between the two
        // blades while they converge, and the hit/stun effects on the units caught.
        void ObserveE()
        {
            if (blade.ESequence != observedE)
            {
                observedE = blade.ESequence;
                RiotVfxPlayer.Play(world, blade.ESecondCast ? "Irelia_Base_E_cas_02" : "Irelia_Base_E_cas",
                    BonePose("BUFFBONE_GLB_GROUND_LOC", true, true));
            }
            foreach (BladeLink link in world.BladeLinks)
                if (link != null && link.Owner == blade && !eLinks.ContainsKey(link)) eLinks[link] = new EVfx { link = link };
            eFinished.Clear();
            foreach (KeyValuePair<BladeLink, EVfx> pair in eLinks)
            {
                // The link is destroyed at the convergence impact (or when Irelia dies / Practice restarts).
                if (pair.Key == null) { pair.Value.Finish(); eFinished.Add(pair.Key); continue; }
                UpdateE(pair.Value);
            }
            foreach (BladeLink link in eFinished) eLinks.Remove(link);
        }

        void UpdateE(EVfx e)
        {
            BladeLink link = e.link;
            float now = Time.time, flight = Mathf.Max(.01f, blade.Tuning.ePlacementTime);
            bool hostile = actor.Team != world.PlayerTeam;
            if (e.missile1 == null && !e.firstLanded)
                e.missile1 = RiotVfxPlayer.Play(world, "Irelia_Base_E_Mis_01", MissilePose(e, true));
            if (!e.firstLanded && now >= link.FirstLaunchedAt + flight)
            {
                e.firstLanded = true;
                if (e.missile1 != null) e.missile1.Stop();
                e.blades1 = RiotVfxPlayer.Play(world, "Irelia_Base_E_Blades", BladePose(e, true));
                e.indicator1 = RiotVfxPlayer.PlayAt(world, hostile ? "Irelia_Base_E_Team_Indicator_Enemy" : "Irelia_Base_E_Team_Indicator",
                    link.First, Quaternion.LookRotation(Flat(link.First - link.FirstOrigin)));
            }
            if (!link.HasSecond) return;
            if (e.missile2 == null && !e.secondLanded && now >= link.SecondLaunchedAt)
                e.missile2 = RiotVfxPlayer.Play(world, "Irelia_Base_E_Mis_01", MissilePose(e, false));
            if (!e.secondLanded && now >= link.SecondLaunchedAt + flight)
            {
                e.secondLanded = true;
                if (e.missile2 != null) e.missile2.Stop();
                e.blades2 = RiotVfxPlayer.Play(world, "Irelia_Base_E_Blades", BladePose(e, false));
                e.indicator2 = RiotVfxPlayer.PlayAt(world, hostile ? "Irelia_Base_E_Team_Indicator_Enemy" : "Irelia_Base_E_Team_Indicator",
                    link.Second, Quaternion.LookRotation(Flat(link.Second - link.SecondOrigin)));
                // The beam spans the two points (it stays there while the blades fly to the middle).
                var beamPose = new RiotVfxPose(link.First, Quaternion.LookRotation(Flat(link.Second - link.First)), link.Second);
                e.beam = RiotVfxPlayer.Play(world, hostile ? "Irelia_Base_E_Beam_Enemy" : "Irelia_Base_E_Beam", () => beamPose);
                var sparksPose = new RiotVfxPose((link.First + link.Second) * .5f, beamPose.rotation);
                e.sparks = RiotVfxPlayer.Play(world, hostile ? "Irelia_Base_E_Beam_Warning_Sparks_Enemy" : "Irelia_Base_E_Beam_Warning_Sparks",
                    () => sparksPose);
            }
            // The standing blades leave their points and fly at each other (E_Mis_02, with its shard
            // trail): they cross in the middle at the hit and fade at the far end, as in LoL.
            if (e.secondLanded && !e.converging && now >= ConvergeStart(link))
            {
                e.converging = true;
                if (e.blades1 != null) e.blades1.Stop();
                if (e.blades2 != null) e.blades2.Stop();
                e.converge1 = RiotVfxPlayer.Play(world, "Irelia_Base_E_Mis_02", ConvergePose(e, true));
                e.converge2 = RiotVfxPlayer.Play(world, "Irelia_Base_E_Mis_02", ConvergePose(e, false));
                float fade = ConvergeArrival(link) + EConvergeFade;
                if (e.converge1 != null) timedStops.Add(new KeyValuePair<RiotVfxPlayer, float>(e.converge1, fade));
                if (e.converge2 != null) timedStops.Add(new KeyValuePair<RiotVfxPlayer, float>(e.converge2, fade));
            }
        }

        // Measured on in-game footage: each blade takes about 0.14 s from its point to the other
        // one, crossing the middle when the stun lands, and its image fades just after arriving.
        const float EConvergeHalfFlight = .07f, EConvergeFade = .06f;
        static float ConvergeStart(BladeLink link) { return Mathf.Max(link.ConvergeAt, link.ImpactAt - EConvergeHalfFlight); }
        static float ConvergeArrival(BladeLink link) { return 2 * link.ImpactAt - ConvergeStart(link); }

        System.Func<RiotVfxPose> ConvergePose(EVfx e, bool first)
        {
            BladeLink link = e.link;
            Vector3 from = first ? link.First : link.Second, to = first ? link.Second : link.First;
            float start = ConvergeStart(link), end = ConvergeArrival(link);
            // In LoL the flying blades lie along the line (mesh Y forward, flat across the band),
            // not standing up: the fans sweep along the beam.
            Vector3 along = Flat(to - from);
            Quaternion facing = Quaternion.LookRotation(Vector3.Cross(Vector3.up, along), along);
            return () => new RiotVfxPose(Vector3.Lerp(from, to, (Time.time - start) / Mathf.Max(.01f, end - start)), facing);
        }

        // The blade flies from Irelia's position at the cast to its point.
        System.Func<RiotVfxPose> MissilePose(EVfx e, bool first)
        {
            return () =>
            {
                if (e.link == null) return first ? e.pose1 : e.pose2;
                BladeLink link = e.link;
                Vector3 from = first ? link.FirstOrigin : link.SecondOrigin, to = first ? link.First : link.Second;
                float launched = first ? link.FirstLaunchedAt : link.SecondLaunchedAt;
                float t = Mathf.Clamp01((Time.time - launched) / Mathf.Max(.01f, blade.Tuning.ePlacementTime));
                var pose = new RiotVfxPose(Vector3.Lerp(from, to, t), Quaternion.LookRotation(Flat(to - from)));
                if (first) e.pose1 = pose; else e.pose2 = pose;
                return pose;
            };
        }

        System.Func<RiotVfxPose> BladePose(EVfx e, bool first) { return () => BladePoseNow(e, first); }

        // Standing blade at its point (it leaves as E_Mis_02 when the pair converges).
        RiotVfxPose BladePoseNow(EVfx e, bool first)
        {
            BladeLink link = e.link;
            if (link == null) return first ? e.pose1 : e.pose2;
            Vector3 point = first ? link.First : link.Second, origin = first ? link.FirstOrigin : link.SecondOrigin;
            var pose = new RiotVfxPose(point, Quaternion.LookRotation(Flat(point - origin)));
            if (first) e.pose1 = pose; else e.pose2 = pose;
            return pose;
        }

        void ELanded(Combatant target, bool stunned)
        {
            if (!Enabled || target == null) return;
            PlayOnTarget(target.Kind == UnitKind.Champion ? "Irelia_Base_E_Hit" : "Irelia_Base_E_Hit_minion", target, target.Position - actor.Position);
            // Stunned champions (and practice dummies) flash their own silhouette.
            if (stunned && target.Kind == UnitKind.Champion)
            {
                Transform visual = target.transform.Find("Model");
                RiotVfxPlayer.Play(world, "Irelia_Base_E_hit_Avatar", TargetBonePose(target, "C_Buffbone_Glb_Chest_Loc", target.Position - actor.Position),
                    1, visual != null ? visual.gameObject : null);
            }
        }

        // R (Vanguard's Edge), as in LoL: the cast swirl (animation event of irelia_spell4), the
        // blades' missile from the end of the cast, and where it detonates on a champion the
        // explosion on that champion and, a moment later, LoL's wall unit (IreliaRBlades: the
        // blades burst out and land on the wall, stand, and sink when it ends) with the wall's
        // lines on the ground, the pulse at its root and a glow on every blade; an enemy the wall
        // hits gets the wall's hit.
        const float RMissileHeight = 100;   // LoL units above the ground the blades fly at
        // In the game footage the wall unit (its blades, the big mark triangle on the floor and the
        // lightning) starts two frames (30 fps) after the explosion.
        const float RWallUnitDelay = 2f / 30;
        // FlexShape bound size of R_Hit_Aoe, measured on the footage: its mark triangle on the floor
        // is about 900 LoL units across and centered 100 units past the impact.
        const float RHitAoeBoundSize = 220;
        void ObserveR()
        {
            if (blade.RSequence != observedR)
            {
                observedR = blade.RSequence;
                RiotVfxPlayer.Play(world, "Irelia_Base_R_cas", BonePose("BUFFBONE_GLB_GROUND_LOC", true, true));
            }
            foreach (BladeWave wave in world.BladeWaves)
            {
                if (wave == null || !wave.IsUltimate || wave.Blade != blade || !wave.Launched || rMissiles.ContainsKey(wave)) continue;
                // The missile is its own small object (bound size 1): the glow along each blade
                // (R_Mis_child, scaled by its bound object) is as long as the blade.
                RiotVfxPlayer missile = RiotVfxPlayer.Play(world, "Irelia_Base_R_Mis", RMissilePose(wave), 1, null, 1);
                if (missile != null) missile.Instance.MissileFrame = true;
                rMissiles[wave] = missile;
            }
            rFlown.Clear();
            foreach (KeyValuePair<BladeWave, RiotVfxPlayer> pair in rMissiles)
                if (pair.Key == null) { if (pair.Value != null) pair.Value.Stop(); rFlown.Add(pair.Key); }
            foreach (BladeWave wave in rFlown) rMissiles.Remove(wave);

            rShown.RemoveWhere(wall => wall == null);
            foreach (BladeWall wall in world.BladeWalls)
                if (wall != null && wall.Blade == blade && !rWalls.ContainsKey(wall) && !rShown.Contains(wall)) rWalls[wall] = StartWall(wall);
            rEnded.Clear();
            foreach (KeyValuePair<BladeWall, RVfx> pair in rWalls)
            {
                RVfx r = pair.Value;
                if (pair.Key == null) { r.End(); rEnded.Add(pair.Key); continue; }
                r.age += Time.deltaTime;
                if (!r.formed && r.age >= RWallUnitDelay) FormWall(r);
                // IreliaBlades' cast clip starts R_Wall_Bladeglow on every blade at its frame 9.
                if (!r.glowing && r.blades != null && r.blades.Age >= IreliaRBlades.BladeGlowAt)
                {
                    r.glowing = true;
                    for (int i = 1; i <= r.blades.BladeCount; i++)
                    {
                        RiotVfxPlayer glow = RiotVfxPlayer.Play(world, r.hostile ? "Irelia_Base_R_Wall_Bladeglow_Enemy" : "Irelia_Base_R_Wall_Bladeglow",
                            FollowPose(r.blades.Bone(i)));
                        if (glow != null) r.glows.Add(glow);
                    }
                }
            }
            foreach (BladeWall wall in rEnded) rWalls.Remove(wall);
        }

        // Follows a transform (a wall blade); keeps its last place once the blade is gone.
        static System.Func<RiotVfxPose> FollowPose(Transform anchor)
        {
            var last = new RiotVfxPose(anchor.position, Quaternion.identity);
            return () =>
            {
                if (anchor != null) last = new RiotVfxPose(anchor.position, Quaternion.identity);
                return last;
            };
        }

        // The blades fly along the R's path at missile height, facing the flight.
        System.Func<RiotVfxPose> RMissilePose(BladeWave wave)
        {
            Quaternion facing = Quaternion.LookRotation(Flat(wave.Direction));
            float height = RMissileHeight * RiotVfxPlayer.UnitScale(world);
            var last = new RiotVfxPose(wave.Head + Vector3.up * height, facing);
            return () =>
            {
                if (wave != null) last = new RiotVfxPose(wave.Head + Vector3.up * height, Quaternion.LookRotation(Flat(wave.Direction)));
                return last;
            };
        }

        RVfx StartWall(BladeWall wall)
        {
            // Enemy colors when the wall hurts the player's team (also a wall reflected back at her).
            var r = new RVfx { hostile = wall.OwnerTeam != world.PlayerTeam, impact = wall.Impact, direction = Flat(wall.Direction) };
            Vector3 impact = r.impact;
            Quaternion facing = Quaternion.LookRotation(r.direction);
            // The explosion on the champion the blades hit.
            Combatant target = ChampionAt(impact);
            RiotVfxPlayer explosion = RiotVfxPlayer.Play(world, "Irelia_Base_R_Mis_Tar_Break", () => new RiotVfxPose(impact, facing), 1, null,
                TargetBoundSize(target));
            if (explosion != null && target != null) explosion.DepthPull = HitDepthPull(target);
            return r;
        }

        // LoL's wall unit, RWallUnitDelay after the explosion.
        void FormWall(RVfx r)
        {
            r.formed = true;
            Vector3 impact = r.impact;
            Quaternion facing = Quaternion.LookRotation(r.direction);
            // The wall's outline and the pulse inside it trace the wall itself: drawn at gameplay
            // scale so they stay on the damage boundary (like the blades).
            r.lines = RiotVfxPlayer.Play(world, r.hostile ? "Irelia_Base_R_groundlines_enemy" : "Irelia_Base_R_groundlines",
                () => new RiotVfxPose(impact, facing));
            if (r.lines != null) r.lines.Instance.UnitScale = LeagueChampionStats.WorldUnitsPerLeagueUnit;
            r.blades = IreliaRBlades.Create(world, impact, r.direction, RiotVfxPlayer.UnitScale(world));
            if (r.blades != null)
            {
                // IreliaBlades' cast clip, frame 0: R_Hit_Aoe on Sword_Root (the mark triangle on
                // the floor, the lightning sweeping out to the wall and the streaks it throws).
                Transform root = r.blades.Bone(0);
                RiotVfxPose rootPose = new RiotVfxPose(root.position, root.rotation);
                RiotVfxPlayer pulse = RiotVfxPlayer.Play(world, "Irelia_Base_R_Hit_Aoe", () => rootPose, 1, null, RHitAoeBoundSize);
                if (pulse != null) pulse.Instance.UnitScale = LeagueChampionStats.WorldUnitsPerLeagueUnit;
            }
        }

        Combatant ChampionAt(Vector3 point)
        {
            Combatant best = null; float bestDistance = .5f;
            foreach (Combatant unit in world.Units)
            {
                if (unit == null || unit.Kind != UnitKind.Champion || !actor.ValidEnemy(unit)) continue;
                float distance = Vector3.Distance(Flat3(unit.Position), Flat3(point));
                if (distance < bestDistance) { best = unit; bestDistance = distance; }
            }
            return best;
        }

        static Vector3 Flat3(Vector3 v) { v.y = 0; return v; }

        // FlexShape bound size of a target (its height in LoL units), like SelfBoundSize for Irelia.
        float TargetBoundSize(Combatant target)
        {
            return target != null && target.VisualHeight > 0 ? target.VisualHeight / RiotVfxPlayer.UnitScale(world) : 100;
        }

        // The wall hit an enemy: the wall's hit on it (its burst, then a gold glow fading to violet).
        // The game no longer disarms (R_Wall_Disarm's sword icon is not shown in the footage).
        void RWallHit(Combatant target)
        {
            if (!Enabled || target == null) return;
            Vector3 from = actor != null ? actor.Position : target.Position;
            Quaternion facing = Quaternion.LookRotation(Flat(target.Position - from));
            float bound = TargetBoundSize(target);
            RiotVfxPlayer hit = RiotVfxPlayer.Play(world, "Irelia_Base_R_Wall_Hit", () => new RiotVfxPose(target != null ? target.Position : from, facing), 1, null, bound);
            if (hit != null) hit.DepthPull = HitDepthPull(target);
        }

        // Passive: glow on one blade per stack, the activation burst and aura at four stacks.
        void ObservePassive()
        {
            int stacks = blade.Stacks;
            if (stacks == 4 && observedStacks < 4)
            {
                if (buff != null) buff.Stop();
                // Bound to Irelia's size (FlexShape): the gold ring of the activation (RippleActivate,
                // eroding into arcs like a laurel wreath around her) is as wide as in LoL.
                buff = RiotVfxPlayer.Play(world, "Irelia_Base_P_buf", BonePose("BUFFBONE_GLB_GROUND_LOC", true), 1, model != null ? model.gameObject : null, SelfBoundSize());
                if (actor.Audio != null) actor.Audio.PassiveMaxed();
                // The hit that completed the stacks shows the 4-stack proc on its target.
                if (lastQVictim != null && Time.time - lastQVictimAt < .25f)
                {
                    PlayOnTarget("Irelia_Base_P_Hit_4stack_left", lastQVictim, blade.QDirection);
                    if (actor.Audio != null) actor.Audio.PassiveMaxedHit();
                }
            }
            if (stacks < 4 && buff != null) { buff.Stop(); buff = null; }
            SetBladesMaxed(stacks == 4);
            if (passiveGlow != null) { passiveGlow.Set(stacks == 4); passiveGlow.Tick(Time.deltaTime); }
            observedStacks = stacks;
            for (int i = 0; i < stackGlows.Length; i++)
            {
                int style = stacks == 4 ? 4 : i < stacks ? 1 : 0;
                if (style == stackGlowStyle[i] && (style == 0 || stackGlows[i] != null)) continue;
                if (stackGlows[i] != null) stackGlows[i].Stop();
                stackGlows[i] = null; stackGlowStyle[i] = style;
                if (style == 0) continue;
                string bone = "Sword" + (i + 1) + "_Skn";
                stackGlows[i] = RiotVfxPlayer.Play(world, style == 4 ? "Irelia_Base_P_Blades_Stacks_4" : "Irelia_Base_P_Blades_Stacks_1_2_3",
                    BonePose(bone, false));
            }
        }

        void HitLanded(Combatant target, bool empowered, bool critical, bool fromQ)
        {
            if (!Enabled || target == null) return;
            Vector3 direction = target.Position - actor.Position;
            if (!fromQ)
            {
                landedSequence = actor.BasicAttackSequence;
                string system = critical ? "Irelia_Base_BA_Hit_Crit" : actor.BasicAttackVariant == 2 ? "Irelia_Base_BA_Hit_02" : "Irelia_Base_BA_Hit";
                PlayOnTarget(system, target, direction);
            }
            if (empowered)
            {
                PlayOnTarget(actor.BasicAttackVariant == 2 && !fromQ ? "Irelia_Base_P_Hit_04" : "Irelia_Base_P_Hit_02", target, direction);
                if (actor.Audio != null) actor.Audio.PassiveEmpoweredHit();
            }
        }

        void QLanded(Combatant target, bool refunded)
        {
            if (!Enabled || target == null) return;
            lastQVictim = target; lastQVictimAt = Time.time;
            PlayOnTarget("Irelia_Base_Q_tar", target, blade.QDirection);
            RiotVfxPlayer.Play(world, "Irelia_Base_Q_heal", BonePose("C_BUFFBONE_GLB_CENTER_LOC", true));
            // A marked target or a kill refunds Q: the mark bursts above the target.
            if (refunded) RiotVfxPlayer.Play(world, "Irelia_Base_Q_Proc", TargetBonePose(target, "C_Buffbone_Glb_Overhead_Loc", blade.QDirection));
        }

        // Attack, passive and Q hits all burst at the target's chest bone (mHitBoneName), as in
        // LoL: the passive flashes are authored like BA_Hit (flat quads around the hit point; the
        // 80-unit emitter offset is overridden by its curve, which stays at 0 height). Anchored at
        // the feet they were hidden under the dummy and its HUD.
        void PlayOnTarget(string system, Combatant target, Vector3 direction)
        {
            RiotVfxPlayer player = RiotVfxPlayer.Play(world, system, TargetBonePose(target, "C_Buffbone_Glb_Chest_Loc", direction));
            if (player != null) player.DepthPull = HitDepthPull(target);
        }

        // The burst is centred inside the target's body; pull it in front so the body does not
        // hide half of it (LoL draws its hit flashes over the target).
        public static float HitDepthPull(Combatant target)
        {
            if (target == null || target.SelectionHeight <= 0) return 0;
            return Mathf.Clamp(target.Radius * target.VisualHeight / target.SelectionHeight * .75f, .15f, 1.2f);
        }

        // Hit effects use the target's official bone and face along the attack. Without the bone,
        // the point falls back to a fraction of the target's visual height.
        public static System.Func<RiotVfxPose> TargetBonePose(Combatant target, string boneName, Vector3 direction, float fallbackHeight = .55f)
        {
            Transform bone = RiotVfxPlayer.FindBone(target != null ? target.transform : null, boneName);
            Quaternion facing = Quaternion.LookRotation(Flat(direction));
            Vector3 last = target != null ? target.Position + Vector3.up * target.VisualHeight * fallbackHeight : Vector3.zero;
            return () =>
            {
                // The target's appearance can switch (dummy <-> minion model): follow the visible rig.
                if (target != null && (bone == null || !bone.gameObject.activeInHierarchy))
                    bone = RiotVfxPlayer.FindBone(target.transform, boneName);
                if (bone != null) last = bone.position;
                else if (target != null) last = target.Position + Vector3.up * target.VisualHeight * fallbackHeight;
                return new RiotVfxPose(last, facing);
            };
        }

        // Character-attached effects: bone position. Buff bones take the character's facing and
        // camera tilt; the floating blades keep their own animated orientation.
        // boneFrame: animation-event particles live in the bone's frame, and Riot's root-level
        // buff bones (C_Buffbone_Glb_Layout_Loc, Buffbone_Glb_Ground_Loc...) are yawed 180°
        // in irelia_base.skl, which the swipes' own 180° birth rotation undoes.
        System.Func<RiotVfxPose> BonePose(string boneName, bool facing, bool boneFrame = false)
        {
            Transform bone = Bone(boneName);
            Quaternion yaw = boneFrame ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
            var last = new RiotVfxPose(actor.Position, Facing() * yaw);
            return () =>
            {
                if (actor == null) return last;
                if (bone != null) last = new RiotVfxPose(bone.position, facing ? Facing() * yaw : bone.rotation);
                else last = new RiotVfxPose(actor.Position, Facing() * yaw);
                return last;
            };
        }

        // The official blades material (Irelia_Blades_Passive_inst) switches its diffuse to the
        // cream/bronze texture while the IreliaPassiveStacks buff is at 4, which makes the fanned
        // floating blades stand out. A property block keeps the shared Material2 (E/R blades) intact.
        void SetBladesMaxed(bool maxed)
        {
            if (maxed == bladesMaxed) return;
            if (bladesRenderer == null && model != null)
                foreach (SkinnedMeshRenderer renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    if (renderer.name == "AnimatedBlades") { bladesRenderer = renderer; break; }
            Texture2D texture = maxed ? Resources.Load<Texture2D>(IreliaGlbAssets.Root + "BladesPassive4") : null;
            if (bladesRenderer == null || (maxed && texture == null)) return;
            bladesMaxed = maxed;
            if (bladesBlock == null) bladesBlock = new MaterialPropertyBlock();
            bladesRenderer.GetPropertyBlock(bladesBlock);
            if (maxed) { bladesBlock.SetTexture(BaseMap, texture); bladesBlock.SetTexture(MainTex, texture); }
            else bladesBlock.Clear();
            bladesRenderer.SetPropertyBlock(bladesBlock);
        }

        Quaternion Facing()
        {
            Transform source = model != null ? model : actor.transform;
            return actor.VisualTiltRotation * Quaternion.LookRotation(Flat(source.forward));
        }

        Transform Bone(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            Transform bone;
            if (!bones.TryGetValue(name, out bone)) bones[name] = bone = RiotVfxPlayer.FindBone(model, name);
            return bone;
        }

        static Vector3 Flat(Vector3 direction)
        {
            direction.y = 0;
            return direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.forward;
        }

        void StopPersistent()
        {
            for (int i = 0; i < stackGlows.Length; i++) { if (stackGlows[i] != null) stackGlows[i].Stop(); stackGlows[i] = null; stackGlowStyle[i] = 0; }
            SetBladesMaxed(false);
            if (passiveGlow != null) passiveGlow.Clear();
            if (wShield != null) wShield.Stop();
            if (wFull != null) wFull.Stop();
            wShield = wFull = null;
            foreach (EVfx e in eLinks.Values) e.Stop();
            eLinks.Clear();
            foreach (RiotVfxPlayer missile in rMissiles.Values) if (missile != null) missile.Stop();
            rMissiles.Clear();
            foreach (RVfx wall in rWalls.Values) wall.Remove();
            rWalls.Clear();
            if (buff != null) buff.Stop();
            buff = null;
            if (dash != null) dash.Instance.StopBound();
            dash = null;
            foreach (KeyValuePair<RiotVfxPlayer, float> stop in timedStops) if (stop.Key != null) stop.Key.Stop();
            timedStops.Clear();
        }
    }
}
