using System;
using System.Collections.Generic;
using UnityEngine;

namespace MidLane
{
    public enum IreliaSoundCue
    {
        Attack1Cast, Attack2Cast, Attack1Hit, Attack2Hit, QCast, QDash, QHit,
        WCharge, WCharged, WRelease, WHit, WBlock,
        EFirst, ESecond, ELaunch, EConverge, EHit, EHitMinion,
        RCast, RLaunch, RHit, RHitMinion, RWallHit,
        // Hito 2.1: critical hit, passive empowered hits and the 4-stack activation.
        CritHit, PSparkles, PMaxSparkles, PMax,
        // Hito 2.2: an E blade landing at its point and its standing glow.
        EBladeLand, ESparkles
    }

    // Hito 2.3: Irelia's official English voice (Tools/Import-IreliaVoice.py). Each name is the
    // WAV prefix in Audio/IreliaVoice; *Max are the lines of the passive at 4 stacks.
    public enum IreliaVoiceCue
    {
        RCast, BasicAttack, BasicAttackMax, Q, QMax, QMaxA, QMaxB, QMaxC, WMax, W2Max, E,
        MoveFirst, Move, MoveLong, AttackOrder, BuyBFSword, BuyRuinedKing, BuyTitanicHydra, BuyTrinityForce,
        Kill, Death, Respawn
    }

    // Called by committed gameplay actions; animation events cannot create fake impacts.
    public sealed class IreliaAudio : MonoBehaviour
    {
        public const string ResourceRoot = "Audio/Irelia/";
        public const int VoiceLimit = 10;
        public const float RVoiceChance = .3f;
        public const string RVoiceResourceRoot = "Audio/IreliaVoice/";
        // Voice lines that are not spells (basic attack efforts, move/attack order lines) wait for
        // this much voice silence and then play with this chance. LoL does not document its rate;
        // its gameplay footage has no such lines while Irelia keeps casting.
        public const float ChatterQuietSeconds = 8, ChatterChance = .35f;
        // LoL plays its long move lines when leaving base with a click beyond 900 units.
        public const float LongMoveDistance = 900 * LeagueChampionStats.WorldUnitsPerLeagueUnit;
        // Stop_sfx_Irelia_IreliaR_onmissilelaunch: the launch sound fades out over 1 s (linear)
        // when the R missile ends.
        public const float RLaunchFadeSeconds = 1;
        const float SfxVolume = .45f;
        // irelia_base_vo_events.bnk: each line's container picks shuffled, never repeating the last
        // N lines (wAvoidRepeatCount). Order follows IreliaVoiceCue; QMaxA-C are a sequence.
        static readonly int[] VoiceAvoidRepeat = { 6, 2, 2, 2, 2, 0, 0, 0, 2, 2, 3, 4, 49, 2, 39, 2, 3, 2, 3, 6, 6, 4 };
        const int VoicePriority = 80, DeathVoicePriority = 85;
        public event Action<AudioClip> RVoicePlayed;
        public event Action<IreliaVoiceCue, AudioClip> VoicePlayed;
        AudioSource voiceSource;
        readonly Dictionary<IreliaVoiceCue, VoiceGroup> voiceGroups = new Dictionary<IreliaVoiceCue, VoiceGroup>();
        System.Random voiceRandom = new System.Random();
        int voicePriority, qMaxStep, qMaxSet;
        IreliaVoiceCue voiceCue;
        float voiceQuietSince = float.NegativeInfinity;
        bool firstMoveSpoken, passiveWasMax;
        public event Action<IreliaSoundCue, AudioClip> Played;
        public bool SourcesPaused { get; private set; }
        public int ClipCount { get; private set; }
        public bool WChargeActive { get; private set; }
        public bool VoiceActive { get { return voiceSource != null && voiceSource.isPlaying; } }
        readonly Dictionary<IreliaSoundCue, AudioClip[]> clips = new Dictionary<IreliaSoundCue, AudioClip[]>();
        readonly int[] variations = new int[Enum.GetValues(typeof(IreliaSoundCue)).Length];
        readonly AudioSource[] voices = new AudioSource[VoiceLimit];
        readonly long[] started = new long[VoiceLimit];
        LaneWorld world;
        Combatant actor;
        long playOrder;
        float wBlockAt = -100, rWallHitAt = -100;
        AudioSource rLaunchSource;
        float rLaunchFade = -1;

        public void Initialize(LaneWorld lane, Combatant owner)
        {
            world = lane; actor = owner;
            var voiceRoot = new GameObject("Irelia voice"); voiceRoot.transform.SetParent(transform, false);
            voiceSource = voiceRoot.AddComponent<AudioSource>(); voiceSource.playOnAwake = false;
            voiceSource.spatialBlend = 0; voiceSource.volume = .7f; voiceSource.priority = 32;
            var lines = Resources.LoadAll<AudioClip>(RVoiceResourceRoot);
            foreach (IreliaVoiceCue cue in Enum.GetValues(typeof(IreliaVoiceCue)))
            {
                var group = Array.FindAll(lines, clip => clip.name.StartsWith(cue + "_", StringComparison.Ordinal));
                // Numeric order: the passive-max Q lines are a sequence (QMaxA_1..4).
                Array.Sort(group, (a, b) => VoiceIndex(a.name).CompareTo(VoiceIndex(b.name)));
                voiceGroups[cue] = new VoiceGroup(group, VoiceAvoidRepeat[(int)cue]);
            }
            var all = Resources.LoadAll<AudioClip>(ResourceRoot);
            Array.Sort(all, (a, b) => string.CompareOrdinal(a.name, b.name)); ClipCount = all.Length;
            foreach (IreliaSoundCue cue in Enum.GetValues(typeof(IreliaSoundCue)))
                clips[cue] = Array.FindAll(all, clip => clip.name.StartsWith(cue + "_", StringComparison.Ordinal));
            for (int i = 0; i < voices.Length; i++)
            {
                var child = new GameObject("Irelia SFX voice " + i); child.transform.SetParent(transform, false);
                var source = child.AddComponent<AudioSource>(); source.playOnAwake = false; source.loop = false;
                source.spatialBlend = 0; source.dopplerLevel = 0; source.volume = SfxVolume;
                source.priority = 64; source.ignoreListenerPause = false; voices[i] = source;
            }
        }
        static int VoiceIndex(string name)
        {
            int value; return int.TryParse(name.Substring(name.LastIndexOf('_') + 1), out value) ? value : 0;
        }
        public int VoiceCount(IreliaVoiceCue cue) { VoiceGroup group; return voiceGroups.TryGetValue(cue, out group) ? group.Clips.Length : 0; }
        bool PassiveMax { get { return actor != null && actor.Stacks >= 4; } }
        public void BasicCast(int variant)
        {
            Play(variant == 2 ? IreliaSoundCue.Attack2Cast : IreliaSoundCue.Attack1Cast);
            // IreliaBasicAttack/BasicAttack2/CritAttack_cast3D share one effort container.
            if (ChatterReady()) Speak(PassiveMax ? IreliaVoiceCue.BasicAttackMax : IreliaVoiceCue.BasicAttack);
        }
        public void BasicHit(int variant, bool critical = false)
        { Play(critical && HasClips(IreliaSoundCue.CritHit) ? IreliaSoundCue.CritHit : variant == 2 ? IreliaSoundCue.Attack2Hit : IreliaSoundCue.Attack1Hit); }
        // Official passive layers (Irelia_Base_P_Hit_* and P_buf sound events).
        public void PassiveEmpoweredHit() { Play(IreliaSoundCue.PSparkles); }
        public void PassiveMaxedHit() { Play(IreliaSoundCue.PMaxSparkles); }
        public void PassiveMaxed() { Play(IreliaSoundCue.PMax); }
        bool HasClips(IreliaSoundCue cue) { AudioClip[] group; return clips.TryGetValue(cue, out group) && group.Length > 0; }
        public void QStart() { Play(IreliaSoundCue.QCast); Play(IreliaSoundCue.QDash); SpeakQ(); }
        public void QImpact() { Play(IreliaSoundCue.QHit); }
        public void WStart()
        {
            WChargeActive = true; Play(IreliaSoundCue.WCharge, true);
            // W and W2 cast3D only hold lines for the passive at 4 stacks.
            if (PassiveMax) Speak(IreliaVoiceCue.WMax);
        }
        public void WFullyCharged() { Play(IreliaSoundCue.WCharged); }
        public void WRelease()
        {
            StopWCharge(); Play(IreliaSoundCue.WRelease);
            if (PassiveMax) Speak(IreliaVoiceCue.W2Max);
        }
        public void StopWCharge()
        {
            WChargeActive = false; if (voices[VoiceLimit - 1] != null) voices[VoiceLimit - 1].Stop();
            // The charge line ends with the charge (in LoL footage it is cut at the release).
            if (voiceSource != null && voiceCue == IreliaVoiceCue.WMax && voiceSource.isPlaying) voiceSource.Stop();
        }
        public void WImpact() { Play(IreliaSoundCue.WHit); }
        public void WBlock()
        {
            if (Time.time < wBlockAt + .12f) return;
            wBlockAt = Time.time; Play(IreliaSoundCue.WBlock);
        }
        public void EPlace(bool second)
        {
            Play(second ? IreliaSoundCue.ESecond : IreliaSoundCue.EFirst); Play(IreliaSoundCue.ELaunch);
            // IreliaE_cast3D and IreliaE2_cast3D share one effort container.
            Speak(IreliaVoiceCue.E);
        }
        public void EConverging() { Play(IreliaSoundCue.EConverge); }
        public void EBladeLanded() { Play(IreliaSoundCue.EBladeLand); Play(IreliaSoundCue.ESparkles); }
        public void EImpact(bool champion) { Play(champion ? IreliaSoundCue.EHit : IreliaSoundCue.EHitMinion); }
        public void RStart()
        {
            Play(IreliaSoundCue.RCast);
            // Dedicated RNG and source keep dialogue independent from gameplay and blade effects.
            if (CanSpeak() && voiceRandom.NextDouble() < RVoiceChance) Speak(IreliaVoiceCue.RCast);
        }
        public void RLaunched()
        {
            int index = Play(IreliaSoundCue.RLaunch);
            if (index >= 0) { rLaunchSource = voices[index]; rLaunchFade = -1; }
        }
        // The R missile is gone (detonated or out of range): its launch sound fades out.
        public void RMissileEnded()
        {
            if (rLaunchSource != null && rLaunchSource.isPlaying && rLaunchFade < 0) rLaunchFade = 0;
        }
        public void RImpact(bool champion) { Play(champion ? IreliaSoundCue.RHit : IreliaSoundCue.RHitMinion); }
        public void RWallImpact()
        {
            if (Time.time < rWallHitAt + .08f) return;
            rWallHitAt = Time.time; Play(IreliaSoundCue.RWallHit);
        }
        // Player orders (2D lines in LoL, only for Irelia's own player).
        public void MoveOrdered(Vector3 point)
        {
            // Move2DFirst: the first move order of the round.
            if (!firstMoveSpoken && VoiceCount(IreliaVoiceCue.MoveFirst) > 0) { firstMoveSpoken = Speak(IreliaVoiceCue.MoveFirst); return; }
            if (!ChatterReady(false)) return;
            point.y = actor.Position.y;
            if (world.InFountain(actor) && Vector3.Distance(actor.Position, point) > LongMoveDistance) Speak(IreliaVoiceCue.MoveLong);
            else if (voiceRandom.NextDouble() < ChatterChance) Speak(IreliaVoiceCue.Move);
        }
        public void AttackOrdered(Combatant target)
        {
            if (target == null || target.Kind != UnitKind.Champion || !actor.ValidEnemy(target)) return;
            if (ChatterReady()) Speak(IreliaVoiceCue.AttackOrder);
        }
        public void ItemBought(int id)
        {
            IreliaVoiceCue cue;
            switch (id)
            {
                case 1038: cue = IreliaVoiceCue.BuyBFSword; break;
                case 3153: cue = IreliaVoiceCue.BuyRuinedKing; break;
                case 3748: cue = IreliaVoiceCue.BuyTitanicHydra; break;
                case 3078: cue = IreliaVoiceCue.BuyTrinityForce; break;
                default: return;
            }
            Speak(cue);
        }
        public void ChampionKilled() { Speak(IreliaVoiceCue.Kill); }
        public void Died()
        {
            StopAll();
            // Death3D has a higher priority (85) than every other line (80): it is never dropped.
            Speak(IreliaVoiceCue.Death, DeathVoicePriority);
        }
        public void Respawned() { Speak(IreliaVoiceCue.Respawn); }

        // Q cast3D: stacks 0-3 shuffle four efforts. At passive max an 8-step sequence plays the four
        // lines of the current set, then four shuffled efforts. When the max passive ends LoL posts
        // Spell3DPEnd_01/02/03, which restart the sequence on one of the three sets.
        void SpeakQ()
        {
            if (!PassiveMax) { Speak(IreliaVoiceCue.Q); return; }
            bool played = qMaxStep < 4 ? SpeakSequence((IreliaVoiceCue)((int)IreliaVoiceCue.QMaxA + qMaxSet), qMaxStep) : Speak(IreliaVoiceCue.QMax);
            if (played) qMaxStep = (qMaxStep + 1) % 8;
        }
        bool ChatterReady(bool roll = true)
        {
            return CanSpeak() && !voiceSource.isPlaying && Time.time - voiceQuietSince >= ChatterQuietSeconds &&
                (!roll || voiceRandom.NextDouble() < ChatterChance);
        }
        bool CanSpeak(bool dead = false)
        {
            return world != null && actor != null && voiceSource != null && isActiveAndEnabled && !world.Paused && (dead || actor.Alive);
        }
        bool Speak(IreliaVoiceCue cue, int priority = VoicePriority)
        {
            VoiceGroup group;
            if (!voiceGroups.TryGetValue(cue, out group) || !CanSpeak(priority > VoicePriority)) return false;
            if (voiceSource.isPlaying && priority <= voicePriority) return false;
            return StartVoice(cue, group.Next(voiceRandom), priority);
        }
        bool SpeakSequence(IreliaVoiceCue cue, int index)
        {
            VoiceGroup group;
            if (!voiceGroups.TryGetValue(cue, out group) || index >= group.Clips.Length || !CanSpeak()) return false;
            return StartVoice(cue, group.Clips[index], VoicePriority);
        }
        bool StartVoice(IreliaVoiceCue cue, AudioClip clip, int priority)
        {
            // The official VO mixer allows one line per champion: while one plays, a new line of the
            // same priority is dropped (Discard newest); a higher one replaces it.
            if (clip == null || (voiceSource.isPlaying && priority <= voicePriority)) return false;
            voiceSource.Stop(); voiceSource.clip = clip; voiceSource.Play();
            voicePriority = priority; voiceCue = cue; voiceQuietSince = Time.time;
            VoicePlayed?.Invoke(cue, clip);
            if (cue == IreliaVoiceCue.RCast) RVoicePlayed?.Invoke(clip);
            return true;
        }
        int Play(IreliaSoundCue cue, bool chargeVoice = false)
        {
            if (world == null || world.Paused || !actor.Alive || !isActiveAndEnabled) return -1;
            AudioClip[] group;
            if (!clips.TryGetValue(cue, out group) || group.Length == 0) return -1;
            // Independent variation cycle: audio must not consume the AI/gameplay random stream.
            var clip = group[variations[(int)cue]++ % group.Length];
            // Last voice belongs to W's sustained charge and cannot be stolen by impacts.
            int index = chargeVoice ? VoiceLimit - 1 : -1;
            if (!chargeVoice) for (int i = 0; i < voices.Length - 1; i++) if (!voices[i].isPlaying) { index = i; break; }
            if (index < 0)
            {
                index = 0;
                for (int i = 1; i < voices.Length - 1; i++) if (started[i] < started[index]) index = i;
            }
            AudioSource source = voices[index]; started[index] = ++playOrder;
            if (source == rLaunchSource) { rLaunchSource = null; rLaunchFade = -1; }
            source.volume = SfxVolume; source.clip = clip; source.Play(); Played?.Invoke(cue, clip);
            return index;
        }
        void LateUpdate()
        {
            if (actor == null || world == null) return;
            if (voiceSource.isPlaying) voiceQuietSince = Time.time;
            // The SFX end with Irelia; her death line keeps playing.
            if (!actor.Alive) StopEffects(); else if (!world.Paused) { TrackPassive(); FadeRLaunch(); }
            if (world.Paused == SourcesPaused) return;
            SourcesPaused = world.Paused;
            foreach (var source in voices)
            { if (SourcesPaused) source.Pause(); else source.UnPause(); }
            if (SourcesPaused) voiceSource.Pause(); else voiceSource.UnPause();
        }
        void TrackPassive()
        {
            bool max = PassiveMax;
            if (passiveWasMax && !max) { qMaxStep = 0; qMaxSet = voiceRandom.Next(3); }
            passiveWasMax = max;
        }
        void FadeRLaunch()
        {
            if (rLaunchFade < 0 || rLaunchSource == null) return;
            rLaunchFade += Time.deltaTime;
            if (rLaunchFade >= RLaunchFadeSeconds || !rLaunchSource.isPlaying)
            { rLaunchSource.Stop(); rLaunchSource.volume = SfxVolume; rLaunchSource = null; rLaunchFade = -1; return; }
            rLaunchSource.volume = SfxVolume * (1 - rLaunchFade / RLaunchFadeSeconds);
        }
        void StopEffects()
        {
            foreach (var source in voices) if (source != null) { source.Stop(); source.volume = SfxVolume; }
            rLaunchSource = null; rLaunchFade = -1; WChargeActive = false;
        }
        public void StopAll()
        {
            StopEffects();
            if (voiceSource != null) voiceSource.Stop();
            SourcesPaused = false;
        }
        void OnDisable() { StopAll(); }

        // Shuffle without repeats inside a round; the last `avoid` lines also wait across rounds.
        sealed class VoiceGroup
        {
            public readonly AudioClip[] Clips;
            readonly int avoid;
            readonly List<int> recent = new List<int>(), remaining = new List<int>();
            public VoiceGroup(AudioClip[] clips, int avoidRepeat) { Clips = clips; avoid = Mathf.Clamp(avoidRepeat, 0, Mathf.Max(0, clips.Length - 1)); }
            public AudioClip Next(System.Random random)
            {
                if (Clips.Length == 0) return null;
                if (remaining.Count == 0) for (int i = 0; i < Clips.Length; i++) remaining.Add(i);
                int open = 0;
                foreach (int i in remaining) if (!recent.Contains(i)) open++;
                int pick = -1, k = random.Next(open > 0 ? open : remaining.Count);
                foreach (int i in remaining)
                    if ((open == 0 || !recent.Contains(i)) && k-- == 0) { pick = i; break; }
                remaining.Remove(pick); recent.Add(pick);
                if (recent.Count > avoid) recent.RemoveAt(0);
                return Clips[pick];
            }
        }
    }
}
