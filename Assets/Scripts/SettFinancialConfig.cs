using System;
using UnityEngine;

namespace MidLane
{
    [Serializable]
    public sealed class SettFinancialConfig
    {
        public float champion_kill_gold = 300, cannon_gold = 60, melee_gold = 21, caster_gold = 14;
        public float lasthit_priority_over_poke = 0.85f;
        public float q_bait_attraction_gold = 100, trap_spacing_units = 150;
        public float all_in_roi_threshold = 1.4f, estimated_death_loss_gold = 275;
        public float reaction_min_seconds = 0.12f, reaction_max_seconds = 0.19f;
        public float panic_health_threshold = 0.25f, panic_reaction_multiplier = 1.25f;
        public float neutral_spacing_units = 650, spacing_noise_units = 45, spacing_noise_interval_seconds = 0.2f;
        public float MinionGold(MinionType type) { return type == MinionType.Cannon ? cannon_gold : type == MinionType.Melee ? melee_gold : caster_gold; }
        public static SettFinancialConfig Load()
        {
            var config = new SettFinancialConfig();
            TextAsset asset = Resources.Load<TextAsset>("SettFinancialAI");
            if (asset != null) JsonUtility.FromJsonOverwrite(asset.text, config);
            config.lasthit_priority_over_poke = Mathf.Clamp01(config.lasthit_priority_over_poke);
            config.reaction_min_seconds = Mathf.Clamp(config.reaction_min_seconds, 0.12f, 0.19f);
            config.reaction_max_seconds = Mathf.Clamp(config.reaction_max_seconds, config.reaction_min_seconds, 0.19f);
            config.spacing_noise_interval_seconds = Mathf.Max(0.02f, config.spacing_noise_interval_seconds);
            return config;
        }
    }
}
