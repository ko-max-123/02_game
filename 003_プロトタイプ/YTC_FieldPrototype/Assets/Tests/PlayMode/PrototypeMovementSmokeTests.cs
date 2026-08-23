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

            float beforeJumpY = controller.transform.position.y;
            controller.Tick(Vector2.zero, true, 0.02f);
            Assert.That(controller.VerticalVelocity, Is.GreaterThan(0f));
            Assert.That(controller.transform.position.y, Is.GreaterThan(beforeJumpY));
        }
    }
}
