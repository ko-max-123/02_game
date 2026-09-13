using UnityEngine;

namespace YTCPrototype
{
    public sealed class PrototypeEnemy : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maximumHealth = 50f;
        [SerializeField, Min(0f)] private float patrolDistance = 1.4f;
        [SerializeField, Min(0f)] private float patrolSpeed = 1.15f;
        [SerializeField, Min(1f)] private float attackRange = 10f;
        [SerializeField, Min(0.1f)] private float attackInterval = 1.6f;
        [SerializeField, Min(0f)] private float attackDamage = 12f;
        [SerializeField, Range(0.25f, 0.8f)] private float attackTelegraphDuration = 0.32f;
        [SerializeField, Min(1f)] private float projectileSpeed = 9f;
        [SerializeField, Range(0.03f, 0.2f)] private float projectileRadius = 0.09f;
        [SerializeField, Range(0.2f, 0.5f)] private float defeatDisplayDuration = 0.32f;
        [SerializeField] private PrototypeCombatDirector director;
        [SerializeField] private PrototypePlayerHealth target;

        private Renderer[] renderers;
        private MaterialPropertyBlock propertyBlock;
        private Vector3 patrolCenter;
        private float currentHealth;
        private float patrolPhase;
        private float nextAttackTime;
        private float hitFlash;
        private float telegraphTimer;
        private float defeatTimer;
        private bool isTelegraphing;
        private bool defeated;
        private Vector3 lockedAimDirection = Vector3.left;

        public float CurrentHealth => currentHealth;
        public float MaximumHealth => maximumHealth;
        public float HealthNormalized => maximumHealth <= 0f ? 0f : currentHealth / maximumHealth;
        public bool IsDefeated => defeated;
        public Vector3 LockedAimDirection => lockedAimDirection;
        public float ProjectileSpeed => projectileSpeed;

        public void Configure(
            PrototypeCombatDirector combatDirector,
            PrototypePlayerHealth playerTarget,
            float phase,
            float enemyAttackInterval)
        {
            director = combatDirector;
            target = playerTarget;
            patrolPhase = phase;
            attackInterval = enemyAttackInterval;
            patrolCenter = transform.position;
        }

        public void ConfigureV2Combat(float telegraphSeconds, float shotSpeed)
        {
            attackTelegraphDuration = Mathf.Clamp(telegraphSeconds, 0.25f, 0.8f);
            projectileSpeed = Mathf.Max(1f, shotSpeed);
            projectileRadius = 0.09f;
        }

        private void Awake()
        {
            currentHealth = maximumHealth;
            patrolCenter = transform.position;
            renderers = GetComponentsInChildren<Renderer>(true);
            propertyBlock = new MaterialPropertyBlock();
            nextAttackTime = Time.time + attackInterval * 0.65f;
        }

        private void Update()
        {
            if (defeated)
            {
                defeatTimer -= Time.deltaTime;
                transform.position += Vector3.down * (Time.deltaTime * 0.85f);
                UpdateVisualFeedback();
                if (defeatTimer <= 0f)
                {
                    gameObject.SetActive(false);
                }
                return;
            }

            float offset = Mathf.Sin(Time.time * patrolSpeed + patrolPhase) * patrolDistance;
            transform.position = new Vector3(
                patrolCenter.x + offset,
                patrolCenter.y,
                patrolCenter.z);

            hitFlash = Mathf.MoveTowards(hitFlash, 0f, Time.deltaTime * 7f);
            UpdateVisualFeedback();
            TryAttack();
        }

        public void ApplyDamage(float damage)
        {
            if (defeated || damage <= 0f)
            {
                return;
            }

            currentHealth = PrototypeCombatMath.ApplyDamage(currentHealth, damage);
            hitFlash = 1f;
            UpdateVisualFeedback();
            if (PrototypeCombatMath.IsDefeated(currentHealth))
            {
                defeated = true;
                defeatTimer = defeatDisplayDuration;
                isTelegraphing = false;
                Collider enemyCollider = GetComponent<Collider>();
                if (enemyCollider != null)
                {
                    enemyCollider.enabled = false;
                }

                Transform sensor = transform.Find("EnemySensorTriangle");
                if (sensor != null)
                {
                    sensor.gameObject.SetActive(false);
                }

                PrototypeShotTracer.SpawnDefeat(transform.position + Vector3.up * 1.05f);
                director?.NotifyEnemyDefeated(this);
            }
        }

        private void TryAttack()
        {
            if (target == null || target.IsRespawning)
            {
                isTelegraphing = false;
                return;
            }

            Vector3 origin = transform.position + Vector3.up * 1.1f;
            Vector3 targetPoint = target.transform.position + Vector3.up * 1.05f;
            Vector3 delta = targetPoint - origin;
            if (delta.sqrMagnitude > attackRange * attackRange)
            {
                isTelegraphing = false;
                return;
            }

            if (isTelegraphing)
            {
                telegraphTimer -= Time.deltaTime;
                hitFlash = Mathf.PingPong(Time.time * 12f, 1f);
                if (telegraphTimer <= 0f)
                {
                    FireAtPlayer(origin, lockedAimDirection);
                    isTelegraphing = false;
                    nextAttackTime = Time.time + attackInterval;
                }

                return;
            }

            if (Time.time < nextAttackTime)
            {
                return;
            }

            isTelegraphing = true;
            telegraphTimer = attackTelegraphDuration;
            lockedAimDirection = delta.normalized;
            PrototypeShotTracer.SpawnTelegraph(origin, targetPoint, attackTelegraphDuration);
        }

        private void FireAtPlayer(Vector3 origin, Vector3 direction)
        {
            PrototypeProjectile.SpawnEnemy(
                origin + direction * 0.16f,
                direction,
                projectileSpeed,
                attackRange,
                projectileRadius,
                attackDamage,
                transform);
        }

        private void UpdateVisualFeedback()
        {
            if (renderers == null)
            {
                return;
            }

            Color baseColor = defeated
                ? new Color(0.2f, 0.22f, 0.23f)
                : new Color(0.24f, 0.28f, 0.3f);
            Color color = Color.Lerp(baseColor, Color.white, hitFlash);
            foreach (Renderer targetRenderer in renderers)
            {
                Color rendererColor = targetRenderer.name.Contains("SensorTriangle")
                    ? Color.Lerp(new Color(0.9f, 0.1f, 0.1f), Color.white, hitFlash)
                    : color;
                targetRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_BaseColor", rendererColor);
                propertyBlock.SetColor("_Color", rendererColor);
                targetRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void OnGUI()
        {
            Camera camera = Camera.main;
            if (defeated || camera == null)
            {
                return;
            }

            Vector3 screen = camera.WorldToScreenPoint(transform.position + Vector3.up * 2.15f);
            if (screen.z <= 0f)
            {
                return;
            }

            const float width = 84f;
            const float height = 9f;
            Rect background = new Rect(
                screen.x - width * 0.5f,
                Screen.height - screen.y,
                width,
                height);
            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.8f);
            GUI.DrawTexture(background, Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.2f, 0.12f, 1f);
            GUI.DrawTexture(
                new Rect(background.x + 2f, background.y + 2f, (width - 4f) * HealthNormalized, height - 4f),
                Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
