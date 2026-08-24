using UnityEngine;

namespace YTC.Prototype
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class YamadaPrototypeController : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 5.5f;
        [SerializeField, Min(0.1f)] private float rotationSpeed = 720f;
        [SerializeField, Min(0.1f)] private float jumpHeight = 1.35f;
        [SerializeField] private float gravity = -24f;
        [SerializeField] private float fallResetHeight = -10f;
        [SerializeField, Min(0.1f)] private float depthLimit = 0.65f;

        private static readonly Vector2[] DepthMovementZones =
        {
            new Vector2(-14f, -9.2f),
            new Vector2(1.7f, 5.3f),
            new Vector2(10.1f, 14f)
        };

        private CharacterController characterController;
        private Vector3 spawnPoint;
        private float verticalVelocity;

        public bool IsGrounded => characterController != null && characterController.isGrounded;
        public float VerticalVelocity => verticalVelocity;
        public float DepthLimit => depthLimit;
        public bool IsDepthMovementAllowed => AllowsDepthMovementAt(transform.position.x);

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            spawnPoint = transform.position;
        }

        public void Configure(Vector3 initialSpawnPoint)
        {
            spawnPoint = initialSpawnPoint;
        }

        public void TeleportTo(Vector3 position)
        {
            characterController.enabled = false;
            transform.SetPositionAndRotation(position, Quaternion.identity);
            verticalVelocity = 0f;
            characterController.enabled = true;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                ResetToSpawn();
                return;
            }

            float horizontal = ReadAxis(KeyCode.A, KeyCode.D);
            float vertical = ReadAxis(KeyCode.S, KeyCode.W);
            Tick(
                new Vector2(horizontal, vertical),
                Input.GetKeyDown(KeyCode.Space),
                Time.deltaTime);
        }

        public void Tick(Vector2 moveInput, bool jumpRequested, float deltaTime)
        {
            float depthInput = IsDepthMovementAllowed ? moveInput.y : 0f;
            Vector3 planarDirection = YamadaMotorMath.PlanarDirection(moveInput.x, depthInput);

            if (planarDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(planarDirection, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * deltaTime);
            }

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (characterController.isGrounded && jumpRequested)
            {
                verticalVelocity = YamadaMotorMath.JumpVelocity(jumpHeight, gravity);
            }

            verticalVelocity = YamadaMotorMath.ApplyGravity(verticalVelocity, gravity, deltaTime);
            Vector3 velocity = planarDirection * moveSpeed + Vector3.up * verticalVelocity;
            characterController.Move(velocity * deltaTime);

            float clampedDepth = Mathf.Clamp(transform.position.z, -depthLimit, depthLimit);
            if (!Mathf.Approximately(clampedDepth, transform.position.z))
            {
                characterController.Move(
                    new Vector3(0f, 0f, clampedDepth - transform.position.z));
            }

            if (transform.position.y < fallResetHeight)
            {
                ResetToSpawn();
            }
        }

        public static bool AllowsDepthMovementAt(float xPosition)
        {
            foreach (Vector2 zone in DepthMovementZones)
            {
                if (xPosition >= zone.x && xPosition <= zone.y)
                {
                    return true;
                }
            }

            return false;
        }

        private static float ReadAxis(KeyCode negative, KeyCode positive)
        {
            float value = 0f;
            if (Input.GetKey(negative))
            {
                value -= 1f;
            }

            if (Input.GetKey(positive))
            {
                value += 1f;
            }

            return value;
        }

        private void ResetToSpawn()
        {
            TeleportTo(spawnPoint);
        }
    }
}
