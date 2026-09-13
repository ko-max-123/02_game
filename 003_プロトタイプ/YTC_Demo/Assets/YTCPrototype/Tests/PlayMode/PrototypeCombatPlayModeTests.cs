using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YTCPrototype.Tests
{
    public sealed class PrototypeCombatPlayModeTests
    {
        [UnityTest]
        public IEnumerator V2Scene_AnimatesRigAndRoutesShotsThroughK11Muzzle()
        {
            SceneManager.LoadScene("YTC_Demo_V2");
            yield return null;
            yield return null;

            PrototypePlayerController movement = Object.FindFirstObjectByType<PrototypePlayerController>();
            PrototypePlayerCombat combat = Object.FindFirstObjectByType<PrototypePlayerCombat>();
            K1V2AnimatorDriver driver = Object.FindFirstObjectByType<K1V2AnimatorDriver>();
            Animator animator = driver != null ? driver.GetComponent<Animator>() : null;

            Assert.That(movement, Is.Not.Null);
            Assert.That(combat, Is.Not.Null);
            Assert.That(driver, Is.Not.Null);
            Assert.That(animator, Is.Not.Null);
            Assert.That(animator.runtimeAnimatorController, Is.Not.Null);
            Assert.That(animator.layerCount, Is.EqualTo(2));
            Assert.That(combat.MuzzlePosition, Is.Not.EqualTo(movement.transform.position));

            uint before = combat.ShotSequence;
            combat.FireForValidation(Vector3.right);
            Assert.That(combat.ShotSequence, Is.EqualTo(before + 1));
            yield return null;

            Assert.That(animator.GetLayerWeight(1), Is.GreaterThan(0.99f));
            Assert.That(driver.enabled, Is.True);
            Assert.That(driver.CurrentBaseState, Is.Not.Null.And.Not.Empty);

            Transform upperLegLeft = FindDescendant(animator.transform, "UpperLeg_L");
            Transform lowerLegLeft = FindDescendant(animator.transform, "LowerLeg_L");
            Transform upperArmLeft = FindDescendant(animator.transform, "UpperArm_L");
            Assert.That(upperLegLeft, Is.Not.Null);
            Assert.That(lowerLegLeft, Is.Not.Null);
            Assert.That(upperArmLeft, Is.Not.Null);

            driver.enabled = false;
            animator.Play(K1V2AnimatorDriver.BaseStateHash(K1V2AnimatorDriver.WalkState), 0, 0f);
            animator.Update(0f);
            Quaternion upperLegBefore = upperLegLeft.localRotation;
            Quaternion lowerLegBefore = lowerLegLeft.localRotation;
            Quaternion upperArmBefore = upperArmLeft.localRotation;
            animator.Update(0.2f);
            Assert.That(Quaternion.Angle(upperLegBefore, upperLegLeft.localRotation), Is.GreaterThan(3f));
            Assert.That(Quaternion.Angle(lowerLegBefore, lowerLegLeft.localRotation), Is.GreaterThan(3f));
            Assert.That(Quaternion.Angle(upperArmBefore, upperArmLeft.localRotation), Is.GreaterThan(3f));
        }

        [UnityTest]
        public IEnumerator ShotDamageDefeatVictoryAndRespawn_CompleteCombatLoop()
        {
            SceneManager.LoadScene("YTC_Demo");
            yield return null;
            yield return null;

            PrototypeCombatDirector director = Object.FindFirstObjectByType<PrototypeCombatDirector>();
            PrototypePlayerCombat combat = Object.FindFirstObjectByType<PrototypePlayerCombat>();
            PrototypePlayerHealth health = Object.FindFirstObjectByType<PrototypePlayerHealth>();
            PrototypePlayerController movement = Object.FindFirstObjectByType<PrototypePlayerController>();
            PrototypeEnemy[] enemies = Object.FindObjectsByType<PrototypeEnemy>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .OrderBy(enemy => enemy.transform.position.x)
                .ToArray();

            Assert.That(director, Is.Not.Null);
            Assert.That(combat, Is.Not.Null);
            Assert.That(health, Is.Not.Null);
            Assert.That(enemies.Length, Is.EqualTo(3));
            Assert.That(director.AliveEnemyCount, Is.EqualTo(3));

            foreach (PrototypeEnemy enemy in enemies)
            {
                enemy.enabled = false;
            }

            PrototypeEnemy target = enemies[0];
            CharacterController character = movement.GetComponent<CharacterController>();
            character.enabled = false;
            movement.transform.position = target.transform.position + Vector3.left * 3f;
            character.enabled = true;
            Physics.SyncTransforms();

            Vector3 aim = target.transform.position + Vector3.up * 0.95f - combat.MuzzlePosition;
            float healthBefore = target.CurrentHealth;
            Assert.That(combat.FireForValidation(aim), Is.True);
            Assert.That(target.CurrentHealth, Is.EqualTo(healthBefore), "Projectile damage must not be hitscan.");
            float timeout = Time.time + 1f;
            while (Mathf.Approximately(target.CurrentHealth, healthBefore) && Time.time < timeout)
            {
                yield return null;
            }
            Assert.That(target.CurrentHealth, Is.LessThan(healthBefore));

            Assert.That(combat.FireForValidation(aim), Is.True);
            timeout = Time.time + 1f;
            while (!target.IsDefeated && Time.time < timeout)
            {
                yield return null;
            }
            Assert.That(target.IsDefeated, Is.True);

            foreach (PrototypeEnemy enemy in enemies.Skip(1))
            {
                enemy.ApplyDamage(999f);
            }

            yield return null;
            Assert.That(director.AliveEnemyCount, Is.EqualTo(0));
            Assert.That(director.AllEnemiesDefeated, Is.True);

            director.RestartBattle();
            yield return null;
            yield return null;

            director = Object.FindFirstObjectByType<PrototypeCombatDirector>();
            health = Object.FindFirstObjectByType<PrototypePlayerHealth>();
            movement = Object.FindFirstObjectByType<PrototypePlayerController>();
            PrototypeEnemy[] restartedEnemies = Object.FindObjectsByType<PrototypeEnemy>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            Assert.That(restartedEnemies.Length, Is.EqualTo(3));
            Assert.That(director.AliveEnemyCount, Is.EqualTo(3));
            Assert.That(director.AllEnemiesDefeated, Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(health.MaximumHealth));
            Assert.That(movement.transform.position.x, Is.EqualTo(-14f).Within(0.1f));

            health.TakeDamage(999f, health.transform.position + Vector3.right);
            Assert.That(health.IsRespawning, Is.True);
            yield return new WaitForSeconds(0.9f);
            Assert.That(health.IsRespawning, Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(health.MaximumHealth));
        }

        [UnityTest]
        public IEnumerator EnemyProjectile_KeepsLockedDirectionAndCanBeDodged()
        {
            SceneManager.LoadScene("YTC_Demo_V2");
            yield return null;
            yield return null;

            PrototypePlayerController movement = Object.FindFirstObjectByType<PrototypePlayerController>();
            PrototypePlayerHealth health = Object.FindFirstObjectByType<PrototypePlayerHealth>();
            foreach (PrototypeEnemy enemy in Object.FindObjectsByType<PrototypeEnemy>(
                         FindObjectsInactive.Exclude,
                         FindObjectsSortMode.None))
            {
                enemy.enabled = false;
            }

            CharacterController character = movement.GetComponent<CharacterController>();
            character.enabled = false;
            movement.transform.position = new Vector3(0f, 6f, 0f);
            character.enabled = true;
            Physics.SyncTransforms();

            Vector3 lockedTarget = movement.transform.position + Vector3.up;
            Vector3 origin = lockedTarget + Vector3.left * 4f;
            float healthBefore = health.CurrentHealth;
            PrototypeProjectile projectile = PrototypeProjectile.SpawnEnemy(
                origin,
                lockedTarget - origin,
                9f,
                8f,
                0.09f,
                12f,
                null);
            Assert.That(projectile, Is.Not.Null);
            Assert.That(projectile.MaximumVisibleLength, Is.LessThanOrEqualTo(0.52f));
            Assert.That(health.CurrentHealth, Is.EqualTo(healthBefore));

            character.Move(new Vector3(-6f, 2.2f, 0f));
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.65f);

            Assert.That(health.CurrentHealth, Is.EqualTo(healthBefore));
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            foreach (Transform child in root)
            {
                Transform found = FindDescendant(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
