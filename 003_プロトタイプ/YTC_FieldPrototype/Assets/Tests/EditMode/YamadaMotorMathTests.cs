using NUnit.Framework;
using UnityEngine;

namespace YTC.Prototype.Tests
{
    public sealed class YamadaMotorMathTests
    {
        [Test]
        public void PlanarDirection_NormalizesDiagonalInput()
        {
            Vector3 direction = YamadaMotorMath.PlanarDirection(1f, 1f);

            Assert.That(direction.magnitude, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(direction.y, Is.EqualTo(0f));
        }

        [Test]
        public void PlanarDirection_PreservesSingleAxisInput()
        {
            Vector3 direction = YamadaMotorMath.PlanarDirection(-1f, 0f);

            Assert.That(direction, Is.EqualTo(Vector3.left));
        }

        [Test]
        public void JumpVelocity_ReachesRequestedHeightUnderConstantGravity()
        {
            const float jumpHeight = 1.35f;
            const float gravity = -24f;

            float velocity = YamadaMotorMath.JumpVelocity(jumpHeight, gravity);
            float calculatedHeight = velocity * velocity / (-2f * gravity);

            Assert.That(calculatedHeight, Is.EqualTo(jumpHeight).Within(0.0001f));
        }

        [Test]
        public void ApplyGravity_DecreasesVerticalVelocity()
        {
            float velocity = YamadaMotorMath.ApplyGravity(5f, -20f, 0.25f);

            Assert.That(velocity, Is.EqualTo(0f).Within(0.0001f));
        }
    }
}
