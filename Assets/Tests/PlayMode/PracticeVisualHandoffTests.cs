#if UNITY_EDITOR
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
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
        [UnityTest] public IEnumerator CameraAndPatrolMatchTheCurrentPracticeAndCapturePreview()
        {
            Assert.AreEqual(32.5f, world.View.fieldOfView, .001f);
            Assert.IsTrue(world.View.GetComponent<IsometricCamera>().Locked);
            var patrol = world.PracticeDummies.Find(dummy => dummy.Patrolling);
            Assert.NotNull(patrol); Assert.AreEqual(3.65f, patrol.Actor.MoveSpeed, .001f);
            Assert.AreEqual(new Rect(-12, -4, 8, 8), patrol.PatrolBounds);
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                var gameView = UnityEditor.EditorWindow.GetWindow(System.Type.GetType("UnityEditor.GameView,UnityEditor"));
                gameView.position = new Rect(30, 30, 1280, 760); gameView.Show();
                yield return new WaitForSeconds(1);
                string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Handoff/Validation/practice-preview.png"));
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                if (File.Exists(path)) File.Delete(path);
                ScreenCapture.CaptureScreenshot(path);
                float deadline = Time.realtimeSinceStartup + 10;
                while (!File.Exists(path) && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsTrue(File.Exists(path));
            }
        }
    }
}
#endif
