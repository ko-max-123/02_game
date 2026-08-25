using UnityEngine;

namespace YTCPrototype
{
    public static class PrototypeCombatMath
    {
        public static float ApplyDamage(float currentHealth, float damage)
        {
            return Mathf.Max(0f, currentHealth - Mathf.Max(0f, damage));
        }

        public static bool IsDefeated(float currentHealth)
        {
            return currentHealth <= 0f;
        }

        public static Vector3 NormalizeAimDirection(Vector3 requestedDirection, Vector3 fallbackDirection)
        {
            Vector3 direction = requestedDirection.sqrMagnitude > 0.0001f
                ? requestedDirection
                : fallbackDirection;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.right;
        }
    }
}
