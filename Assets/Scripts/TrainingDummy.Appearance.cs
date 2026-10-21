using UnityEngine;

namespace MidLane
{
    public sealed partial class TrainingDummy
    {
        GameObject minionAppearance;
        public bool UsesMinionModel { get; private set; }
        public MinionType DisplayMinionType { get; private set; }
        public float VisualHeight => UsesMinionModel ? world.Presentation.MinionVisualHeight(DisplayMinionType) : world.Presentation.DummyVisualHeight;

        internal void SetMinionAppearance(bool enabled, MinionType type)
        {
            if (enabled && minionAppearance == null)
            {
                DisplayMinionType = type;
                minionAppearance = new GameObject("Practice minion appearance");
                minionAppearance.transform.SetParent(transform.Find("Model"), false);
                MinionVisuals.Create(world, minionAppearance.transform, Actor.Team, type);
            }
            UsesMinionModel = enabled;
            Visual.transform.parent.gameObject.SetActive(!enabled);
            if (minionAppearance != null)
            {
                minionAppearance.SetActive(enabled);
                var animation = minionAppearance.GetComponent<MinionModelAnimation>();
                if (enabled) animation.Bind(world, Actor);
                else animation.Unbind();
            }
        }
    }
}
