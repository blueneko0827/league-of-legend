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
            foreach (string shader in new[] { "IreliaRAim", "IreliaRBlade", "IreliaREnergy", "IreliaEConvergence", "IreliaWAim", "IreliaPassiveGlow" })
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
        [UnityTest] public IEnumerator OfficialWAndEEffectsPlay()
        {
            Assert.IsTrue(RiotVfxLibrary.For(world).Loaded);
            var dummy = world.PracticeDummies[0]; dummy.enabled = false;
            var started = new System.Collections.Generic.List<string>();
            System.Action<RiotVfxPlayer> record = player => started.Add(player.SystemName);
            RiotVfxPlayer.Started += record;
            try
            {
                world.Player.transform.position = dummy.Home - Vector3.right * 3; world.Player.Stop();
                // W: shield while charging, the full-charge flash, then the empowered slash and its hit.
                Assert.IsTrue(world.Player.Cast(1, dummy.Actor.Position));
                yield return new WaitForSeconds(.9f);
                Assert.Contains("Irelia_Base_W_DR", started); Assert.Contains("Irelia_Base_W_FullPower", started);
                world.Player.Blade.RequestWRelease(dummy.Actor.Position);
                yield return new WaitForSeconds(.6f);
                Assert.Contains("Irelia_Base_W_Swipe_Empowered", started); Assert.Contains("Irelia_Base_W_Hit", started);
                yield return new WaitForSeconds(.5f);
                // E: cast swirl, both blade missiles, the standing blades and their beam, the hit and stun.
                world.Player.Stop();
                Assert.IsTrue(world.Player.Cast(2, dummy.Home - Vector3.forward * 2));
                yield return new WaitForSeconds(.4f);
                Assert.IsTrue(world.Player.Cast(2, dummy.Home + Vector3.forward * 2));
                // Second throw (0.125 + 0.25 s), then the hold before the blades meet (0.35 s).
                yield return new WaitForSeconds(1f);
                foreach (string name in new[] { "Irelia_Base_E_cas", "Irelia_Base_E_Mis_01", "Irelia_Base_E_Blades", "Irelia_Base_E_Team_Indicator",
                    "Irelia_Base_E_cas_02", "Irelia_Base_E_Mis_02", "Irelia_Base_E_Beam", "Irelia_Base_E_Hit", "Irelia_Base_E_hit_Avatar" })
                    Assert.Contains(name, started, name);
                Assert.IsTrue(dummy.Actor.Marked);
            }
            finally { RiotVfxPlayer.Started -= record; }
        }
        [UnityTest] public IEnumerator OfficialREffectsPlay()
        {
            Assert.IsTrue(RiotVfxLibrary.For(world).Loaded);
            Assert.IsTrue(IreliaRBlades.Available);
            var dummy = world.PracticeDummies[0]; dummy.enabled = false;
            var started = new System.Collections.Generic.List<string>();
            System.Action<RiotVfxPlayer> record = player => started.Add(player.SystemName);
            RiotVfxPlayer.Started += record;
            try
            {
                world.Player.transform.position = dummy.Home - Vector3.right * 3; world.Player.Stop();
                // R: cast swirl, the blades' missile, then the explosion on the dummy and LoL's wall
                // (IreliaBlades with its lines, the pulse at its root and a glow on every blade).
                Assert.IsTrue(world.Player.Cast(3, dummy.Actor.Position));
                yield return new WaitForSeconds(1.2f);
                foreach (string name in new[] { "Irelia_Base_R_cas", "Irelia_Base_R_Mis", "Irelia_Base_R_Mis_Tar_Break",
                    "Irelia_Base_R_groundlines", "Irelia_Base_R_Hit_Aoe", "Irelia_Base_R_Wall_Bladeglow" })
                    Assert.Contains(name, started, name);
                // As in the footage: the wall unit comes after the explosion, and no disarm icon.
                Assert.Less(started.IndexOf("Irelia_Base_R_Mis_Tar_Break"), started.IndexOf("Irelia_Base_R_Hit_Aoe"));
                Assert.IsFalse(started.Contains("Irelia_Base_R_Wall_Disarm"));
                Assert.AreEqual(1, world.BladeWalls.Count);
                var wall = Object.FindAnyObjectByType<IreliaRBlades>();
                Assert.NotNull(wall); Assert.AreEqual(26, wall.BladeCount);
            }
            finally { RiotVfxPlayer.Started -= record; }
        }
        [UnityTest] public IEnumerator RHitAtFourStacksFlaresThePassiveAgain()
        {
            Assert.IsTrue(RiotVfxLibrary.For(world).Loaded);
            var dummy = world.PracticeDummies[0]; dummy.enabled = false;
            world.Player.transform.position = dummy.Home - Vector3.right * 3; world.Player.Stop();
            int flares = 0;
            System.Action<RiotVfxPlayer> record = player => { if (player.SystemName == "Irelia_Base_P_buf") flares++; };
            RiotVfxPlayer.Started += record;
            try
            {
                // Ionian Fervor full: the max-stack aura (deep red Dark, Glow3, Avatar_Burst) flares.
                for (int i = 0; i < 4; i++) world.Player.Blade.AwardSpellHits(new[] { dummy.Actor });
                yield return null; yield return null;
                Assert.AreEqual(4, world.Player.Blade.Stacks); Assert.AreEqual(1, flares);
                // As in the game footage: the R hitting with the passive already full flares it again.
                Assert.IsTrue(world.Player.Cast(3, dummy.Actor.Position));
                yield return new WaitForSeconds(1f);
                Assert.AreEqual(4, world.Player.Blade.Stacks); Assert.GreaterOrEqual(flares, 2);
            }
            finally { RiotVfxPlayer.Started -= record; }
        }
        [UnityTest] public IEnumerator RRightAfterWIsQueuedAndHops()
        {
            var dummy = world.PracticeDummies[0]; dummy.enabled = false;
            world.Player.transform.position = dummy.Home - Vector3.right * 3; world.Player.Stop();
            var rig = world.Player.GetComponentInChildren<IreliaRigAnimation>();
            Transform root = System.Array.Find(rig.GetComponentsInChildren<Transform>(), t => t.name == "Root");
            Assert.NotNull(root);
            Assert.IsTrue(world.Player.Cast(1, dummy.Actor.Position));
            yield return new WaitForSeconds(.3f);
            world.Player.Blade.RequestWRelease(dummy.Actor.Position);
            yield return null;
            // Pressed during W's release lock: queued, then cast when the lock ends (as in LoL).
            int rBefore = world.Player.Blade.RSequence;
            Assert.IsTrue(world.Player.Blade.Command(3, dummy.Actor.Position, null));
            float rest = root.localPosition.y, highest = rest, farthest = 0;
            for (float t = 0; t < .9f; t += Time.deltaTime)
            {
                yield return null;
                highest = Mathf.Max(highest, root.localPosition.y); farthest = Mathf.Max(farthest, root.localPosition.z);
            }
            Assert.AreEqual(rBefore + 1, world.Player.Blade.RSequence);
            // spell4's hop: the root rises ~230 units over its idle height and lunges forward.
            Assert.Greater(highest, rest + 150); Assert.Greater(farthest, 100);
        }
        [UnityTest] public IEnumerator OfficialVoiceLinesPlay()
        {
            var audio = world.Player.Audio; Assert.NotNull(audio);
            foreach (var group in new[] { (IreliaVoiceCue.RCast, 8), (IreliaVoiceCue.Q, 4), (IreliaVoiceCue.QMaxA, 4), (IreliaVoiceCue.E, 5),
                (IreliaVoiceCue.MoveFirst, 5), (IreliaVoiceCue.Move, 50), (IreliaVoiceCue.AttackOrder, 40), (IreliaVoiceCue.BuyBFSword, 3) })
                Assert.AreEqual(group.Item2, audio.VoiceCount(group.Item1), group.Item1.ToString());
            var spoken = new System.Collections.Generic.List<IreliaVoiceCue>();
            System.Action<IreliaVoiceCue, AudioClip> record = (cue, clip) => spoken.Add(cue);
            audio.VoicePlayed += record;
            try
            {
                var dummy = world.PracticeDummies[0]; dummy.enabled = false;
                world.Player.transform.position = dummy.Home - Vector3.right * 4; world.Player.Stop();
                // The round's first move order speaks Move2DFirst.
                audio.MoveOrdered(dummy.Home);
                Assert.AreEqual(1, spoken.Count); Assert.AreEqual(IreliaVoiceCue.MoveFirst, spoken[0]);
                // One line at a time: while it plays, an effort of the same priority is dropped.
                if (audio.VoiceActive) { audio.EPlace(false); Assert.AreEqual(1, spoken.Count); }
                audio.StopAll();
                Assert.IsTrue(world.Player.Cast(0, dummy.Actor.Position, dummy.Actor));
                Assert.AreEqual(IreliaVoiceCue.Q, spoken[spoken.Count - 1]);
                yield return new WaitForSeconds(.6f);
                // Passive at 4 stacks: the Q lines step through their set, then shuffled efforts;
                // W speaks only now.
                world.Player.Blade.AwardSpellHits(new[] { dummy.Actor, dummy.Actor, dummy.Actor, dummy.Actor });
                Assert.AreEqual(4, world.Player.Stacks);
                int before = spoken.Count;
                for (int i = 0; i < 5; i++) { audio.StopAll(); audio.QStart(); }
                CollectionAssert.AreEqual(new[] { IreliaVoiceCue.QMaxA, IreliaVoiceCue.QMaxA, IreliaVoiceCue.QMaxA, IreliaVoiceCue.QMaxA, IreliaVoiceCue.QMax },
                    spoken.GetRange(before, 5));
                audio.StopAll(); audio.WStart(); Assert.AreEqual(IreliaVoiceCue.WMax, spoken[spoken.Count - 1]);
                // Releasing the W cuts the charge line, so the release line plays at once.
                audio.WRelease(); Assert.AreEqual(IreliaVoiceCue.W2Max, spoken[spoken.Count - 1]);
                audio.StopAll(); string reason;
                Assert.IsTrue(world.Player.BuyItem(1038, out reason), reason);
                Assert.AreEqual(IreliaVoiceCue.BuyBFSword, spoken[spoken.Count - 1]);
            }
            finally { audio.VoicePlayed -= record; }
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
