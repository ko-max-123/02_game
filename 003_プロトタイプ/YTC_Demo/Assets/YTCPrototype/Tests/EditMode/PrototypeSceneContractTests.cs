using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YTCPrototype.Tests
{
    public sealed class PrototypeSceneContractTests
    {
        private const string ScenePath = "Assets/YTCPrototype/Scenes/YTC_Demo.unity";

        [OneTimeSetUp]
        public void OpenGeneratedScene()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [Test]
        public void Player_UsesOfficialVisualAndNarrowDepthLane()
        {
            GameObject root = GameObject.Find("YTC_PrototypeRoot");
            Assert.That(root, Is.Not.Null);

            Transform player = root.transform.Find("Yamada_K1_Player");
            Assert.That(player, Is.Not.Null);
            Assert.That(player.GetComponent<CharacterController>(), Is.Not.Null);

            PrototypePlayerController controller = player.GetComponent<PrototypePlayerController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.MinimumDepth, Is.EqualTo(-2.5f));
            Assert.That(controller.MaximumDepth, Is.EqualTo(2.5f));

            Transform officialVisual = player.Find("PlayerVisualRoot/YamadaK1DesignVisual");
            Transform fallback = player.Find("PlayerVisualRoot/PrimitiveK1Fallback");
            Assert.That(officialVisual, Is.Not.Null);
            Assert.That(officialVisual.gameObject.activeSelf, Is.True);
            Assert.That(fallback, Is.Not.Null);
            Assert.That(fallback.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void Field_UsesOfficialVisualAndHiddenCollisionMesh()
        {
            GameObject root = GameObject.Find("YTC_PrototypeRoot");
            Transform field = root.transform.Find("DemoField");
            Transform visual = field.Find("DesignFieldVisual");
            Transform collision = field.Find("DesignFieldCollision");
            Transform fallback = field.Find("PrimitiveFieldFallback");

            Assert.That(visual, Is.Not.Null);
            Assert.That(collision, Is.Not.Null);
            Assert.That(fallback.gameObject.activeSelf, Is.False);
            Assert.That(collision.GetComponentsInChildren<MeshCollider>(true).Length, Is.GreaterThan(0));
            Assert.That(
                collision.GetComponentsInChildren<Renderer>(true).All(renderer => !renderer.enabled),
                Is.True);
        }

        [Test]
        public void CameraAndHud_EnforceSideViewContract()
        {
            Camera camera = GameObject.Find("Main Camera").GetComponent<Camera>();
            Assert.That(camera, Is.Not.Null);
            Assert.That(camera.orthographic, Is.True);
            Assert.That(camera.transform.position.z, Is.EqualTo(-16f).Within(0.0001f));
            Assert.That(camera.GetComponent<FixedDepthCamera>(), Is.Not.Null);

            GameObject guide = GameObject.Find("PrototypeGuideOverlay");
            Assert.That(guide, Is.Not.Null);
            Assert.That(guide.GetComponent<PrototypeGuideOverlay>(), Is.Not.Null);
        }

        [Test]
        public void CombatScene_HasWeaponsHealthDirectorAndReadableEnemyShapes()
        {
            GameObject player = GameObject.Find("Yamada_K1_Player");
            Assert.That(player.GetComponent<PrototypePlayerCombat>(), Is.Not.Null);
            Assert.That(player.GetComponent<PrototypePlayerHealth>(), Is.Not.Null);
            Assert.That(GameObject.Find("PrototypeCombatDirector").GetComponent<PrototypeCombatDirector>(), Is.Not.Null);

            PrototypeEnemy[] enemies = Object.FindObjectsByType<PrototypeEnemy>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            Assert.That(enemies.Length, Is.EqualTo(3));
            foreach (PrototypeEnemy enemy in enemies)
            {
                Assert.That(enemy.transform.Find("Body"), Is.Not.Null);
                Assert.That(enemy.transform.Find("LowHead"), Is.Not.Null);
                Assert.That(enemy.transform.Find("EnemySensorTriangle"), Is.Not.Null);
                Assert.That(enemy.GetComponent<CapsuleCollider>(), Is.Not.Null);
            }
        }

        [Test]
        public void AirborneGroundProbe_ExcludesSelfAndAllowsHeldJet()
        {
            PrototypePlayerController controller = GameObject.Find("Yamada_K1_Player")
                .GetComponent<PrototypePlayerController>();
            Vector3 originalPosition = controller.transform.position;

            try
            {
                controller.transform.position = new Vector3(originalPosition.x, 20f, originalPosition.z);
                Physics.SyncTransforms();

                MethodInfo probeGround = typeof(PrototypePlayerController).GetMethod(
                    "ProbeGround",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(probeGround, Is.Not.Null);

                bool grounded = (bool)probeGround.Invoke(controller, null);
                bool jetAllowed = PrototypeMovementMath.ShouldApplyFlight(
                    true,
                    grounded,
                    0.18f,
                    0.18f,
                    100f);

                Assert.That(grounded, Is.False, "The player's own CharacterController must not count as ground.");
                Assert.That(jetAllowed, Is.True, "Held Space must allow jet after leaving the ground.");
            }
            finally
            {
                controller.transform.position = originalPosition;
                Physics.SyncTransforms();
            }
        }
    }
}
