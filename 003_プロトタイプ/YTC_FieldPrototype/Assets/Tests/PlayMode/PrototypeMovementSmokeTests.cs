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
            Assert.That(
                GameObject.Find("Yamada_DesignAsset"),
                Is.Not.Null,
                "The official Yamada OBJ was not loaded from Resources.");
            Assert.That(
                GameObject.Find("DemoField_DesignAsset"),
                Is.Not.Null,
                "The official demo-field OBJ was not loaded from Resources.");

            for (int index = 0; index < 4; index++)
            {
                controller.Tick(Vector2.zero, false, 0.02f);
                yield return null;
            }

            Assert.That(controller.IsGrounded, Is.True, "Player did not settle on the field ground.");

            Vector3 beforeMove = controller.transform.position;
            controller.Tick(Vector2.right, false, 0.1f);
            Assert.That(controller.transform.position.x, Is.GreaterThan(beforeMove.x + 0.25f));

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
    }
}
