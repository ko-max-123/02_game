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

            CharacterController characterController =
                controller.GetComponent<CharacterController>();

            SetPlayerPosition(characterController, new Vector3(-8.5f, 0.05f, 0f));
            yield return Settle(controller, 4);

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

            SetPlayerPosition(characterController, new Vector3(-5.5f, 0.05f, 0f));
            yield return Settle(controller, 4);

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
        }

        private static IEnumerator Settle(YamadaPrototypeController controller, int frames)
        {
            for (int index = 0; index < frames; index++)
            {
                controller.Tick(Vector2.zero, false, 0.02f);
                yield return null;
            }
        }

        private static void SetPlayerPosition(
            CharacterController characterController,
            Vector3 position)
        {
            characterController.enabled = false;
            characterController.transform.SetPositionAndRotation(position, Quaternion.identity);
            characterController.enabled = true;
        }
    }
}
