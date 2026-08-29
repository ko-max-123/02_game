using System;
using UnityEngine;

namespace YTCPrototype
{
    public sealed class PrototypePlayerCombat : MonoBehaviour
    {
        [SerializeField] private PrototypePlayerController movement;
        [SerializeField] private PrototypePlayerHealth health;
        [SerializeField] private Camera aimCamera;
        [SerializeField] private Transform muzzle;
        [SerializeField, Min(1f)] private float damagePerShot = 25f;
        [SerializeField, Min(1f)] private float range = 42f;
        [SerializeField, Min(0.01f)] private float shotInterval = 0.14f;

        private Vector3 currentAimDirection = Vector3.right;
        private float nextShotTime;
        private float shotFeedback;
        private uint shotSequence;

        public Vector3 CurrentAimDirection => currentAimDirection;
        public Vector3 MuzzlePosition => muzzle != null ? muzzle.position : transform.position + Vector3.up * 1.2f;
        public float ShotFeedback => shotFeedback;
        public uint ShotSequence => shotSequence;

        public void Configure(
            PrototypePlayerController playerMovement,
            PrototypePlayerHealth playerHealth,
            Camera camera,
            Transform muzzleTransform)
        {
            movement = playerMovement;
            health = playerHealth;
            aimCamera = camera;
            muzzle = muzzleTransform;
        }

        private void Awake()
        {
            if (aimCamera == null)
            {
                aimCamera = Camera.main;
            }
        }

        private void Update()
        {
            shotFeedback = Mathf.MoveTowards(shotFeedback, 0f, Time.deltaTime * 9f);
            UpdateMouseAim();

            if (health != null && health.IsRespawning)
            {
                return;
            }

            if (Input.GetMouseButton(0))
            {
                TryFire(currentAimDirection, false);
            }
            else if (Input.GetKey(KeyCode.J))
            {
                Vector3 facing = movement != null ? movement.FacingDirection : currentAimDirection;
                TryFire(facing, false);
            }
        }

        public bool FireForValidation(Vector3 direction)
        {
            return TryFire(direction, true);
        }

        private void UpdateMouseAim()
        {
            if (aimCamera == null)
            {
                return;
            }

            Ray ray = aimCamera.ScreenPointToRay(Input.mousePosition);
            Plane aimPlane = new Plane(Vector3.forward, new Vector3(0f, 0f, transform.position.z));
            if (!aimPlane.Raycast(ray, out float distance))
            {
                return;
            }

            Vector3 aimPoint = ray.GetPoint(distance);
            Vector3 requested = aimPoint - MuzzlePosition;
            requested.z = 0f;
            currentAimDirection = PrototypeCombatMath.NormalizeAimDirection(
                requested,
                movement != null ? movement.FacingDirection : Vector3.right);

            if (movement != null && Mathf.Abs(currentAimDirection.x) > 0.05f)
            {
                movement.SetFacingDirection(currentAimDirection.x);
            }
        }

        private bool TryFire(Vector3 requestedDirection, bool bypassCooldown)
        {
            if (!bypassCooldown && movement != null && movement.IsTurning)
            {
                return false;
            }

            if (!bypassCooldown && Time.time < nextShotTime)
            {
                return false;
            }

            Vector3 direction = PrototypeCombatMath.NormalizeAimDirection(
                requestedDirection,
                movement != null ? movement.FacingDirection : Vector3.right);
            nextShotTime = Time.time + shotInterval;
            shotFeedback = 1f;
            shotSequence++;

            Vector3 origin = MuzzlePosition;
            Vector3 end = origin + direction * range;
            bool hitEnemy = false;
            RaycastHit[] hits = Physics.RaycastAll(
                origin,
                direction,
                range,
                ~0,
                QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            foreach (RaycastHit hit in hits)
            {
                if (hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                end = hit.point;
                PrototypeEnemy enemy = hit.collider.GetComponentInParent<PrototypeEnemy>();
                if (enemy != null)
                {
                    enemy.ApplyDamage(damagePerShot);
                    hitEnemy = true;
                    PrototypeShotTracer.SpawnImpact(hit.point);
                }

                break;
            }

            PrototypeShotTracer.SpawnPlayerShot(origin, end);
            return hitEnemy;
        }
    }
}
