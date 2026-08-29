using UnityEngine;

namespace YTCPrototype
{
    public enum K1V2Locomotion
    {
        Idle,
        WalkForward,
        WalkDepthPositive,
        WalkDepthNegative
    }

    public static class K1V2MotionMath
    {
        public const float ForwardReferenceSpeed = 4.2f;
        public const float ForwardCycleSeconds = 0.80f;
        public const float DepthCycleSeconds = 0.86f;

        public static K1V2Locomotion SelectGroundedLocomotion(float horizontal, float depth)
        {
            float horizontalMagnitude = Mathf.Abs(horizontal);
            float depthMagnitude = Mathf.Abs(depth);
            if (horizontalMagnitude < 0.01f && depthMagnitude < 0.01f)
            {
                return K1V2Locomotion.Idle;
            }

            if (horizontalMagnitude >= depthMagnitude)
            {
                return K1V2Locomotion.WalkForward;
            }

            return depth >= 0f
                ? K1V2Locomotion.WalkDepthPositive
                : K1V2Locomotion.WalkDepthNegative;
        }

        public static float CalculateLocomotionRate(
            K1V2Locomotion locomotion,
            float actualHorizontalSpeed,
            float actualDepthSpeed)
        {
            return locomotion switch
            {
                K1V2Locomotion.WalkForward => Mathf.Clamp(
                    Mathf.Abs(actualHorizontalSpeed) / ForwardReferenceSpeed,
                    0.2f,
                    1.5f),
                K1V2Locomotion.WalkDepthPositive => Mathf.Clamp(
                    Mathf.Abs(actualDepthSpeed) / 3.5f,
                    0.2f,
                    1.5f),
                K1V2Locomotion.WalkDepthNegative => Mathf.Clamp(
                    Mathf.Abs(actualDepthSpeed) / 3.5f,
                    0.2f,
                    1.5f),
                _ => 1f
            };
        }

        public static float AdvanceNormalizedCycle(
            float normalizedTime,
            float cycleSeconds,
            float playbackRate,
            float deltaTime)
        {
            if (cycleSeconds <= 0f)
            {
                return normalizedTime;
            }

            return Mathf.Repeat(
                normalizedTime + Mathf.Max(0f, playbackRate) * deltaTime / cycleSeconds,
                1f);
        }
    }
}
