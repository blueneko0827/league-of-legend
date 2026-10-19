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
        RCast, RLaunch, RHit, RHitMinion, RWallHit
    }

    // Called by committed gameplay actions; animation events cannot create fake impacts.
    public sealed class IreliaAudio : MonoBehaviour
    {
        public const string ResourceRoot = "Audio/Irelia/";
        public const int VoiceLimit = 10;
        public const float RVoiceChance = .3f;
        public const string RVoiceResourceRoot = "Audio/IreliaVoice/";
        public event Action<AudioClip> RVoicePlayed;
        AudioClip[] rVoiceClips;
        AudioSource rVoiceSource;
        System.Random voiceRandom = new System.Random();
        public event Action<IreliaSoundCue, AudioClip> Played;
        public bool SourcesPaused { get; private set; }
        public int ClipCount { get; private set; }
        public bool WChargeActive { get; private set; }
        readonly Dictionary<IreliaSoundCue, AudioClip[]> clips = new Dictionary<IreliaSoundCue, AudioClip[]>();
        readonly int[] variations = new int[Enum.GetValues(typeof(IreliaSoundCue)).Length];
        readonly AudioSource[] voices = new AudioSource[VoiceLimit];
        readonly long[] started = new long[VoiceLimit];
        LaneWorld world;
        Combatant actor;
        long playOrder;
        float wBlockAt = -100, rWallHitAt = -100;

        public void Initialize(LaneWorld lane, Combatant owner)
        {
            world = lane; actor = owner;
            rVoiceClips = Resources.LoadAll<AudioClip>(RVoiceResourceRoot);
            Array.Sort(rVoiceClips, (a, b) => string.CompareOrdinal(a.name, b.name));
            var voiceRoot = new GameObject("Irelia R dialogue"); voiceRoot.transform.SetParent(transform, false);
            rVoiceSource = voiceRoot.AddComponent<AudioSource>(); rVoiceSource.playOnAwake = false;
            rVoiceSource.spatialBlend = 0; rVoiceSource.volume = .7f; rVoiceSource.priority = 32;
            var all = Resources.LoadAll<AudioClip>(ResourceRoot);
            Array.Sort(all, (a, b) => string.CompareOrdinal(a.name, b.name)); ClipCount = all.Length;
            foreach (IreliaSoundCue cue in Enum.GetValues(typeof(IreliaSoundCue)))
                clips[cue] = Array.FindAll(all, clip => clip.name.StartsWith(cue + "_", StringComparison.Ordinal));
            for (int i = 0; i < voices.Length; i++)
            {
                var child = new GameObject("Irelia SFX voice " + i); child.transform.SetParent(transform, false);
                var source = child.AddComponent<AudioSource>(); source.playOnAwake = false; source.loop = false;
                source.spatialBlend = 0; source.dopplerLevel = 0; source.volume = .45f;
                source.priority = 64; source.ignoreListenerPause = false; voices[i] = source;
            }
        }
        public void BasicCast(int variant) { Play(variant == 2 ? IreliaSoundCue.Attack2Cast : IreliaSoundCue.Attack1Cast); }
        public void BasicHit(int variant) { Play(variant == 2 ? IreliaSoundCue.Attack2Hit : IreliaSoundCue.Attack1Hit); }
        public void QStart() { Play(IreliaSoundCue.QCast); Play(IreliaSoundCue.QDash); }
        public void QImpact() { Play(IreliaSoundCue.QHit); }
        public void WStart() { WChargeActive = true; Play(IreliaSoundCue.WCharge, true); }
        public void WFullyCharged() { Play(IreliaSoundCue.WCharged); }
        public void WRelease() { StopWCharge(); Play(IreliaSoundCue.WRelease); }
        public void StopWCharge() { WChargeActive = false; if (voices[VoiceLimit - 1] != null) voices[VoiceLimit - 1].Stop(); }
        public void WImpact() { Play(IreliaSoundCue.WHit); }
        public void WBlock()
        {
            if (Time.time < wBlockAt + .12f) return;
            wBlockAt = Time.time; Play(IreliaSoundCue.WBlock);
        }
        public void EPlace(bool second) { Play(second ? IreliaSoundCue.ESecond : IreliaSoundCue.EFirst); Play(IreliaSoundCue.ELaunch); }
        public void EConverging() { Play(IreliaSoundCue.EConverge); }
        public void EImpact(bool champion) { Play(champion ? IreliaSoundCue.EHit : IreliaSoundCue.EHitMinion); }
        public void RStart()
        {
            Play(IreliaSoundCue.RCast);
            if (world == null || world.Paused || !actor.Alive || !isActiveAndEnabled || rVoiceClips.Length == 0) return;
            // Dedicated RNG and source keep dialogue independent from gameplay and blade effects.
            if (voiceRandom.NextDouble() >= RVoiceChance) return;
            var clip = rVoiceClips[voiceRandom.Next(rVoiceClips.Length)];
            rVoiceSource.clip = clip; rVoiceSource.Play(); RVoicePlayed?.Invoke(clip);
        }
        public void RLaunched() { Play(IreliaSoundCue.RLaunch); }
        public void RImpact(bool champion) { Play(champion ? IreliaSoundCue.RHit : IreliaSoundCue.RHitMinion); }
        public void RWallImpact()
        {
            if (Time.time < rWallHitAt + .08f) return;
            rWallHitAt = Time.time; Play(IreliaSoundCue.RWallHit);
        }
        void Play(IreliaSoundCue cue, bool chargeVoice = false)
        {
            if (world == null || world.Paused || !actor.Alive || !isActiveAndEnabled) return;
            AudioClip[] group;
            if (!clips.TryGetValue(cue, out group) || group.Length == 0) return;
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
            source.clip = clip; source.Play(); Played?.Invoke(cue, clip);
        }
        void LateUpdate()
        {
            if (actor == null || world == null) return;
            if (!actor.Alive) { StopAll(); return; }
            if (world.Paused == SourcesPaused) return;
            SourcesPaused = world.Paused;
            foreach (var source in voices)
            { if (SourcesPaused) source.Pause(); else source.UnPause(); }
            if (SourcesPaused) rVoiceSource.Pause(); else rVoiceSource.UnPause();
        }
        public void StopAll()
        {
            foreach (var source in voices) if (source != null) source.Stop();
            if (rVoiceSource != null) rVoiceSource.Stop();
            SourcesPaused = WChargeActive = false;
        }
        void OnDisable() { StopAll(); }
    }
}
