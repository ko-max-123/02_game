using System;
using UnityEngine;

namespace YTCPrototype
{
    public sealed class PrototypeProjectile : MonoBehaviour
    {
        private static Material sharedMaterial;

        private readonly RaycastHit[] hitBuffer = new RaycastHit[24];
        private LineRenderer outerLine;
        private LineRenderer coreLine;
        private Transform owner;
        private Vector3 direction;
        private float speed;
        private float remainingRange;
        private float radius;
        private float damage;
        private float travelledDistance;
        private bool enemyProjectile;
        private bool resolved;

        public float Speed => speed;
        public float TravelledDistance => travelledDistance;
        public float CollisionRadius => radius;
        public bool IsEnemyProjectile => enemyProjectile;
        public float MaximumVisibleLength => 0.52f;

        public static PrototypeProjectile SpawnPlayer(
            Vector3 origin,
            Vector3 requestedDirection,
            float projectileSpeed,
            float maximumRange,
            float collisionRadius,
            float shotDamage,
            Transform shotOwner)
        {
            return Spawn(
                "PlayerProjectile",
                origin,
                requestedDirection,
                projectileSpeed,
                maximumRange,
                collisionRadius,
                shotDamage,
                shotOwner,
                false);
        }

        public static PrototypeProjectile SpawnEnemy(
            Vector3 origin,
            Vector3 requestedDirection,
            float projectileSpeed,
            float maximumRange,
            float collisionRadius,
            float shotDamage,
            Transform shotOwner)
        {
            return Spawn(
                "EnemyProjectile",
                origin,
                requestedDirection,
                projectileSpeed,
                maximumRange,
                collisionRadius,
                shotDamage,
                shotOwner,
                true);
        }

        private static PrototypeProjectile Spawn(
            string objectName,
            Vector3 origin,
            Vector3 requestedDirection,
            float projectileSpeed,
            float maximumRange,
            float collisionRadius,
            float shotDamage,
            Transform shotOwner,
            bool isEnemyProjectile)
        {
            if (requestedDirection.sqrMagnitude < 0.0001f)
            {
                return null;
            }

            GameObject projectileObject = new GameObject(objectName);
            PrototypeProjectile projectile = projectileObject.AddComponent<PrototypeProjectile>();
            projectile.Initialize(
                origin,
                requestedDirection,
                projectileSpeed,
                maximumRange,
                collisionRadius,
                shotDamage,
                shotOwner,
                isEnemyProjectile);
            return projectile;
        }

        private void Initialize(
            Vector3 origin,
            Vector3 requestedDirection,
            float projectileSpeed,
            float maximumRange,
            float collisionRadius,
            float shotDamage,
            Transform shotOwner,
            bool isEnemyProjectile)
        {
            transform.position = origin;
            direction = requestedDirection.normalized;
            speed = Mathf.Max(1f, projectileSpeed);
            remainingRange = Mathf.Max(0.1f, maximumRange);
            radius = Mathf.Clamp(collisionRadius, 0.03f, 0.2f);
            damage = Mathf.Max(0f, shotDamage);
            owner = shotOwner;
            enemyProjectile = isEnemyProjectile;
            CreateVisuals();
            UpdateVisuals();
        }

        private void Update()
        {
            if (resolved)
            {
                return;
            }

            float step = Mathf.Min(speed * Time.deltaTime, remainingRange);
            if (step <= 0f)
            {
                ResolveAt(transform.position, false);
                return;
            }

            Vector3 origin = transform.position;
            int hitCount = Physics.SphereCastNonAlloc(
                origin,
                radius,
                direction,
                hitBuffer,
                step,
                ~0,
                QueryTriggerInteraction.Ignore);
            Array.Sort(hitBuffer, 0, hitCount, RaycastHitDistanceComparer.Instance);

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = hitBuffer[i];
                if (ShouldIgnore(hit.collider))
                {
                    continue;
                }

                transform.position = origin + direction * hit.distance;
                travelledDistance += hit.distance;
                ResolveHit(hit);
                return;
            }

            transform.position = origin + direction * step;
            travelledDistance += step;
            remainingRange -= step;
            UpdateVisuals();
            if (remainingRange <= 0.001f)
            {
                ResolveAt(transform.position, false);
            }
        }

        private bool ShouldIgnore(Collider hitCollider)
        {
            if (hitCollider == null)
            {
                return true;
            }

            Transform hitTransform = hitCollider.transform;
            if (owner != null && (hitTransform == owner || hitTransform.IsChildOf(owner)))
            {
                return true;
            }

            if (enemyProjectile)
            {
                return hitCollider.GetComponentInParent<PrototypeEnemy>() != null;
            }

            return hitCollider.GetComponentInParent<PrototypePlayerHealth>() != null;
        }

        private void ResolveHit(RaycastHit hit)
        {
            bool appliedDamage = false;
            if (enemyProjectile)
            {
                PrototypePlayerHealth player = hit.collider.GetComponentInParent<PrototypePlayerHealth>();
                if (player != null)
                {
                    player.TakeDamage(damage, owner != null ? owner.position : transform.position - direction);
                    appliedDamage = true;
                }
            }
            else
            {
                PrototypeEnemy enemy = hit.collider.GetComponentInParent<PrototypeEnemy>();
                if (enemy != null)
                {
                    enemy.ApplyDamage(damage);
                    appliedDamage = true;
                }
            }

            ResolveAt(hit.point, appliedDamage);
        }

        private void ResolveAt(Vector3 point, bool showImpact)
        {
            if (resolved)
            {
                return;
            }

            resolved = true;
            if (showImpact)
            {
                PrototypeShotTracer.SpawnImpact(point);
            }
            Destroy(gameObject);
        }

        private void CreateVisuals()
        {
            Color outer = enemyProjectile
                ? new Color(1f, 0.08f, 0.05f, 1f)
                : new Color(1f, 0.42f, 0.04f, 1f);
            outerLine = CreateLine("ProjectileGlow", outer, enemyProjectile ? 0.12f : 0.105f, 0);
            coreLine = CreateLine("ProjectileCore", Color.white, enemyProjectile ? 0.042f : 0.036f, 1);
        }

        private LineRenderer CreateLine(string layerName, Color color, float width, int sortingOrder)
        {
            GameObject layer = new GameObject(layerName);
            layer.transform.SetParent(transform, false);
            LineRenderer line = layer.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = width;
            line.endWidth = width * 0.58f;
            line.numCapVertices = 5;
            line.sharedMaterial = GetSharedMaterial();
            line.startColor = color;
            line.endColor = color;
            line.sortingOrder = sortingOrder;
            return line;
        }

        private void UpdateVisuals()
        {
            float visibleLength = Mathf.Min(MaximumVisibleLength, 0.14f + travelledDistance);
            Vector3 head = transform.position + direction * 0.06f;
            Vector3 tail = transform.position - direction * visibleLength;
            outerLine.SetPosition(0, tail);
            outerLine.SetPosition(1, head);

            Vector3 coreTail = transform.position - direction * Mathf.Min(0.22f, visibleLength);
            coreLine.SetPosition(0, coreTail);
            coreLine.SetPosition(1, head);
        }

        private static Material GetSharedMaterial()
        {
            if (sharedMaterial != null)
            {
                return sharedMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Unlit/Color");
            sharedMaterial = new Material(shader)
            {
                name = "YTC Runtime Projectile Material",
                hideFlags = HideFlags.HideAndDontSave
            };
            return sharedMaterial;
        }

        private sealed class RaycastHitDistanceComparer : System.Collections.Generic.IComparer<RaycastHit>
        {
            public static readonly RaycastHitDistanceComparer Instance = new RaycastHitDistanceComparer();

            public int Compare(RaycastHit left, RaycastHit right)
            {
                return left.distance.CompareTo(right.distance);
            }
        }
    }
}
