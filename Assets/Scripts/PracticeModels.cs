using UnityEngine;

namespace MidLane
{
    public static class PracticeModels
    {
        public static void CreateIrelia(LaneWorld world, Transform parent, Team team)
        {
            GameObject body = CreateChampionBody(world, parent, team, Archetype.Blade);
            // This rig already contains its six floating blades and their animation channels.
            parent.gameObject.AddComponent<IreliaModelAnimation>().Configure(body.transform, System.Array.Empty<Transform>());
        }
        public static GameObject CreateChampionBody(LaneWorld world, Transform parent, Team team, Archetype champion)
        {
            // Normalize a wrapper rather than the imported root: both packages have offset pivots/transforms.
            var body = new GameObject("ChampionBody"); body.transform.SetParent(parent, false);
            var geometry = new GameObject("Geometry").transform; geometry.SetParent(body.transform, false);
            bool female = champion == Archetype.Blade || champion == Archetype.Prism;
            bool crimson = champion == Archetype.Blade;
            bool sett = champion == Archetype.Brawler;
            bool mel = champion == Archetype.Prism;
            IreliaGlbAssets assets = crimson ? world.GetComponent<IreliaGlbAssets>() : null;
            if (crimson && assets == null) assets = world.gameObject.AddComponent<IreliaGlbAssets>();
            SettModelAssets settAssets = sett ? world.GetComponent<SettModelAssets>() : null;
            if (sett && settAssets == null) settAssets = world.gameObject.AddComponent<SettModelAssets>();
            GameObject visual = crimson ? assets.CreateBody(geometry) : sett ? settAssets.CreateBody(geometry) : mel ? MelModelAssets.CreateBody(geometry) : Load(female ? "Models/ChampionBases/WomenBase" : "Models/ChampionBases/CharacterBase",
                geometry, female ? "WomenBase" : "CharacterBase");
            FitHeight(geometry, body.transform, TowerToTowerRules.ChampionSelectionHeight, true, crimson);
            // Scale outside the imported rig so animation sampling cannot overwrite it.
            // The gameplay root and its collider stay at scale one.
            body.transform.localScale = Vector3.one * world.Presentation.visual_mesh_multiplier;
            Color baseColor = champion == Archetype.Blade ? new Color(0.68f, 0.72f, 0.84f) :
                champion == Archetype.Prism ? new Color(0.90f, 0.77f, 0.54f) :
                champion == Archetype.Brawler ? new Color(0.78f, 0.58f, 0.44f) : new Color(0.60f, 0.24f, 0.30f);
            Material material = crimson || mel || sett ? null : world.Material(Color.Lerp(baseColor, team == Team.Blue ? Color.cyan : new Color(1, 0.3f, 0.4f), 0.12f));
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>())
            {
                if (crimson || mel || sett) continue; // Keep the supplied official texture atlases.
                var materials = new Material[Mathf.Max(1, renderer.sharedMaterials.Length)];
                for (int i = 0; i < materials.Length; i++) materials[i] = material;
                renderer.sharedMaterials = materials;
            }
            if (sett) parent.gameObject.AddComponent<SettModelAnimation>().Configure(visual);
            if (mel) parent.gameObject.AddComponent<MelModelAnimation>().Configure(visual);
            geometry.gameObject.AddComponent<CameraFacingVisualTilt>().Initialize(world, crimson);
            UnitShadows.Attach(world, visual);
            return body;
        }
        public static void CreateTower(LaneWorld world, Transform parent, Team team)
        {
            GameObject tower = OfficialLaneUnitAssets.Create(team == Team.Blue ? "BlueTower" : "RedTower", parent);
            tower.transform.localRotation = Quaternion.Euler(0, team == Team.Blue ? 180 : 0, 0);
            var renderers = tower.GetComponentsInChildren<Renderer>(); Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            // Riot's authored Y=0 is the ground plane. Its long foundation extends underground;
            // fitting its lowest vertex to the floor would expose the buried support as a pedestal.
            Vector3 top = parent.InverseTransformPoint(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z));
            float factor = world.Presentation.TowerVisualHeight / Mathf.Max(.001f, top.y);
            tower.transform.localScale *= factor;
            tower.transform.localPosition = new Vector3(-top.x, 0, -top.z) * factor;
            UnitShadows.Attach(world, tower);
        }
        public static GameObject CreateSpellBlade(LaneWorld world, Transform parent, Team team, string label, Vector3 position)
        {
            return world.GetComponent<IreliaGlbAssets>().CreateSpellBlade(parent, label, position);
        }
        static GameObject Load(string path, Transform parent, string name)
        {
            GameObject asset = Resources.Load<GameObject>(path);
            if (asset == null) throw new System.InvalidOperationException("Falta el modelo: " + path);
            GameObject visual = Object.Instantiate(asset, parent, false); visual.name = name;
            foreach (Collider collider in visual.GetComponentsInChildren<Collider>()) { collider.enabled = false; Object.Destroy(collider); }
            return visual;
        }
        internal static void FitHeight(Transform visual, Transform parent, float height, bool centerBody, bool excludeBlades = false)
        {
            Renderer[] renderers = System.Array.FindAll(visual.GetComponentsInChildren<Renderer>(), renderer => renderer.enabled && (!excludeBlades || renderer.name != "AnimatedBlades"));
            if (renderers.Length == 0) throw new System.InvalidOperationException("Modelo sin geometría: " + visual.name);
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            Vector3 center = parent.InverseTransformPoint(bounds.center);
            Vector3 bottom = parent.InverseTransformPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
            float factor = height / Mathf.Max(0.001f, bounds.size.y); visual.localScale *= factor;
            visual.localPosition = new Vector3(centerBody ? -center.x : 0, -bottom.y, centerBody ? -center.z : 0) * factor;
        }
        static void SetMaterials(LaneWorld world, GameObject visual, Team team)
        {
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>())
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    string name = materials[i] == null ? "" : materials[i].name;
                    Color color;
                    if (name.StartsWith("StoneLight")) color = new Color(0.62f, 0.69f, 0.77f);
                    else if (name.StartsWith("StoneDark")) color = new Color(0.22f, 0.29f, 0.4f);
                    else if (name.StartsWith("Stone")) color = new Color(0.4f, 0.47f, 0.62f);
                    else if (name.StartsWith("Gold")) color = new Color(0.82f, 0.61f, 0.27f);
                    else if (name.StartsWith("Crystal")) color = team == Team.Blue ? new Color(0.12f, 0.78f, 1) : new Color(1, 0.18f, 0.24f);
                    else if (name.StartsWith("BodySkin")) color = new Color(0.89f, 0.79f, 0.73f);
                    else if (name.StartsWith("HairMat")) color = new Color(0.12f, 0.14f, 0.22f);
                    else if (name.StartsWith("ArmorMat")) color = new Color(0.76f, 0.79f, 0.85f);
                    else if (name.StartsWith("BladeMat")) color = Color.Lerp(new Color(0.82f, 0.83f, 0.88f), team == Team.Blue ? Color.cyan : new Color(1, 0.3f, 0.4f), 0.18f);
                    else color = new Color(0.68f, 0.72f, 0.84f);
                    materials[i] = world.Material(color);
                }
                renderer.sharedMaterials = materials;
            }
        }
    }
}
