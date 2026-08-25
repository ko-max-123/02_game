using UnityEngine;

namespace YTCPrototype
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PrototypePlayerController : MonoBehaviour
    {
        [Header("2.5D movement")]
        [SerializeField, Min(0f)] private float horizontalSpeed = 7f;
        [SerializeField, Min(0f)] private float depthSpeed = 3.5f;
        [SerializeField] private float minimumDepth = -2.5f;
        [SerializeField] private float maximumDepth = 2.5f;

        [Header("Jump and flight")]
        [SerializeField, Min(0f)] private float jumpHeight = 2.2f;
        [SerializeField] private float gravity = -24f;
        [SerializeField, Min(0f)] private float flightHoldDelay = 0.18f;
        [SerializeField, Min(0f)] private float flightAcceleration = 20f;
        [SerializeField, Min(0f)] private float maximumFlightSpeed = 5.5f;

        [Header("Jet energy")]
        [SerializeField, Min(1f)] private float maximumJetEnergy = 100f;
        [SerializeField, Min(0f)] private float jetEnergyDrainPerSecond = 28f;
        [SerializeField, Min(0f)] private float jetEnergyRecoveryPerSecond = 22f;
        [SerializeField, Min(0f)] private float jetRecoveryDelay = 0.65f;

        [Header("Ground and recovery")]
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField, Range(0.05f, 0.6f)] private float groundProbeRadius = 0.36f;
        [SerializeField] private float fallRecoveryHeight = -8f;

        [Header("Optional visual")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float faceRightYaw = 90f;
        [SerializeField] private float faceLeftYaw = -90f;

        private CharacterController characterController;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private float verticalVelocity;
        private float spaceHeldDuration;
        private float currentJetEnergy;
        private float timeSinceJetUse;
        private bool isGrounded;
        private bool isFlying;
        private float facingSign = 1f;
        private readonly Collider[] groundProbeHits = new Collider[8];

        public bool IsGrounded => isGrounded;
        public bool IsFlying => isFlying;
        public float CurrentDepth => transform.position.z;
        public float MinimumDepth => minimumDepth;
        public float MaximumDepth => maximumDepth;
        public float CurrentJetEnergy => currentJetEnergy;
        public float MaximumJetEnergy => maximumJetEnergy;
        public float JetEnergyNormalized => maximumJetEnergy <= 0f ? 0f : currentJetEnergy / maximumJetEnergy;
        public Vector3 FacingDirection => facingSign >= 0f ? Vector3.right : Vector3.left;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            currentJetEnergy = maximumJetEnergy;
            CaptureSpawnPose();
        }

        private void OnValidate()
        {
            if (minimumDepth > maximumDepth)
            {
                (minimumDepth, maximumDepth) = (maximumDepth, minimumDepth);
            }

            gravity = Mathf.Min(-0.01f, gravity);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                ResetToSpawn();
                return;
            }

            isGrounded = ProbeGround();
            if (isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            bool spacePressed = Input.GetKeyDown(KeyCode.Space);
            bool spaceHeld = Input.GetKey(KeyCode.Space);

            if (spacePressed && isGrounded)
            {
                verticalVelocity = PrototypeMovementMath.CalculateJumpVelocity(jumpHeight, gravity);
                spaceHeldDuration = 0f;
            }

            if (spaceHeld)
            {
                spaceHeldDuration += Time.deltaTime;
            }
            else
            {
                spaceHeldDuration = 0f;
            }

            isFlying = PrototypeMovementMath.ShouldApplyFlight(
                spaceHeld,
                isGrounded,
                spaceHeldDuration,
                flightHoldDelay,
                currentJetEnergy);

            if (isFlying)
            {
                timeSinceJetUse = 0f;
            }
            else
            {
                timeSinceJetUse += Time.deltaTime;
            }

            currentJetEnergy = PrototypeMovementMath.StepJetEnergy(
                currentJetEnergy,
                Time.deltaTime,
                maximumJetEnergy,
                isFlying,
                jetEnergyDrainPerSecond,
                timeSinceJetUse >= jetRecoveryDelay,
                jetEnergyRecoveryPerSecond);

            verticalVelocity = PrototypeMovementMath.StepVerticalVelocity(
                verticalVelocity,
                Time.deltaTime,
                gravity,
                isFlying,
                flightAcceleration,
                maximumFlightSpeed);

            float horizontal = ReadAxis(KeyCode.A, KeyCode.D);
            float depth = ReadAxis(KeyCode.S, KeyCode.W);
            Vector2 planarInput = PrototypeMovementMath.ClampPlanarInput(horizontal, depth);

            float horizontalDelta = planarInput.x * horizontalSpeed * Time.deltaTime;
            float requestedDepthDelta = planarInput.y * depthSpeed * Time.deltaTime;
            float targetDepth = PrototypeMovementMath.ClampDepth(
                transform.position.z,
                requestedDepthDelta,
                minimumDepth,
                maximumDepth);

            Vector3 motion = new Vector3(
                horizontalDelta,
                verticalVelocity * Time.deltaTime,
                targetDepth - transform.position.z);

            characterController.Move(motion);
            UpdateFacing(planarInput.x);

            if (PrototypeMovementMath.HasFallen(transform.position.y, fallRecoveryHeight))
            {
                ResetToSpawn();
            }
        }

        public void CaptureSpawnPose()
        {
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
        }

        public void ResetToSpawn()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            characterController.enabled = false;
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            characterController.enabled = true;

            verticalVelocity = 0f;
            spaceHeldDuration = 0f;
            currentJetEnergy = maximumJetEnergy;
            timeSinceJetUse = 0f;
            isFlying = false;
            isGrounded = false;
        }

        public void ConfigureVisualRoot(Transform root)
        {
            visualRoot = root;
        }

        public void SetFacingDirection(float horizontalDirection)
        {
            UpdateFacing(horizontalDirection);
        }

        private bool ProbeGround()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            Vector3 probeCenter = transform.position
                + characterController.center
                + Vector3.down * (characterController.height * 0.5f - groundProbeRadius);

            if (characterController.isGrounded)
            {
                return true;
            }

            int hitCount = Physics.OverlapSphereNonAlloc(
                probeCenter,
                groundProbeRadius,
                groundProbeHits,
                groundLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = groundProbeHits[i];
                if (hit != null && !hit.transform.IsChildOf(transform))
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateFacing(float horizontalInput)
        {
            if (Mathf.Abs(horizontalInput) < 0.01f)
            {
                return;
            }

            facingSign = horizontalInput > 0f ? 1f : -1f;
            if (visualRoot == null)
            {
                return;
            }

            float yaw = facingSign > 0f ? faceRightYaw : faceLeftYaw;
            visualRoot.localRotation = Quaternion.Euler(0f, yaw, 0f);
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
    }
}
