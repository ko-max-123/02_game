using NUnit.Framework;

namespace YTCPrototype.Tests
{
    public sealed class K1V2MotionMathTests
    {
        [Test]
        public void LocomotionSelection_PrefersDominantAxisAndDepthSign()
        {
            Assert.That(K1V2MotionMath.SelectGroundedLocomotion(0f, 0f), Is.EqualTo(K1V2Locomotion.Idle));
            Assert.That(K1V2MotionMath.SelectGroundedLocomotion(1f, 0.5f), Is.EqualTo(K1V2Locomotion.WalkForward));
            Assert.That(K1V2MotionMath.SelectGroundedLocomotion(0.2f, 1f), Is.EqualTo(K1V2Locomotion.WalkDepthPositive));
            Assert.That(K1V2MotionMath.SelectGroundedLocomotion(0.2f, -1f), Is.EqualTo(K1V2Locomotion.WalkDepthNegative));
        }

        [Test]
        public void AnimationCycle_IsFrameRateIndependentAt30And60Fps()
        {
            float at30 = SimulateCycle(30, 2f, K1V2MotionMath.ForwardCycleSeconds);
            float at60 = SimulateCycle(60, 2f, K1V2MotionMath.ForwardCycleSeconds);

            Assert.That(at30, Is.EqualTo(at60).Within(0.0001f));
            Assert.That(at30, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void ReferenceSpeeds_ProduceOneTimesPlayback()
        {
            Assert.That(
                K1V2MotionMath.CalculateLocomotionRate(
                    K1V2Locomotion.WalkForward,
                    K1V2MotionMath.ForwardReferenceSpeed,
                    0f),
                Is.EqualTo(1f));
            Assert.That(
                K1V2MotionMath.CalculateLocomotionRate(K1V2Locomotion.WalkDepthPositive, 0f, 3.5f),
                Is.EqualTo(1f));
        }

        private static float SimulateCycle(int fps, float seconds, float cycleSeconds)
        {
            float normalizedTime = 0f;
            float deltaTime = 1f / fps;
            for (int frame = 0; frame < fps * seconds; frame++)
            {
                normalizedTime = K1V2MotionMath.AdvanceNormalizedCycle(
                    normalizedTime,
                    cycleSeconds,
                    1f,
                    deltaTime);
            }
            return normalizedTime;
        }
    }
}
