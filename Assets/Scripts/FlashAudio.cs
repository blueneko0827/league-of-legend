using System;
using UnityEngine;

namespace MidLane
{
    // Shared by player and bots; plays only after a successful Flash displacement.
    public sealed class FlashAudio : MonoBehaviour
    {
        public event Action<AudioClip> Played;
        readonly AudioSource[] sources = new AudioSource[4];
        AudioClip[] clips;
        LaneWorld world;
        int variation, nextSource;
        bool paused;
        public void Initialize(LaneWorld lane)
        {
            world = lane; clips = Resources.LoadAll<AudioClip>("Audio/Flash/");
            Array.Sort(clips, (a, b) => string.CompareOrdinal(a.name, b.name));
            for (int i = 0; i < sources.Length; i++)
            {
                var child = new GameObject("Flash sound " + i); child.transform.SetParent(transform, false);
                var source = child.AddComponent<AudioSource>(); source.playOnAwake = false;
                source.spatialBlend = 0; source.volume = .45f; source.priority = 64;
                sources[i] = source;
            }
        }
        public void Play()
        {
            if (world == null || world.Paused || !isActiveAndEnabled || clips.Length == 0) return;
            var source = sources[nextSource++ % sources.Length];
            source.clip = clips[variation++ % clips.Length]; source.Play(); Played?.Invoke(source.clip);
        }
        void LateUpdate()
        {
            if (world == null || world.Paused == paused) return;
            paused = world.Paused;
            foreach (var source in sources) { if (paused) source.Pause(); else source.UnPause(); }
        }
        public void StopAll()
        {
            foreach (var source in sources) if (source != null) source.Stop();
            paused = false;
        }
        void OnDisable() { StopAll(); }
    }
}
