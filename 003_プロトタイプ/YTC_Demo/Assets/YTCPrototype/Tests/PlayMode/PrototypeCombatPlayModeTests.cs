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
            Assert.That(target.CurrentHealth, Is.LessThan(healthBefore));
            Assert.That(combat.FireForValidation(aim), Is.True);
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
    }
}
