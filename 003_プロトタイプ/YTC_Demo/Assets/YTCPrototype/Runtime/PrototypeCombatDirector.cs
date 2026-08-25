using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Linq;

namespace YTCPrototype
{
    public sealed class PrototypeCombatDirector : MonoBehaviour
    {
        [SerializeField] private PrototypePlayerController player;
        [SerializeField] private PrototypePlayerHealth playerHealth;
        [SerializeField] private PrototypePlayerCombat playerCombat;

        private int initialEnemyCount;
        private int aliveEnemyCount;
        private bool allEnemiesDefeated;
        private bool standaloneSmokeTest;
        private float smokeTestExitTime;

        public int InitialEnemyCount => initialEnemyCount;
        public int AliveEnemyCount => aliveEnemyCount;
        public bool AllEnemiesDefeated => allEnemiesDefeated;

        public void Configure(
            PrototypePlayerController trackedPlayer,
            PrototypePlayerHealth trackedHealth,
            PrototypePlayerCombat trackedCombat)
        {
            player = trackedPlayer;
            playerHealth = trackedHealth;
            playerCombat = trackedCombat;
        }

        private void Start()
        {
            RecountEnemies();
            standaloneSmokeTest = Environment.GetCommandLineArgs().Contains("-ytc-smoke-test");
            if (standaloneSmokeTest)
            {
                smokeTestExitTime = Time.realtimeSinceStartup + 2f;
                Debug.Log("YTC standalone smoke test started.");
            }
        }

        private void Update()
        {
            if (standaloneSmokeTest && Time.realtimeSinceStartup >= smokeTestExitTime)
            {
                Debug.Log("YTC standalone smoke test completed successfully.");
                Application.Quit(0);
                return;
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartBattle();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
        }

        public void RecountEnemies()
        {
            PrototypeEnemy[] enemies = FindObjectsByType<PrototypeEnemy>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            initialEnemyCount = enemies.Length;
            aliveEnemyCount = enemies.Length;
            allEnemiesDefeated = initialEnemyCount > 0 && aliveEnemyCount == 0;
        }

        public void NotifyEnemyDefeated(PrototypeEnemy enemy)
        {
            aliveEnemyCount = Mathf.Max(0, aliveEnemyCount - 1);
            allEnemiesDefeated = initialEnemyCount > 0 && aliveEnemyCount == 0;
        }

        public void RestartBattle()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.buildIndex >= 0)
            {
                SceneManager.LoadScene(activeScene.buildIndex);
            }
            else
            {
                SceneManager.LoadScene(activeScene.path);
            }
        }
    }
}
