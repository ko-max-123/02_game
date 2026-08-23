using NUnit.Framework;
using UnityEngine;

namespace YTCPrototype.Tests
{
    public sealed class PrototypeMovementMathTests
    {
        [Test]
        public void PlanarInput_DiagonalMagnitudeDoesNotExceedOne()
        {
            Vector2 result = PrototypeMovementMath.ClampPlanarInput(1f, 1f);

            Assert.That(result.magnitude, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(result.x, Is.GreaterThan(0f));
            Assert.That(result.y, Is.GreaterThan(0f));
        }

        [TestCase(0f, 9f, -2.5f, 2.5f, 2.5f)]
        [TestCase(0f, -9f, -2.5f, 2.5f, -2.5f)]
        [TestCase(1f, 0.4f, -2.5f, 2.5f, 1.4f)]
        public void DepthMovement_IsClampedToNarrowLane(
            float current,
            float delta,
            float minimum,
            float maximum,
            float expected)
        {
            Assert.That(
                PrototypeMovementMath.ClampDepth(current, delta, minimum, maximum),
                Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void JumpVelocity_IsPositiveForNegativeGravity()
        {
            float velocity = PrototypeMovementMath.CalculateJumpVelocity(2.2f, -24f);

            Assert.That(velocity, Is.EqualTo(Mathf.Sqrt(105.6f)).Within(0.0001f));
        }

        [Test]
        public void Flight_RequiresHoldDelayAirborneStateAndEnergy()
        {
            Assert.That(PrototypeMovementMath.ShouldApplyFlight(true, false, 0.17f, 0.18f, 100f), Is.False);
            Assert.That(PrototypeMovementMath.ShouldApplyFlight(true, true, 0.5f, 0.18f, 100f), Is.False);
            Assert.That(PrototypeMovementMath.ShouldApplyFlight(true, false, 0.5f, 0.18f, 0f), Is.False);
            Assert.That(PrototypeMovementMath.ShouldApplyFlight(true, false, 0.18f, 0.18f, 100f), Is.True);
        }

        [Test]
        public void JetEnergy_DrainsRecoversAndClamps()
        {
            float drained = PrototypeMovementMath.StepJetEnergy(100f, 1f, 100f, true, 28f, false, 22f);
            float recovered = PrototypeMovementMath.StepJetEnergy(drained, 1f, 100f, false, 28f, true, 22f);
            float clampedEmpty = PrototypeMovementMath.StepJetEnergy(5f, 1f, 100f, true, 28f, false, 22f);

            Assert.That(drained, Is.EqualTo(72f).Within(0.0001f));
            Assert.That(recovered, Is.EqualTo(94f).Within(0.0001f));
            Assert.That(clampedEmpty, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void FixedDepthCamera_IgnoresTargetDepth()
        {
            Vector3 result = PrototypeMovementMath.CalculateFixedDepthCameraPosition(
                new Vector3(4f, 2f, 99f),
                new Vector2(1f, 3f),
                -16f);

            Assert.That(result, Is.EqualTo(new Vector3(5f, 5f, -16f)));
        }

        [Test]
        public void FallRecovery_TriggersOnlyBelowThreshold()
        {
            Assert.That(PrototypeMovementMath.HasFallen(-8.01f, -8f), Is.True);
            Assert.That(PrototypeMovementMath.HasFallen(-8f, -8f), Is.False);
        }
    }
}
