using UnityEngine;

namespace MidLane
{
    [CreateAssetMenu(menuName = "MidLane/Sandbox settings")]
    public sealed class SandboxSettings : ScriptableObject
    {
        [Header("Practice")]
        public Archetype opponent = Archetype.Brawler;
        public Difficulty difficulty = Difficulty.Hard;
        public bool spawnWaves = true;
        public bool pvpMode = true;
        [Range(0, 15)] public float pvpSettWaitOffset = 10;
        public bool practiceMode;
        [Min(0)] public float extraWaveInterval = 5;
        public bool testMode;
        public bool muteAllAudio;
        [Range(0, PlayerLatency.MaxMilliseconds)] public int simulatedPingMs;
        public bool usePracticeRespawnTime = true;
        [Min(0)] public float practiceRespawnSeconds = 3;
        public bool keepOpponentProgress = true;
        [Range(0, 1)] public float aiIgniteChance = 0.3f;
        public bool randomizePlayerSide = true;
        public SummonerSpell dSummoner = SummonerSpell.Flash;
        public SummonerSpell fSummoner = SummonerSpell.Ignite;
        public Team fixedPlayerSide = Team.Blue;
        [Header("Mouse selection / pixels at 720p")]
        [Range(0, 40)] public float selectionPadding = 18;
        [Header("Locked camera / offset toward the opposing side")]
        [Range(0, 5)] public float lockedCameraOffset = 2.5f;
        [Header("Lane")]
        [HideInInspector] public float halfLength = TowerToTowerRules.HalfLength;
        [HideInInspector] public float halfWidth = TowerToTowerRules.HalfWidth;
    }
}
