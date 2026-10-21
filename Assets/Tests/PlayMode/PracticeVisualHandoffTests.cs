#if UNITY_EDITOR
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace MidLane.Tests
{
    public sealed class PracticeVisualHandoffTests
    {
        LaneWorld world;
        bool oldAsyncShaders;
        [UnitySetUp] public IEnumerator SetUp()
        {
            oldAsyncShaders = UnityEditor.ShaderUtil.allowAsyncCompilation;
            UnityEditor.ShaderUtil.allowAsyncCompilation = false;
            world = new GameObject("Practice visual validation").AddComponent<LaneWorld>();
            world.GetComponent<PlayerCommands>().enabled = false;
            yield return null;
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            if (world != null) Object.Destroy(world.gameObject);
            Time.timeScale = 1;
            UnityEditor.ShaderUtil.allowAsyncCompilation = oldAsyncShaders;
            PlayerPrefs.DeleteKey(LaneWorld.GameModePreferenceKey);
            yield return null;
        }
        [Test] public void StartupKeepsTheCurrentPracticeLoadoutAndFiveDummies()
        {
            Assert.IsTrue(world.PracticeModeActive); Assert.IsFalse(world.PvpModeActive);
            Assert.IsFalse(world.NormalplayModeActive); Assert.IsFalse(world.Settings.spawnWaves);
            Assert.IsNull(world.Opponent); Assert.AreEqual(5, world.PracticeDummies.Count);
            Assert.AreEqual(6, world.Player.Level); Assert.AreEqual(50000, world.Player.Gold);
            Assert.AreEqual(2, world.Units.FindAll(unit => unit.Kind == UnitKind.Tower).Count);
            Assert.AreEqual(0, world.Units.FindAll(unit => unit.Kind == UnitKind.Minion).Count);
            Assert.IsTrue(world.Player.UnlimitedMana);
        }
        [Test] public void OtherModesStayUnavailableAndRestartOverridesSavedOrChangedSettings()
        {
            foreach (int savedMode in new[] { 0, 1, 2 })
            {
                PlayerPrefs.SetInt(LaneWorld.GameModePreferenceKey, savedMode);
                world.Settings.practiceMode = false; world.Settings.pvpMode = true; world.Settings.spawnWaves = true;
                world.Restart(); world.TogglePvpMode(); world.TogglePracticeMode();
                world.GetComponent<PracticeHud>().SelectOpponent(Archetype.Brawler);
                world.SpawnWave(world.MatchTime);
                Assert.IsTrue(world.PracticeModeActive); Assert.IsFalse(world.Settings.spawnWaves);
                Assert.IsFalse(world.PvpModeActive); Assert.IsNull(world.Opponent);
                Assert.AreEqual(5, world.PracticeDummies.Count);
                Assert.AreEqual(0, world.Units.FindAll(unit => unit.Kind == UnitKind.Minion).Count);
            }
        }
        [UnityTest] public IEnumerator ANewSessionIgnoresSavedPvpAndNormalplayModes()
        {
            Object.Destroy(world.gameObject); yield return null;
            foreach (int mode in new[] { 0, 1 })
            {
                PlayerPrefs.SetInt(LaneWorld.GameModePreferenceKey, mode);
                world = new GameObject("Saved mode validation").AddComponent<LaneWorld>();
                world.GetComponent<PlayerCommands>().enabled = false;
                Assert.IsTrue(world.PracticeModeActive); Assert.IsNull(world.Opponent);
                Object.Destroy(world.gameObject); yield return null;
            }
            world = null;
        }
        [Test] public void ConvertedAssetsAndPrefabsLoadWithTheirCurrentMaterials()
        {
            var body = world.Player.GetComponentInChildren<SkinnedMeshRenderer>();
            Assert.NotNull(body); Assert.NotNull(body.sharedMesh); Assert.NotNull(body.sharedMaterial);
            Assert.NotNull(world.Player.GetComponentInChildren<IreliaRigAnimation>());
            foreach (var dummy in world.PracticeDummies)
            { Assert.NotNull(dummy.Visual); Assert.Greater(dummy.Visual.IdleClip.length, 0); Assert.Greater(dummy.Visual.HitClip.length, 0); }
            foreach (string clip in new[] { "Run", "Attack", "WCharge", "EFirst", "RCast" })
                Assert.NotNull(Resources.Load<AnimationClip>("Models/IreliaOfficial/" + clip));
            foreach (string shader in new[] { "IreliaRAim", "IreliaRBlade", "IreliaREnergy", "IreliaEConvergence" })
                Assert.NotNull(Resources.Load<Shader>("Shaders/" + shader));
        }
        [UnityTest] public IEnumerator QAndEStillWorkAndTheShopCanBuyOutsideTheFountain()
        {
            var dummy = world.PracticeDummies[0]; dummy.enabled = false;
            world.Player.transform.position = dummy.Home - Vector3.right * 3;
            world.Player.Stop();
            string reason;
            Assert.IsTrue(world.Player.BuyItem(1055, out reason), reason);
            Assert.IsTrue(world.Player.Cast(0, dummy.Actor.Position, dummy.Actor));
            yield return new WaitForSeconds(.5f);
            world.Player.Stop(); Assert.Greater(dummy.TotalDamage, 0);
            Assert.IsTrue(world.Player.Cast(2, dummy.Home - Vector3.forward * 2));
            yield return new WaitForSeconds(.3f);
            Assert.IsTrue(world.Player.Cast(2, dummy.Home + Vector3.forward * 2));
            yield return new WaitForSeconds(.7f);
            Assert.IsTrue(dummy.Actor.Marked); Assert.Greater(dummy.TotalDamage, 0);
        }
        [UnityTest] public IEnumerator OfficialBasicAttackPassiveAndQEffectsPlay()
        {
            var library = RiotVfxLibrary.For(world);
            Assert.IsTrue(library.Loaded);
            foreach (string name in new[] { "Irelia_Base_BA_Far_swipe_01", "Irelia_Base_BA_Far_swipe_02", "Irelia_Base_BA_Hit",
                "Irelia_Base_P_Blades_Stacks_1_2_3", "Irelia_Base_P_buf", "Irelia_Base_Q_Dash", "Irelia_Base_Q_tar", "Irelia_Base_Q_Mark_SelfOnly" })
                Assert.NotNull(library.FindSystem(name), name);
            Assert.IsNotEmpty(library.Data.events);
            var started = new System.Collections.Generic.List<string>();
            System.Action<RiotVfxPlayer> record = player => started.Add(player.SystemName);
            RiotVfxPlayer.Started += record;
            try
            {
                var dummy = world.PracticeDummies[0]; dummy.enabled = false;
                world.Player.transform.position = dummy.Home - Vector3.right * 2.5f;
                world.Player.Stop(); world.Player.OrderAttack(dummy.Actor);
                yield return new WaitForSeconds(1.2f);
                Assert.Contains("Irelia_Base_BA_Far_swipe_01", started);
                Assert.IsTrue(started.Exists(name => name.StartsWith("Irelia_Base_BA_Hit")));
                world.Player.Stop();
                yield return new WaitForSeconds(1.2f);
                world.Player.transform.position = dummy.Home - Vector3.right * 4;
                Assert.IsTrue(world.Player.Cast(0, dummy.Actor.Position, dummy.Actor));
                yield return new WaitForSeconds(.6f);
                Assert.Contains("Irelia_Base_Q_Dash", started); Assert.Contains("Irelia_Base_Q_tar", started);
                Assert.Contains("Irelia_Base_P_Blades_Stacks_1_2_3", started);
            }
            finally { RiotVfxPlayer.Started -= record; }
        }
        [UnityTest] public IEnumerator EmpoweredHitBurstsAtTheDummyChest()
        {
            var dummy = world.PracticeDummies[0]; dummy.enabled = false;
            RiotVfxPlayer passiveHit = null;
            System.Action<RiotVfxPlayer> record = player => { if (player.SystemName.StartsWith("Irelia_Base_P_Hit_0")) passiveHit = player; };
            RiotVfxPlayer.Started += record;
            try
            {
                world.Player.transform.position = dummy.Home - Vector3.right * 2.5f;
                world.Player.Blade.AwardSpellHits(new[] { dummy.Actor, dummy.Actor, dummy.Actor, dummy.Actor });
                Assert.AreEqual(4, world.Player.Stacks);
                world.Player.Stop(); world.Player.OrderAttack(dummy.Actor);
                float deadline = Time.time + 2;
                while (passiveHit == null && Time.time < deadline) yield return null;
                Assert.NotNull(passiveHit, "golpe potenciado sin efecto de pasiva");
                yield return null;
                Transform chest = RiotVfxPlayer.FindBone(dummy.Actor.transform, "C_Buffbone_Glb_Chest_Loc");
                Assert.NotNull(chest);
                Assert.Less(Vector3.Distance(passiveHit.Pose.position, chest.position), .05f);
                Assert.Greater(passiveHit.Pose.position.y - dummy.Actor.Position.y, dummy.Actor.VisualHeight * .3f);
                Assert.Greater(passiveHit.DepthPull, 0);
            }
            finally { RiotVfxPlayer.Started -= record; }
        }
        [UnityTest] public IEnumerator CameraAndPatrolMatchTheCurrentPracticeAndCapturePreview()
        {
            Assert.AreEqual(32.5f, world.View.fieldOfView, .001f);
            Assert.IsTrue(world.View.GetComponent<IsometricCamera>().Locked);
            var patrol = world.PracticeDummies.Find(dummy => dummy.Patrolling);
            Assert.NotNull(patrol); Assert.AreEqual(3.65f, patrol.Actor.MoveSpeed, .001f);
            Assert.AreEqual(new Rect(-12, -4, 8, 8), patrol.PatrolBounds);
            yield return null;
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Handoff/Validation/practice-preview.png"));
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                // Render directly: batchmode does not repaint the Game view or service
                // end-of-frame screenshots. This preview contains the 3D scene, not OnGUI.
                var buffer = new RenderTexture(1280, 720, 24); buffer.Create();
                var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                var previous = RenderTexture.active;
                float aspect = world.View.aspect;
                try
                {
                    world.View.GetComponent<IsometricCamera>().Snap(); world.View.aspect = 1280f / 720;
                    var request = new RenderPipeline.StandardRequest { destination = buffer };
                    Assert.IsTrue(RenderPipeline.SupportsRenderRequest(world.View, request));
                    RenderPipeline.SubmitRenderRequest(world.View, request);
                    RenderTexture.active = buffer; image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
                    File.WriteAllBytes(path, image.EncodeToPNG());
                }
                finally
                {
                    world.View.aspect = aspect; RenderTexture.active = previous;
                    buffer.Release(); Object.Destroy(buffer); Object.Destroy(image);
                }
                Assert.IsTrue(File.Exists(path));
            }
        }
    }
}
#endif
