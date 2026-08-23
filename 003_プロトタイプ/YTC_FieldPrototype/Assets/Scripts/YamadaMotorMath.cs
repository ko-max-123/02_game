using System;
using UnityEngine;

namespace YTC.Prototype
{
    public static class YamadaMotorMath
    {
        public static Vector3 PlanarDirection(float horizontal, float vertical)
        {
            var direction = new Vector3(horizontal, 0f, vertical);
            return direction.sqrMagnitude > 1f ? direction.normalized : direction;
        }

        public static float JumpVelocity(float jumpHeight, float gravity)
        {
            if (jumpHeight < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(jumpHeight), "Jump height must not be negative.");
            }

            if (gravity >= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(gravity), "Gravity must be negative.");
            }

            return Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        public static float ApplyGravity(float verticalVelocity, float gravity, float deltaTime)
        {
            return verticalVelocity + gravity * Mathf.Max(0f, deltaTime);
        }
    }
}
