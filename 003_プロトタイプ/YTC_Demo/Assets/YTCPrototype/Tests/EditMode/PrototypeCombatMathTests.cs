using NUnit.Framework;
using UnityEngine;

namespace YTCPrototype.Tests
{
    public sealed class PrototypeCombatMathTests
    {
        [TestCase(100f, 25f, 75f)]
        [TestCase(20f, 50f, 0f)]
        [TestCase(40f, -10f, 40f)]
        public void Damage_ClampsAtZeroAndRejectsNegativeDamage(
            float current,
            float damage,
            float expected)
        {
            Assert.That(PrototypeCombatMath.ApplyDamage(current, damage), Is.EqualTo(expected));
        }

        [Test]
        public void Defeat_OnlyOccursAtZeroHealth()
        {
            Assert.That(PrototypeCombatMath.IsDefeated(1f), Is.False);
            Assert.That(PrototypeCombatMath.IsDefeated(0f), Is.True);
        }

        [Test]
        public void AimDirection_IsNormalizedAndUsesFacingFallback()
        {
            Vector3 normalized = PrototypeCombatMath.NormalizeAimDirection(
                new Vector3(4f, 3f, 0f),
                Vector3.left);
            Assert.That(Vector3.Distance(normalized, new Vector3(0.8f, 0.6f, 0f)), Is.LessThan(0.0001f));
            Assert.That(
                PrototypeCombatMath.NormalizeAimDirection(Vector3.zero, Vector3.left),
                Is.EqualTo(Vector3.left));
        }
    }
}
