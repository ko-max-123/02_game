using UnityEngine;

namespace YTCPrototype
{
    public sealed class PrototypePlayerHealth : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maximumHealth = 100f;
        [SerializeField, Min(0.1f)] private float respawnDelay = 0.8f;
        [SerializeField] private PrototypePlayerController movement;
        [SerializeField] private PrototypePlayerCombat combat;

        private float currentHealth;
        private float respawnTimer;
        private float hitFeedback;
        private float damageDirectionFeedback;
        private float lastDamageDirection;
        private bool isRespawning;

        public float CurrentHealth => currentHealth;
        public float MaximumHealth => maximumHealth;
        public float HealthNormalized => maximumHealth <= 0f ? 0f : currentHealth / maximumHealth;
        public float HitFeedback => hitFeedback;
        public float DamageDirectionFeedback => damageDirectionFeedback;
        public float LastDamageDirection => lastDamageDirection;
        public bool IsRespawning => isRespawning;

        public void Configure(PrototypePlayerController playerMovement)
        {
            movement = playerMovement;
        }

        public void ConfigureCombat(PrototypePlayerCombat playerCombat)
        {
            combat = playerCombat;
        }

        private void Awake()
        {
            currentHealth = maximumHealth;
        }

        private void Update()
        {
            hitFeedback = Mathf.MoveTowards(hitFeedback, 0f, Time.deltaTime * 2.8f);
            damageDirectionFeedback = Mathf.MoveTowards(
                damageDirectionFeedback,
                0f,
                Time.deltaTime * 3.5f);
            if (!isRespawning)
            {
                return;
            }

            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0f)
            {
                CompleteRespawn();
            }
        }

        public void TakeDamage(float damage)
        {
            TakeDamage(damage, transform.position - Vector3.right);
        }

        public void TakeDamage(float damage, Vector3 sourcePosition)
        {
            if (isRespawning || damage <= 0f)
            {
                return;
            }

            currentHealth = PrototypeCombatMath.ApplyDamage(currentHealth, damage);
            hitFeedback = 1f;
            lastDamageDirection = Mathf.Sign(sourcePosition.x - transform.position.x);
            damageDirectionFeedback = 1f;
            if (PrototypeCombatMath.IsDefeated(currentHealth))
            {
                BeginRespawn();
            }
        }

        private void BeginRespawn()
        {
            isRespawning = true;
            respawnTimer = respawnDelay;
            if (movement != null)
            {
                movement.enabled = false;
            }

            if (combat != null)
            {
                combat.enabled = false;
            }
        }

        private void CompleteRespawn()
        {
            if (movement != null)
            {
                movement.ResetToSpawn();
                movement.enabled = true;
            }

            if (combat != null)
            {
                combat.enabled = true;
            }

            currentHealth = maximumHealth;
            isRespawning = false;
            hitFeedback = 0f;
            damageDirectionFeedback = 0f;
        }
    }
}
