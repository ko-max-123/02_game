using UnityEngine;

namespace YTCPrototype
{
    public static class PrototypeMovementMath
    {
        public static Vector2 ClampPlanarInput(float horizontal, float depth)
        {
            return Vector2.ClampMagnitude(new Vector2(horizontal, depth), 1f);
        }

        public static float ClampDepth(float currentDepth, float requestedDelta, float minimum, float maximum)
        {
            return Mathf.Clamp(currentDepth + requestedDelta, minimum, maximum);
        }

        public static float CalculateJumpVelocity(float jumpHeight, float gravity)
        {
            return Mathf.Sqrt(Mathf.Max(0f, jumpHeight) * -2f * Mathf.Min(-0.01f, gravity));
        }

        public static bool ShouldApplyFlight(
            bool spaceHeld,
            bool grounded,
            float heldDuration,
            float holdDelay,
            float currentEnergy)
        {
            return spaceHeld && !grounded && heldDuration >= holdDelay && currentEnergy > 0f;
        }

        public static float StepVerticalVelocity(
            float currentVelocity,
            float deltaTime,
            float gravity,
            bool applyingFlight,
            float flightAcceleration,
            float maximumFlightSpeed)
        {
            if (!applyingFlight)
            {
                return currentVelocity + gravity * deltaTime;
            }

            return Mathf.MoveTowards(
                currentVelocity,
                maximumFlightSpeed,
                Mathf.Max(0f, flightAcceleration) * deltaTime);
        }

        public static bool HasFallen(float height, float recoveryHeight)
        {
            return height < recoveryHeight;
        }

        public static float StepJetEnergy(
            float currentEnergy,
            float deltaTime,
            float maximumEnergy,
            bool consuming,
            float drainPerSecond,
            bool canRecover,
            float recoveryPerSecond)
        {
            float delta = consuming
                ? -Mathf.Max(0f, drainPerSecond) * deltaTime
                : canRecover
                    ? Mathf.Max(0f, recoveryPerSecond) * deltaTime
                    : 0f;

            return Mathf.Clamp(currentEnergy + delta, 0f, Mathf.Max(0f, maximumEnergy));
        }

        public static Vector3 CalculateFixedDepthCameraPosition(
            Vector3 targetPosition,
            Vector2 followOffset,
            float fixedDepth)
        {
            return new Vector3(
                targetPosition.x + followOffset.x,
                targetPosition.y + followOffset.y,
                fixedDepth);
        }
    }
}
