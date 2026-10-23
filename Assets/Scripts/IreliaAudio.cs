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
        // Lines that are not spells (basic attack efforts, move/attack order lines) need this much
        // voice silence; then the next order or attack rolls this chance, and a miss waits another
        // window. LoL does not document their rate; its gameplay footage has none while Irelia keeps
        // casting.
        public const float ChatterQuietSeconds = 5, ChatterChance = .5f;
        // LoL plays its long move lines when leaving base with a click beyond 900 units.
        public const float LongMoveDistance = 900 * LeagueChampionStats.WorldUnitsPerLeagueUnit;
        // Stop_sfx_Irelia_IreliaR_onmissilelaunch: the launch sound fades out over 1 s (linear)
        // when the R missile ends.
        public const float RLaunchFadeSeconds = 1;
        const float SfxVolume = .45f;
        const int VoicePriority = 80, DeathVoicePriority = 85;
        public event Action<AudioClip> RVoicePlayed;
        public event Action<IreliaVoiceCue, AudioClip> VoicePlayed;
        AudioSource voiceSource;
        readonly Dictionary<IreliaVoiceCue, VoiceGroup> voiceGroups = new Dictionary<IreliaVoiceCue, VoiceGroup>();
        System.Random voiceRandom = new System.Random();
        int voicePriority, qMaxStep, qMaxSet;
        IreliaVoiceCue voiceCue;
        float voiceQuietSince = float.NegativeInfinity;
        bool firstMoveTried, longMoveDone, passiveWasMax;
        public event Action<IreliaSoundCue, AudioClip> Played;
        public bool SourcesPaused { get; private set; }
        public int ClipCount { get; private set; }
        public bool WChargeActive { get; private set; }
        public bool VoiceActive { get { return voiceSource != null && voiceSource.isPlaying; } }
        readonly Dictionary<IreliaSoundCue, AudioClip[]> clips = new Dictionary<IreliaSoundCue, AudioClip[]>();
        readonly int[] variations = new int[Enum.GetValues(typeof(IreliaSoundCue)).Length];
        readonly AudioSource[] voices = new AudioSource[VoiceLimit];
        readonly long[] started = new long[VoiceLimit];
        // Seconds into a fade-out per SFX voice (-1: none); used by the R launch sound.
        readonly float[] fades = new float[VoiceLimit];
        LaneWorld world;
        Combatant actor;
        long playOrder;
        float wBlockAt = -100, rWallHitAt = -100;

        public void Initialize(LaneWorld lane, Combatant owner)
        {
            world = lane; actor = owner;
            var all = Resources.LoadAll<AudioClip>(ResourceRoot);
            Array.Sort(all, (a, b) => string.CompareOrdinal(a.name, b.name)); ClipCount = all.Length;
            foreach (IreliaSoundCue cue in Enum.GetValues(typeof(IreliaSoundCue)))
                clips[cue] = Array.FindAll(all, clip => clip.name.StartsWith(cue + "_", StringComparison.Ordinal));
            for (int i = 0; i < voices.Length; i++)
            {
                var child = new GameObject("Irelia SFX voice " + i); child.transform.SetParent(transform, false);
                var source = child.AddComponent<AudioSource>(); source.playOnAwake = false; source.loop = false;
                source.spatialBlend = 0; source.dopplerLevel = 0; source.volume = SfxVolume;
                source.priority = 64; source.ignoreListenerPause = false; voices[i] = source; fades[i] = -1;
            }
            var voiceRoot = new GameObject("Irelia voice"); voiceRoot.transform.SetParent(transform, false);
            voiceSource = voiceRoot.AddComponent<AudioSource>(); voiceSource.playOnAwake = false;
            voiceSource.spatialBlend = 0; voiceSource.volume = .7f; voiceSource.priority = 32;
            var lines = Resources.LoadAll<AudioClip>(RVoiceResourceRoot);
            foreach (IreliaVoiceCue cue in Enum.GetValues(typeof(IreliaVoiceCue)))
            {
                var group = Array.FindAll(lines, clip => clip.name.StartsWith(cue + "_", StringComparison.Ordinal));
                // Numeric order: the passive-max Q lines are a sequence (QMaxA_1..4).
                Array.Sort(group, (a, b) => VoiceIndex(a.name).CompareTo(VoiceIndex(b.name)));
                voiceGroups[cue] = new VoiceGroup(group, AvoidRepeat(cue));
            }
        }
        static int VoiceIndex(string name)
        {
            int value; return int.TryParse(name.Substring(name.LastIndexOf('_') + 1), out value) ? value : 0;
        }
        // irelia_base_vo_events.bnk: each line's container is shuffled and never repeats its last N
        // lines (wAvoidRepeatCount). QMaxA-C play in order (a sequence).
        static int AvoidRepeat(IreliaVoiceCue cue)
        {
            switch (cue)
            {
                case IreliaVoiceCue.RCast: case IreliaVoiceCue.Kill: case IreliaVoiceCue.Death: return 6;
                case IreliaVoiceCue.MoveFirst: case IreliaVoiceCue.Respawn: return 4;
                case IreliaVoiceCue.E: case IreliaVoiceCue.BuyRuinedKing: case IreliaVoiceCue.BuyTrinityForce: return 3;
                case IreliaVoiceCue.Move: return 49;
                case IreliaVoiceCue.AttackOrder: return 39;
                case IreliaVoiceCue.QMaxA: case IreliaVoiceCue.QMaxB: case IreliaVoiceCue.QMaxC: return 0;
                default: return 2;
            }
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
            // 30% of the casts that find Irelia's voice free, with a dedicated RNG (independent from
            // gameplay and blade effects).
            if (CanSpeak() && !voiceSource.isPlaying && voiceRandom.NextDouble() < RVoiceChance) Speak(IreliaVoiceCue.RCast);
        }
        // Returns the launch sound's play id (0: none), for RMissileEnded.
        public long RLaunched()
        {
            int index = Play(IreliaSoundCue.RLaunch);
            return index >= 0 ? started[index] : 0;
        }
        // That R missile is gone (detonated or out of range): its launch sound fades out.
        public void RMissileEnded(long launch)
        {
            if (launch <= 0) return;
            for (int i = 0; i < voices.Length; i++)
                if (started[i] == launch && voices[i] != null && voices[i].isPlaying && fades[i] < 0) fades[i] = 0;
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
            if (world == null || actor == null) return;
            bool fromBase = world.InFountain(actor);
            // Move2DFirst: the round's first move order, tried once. It also covers leaving base.
            if (!firstMoveTried && VoiceCount(IreliaVoiceCue.MoveFirst) > 0)
            { firstMoveTried = true; longMoveDone |= fromBase; Speak(IreliaVoiceCue.MoveFirst); return; }
            point.y = actor.Position.y;
            // Move2DLong: leaving base with a click beyond 900 units, once per stay in the fountain.
            if (fromBase && !longMoveDone && Vector3.Distance(actor.Position, point) > LongMoveDistance)
            { longMoveDone = Speak(IreliaVoiceCue.MoveLong); return; }
            if (ChatterReady()) Speak(IreliaVoiceCue.Move);
        }
        public void AttackOrdered(Combatant target)
        {
            if (actor == null || target == null || target.Kind != UnitKind.Champion || !actor.ValidEnemy(target)) return;
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
        // Lines that are not spells: after ChatterQuietSeconds of voice silence one attempt rolls
        // ChatterChance; a miss restarts the wait.
        bool ChatterReady()
        {
            if (!CanSpeak() || voiceSource.isPlaying || Time.time - voiceQuietSince < ChatterQuietSeconds) return false;
            if (voiceRandom.NextDouble() < ChatterChance) return true;
            voiceQuietSince = Time.time; return false;
        }
        bool CanSpeak(bool dead = false)
        {
            if (world == null || actor == null || voiceSource == null || !isActiveAndEnabled || world.Paused || (!dead && !actor.Alive)) return false;
            SyncPause();
            return true;
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
            // Listeners are presentation: an exception there must not break the gameplay call.
            try
            {
                VoicePlayed?.Invoke(cue, clip);
                if (cue == IreliaVoiceCue.RCast) RVoicePlayed?.Invoke(clip);
            }
            catch (Exception e) { Debug.LogException(e); }
            return true;
        }
        int Play(IreliaSoundCue cue, bool chargeVoice = false)
        {
            if (world == null || world.Paused || !actor.Alive || !isActiveAndEnabled) return -1;
            SyncPause();
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
            AudioSource source = voices[index]; started[index] = ++playOrder; fades[index] = -1;
            source.volume = SfxVolume; source.clip = clip; source.Play(); Played?.Invoke(cue, clip);
            return index;
        }
        // Sources follow the game's pause. Also run before playing: after a resume a frame of
        // gameplay can come before LateUpdate, and a paused source reports isPlaying false.
        void SyncPause()
        {
            if (world == null || world.Paused == SourcesPaused) return;
            SourcesPaused = world.Paused;
            foreach (var source in voices)
                if (source != null) { if (SourcesPaused) source.Pause(); else source.UnPause(); }
            if (voiceSource != null) { if (SourcesPaused) voiceSource.Pause(); else voiceSource.UnPause(); }
        }
        void LateUpdate()
        {
            if (actor == null || world == null) return;
            SyncPause();
            if (voiceSource.isPlaying) voiceQuietSince = Time.time;
            // The SFX end with Irelia; her death line keeps playing.
            if (!actor.Alive) { StopEffects(); return; }
            if (world.Paused) return;
            TrackPassive(); FadeEffects();
            if (longMoveDone && !world.InFountain(actor)) longMoveDone = false;
        }
        void TrackPassive()
        {
            bool max = PassiveMax;
            if (passiveWasMax && !max) { qMaxStep = 0; qMaxSet = voiceRandom.Next(3); }
            passiveWasMax = max;
        }
        void FadeEffects()
        {
            for (int i = 0; i < voices.Length; i++)
            {
                if (fades[i] < 0) continue;
                fades[i] += Time.deltaTime;
                if (fades[i] >= RLaunchFadeSeconds || !voices[i].isPlaying)
                { voices[i].Stop(); voices[i].volume = SfxVolume; fades[i] = -1; }
                else voices[i].volume = SfxVolume * (1 - fades[i] / RLaunchFadeSeconds);
            }
        }
        void StopEffects()
        {
            for (int i = 0; i < voices.Length; i++)
                if (voices[i] != null) { voices[i].Stop(); voices[i].volume = SfxVolume; fades[i] = -1; }
            WChargeActive = false;
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
