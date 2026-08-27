using UnityEngine;

namespace YTCPrototype
{
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(Animator))]
    public sealed class K1V2AnimatorDriver : MonoBehaviour
    {
        public const string IdleState = "Idle_Loop";
        public const string WalkState = "WalkForward_Loop";
        public const string DepthPositiveState = "WalkDepth_Positive_Loop";
        public const string DepthNegativeState = "WalkDepth_Negative_Loop";
        public const string TurnLeftState = "Turn180_L";
        public const string TurnRightState = "Turn180_R";
        public const string JumpStartState = "Jump_Start";
        public const string JumpLoopState = "Jump_Loop";
        public const string LandState = "Land";
        public const string JetStartState = "Jet_Start";
        public const string JetLoopState = "Jet_Loop";
        public const string JetEndState = "Jet_End";
        public const string ShootState = "Shoot_Recoil";
        public const string LocomotionRateParameter = "LocomotionRate";

        [SerializeField] private PrototypePlayerController movement;
        [SerializeField] private PrototypePlayerCombat combat;
        [SerializeField] private Animator animator;
        [SerializeField, Min(0f)] private float crossFadeSeconds = 0.06f;

        private string currentBaseState;
        private float stateLockRemaining;
        private bool previousGrounded;
        private bool previousFlying;
        private bool hasBeenAirborne;
        private uint observedShotSequence;
        private float shootLayerRemaining;

        public string CurrentBaseState => currentBaseState;

        public void Configure(
            PrototypePlayerController playerMovement,
            PrototypePlayerCombat playerCombat,
            Animator targetAnimator)
        {
            movement = playerMovement;
            combat = playerCombat;
            animator = targetAnimator;
            observedShotSequence = combat != null ? combat.ShotSequence : 0;
        }

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }

        private void OnEnable()
        {
            currentBaseState = null;
            stateLockRemaining = 0f;
            previousGrounded = movement != null && movement.IsGrounded;
            previousFlying = movement != null && movement.IsFlying;
            hasBeenAirborne = false;
            shootLayerRemaining = 0f;
            if (animator != null && animator.layerCount > 1)
            {
                animator.SetLayerWeight(1, 0f);
            }
        }

        private void Update()
        {
            if (movement == null || animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            stateLockRemaining = Mathf.Max(0f, stateLockRemaining - Time.deltaTime);
            StepShootLayer();
            PlayShotIfRequested();

            bool grounded = movement.IsGrounded;
            bool flying = movement.IsFlying;

            if (!grounded)
            {
                hasBeenAirborne = true;
            }

            if (movement.IsTurning)
            {
                SetBaseState(movement.TurnDirection < 0f ? TurnLeftState : TurnRightState, 0.30f);
            }
            else if (flying && !previousFlying)
            {
                SetBaseState(JetStartState, 0.18f);
            }
            else if (!flying && previousFlying)
            {
                SetBaseState(JetEndState, 0.20f);
            }
            else if (!grounded && previousGrounded && !flying)
            {
                SetBaseState(JumpStartState, 0.24f);
            }
            else if (grounded && !previousGrounded && hasBeenAirborne)
            {
                SetBaseState(LandState, 0.28f);
                hasBeenAirborne = false;
            }
            else if (stateLockRemaining <= 0f)
            {
                SetBaseState(SelectSustainedState(grounded, flying), 0f);
            }

            SetLocomotionRate();
            previousGrounded = grounded;
            previousFlying = flying;
        }

        private string SelectSustainedState(bool grounded, bool flying)
        {
            if (flying)
            {
                return JetLoopState;
            }

            if (!grounded)
            {
                return JumpLoopState;
            }

            return K1V2MotionMath.SelectGroundedLocomotion(
                movement.HorizontalInput,
                movement.DepthInput) switch
            {
                K1V2Locomotion.WalkForward => WalkState,
                K1V2Locomotion.WalkDepthPositive => DepthPositiveState,
                K1V2Locomotion.WalkDepthNegative => DepthNegativeState,
                _ => IdleState
            };
        }

        private void SetBaseState(string stateName, float lockSeconds)
        {
            if (currentBaseState == stateName)
            {
                return;
            }

            int stateHash = Animator.StringToHash(stateName);
            if (!animator.HasState(0, stateHash))
            {
                Debug.LogError($"K1 V2 Animator state is missing: {stateName}", this);
                enabled = false;
                return;
            }

            if (currentBaseState == null)
            {
                animator.Play(stateHash, 0, 0f);
            }
            else
            {
                animator.CrossFadeInFixedTime(stateHash, crossFadeSeconds, 0, 0f);
            }

            currentBaseState = stateName;
            stateLockRemaining = lockSeconds;
        }

        private void PlayShotIfRequested()
        {
            if (combat == null || combat.ShotSequence == observedShotSequence)
            {
                return;
            }

            observedShotSequence = combat.ShotSequence;
            int stateHash = Animator.StringToHash(ShootState);
            if (animator.layerCount > 1 && animator.HasState(1, stateHash))
            {
                animator.SetLayerWeight(1, 1f);
                animator.Play(stateHash, 1, 0f);
                shootLayerRemaining = 0.15f;
            }
        }

        private void StepShootLayer()
        {
            if (animator == null || animator.layerCount <= 1 || shootLayerRemaining <= 0f)
            {
                return;
            }

            shootLayerRemaining = Mathf.Max(0f, shootLayerRemaining - Time.deltaTime);
            if (shootLayerRemaining <= 0f)
            {
                animator.SetLayerWeight(1, 0f);
            }
        }

        private void SetLocomotionRate()
        {
            K1V2Locomotion locomotion = currentBaseState switch
            {
                WalkState => K1V2Locomotion.WalkForward,
                DepthPositiveState => K1V2Locomotion.WalkDepthPositive,
                DepthNegativeState => K1V2Locomotion.WalkDepthNegative,
                _ => K1V2Locomotion.Idle
            };
            animator.SetFloat(
                LocomotionRateParameter,
                K1V2MotionMath.CalculateLocomotionRate(
                    locomotion,
                    movement.HorizontalInput * K1V2MotionMath.ForwardReferenceSpeed,
                    movement.DepthInput * 3.5f));
        }
    }
}
