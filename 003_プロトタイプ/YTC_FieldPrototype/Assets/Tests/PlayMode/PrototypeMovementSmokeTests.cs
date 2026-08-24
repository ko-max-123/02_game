using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace YTC.Prototype.Tests
{
    public sealed class PrototypeMovementSmokeTests
    {
        [UnityTest]
        public IEnumerator Bootstrap_CreatesPlayerThatMovesAndJumps()
        {
            yield return null;

            YamadaPrototypeController controller =
                Object.FindAnyObjectByType<YamadaPrototypeController>();
            Assert.That(controller, Is.Not.Null, "Prototype bootstrap did not create Yamada_Player.");
            controller.enabled = false;
            Assert.That(GameObject.Find("[YTC] Environment"), Is.Not.Null);
            GameObject yamadaModel = GameObject.Find("Yamada_DesignAsset");
            Assert.That(
                yamadaModel,
                Is.Not.Null,
                "The official Yamada OBJ was not loaded from Resources.");
            Assert.That(
                GameObject.Find("DemoField_DesignAsset"),
                Is.Not.Null,
                "The official demo-field OBJ was not loaded from Resources.");

            Bounds modelBounds = RendererBounds(yamadaModel);
            Assert.That(modelBounds.size.y, Is.EqualTo(2.3f).Within(0.03f));
            Assert.That(modelBounds.min.y, Is.EqualTo(controller.transform.position.y).Within(0.03f));

            CharacterController characterController =
                controller.GetComponent<CharacterController>();
            Assert.That(characterController.height, Is.EqualTo(1.86f).Within(0.001f));
            Assert.That(characterController.center.y, Is.EqualTo(0.93f).Within(0.001f));

            Camera camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            Assert.That(camera.orthographic, Is.True);
            PrototypeFollowCamera followCamera = camera.GetComponent<PrototypeFollowCamera>();
            Assert.That(followCamera, Is.Not.Null);
            Assert.That(followCamera.Offset, Is.EqualTo(new Vector3(0f, 3.8f, 10.5f)));

            followCamera.enabled = false;
            float fixedCameraDepth = camera.transform.position.z;
            Vector3 cameraBeforeFollow = camera.transform.position;
            controller.TeleportTo(new Vector3(-11f, 0.8f, 0.4f));
            followCamera.Tick(0.2f);
            Assert.That(camera.transform.position.x, Is.GreaterThan(cameraBeforeFollow.x));
            Assert.That(camera.transform.position.y, Is.GreaterThan(cameraBeforeFollow.y));
            Assert.That(camera.transform.position.z, Is.EqualTo(fixedCameraDepth).Within(0.0001f));
            followCamera.SnapToTarget();
            Assert.That(camera.transform.position.x, Is.EqualTo(controller.transform.position.x).Within(0.0001f));
            Assert.That(
                camera.transform.position.y,
                Is.EqualTo(controller.transform.position.y + 3.8f).Within(0.0001f));
            Assert.That(camera.transform.position.z, Is.EqualTo(fixedCameraDepth).Within(0.0001f));
            Assert.That(camera.transform.forward.z, Is.LessThan(-0.95f));

            controller.TeleportTo(new Vector3(-13.7f, 0.05f, 0f));

            for (int index = 0; index < 4; index++)
            {
                controller.Tick(Vector2.zero, false, 0.02f);
                yield return null;
            }

            Assert.That(controller.IsGrounded, Is.True, "Player did not settle on the field ground.");

            Vector3 beforeMove = controller.transform.position;
            controller.Tick(Vector2.right, false, 0.1f);
            Assert.That(controller.transform.position.x, Is.GreaterThan(beforeMove.x + 0.25f));

            controller.TeleportTo(new Vector3(-12f, 0.05f, 0f));
            yield return Settle(controller, 4);
            for (int index = 0; index < 20; index++)
            {
                controller.Tick(Vector2.up, false, 0.02f);
                yield return null;
            }

            Assert.That(controller.IsDepthMovementAllowed, Is.True);
            Assert.That(controller.transform.position.z, Is.GreaterThan(0.5f));
            Assert.That(
                controller.transform.position.z,
                Is.LessThanOrEqualTo(controller.DepthLimit + 0.001f));

            Assert.That(GameObject.Find("COLLISION_START_PLATFORM"), Is.Not.Null);
            Assert.That(GameObject.Find("COLLISION_MIDDLE_PLATFORM"), Is.Not.Null);
            Assert.That(GameObject.Find("COLLISION_GOAL_PLATFORM"), Is.Not.Null);
            Assert.That(GameObject.Find("COLLISION_ASCENT_RAMP"), Is.Not.Null);

            controller.TeleportTo(new Vector3(-8.5f, 0.05f, 0f));
            yield return Settle(controller, 4);
            Assert.That(
                controller.IsGrounded,
                Is.True,
                $"Player did not settle before the obstacle: {controller.transform.position}.");
            float blockedDepth = controller.transform.position.z;
            controller.Tick(Vector2.up, false, 0.1f);
            Assert.That(controller.IsDepthMovementAllowed, Is.False);
            Assert.That(controller.transform.position.z, Is.EqualTo(blockedDepth).Within(0.0001f));

            float obstaclePeak = controller.transform.position.y;
            for (int index = 0; index < 40 && controller.transform.position.x < -6.2f; index++)
            {
                controller.Tick(Vector2.right, index == 0, 0.02f);
                obstaclePeak = Mathf.Max(obstaclePeak, controller.transform.position.y);
                yield return null;
            }

            Assert.That(
                controller.transform.position.x,
                Is.GreaterThan(-6.2f),
                "Player did not clear the official 0.6 m obstacle.");
            Assert.That(obstaclePeak, Is.GreaterThan(0.65f));

            for (int index = 0; index < 80 && !controller.IsGrounded; index++)
            {
                controller.Tick(Vector2.zero, false, 0.02f);
                yield return null;
            }

            Assert.That(controller.IsGrounded, Is.True);

            controller.TeleportTo(new Vector3(-5.5f, 0.05f, 0f));
            yield return Settle(controller, 4);
            Assert.That(
                controller.IsGrounded,
                Is.True,
                $"Player did not settle before the gap: {controller.transform.position}.");

            float gapMinimumY = controller.transform.position.y;
            for (int index = 0; index < 50 && controller.transform.position.x < -2.5f; index++)
            {
                controller.Tick(Vector2.right, index == 0, 0.02f);
                gapMinimumY = Mathf.Min(gapMinimumY, controller.transform.position.y);
                yield return null;
            }

            Assert.That(
                controller.transform.position.x,
                Is.GreaterThan(-2.5f),
                "Player did not clear the official 2 m gap.");
            Assert.That(gapMinimumY, Is.GreaterThan(-0.2f));

            yield return WaitUntilGrounded(controller, 80);
            Assert.That(controller.IsGrounded, Is.True);

            controller.TeleportTo(new Vector3(-2.4f, 0.05f, 0f));
            yield return Settle(controller, 4);
            Assert.That(
                controller.IsGrounded,
                Is.True,
                $"Player did not settle before the steps: {controller.transform.position}.");

            for (int index = 0; index < 70 && controller.transform.position.x < 1.7f; index++)
            {
                controller.Tick(Vector2.right, index == 0, 0.02f);
                yield return null;
            }

            Assert.That(
                controller.transform.position.x,
                Is.GreaterThan(1.7f),
                "Player did not clear the official 0.2/0.4/0.6 m steps.");

            yield return WaitUntilGrounded(controller, 80);
            Assert.That(controller.IsGrounded, Is.True);

            controller.TeleportTo(new Vector3(5.5f, 0.05f, 0f));
            yield return Settle(controller, 4);
            Assert.That(
                controller.IsGrounded,
                Is.True,
                $"Player did not settle before the ramp: {controller.transform.position}.");

            for (int index = 0; index < 50 && controller.transform.position.x < 9.5f; index++)
            {
                controller.Tick(Vector2.right, false, 0.02f);
                yield return null;
            }

            Assert.That(
                controller.transform.position.x,
                Is.GreaterThan(9.5f),
                "Player did not traverse the official ascent ramp.");
            Assert.That(controller.transform.position.y, Is.GreaterThan(0.7f));
        }

        private static IEnumerator Settle(YamadaPrototypeController controller, int frames)
        {
            for (int index = 0; index < frames; index++)
            {
                controller.Tick(Vector2.zero, false, 0.02f);
                yield return null;
            }
        }

        private static IEnumerator WaitUntilGrounded(
            YamadaPrototypeController controller,
            int maximumFrames)
        {
            for (int index = 0; index < maximumFrames && !controller.IsGrounded; index++)
            {
                controller.Tick(Vector2.zero, false, 0.02f);
                yield return null;
            }
        }

        private static Bounds RendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers, Is.Not.Empty);
            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }
    }
}
